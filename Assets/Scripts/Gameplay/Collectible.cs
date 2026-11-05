using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    public enum CollectibleKind { Coin, PowerUp }

    /// <summary>Coin or power-up. Animated centrally by CollectibleUpdater (no per-object Update).</summary>
    public class Collectible : MonoBehaviour
    {
        public static readonly List<Collectible> Active = new List<Collectible>();

        public CollectibleKind Kind;
        public PowerUpType PowerType;
        public Transform Visual;
        public float SpinSpeed = 200f;

        Collider col;
        bool collecting;
        float collectTimer;
        bool initialized;
        Vector3 visualBase;
        float bobSeed;

        void OnEnable()
        {
            collecting = false;
            if (col == null) col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            if (Visual != null) Visual.localScale = Vector3.one;
            if (initialized && Visual != null) Visual.localPosition = visualBase;
            Active.Add(this);
        }

        void OnDisable() { Active.Remove(this); }

        public void Collect()
        {
            if (collecting) return;
            collecting = true;
            collectTimer = 0f;
            if (col != null) col.enabled = false;
            Vector3 pos = transform.position;
            if (Kind == CollectibleKind.Coin)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCoins(1);
                AudioManager.Play(Sfx.Coin, 0.8f);
                PropFactory.Fx("fx_coin", pos, new Color(1f, 0.85f, 0.2f));
            }
            else
            {
                if (PowerUpManager.Instance != null) PowerUpManager.Instance.Activate(PowerType);
                AudioManager.Play(Sfx.PowerUp);
                PropFactory.Fx("fx_powerup", pos, PowerUpManager.ColorOf(PowerType));
            }
        }

        public void Tick(float dt, float time, bool magnet, Vector3 playerPos)
        {
            if (Visual == null) return;
            if (!initialized)
            {
                initialized = true;
                visualBase = Visual.localPosition;
                bobSeed = Random.value * 6.28f;
            }
            if (collecting)
            {
                collectTimer += dt;
                float k = collectTimer / 0.28f;
                Visual.localScale = Vector3.one * Mathf.Lerp(1f, 0f, k);
                Visual.localPosition = visualBase + Vector3.up * (k * 1.2f);
                if (k >= 1f)
                {
                    PooledObject po = GetComponent<PooledObject>();
                    if (po != null) po.Release(); else gameObject.SetActive(false);
                }
                return;
            }
            if (SpinSpeed != 0f) Visual.Rotate(0f, SpinSpeed * dt, 0f, Space.Self);
            if (Kind == CollectibleKind.PowerUp)
                Visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(time * 3f + bobSeed) * 0.15f);
            if (magnet && Kind == CollectibleKind.Coin)
            {
                Vector3 d = playerPos + Vector3.up * 0.9f - transform.position;
                float sq = d.sqrMagnitude;
                if (sq < PowerUpManager.MagnetRadius * PowerUpManager.MagnetRadius && sq > 0.0001f)
                {
                    float dist = Mathf.Sqrt(sq);
                    transform.position += d / dist * Mathf.Min(dist, 40f * dt);
                }
            }
        }
    }

    /// <summary>Single Update that animates every active collectible and applies the magnet.</summary>
    public class CollectibleUpdater : MonoBehaviour
    {
        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            PowerUpManager pm = PowerUpManager.Instance;
            bool magnet = pm != null && pm.MagnetActive;
            Vector3 pp = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
            for (int i = Collectible.Active.Count - 1; i >= 0; i--)
            {
                if (i >= Collectible.Active.Count) continue;
                Collectible c = Collectible.Active[i];
                if (c == null) { Collectible.Active.RemoveAt(i); continue; }
                c.Tick(dt, t, magnet, pp);
            }
        }
    }
}
