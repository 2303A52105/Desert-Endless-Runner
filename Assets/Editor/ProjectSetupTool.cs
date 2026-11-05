#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesertRunner.EditorTools
{
    /// <summary>
    /// One-time automatic project setup (runs on first open) + menu items under "Desert Runner".
    /// Creates folders, scenes, build settings, player settings (Android/Windows), graphics settings,
    /// and exports the procedural prefabs, materials, textures, meshes and audio as real assets.
    /// The game itself builds everything at runtime, so it works even if some export step fails.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetupTool
    {
        const string Marker = "ProjectSettings/DesertRunnerSetup.done";
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string GameScenePath = "Assets/Scenes/Gameplay.unity";

        static readonly string[] Folders =
        {
            "Assets/Scenes", "Assets/Materials", "Assets/Textures", "Assets/Models", "Assets/Audio", "Assets/Particles", "Assets/Settings",
            "Assets/Resources/ShaderWarmup",
            "Assets/Prefabs/Player", "Assets/Prefabs/Obstacles", "Assets/Prefabs/Coins", "Assets/Prefabs/PowerUps",
            "Assets/Prefabs/Environment", "Assets/Prefabs/UI"
        };

        static readonly Dictionary<Object, Object> persisted = new Dictionary<Object, Object>();

        static ProjectSetupTool()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (File.Exists(Marker)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetup;
                return;
            }
            RunFullSetup(true);
        }

        [MenuItem("Desert Runner/1. Run Full Project Setup")]
        public static void RunFullSetupMenu() { RunFullSetup(false); }

        static void Step(string name, System.Action action)
        {
            try { action(); }
            catch (System.Exception e) { Debug.LogWarning("[Desert Runner] Setup step '" + name + "' failed: " + e.Message); }
        }

        static void RunFullSetup(bool openGameplay)
        {
            persisted.Clear();
            Step("folders", EnsureFolders);
            Step("scenes", delegate { EnsureScenes(false); });
            Step("build settings", ConfigureBuildSettings);
            Step("player settings", ConfigurePlayerSettings);
            Step("graphics settings", ConfigureGraphics);
            Step("textures", ExportTextures);
            Step("shader warm-up materials", ExportWarmupMaterials);
            Step("prefabs", ExportPrefabs);
            Step("audio", ExportAudio);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(Marker, "Desert Runner setup completed");
            Debug.Log("[Desert Runner] Project setup complete. Open Assets/Scenes/Gameplay.unity and press Play.");
            if (openGameplay)
            {
                Step("open gameplay scene", delegate
                {
                    if (File.Exists(GameScenePath) && !EditorApplication.isPlaying)
                        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                });
            }
        }

        // ------------------------------------------------------------------ folders / scenes / settings
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void EnsureFolders()
        {
            for (int i = 0; i < Folders.Length; i++) EnsureFolder(Folders[i]);
        }

        static void EnsureScenes(bool overwrite)
        {
            string[] paths = { MenuScenePath, GameScenePath };
            for (int i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i]) && !overwrite) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, paths[i]);
            }
        }

        [MenuItem("Desert Runner/Rebuild Scenes (MainMenu + Gameplay)")]
        static void RebuildScenesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/Scenes");
            EnsureScenes(true);
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Debug.Log("[Desert Runner] Scenes rebuilt.");
        }

        static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        [MenuItem("Desert Runner/Configure Player Settings (Android + Windows)")]
        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "DesertRunnerStudio";
            PlayerSettings.productName = "Desert Endless Runner";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            Step("android settings", delegate
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.desertrunner.endlessrunner");
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            });
        }

        static void ConfigureGraphics()
        {
            Object gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            SerializedObject so = new SerializedObject(gs);
            SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
            string[] names = { "Standard", "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Sprites/Default", "Unlit/Color", "Legacy Shaders/Diffuse", "Legacy Shaders/Transparent/Diffuse" };
            if (arr != null)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    Shader sh = Shader.Find(names[n]);
                    if (sh == null) continue;
                    bool exists = false;
                    for (int i = 0; i < arr.arraySize; i++)
                        if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { exists = true; break; }
                    if (exists) continue;
                    arr.InsertArrayElementAtIndex(arr.arraySize);
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                }
            }
            // keep linear fog variants (fog is applied at runtime)
            SerializedProperty fogStrip = so.FindProperty("m_FogStripping");
            if (fogStrip != null) fogStrip.intValue = 1;
            SerializedProperty keepLinear = so.FindProperty("m_FogKeepLinear");
            if (keepLinear != null) keepLinear.boolValue = true;
            so.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------ asset persistence helpers
        static string SafeName(string n)
        {
            char[] bad = Path.GetInvalidFileNameChars();
            for (int i = 0; i < bad.Length; i++) n = n.Replace(bad[i], '_');
            return n.Replace(' ', '_').Replace('(', '_').Replace(')', '_');
        }

        static Texture2D PersistTexture(Texture2D t)
        {
            if (t == null) return null;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t))) return t;
            Object saved;
            if (persisted.TryGetValue(t, out saved) && saved != null) return (Texture2D)saved;
            try
            {
                string path = "Assets/Textures/" + SafeName(t.name) + ".png";
                if (!File.Exists(path))
                {
                    File.WriteAllBytes(path, t.EncodeToPNG());
                    AssetDatabase.ImportAsset(path);
                    TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp != null)
                    {
                        imp.wrapMode = t.wrapMode;
                        imp.mipmapEnabled = t.mipmapCount > 1;
                        imp.SaveAndReimport();
                    }
                }
                Texture2D loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (loaded == null) return t;
                persisted[t] = loaded;
                return loaded;
            }
            catch (System.Exception)
            {
                return t;   // texture not readable (e.g. built-in font atlas): keep the original reference
            }
        }

        static Material PersistMaterial(Material m, string folder)
        {
            if (m == null) return null;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) return m;
            Object saved;
            if (persisted.TryGetValue(m, out saved) && saved != null) return (Material)saved;
            string path = folder + "/" + SafeName(m.name) + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { persisted[m] = existing; return existing; }
            Material copy = new Material(m);
            copy.name = SafeName(m.name);
            copy.hideFlags = HideFlags.None;
            if (copy.HasProperty("_MainTex"))
            {
                Texture2D tex = copy.GetTexture("_MainTex") as Texture2D;
                if (tex != null)
                {
                    Vector2 scale = copy.mainTextureScale;
                    copy.SetTexture("_MainTex", PersistTexture(tex));
                    copy.mainTextureScale = scale;
                }
            }
            AssetDatabase.CreateAsset(copy, path);
            persisted[m] = copy;
            return copy;
        }

        static Mesh PersistMesh(Mesh m)
        {
            if (m == null) return null;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) return m;
            Object saved;
            if (persisted.TryGetValue(m, out saved) && saved != null) return (Mesh)saved;
            string path = "Assets/Models/" + SafeName(m.name) + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { persisted[m] = existing; return existing; }
            Mesh copy = Object.Instantiate(m);
            copy.name = SafeName(m.name);
            copy.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(copy, path);
            persisted[m] = copy;
            return copy;
        }

        static void PersistHierarchy(GameObject go)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].sharedMaterials;
                for (int j = 0; j < mats.Length; j++) mats[j] = PersistMaterial(mats[j], "Assets/Materials");
                rends[i].sharedMaterials = mats;
            }
            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++) filters[i].sharedMesh = PersistMesh(filters[i].sharedMesh);
        }

        // ------------------------------------------------------------------ exports
        static void ExportTextures()
        {
            PersistTexture(MaterialFactory.SandTexture());
            PersistTexture(MaterialFactory.PathTexture());
            PersistTexture(MaterialFactory.SoftCircle());
            PersistTexture(MaterialFactory.SkyGradient());
            PersistTexture(MaterialFactory.SunGlow());
        }

        static void ExportWarmupMaterials()
        {
            // Materials inside a Resources folder are always included in player builds, which guarantees
            // the Standard-shader keyword variants (emission, transparency, instancing) used at runtime are compiled.
            Material[] mats =
            {
                MaterialFactory.Transparent("warm_transparent", new Color(1f, 1f, 1f, 0.3f), new Color(0.2f, 0.2f, 0.2f)),
                MaterialFactory.Glow("warm_glow", Color.white, new Color(0.3f, 0.3f, 0.1f), 0.3f, 0.5f),
                MaterialFactory.Solid(Color.white, 0.2f, 0.5f),
                MaterialFactory.Textured("warm_textured", MaterialFactory.SandTexture(), Color.white, new Vector2(4f, 4f)),
                MaterialFactory.ParticleMat()
            };
            for (int i = 0; i < mats.Length; i++) PersistMaterial(mats[i], "Assets/Resources/ShaderWarmup");
        }

        static void SavePrefab(string key, string folder)
        {
            GameObject go = null;
            try
            {
                go = PropFactory.Build(key);
                go.name = key;
                PersistHierarchy(go);
                PrefabUtility.SaveAsPrefabAsset(go, folder + "/" + key + ".prefab");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Desert Runner] Could not export prefab '" + key + "': " + e.Message);
            }
            if (go != null) MeshFactory.DestroySafe(go);
        }

        static void ExportPrefabs()
        {
            foreach (string k in PropFactory.LowObstacleKeys) SavePrefab(k, "Assets/Prefabs/Obstacles");
            foreach (string k in PropFactory.TallObstacleKeys) SavePrefab(k, "Assets/Prefabs/Obstacles");
            SavePrefab(PropFactory.GateKey, "Assets/Prefabs/Obstacles");
            SavePrefab("coin", "Assets/Prefabs/Coins");
            SavePrefab("pu_magnet", "Assets/Prefabs/PowerUps");
            SavePrefab("pu_shield", "Assets/Prefabs/PowerUps");
            SavePrefab("pu_speed", "Assets/Prefabs/PowerUps");
            SavePrefab("pu_multiplier", "Assets/Prefabs/PowerUps");
            SavePrefab("pu_bonus", "Assets/Prefabs/PowerUps");
            foreach (string k in PropFactory.SceneryKeys) SavePrefab(k, "Assets/Prefabs/Environment");
            foreach (string k in PropFactory.FxKeys) SavePrefab(k, "Assets/Particles");

            // Player prefab (model built explicitly because Awake does not run in edit mode)
            GameObject player = new GameObject("Player");
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            player.AddComponent<BoxCollider>();
            PlayerVisual pv = player.AddComponent<PlayerVisual>();
            pv.Build();
            player.AddComponent<InputHandler>();
            player.AddComponent<PlayerEffects>();
            player.AddComponent<PlayerController>();
            PersistHierarchy(player);
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab");
            MeshFactory.DestroySafe(player);

            File.WriteAllText("Assets/Prefabs/UI/README.txt",
                "UI (menus, HUD, pause, game over) is generated at runtime by Scripts/UI/UIFactory.cs, MainMenuUI.cs and GameplayUI.cs.\n");
        }

        static void WriteWav(string path, float[] samples, int rate)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int n = samples.Length;
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + n * 2);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(rate);
                bw.Write(rate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(n * 2);
                for (int i = 0; i < n; i++) bw.Write((short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f));
            }
        }

        static void SaveClip(AudioClip clip, string name)
        {
            if (clip == null) return;
            float[] data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            WriteWav("Assets/Audio/" + name + ".wav", data, clip.frequency);
        }

        static void ExportAudio()
        {
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
                SaveClip(AudioManager.GetClip(s), "sfx_" + s.ToString().ToLower());
            SaveClip(AudioManager.GetMusic(), "music_desert_theme");
            SaveClip(AudioManager.GetWind(), "ambience_wind");
        }

        [MenuItem("Desert Runner/Export Prefabs, Materials, Textures and Audio")]
        static void ExportMenu()
        {
            persisted.Clear();
            EnsureFolders();
            Step("textures", ExportTextures);
            Step("shader warm-up materials", ExportWarmupMaterials);
            Step("prefabs", ExportPrefabs);
            Step("audio", ExportAudio);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Desert Runner] Assets exported.");
        }

        // ------------------------------------------------------------------ builds
        static void Build(BuildTarget target, string output)
        {
            ConfigureBuildSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildPlayerOptions opts = new BuildPlayerOptions();
            opts.scenes = new string[] { MenuScenePath, GameScenePath };
            opts.locationPathName = output;
            opts.target = target;
            opts.options = BuildOptions.None;
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Desert Runner] Build finished: " + report.summary.result + " -> " + output);
        }

        [MenuItem("Desert Runner/Build/Windows 64-bit")]
        static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, "Builds/Windows/DesertEndlessRunner.exe"); }

        [MenuItem("Desert Runner/Build/Android APK")]
        static void BuildAndroid() { Build(BuildTarget.Android, "Builds/Android/DesertEndlessRunner.apk"); }
    }
}
#endif
