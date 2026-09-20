using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Scrambly.Catch
{
    /// <summary>
    /// Runtime-built overlay HUD: score, timer, hint, mid-game CTA, end card, confirmation toast, Restart.
    /// </summary>
    /// <remarks>
    /// Uses legacy uGUI Text (not TMP) so the WebGL player does not pull TextMeshPro. The canvas is
    /// constructed once in <see cref="Initialize"/>; Restart only toggles panels via <see cref="ResetState"/>.
    /// CTA buttons never navigate — they call <see cref="CtaService.NotifyClicked"/>, which shows a local
    /// "CTA clicked" toast. CanvasScaler match bias changes with portrait vs landscape so 320×568 and
    /// 844×390 both stay readable. Restart is always on the top bar for reviewers, plus again on the end card.
    /// </remarks>
    public sealed class HudController : MonoBehaviour
    {
        private VisualKit _kit;
        private CtaService _cta;
        private Text _scoreText;
        private Text _timeText;
        private Image _timeFill;
        private RectTransform _timeTrack;
        private GameObject _hint;
        private GameObject _midCta;
        private GameObject _endPanel;
        private Text _endScore;
        private GameObject _confirm;
        private Text _confirmText;
        private CanvasScaler _scaler;
        private Coroutine _confirmRoutine;
        private bool _built;

        /// <summary>
        /// Builds the canvas once and binds CTA / Restart callbacks.
        /// </summary>
        /// <param name="kit">Sprites, rounded button graphic, and UI font (keeps Arial from stripping).</param>
        /// <param name="cta">Local-only CTA handler shown by mid and end buttons.</param>
        public void Initialize(VisualKit kit, CtaService cta)
        {
            _kit = kit;
            _cta = cta;
            UiWidgets.SetFont(kit.UiFont);
            if (!_built)
            {
                Build();
                _built = true;
            }

            ResetState();
        }

        /// <summary>
        /// Restores HUD to the start-of-round layout without rebuilding listeners.
        /// </summary>
        /// <remarks>
        /// Hides mid CTA, end card, and toast; shows the drag hint; zeros score and timer fill.
        /// </remarks>
        public void ResetState()
        {
            StopConfirmRoutine();
            if (_midCta != null)
            {
                _midCta.SetActive(false);
            }

            if (_endPanel != null)
            {
                _endPanel.SetActive(false);
            }

            if (_confirm != null)
            {
                _confirm.SetActive(false);
            }

            if (_hint != null)
            {
                _hint.SetActive(true);
            }

            SetScore(0);
            SetTimer(0f, 1f);
            RefreshScaler();
        }

        /// <summary>
        /// Per-frame HUD refresh: score, remaining seconds, hint visibility, and canvas match mode.
        /// </summary>
        /// <param name="elapsed">Seconds into the current round (does not advance while paused).</param>
        /// <param name="duration">Round length from GameConfig.</param>
        /// <param name="score">Current total, already updated by the director.</param>
        /// <param name="phase">Used to hide the hint after Playing ends.</param>
        public void Tick(float elapsed, float duration, int score, GamePhase phase)
        {
            RefreshScaler();
            SetScore(score);
            SetTimer(elapsed, duration);
            if (_hint != null)
            {
                _hint.SetActive(phase == GamePhase.Playing && elapsed < 3f);
            }
        }

        /// <summary>
        /// Reveals the compact bottom CTA used during play. Called once at MidCtaTime.
        /// </summary>
        public void ShowMidCta()
        {
            if (_midCta != null)
            {
                _midCta.SetActive(true);
            }
        }

        /// <summary>
        /// Shows the end card with the final score. Mid CTA and hint hide; Restart and end CTA stay available.
        /// </summary>
        /// <param name="score">Final round score displayed on the card.</param>
        public void ShowEnd(int score)
        {
            if (_midCta != null)
            {
                _midCta.SetActive(false);
            }

            if (_hint != null)
            {
                _hint.SetActive(false);
            }

            if (_endScore != null)
            {
                _endScore.text = "Score " + score;
            }

            if (_endPanel != null)
            {
                _endPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Local confirmation overlay. Does not navigate away from the playable page.
        /// </summary>
        /// <param name="message">Copy shown on the toast, typically "CTA clicked".</param>
        /// <remarks>
        /// Uses realtime wait so the toast still expires if the page is focus-paused with timeScale 0.
        /// </remarks>
        public void ShowCtaConfirmation(string message)
        {
            if (_confirmText != null)
            {
                _confirmText.text = message;
            }

            if (_confirm != null)
            {
                _confirm.SetActive(true);
            }

            StopConfirmRoutine();
            _confirmRoutine = StartCoroutine(HideConfirmAfter(2f));
        }

        /// <summary>
        /// Constructs overlay canvas, top bar, hint, mid CTA, end card, and toast. Called once.
        /// </summary>
        private void Build()
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 10;
            _scaler = canvasGo.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(390f, 844f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = UiWidgets.CreateRect(canvasGo.transform, "SafeArea");
            UiWidgets.Stretch(root, 12f, 10f, 12f, 12f);

            BuildTopBar(root);
            _hint = UiWidgets.CreateText(root, "Hint", "Drag to catch", 20, Palette.WarmWhite, TextAnchor.LowerCenter).gameObject;
            var hintRect = _hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.1f, 0.18f);
            hintRect.anchorMax = new Vector2(0.9f, 0.28f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;

            Button mid = UiWidgets.CreateButton(root, "MidCta", "Play on Scrambly", _kit.RoundedSprite, Palette.Orange, HandleCta);
            var midRect = mid.GetComponent<RectTransform>();
            midRect.anchorMin = new Vector2(0.08f, 0.03f);
            midRect.anchorMax = new Vector2(0.92f, 0.11f);
            midRect.offsetMin = Vector2.zero;
            midRect.offsetMax = Vector2.zero;
            _midCta = mid.gameObject;
            _midCta.SetActive(false);

            BuildEndPanel(canvasGo.transform);
            BuildConfirm(canvasGo.transform);
        }

        /// <summary>
        /// Compact score chip on the left, pill timer in the middle, always-visible Restart on the right.
        /// </summary>
        /// <remarks>
        /// The meter uses <see cref="VisualKit.BarSprite"/> instead of the button round-rect so 9-slice
        /// corners match the 18px track height. Score sits in a tight chip aligned to that same row
        /// instead of a wide empty column.
        /// </remarks>
        private void BuildTopBar(RectTransform root)
        {
            const float barHeight = 36f;
            const float rowTop = -4f;
            const float controlHeight = 26f;
            const float trackHeight = 18f;
            const float scoreWidth = 48f;
            const float restartWidth = 96f;
            const float gap = 8f;
            const float trackTop = rowTop - (controlHeight - trackHeight) * 0.5f;

            RectTransform bar = UiWidgets.CreateRect(root, "TopBar");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, barHeight);
            bar.anchoredPosition = Vector2.zero;

            Image scorePlate = UiWidgets.CreateImage(bar, "ScorePlate", _kit.BarSprite, Palette.Plum);
            scorePlate.raycastTarget = false;
            var scorePlateRect = scorePlate.rectTransform;
            scorePlateRect.anchorMin = new Vector2(0f, 1f);
            scorePlateRect.anchorMax = new Vector2(0f, 1f);
            scorePlateRect.pivot = new Vector2(0f, 1f);
            scorePlateRect.anchoredPosition = new Vector2(0f, rowTop);
            scorePlateRect.sizeDelta = new Vector2(scoreWidth, controlHeight);

            _scoreText = UiWidgets.CreateText(scorePlate.transform, "Score", "0", 22, Palette.WarmWhite, TextAnchor.MiddleCenter);
            UiWidgets.Stretch(_scoreText.rectTransform, 4f, 1f, 4f, 1f);
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _scoreText.verticalOverflow = VerticalWrapMode.Overflow;

            Button restart = UiWidgets.CreateButton(bar, "Restart", "Restart", _kit.RoundedSprite, Palette.Purple, HandleRestart);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(1f, 1f);
            restartRect.anchorMax = new Vector2(1f, 1f);
            restartRect.pivot = new Vector2(1f, 1f);
            restartRect.anchoredPosition = new Vector2(0f, rowTop);
            restartRect.sizeDelta = new Vector2(restartWidth, controlHeight);
            restart.GetComponentInChildren<Text>().fontSize = 16;

            Image track = UiWidgets.CreateImage(bar, "TimeTrack", _kit.BarSprite, Palette.Plum);
            track.raycastTarget = false;
            track.pixelsPerUnitMultiplier = 1f;
            _timeTrack = track.rectTransform;
            _timeTrack.anchorMin = new Vector2(0f, 1f);
            _timeTrack.anchorMax = new Vector2(1f, 1f);
            _timeTrack.pivot = new Vector2(0.5f, 1f);
            _timeTrack.offsetMin = new Vector2(scoreWidth + gap, trackTop - trackHeight);
            _timeTrack.offsetMax = new Vector2(-(restartWidth + gap), trackTop);

            _timeFill = UiWidgets.CreateImage(track.transform, "Fill", _kit.BarSprite, Palette.Orange);
            _timeFill.type = Image.Type.Sliced;
            _timeFill.pixelsPerUnitMultiplier = 1f;
            _timeFill.raycastTarget = false;
            _timeFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _timeFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _timeFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            _timeText = UiWidgets.CreateText(track.transform, "Time", "18s", 13, Palette.WarmWhite, TextAnchor.MiddleRight);
            UiWidgets.Stretch(_timeText.rectTransform, 10f, 0f, 10f, 0f);
            _timeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _timeText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>
        /// Dimmed end card with score, disclaimer, Play on Scrambly CTA, and a second Restart button.
        /// </summary>
        private void BuildEndPanel(Transform canvas)
        {
            Image dim = UiWidgets.CreateImage(canvas, "EndPanel", _kit.WhiteSprite, new Color(Palette.DeepInk.r, Palette.DeepInk.g, Palette.DeepInk.b, 0.78f));
            UiWidgets.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            _endPanel = dim.gameObject;

            Image card = UiWidgets.CreateImage(dim.transform, "Card", _kit.RoundedSprite, Palette.Plum);
            var cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0.08f, 0.18f);
            cardRect.anchorMax = new Vector2(0.92f, 0.82f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            Text title = UiWidgets.CreateText(card.transform, "Title", "Nice catch!", 32, Palette.WarmWhite, TextAnchor.MiddleCenter);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.06f, 0.72f);
            titleRect.anchorMax = new Vector2(0.94f, 0.92f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 18;
            title.resizeTextMaxSize = 34;

            _endScore = UiWidgets.CreateText(card.transform, "EndScore", "Score 0", 26, Palette.Orange, TextAnchor.MiddleCenter);
            var scoreRect = _endScore.rectTransform;
            scoreRect.anchorMin = new Vector2(0.08f, 0.58f);
            scoreRect.anchorMax = new Vector2(0.92f, 0.74f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;

            Text body = UiWidgets.CreateText(card.transform, "Body", "Discover games and rewards on Scrambly.", 18, Palette.WarmWhite, TextAnchor.MiddleCenter);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0.08f, 0.42f);
            bodyRect.anchorMax = new Vector2(0.92f, 0.58f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;

            Text disclaimer = UiWidgets.CreateText(card.transform, "Disclaimer", "Demo rewards are simulated.", 14, new Color(1f, 0.96f, 0.91f, 0.7f), TextAnchor.MiddleCenter);
            var disclaimerRect = disclaimer.rectTransform;
            disclaimerRect.anchorMin = new Vector2(0.08f, 0.34f);
            disclaimerRect.anchorMax = new Vector2(0.92f, 0.42f);
            disclaimerRect.offsetMin = Vector2.zero;
            disclaimerRect.offsetMax = Vector2.zero;

            Button cta = UiWidgets.CreateButton(card.transform, "EndCta", "Play on Scrambly", _kit.RoundedSprite, Palette.Orange, HandleCta);
            var ctaRect = cta.GetComponent<RectTransform>();
            ctaRect.anchorMin = new Vector2(0.1f, 0.16f);
            ctaRect.anchorMax = new Vector2(0.9f, 0.32f);
            ctaRect.offsetMin = Vector2.zero;
            ctaRect.offsetMax = Vector2.zero;

            Button restart = UiWidgets.CreateButton(card.transform, "EndRestart", "Restart", _kit.RoundedSprite, Palette.Purple, HandleRestart);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.18f, 0.04f);
            restartRect.anchorMax = new Vector2(0.82f, 0.14f);
            restartRect.offsetMin = Vector2.zero;
            restartRect.offsetMax = Vector2.zero;
            restart.GetComponentInChildren<Text>().fontSize = 18;
        }

        /// <summary>
        /// Non-raycast toast used after a CTA click. Hidden after two realtime seconds.
        /// </summary>
        private void BuildConfirm(Transform canvas)
        {
            Image toast = UiWidgets.CreateImage(canvas, "CtaConfirm", _kit.RoundedSprite, Palette.Orange);
            var rect = toast.rectTransform;
            rect.anchorMin = new Vector2(0.12f, 0.44f);
            rect.anchorMax = new Vector2(0.88f, 0.56f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _confirmText = UiWidgets.CreateText(toast.transform, "Label", "CTA clicked", 22, Palette.WarmWhite, TextAnchor.MiddleCenter);
            UiWidgets.Stretch(_confirmText.rectTransform, 8f, 4f, 8f, 4f);
            _confirmText.fontStyle = FontStyle.Bold;
            toast.raycastTarget = false;
            _confirm = toast.gameObject;
            _confirm.SetActive(false);
        }

        /// <summary>
        /// Writes the numeric score in the top-left label.
        /// </summary>
        private void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }
        }

        /// <summary>
        /// Updates the orange pill width (time remaining) and the "Ns" label under the bar.
        /// </summary>
        /// <remarks>
        /// Width is scaled on a sliced capsule instead of Image.Filled so the leading end stays round.
        /// A minimum width equal to the track height keeps 9-slice corners from squashing when almost empty.
        /// </remarks>
        private void SetTimer(float elapsed, float duration)
        {
            float remaining = Mathf.Max(0f, duration - elapsed);
            if (_timeFill != null && _timeTrack != null)
            {
                if (_timeTrack.rect.width <= 1f)
                {
                    Canvas.ForceUpdateCanvases();
                }
                float amount = duration <= 0f ? 0f : Mathf.Clamp01(1f - elapsed / duration);
                const float inset = 2f;
                float innerWidth = Mathf.Max(0f, _timeTrack.rect.width - inset * 2f);
                float height = Mathf.Max(4f, _timeTrack.rect.height - inset * 2f);
                float width = amount <= 0.001f ? 0f : Mathf.Max(height, innerWidth * amount);
                var fillRect = _timeFill.rectTransform;
                fillRect.offsetMin = new Vector2(inset, inset);
                fillRect.offsetMax = new Vector2(inset + width, -inset);
                _timeFill.enabled = width > 0f;
            }

            if (_timeText != null)
            {
                _timeText.text = Mathf.CeilToInt(remaining) + "s";
            }
        }

        /// <summary>
        /// Bias CanvasScaler toward height in portrait and width in landscape so HUD does not clip.
        /// </summary>
        private void RefreshScaler()
        {
            if (_scaler == null)
            {
                return;
            }

            _scaler.matchWidthOrHeight = Screen.height >= Screen.width ? 0.55f : 0.3f;
        }

        /// <summary>
        /// Explicit CTA tap from mid or end button. Does not open a URL.
        /// </summary>
        private void HandleCta()
        {
            if (_cta != null)
            {
                _cta.NotifyClicked();
            }
        }

        /// <summary>
        /// Raises RestartRequested so the director can reset without the HUD knowing about gameplay types.
        /// </summary>
        private static void HandleRestart()
        {
            GameEvents.RaiseRestartRequested();
        }

        /// <summary>
        /// Stops a pending toast hide so a second CTA click restarts the 2s window.
        /// </summary>
        private void StopConfirmRoutine()
        {
            if (_confirmRoutine == null)
            {
                return;
            }

            StopCoroutine(_confirmRoutine);
            _confirmRoutine = null;
        }

        /// <summary>
        /// Hides the CTA toast after a realtime delay (works even when timeScale is 0).
        /// </summary>
        private IEnumerator HideConfirmAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (_confirm != null)
            {
                _confirm.SetActive(false);
            }

            _confirmRoutine = null;
        }
    }
}
