using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Pauses scaled time and audio when the host page or WebGL player loses focus, then resumes without a time jump.
    /// </summary>
    /// <remarks>
    /// Playables sit in iframes that blur often. Unity would otherwise deliver a large deltaTime on resume
    /// and skip falling items / burn the round timer. This guard sets <c>Time.timeScale</c> to 0, pauses
    /// AudioListener, and calls <see cref="PlayableTime.NotifyResume"/> on unpause. Execution order -50
    /// so pause is applied before <see cref="GameDirector"/> ticks. The catcher also cancels its pointer
    /// via <see cref="GameEvents.PauseChanged"/>.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    public sealed class FocusPauseGuard : MonoBehaviour
    {
        private bool _pausedByFocus;

        #region Unity Callbacks

        private void OnEnable()
        {
            GameEvents.PauseChanged += HandleExternalPauseRequest;
        }

        private void OnDisable()
        {
            GameEvents.PauseChanged -= HandleExternalPauseRequest;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetPaused(!hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetPaused(pauseStatus);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// True while this guard froze the playable because the page/player is unfocused.
        /// </summary>
        public bool IsPaused => _pausedByFocus;

        #endregion

        #region Private Methods

        /// <summary>
        /// Extra PauseChanged listener. Resume always comes from OS focus, not from other gameplay code.
        /// </summary>
        private void HandleExternalPauseRequest(bool paused)
        {
            if (!paused)
            {
                return;
            }

            // Input systems cancel pointers when we raise pause; keep time frozen until OS focus returns.
        }

        /// <summary>
        /// Applies or clears the focus freeze. No-op if the requested state already matches.
        /// </summary>
        /// <param name="paused">True to freeze scaled time and audio; false to resume with a grace window.</param>
        private void SetPaused(bool paused)
        {
            if (_pausedByFocus == paused)
            {
                return;
            }

            _pausedByFocus = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (!paused)
            {
                PlayableTime.NotifyResume();
            }

            GameEvents.RaisePauseChanged(paused);
        }

        #endregion
    }
}
