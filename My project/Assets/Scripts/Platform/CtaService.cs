using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Local-only call-to-action handler for the playable-ad review flow.
    /// </summary>
    /// <remarks>
    /// Playable ads must not navigate the host page. <see cref="NotifyClicked"/> logs "CTA clicked"
    /// to the Unity log and the browser console, raises <see cref="GameEvents.CtaClicked"/>, and asks
    /// the HUD to show a toast. Wire this from HUD buttons only — never from hover or auto-timers —
    /// so a click is always an explicit player action.
    /// </remarks>
    public sealed class CtaService : MonoBehaviour
    {
        private HudController _hud;

        /// <summary>
        /// Binds the HUD used to show the local confirmation overlay.
        /// </summary>
        /// <param name="hud">Runtime HUD that owns the toast. May be null in tests; logging still runs.</param>
        public void Initialize(HudController hud)
        {
            _hud = hud;
        }

        /// <summary>
        /// Handles an explicit click/tap on a CTA control (mid-game bar or end card).
        /// </summary>
        /// <remarks>
        /// Side effects: Debug.Log + WebGL console, GameEvents.CtaClicked, HUD toast. No Application.OpenURL.
        /// </remarks>
        public void NotifyClicked()
        {
            const string message = "CTA clicked";
            BrowserConsole.Log(message);
            GameEvents.RaiseCtaClicked();
            if (_hud != null)
            {
                _hud.ShowCtaConfirmation(message);
            }
        }
    }
}
