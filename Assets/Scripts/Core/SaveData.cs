using UnityEngine;

namespace DesertRunner
{
    /// <summary>Persistent data (PlayerPrefs).</summary>
    public static class SaveData
    {
        const string KeyHigh = "DER_HighScore";
        const string KeyCoins = "DER_TotalCoins";
        const string KeySound = "DER_SoundOn";
        const string KeyDist = "DER_BestDistance";

        public static int HighScore
        {
            get { return PlayerPrefs.GetInt(KeyHigh, 0); }
            set { PlayerPrefs.SetInt(KeyHigh, Mathf.Max(0, value)); }
        }

        public static int BestDistance
        {
            get { return PlayerPrefs.GetInt(KeyDist, 0); }
            set { PlayerPrefs.SetInt(KeyDist, Mathf.Max(0, value)); }
        }

        public static int TotalCoins
        {
            get { return PlayerPrefs.GetInt(KeyCoins, 0); }
            set { PlayerPrefs.SetInt(KeyCoins, Mathf.Max(0, value)); }
        }

        public static bool SoundOn
        {
            get { return PlayerPrefs.GetInt(KeySound, 1) == 1; }
            set { PlayerPrefs.SetInt(KeySound, value ? 1 : 0); ApplyAudio(); }
        }

        public static void ApplyAudio() { AudioListener.volume = SoundOn ? 1f : 0f; }
        public static void Save() { PlayerPrefs.Save(); }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(KeyHigh);
            PlayerPrefs.DeleteKey(KeyCoins);
            PlayerPrefs.DeleteKey(KeyDist);
            PlayerPrefs.Save();
        }
    }
}
