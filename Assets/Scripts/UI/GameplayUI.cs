using UnityEngine;
using UnityEngine.UI;

namespace DesertRunner
{
    public class GameplayUI : MonoBehaviour
    {
        class Slot
        {
            public PowerUpType Type;
            public RectTransform Root, Fill;
            public Text Label;
        }

        Text coinsText, scoreText, distText, speedText, toastText;
        Text goScore, goBest, goCoins;
        GameObject pausePanel, gameOverPanel;
        Button pauseSoundButton, reviveButton;
        readonly Slot[] slots = new Slot[4];
        int lastScore = -1, lastDist = -1, lastSpeed10 = -1, lastMask = -1;
        float toastTimer;
        GameManager gm;

        void Start()
        {
            gm = GameManager.Instance;
            UIFactory.EnsureEventSystem();
            Build();
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                gm.CoinsChanged += OnCoinsChanged;
                OnCoinsChanged();
            }
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.Collected += OnPowerUpCollected;
        }

        void OnDestroy()
        {
            if (gm != null) { gm.StateChanged -= OnStateChanged; gm.CoinsChanged -= OnCoinsChanged; }
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.Collected -= OnPowerUpCollected;
        }

        // ------------------------------------------------------------ build
        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("HudCanvas", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            RectTransform coinPanel = UIFactory.MakePanel(root, "CoinPanel", UIFactory.PanelColor, UIFactory.A(0.012f, 0.885f), UIFactory.A(0.215f, 0.985f));
            UIFactory.MakeIcon(coinPanel, "Icon", UIFactory.Gold, UIFactory.A(0.05f, 0.2f), UIFactory.A(0.25f, 0.8f));
            coinsText = UIFactory.MakeLabel(coinPanel, "Coins", "0", 56, Color.white, TextAnchor.MiddleLeft, UIFactory.A(0.30f, 0.05f), UIFactory.A(0.97f, 0.95f));

            RectTransform scorePanel = UIFactory.MakePanel(root, "ScorePanel", UIFactory.PanelColor, UIFactory.A(0.36f, 0.875f), UIFactory.A(0.64f, 0.985f));
            UIFactory.MakeLabel(scorePanel, "ScoreLabel", "SCORE", 28, UIFactory.Cream, TextAnchor.MiddleCenter, UIFactory.A(0f, 0.62f), UIFactory.A(1f, 0.98f), false);
            scoreText = UIFactory.MakeLabel(scorePanel, "ScoreValue", "0", 64, Color.white, TextAnchor.MiddleCenter, UIFactory.A(0f, 0.04f), UIFactory.A(1f, 0.66f));

            RectTransform distPanel = UIFactory.MakePanel(root, "DistancePanel", UIFactory.PanelColor, UIFactory.A(0.40f, 0.80f), UIFactory.A(0.60f, 0.865f));
            distText = UIFactory.MakeLabel(distPanel, "Distance", "DISTANCE 0 m", 36, UIFactory.Cream, TextAnchor.MiddleCenter, UIFactory.A(0.03f, 0.05f), UIFactory.A(0.97f, 0.95f), false);

            RectTransform speedPanel = UIFactory.MakePanel(root, "SpeedPanel", UIFactory.PanelColor, UIFactory.A(0.012f, 0.02f), UIFactory.A(0.25f, 0.095f));
            speedText = UIFactory.MakeLabel(speedPanel, "Speed", "SPEED 0.0 m/s", 36, UIFactory.Cream, TextAnchor.MiddleCenter, UIFactory.A(0.03f, 0.05f), UIFactory.A(0.97f, 0.95f), false);

            UIFactory.MakeButton(root, "PauseButton", "II", UIFactory.Orange, UIFactory.A(0.925f, 0.875f), UIFactory.A(0.99f, 0.985f), OnPauseClicked, 52);

            PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.SpeedBoost, PowerUpType.ScoreMultiplier };
            for (int i = 0; i < 4; i++)
            {
                Color c = PowerUpManager.ColorOf(types[i]);
                Slot s = new Slot { Type = types[i] };
                s.Root = UIFactory.MakePanel(root, "Slot_" + types[i], new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.9f), UIFactory.A(0.012f, 0.74f), UIFactory.A(0.2f, 0.80f));
                s.Label = UIFactory.MakeLabel(s.Root, "Name", PowerUpManager.NameOf(types[i]), 30, Color.white, TextAnchor.MiddleLeft, UIFactory.A(0.06f, 0.42f), UIFactory.A(0.96f, 0.98f));
                UIFactory.MakePanel(s.Root, "BarBg", new Color(0f, 0f, 0f, 0.5f), UIFactory.A(0.06f, 0.1f), UIFactory.A(0.94f, 0.34f), false);
                s.Fill = UIFactory.MakePanel(s.Root, "BarFill", c, UIFactory.A(0.06f, 0.1f), UIFactory.A(0.94f, 0.34f), false);
                s.Root.gameObject.SetActive(false);
                slots[i] = s;
            }

            toastText = UIFactory.MakeLabel(root, "Toast", "", 64, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.3f, 0.64f), UIFactory.A(0.7f, 0.75f));
            toastText.gameObject.SetActive(false);

            BuildPause(root);
            BuildGameOver(root);
        }

        void BuildPause(Transform root)
        {
            RectTransform dim = UIFactory.MakePanel(root, "PauseOverlay", new Color(0f, 0f, 0f, 0.65f), UIFactory.A(0f, 0f), UIFactory.A(1f, 1f), false);
            pausePanel = dim.gameObject;
            RectTransform box = UIFactory.MakePanel(dim, "Box", new Color(0.22f, 0.12f, 0.06f, 0.97f), UIFactory.A(0.3f, 0.16f), UIFactory.A(0.7f, 0.84f));
            UIFactory.MakeLabel(box, "Title", "PAUSED", 80, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.84f), UIFactory.A(0.95f, 0.98f));
            UIFactory.MakeButton(box, "Resume", "RESUME", UIFactory.Green, UIFactory.A(0.1f, 0.63f), UIFactory.A(0.9f, 0.79f), OnResume, 50);
            UIFactory.MakeButton(box, "Restart", "RESTART", UIFactory.Orange, UIFactory.A(0.1f, 0.44f), UIFactory.A(0.9f, 0.60f), OnRestart, 50);
            UIFactory.MakeButton(box, "Home", "HOME", UIFactory.Teal, UIFactory.A(0.1f, 0.25f), UIFactory.A(0.9f, 0.41f), OnHome, 50);
            pauseSoundButton = UIFactory.MakeButton(box, "Sound", SoundLabel(), UIFactory.Red, UIFactory.A(0.1f, 0.06f), UIFactory.A(0.9f, 0.22f), OnToggleSound, 44);
            pausePanel.SetActive(false);
        }

        void BuildGameOver(Transform root)
        {
            RectTransform dim = UIFactory.MakePanel(root, "GameOverOverlay", new Color(0f, 0f, 0f, 0.7f), UIFactory.A(0f, 0f), UIFactory.A(1f, 1f), false);
            gameOverPanel = dim.gameObject;
            RectTransform box = UIFactory.MakePanel(dim, "Box", new Color(0.22f, 0.12f, 0.06f, 0.97f), UIFactory.A(0.28f, 0.07f), UIFactory.A(0.72f, 0.93f));
            UIFactory.MakeLabel(box, "Title", "GAME OVER", 96, new Color(1f, 0.35f, 0.25f), TextAnchor.MiddleCenter, UIFactory.A(0.03f, 0.85f), UIFactory.A(0.97f, 0.98f));
            goScore = UIFactory.MakeLabel(box, "Score", "SCORE 0", 56, Color.white, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.73f), UIFactory.A(0.95f, 0.84f));
            goBest = UIFactory.MakeLabel(box, "Best", "BEST 0", 48, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.63f), UIFactory.A(0.95f, 0.73f));
            goCoins = UIFactory.MakeLabel(box, "Coins", "COINS 0", 48, UIFactory.Gold, TextAnchor.MiddleCenter, UIFactory.A(0.05f, 0.53f), UIFactory.A(0.95f, 0.63f));
            reviveButton = UIFactory.MakeButton(box, "Revive", "REVIVE", UIFactory.Teal, UIFactory.A(0.1f, 0.36f), UIFactory.A(0.9f, 0.50f), OnRevive, 46);
            UIFactory.MakeButton(box, "Retry", "RETRY", UIFactory.Green, UIFactory.A(0.1f, 0.21f), UIFactory.A(0.9f, 0.34f), OnRestart, 50);
            UIFactory.MakeButton(box, "Home", "HOME", UIFactory.Orange, UIFactory.A(0.1f, 0.06f), UIFactory.A(0.9f, 0.19f), OnHome, 50);
            gameOverPanel.SetActive(false);
        }

        // ------------------------------------------------------------ events
        static string SoundLabel() { return SaveData.SoundOn ? "SOUND: ON" : "SOUND: OFF"; }

        void OnCoinsChanged()
        {
            if (gm != null && coinsText != null) coinsText.text = gm.RunCoins.ToString();
        }

        void OnStateChanged(GameState s)
        {
            pausePanel.SetActive(s == GameState.Paused);
            bool over = s == GameState.GameOver;
            gameOverPanel.SetActive(over);
            if (over) RefreshGameOver();
        }

        void RefreshGameOver()
        {
            goScore.text = "SCORE  " + gm.Score;
            goBest.text = gm.NewBest ? "NEW BEST!  " + SaveData.HighScore : "BEST  " + SaveData.HighScore;
            goCoins.text = "COINS  " + gm.RunCoins;
            bool can = gm.CanRevive;
            reviveButton.interactable = can;
            string label = "REVIVE  (" + gm.ReviveCost + ")";
            if (gm.ReviveCount >= GameManager.MaxRevives) label = "NO REVIVES LEFT";
            else if (!can) label = "REVIVE  (" + gm.ReviveCost + ")  NEED COINS";
            UIFactory.SetButtonLabel(reviveButton, label);
        }

        void OnPowerUpCollected(PowerUpType t)
        {
            toastText.text = t == PowerUpType.CoinBonus ? "+" + PowerUpManager.BonusCoins + " COINS!" : PowerUpManager.NameOf(t) + "!";
            toastText.color = PowerUpManager.ColorOf(t);
            toastText.gameObject.SetActive(true);
            toastTimer = 1.3f;
        }

        void OnPauseClicked() { if (gm != null) gm.Pause(); }
        void OnResume() { if (gm != null) gm.Resume(); }
        void OnRestart() { if (gm != null) gm.Restart(); else GameManager.LoadScene(GameBootstrap.GameScene); }
        void OnHome() { if (gm != null) gm.GoHome(); else GameManager.LoadScene(GameBootstrap.MenuScene); }
        void OnRevive() { if (gm != null) gm.TryRevive(); }

        void OnToggleSound()
        {
            SaveData.SoundOn = !SaveData.SoundOn;
            SaveData.Save();
            UIFactory.SetButtonLabel(pauseSoundButton, SoundLabel());
        }

        // ------------------------------------------------------------ per-frame HUD (writes strings only when values change)
        void Update()
        {
            if (gm == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) gm.TogglePause();

            int score = gm.Score;
            if (score != lastScore) { lastScore = score; scoreText.text = score.ToString(); }
            int dist = (int)gm.Distance;
            if (dist != lastDist) { lastDist = dist; distText.text = "DISTANCE  " + dist + " m"; }
            int sp = (int)(gm.CurrentSpeed * 10f);
            if (sp != lastSpeed10) { lastSpeed10 = sp; speedText.text = "SPEED  " + (sp / 10f).ToString("F1") + " m/s"; }

            UpdateSlots();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastText.gameObject.SetActive(false);
            }
        }

        void UpdateSlots()
        {
            PowerUpManager pm = PowerUpManager.Instance;
            if (pm == null) return;
            int mask = 0;
            for (int i = 0; i < 4; i++) if (pm.Remaining(slots[i].Type) > 0f) mask |= 1 << i;
            if (mask != lastMask)
            {
                lastMask = mask;
                int row = 0;
                for (int i = 0; i < 4; i++)
                {
                    bool on = (mask & (1 << i)) != 0;
                    slots[i].Root.gameObject.SetActive(on);
                    if (!on) continue;
                    float top = 0.80f - row * 0.075f;
                    slots[i].Root.anchorMin = new Vector2(0.012f, top - 0.065f);
                    slots[i].Root.anchorMax = new Vector2(0.2f, top);
                    slots[i].Root.offsetMin = Vector2.zero; slots[i].Root.offsetMax = Vector2.zero;
                    row++;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                float frac = Mathf.Clamp01(pm.Remaining(slots[i].Type) / PowerUpManager.DurationOf(slots[i].Type));
                slots[i].Fill.anchorMax = new Vector2(0.06f + 0.88f * frac, 0.34f);
            }
        }
    }
}
