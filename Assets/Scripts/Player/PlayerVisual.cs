using UnityEngine;

namespace DesertRunner
{
    public enum AnimState { Run, Jump, Slide, Dead }

    /// <summary>Procedural desert-explorer character built from primitives, animated by code.</summary>
    public class PlayerVisual : MonoBehaviour
    {
        Transform model, legL, legR, armL, armR, scarfTail, head;
        Renderer[] renderers;
        bool built, blink, wasBlink;
        AnimState state = AnimState.Run;
        float phase, speed = 14f, steer;

        static Material S(float r, float g, float b, float m = 0f, float s = 0.1f)
        {
            return MaterialFactory.Solid(new Color(r, g, b), m, s);
        }

        void Awake() { Build(); }

        public void SetState(AnimState s) { state = s; }
        public void SetSpeed(float s) { speed = s; }
        public void SetSteer(float dx) { steer = dx; }
        public void SetBlink(bool b) { blink = b; }

        static GameObject Part(Transform parent, string name, PrimitiveType prim, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Primitive(prim);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        public void Build()
        {
            if (built) return;
            built = true;
            model = Pivot(transform, "Model", Vector3.zero);
            Material skin = S(0.86f, 0.64f, 0.46f), cream = S(0.93f, 0.88f, 0.76f), vest = S(0.16f, 0.45f, 0.50f);
            Material pants = S(0.55f, 0.38f, 0.22f), boots = S(0.25f, 0.16f, 0.10f), wrap = S(0.96f, 0.94f, 0.88f);
            Material red = S(0.80f, 0.20f, 0.15f), pack = S(0.55f, 0.33f, 0.15f), goggles = S(0.1f, 0.1f, 0.12f, 0.3f, 0.5f);

            Part(model, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.28f, 0f), new Vector3(0.5f, 0.62f, 0.30f), cream);
            Part(model, "Vest", PrimitiveType.Cube, new Vector3(0f, 1.22f, 0f), new Vector3(0.53f, 0.46f, 0.33f), vest);
            Part(model, "Belt", PrimitiveType.Cube, new Vector3(0f, 0.98f, 0f), new Vector3(0.53f, 0.09f, 0.33f), boots);
            Part(model, "Pack", PrimitiveType.Cube, new Vector3(0f, 1.3f, -0.26f), new Vector3(0.38f, 0.45f, 0.2f), pack);
            Part(model, "NeckScarf", PrimitiveType.Cube, new Vector3(0f, 1.58f, 0f), new Vector3(0.36f, 0.1f, 0.34f), red);

            legL = Pivot(model, "LegL", new Vector3(-0.14f, 0.95f, 0f));
            legR = Pivot(model, "LegR", new Vector3(0.14f, 0.95f, 0f));
            Transform[] legs = { legL, legR };
            for (int i = 0; i < 2; i++)
            {
                Part(legs[i], "Leg", PrimitiveType.Capsule, new Vector3(0f, -0.45f, 0f), new Vector3(0.21f, 0.45f, 0.21f), pants);
                Part(legs[i], "Boot", PrimitiveType.Cube, new Vector3(0f, -0.88f, 0.04f), new Vector3(0.2f, 0.14f, 0.3f), boots);
            }
            armL = Pivot(model, "ArmL", new Vector3(-0.33f, 1.5f, 0f));
            armR = Pivot(model, "ArmR", new Vector3(0.33f, 1.5f, 0f));
            Transform[] arms = { armL, armR };
            for (int i = 0; i < 2; i++)
            {
                Part(arms[i], "Arm", PrimitiveType.Capsule, new Vector3(0f, -0.32f, 0f), new Vector3(0.15f, 0.34f, 0.15f), cream);
                Part(arms[i], "Hand", PrimitiveType.Sphere, new Vector3(0f, -0.68f, 0f), new Vector3(0.14f, 0.14f, 0.14f), skin);
            }
            head = Pivot(model, "Head", new Vector3(0f, 1.62f, 0f));
            Part(head, "Face", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0f), new Vector3(0.32f, 0.32f, 0.32f), skin);
            Part(head, "Wrap", PrimitiveType.Sphere, new Vector3(0f, 0.27f, 0f), new Vector3(0.38f, 0.28f, 0.38f), wrap);
            Part(head, "Goggles", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.15f), new Vector3(0.3f, 0.09f, 0.06f), goggles);
            scarfTail = Pivot(model, "ScarfTail", new Vector3(0f, 1.6f, -0.14f));
            Part(scarfTail, "Tail", PrimitiveType.Cube, new Vector3(0f, 0f, -0.25f), new Vector3(0.1f, 0.04f, 0.5f), red);

            renderers = GetComponentsInChildren<Renderer>();
        }

        static void Pose(Transform t, float x, float z, float k)
        {
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(x, 0f, z), k);
        }

        void Update()
        {
            if (model == null) return;
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-dt * 18f);
            float tiltX = 0f, offY = 0f;
            switch (state)
            {
                case AnimState.Run:
                {
                    phase += dt * (7f + speed * 0.28f);
                    float s = Mathf.Sin(phase);
                    legL.localRotation = Quaternion.Euler(s * 50f, 0f, 0f);
                    legR.localRotation = Quaternion.Euler(-s * 50f, 0f, 0f);
                    armL.localRotation = Quaternion.Euler(-s * 45f, 0f, 0f);
                    armR.localRotation = Quaternion.Euler(s * 45f, 0f, 0f);
                    tiltX = 10f;
                    offY = Mathf.Abs(Mathf.Cos(phase)) * 0.05f;
                    break;
                }
                case AnimState.Jump:
                    Pose(legL, -50f, 0f, k); Pose(legR, 30f, 0f, k);
                    Pose(armL, -140f, -8f, k); Pose(armR, -140f, 8f, k);
                    tiltX = 5f;
                    break;
                case AnimState.Slide:
                    Pose(legL, 0f, 0f, k); Pose(legR, 0f, 0f, k);
                    Pose(armL, -30f, 0f, k); Pose(armR, -30f, 0f, k);
                    tiltX = -80f; offY = 0.3f;
                    break;
                default:
                    Pose(legL, 10f, 0f, k); Pose(legR, -10f, 0f, k);
                    Pose(armL, 20f, 15f, k); Pose(armR, 20f, -15f, k);
                    tiltX = 80f; offY = 0.15f;
                    break;
            }
            float yaw = Mathf.Clamp(steer * 10f, -25f, 25f);
            float roll = -Mathf.Clamp(steer * 5f, -12f, 12f);
            Quaternion target = Quaternion.Euler(tiltX, yaw, roll);
            model.localRotation = Quaternion.Slerp(model.localRotation, target, 1f - Mathf.Exp(-dt * 14f));
            model.localPosition = Vector3.Lerp(model.localPosition, new Vector3(0f, offY, 0f), 1f - Mathf.Exp(-dt * 14f));
            if (scarfTail != null) scarfTail.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 14f) * 8f, Mathf.Sin(Time.time * 9f) * 14f, 0f);

            if (blink)
            {
                bool vis = Mathf.Sin(Time.time * 30f) > 0f;
                for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = vis;
                wasBlink = true;
            }
            else if (wasBlink)
            {
                for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = true;
                wasBlink = false;
            }
        }
    }
}
