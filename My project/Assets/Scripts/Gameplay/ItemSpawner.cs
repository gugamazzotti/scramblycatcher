using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Emits pooled collectibles just above the camera frustum on a ramping cadence.
    /// </summary>
    /// <remarks>
    /// Only <see cref="GameDirector"/> should call <see cref="Tick"/>, and only while the phase is Playing.
    /// Interval and fall speed lerp from GameConfig start→end using round progress so the rain densifies
    /// without a second difficulty system. If the pool is exhausted the beat is skipped (no runtime Instantiate).
    /// <see cref="ResetState"/> seeds the accumulator with a short delay so Restart cannot dump a burst on frame 0.
    /// </remarks>
    public sealed class ItemSpawner : MonoBehaviour
    {
        private GameConfig _config;
        private Playfield _playfield;
        private ItemPool _pool;
        private VisualKit _kit;
        private float _accum;
        private float _progress;

        /// <summary>
        /// Binds config and scene services used while the round is running.
        /// </summary>
        /// <param name="config">Spawn interval, fall speed, and item weights.</param>
        /// <param name="playfield">Random X and SpawnY.</param>
        /// <param name="pool">Source of FallingItem instances.</param>
        /// <param name="kit">Materials/meshes applied in Launch.</param>
        public void Initialize(GameConfig config, Playfield playfield, ItemPool pool, VisualKit kit)
        {
            _config = config;
            _playfield = playfield;
            _pool = pool;
            _kit = kit;
            ResetState();
        }

        /// <summary>
        /// Clears spawn progress and puts a 0.35s delay on the accumulator so Restart cannot burst items.
        /// </summary>
        public void ResetState()
        {
            _accum = 0.35f;
            _progress = 0f;
        }

        /// <summary>
        /// Updates 0–1 round progress used to lerp spawn interval and fall speed.
        /// </summary>
        /// <param name="progress">Elapsed / round duration, already clamped by the director.</param>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Advances spawn timing. Call only while the round is playing.
        /// </summary>
        /// <param name="deltaTime">Clamped scaled delta. Zero during pause/resume grace, so no catch-up burst.</param>
        public void Tick(float deltaTime)
        {
            if (_config == null || _pool == null)
            {
                return;
            }

            _accum += deltaTime;
            float interval = Mathf.Lerp(_config.SpawnIntervalStart, _config.SpawnIntervalEnd, _progress);
            if (_accum < interval)
            {
                return;
            }

            _accum = 0f;
            SpawnOne();
        }

        /// <summary>
        /// Pulls one pooled item, rolls a kind, and launches it at a random lane X.
        /// </summary>
        private void SpawnOne()
        {
            FallingItem item = _pool.Spawn();
            if (item == null)
            {
                return;
            }

            ItemKind kind = _config.RollKind();
            float speed = Mathf.Lerp(_config.FallSpeedStart, _config.FallSpeedEnd, _progress);
            var position = new Vector3(_playfield.RandomX(), _playfield.SpawnY, 0f);
            item.Launch(kind, position, speed, _kit);
        }
    }
}
