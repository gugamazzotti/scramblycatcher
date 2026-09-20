using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Shared Scrambly-inspired colors used by camera clear, unlit materials, and uGUI.
    /// </summary>
    /// <remarks>
    /// Values match the reference kit: orange #F58324, purple #7845D8, deep ink #201338, warm white #FFF6E8.
    /// This is a working playable palette, not an official brand standard. Keep HUD and world on these
    /// constants so a later recolor is a one-file change.
    /// </remarks>
    public static class Palette
    {
        /// <summary>Primary CTA and timer fill. Hex #F58324.</summary>
        public static readonly Color Orange = new Color32(0xF5, 0x83, 0x24, 0xFF);

        /// <summary>Secondary buttons (Restart) and gem accent. Hex #7845D8.</summary>
        public static readonly Color Purple = new Color32(0x78, 0x45, 0xD8, 0xFF);

        /// <summary>Camera clear color, ambient light, and end-card dimmer base. Hex #201338.</summary>
        public static readonly Color DeepInk = new Color32(0x20, 0x13, 0x38, 0xFF);

        /// <summary>HUD labels and button text. Hex #FFF6E8.</summary>
        public static readonly Color WarmWhite = new Color32(0xFF, 0xF6, 0xE8, 0xFF);

        /// <summary>End card and timer track fill. Darker plum so orange/purple buttons stay readable.</summary>
        public static readonly Color Plum = new Color32(0x3A, 0x1B, 0x63, 0xFF);

        /// <summary>Optional lighter purple for decorative meshes that need more contrast than <see cref="Purple"/>.</summary>
        public static readonly Color Cushion = new Color32(0x8B, 0x5C, 0xE8, 0xFF);

        /// <summary>Fallback coin material if the painted sprite is missing. Hex #F6C14A.</summary>
        public static readonly Color CoinMetal = new Color32(0xF6, 0xC1, 0x4A, 0xFF);
    }
}
