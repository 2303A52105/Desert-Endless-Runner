using UnityEngine;
using UnityEngine.UI;

namespace DesertRunner
{
    public class MainMenuUI : MonoBehaviour
    {
        GameObject settingsPanel;
        Button soundButton, settingsSoundButton, resetButton;
        bool confirmReset;

        void Start()
        {
            Build();
        }

        void Build()
        {
            UIFactory.EnsureEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("MenuCanvas", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            Text title = UIFactory.MakeLabel(root, "Title", "DESERT ENDLESS RUNNER", 120, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.72f), UIFactory.A(0.95f, 0.97f));
            title.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            UIFactory.MakeLabel(root, "Subtitle", "RUN  •  JUMP  •  SLIDE  •  SURVIVE", 40, UIFactory.Cream, TextAnchor.MiddleCenter, UIFactory.A(0.2f, 0.65f), UIFactory.A(0.8f, 0.72f));

            UIFactory.MakeButton(root, "PlayButton", "PLAY", UIFactory.Green, UIFactory.A(0.36f, 0.40f), UIFactory.A(0.64f, 0.56f), OnPlay, 80);
            UIFactory.MakeButton(root, "SettingsButton", "SETTINGS", UIFactory.Orange, UIFactory.A(0.36f, 0.25f), UIFactory.A(0.64f, 0.36f), OnSettings, 52);
            soundButton = UIFactory.MakeButton(root, "SoundButton", SoundLabel(), UIFactory.Teal, UIFactory.A(0.36f, 0.12f), UIFactory.A(0.64f, 0.21f), ToggleSound, 44);

            RectTransform stats = UIFactory.MakePanel(root, "Stats", UIFactory.PanelColor, UIFactory.A(0.02f, 0.03f), UIFactory.A(0.27f, 0.22f));
            UIFactory.MakeLabel(stats, "BestLabel", "BEST SCORE", 30, UIFactory.Cream, TextAnchor.MiddleLeft, UIFactory.A(0.06f, 0.70f), UIFactory.A(0.94f, 0.95f), false);
            UIFactory.MakeLabel(stats, "BestValue", SaveData.HighScore.ToString(), 52, Color.white, TextAnchor.MiddleLeft, UIFactory.A(0.06f, 0.46f), UIFactory.A(0.94f, 0.72f));
            UIFactory.MakeIcon(stats, "CoinIcon", UIFactory.Gold, UIFactory.A(0.06f, 0.08f), UIFactory.A(0.16f, 0.36f));
            UIFactory.MakeLabel(stats, "CoinsValue", "COINS  " + SaveData.TotalCoins, 44, UIFactory.Gold, TextAnchor.MiddleLeft, UIFactory.A(0.20f, 0.06f), UIFactory.A(0.96f, 0.40f));

            if (!Application.isMobilePlatform)
                UIFactory.MakeButton(root, "QuitButton", "QUIT", UIFactory.Red, UIFactory.A(0.90f, 0.91f), UIFactory.A(0.99f, 0.98f), OnQuit, 34);

            BuildSettings(root);
        }

        void BuildSettings(Transform root)
        {
            RectTransform dim = UIFactory.MakePanel(root, "SettingsOverlay", new Color(0f, 0f, 0f, 0.65f), UIFactory.A(0f, 0f), UIFactory.A(1f, 1f), false);
            settingsPanel = dim.gameObject;
            RectTransform box = UIFactory.MakePanel(dim, "Box", new Color(0.22f, 0.12f, 0.06f, 0.97f), UIFactory.A(0.3f, 0.2f), UIFactory.A(0.7f, 0.82f));
            UIFactory.MakeLabel(box, "Title", "SETTINGS", 76, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.78f), UIFactory.A(0.95f, 0.96f));
            settingsSoundButton = UIFactory.MakeButton(box, "SoundToggle", SoundLabel(), UIFactory.Teal, UIFactory.A(0.1f, 0.56f), UIFactory.A(0.9f, 0.72f), ToggleSound, 48);
            resetButton = UIFactory.MakeButton(box, "ResetProgress", "RESET PROGRESS", UIFactory.Red, UIFactory.A(0.1f, 0.36f), UIFactory.A(0.9f, 0.52f), OnReset, 44);
            UIFactory.MakeButton(box, "Close", "CLOSE", UIFactory.Green, UIFactory.A(0.1f, 0.10f), UIFactory.A(0.9f, 0.28f), OnCloseSettings, 52);
            settingsPanel.SetActive(false);
        }

        static string SoundLabel() { return SaveData.SoundOn ? "SOUND: ON" : "SOUND: OFF"; }

        void OnPlay() { GameManager.LoadScene(GameBootstrap.GameScene); }
        void OnSettings() { settingsPanel.SetActive(true); confirmReset = false; UIFactory.SetButtonLabel(resetButton, "RESET PROGRESS"); }
        void OnCloseSettings() { settingsPanel.SetActive(false); }

        void ToggleSound()
        {
            SaveData.SoundOn = !SaveData.SoundOn;
            SaveData.Save();
            UIFactory.SetButtonLabel(soundButton, SoundLabel());
            UIFactory.SetButtonLabel(settingsSoundButton, SoundLabel());
        }

        void OnReset()
        {
            if (!confirmReset)
            {
                confirmReset = true;
                UIFactory.SetButtonLabel(resetButton, "TAP AGAIN TO CONFIRM");
                return;
            }
            SaveData.ResetProgress();
            GameManager.LoadScene(GameBootstrap.MenuScene);
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
