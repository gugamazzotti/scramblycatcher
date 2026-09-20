using System;

namespace Scrambly.Catch
{
    /// <summary>
    /// Static event bus that decouples HUD, pause, CTA, and gameplay without FindObjectOfType.
    /// </summary>
    /// <remarks>
    /// Raise methods are the only supported way to fire events so null-conditional invoke stays in one place.
    /// Subscribers must unsubscribe in <c>OnDisable</c> or they will leak across domain reloads in the editor
    /// and keep stale HUD/director callbacks after a restart.
    /// Score and pause payloads are value types; no allocations on the raise path.
    /// </remarks>
    public static class GameEvents
    {
        /// <summary>
        /// Fired after <see cref="GameDirector"/> resets score, pool, catcher, and HUD for a new round.
        /// </summary>
        public static event Action RoundStarted;

        /// <summary>
        /// Fired when the round timer expires and the end card is shown. Falling items are already despawned.
        /// </summary>
        public static event Action RoundEnded;

        /// <summary>
        /// Fired by HUD Restart buttons. <see cref="GameDirector"/> listens and calls its round reset.
        /// </summary>
        public static event Action RestartRequested;

        /// <summary>
        /// Fired after an explicit CTA tap. <see cref="CtaService"/> raises this; it does not open a URL.
        /// </summary>
        public static event Action CtaClicked;

        /// <summary>
        /// Fired whenever the round score changes.
        /// </summary>
        /// <remarks>
        /// First argument is the new total. Second is the points just awarded (0 on round start).
        /// </remarks>
        public static event Action<int, int> ScoreChanged;

        /// <summary>
        /// Fired when <see cref="FocusPauseGuard"/> freezes or unfreezes the playable after a focus change.
        /// </summary>
        /// <remarks>
        /// Argument is true while the page/player is unfocused. The catcher cancels its pointer on pause.
        /// </remarks>
        public static event Action<bool> PauseChanged;

        /// <summary>
        /// Notifies listeners that a round has started and gameplay systems are back in the Playing phase.
        /// </summary>
        public static void RaiseRoundStarted()
        {
            RoundStarted?.Invoke();
        }

        /// <summary>
        /// Notifies listeners that the timer ended and the end card is visible.
        /// </summary>
        public static void RaiseRoundEnded()
        {
            RoundEnded?.Invoke();
        }

        /// <summary>
        /// Requests a clean restart. Safe to call from UI; the director is the only consumer that resets state.
        /// </summary>
        public static void RaiseRestartRequested()
        {
            RestartRequested?.Invoke();
        }

        /// <summary>
        /// Records that the player explicitly clicked a CTA control.
        /// </summary>
        public static void RaiseCtaClicked()
        {
            CtaClicked?.Invoke();
        }

        /// <summary>
        /// Broadcasts a score update after a catch (or a reset to zero).
        /// </summary>
        /// <param name="score">Total score after the change.</param>
        /// <param name="delta">Points just awarded. Zero when a round starts.</param>
        public static void RaiseScoreChanged(int score, int delta)
        {
            ScoreChanged?.Invoke(score, delta);
        }

        /// <summary>
        /// Broadcasts a focus-driven pause or resume.
        /// </summary>
        /// <param name="paused">True when the playable lost page/player focus and scaled time is 0.</param>
        public static void RaisePauseChanged(bool paused)
        {
            PauseChanged?.Invoke(paused);
        }
    }
}
