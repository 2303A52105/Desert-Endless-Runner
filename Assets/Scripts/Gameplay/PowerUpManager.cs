using UnityEngine;

namespace DesertRunner
{
    public enum PowerUpType { Magnet = 0, Shield = 1, SpeedBoost = 2, ScoreMultiplier = 3, CoinBonus = 4 }

    public class PowerUpManager : MonoBehaviour
    {
        public const float MagnetDuration = 10f, ShieldDuration = 20f, BoostDuration = 6f, MultiplierDuration = 12f;
        public const int BonusCoins = 25;
        public const float MagnetRadius = 9f;

        public static PowerUpManager Instance { get; private set; }

        readonly float[] remaining = new float[5];
        public event System.Action<PowerUpType> Collected;

        public bool MagnetActive { get { return remaining[0] > 0f; } }
        public bool ShieldActive { get { return remaining[1] > 0f; } }
        public bool BoostActive { get { return remaining[2] > 0f; } }
        public int ScoreMultiplier { get { return remaining[3] > 0f ? 2 : 1; } }

        public static Color ColorOf(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Magnet: return new Color(0.95f, 0.2f, 0.2f);
                case PowerUpType.Shield: return new Color(0.3f, 0.85f, 1f);
                case PowerUpType.SpeedBoost: return new Color(0.75f, 1f, 0.2f);
                case PowerUpType.ScoreMultiplier: return new Color(0.75f, 0.35f, 1f);
                default: return new Color(1f, 0.85f, 0.2f);
            }
        }

        public static string NameOf(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Magnet: return "MAGNET";
                case PowerUpType.Shield: return "SHIELD";
                case PowerUpType.SpeedBoost: return "SPEED BOOST";
                case PowerUpType.ScoreMultiplier: return "SCORE x2";
                default: return "COIN BONUS";
            }
        }

        public static float DurationOf(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Magnet: return MagnetDuration;
                case PowerUpType.Shield: return ShieldDuration;
                case PowerUpType.SpeedBoost: return BoostDuration;
                case PowerUpType.ScoreMultiplier: return MultiplierDuration;
                default: return 0f;
            }
        }

        public float Remaining(PowerUpType t) { return remaining[(int)t]; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Activate(PowerUpType t)
        {
            if (t == PowerUpType.CoinBonus)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCoins(BonusCoins);
            }
            else
            {
                remaining[(int)t] = DurationOf(t);
            }
            if (Collected != null) Collected(t);
        }

        public bool ConsumeShield()
        {
            if (!ShieldActive) return false;
            remaining[(int)PowerUpType.Shield] = 0f;
            return true;
        }

        public void ClearAll()
        {
            for (int i = 0; i < remaining.Length; i++) remaining[i] = 0f;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < 4; i++)
            {
                if (remaining[i] <= 0f) continue;
                remaining[i] -= dt;
                if (remaining[i] <= 0f)
                {
                    remaining[i] = 0f;
                    if (i == (int)PowerUpType.SpeedBoost && PlayerController.Instance != null)
                        PlayerController.Instance.GrantInvulnerability(1f);
                }
            }
        }
    }
}
