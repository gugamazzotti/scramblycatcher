using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// World-space play lane derived from the camera frustum on the Z=0 plane.
    /// </summary>
    /// <remarks>
    /// Portrait and landscape use different FOV and camera height so the fox stays readable at
    /// 320×568, 390×844, 568×320, and 844×390. Bounds are recomputed when Screen width/height change
    /// (orientation or CSS resize). The illustrated backdrop is scaled with a 18% bleed so edges
    /// never show the ink clear color during a rotation.
    /// SpawnY is slightly above the top of the frustum; KillY is slightly below the bottom.
    /// </remarks>
    public sealed class Playfield : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _edgePadding = 0.55f;
        [SerializeField] private float _catcherHeightFromBottom = 1.55f;
        [SerializeField] private float _spawnPadding = 0.35f;

        private int _cachedWidth;
        private int _cachedHeight;

        private Transform _backdrop;

        /// <summary>Left edge of the catchable lane after padding, world X.</summary>
        public float MinX { get; private set; }

        /// <summary>Right edge of the catchable lane after padding, world X.</summary>
        public float MaxX { get; private set; }

        /// <summary>World Y where new items appear, just above the visible frustum.</summary>
        public float SpawnY { get; private set; }

        /// <summary>World Y below which a falling item is treated as a miss.</summary>
        public float KillY { get; private set; }

        /// <summary>World Y of the fox root, lifted from the bottom of the frustum.</summary>
        public float CatcherY { get; private set; }

        /// <summary>True when Screen height is greater than or equal to width. Drives FOV and catcher height.</summary>
        public bool IsPortrait { get; private set; }

        /// <summary>
        /// Binds the gameplay camera and computes the first set of bounds.
        /// </summary>
        /// <param name="gameplayCamera">Perspective camera used for ViewportToWorldPoint.</param>
        public void Initialize(Camera gameplayCamera)
        {
            _camera = gameplayCamera;
            Refresh(true);
        }

        /// <summary>
        /// Recomputes frustum bounds when the canvas CSS size or orientation changes.
        /// </summary>
        /// <param name="force">When true, ignores the cached resolution check (call after assigning a backdrop or on Restart).</param>
        public void Refresh(bool force)
        {
            if (_camera == null)
            {
                return;
            }

            if (!force && _cachedWidth == Screen.width && _cachedHeight == Screen.height)
            {
                return;
            }

            _cachedWidth = Screen.width;
            _cachedHeight = Screen.height;
            IsPortrait = _cachedHeight >= _cachedWidth;
            _camera.fieldOfView = IsPortrait ? 48f : 36f;
            _camera.transform.position = IsPortrait ? new Vector3(0f, 3.35f, -11.4f) : new Vector3(0f, 2.55f, -12.2f);
            _camera.transform.rotation = Quaternion.identity;

            float zDistance = Mathf.Abs(_camera.transform.position.z);
            Vector3 min = _camera.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance));
            Vector3 max = _camera.ViewportToWorldPoint(new Vector3(1f, 1f, zDistance));
            MinX = min.x + _edgePadding;
            MaxX = max.x - _edgePadding;
            if (MinX > MaxX)
            {
                float mid = (min.x + max.x) * 0.5f;
                MinX = mid - 0.4f;
                MaxX = mid + 0.4f;
            }

            SpawnY = max.y + _spawnPadding;
            KillY = min.y - 0.35f;
            CatcherY = min.y + (IsPortrait ? _catcherHeightFromBottom : 1.15f);
            FitBackdrop(min, max);
        }

        /// <summary>
        /// Registers the background quad that should cover the camera frustum, then forces a Refresh.
        /// </summary>
        /// <param name="backdrop">World-space quad using the illustrated background material.</param>
        public void SetBackdrop(Transform backdrop)
        {
            _backdrop = backdrop;
            Refresh(true);
        }

        /// <summary>
        /// Clamps a world X position to the visible playable lane so the fox cannot leave the screen.
        /// </summary>
        /// <param name="x">Unclamped world X from pointer mapping.</param>
        /// <returns>X between <see cref="MinX"/> and <see cref="MaxX"/>.</returns>
        public float ClampX(float x)
        {
            return Mathf.Clamp(x, MinX, MaxX);
        }

        /// <summary>
        /// Random X inside the current lane. Used by the spawner so items appear across the width.
        /// </summary>
        public float RandomX()
        {
            return Random.Range(MinX, MaxX);
        }

        /// <summary>
        /// Scales and centers the backdrop with bleed so rotation does not flash the clear color.
        /// </summary>
        private void FitBackdrop(Vector3 min, Vector3 max)
        {
            if (_backdrop == null)
            {
                return;
            }

            float width = (max.x - min.x) * 1.18f;
            float height = (max.y - min.y) * 1.18f;
            _backdrop.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 3.1f);
            _backdrop.localScale = new Vector3(Mathf.Max(2f, width), Mathf.Max(2f, height), 1f);
            _backdrop.rotation = Quaternion.identity;
        }
    }
}
