using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesertRunner
{
    public enum GameState { Ready, Playing, Paused, Dead, GameOver }

    public class GameManager : MonoBehaviour
    {
        public const float StartSpeed = 13f;
        public const float MaxSpeed = 30f;
        public const float Acceleration = 0.14f;
        public const float BoostFactor = 1.45f;
        public const float SpeedChangeRate = 30f;
        public const int MaxRevives = 3;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public float BaseSpeed { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float Distance { get; private set; }
        public int RunCoins { get; private set; }
        public int ReviveCount { get; private set; }
        public bool NewBest { get; private set; }
        public int Score { get { return (int)scoreF; } }
        public float Difficulty { get { return Mathf.Clamp01(Distance / 2500f); } }
        public int ReviveCost { get { return 50 * (ReviveCount + 1); } }
        public bool CanRevive { get { return ReviveCount < MaxRevives && SaveData.TotalCoins >= ReviveCost; } }

        public event System.Action<GameState> StateChanged;
        public event System.Action CoinsChanged;

        float scoreF;
        int bestBeforeRun;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            State = GameState.Ready;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void StartRun()
        {
            Time.timeScale = 1f;
            Distance = 0f; scoreF = 0f; RunCoins = 0; ReviveCount = 0; NewBest = false;
            BaseSpeed = StartSpeed; CurrentSpeed = StartSpeed;
            bestBeforeRun = SaveData.HighScore;
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.ClearAll();
            SetState(GameState.Playing);
            if (CoinsChanged != null) CoinsChanged();
        }

        void SetState(GameState s)
        {
            State = s;
            if (StateChanged != null) StateChanged(s);
        }

        void Update()
        {
            if (State != GameState.Playing) return;
            float dt = Time.deltaTime;
            BaseSpeed = Mathf.Min(MaxSpeed, BaseSpeed + Acceleration * dt);
            PowerUpManager pm = PowerUpManager.Instance;
            float target = BaseSpeed * ((pm != null && pm.BoostActive) ? BoostFactor : 1f);
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, target, SpeedChangeRate * dt);
            float dz = CurrentSpeed * dt;
            Distance += dz;
            scoreF += dz * (pm != null ? pm.ScoreMultiplier : 1);
        }

        public void AddCoins(int n)
        {
            RunCoins += n;
            SaveData.TotalCoins += n;
            scoreF += n * 10f * (PowerUpManager.Instance != null ? PowerUpManager.Instance.ScoreMultiplier : 1);
            if (CoinsChanged != null) CoinsChanged();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            AudioManager.SetMusicVolume(0.15f);
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            AudioManager.SetMusicVolume(0.38f);
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void PlayerDied()
        {
            if (State != GameState.Playing) return;
            CurrentSpeed = 0f;
            SetState(GameState.Dead);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(1.2f);
            if (State == GameState.Dead) FinishGame();
        }

        void FinishGame()
        {
            NewBest = Score > bestBeforeRun;
            if (Score > SaveData.HighScore) SaveData.HighScore = Score;
            if ((int)Distance > SaveData.BestDistance) SaveData.BestDistance = (int)Distance;
            SaveData.Save();
            AudioManager.Play(Sfx.GameOver);
            SetState(GameState.GameOver);
        }

        public bool TryRevive()
        {
            if (State != GameState.GameOver || !CanRevive) return false;
            SaveData.TotalCoins -= ReviveCost;
            ReviveCount++;
            SaveData.Save();
            CurrentSpeed = BaseSpeed * 0.7f;
            Time.timeScale = 1f;
            if (PlayerController.Instance != null)
            {
                float z = PlayerController.Instance.transform.position.z;
                Obstacle.ClearRange(z - 8f, z + 50f);
                PlayerController.Instance.Revive();
            }
            AudioManager.Play(Sfx.Revive);
            SetState(GameState.Playing);
            if (CoinsChanged != null) CoinsChanged();
            return true;
        }

        public static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SaveData.Save();
            SceneManager.LoadScene(sceneName);
        }

        public void Restart() { LoadScene(GameBootstrap.GameScene); }
        public void GoHome() { LoadScene(GameBootstrap.MenuScene); }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            SaveData.Save();
            if (State == GameState.Playing) Pause();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && State == GameState.Playing) Pause();
        }

        void OnApplicationQuit() { SaveData.Save(); }
    }
}
