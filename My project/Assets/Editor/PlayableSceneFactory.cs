using System.IO;
using Scrambly.Catch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Scrambly.EditorTools
{
    /// <summary>
    /// Editor factory that creates the Fox Catch scene, materials, prefabs, texture import settings, and GameConfig.
    /// </summary>
    /// <remarks>
    /// Called by PlayableBuilder before every WebGL build so missing art/shaders fail fast.
    /// Textures stay crunch-compressed (fox 256, items 128, background max 512) to protect the 5 MB zip.
    /// The scene itself is almost empty: PlayableBootstrap instantiates catcher, world, HUD, and systems at runtime.
    /// Prefabs exist so reviewers can inspect CatcherController / FallingItem in isolation.
    /// </remarks>
    public static class PlayableSceneFactory
    {
        /// <summary>Playable scene included in EditorBuildSettings.</summary>
        public const string ScenePath = "Assets/Scenes/FoxCatch.unity";

        /// <summary>ScriptableObject with round length, spawn curve, and catch radius.</summary>
        public const string ConfigPath = "Assets/Settings/GameConfig.asset";

        /// <summary>Fox catcher sprite (transparent, 256 max).</summary>
        public const string FoxPath = "Assets/Art/Fox.png";

        /// <summary>Star-coin collectible sprite (transparent, 128 max).</summary>
        public const string CoinPath = "Assets/Art/Coin.png";

        /// <summary>Gem / puzzle-piece collectible sprite (transparent, 128 max).</summary>
        public const string GemPath = "Assets/Art/Gem.png";

        /// <summary>Treat collectible sprite (transparent, 128 max).</summary>
        public const string TreatPath = "Assets/Art/Treat.png";

        /// <summary>Illustrated playfield backdrop (opaque, 512 max).</summary>
        public const string BackgroundPath = "Assets/Art/Background.png";

        /// <summary>Minimal Catcher prefab (controller only; visual is built at runtime).</summary>
        public const string CatcherPrefabPath = "Assets/Prefabs/Catcher.prefab";

        /// <summary>Minimal FallingItem prefab used as a review/reference asset.</summary>
        public const string ItemPrefabPath = "Assets/Prefabs/FallingItem.prefab";

        /// <summary>
        /// Generates or refreshes every playable asset required by the WebGL build.
        /// </summary>
        /// <remarks>
        /// Import settings, unlit materials, GameConfig, jslib platform flags, prefabs, and the FoxCatch scene.
        /// Throws if Playable/Unlit* shaders are not imported yet (builder retries after compile).
        /// </remarks>
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Materials");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Scenes");
            ConfigureSpriteTexture(FoxPath, 256, true);
            ConfigureSpriteTexture(CoinPath, 128, true);
            ConfigureSpriteTexture(GemPath, 128, true);
            ConfigureSpriteTexture(TreatPath, 128, true);
            ConfigureSpriteTexture(BackgroundPath, 512, false);

            Shader colorShader = Shader.Find("Playable/UnlitColor");
            Shader transparentShader = Shader.Find("Playable/UnlitTransparent");
            Shader textureShader = Shader.Find("Playable/UnlitTexture");
            if (colorShader == null || transparentShader == null || textureShader == null)
            {
                throw new FileNotFoundException("Playable shaders were not imported yet.");
            }

            Material orange = CreateColorMaterial("Assets/Art/Materials/Orange.mat", colorShader, new Color32(0xF5, 0x83, 0x24, 0xFF));
            Material purple = CreateColorMaterial("Assets/Art/Materials/Purple.mat", colorShader, new Color32(0x78, 0x45, 0xD8, 0xFF));
            Material cream = CreateColorMaterial("Assets/Art/Materials/Cream.mat", colorShader, new Color32(0xFF, 0xF6, 0xE8, 0xFF));
            Material ink = CreateColorMaterial("Assets/Art/Materials/Ink.mat", colorShader, new Color32(0x20, 0x13, 0x38, 0xFF));
            Material plum = CreateColorMaterial("Assets/Art/Materials/Plum.mat", colorShader, new Color32(0x3A, 0x1B, 0x63, 0xFF));
            Material gold = CreateColorMaterial("Assets/Art/Materials/Gold.mat", colorShader, new Color32(0xF6, 0xC1, 0x4A, 0xFF));
            Texture2D fox = AssetDatabase.LoadAssetAtPath<Texture2D>(FoxPath);
            Texture2D coin = AssetDatabase.LoadAssetAtPath<Texture2D>(CoinPath);
            Texture2D gem = AssetDatabase.LoadAssetAtPath<Texture2D>(GemPath);
            Texture2D treat = AssetDatabase.LoadAssetAtPath<Texture2D>(TreatPath);
            Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            Material foxMat = CreateTexturedMaterial("Assets/Art/Materials/Fox.mat", transparentShader, fox);
            Material coinMat = CreateTexturedMaterial("Assets/Art/Materials/Coin.mat", transparentShader, coin);
            Material gemMat = CreateTexturedMaterial("Assets/Art/Materials/Gem.mat", transparentShader, gem);
            Material treatMat = CreateTexturedMaterial("Assets/Art/Materials/Treat.mat", transparentShader, treat);
            Material backgroundMat = CreateTexturedMaterial("Assets/Art/Materials/Background.mat", textureShader, background);

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            ConfigureWebGlPlugin();
            CreateCatcherPrefab();
            CreateItemPrefab();
            CreateScene(config, fox, coin, gem, treat, background, colorShader, transparentShader, textureShader, foxMat, coinMat, gemMat, treatMat, backgroundMat, orange, purple, cream, ink, plum, gold);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Writes an empty scene with Main Camera, EventSystem, and a Playable root that holds VisualKit + Bootstrap.
        /// </summary>
        /// <remarks>
        /// SerializedObject assigns textures, shaders, and materials so runtime Initialize does not Shader.Find
        /// on a stripped player. World meshes are still created at play time by PlayableBootstrap.
        /// </remarks>
        private static void CreateScene(
            GameConfig config,
            Texture2D fox,
            Texture2D coin,
            Texture2D gem,
            Texture2D treat,
            Texture2D background,
            Shader colorShader,
            Shader transparentShader,
            Shader textureShader,
            Material foxMat,
            Material coinMat,
            Material gemMat,
            Material treatMat,
            Material backgroundMat,
            Material orange,
            Material purple,
            Material cream,
            Material ink,
            Material plum,
            Material gold)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color32(0x20, 0x13, 0x38, 0xFF);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x20, 0x13, 0x38, 0xFF);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.transform.position = new Vector3(0f, 3.35f, -11.4f);
            cameraGo.AddComponent<AudioListener>();

            var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventGo.hideFlags = HideFlags.None;

            var root = new GameObject("Playable");
            var kit = root.AddComponent<VisualKit>();
            var bootstrap = root.AddComponent<PlayableBootstrap>();
            SerializedObject kitSo = new SerializedObject(kit);
            kitSo.FindProperty("_foxTexture").objectReferenceValue = fox;
            kitSo.FindProperty("_coinTexture").objectReferenceValue = coin;
            kitSo.FindProperty("_gemTexture").objectReferenceValue = gem;
            kitSo.FindProperty("_treatTexture").objectReferenceValue = treat;
            kitSo.FindProperty("_backgroundTexture").objectReferenceValue = background;
            kitSo.FindProperty("_colorShader").objectReferenceValue = colorShader;
            kitSo.FindProperty("_transparentShader").objectReferenceValue = transparentShader;
            kitSo.FindProperty("_textureShader").objectReferenceValue = textureShader;
            kitSo.FindProperty("_foxMaterial").objectReferenceValue = foxMat;
            kitSo.FindProperty("_coinMaterial").objectReferenceValue = coinMat;
            kitSo.FindProperty("_gemMaterial").objectReferenceValue = gemMat;
            kitSo.FindProperty("_treatMaterial").objectReferenceValue = treatMat;
            kitSo.FindProperty("_backgroundMaterial").objectReferenceValue = backgroundMat;
            kitSo.FindProperty("_orangeMaterial").objectReferenceValue = orange;
            kitSo.FindProperty("_purpleMaterial").objectReferenceValue = purple;
            kitSo.FindProperty("_creamMaterial").objectReferenceValue = cream;
            kitSo.FindProperty("_inkMaterial").objectReferenceValue = ink;
            kitSo.FindProperty("_plumMaterial").objectReferenceValue = plum;
            kitSo.FindProperty("_goldMaterial").objectReferenceValue = gold;
            kitSo.FindProperty("_uiFont").objectReferenceValue = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            kitSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject bootSo = new SerializedObject(bootstrap);
            bootSo.FindProperty("_config").objectReferenceValue = config;
            bootSo.FindProperty("_foxTexture").objectReferenceValue = fox;
            bootSo.FindProperty("_camera").objectReferenceValue = camera;
            bootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>
        /// Marks BrowserConsole.jslib as WebGL-only so the editor does not try to load the JS plugin.
        /// </summary>
        private static void ConfigureWebGlPlugin()
        {
            const string pluginPath = "Assets/Plugins/WebGL/BrowserConsole.jslib";
            var importer = AssetImporter.GetAtPath(pluginPath) as PluginImporter;
            if (importer == null)
            {
                return;
            }

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform(BuildTarget.WebGL, true);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Saves a Catcher GameObject with CatcherController for Inspector review. Runtime still builds its own instance.
        /// </summary>
        private static void CreateCatcherPrefab()
        {
            var go = new GameObject("Catcher");
            go.AddComponent<CatcherController>();
            PrefabUtility.SaveAsPrefabAsset(go, CatcherPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Saves a FallingItem with a Visual child (filter + renderer) matching the pooled runtime layout.
        /// </summary>
        private static void CreateItemPrefab()
        {
            var go = new GameObject("FallingItem");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var filter = visual.AddComponent<MeshFilter>();
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var item = go.AddComponent<FallingItem>();
            item.Bind(filter, renderer, visual.transform);
            PrefabUtility.SaveAsPrefabAsset(go, ItemPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Applies crunch compression and max size. Alpha sprites keep transparency; the backdrop is opaque.
        /// </summary>
        /// <param name="path">Asset path under Assets/Art.</param>
        /// <param name="maxSize">Importer maxTextureSize (256 fox, 128 items, 512 background).</param>
        /// <param name="alpha">True for sprites that need knockout; false for the backdrop.</param>
        private static void ConfigureSpriteTexture(string path, int maxSize, bool alpha)
        {
            if (!File.Exists(path))
            {
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = alpha ? 45 : 35;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Creates or updates an unlit textured material at <paramref name="path"/>.
        /// </summary>
        private static Material CreateTexturedMaterial(string path, Shader shader, Texture2D texture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = Color.white;
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Creates or updates an unlit solid-color material at <paramref name="path"/>.
        /// </summary>
        private static Material CreateColorMaterial(string path, Shader shader, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Recursively creates an Assets/... folder if AssetDatabase does not already have it.
        /// </summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
