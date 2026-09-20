using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Frame delta used by spawners, items, and the round timer so a blur/resume cannot skip time.
    /// </summary>
    /// <remarks>
    /// Unity can deliver a huge <c>Time.deltaTime</c> on the first frame after <c>timeScale</c> returns to 1,
    /// which would teleport falling items and burn the 18s round. Call <see cref="NotifyResume"/> when
    /// unpausing; the next two reads of <see cref="Delta"/> return 0, then later frames are clamped to 50 ms.
    /// Always prefer this over raw <c>Time.deltaTime</c> in gameplay ticks.
    /// </remarks>
    public static class PlayableTime
    {
        private const float MaxDelta = 0.05f;
        private static int _graceFrames;

        /// <summary>
        /// Arms a two-frame grace window after focus returns or a round restarts.
        /// </summary>
        /// <remarks>
        /// <see cref="FocusPauseGuard"/> and <see cref="GameDirector"/> both call this so resume and Restart
        /// share the same anti-jump behavior.
        /// </remarks>
        public static void NotifyResume()
        {
            _graceFrames = 2;
        }

        /// <summary>
        /// Scaled delta clamped to a 20 FPS equivalent. Zero while paused or during the resume grace window.
        /// </summary>
        public static float Delta
        {
            get
            {
                if (_graceFrames > 0)
                {
                    _graceFrames--;
                    return 0f;
                }

                return Mathf.Min(Time.deltaTime, MaxDelta);
            }
        }
    }
}
