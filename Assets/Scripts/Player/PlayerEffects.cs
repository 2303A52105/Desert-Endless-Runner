using UnityEngine;
using UnityEngine.Rendering;

namespace DesertRunner
{
    /// <summary>Visual effects for active power-ups (shield bubble, magnet aura, boost trail, multiplier sparkle).</summary>
    public class PlayerEffects : MonoBehaviour
    {
        GameObject shield;
        Transform shieldT;
        ParticleSystem magnetFx, multFx, boostFx;

        void Start()
        {
            shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldBubble";
            MeshFactory.DestroySafe(shield.GetComponent<Collider>());
            shield.transform.SetParent(transform, false);
            shield.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            MeshRenderer mr = shield.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialFactory.Transparent("shield_bubble", new Color(0.3f, 0.85f, 1f, 0.28f), new Color(0.1f, 0.35f, 0.45f));
            mr.shadowCastingMode = ShadowCastingMode.Off;
            shieldT = shield.transform;
            shield.SetActive(false);

            magnetFx = MakeAura("MagnetAura", new Color(1f, 0.3f, 0.2f, 0.8f), 1.5f, 28f, -1.4f);
            multFx = MakeAura("MultiplierAura", new Color(0.85f, 0.5f, 1f, 0.8f), 0.9f, 22f, 0.8f);
            boostFx = MakeAura("BoostTrail", new Color(1f, 0.85f, 0.25f, 0.9f), 0.5f, 60f, 1.5f);
        }

        ParticleSystem MakeAura(string name, Color color, float radius, float rate, float speed)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.8f;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 120;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = radius;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = MaterialFactory.ParticleMat();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return ps;
        }

        static void SetEmitting(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on) { if (!ps.isPlaying) ps.Play(); }
            else if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Update()
        {
            PowerUpManager pm = PowerUpManager.Instance;
            if (pm == null || shield == null) return;
            bool sh = pm.ShieldActive;
            if (sh)
            {
                float rem = pm.Remaining(PowerUpType.Shield);
                bool visible = rem > 3f || Mathf.Sin(Time.time * 18f) > 0f;
                if (shield.activeSelf != visible) shield.SetActive(visible);
                shieldT.localScale = Vector3.one * (1.9f + 0.08f * Mathf.Sin(Time.time * 6f));
            }
            else if (shield.activeSelf) shield.SetActive(false);
            SetEmitting(magnetFx, pm.MagnetActive);
            SetEmitting(boostFx, pm.BoostActive);
            SetEmitting(multFx, pm.ScoreMultiplier > 1);
        }
    }
}
