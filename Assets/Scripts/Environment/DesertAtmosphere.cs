using UnityEngine;
using UnityEngine.Rendering;

namespace DesertRunner
{
    /// <summary>Keeps a transform snapped to a grid under the target (infinite sand ground).</summary>
    public class GroundFollower : MonoBehaviour
    {
        public Transform Target;
        public float Grid = 10f;
        void LateUpdate()
        {
            if (Target == null) return;
            Vector3 p = Target.position;
            transform.position = new Vector3(Mathf.Round(p.x / Grid) * Grid, transform.position.y, Mathf.Round(p.z / Grid) * Grid);
        }
    }

    /// <summary>World-aligned rig that follows the camera position (sky backdrop).</summary>
    public class SkyFollower : MonoBehaviour
    {
        public Transform Target;
        void LateUpdate()
        {
            if (Target == null) return;
            transform.position = Target.position;
            transform.rotation = Quaternion.identity;
        }
    }

    public static class DesertAtmosphere
    {
        public static readonly Color FogColor = new Color(0.93f, 0.76f, 0.52f);

        public static Camera CreateCamera(Transform parent)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 4.3f, -8f);
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 60f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>Lighting, fog, sky, sun, infinite ground and sandstorm.</summary>
        public static void Apply(Camera cam, Transform root, Transform groundTarget, float sandRate)
        {
            // Fog + ambient
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 240f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.46f);
            RenderSettings.ambientIntensity = 1f;

            // Golden sun light with shadows
            GameObject lg = new GameObject("Sun Light");
            lg.transform.SetParent(root, false);
            lg.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.86f, 0.62f);
            l.intensity = 1.25f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.75f;
            RenderSettings.sun = l;
            QualitySettings.shadowDistance = 70f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;

            // Infinite sand ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            MeshFactory.DestroySafe(ground.GetComponent<Collider>());
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -0.12f, 0f);
            ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(700f, 700f, 1f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Sand;
            ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            GroundFollower gf = ground.AddComponent<GroundFollower>();
            gf.Target = groundTarget; gf.Grid = 10f;

            // Sky backdrop + sun glow (follow camera position, world aligned)
            GameObject rig = new GameObject("SkyRig");
            rig.transform.SetParent(root, false);
            SkyFollower sf = rig.AddComponent<SkyFollower>();
            sf.Target = cam.transform;
            GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sky.name = "Sky";
            MeshFactory.DestroySafe(sky.GetComponent<Collider>());
            sky.transform.SetParent(rig.transform, false);
            sky.transform.localPosition = new Vector3(0f, 100f, 420f);
            sky.transform.localScale = new Vector3(1700f, 280f, 1f);
            MeshRenderer skr = sky.GetComponent<MeshRenderer>();
            skr.sharedMaterial = MaterialFactory.SpriteMat("sky_mat", MaterialFactory.SkyGradient());
            skr.shadowCastingMode = ShadowCastingMode.Off; skr.receiveShadows = false; skr.sortingOrder = -10;
            GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sun.name = "SunGlow";
            MeshFactory.DestroySafe(sun.GetComponent<Collider>());
            sun.transform.SetParent(rig.transform, false);
            sun.transform.localPosition = new Vector3(-110f, 92f, 405f);
            sun.transform.localScale = new Vector3(130f, 130f, 1f);
            MeshRenderer sr = sun.GetComponent<MeshRenderer>();
            sr.sharedMaterial = MaterialFactory.SpriteMat("sun_mat", MaterialFactory.SunGlow());
            sr.shadowCastingMode = ShadowCastingMode.Off; sr.receiveShadows = false; sr.sortingOrder = -5;

            BuildSandstorm(cam.transform, sandRate);
        }

        static void BuildSandstorm(Transform camT, float rate)
        {
            GameObject go = new GameObject("Sandstorm");
            go.transform.SetParent(camT, false);
            go.transform.localPosition = new Vector3(0f, 0f, 24f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.80f, 0.55f, 0.32f), new Color(0.85f, 0.65f, 0.40f, 0.18f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 700;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(70f, 12f, 44f);
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-26f, -14f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            vel.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = MaterialFactory.ParticleMat();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.velocityScale = 0.06f;
            rend.lengthScale = 2.5f;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            ps.Play();
        }
    }
}
