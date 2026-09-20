using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Scrambly.Catch
{
    /// <summary>
    /// Tiny uGUI factory used by <see cref="HudController"/> so overlay construction stays readable.
    /// </summary>
    /// <remarks>
    /// Legacy <c>UnityEngine.UI.Text</c> only — no TextMeshPro — to keep the WebGL zip under 5 MB.
    /// Call <see cref="SetFont"/> from HUD Initialize so IL2CPP cannot strip the built-in font.
    /// Helpers parent to the given transform with <c>worldPositionStays: false</c> for correct canvas scale.
    /// </remarks>
    public static class UiWidgets
    {
        private static Font _font;

        /// <summary>
        /// Font currently used by CreateText. Falls back to LegacyRuntime.ttf (Unity's built-in Arial replacement).
        /// </summary>
        public static Font Font => _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>
        /// Pins the UI font so managed stripping cannot drop it from the WebGL player.
        /// </summary>
        /// <param name="font">Usually VisualKit.UiFont. Ignored when null.</param>
        public static void SetFont(Font font)
        {
            if (font != null)
            {
                _font = font;
            }
        }

        /// <summary>
        /// Creates an empty RectTransform child. Anchors stay at Unity defaults until Stretch or layout code runs.
        /// </summary>
        /// <param name="parent">Canvas or layout parent.</param>
        /// <param name="name">GameObject name, useful in the Hierarchy during review.</param>
        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>
        /// Anchors a rect to all four edges of its parent with optional padding in pixels.
        /// </summary>
        /// <param name="rect">Target rect.</param>
        /// <param name="left">Inset from parent left.</param>
        /// <param name="top">Inset from parent top (applied as negative offsetMax.y).</param>
        /// <param name="right">Inset from parent right.</param>
        /// <param name="bottom">Inset from parent bottom.</param>
        public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// Creates a colored Image. Uses Sliced type when the sprite has a 9-slice border (rounded buttons).
        /// </summary>
        /// <param name="parent">Layout parent.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="sprite">WhiteSprite, RoundedSprite, or null.</param>
        /// <param name="color">Multiply color (palette orange/purple/plum/ink).</param>
        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        /// <summary>
        /// Creates a non-raycast legacy Text element with wrap on X and overflow on Y.
        /// </summary>
        /// <param name="parent">Layout parent.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="content">Initial string.</param>
        /// <param name="size">Font size in points.</param>
        /// <param name="color">Usually Palette.WarmWhite.</param>
        /// <param name="anchor">Text alignment inside the rect.</param>
        public static Text CreateText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
        {
            var rect = CreateRect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// Creates a tappable Button with a bold WarmWhite label. onClick is added immediately.
        /// </summary>
        /// <param name="parent">Layout parent.</param>
        /// <param name="name">GameObject name (MidCta, Restart, EndCta, …).</param>
        /// <param name="label">Visible button copy.</param>
        /// <param name="sprite">Usually VisualKit.RoundedSprite.</param>
        /// <param name="color">Button face color from Palette.</param>
        /// <param name="onClick">Listener; HUD passes HandleCta or HandleRestart.</param>
        public static Button CreateButton(Transform parent, string name, string label, Sprite sprite, Color color, UnityAction onClick)
        {
            Image image = CreateImage(parent, name, sprite, color);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;
            button.targetGraphic = image;
            Text text = CreateText(image.transform, "Label", label, 22, Palette.WarmWhite, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f, 4f, 8f, 4f);
            text.fontStyle = FontStyle.Bold;
            button.onClick.AddListener(onClick);
            return button;
        }
    }
}
