using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Owns one Fox Catch round: timer, score, catch loop, end state, and Restart.
    /// </summary>
    /// <remarks>
    /// Execution order 50 so playfield, catcher, and HUD exist before the first tick.
    /// There is no physics: each frame the director moves items, then checks 2D distance from
    /// <see cref="CatcherController.CatchPoint"/> against <see cref="GameConfig.CatchRadius"/> plus item radius.
    /// Scaled time still drives Unity animations, but gameplay uses <see cref="PlayableTime.Delta"/> so a
    /// tab-blur resume cannot skip the round. Mid-CTA is shown once when elapsed time hits
    /// <see cref="GameConfig.MidCtaTime"/>.
    /// </remarks>
    [DefaultExecutionOrder(50)]
    public sealed class GameDirector : MonoBehaviour
    {
        private GameConfig _config;
        private CatcherController _catcher;
        private ItemSpawner _spawner;
        private ItemPool _pool;
        private Playfield _playfield;
        private HudController _hud;
        private GamePhase _phase = GamePhase.Boot;
        private float _elapsed;
        private int _score;
        private int _runId;
        private bool _midCtaShown;
        private bool _subscribed;

        /// <summary>
        /// Current loop phase. Boot until Initialize, Playing during the timer, Ended on the end card.
        /// </summary>
        public GamePhase Phase => _phase;

        /// <summary>
        /// Injects gameplay services and starts the first round.
        /// </summary>
        /// <param name="config">Round length, spawn curve, catch radius.</param>
        /// <param name="catcher">Fox controller ticked every frame.</param>
        /// <param name="spawner">Pooled item emitter.</param>
        /// <param name="pool">Active/inactive falling items.</param>
        /// <param name="playfield">Frustum bounds and kill plane.</param>
        /// <param name="hud">Score, timer, CTA, end card.</param>
        /// <remarks>
        /// Call once from <see cref="PlayableBootstrap"/>. Re-calling is not a restart; use the Restart event for that.
        /// </remarks>
        public void Initialize(
            GameConfig config,
            CatcherController catcher,
            ItemSpawner spawner,
            ItemPool pool,
            Playfield playfield,
            HudController hud)
        {
            _config = config;
            _catcher = catcher;
            _spawner = spawner;
            _pool = pool;
            _playfield = playfield;
            _hud = hud;
            EnsureSubscribed();
            BeginRound();
        }

        #region Unity Callbacks

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            GameEvents.RestartRequested -= HandleRestart;
            GameEvents.PauseChanged -= HandlePauseChanged;
            _subscribed = false;
        }

        /// <summary>
        /// Main playable loop: refresh lane, tick catcher, spawn/move items, mid-CTA, HUD, then end when the timer elapses.
        /// </summary>
        /// <remarks>
        /// Skips work while still in Boot. After Ended, still refreshes playfield and HUD so orientation changes stay valid.
        /// Catcher input is disabled when timeScale is 0 (focus pause).
        /// </remarks>
        private void Update()
        {
            if (_config == null || _phase == GamePhase.Boot)
            {
                return;
            }

            _playfield.Refresh(false);
            float dt = PlayableTime.Delta;
            bool playing = _phase == GamePhase.Playing;
            _catcher.Tick(dt, playing && Time.timeScale > 0f);

            if (!playing)
            {
                _hud.Tick(_elapsed, _config.RoundDuration, _score, _phase);
                return;
            }

            _elapsed += dt;
            float progress = Mathf.Clamp01(_elapsed / _config.RoundDuration);
            _spawner.SetProgress(progress);
            _spawner.Tick(dt);
            TickItems(dt);

            if (!_midCtaShown && _elapsed >= _config.MidCtaTime)
            {
                _midCtaShown = true;
                _hud.ShowMidCta();
            }

            _hud.Tick(_elapsed, _config.RoundDuration, _score, _phase);
            if (_elapsed >= _config.RoundDuration)
            {
                EndRound();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Subscribes to Restart and pause events once. Safe to call from Initialize and OnEnable.
        /// </summary>
        private void EnsureSubscribed()
        {
            if (_subscribed)
            {
                return;
            }

            GameEvents.RestartRequested += HandleRestart;
            GameEvents.PauseChanged += HandlePauseChanged;
            _subscribed = true;
        }

        /// <summary>
        /// HUD Restart button path. Starts a fresh round without rebuilding the scene.
        /// </summary>
        private void HandleRestart()
        {
            BeginRound();
        }

        /// <summary>
        /// Drops an in-progress drag when the page loses focus so the fox does not keep sliding.
        /// </summary>
        private void HandlePauseChanged(bool paused)
        {
            if (paused)
            {
                _catcher.CancelPointer();
            }
        }

        /// <summary>
        /// Resets timer, score, pool, spawner, catcher, and HUD, then raises RoundStarted.
        /// </summary>
        /// <remarks>
        /// Increments <c>_runId</c> so in-flight coroutines from a previous round (HUD toast) can be stopped.
        /// Always restores timeScale and AudioListener.pause in case Restart is pressed while unfocused.
        /// </remarks>
        private void BeginRound()
        {
            _runId++;
            StopAllCoroutines();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            PlayableTime.NotifyResume();
            _phase = GamePhase.Playing;
            _elapsed = 0f;
            _score = 0;
            _midCtaShown = false;
            _pool.DespawnAll();
            _spawner.ResetState();
            _catcher.ResetState();
            _hud.ResetState();
            _playfield.Refresh(true);
            GameEvents.RaiseScoreChanged(_score, 0);
            GameEvents.RaiseRoundStarted();
        }

        /// <summary>
        /// Stops spawning, clears items, shows the end card, and raises RoundEnded. Idempotent if already Ended.
        /// </summary>
        private void EndRound()
        {
            if (_phase != GamePhase.Playing)
            {
                return;
            }

            _phase = GamePhase.Ended;
            _pool.DespawnAll();
            _catcher.CancelPointer();
            _hud.ShowEnd(_score);
            GameEvents.RaiseRoundEnded();
        }

        /// <summary>
        /// Moves every active item, then awards a catch or plays a miss if it passed the kill plane.
        /// </summary>
        /// <param name="dt">Clamped scaled delta from <see cref="PlayableTime"/>.</param>
        /// <remarks>
        /// Iterates the active list backwards so despawns do not skip the next item.
        /// Catch uses XY distance only; Z is ignored because everything lives on the play plane.
        /// </remarks>
        private void TickItems(float dt)
        {
            var active = _pool.Active;
            float catchRadius = _config.CatchRadius;
            Vector3 catchPoint = _catcher.CatchPoint;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                FallingItem item = active[i];
                item.Tick(dt);
                if (item.IsFalling)
                {
                    float distance = Vector2.Distance(
                        new Vector2(item.transform.position.x, item.transform.position.y),
                        new Vector2(catchPoint.x, catchPoint.y));
                    if (distance <= catchRadius + item.Radius)
                    {
                        item.PlayCaught();
                        AddScore(VisualKit.ScoreFor(item.Kind));
                    }
                    else if (item.transform.position.y < _playfield.KillY)
                    {
                        item.PlayMiss();
                    }
                }

                if (item.IsReadyToDespawn)
                {
                    _pool.Despawn(item);
                }
            }
        }

        /// <summary>
        /// Adds points from a catch and notifies HUD listeners.
        /// </summary>
        /// <param name="delta">Points for the caught kind (see <see cref="VisualKit.ScoreFor"/>).</param>
        private void AddScore(int delta)
        {
            _score += delta;
            GameEvents.RaiseScoreChanged(_score, delta);
        }

        #endregion
    }
}
