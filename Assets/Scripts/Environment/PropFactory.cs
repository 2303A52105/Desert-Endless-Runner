using UnityEngine;
using UnityEngine.Rendering;

namespace DesertRunner
{
    /// <summary>Builds every obstacle, collectible, scenery piece and FX object procedurally from primitives.</summary>
    public static class PropFactory
    {
        public const float LaneWidth = 2.4f;

        public static readonly string[] LowObstacleKeys = { "ob_crate", "ob_palm", "ob_cactus", "ob_stone", "ob_fence" };
        public static readonly string[] TallObstacleKeys = { "ob_rock", "ob_pillar", "ob_vehicle" };
        public const string GateKey = "ob_gate";
        public static readonly string[] CollectibleKeys = { "coin", "pu_magnet", "pu_shield", "pu_speed", "pu_multiplier", "pu_bonus" };
        public static readonly string[] SceneryKeys = { "prop_palm", "prop_drytree", "prop_cactus", "prop_rocks", "prop_ruins", "prop_hut", "prop_temple", "prop_bridge", "prop_dune", "prop_mountain" };
        public static readonly string[] FxKeys = { "fx_coin", "fx_powerup", "fx_debris", "fx_hit" };

        public static System.Collections.Generic.IEnumerable<string> AllKeys()
        {
            foreach (string k in LowObstacleKeys) yield return k;
            foreach (string k in TallObstacleKeys) yield return k;
            yield return GateKey;
            foreach (string k in CollectibleKeys) yield return k;
            foreach (string k in SceneryKeys) yield return k;
            foreach (string k in FxKeys) yield return k;
        }

        public static void RegisterAll()
        {
            foreach (string k in AllKeys())
            {
                string key = k;
                PoolManager.Register(key, () => Build(key));
            }
        }

        /// <summary>Spawn a pooled particle burst.</summary>
        public static void Fx(string key, Vector3 pos, Color color)
        {
            GameObject go = PoolManager.Get(key, pos, Quaternion.identity);
            if (go == null) return;
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null) return;
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
            ps.Clear();
            ps.Play();
        }

        // ------------------------------------------------------------------ materials
        static Material S(float r, float g, float b, float metallic = 0f, float smooth = 0.08f)
        {
            return MaterialFactory.Solid(new Color(r, g, b), metallic, smooth);
        }
        static Material Wood { get { return S(0.60f, 0.41f, 0.22f); } }
        static Material WoodDark { get { return S(0.36f, 0.23f, 0.12f); } }
        static Material DryWood { get { return S(0.47f, 0.36f, 0.26f); } }
        static Material Stone { get { return S(0.74f, 0.64f, 0.47f); } }
        static Material StoneDark { get { return S(0.55f, 0.46f, 0.34f); } }
        static Material RockMat { get { return S(0.58f, 0.40f, 0.28f); } }
        static Material RockDark { get { return S(0.43f, 0.29f, 0.20f); } }
        static Material Leaf { get { return S(0.28f, 0.46f, 0.20f); } }
        static Material CactusMat { get { return S(0.33f, 0.52f, 0.28f); } }
        static Material Thatch { get { return S(0.78f, 0.62f, 0.32f); } }
        static Material Cloth { get { return S(0.78f, 0.22f, 0.15f); } }
        static Material Rust { get { return S(0.62f, 0.36f, 0.20f, 0.2f, 0.2f); } }
        static Material Dark { get { return S(0.16f, 0.16f, 0.18f, 0.2f, 0.25f); } }
        static Material GlowMat(Color c) { return MaterialFactory.Glow("glow_" + ColorUtility.ToHtmlStringRGB(c), c, c * 0.45f, 0.2f, 0.5f); }

        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        // ------------------------------------------------------------------ helpers
        static GameObject Part(Transform parent, string name, PrimitiveType prim, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default(Vector3))
        {
            GameObject go = new GameObject(name);
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localScale = scale; t.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Primitive(prim);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject MeshPart(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default(Vector3))
        {
            GameObject go = new GameObject(name);
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localScale = scale; t.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject NewObstacle(string name, ObstacleKind kind, Vector3 center, Vector3 size)
        {
            GameObject root = new GameObject(name);
            BoxCollider bc = root.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = center; bc.size = size;
            Obstacle ob = root.AddComponent<Obstacle>();
            ob.Kind = kind;
            return root;
        }

        static GameObject NewCollectible(string name, CollectibleKind kind, PowerUpType type, float radius, out Transform spin)
        {
            GameObject root = new GameObject(name);
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true; sc.radius = radius;
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            Collectible c = root.AddComponent<Collectible>();
            c.Kind = kind; c.PowerType = type;
            spin = new GameObject("Spin").transform;
            spin.SetParent(root.transform, false);
            c.Visual = spin;
            return root;
        }

        // ------------------------------------------------------------------ build switch
        public static GameObject Build(string key)
        {
            switch (key)
            {
                case "ob_crate": return BuildCrate();
                case "ob_rock": return BuildRock();
                case "ob_palm": return BuildFallenPalm();
                case "ob_cactus": return BuildCactusObstacle();
                case "ob_stone": return BuildStoneBarrier();
                case "ob_pillar": return BuildPillar();
                case "ob_vehicle": return BuildVehicle();
                case "ob_fence": return BuildFence();
                case "ob_gate": return BuildGate();
                case "coin": return BuildCoin();
                case "pu_magnet": return BuildPowerUp(PowerUpType.Magnet);
                case "pu_shield": return BuildPowerUp(PowerUpType.Shield);
                case "pu_speed": return BuildPowerUp(PowerUpType.SpeedBoost);
                case "pu_multiplier": return BuildPowerUp(PowerUpType.ScoreMultiplier);
                case "pu_bonus": return BuildPowerUp(PowerUpType.CoinBonus);
                case "prop_palm": return BuildPalm();
                case "prop_drytree": return BuildDryTree();
                case "prop_cactus": return BuildBigCactus();
                case "prop_rocks": return BuildRocks();
                case "prop_ruins": return BuildRuins();
                case "prop_hut": return BuildHut();
                case "prop_temple": return BuildTemple();
                case "prop_bridge": return BuildBridge();
                case "prop_dune": return BuildDune();
                case "prop_mountain": return BuildMountain();
                case "fx_coin": return BuildBurst("FxCoin", 12, 2f, 4.5f, 0.10f, 0.20f, 0.5f, 0.4f);
                case "fx_powerup": return BuildBurst("FxPowerUp", 28, 3f, 7f, 0.20f, 0.40f, 0.8f, 0.2f);
                case "fx_debris": return BuildBurst("FxDebris", 30, 3f, 8f, 0.15f, 0.40f, 0.9f, 1.5f);
                case "fx_hit": return BuildBurst("FxHit", 40, 4f, 10f, 0.20f, 0.50f, 0.8f, 0.5f);
                default:
                    Debug.LogWarning("PropFactory: unknown key " + key);
                    return new GameObject(key);
            }
        }

        // ------------------------------------------------------------------ OBSTACLES
        static GameObject BuildCrate()
        {
            GameObject r = NewObstacle("Crate", ObstacleKind.Low, V(0f, 0.55f, 0f), V(1.5f, 1.1f, 1.4f));
            Transform t = r.transform;
            Part(t, "Box", PrimitiveType.Cube, V(0f, 0.55f, 0f), V(1.4f, 1.1f, 1.3f), Wood);
            Part(t, "BattenL", PrimitiveType.Cube, V(-0.66f, 0.55f, 0f), V(0.12f, 1.14f, 1.34f), WoodDark);
            Part(t, "BattenR", PrimitiveType.Cube, V(0.66f, 0.55f, 0f), V(0.12f, 1.14f, 1.34f), WoodDark);
            Part(t, "BattenT", PrimitiveType.Cube, V(0f, 1.08f, 0f), V(1.4f, 0.1f, 1.34f), WoodDark);
            Part(t, "BattenB", PrimitiveType.Cube, V(0f, 0.03f, 0f), V(1.4f, 0.1f, 1.34f), WoodDark);
            Part(t, "Cross1", PrimitiveType.Cube, V(0f, 0.55f, -0.67f), V(1.7f, 0.12f, 0.05f), WoodDark, V(0f, 0f, 38f));
            Part(t, "Cross2", PrimitiveType.Cube, V(0f, 0.55f, -0.67f), V(1.7f, 0.12f, 0.05f), WoodDark, V(0f, 0f, -38f));
            return r;
        }

        static GameObject BuildRock()
        {
            GameObject r = NewObstacle("Rock", ObstacleKind.Tall, V(0f, 1.2f, 0f), V(1.9f, 2.4f, 1.9f));
            Transform t = r.transform;
            MeshPart(t, "Main", MeshFactory.Rock(1), V(0f, 0.68f, 0f), V(1.5f, 2.8f, 1.5f), RockMat);
            MeshPart(t, "Small", MeshFactory.Rock(2), V(0.85f, 0.2f, -0.55f), V(0.8f, 0.7f, 0.8f), RockDark);
            return r;
        }

        static GameObject BuildFallenPalm()
        {
            GameObject r = NewObstacle("FallenPalm", ObstacleKind.Low, V(0f, 0.47f, 0f), V(2.4f, 0.95f, 1.1f));
            Transform t = r.transform;
            Part(t, "Trunk", PrimitiveType.Cylinder, V(0f, 0.45f, 0f), V(0.9f, 1.15f, 0.9f), DryWood, V(0f, 0f, 90f));
            Part(t, "Ring1", PrimitiveType.Cylinder, V(-0.5f, 0.45f, 0f), V(0.95f, 0.04f, 0.95f), WoodDark, V(0f, 0f, 90f));
            Part(t, "Ring2", PrimitiveType.Cylinder, V(0.45f, 0.45f, 0f), V(0.95f, 0.04f, 0.95f), WoodDark, V(0f, 0f, 90f));
            for (int i = 0; i < 5; i++)
                Part(t, "Frond" + i, PrimitiveType.Cube, V(1.3f, 0.45f + 0.08f * i, (i - 2) * 0.17f), V(1.0f, 0.05f, 0.28f), Leaf, V(0f, (i - 2) * 22f, 12f));
            return r;
        }

        static GameObject BuildCactusObstacle()
        {
            GameObject r = NewObstacle("CactusObstacle", ObstacleKind.Low, V(0.05f, 0.7f, 0f), V(1.7f, 1.4f, 1.2f));
            Transform t = r.transform;
            Part(t, "Main", PrimitiveType.Cylinder, V(0f, 0.65f, 0f), V(0.6f, 0.65f, 0.6f), CactusMat);
            Part(t, "Top", PrimitiveType.Sphere, V(0f, 1.3f, 0f), V(0.6f, 0.6f, 0.6f), CactusMat);
            Part(t, "ArmH", PrimitiveType.Cylinder, V(0.42f, 0.7f, 0f), V(0.26f, 0.3f, 0.26f), CactusMat, V(0f, 0f, 90f));
            Part(t, "ArmV", PrimitiveType.Cylinder, V(0.68f, 0.95f, 0f), V(0.26f, 0.3f, 0.26f), CactusMat);
            Part(t, "Small", PrimitiveType.Cylinder, V(-0.6f, 0.35f, 0.25f), V(0.42f, 0.35f, 0.42f), CactusMat);
            Part(t, "SmallTop", PrimitiveType.Sphere, V(-0.6f, 0.7f, 0.25f), V(0.42f, 0.42f, 0.42f), CactusMat);
            Part(t, "Flower", PrimitiveType.Sphere, V(0f, 1.62f, 0f), V(0.2f, 0.2f, 0.2f), S(0.95f, 0.45f, 0.6f));
            return r;
        }

        static GameObject BuildStoneBarrier()
        {
            GameObject r = NewObstacle("StoneBarrier", ObstacleKind.Low, V(0f, 0.52f, 0f), V(2.2f, 1.05f, 1.1f));
            Transform t = r.transform;
            float[] h = { 0.95f, 1.05f, 0.9f };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.74f;
                Part(t, "Block" + i, PrimitiveType.Cube, V(x, h[i] * 0.5f, 0f), V(0.7f, h[i], 1.0f), i == 1 ? Stone : StoneDark, V(0f, (i - 1) * 4f, 0f));
                Part(t, "Cap" + i, PrimitiveType.Cube, V(x, h[i] + 0.04f, 0f), V(0.76f, 0.08f, 1.06f), Stone);
            }
            return r;
        }

        static GameObject BuildPillar()
        {
            GameObject r = NewObstacle("TemplePillar", ObstacleKind.Tall, V(0f, 2.2f, 0f), V(1.4f, 4.4f, 1.4f));
            Transform t = r.transform;
            Part(t, "Base", PrimitiveType.Cube, V(0f, 0.15f, 0f), V(1.5f, 0.3f, 1.5f), StoneDark);
            Part(t, "Shaft", PrimitiveType.Cylinder, V(0f, 2.2f, 0f), V(1.1f, 1.9f, 1.1f), Stone);
            Part(t, "Capital", PrimitiveType.Cube, V(0f, 4.2f, 0f), V(1.5f, 0.35f, 1.5f), StoneDark);
            for (int i = 0; i < 3; i++)
                Part(t, "Band" + i, PrimitiveType.Cylinder, V(0f, 1f + i * 1.2f, 0f), V(1.18f, 0.05f, 1.18f), StoneDark);
            return r;
        }

        static GameObject BuildVehicle()
        {
            GameObject r = NewObstacle("AbandonedVehicle", ObstacleKind.Tall, V(0f, 0.95f, 0f), V(2.0f, 1.9f, 3.5f));
            Transform t = r.transform;
            Part(t, "Body", PrimitiveType.Cube, V(0f, 0.75f, 0f), V(1.9f, 0.7f, 3.4f), Rust);
            Part(t, "Hood", PrimitiveType.Cube, V(0f, 1.15f, 1.05f), V(1.8f, 0.1f, 1.1f), S(0.55f, 0.32f, 0.18f, 0.2f, 0.2f));
            Part(t, "Cabin", PrimitiveType.Cube, V(0f, 1.4f, -0.45f), V(1.7f, 0.7f, 1.5f), S(0.50f, 0.30f, 0.17f, 0.2f, 0.2f));
            Part(t, "WindowBack", PrimitiveType.Cube, V(0f, 1.45f, -1.21f), V(1.4f, 0.4f, 0.05f), Dark);
            Part(t, "WindowSide1", PrimitiveType.Cube, V(0.86f, 1.45f, -0.45f), V(0.05f, 0.4f, 1.1f), Dark);
            Part(t, "WindowSide2", PrimitiveType.Cube, V(-0.86f, 1.45f, -0.45f), V(0.05f, 0.4f, 1.1f), Dark);
            Part(t, "SpareTire", PrimitiveType.Cylinder, V(0f, 0.95f, -1.75f), V(0.7f, 0.1f, 0.7f), Dark, V(90f, 0f, 0f));
            Part(t, "RollBar", PrimitiveType.Cube, V(0f, 1.8f, -0.45f), V(1.6f, 0.08f, 0.08f), Dark);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.95f : 0.95f;
                float z = (i < 2) ? -1.1f : 1.1f;
                Part(t, "Wheel" + i, PrimitiveType.Cylinder, V(x, 0.4f, z), V(0.8f, 0.15f, 0.8f), Dark, V(0f, 0f, 90f));
            }
            Part(t, "Patch", PrimitiveType.Cube, V(0.96f, 0.8f, 0.3f), V(0.03f, 0.35f, 0.8f), S(0.40f, 0.25f, 0.15f));
            return r;
        }

        static GameObject BuildFence()
        {
            GameObject r = NewObstacle("WoodenBarrier", ObstacleKind.Low, V(0f, 0.5f, 0f), V(2.1f, 1.0f, 1.0f));
            Transform t = r.transform;
            Part(t, "PostL", PrimitiveType.Cube, V(-0.9f, 0.55f, 0f), V(0.2f, 1.1f, 0.2f), WoodDark);
            Part(t, "PostR", PrimitiveType.Cube, V(0.9f, 0.55f, 0f), V(0.2f, 1.1f, 0.2f), WoodDark);
            Part(t, "PlankLow", PrimitiveType.Cube, V(0f, 0.42f, -0.12f), V(2.0f, 0.22f, 0.1f), Wood);
            Part(t, "PlankHigh", PrimitiveType.Cube, V(0f, 0.82f, -0.12f), V(2.0f, 0.22f, 0.1f), Cloth);
            Part(t, "Brace", PrimitiveType.Cube, V(0f, 0.62f, 0.1f), V(2.0f, 0.12f, 0.08f), WoodDark, V(0f, 0f, 8f));
            return r;
        }

        static GameObject BuildGate()
        {
            GameObject r = NewObstacle("SlideGate", ObstacleKind.Overhead, V(0f, 1.7f, 0f), V(1.9f, 1.4f, 1.0f));
            Transform t = r.transform;
            Part(t, "PostL", PrimitiveType.Cube, V(-1.05f, 1.3f, 0f), V(0.4f, 2.6f, 0.4f), StoneDark);
            Part(t, "PostR", PrimitiveType.Cube, V(1.05f, 1.3f, 0f), V(0.4f, 2.6f, 0.4f), StoneDark);
            Part(t, "Lintel", PrimitiveType.Cube, V(0f, 2.6f, 0f), V(2.8f, 0.45f, 0.7f), Stone);
            Part(t, "LowBeam", PrimitiveType.Cube, V(0f, 1.25f, 0f), V(2.2f, 0.4f, 0.45f), WoodDark);
            Part(t, "Banner", PrimitiveType.Cube, V(0f, 1.95f, -0.1f), V(1.7f, 0.9f, 0.05f), Cloth);
            Part(t, "RopeL", PrimitiveType.Cube, V(-0.7f, 1.75f, -0.25f), V(0.05f, 0.5f, 0.05f), WoodDark);
            Part(t, "RopeR", PrimitiveType.Cube, V(0.7f, 1.75f, -0.25f), V(0.05f, 0.5f, 0.05f), WoodDark);
            return r;
        }

        // ------------------------------------------------------------------ COLLECTIBLES
        static GameObject BuildCoin()
        {
            Transform spin;
            GameObject r = NewCollectible("Coin", CollectibleKind.Coin, PowerUpType.CoinBonus, 0.7f, out spin);
            Part(spin, "Disc", PrimitiveType.Cylinder, Vector3.zero, V(0.75f, 0.06f, 0.75f), MaterialFactory.Gold, V(90f, 0f, 0f));
            Part(spin, "Inner", PrimitiveType.Cylinder, Vector3.zero, V(0.52f, 0.075f, 0.52f), MaterialFactory.GoldLight, V(90f, 0f, 0f));
            return r;
        }

        static GameObject BuildPowerUp(PowerUpType type)
        {
            Transform spin;
            GameObject r = NewCollectible("PowerUp_" + type, CollectibleKind.PowerUp, type, 1.0f, out spin);
            Collectible col = r.GetComponent<Collectible>();
            Color c = PowerUpManager.ColorOf(type);
            Part(r.transform, "Halo", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.7f,
                MaterialFactory.Transparent("halo_" + type, new Color(c.r, c.g, c.b, 0.22f), c * 0.5f));
            Material main = GlowMat(c);
            switch (type)
            {
                case PowerUpType.Magnet:
                    Part(spin, "BarL", PrimitiveType.Cube, V(-0.32f, 0f, 0f), V(0.22f, 0.8f, 0.22f), main);
                    Part(spin, "BarR", PrimitiveType.Cube, V(0.32f, 0f, 0f), V(0.22f, 0.8f, 0.22f), main);
                    Part(spin, "Bottom", PrimitiveType.Cube, V(0f, -0.4f, 0f), V(0.86f, 0.22f, 0.22f), main);
                    Part(spin, "TipL", PrimitiveType.Cube, V(-0.32f, 0.48f, 0f), V(0.23f, 0.2f, 0.23f), S(0.9f, 0.9f, 0.95f, 0.3f, 0.5f));
                    Part(spin, "TipR", PrimitiveType.Cube, V(0.32f, 0.48f, 0f), V(0.23f, 0.2f, 0.23f), S(0.9f, 0.9f, 0.95f, 0.3f, 0.5f));
                    break;
                case PowerUpType.Shield:
                    Part(spin, "Rim", PrimitiveType.Sphere, V(0f, 0f, 0.05f), V(0.95f, 1.1f, 0.2f), MaterialFactory.Gold);
                    Part(spin, "Face", PrimitiveType.Sphere, V(0f, 0f, 0f), V(0.82f, 0.97f, 0.3f), main);
                    Part(spin, "CrossV", PrimitiveType.Cube, V(0f, 0f, -0.14f), V(0.12f, 0.6f, 0.06f), MaterialFactory.GoldLight);
                    Part(spin, "CrossH", PrimitiveType.Cube, V(0f, 0.08f, -0.14f), V(0.42f, 0.12f, 0.06f), MaterialFactory.GoldLight);
                    break;
                case PowerUpType.SpeedBoost:
                    Part(spin, "BoltTop", PrimitiveType.Cube, V(0.12f, 0.3f, 0f), V(0.26f, 0.6f, 0.16f), main, V(0f, 0f, -20f));
                    Part(spin, "BoltMid", PrimitiveType.Cube, V(0f, 0f, 0f), V(0.55f, 0.18f, 0.16f), main);
                    Part(spin, "BoltBot", PrimitiveType.Cube, V(-0.12f, -0.3f, 0f), V(0.26f, 0.6f, 0.16f), main, V(0f, 0f, -20f));
                    break;
                case PowerUpType.ScoreMultiplier:
                    col.SpinSpeed = 0f;
                    Part(spin, "StarA", PrimitiveType.Cube, Vector3.zero, V(0.85f, 0.85f, 0.18f), main);
                    Part(spin, "StarB", PrimitiveType.Cube, Vector3.zero, V(0.85f, 0.85f, 0.18f), main, V(0f, 0f, 45f));
                    BuildLabel(r.transform, "x2");
                    break;
                default:
                    Part(spin, "ChestBase", PrimitiveType.Cube, V(0f, -0.2f, 0f), V(0.9f, 0.5f, 0.6f), Wood);
                    Part(spin, "ChestLid", PrimitiveType.Cylinder, V(0f, 0.1f, 0f), V(0.6f, 0.45f, 0.6f), Wood, V(0f, 0f, 90f));
                    Part(spin, "BandL", PrimitiveType.Cube, V(-0.28f, -0.05f, 0f), V(0.08f, 0.8f, 0.64f), MaterialFactory.Gold);
                    Part(spin, "BandR", PrimitiveType.Cube, V(0.28f, -0.05f, 0f), V(0.08f, 0.8f, 0.64f), MaterialFactory.Gold);
                    Part(spin, "Lock", PrimitiveType.Cube, V(0f, -0.05f, -0.32f), V(0.16f, 0.2f, 0.05f), MaterialFactory.GoldLight);
                    Part(spin, "CoinOnTop", PrimitiveType.Cylinder, V(0f, 0.5f, 0f), V(0.35f, 0.04f, 0.35f), MaterialFactory.Gold, V(90f, 0f, 0f));
                    break;
            }
            return r;
        }

        static void BuildLabel(Transform parent, string text)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            TextMesh tm = go.AddComponent<TextMesh>();
            Font f = MaterialFactory.BuiltinFont;
            tm.text = text;
            tm.font = f;
            tm.fontSize = 64;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && f != null) mr.sharedMaterial = f.material;
        }

        // ------------------------------------------------------------------ SCENERY
        static GameObject BuildDune()
        {
            GameObject root = new GameObject("Dune");
            Part(root.transform, "Mound", PrimitiveType.Sphere, Vector3.zero, Vector3.one, MaterialFactory.DuneSand);
            return root;
        }

        static GameObject BuildMountain()
        {
            GameObject root = new GameObject("Mountain");
            Transform t = root.transform;
            MeshPart(t, "Peak", MeshFactory.Cone(7), Vector3.zero, Vector3.one, S(0.66f, 0.43f, 0.30f));
            MeshPart(t, "Side", MeshFactory.Cone(6), V(0.32f, 0f, -0.2f), V(0.6f, 0.7f, 0.6f), S(0.58f, 0.38f, 0.27f));
            MeshPart(t, "Side2", MeshFactory.Cone(5), V(-0.35f, 0f, 0.15f), V(0.5f, 0.55f, 0.5f), S(0.70f, 0.47f, 0.32f));
            return root;
        }

        static GameObject BuildPalm()
        {
            GameObject root = new GameObject("Palm");
            Transform parent = root.transform;
            Material trunk = S(0.50f, 0.36f, 0.22f);
            for (int i = 0; i < 3; i++)
            {
                Transform seg = new GameObject("Seg" + i).transform;
                seg.SetParent(parent, false);
                seg.localPosition = i == 0 ? Vector3.zero : V(0f, 2.4f, 0f);
                seg.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 3f : 5f);
                float r = 0.38f - 0.05f * i;
                Part(seg, "Trunk", PrimitiveType.Cylinder, V(0f, 1.2f, 0f), V(r * 2f, 1.2f, r * 2f), trunk);
                parent = seg;
            }
            Transform crown = new GameObject("Crown").transform;
            crown.SetParent(parent, false);
            crown.localPosition = V(0f, 2.4f, 0f);
            for (int k = 0; k < 8; k++)
            {
                Transform pivot = new GameObject("Leaf" + k).transform;
                pivot.SetParent(crown, false);
                pivot.localRotation = Quaternion.Euler(28f + (k % 2) * 12f, k * 45f, 0f);
                Part(pivot, "Blade", PrimitiveType.Cube, V(0f, 0f, 1.3f), V(0.9f, 0.05f, 2.8f), Leaf);
            }
            Part(crown, "Nut1", PrimitiveType.Sphere, V(0.2f, -0.2f, 0.1f), Vector3.one * 0.35f, WoodDark);
            Part(crown, "Nut2", PrimitiveType.Sphere, V(-0.15f, -0.25f, -0.1f), Vector3.one * 0.33f, WoodDark);
            return root;
        }

        static GameObject BuildDryTree()
        {
            GameObject root = new GameObject("DryTree");
            Transform t = root.transform;
            Part(t, "Trunk", PrimitiveType.Cylinder, V(0f, 1.3f, 0f), V(0.4f, 1.3f, 0.4f), DryWood, V(0f, 0f, 4f));
            Part(t, "Upper", PrimitiveType.Cylinder, V(0.15f, 3.0f, 0f), V(0.28f, 0.8f, 0.28f), DryWood, V(0f, 0f, -12f));
            float[] ang = { 50f, -45f, 70f, -65f, 30f };
            float[] hgt = { 2.4f, 2.7f, 3.3f, 3.0f, 3.9f };
            for (int i = 0; i < ang.Length; i++)
                Part(t, "Branch" + i, PrimitiveType.Cylinder, V(Mathf.Sign(ang[i]) * 0.45f, hgt[i], (i - 2) * 0.12f), V(0.12f, 0.7f, 0.12f), DryWood, V((i - 2) * 10f, 0f, -ang[i]));
            return root;
        }

        static GameObject BuildBigCactus()
        {
            GameObject root = new GameObject("BigCactus");
            Transform t = root.transform;
            Part(t, "Main", PrimitiveType.Cylinder, V(0f, 2.2f, 0f), V(0.95f, 2.2f, 0.95f), CactusMat);
            Part(t, "Top", PrimitiveType.Sphere, V(0f, 4.4f, 0f), V(0.95f, 0.95f, 0.95f), CactusMat);
            Part(t, "ArmL1", PrimitiveType.Cylinder, V(-0.85f, 2.4f, 0f), V(0.45f, 0.5f, 0.45f), CactusMat, V(0f, 0f, 90f));
            Part(t, "ArmL2", PrimitiveType.Cylinder, V(-1.3f, 3.1f, 0f), V(0.45f, 0.8f, 0.45f), CactusMat);
            Part(t, "ArmL3", PrimitiveType.Sphere, V(-1.3f, 3.9f, 0f), V(0.45f, 0.45f, 0.45f), CactusMat);
            Part(t, "ArmR1", PrimitiveType.Cylinder, V(0.8f, 1.7f, 0f), V(0.42f, 0.45f, 0.42f), CactusMat, V(0f, 0f, 90f));
            Part(t, "ArmR2", PrimitiveType.Cylinder, V(1.2f, 2.3f, 0f), V(0.42f, 0.65f, 0.42f), CactusMat);
            Part(t, "ArmR3", PrimitiveType.Sphere, V(1.2f, 2.95f, 0f), V(0.42f, 0.42f, 0.42f), CactusMat);
            return root;
        }

        static GameObject BuildRocks()
        {
            GameObject root = new GameObject("Rocks");
            Transform t = root.transform;
            MeshPart(t, "R1", MeshFactory.Rock(3), V(0f, 0.6f, 0f), V(3.2f, 2.6f, 3.0f), RockMat);
            MeshPart(t, "R2", MeshFactory.Rock(4), V(2.6f, 0.4f, 1.2f), V(2.0f, 1.7f, 2.0f), RockDark);
            MeshPart(t, "R3", MeshFactory.Rock(5), V(-2.3f, 0.3f, -1.1f), V(1.6f, 1.3f, 1.7f), RockMat);
            MeshPart(t, "R4", MeshFactory.Rock(6), V(0.8f, 0.2f, -2.2f), V(1.1f, 0.9f, 1.2f), RockDark);
            return root;
        }

        static GameObject BuildRuins()
        {
            GameObject root = new GameObject("Ruins");
            Transform t = root.transform;
            float[] hs = { 5.0f, 3.2f, 4.2f, 1.8f };
            float[] xs = { -3.5f, -1.2f, 2.5f, 4.6f };
            float[] zs = { 0f, 3.5f, 0.5f, 4.0f };
            for (int i = 0; i < hs.Length; i++)
            {
                Part(t, "ColBase" + i, PrimitiveType.Cube, V(xs[i], 0.2f, zs[i]), V(1.3f, 0.4f, 1.3f), StoneDark);
                Part(t, "Col" + i, PrimitiveType.Cylinder, V(xs[i], 0.4f + hs[i] * 0.5f, zs[i]), V(0.9f, hs[i] * 0.5f, 0.9f), Stone);
            }
            Part(t, "Lintel", PrimitiveType.Cube, V(-2.35f, 5.15f, 0f), V(3.2f, 0.5f, 1.0f), Stone, V(0f, 0f, 4f));
            Part(t, "Wall1", PrimitiveType.Cube, V(0f, 1.2f, -3.0f), V(5.0f, 2.4f, 0.7f), StoneDark);
            Part(t, "Wall2", PrimitiveType.Cube, V(3.6f, 0.8f, -3.0f), V(2.2f, 1.6f, 0.7f), Stone);
            for (int i = 0; i < 6; i++)
                Part(t, "Rubble" + i, PrimitiveType.Cube, V(-4f + i * 1.6f, 0.2f, 2.0f - (i % 3)), V(0.7f, 0.4f, 0.6f), StoneDark, V(0f, i * 31f, 0f));
            return root;
        }

        static GameObject BuildHut()
        {
            GameObject root = new GameObject("Hut");
            Transform t = root.transform;
            Part(t, "Walls", PrimitiveType.Cube, V(0f, 1.1f, 0f), V(4.0f, 2.2f, 4.0f), Wood);
            MeshPart(t, "Roof", MeshFactory.Cone(4), V(0f, 2.2f, 0f), V(6.6f, 2.2f, 6.6f), Thatch, V(0f, 45f, 0f));
            Part(t, "Door", PrimitiveType.Cube, V(0f, 0.85f, -2.02f), V(1.0f, 1.7f, 0.06f), WoodDark);
            Part(t, "Window", PrimitiveType.Cube, V(1.9f, 1.4f, -2.02f), V(0.7f, 0.6f, 0.06f), Dark);
            Part(t, "Plank", PrimitiveType.Cube, V(-2.3f, 1.0f, -1.0f), V(0.2f, 2.0f, 0.1f), WoodDark, V(0f, 0f, 15f));
            Part(t, "Barrel", PrimitiveType.Cylinder, V(-2.6f, 0.5f, -2.3f), V(0.9f, 0.5f, 0.9f), WoodDark);
            return root;
        }

        static GameObject BuildTemple()
        {
            GameObject root = new GameObject("Temple");
            Transform t = root.transform;
            Part(t, "Tier1", PrimitiveType.Cube, V(0f, 0.6f, 0f), V(14f, 1.2f, 14f), Stone);
            Part(t, "Tier2", PrimitiveType.Cube, V(0f, 1.8f, 0f), V(10.5f, 1.2f, 10.5f), StoneDark);
            Part(t, "Tier3", PrimitiveType.Cube, V(0f, 3.0f, 0f), V(7f, 1.2f, 7f), Stone);
            Part(t, "Shrine", PrimitiveType.Cube, V(0f, 4.8f, 0f), V(4f, 2.4f, 4f), StoneDark);
            MeshPart(t, "Cap", MeshFactory.Cone(4), V(0f, 6.0f, 0f), V(6f, 2.4f, 6f), Stone, V(0f, 45f, 0f));
            for (int i = 0; i < 4; i++)
                Part(t, "Pillar" + i, PrimitiveType.Cylinder, V(-4.8f + i * 3.2f, 3.2f, -8.0f), V(1.0f, 2.2f, 1.0f), Stone);
            Part(t, "PillarBeam", PrimitiveType.Cube, V(0f, 5.5f, -8.0f), V(11.5f, 0.6f, 1.4f), StoneDark);
            return root;
        }

        static GameObject BuildBridge()
        {
            // Decorative wooden bridge spanning high above the track (no colliders).
            GameObject root = new GameObject("Bridge");
            Transform t = root.transform;
            Part(t, "Deck", PrimitiveType.Cube, V(0f, 7.3f, 0f), V(20f, 0.4f, 3.4f), Wood);
            Part(t, "RailF", PrimitiveType.Cube, V(0f, 8.1f, -1.6f), V(20f, 0.12f, 0.12f), WoodDark);
            Part(t, "RailB", PrimitiveType.Cube, V(0f, 8.1f, 1.6f), V(20f, 0.12f, 0.12f), WoodDark);
            for (int i = 0; i < 7; i++)
            {
                float x = -9f + i * 3f;
                Part(t, "BalF" + i, PrimitiveType.Cube, V(x, 7.8f, -1.6f), V(0.1f, 0.8f, 0.1f), WoodDark);
                Part(t, "BalB" + i, PrimitiveType.Cube, V(x, 7.8f, 1.6f), V(0.1f, 0.8f, 0.1f), WoodDark);
            }
            float[] px = { -8.5f, 8.5f };
            for (int i = 0; i < 2; i++)
            {
                Part(t, "PostA" + i, PrimitiveType.Cube, V(px[i], 3.5f, -1.2f), V(0.7f, 7.2f, 0.7f), WoodDark);
                Part(t, "PostB" + i, PrimitiveType.Cube, V(px[i], 3.5f, 1.2f), V(0.7f, 7.2f, 0.7f), WoodDark);
                Part(t, "Brace" + i, PrimitiveType.Cube, V(px[i], 5.0f, 0f), V(0.4f, 0.4f, 3.0f), Wood);
            }
            return root;
        }

        // ------------------------------------------------------------------ FX
        static GameObject BuildBurst(string name, int count, float speedMin, float speedMax, float sizeMin, float sizeMax, float life, float gravity)
        {
            GameObject go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.25f;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = MaterialFactory.ParticleMat();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            AutoRelease ar = go.AddComponent<AutoRelease>();
            ar.Life = life + 0.3f;
            return go;
        }
    }
}
