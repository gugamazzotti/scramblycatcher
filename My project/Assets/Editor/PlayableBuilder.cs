using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrambly.EditorTools
{
    /// <summary>
    /// Applies size-focused WebGL player settings and writes <c>Builds/FoxCatch-WebGL.zip</c>.
    /// </summary>
    /// <remarks>
    /// Menu: Playable → Build WebGL &lt; 5MB. Batchmode:
    /// <c>Unity.exe -executeMethod Scrambly.EditorTools.PlayableBuilder.BuildWebGL</c>.
    /// The editor also auto-builds when <c>Tools/build-webgl.flag</c> exists (used by the agent workflow
    /// because an already-open Unity cannot start a second batchmode instance). After a platform switch
    /// the build continues on the next domain reload via SessionState.
    /// Status is written to <c>Builds/build-status.txt</c> (STARTED / SUCCEEDED / FAILED / OVERSIZE).
    /// Compression is Brotli with decompression fallback; managed stripping is High; IL2CPP Master + OptimizeSize.
    /// HUD meter uses a dedicated capsule sprite so 9-slice corners fit the 18px track.
    /// </remarks>
    public static class PlayableBuilder
    {
        /// <summary>Output folder for the Unity WebGL player (index.html + Build/ + TemplateData/).</summary>
        public const string BuildFolder = "Builds/WebGL";

        /// <summary>Zip reviewers download. Must stay under 5 MB.</summary>
        public const string ZipPath = "Builds/FoxCatch-WebGL.zip";

        /// <summary>One-line status file polled by the local build watcher.</summary>
        public const string StatusPath = "Builds/build-status.txt";

        /// <summary>When this file exists, the open editor starts a WebGL build after compile/import settles.</summary>
        public const string FlagPath = "Tools/build-webgl.flag";

        /// <summary>
        /// Editor menu entry. Same path as <see cref="BuildWebGL"/>.
        /// </summary>
        [MenuItem("Playable/Build WebGL < 5MB")]
        public static void BuildWebGLMenu()
        {
            BuildWebGL();
        }

        /// <summary>
        /// Builds the Fox Catch WebGL player, zips it, and records size in build-status.txt.
        /// </summary>
        /// <remarks>
        /// Ensures playable assets via PlayableSceneFactory, switches to WebGL if needed (returns early
        /// in the editor until domain reload), then calls BuildInternal. Batchmode exits 0 under 5 MB, 1 otherwise.
        /// Background art is imported at 512px max so the playfield stays sharp in the zip.
        /// </remarks>
        public static void BuildWebGL()
        {
            try
            {
                WriteStatus("STARTED");
                ApplyPlayerSettings();
                PlayableSceneFactory.EnsureAssets();

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                {
                    SessionState.SetBool("Playable_BuildAfterSwitch", true);
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                    if (!Application.isBatchMode)
                    {
                        WriteStatus("SWITCHING_TO_WEBGL");
                        return;
                    }
                }

                BuildInternal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteStatus("FAILED " + exception.Message);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        /// <summary>
        /// After every domain reload, continue a pending WebGL switch or consume build-webgl.flag.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ContinueAfterDomainReload()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        /// <summary>
        /// Waits until compile/import finish, then either resumes a platform switch or starts a flagged build.
        /// </summary>
        private static void TryAutoBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            bool pendingSwitch = SessionState.GetBool("Playable_BuildAfterSwitch", false);
            if (pendingSwitch && EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                SessionState.SetBool("Playable_BuildAfterSwitch", false);
                try
                {
                    BuildInternal();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    WriteStatus("FAILED " + exception.Message);
                }

                return;
            }

            string flag = Absolute(FlagPath);
            bool hasFlag = File.Exists(flag);
            if (hasFlag && (Shader.Find("Playable/UnlitColor") == null || Shader.Find("Playable/UnlitTexture") == null))
            {
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            if (hasFlag)
            {
                File.Delete(flag);
                BuildWebGL();
            }
        }

        /// <summary>
        /// Runs BuildPipeline.BuildPlayer into Builds/WebGL, zips the folder, and writes SUCCEEDED or OVERSIZE.
        /// </summary>
        private static void BuildInternal()
        {
            ApplyPlayerSettings();
            string output = Absolute(BuildFolder);
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { PlayableSceneFactory.ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                WriteStatus("FAILED " + report.summary.result);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            string zip = Absolute(ZipPath);
            if (File.Exists(zip))
            {
                File.Delete(zip);
            }

            ZipFile.CreateFromDirectory(output, zip, System.IO.Compression.CompressionLevel.Optimal, false);
            long bytes = new FileInfo(zip).Length;
            double mb = bytes / (1024d * 1024d);
            string message = bytes < 5d * 1024d * 1024d
                ? "SUCCEEDED zip=" + mb.ToString("0.00") + "MB"
                : "OVERSIZE zip=" + mb.ToString("0.00") + "MB";
            Debug.Log("[PlayableBuilder] " + message + " path=" + zip);
            WriteStatus(message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(bytes < 5d * 1024d * 1024d ? 0 : 1);
            }
        }

        /// <summary>
        /// Forces the size-oriented WebGL player settings used by this playable (no splash, Brotli, High stripping).
        /// </summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Scrambly";
            PlayerSettings.productName = "Fox Catch";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.defaultWebScreenWidth = 390;
            PlayerSettings.defaultWebScreenHeight = 844;
            PlayerSettings.runInBackground = false;
            PlayerSettings.gpuSkinning = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.gcIncremental = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.template = "PROJECT:PlayableAd";
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.lodBias = 0.3f;
            QualitySettings.particleRaycastBudget = 4;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
        }

        /// <summary>
        /// Overwrites Builds/build-status.txt so an external watcher can poll without reading the Editor.log.
        /// </summary>
        private static void WriteStatus(string status)
        {
            string directory = Absolute("Builds");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Absolute(StatusPath), status);
        }

        /// <summary>
        /// Resolves a path relative to the Unity project root (parent of Assets/).
        /// </summary>
        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, relative));
        }
    }
}
