namespace Scrambly.Catch
{
    /// <summary>
    /// High-level states of one playable session. Owned by <see cref="GameDirector"/>.
    /// </summary>
    /// <remarks>
    /// There is no menu or loading phase: bootstrap jumps straight into Playing after wiring services.
    /// Ended keeps the HUD end card up until the player hits Restart (which returns to Playing).
    /// </remarks>
    public enum GamePhase
    {
        /// <summary>
        /// Director exists but <see cref="GameDirector.Initialize"/> has not run yet. Update is a no-op.
        /// </summary>
        Boot = 0,

        /// <summary>
        /// Timer is running, items spawn, catcher input is enabled (unless focus-paused).
        /// </summary>
        Playing = 1,

        /// <summary>
        /// Timer finished. Pool is emptied, input is cancelled, end card + CTA + Restart are visible.
        /// </summary>
        Ended = 2
    }
}
