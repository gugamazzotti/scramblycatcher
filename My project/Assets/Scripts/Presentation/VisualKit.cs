using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Shared materials, procedural meshes, and uGUI sprites for the playable.
    /// </summary>
    /// <remarks>
    /// Lives on the same GameObject as <see cref="PlayableBootstrap"/>. Textures are assigned by the
    /// editor factory (fox, coin, gem, treat, background) and turned into unlit materials in
    /// <see cref="Initialize"/>. Collectibles use billboard quads so painted sprites stay readable;
    /// colored fallbacks exist if a texture is missing. Keep this kit as the single place for
    /// per-kind scale, catch radius, and score so gameplay scripts do not hardcode art numbers.
    /// </remarks>
    public sealed class VisualKit : MonoBehaviour
    {
        [SerializeField] private Texture2D _foxTexture;
        [SerializeField] private Texture2D _coinTexture;
        [SerializeField] private Texture2D _gemTexture;
        [SerializeField] private Texture2D _treatTexture;
        [SerializeField] private Texture2D _backgroundTexture;
        [SerializeField] private Shader _colorShader;
        [SerializeField] private Shader _transparentShader;
        [SerializeField] private Shader _textureShader;
        [SerializeField] private Material _foxMaterial;
        [SerializeField] private Material _coinMaterial;
        [SerializeField] private Material _gemMaterial;
        [SerializeField] private Material _treatMaterial;
        [SerializeField] private Material _backgroundMaterial;
        [SerializeField] private Material _orangeMaterial;
        [SerializeField] private Material _purpleMaterial;
        [SerializeField] private Material _creamMaterial;
        [SerializeField] private Material _inkMaterial;
        [SerializeField] private Material _plumMaterial;
        [SerializeField] private Material _goldMaterial;
        [SerializeField] private Font _uiFont;

        private Mesh _quad;
        private Mesh _box;
        private Mesh _sphere;
        private Mesh _cylinder;
        private Mesh _octahedron;
        private Sprite _whiteSprite;
        private Sprite _roundedSprite;
        private Sprite _barSprite;

        /// <summary>
        /// Fox PNG used on the catcher billboard. Null falls back to whatever Initialize received.
        /// </summary>
        public Texture2D FoxTexture => _foxTexture;

        /// <summary>Unit XY quad facing -Z. Used for fox, items, and the illustrated backdrop.</summary>
        public Mesh Quad => _quad != null ? _quad : (_quad = PrimitiveFactory.CreateQuad());

        /// <summary>Unit cube. Used for the thin ground slab under the fox.</summary>
        public Mesh Box => _box != null ? _box : (_box = PrimitiveFactory.CreateBox());

        /// <summary>Low-poly sphere kept for optional decorative meshes without importing FBX.</summary>
        public Mesh Sphere => _sphere != null ? _sphere : (_sphere = PrimitiveFactory.CreateSphere());

        /// <summary>Cylinder kept as a coin-disc fallback if sprite materials are missing.</summary>
        public Mesh Cylinder => _cylinder != null ? _cylinder : (_cylinder = PrimitiveFactory.CreateCylinder());

        /// <summary>Octahedron kept as a gem fallback mesh.</summary>
        public Mesh Octahedron => _octahedron != null ? _octahedron : (_octahedron = PrimitiveFactory.CreateOctahedron());

        /// <summary>Unlit transparent material sampling the fox texture.</summary>
        public Material FoxMaterial => _foxMaterial;

        /// <summary>Unlit transparent material for the star-coin sprite.</summary>
        public Material CoinMaterial => _coinMaterial;

        /// <summary>Unlit transparent material for the gem / puzzle-piece sprite.</summary>
        public Material GemMaterial => _gemMaterial;

        /// <summary>Unlit transparent material for the treat sprite.</summary>
        public Material TreatMaterial => _treatMaterial;

        /// <summary>Opaque unlit texture material for the playfield backdrop.</summary>
        public Material BackgroundMaterial => _backgroundMaterial;

        /// <summary>Solid orange unlit material (CTA / timer fallback color).</summary>
        public Material OrangeMaterial => _orangeMaterial;

        /// <summary>Solid purple unlit material (Restart / gem fallback).</summary>
        public Material PurpleMaterial => _purpleMaterial;

        /// <summary>Solid warm-white unlit material (treat fallback).</summary>
        public Material CreamMaterial => _creamMaterial;

        /// <summary>Solid deep-ink unlit material matching the camera clear color.</summary>
        public Material InkMaterial => _inkMaterial;

        /// <summary>Solid plum unlit material used by the ground slab.</summary>
        public Material PlumMaterial => _plumMaterial;

        /// <summary>Solid gold unlit material used if the coin sprite is missing.</summary>
        public Material GoldMaterial => _goldMaterial;

        /// <summary>
        /// Built-in legacy font referenced so IL2CPP stripping cannot drop UI text from WebGL.
        /// </summary>
        public Font UiFont => _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>
        /// 1×1 white sprite used by uGUI Images (timer fill, end-card dimmer). Created lazily from Texture2D.whiteTexture.
        /// </summary>
        public Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 4f);
                return _whiteSprite;
            }
        }

        /// <summary>
        /// 64px sliced rounded-rect sprite used by buttons, the end card, and the CTA toast.
        /// </summary>
        /// <remarks>
        /// Generated at runtime with a 18px corner radius so we do not ship a 9-slice PNG. Alpha is
        /// baked into the texture; Image.type is Sliced via the sprite border.
        /// </remarks>
        public Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null)
                {
                    return _roundedSprite;
                }

                const int size = 64;
                const int radius = 18;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float ax = Mathf.Min(x, size - 1 - x);
                        float ay = Mathf.Min(y, size - 1 - y);
                        float alpha = 1f;
                        if (ax < radius && ay < radius)
                        {
                            float dx = radius - ax;
                            float dy = radius - ay;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            alpha = Mathf.Clamp01(radius + 0.5f - d);
                        }

                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false, true);
                _roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return _roundedSprite;
            }
        }

        /// <summary>
        /// Thin capsule sprite for HUD meters. Pixels-per-unit matches the canvas so 9-slice corners stay circular.
        /// </summary>
        /// <remarks>
        /// The button <see cref="RoundedSprite"/> uses 18px borders at PPU 64, which become ~28 canvas pixels
        /// and squash inside an 18px timer track. This sprite is 128×18 with a 9px radius at PPU 100, so a
        /// track of the same height is a true pill when sliced.
        /// </remarks>
        public Sprite BarSprite
        {
            get
            {
                if (_barSprite != null)
                {
                    return _barSprite;
                }

                const int width = 128;
                const int height = 18;
                const int radius = 9;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float ax = Mathf.Min(x, width - 1 - x);
                        float ay = Mathf.Min(y, height - 1 - y);
                        float alpha = 1f;
                        if (ax < radius && ay < radius)
                        {
                            float dx = radius - ax;
                            float dy = radius - ay;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            alpha = Mathf.Clamp01(radius + 0.5f - d);
                        }

                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false, true);
                _barSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                return _barSprite;
            }
        }

        /// <summary>
        /// Resolves Playable/Unlit* shaders, builds missing materials, and assigns sprite textures.
        /// </summary>
        /// <param name="foxTexture">Optional fox PNG. The serialized Inspector texture wins when already assigned.</param>
        /// <remarks>
        /// Call once from bootstrap before catcher/world construction. Shader.Find is used only as a
        /// fallback; the editor factory serializes shader references so stripping cannot drop them.
        /// </remarks>
        public void Initialize(Texture2D foxTexture)
        {
            if (_foxTexture == null)
            {
                _foxTexture = foxTexture;
            }

            if (_colorShader == null)
            {
                _colorShader = Shader.Find("Playable/UnlitColor");
            }

            if (_transparentShader == null)
            {
                _transparentShader = Shader.Find("Playable/UnlitTransparent");
            }

            if (_textureShader == null)
            {
                _textureShader = Shader.Find("Playable/UnlitTexture");
            }

            _orangeMaterial = EnsureColorMaterial(_orangeMaterial, "Mat_Orange", Palette.Orange);
            _purpleMaterial = EnsureColorMaterial(_purpleMaterial, "Mat_Purple", Palette.Purple);
            _creamMaterial = EnsureColorMaterial(_creamMaterial, "Mat_Cream", Palette.WarmWhite);
            _inkMaterial = EnsureColorMaterial(_inkMaterial, "Mat_Ink", Palette.DeepInk);
            _plumMaterial = EnsureColorMaterial(_plumMaterial, "Mat_Plum", Palette.Plum);
            _goldMaterial = EnsureColorMaterial(_goldMaterial, "Mat_Gold", Palette.CoinMetal);
            _foxMaterial = EnsureSpriteMaterial(_foxMaterial, "Mat_Fox", _foxTexture, true);
            _coinMaterial = EnsureSpriteMaterial(_coinMaterial, "Mat_Coin", _coinTexture, true);
            _gemMaterial = EnsureSpriteMaterial(_gemMaterial, "Mat_Gem", _gemTexture, true);
            _treatMaterial = EnsureSpriteMaterial(_treatMaterial, "Mat_Treat", _treatTexture, true);
            _backgroundMaterial = EnsureSpriteMaterial(_backgroundMaterial, "Mat_Background", _backgroundTexture, false);
            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        /// <summary>
        /// Material used by a falling item. Prefers the painted sprite; falls back to a solid palette color.
        /// </summary>
        /// <param name="kind">Coin, gem, or treat.</param>
        public Material MaterialFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Gem:
                    return _gemMaterial != null ? _gemMaterial : _purpleMaterial;
                case ItemKind.Treat:
                    return _treatMaterial != null ? _treatMaterial : _creamMaterial;
                default:
                    return _coinMaterial != null ? _coinMaterial : _goldMaterial;
            }
        }

        /// <summary>
        /// Mesh used by a falling item. Always a billboard quad so painted sprites stay readable on WebGL.
        /// </summary>
        /// <param name="kind">Kept so a later kind can opt into a 3D mesh without changing FallingItem.</param>
        public Mesh MeshFor(ItemKind kind)
        {
            return Quad;
        }

        /// <summary>
        /// Local scale of the item visual. Coins are slightly larger than gems/treats.
        /// </summary>
        /// <param name="kind">Collectible variant launched by the spawner.</param>
        public static Vector3 ScaleFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Gem:
                    return new Vector3(0.78f, 0.78f, 1f);
                case ItemKind.Treat:
                    return new Vector3(0.8f, 0.8f, 1f);
                default:
                    return new Vector3(0.86f, 0.86f, 1f);
            }
        }

        /// <summary>
        /// Extra catch radius in world units, matched to the sprite size (added to GameConfig.CatchRadius).
        /// </summary>
        /// <param name="kind">Collectible variant being launched.</param>
        public static float RadiusFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Gem:
                    return 0.38f;
                case ItemKind.Treat:
                    return 0.36f;
                default:
                    return 0.4f;
            }
        }

        /// <summary>
        /// Points awarded when this kind is caught: gem 3, coin 2, treat 1.
        /// </summary>
        /// <param name="kind">Collectible that just entered the Caught state.</param>
        public static int ScoreFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Gem:
                    return 3;
                case ItemKind.Treat:
                    return 1;
                default:
                    return 2;
            }
        }

        /// <summary>
        /// Returns <paramref name="current"/> if already assigned; otherwise builds a DontSave unlit color material.
        /// </summary>
        private Material EnsureColorMaterial(Material current, string name, Color color)
        {
            if (current != null)
            {
                return current;
            }

            var material = new Material(_colorShader != null ? _colorShader : Shader.Find("Unlit/Color"))
            {
                name = name,
                color = color,
                hideFlags = HideFlags.DontSave
            };
            return material;
        }

        /// <summary>
        /// Ensures a textured unlit material. Transparent items use UnlitTransparent; the backdrop uses UnlitTexture.
        /// </summary>
        private Material EnsureSpriteMaterial(Material current, string name, Texture2D texture, bool transparent)
        {
            Shader shader = transparent
                ? (_transparentShader != null ? _transparentShader : Shader.Find("Playable/UnlitTransparent"))
                : (_textureShader != null ? _textureShader : Shader.Find("Playable/UnlitTexture"));
            if (current == null)
            {
                current = new Material(shader)
                {
                    name = name,
                    hideFlags = HideFlags.DontSave
                };
            }

            current.shader = shader;
            current.color = Color.white;
            if (texture != null)
            {
                current.mainTexture = texture;
            }

            return current;
        }
    }
}
