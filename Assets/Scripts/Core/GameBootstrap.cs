using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesertRunner
{
    /// <summary>
    /// Builds the whole game procedurally when the MainMenu or Gameplay scene loads.
    /// This is why the scenes need no manual GameObject setup: press Play and everything is created.
    /// </summary>
    public static class GameBootstrap
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Gameplay";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Build(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Build(scene); }

        static void Build(Scene scene)
        {
            if (!scene.IsValid()) return;
            if (scene.name == GameScene) BuildGameplay();
            else if (scene.name == MenuScene) BuildMenu();
        }

        static void CommonSetup()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Physics.autoSyncTransforms = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
            SaveData.ApplyAudio();
            Obstacle.Active.Clear();
            Collectible.Active.Clear();
            // remove the placeholder camera/light stored in the scene file
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject go = roots[i];
                if (go.GetComponentInChildren<Camera>(true) != null || go.GetComponentInChildren<Light>(true) != null)
                {
                    go.SetActive(false);
                    Object.Destroy(go);
                }
            }
        }

        static Transform NewChild(Transform parent, string name)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // ------------------------------------------------------------------ GAMEPLAY
        static void BuildGameplay()
        {
            CommonSetup();
            Transform root = new GameObject("DesertRunner").transform;
            PoolManager.Init(NewChild(root, "Pool"));
            PropFactory.RegisterAll();

            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(root, false);
            systems.AddComponent<AudioManager>();
            GameManager gm = systems.AddComponent<GameManager>();
            systems.AddComponent<PowerUpManager>();
            systems.AddComponent<CollectibleUpdater>();

            Camera cam = DesertAtmosphere.CreateCamera(root);

            // Player: components added in dependency order (collider/visual/input before controller)
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root, false);
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            player.AddComponent<BoxCollider>();
            player.AddComponent<PlayerVisual>();
            player.AddComponent<InputHandler>();
            player.AddComponent<PlayerEffects>();
            player.AddComponent<PlayerController>();

            FollowCamera fc = cam.gameObject.AddComponent<FollowCamera>();
            fc.Target = player.transform;
            DesertAtmosphere.Apply(cam, root, player.transform, 150f);

            GameObject track = new GameObject("Track");
            track.transform.SetParent(root, false);
            track.AddComponent<TrackGenerator>().Init(player.transform, false);

            GameObject ui = new GameObject("GameplayUI");
            ui.transform.SetParent(root, false);
            ui.AddComponent<GameplayUI>();

            gm.StartRun();
        }

        // ------------------------------------------------------------------ MAIN MENU
        static void BuildMenu()
        {
            CommonSetup();
            Transform root = new GameObject("DesertRunnerMenu").transform;
            PoolManager.Init(NewChild(root, "Pool"));
            PropFactory.RegisterAll();

            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(root, false);
            systems.AddComponent<AudioManager>();

            Camera cam = DesertAtmosphere.CreateCamera(root);
            GameObject hero = new GameObject("Hero");
            hero.transform.SetParent(root, false);
            PlayerVisual pv = hero.AddComponent<PlayerVisual>();
            pv.SetState(AnimState.Run);
            pv.SetSpeed(12f);

            FollowCamera fc = cam.gameObject.AddComponent<FollowCamera>();
            fc.Target = hero.transform;
            fc.MenuMode = true;
            DesertAtmosphere.Apply(cam, root, hero.transform, 90f);

            GameObject track = new GameObject("Track");
            track.transform.SetParent(root, false);
            track.AddComponent<TrackGenerator>().Init(hero.transform, true);

            GameObject ui = new GameObject("MenuUI");
            ui.transform.SetParent(root, false);
            ui.AddComponent<MainMenuUI>();
        }
    }
}
