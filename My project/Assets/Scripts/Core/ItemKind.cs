namespace Scrambly.Catch
{
    /// <summary>
    /// Collectible variants the fox can catch. Chosen by <see cref="GameConfig.RollKind"/>.
    /// </summary>
    /// <remarks>
    /// Visuals, catch radius, and score live on <see cref="VisualKit"/> (<c>MaterialFor</c>, <c>ScaleFor</c>,
    /// <c>RadiusFor</c>, <c>ScoreFor</c>) so adding a fourth kind is a VisualKit + weight change, not a new prefab.
    /// </remarks>
    public enum ItemKind
    {
        /// <summary>
        /// Star coin sprite. Default spawn weight, 2 points, slightly larger catch radius.
        /// </summary>
        Coin = 0,

        /// <summary>
        /// Puzzle-gem sprite. Highest score (3 points), slightly smaller visual.
        /// </summary>
        Gem = 1,

        /// <summary>
        /// Treat sprite. Lowest score (1 point). Fills out the rain so coins/gems stay readable.
        /// </summary>
        Treat = 2
    }
}
