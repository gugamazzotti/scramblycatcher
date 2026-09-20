using UnityEngine;
using UnityEngine.EventSystems;

namespace Scrambly.Catch
{
    /// <summary>
    /// Horizontal fox catcher driven by mouse drag or a single touch, clamped to the playfield lane.
    /// </summary>
    /// <remarks>
    /// Bootstrap creates the fox billboard and calls <see cref="SetVisual"/>; <see cref="GameDirector"/>
    /// calls <see cref="Tick"/> so this component has no Update of its own.
    /// Pointer mapping is a camera ray against the Z=0 plane (same plane as falling items).
    /// A stroke that begins on HUD (Restart, CTA) is ignored for the rest of that press so buttons stay clickable.
    /// Multi-touch is disabled in bootstrap; only finger 0 / left mouse is sampled.
    /// </remarks>
    public sealed class CatcherController : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private float _moveLerp = 22f;

        private Camera _camera;
        private Playfield _playfield;
        private bool _holding;
        private bool _ignoreUiForStroke;
        private float _targetX;
        private float _smoothVelocity;
        private float _facing = 1f;
        private Vector3 _visualBaseScale = Vector3.one;

        /// <summary>
        /// World-space origin used for catch distance checks, slightly above the fox root so items meet the snout.
        /// </summary>
        public Vector3 CatchPoint => new Vector3(transform.position.x, transform.position.y + 0.95f, 0f);

        /// <summary>
        /// Wires camera, lane, and horizontal smoothing. Places the fox at X=0 on the current catcher Y.
        /// </summary>
        /// <param name="gameplayCamera">Camera used to unproject pointer position onto Z=0.</param>
        /// <param name="playfield">Source of CatcherY and ClampX.</param>
        /// <param name="moveLerp">SmoothDamp rate from <see cref="GameConfig.CatcherLerp"/>.</param>
        public void Initialize(Camera gameplayCamera, Playfield playfield, float moveLerp)
        {
            _camera = gameplayCamera;
            _playfield = playfield;
            _moveLerp = moveLerp;
            _targetX = 0f;
            transform.position = new Vector3(0f, playfield.CatcherY, 0f);
        }

        /// <summary>
        /// Returns the fox to the center of the current lane and drops any held pointer. Used on Restart.
        /// </summary>
        public void ResetState()
        {
            CancelPointer();
            _targetX = 0f;
            _smoothVelocity = 0f;
            _facing = 1f;
            if (_playfield != null)
            {
                transform.position = new Vector3(0f, _playfield.CatcherY, 0f);
            }
        }

        /// <summary>
        /// Drops an in-progress drag after focus loss, round end, or a canceled touch.
        /// </summary>
        /// <remarks>
        /// Does not move the transform. Call this whenever input should no longer drive <c>_targetX</c>.
        /// </remarks>
        public void CancelPointer()
        {
            _holding = false;
            _ignoreUiForStroke = false;
        }

        /// <summary>
        /// Steps pointer tracking, SmoothDamp toward the target X, and fox facing/tilt/bob.
        /// </summary>
        /// <param name="deltaTime">Clamped scaled delta from <see cref="PlayableTime"/>.</param>
        /// <param name="inputEnabled">False while paused, in Boot, or after the round ends.</param>
        public void Tick(float deltaTime, bool inputEnabled)
        {
            if (_playfield == null)
            {
                return;
            }

            transform.position = new Vector3(transform.position.x, _playfield.CatcherY, 0f);
            if (!inputEnabled)
            {
                CancelPointer();
            }
            else
            {
                SamplePointer();
            }

            float x = Mathf.SmoothDamp(transform.position.x, _playfield.ClampX(_targetX), ref _smoothVelocity, 1f / Mathf.Max(1f, _moveLerp), Mathf.Infinity, deltaTime);
            transform.position = new Vector3(x, _playfield.CatcherY, 0f);

            if (_visual != null)
            {
                float tilt = Mathf.Clamp((_targetX - x) * 10f, -12f, 12f);
                float desiredFacing = _targetX >= x - 0.08f ? 1f : -1f;
                _facing = Mathf.MoveTowards(_facing, desiredFacing, deltaTime * 10f);
                if (Mathf.Abs(_facing) < 0.2f)
                {
                    _facing = desiredFacing;
                }

                _visual.localScale = new Vector3(_visualBaseScale.x * _facing, _visualBaseScale.y, _visualBaseScale.z);
                float bob = inputEnabled ? Mathf.Sin(Time.time * 3.4f) * 0.04f : 0f;
                _visual.localPosition = new Vector3(_visual.localPosition.x, _visualBaseScale.y * 0.5f + bob, _visual.localPosition.z);
                _visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
        }

        /// <summary>
        /// Assigns the fox billboard transform and caches its authored scale for facing flips.
        /// </summary>
        /// <param name="visual">Child quad with the fox sprite material. May be null (root still moves).</param>
        public void SetVisual(Transform visual)
        {
            _visual = visual;
            if (visual != null)
            {
                _visualBaseScale = visual.localScale;
            }
        }

        /// <summary>
        /// Samples touch 0 first, then left mouse. Begins a stroke only if the press is not over uGUI.
        /// </summary>
        private void SamplePointer()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    CancelPointer();
                    return;
                }

                if (touch.phase == TouchPhase.Began)
                {
                    _ignoreUiForStroke = !IsPointerOverUi(touch.fingerId);
                    _holding = _ignoreUiForStroke;
                }

                if (_holding)
                {
                    MoveToScreen(touch.position);
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _ignoreUiForStroke = !IsPointerOverUi(-1);
                _holding = _ignoreUiForStroke;
            }

            if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
            {
                if (!Input.GetMouseButton(0))
                {
                    CancelPointer();
                }
            }

            if (_holding && Input.GetMouseButton(0))
            {
                MoveToScreen(Input.mousePosition);
            }
        }

        /// <summary>
        /// Projects a screen point onto the Z=0 play plane and stores the world X as the movement target.
        /// </summary>
        /// <param name="screen">Pixel position from mouse or touch (devicePixelRatio is 1 in the WebGL template).</param>
        private void MoveToScreen(Vector3 screen)
        {
            if (_camera == null)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float enter))
            {
                return;
            }

            _targetX = ray.GetPoint(enter).x;
        }

        /// <summary>
        /// True when the EventSystem reports the pointer over a uGUI graphic (HUD buttons).
        /// </summary>
        /// <param name="fingerId">Touch finger id, or -1 for mouse so Unity uses the default pointer.</param>
        private static bool IsPointerOverUi(int fingerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (fingerId >= 0 && eventSystem.IsPointerOverGameObject(fingerId))
            {
                return true;
            }

            return eventSystem.IsPointerOverGameObject();
        }
    }
}
