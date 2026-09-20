using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// ScriptableObject that holds every designer-facing number for one Fox Catch round.
    /// </summary>
    /// <remarks>
    /// Bootstrap and <see cref="GameDirector"/> read these values at runtime; they are not mutated during play.
    /// Tweak the asset at <c>Assets/Settings/GameConfig.asset</c> (or the Inspector on that SO) instead of
    /// hardcoding spawn cadence, fall speed, or catch radius.
    /// Weights on coin / gem / treat are relative: they do not need to sum to 100.
    /// Spawn interval and fall speed lerp from start to end using round progress (0 at start, 1 at the timer).
    /// </remarks>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Scrambly/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Round")]
        [SerializeField] private float _roundDuration = 18f;
        [SerializeField] private float _midCtaTime = 7f;
        [SerializeField] private float _hintDuration = 3f;

        [Header("Spawning")]
        [SerializeField] private float _spawnIntervalStart = 0.82f;
        [SerializeField] private float _spawnIntervalEnd = 0.34f;
        [SerializeField] private int _poolSize = 18;
        [SerializeField] private int _coinWeight = 50;
        [SerializeField] private int _gemWeight = 30;
        [SerializeField] private int _treatWeight = 20;

        [Header("Motion")]
        [SerializeField] private float _fallSpeedStart = 3.05f;
        [SerializeField] private float _fallSpeedEnd = 4.15f;
        [SerializeField] private float _catcherLerp = 22f;
        [SerializeField] private float _catchRadius = 1.05f;

        /// <summary>
        /// Length of one round in seconds. When elapsed time reaches this value the director ends the round.
        /// </summary>
        public float RoundDuration => _roundDuration;

        /// <summary>
        /// Seconds from round start until the compact in-play CTA ("Play on Scrambly") is shown.
        /// </summary>
        /// <remarks>
        /// Must stay below <see cref="RoundDuration"/>. The HUD only reveals the mid CTA once per round.
        /// </remarks>
        public float MidCtaTime => _midCtaTime;

        /// <summary>
        /// Authoring value for how long the "Drag to catch" hint should stay up.
        /// </summary>
        /// <remarks>
        /// <see cref="HudController.Tick"/> currently shows the hint for the first 3 seconds of Playing,
        /// matching the default. Keep this field in sync if you change that window.
        /// </remarks>
        public float HintDuration => _hintDuration;

        /// <summary>
        /// Seconds between item spawns at round start (progress 0).
        /// </summary>
        public float SpawnIntervalStart => _spawnIntervalStart;

        /// <summary>
        /// Seconds between item spawns at the end of the round (progress 1). Smaller = denser rain.
        /// </summary>
        public float SpawnIntervalEnd => _spawnIntervalEnd;

        /// <summary>
        /// How many <see cref="FallingItem"/> instances <see cref="ItemPool"/> pre-creates. Extra spawn requests are dropped.
        /// </summary>
        public int PoolSize => _poolSize;

        /// <summary>
        /// World units per second at round start. Items fall straight down on the Z=0 plane.
        /// </summary>
        public float FallSpeedStart => _fallSpeedStart;

        /// <summary>
        /// World units per second at round end. Lerped with spawn interval using the same progress value.
        /// </summary>
        public float FallSpeedEnd => _fallSpeedEnd;

        /// <summary>
        /// Catcher horizontal smoothing. Passed to <see cref="CatcherController"/> as the SmoothDamp rate.
        /// Higher values snap faster toward the pointer.
        /// </summary>
        public float CatcherLerp => _catcherLerp;

        /// <summary>
        /// Distance in world units from the fox catch point to an item center that counts as a catch.
        /// Combined with each item's own <see cref="FallingItem.Radius"/>; there is no physics collider.
        /// </summary>
        public float CatchRadius => _catchRadius;

        /// <summary>
        /// Rolls one collectible type using the Inspector weights.
        /// </summary>
        /// <returns>
        /// Coin, gem, or treat. If every weight is zero the method still returns a valid kind by treating the total as 1.
        /// </returns>
        public ItemKind RollKind()
        {
            int total = Mathf.Max(1, _coinWeight + _gemWeight + _treatWeight);
            int roll = Random.Range(0, total);
            if (roll < _coinWeight)
            {
                return ItemKind.Coin;
            }

            if (roll < _coinWeight + _gemWeight)
            {
                return ItemKind.Gem;
            }

            return ItemKind.Treat;
        }
    }
}
