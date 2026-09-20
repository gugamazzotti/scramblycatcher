using UnityEngine;
using UnityEngine.EventSystems;

namespace Scrambly.Catch
{
    /// <summary>
    /// Runtime composition root for the WebGL playable.
    /// </summary>
    /// <remarks>
    /// Runs at execution order -100 so camera, VisualKit, pool, HUD, CTA, pause guard, and
    /// <see cref="GameDirector"/> exist before any gameplay Update.
    /// The scene stays nearly empty on disk: meshes, catcher, backdrop, and uGUI are created here
    /// so Restart does not depend on leftover scene objects and the WebGL zip stays small.
    /// Call <see cref="ApplyAssets"/> from the editor factory if the serialized fox texture / config
    /// are missing; <see cref="Boot"/> also synthesizes a default <see cref="GameConfig"/> if needed.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayableBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Texture2D _foxTexture;
        [SerializeField] private Camera _camera;

        private bool _booted;

        #region Unity Callbacks

        private void Awake()
        {
            Boot();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Injects the round config and fox texture before play mode.
        /// </summary>
        /// <param name="config">ScriptableObject with round length, spawn, and catch values. May be null if Boot should create a default.</param>
        /// <param name="foxTexture">PNG used by VisualKit when the serialized fox field is empty.</param>
        /// <remarks>
        /// The editor scene factory writes the same fields through SerializedObject;
        /// this method is the runtime/editor-play fallback.
        /// </remarks>
        public void ApplyAssets(GameConfig config, Texture2D foxTexture)
        {
            _config = config;
            _foxTexture = foxTexture;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Wires every gameplay service exactly once: camera, EventSystem, kit, playfield, pool, spawner, catcher, HUD, CTA, pause, director.
        /// </summary>
        private void Boot()
        {
            if (_booted)
            {
                return;
            }

            _booted = true;
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            Time.timeScale = 1f;

            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<GameConfig>();
            }

            _camera = _camera != null ? _camera : Camera.main;
            if (_camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                _camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.tag = "MainCamera";
            }

            ConfigureCamera();
            EnsureEventSystem();

            VisualKit kit = gameObject.GetComponent<VisualKit>();
            if (kit == null)
            {
                kit = gameObject.AddComponent<VisualKit>();
            }

            kit.Initialize(_foxTexture);

            Playfield playfield = GetOrAdd<Playfield>();
            playfield.Initialize(_camera);

            ItemPool pool = GetOrAdd<ItemPool>();
            pool.Warm(_config.PoolSize, kit);

            ItemSpawner spawner = GetOrAdd<ItemSpawner>();
            spawner.Initialize(_config, playfield, pool, kit);

            CatcherController catcher = BuildCatcher(kit, playfield);
            BuildWorld(kit, playfield);

            HudController hud = GetOrAdd<HudController>();
            CtaService cta = GetOrAdd<CtaService>();
            cta.Initialize(hud);
            hud.Initialize(kit, cta);

            GetOrAdd<FocusPauseGuard>();
            GameDirector director = GetOrAdd<GameDirector>();
            director.Initialize(_config, catcher, spawner, pool, playfield, hud);
        }

        /// <summary>
        /// Cheap perspective camera: solid ink clear, no HDR/MSAA/skybox/fog, so the WebGL player stays small.
        /// </summary>
        private void ConfigureCamera()
        {
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Palette.DeepInk;
            _camera.orthographic = false;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 60f;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            _camera.depth = -1;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.DeepInk;
        }

        /// <summary>
        /// Guarantees a uGUI EventSystem so HUD buttons receive clicks on desktop and touch.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>
        /// Instantiates the catcher root and fox billboard, then hands the visual to <see cref="CatcherController"/>.
        /// </summary>
        /// <param name="kit">Shared meshes and fox material.</param>
        /// <param name="playfield">Lane used to place the fox on the first frame.</param>
        /// <returns>The controller the director will tick each frame.</returns>
        private CatcherController BuildCatcher(VisualKit kit, Playfield playfield)
        {
            var root = new GameObject("Catcher");
            root.transform.SetParent(transform, false);
            var catcher = root.AddComponent<CatcherController>();

            var fox = CreateMeshObject("Fox", kit.Quad, kit.FoxMaterial, new Vector3(2.25f, 2.25f, 1f));
            fox.transform.SetParent(root.transform, false);
            fox.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            catcher.SetVisual(fox.transform);
            catcher.Initialize(_camera, playfield, _config.CatcherLerp);
            return catcher;
        }

        /// <summary>
        /// Builds the illustrated backdrop (fitted by Playfield) and a thin ground slab under the fox.
        /// </summary>
        private void BuildWorld(VisualKit kit, Playfield playfield)
        {
            var world = new GameObject("World");
            world.transform.SetParent(transform, false);

            var backdrop = CreateMeshObject("Backdrop", kit.Quad, kit.BackgroundMaterial, Vector3.one);
            backdrop.transform.SetParent(world.transform, false);
            playfield.SetBackdrop(backdrop.transform);

            var ground = CreateMeshObject("Ground", kit.Box, kit.PlumMaterial, new Vector3(22f, 0.22f, 4.5f));
            ground.transform.SetParent(world.transform, false);
            ground.transform.position = new Vector3(0f, playfield.CatcherY - 1.15f, 1.2f);
        }

        /// <summary>
        /// Creates an unlit mesh object with shadows and probes disabled for WebGL size.
        /// </summary>
        private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>
        /// Returns an existing component on this GameObject or adds one. Used so Boot is idempotent with scene-placed systems.
        /// </summary>
        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        #endregion
    }
}
