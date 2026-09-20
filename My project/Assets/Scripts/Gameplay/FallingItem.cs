using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// One pooled falling collectible. Motion is ticked by <see cref="GameDirector"/> (no own Update).
    /// </summary>
    /// <remarks>
    /// States: Inactive (asleep in the pool), Falling (awards score on catch), Caught (short scale pop),
    /// Missed (shrink after passing KillY). The director awards score on PlayCaught immediately; the
    /// outro is cosmetic so the HUD never waits on animation. Catch uses XY distance plus
    /// <see cref="Radius"/>, not colliders, so the physics module can stay stripped from WebGL.
    /// </remarks>
    public sealed class FallingItem : MonoBehaviour
    {
        /// <summary>
        /// Internal lifecycle of one pooled instance. Only Falling can be caught.
        /// </summary>
        private enum ItemState
        {
            /// <summary>Disabled in the pool. Tick is a no-op.</summary>
            Inactive,
            /// <summary>Moving down; eligible for catch or miss.</summary>
            Falling,
            /// <summary>Caught this frame; playing a short scale pop before despawn.</summary>
            Caught,
            /// <summary>Passed KillY; shrinking before despawn. No score.</summary>
            Missed
        }

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Transform _visual;

        private ItemState _state = ItemState.Inactive;
        private float _speed;
        private float _life;
        private Vector3 _baseScale;

        /// <summary>Kind chosen at Launch. Drives material, spin, radius, and score.</summary>
        public ItemKind Kind { get; private set; }

        /// <summary>World-unit catch radius for this kind. Added to GameConfig.CatchRadius by the director.</summary>
        public float Radius { get; private set; }

        /// <summary>True only while the item can still be caught. False during catch/miss outro and while pooled.</summary>
        public bool IsFalling => _state == ItemState.Falling;

        /// <summary>True when the catch or miss outro finished and the pool may Sleep this instance.</summary>
        public bool IsReadyToDespawn => _state == ItemState.Caught && _life <= 0f || _state == ItemState.Missed && _life <= 0f;

        /// <summary>
        /// Binds mesh components created by the pool/bootstrap. Call once when the instance is built.
        /// </summary>
        /// <param name="meshFilter">Filter on the Visual child; mesh is swapped per kind on Launch.</param>
        /// <param name="meshRenderer">Renderer whose sharedMaterial is swapped per kind.</param>
        /// <param name="visual">Transform that spins and scales; keep separate from the root so catch pop does not move world position.</param>
        public void Bind(MeshFilter meshFilter, MeshRenderer meshRenderer, Transform visual)
        {
            _meshFilter = meshFilter;
            _meshRenderer = meshRenderer;
            _visual = visual;
        }

        /// <summary>
        /// Activates the item at a spawn point with the given kind, fall speed, and VisualKit look.
        /// </summary>
        /// <param name="kind">Coin, gem, or treat.</param>
        /// <param name="position">World position, typically (RandomX, SpawnY, 0).</param>
        /// <param name="speed">Downward world units per second for this round progress.</param>
        /// <param name="kit">Source of mesh and material for <paramref name="kind"/>.</param>
        public void Launch(ItemKind kind, Vector3 position, float speed, VisualKit kit)
        {
            Kind = kind;
            _speed = speed;
            _state = ItemState.Falling;
            _life = 0.18f;
            Radius = VisualKit.RadiusFor(kind);
            _baseScale = VisualKit.ScaleFor(kind);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);
            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = kit.MeshFor(kind);
            }

            if (_meshRenderer != null)
            {
                _meshRenderer.sharedMaterial = kit.MaterialFor(kind);
                _meshRenderer.enabled = true;
            }

            if (_visual != null)
            {
                _visual.localScale = _baseScale;
            }
        }

        /// <summary>
        /// Advances fall, catch pop, or miss fade. No-op while Inactive.
        /// </summary>
        /// <param name="deltaTime">Clamped scaled delta. Zero during pause so items freeze in place.</param>
        public void Tick(float deltaTime)
        {
            if (_state == ItemState.Inactive)
            {
                return;
            }

            if (_state == ItemState.Falling)
            {
                transform.position += Vector3.down * (_speed * deltaTime);
                if (_visual != null)
                {
                    Vector3 spin = Kind == ItemKind.Coin
                        ? new Vector3(0f, 0f, 120f)
                        : Kind == ItemKind.Gem
                            ? new Vector3(0f, 0f, 80f)
                            : new Vector3(0f, 0f, 55f);
                    _visual.Rotate(spin * deltaTime, Space.Self);
                }

                return;
            }

            _life -= deltaTime;
            if (_visual == null)
            {
                return;
            }

            if (_state == ItemState.Caught)
            {
                float t = 1f - Mathf.Clamp01(_life / 0.18f);
                _visual.localScale = _baseScale * (1f + t * 0.55f);
            }
            else
            {
                float t = Mathf.Clamp01(_life / 0.2f);
                _visual.localScale = _baseScale * t;
            }
        }

        /// <summary>
        /// Starts the catch pop so the director can award score immediately. Ignored if not Falling.
        /// </summary>
        public void PlayCaught()
        {
            if (_state != ItemState.Falling)
            {
                return;
            }

            _state = ItemState.Caught;
            _life = 0.18f;
        }

        /// <summary>
        /// Starts a short shrink when the item passed KillY. Ignored if not Falling (already caught).
        /// </summary>
        public void PlayMiss()
        {
            if (_state != ItemState.Falling)
            {
                return;
            }

            _state = ItemState.Missed;
            _life = 0.2f;
        }

        /// <summary>
        /// Returns the instance to an inactive pooled state (disabled GameObject, no tick work).
        /// </summary>
        public void Sleep()
        {
            _state = ItemState.Inactive;
            gameObject.SetActive(false);
        }
    }
}
