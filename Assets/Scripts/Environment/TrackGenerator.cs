using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    /// <summary>
    /// Endless procedural track. A ring of segments is recycled in front of the player; each segment
    /// places pooled scenery, obstacles, coins and power-ups. Every row is guaranteed solvable:
    /// one "safe" lane (adjacent to the previous row's safe lane) never contains a tall obstacle.
    /// </summary>
    public class TrackGenerator : MonoBehaviour
    {
        public const float SegmentLength = 42f;
        public const int SegmentCount = 9;
        public const float PathWidth = 10.4f;
        const float BehindMargin = 20f;

        class Segment
        {
            public Transform Root;
            public float StartZ;
            public readonly List<PooledObject> Items = new List<PooledObject>();
        }

        Transform target;
        bool menuMode;
        Transform itemsRoot;
        readonly List<Segment> segments = new List<Segment>();
        float nextZ;
        int lastSafe = 1;
        int sinceBridge = 10;

        public void Init(Transform playerTarget, bool menu)
        {
            target = playerTarget;
            menuMode = menu;
            itemsRoot = new GameObject("TrackItems").transform;
            itemsRoot.SetParent(transform, false);
            nextZ = -SegmentLength;
            for (int i = 0; i < SegmentCount; i++)
            {
                Segment s = CreateSegment();
                s.StartZ = nextZ;
                nextZ += SegmentLength;
                Position(s);
                Populate(s);
                segments.Add(s);
            }
        }

        void Update()
        {
            if (target == null || segments.Count == 0) return;
            while (segments[0].StartZ + SegmentLength + BehindMargin < target.position.z)
            {
                Segment s = segments[0];
                segments.RemoveAt(0);
                for (int i = 0; i < s.Items.Count; i++) if (s.Items[i] != null) s.Items[i].Release();
                s.Items.Clear();
                s.StartZ = nextZ;
                nextZ += SegmentLength;
                Position(s);
                Populate(s);
                segments.Add(s);
            }
        }

        // ------------------------------------------------------------ segment construction
        static void Position(Segment s)
        {
            s.Root.position = new Vector3(0f, 0f, s.StartZ + SegmentLength * 0.5f);
        }

        Segment CreateSegment()
        {
            Segment s = new Segment();
            GameObject root = new GameObject("Segment");
            root.transform.SetParent(transform, false);
            s.Root = root.transform;

            // Path surface (quad facing up; u across, v along the track)
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Quad);
            path.name = "Path";
            MeshFactory.DestroySafe(path.GetComponent<Collider>());
            path.transform.SetParent(s.Root, false);
            path.transform.localPosition = Vector3.zero;
            path.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            path.transform.localScale = new Vector3(PathWidth, SegmentLength, 1f);
            MeshRenderer pr = path.GetComponent<MeshRenderer>();
            pr.sharedMaterial = MaterialFactory.PathSand;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Material curb = MaterialFactory.Solid(new Color(0.55f, 0.46f, 0.34f));
            Material pole = MaterialFactory.Solid(new Color(0.36f, 0.23f, 0.12f));
            Material flag = MaterialFactory.Solid(new Color(0.78f, 0.22f, 0.15f));
            for (int side = -1; side <= 1; side += 2)
            {
                AddBox(s.Root, "Curb", new Vector3(side * 5.45f, 0.15f, 0f), new Vector3(0.5f, 0.3f, SegmentLength), curb);
                for (int k = 0; k < 2; k++)
                {
                    float z = -SegmentLength * 0.5f + 10f + k * 21f;
                    AddBox(s.Root, "Pole", new Vector3(side * 5.8f, 1.6f, z), new Vector3(0.15f, 3.2f, 0.15f), pole);
                    AddBox(s.Root, "Flag", new Vector3(side * 5.35f, 2.85f, z), new Vector3(0.8f, 0.55f, 0.04f), flag);
                }
            }
            return s;
        }

        static void AddBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Primitive(PrimitiveType.Cube);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        // ------------------------------------------------------------ population
        void Populate(Segment s)
        {
            PopulateScenery(s);
            if (!menuMode) PopulateGameplay(s);
        }

        GameObject Spawn(Segment s, string key, Vector3 pos, Vector3 scale, float yaw)
        {
            GameObject go = PoolManager.Get(key, pos, Quaternion.Euler(0f, yaw, 0f), itemsRoot);
            if (go == null) return null;
            go.transform.localScale = scale;
            PooledObject po = go.GetComponent<PooledObject>();
            if (po != null) s.Items.Add(po);
            return go;
        }

        static string PickScenery()
        {
            float r = Random.value * 103f;
            if (r < 30f) return "prop_palm";
            if (r < 42f) return "prop_drytree";
            if (r < 56f) return "prop_cactus";
            if (r < 72f) return "prop_rocks";
            if (r < 80f) return "prop_ruins";
            if (r < 85f) return "prop_hut";
            if (r < 88f) return "prop_temple";
            return Random.value < 0.5f ? "prop_palm" : "prop_cactus";
        }

        void PopulateScenery(Segment s)
        {
            float z0 = s.StartZ, z1 = s.StartZ + SegmentLength;
            for (int side = -1; side <= 1; side += 2)
            {
                // low sand mounds near the path
                if (Random.value < 0.7f)
                {
                    float sy = Random.Range(1.6f, 2.8f);
                    Spawn(s, "prop_dune", new Vector3(side * Random.Range(9f, 22f), -0.25f * sy, Random.Range(z0, z1)),
                        new Vector3(Random.Range(8f, 14f), sy, Random.Range(10f, 20f)), 0f);
                }
                // big dunes further out (inner edge always clear of the path and props)
                float sx = Random.Range(36f, 56f);
                float dsy = Random.Range(10f, 18f);
                Spawn(s, "prop_dune", new Vector3(side * (28f + sx * 0.46f + Random.Range(0f, 10f)), -0.2f * dsy, Random.Range(z0, z1)),
                    new Vector3(sx, dsy, Random.Range(40f, 60f)), 0f);
                if (Random.value < 0.8f)
                {
                    float fsx = Random.Range(60f, 90f);
                    float fsy = Random.Range(22f, 34f);
                    Spawn(s, "prop_dune", new Vector3(side * (60f + fsx * 0.46f + Random.Range(0f, 15f)), -0.2f * fsy, Random.Range(z0, z1)),
                        new Vector3(fsx, fsy, Random.Range(50f, 80f)), 0f);
                }
                // mountains
                if (Random.value < 0.45f)
                {
                    float m = Random.Range(60f, 100f);
                    Spawn(s, "prop_mountain", new Vector3(side * Random.Range(95f, 140f), 0f, Random.Range(z0, z1)),
                        new Vector3(m, m * Random.Range(0.5f, 0.75f), m), Random.Range(0f, 360f));
                }
                // props
                int n = 2 + (Random.value < 0.5f ? 1 : 0);
                for (int i = 0; i < n; i++)
                {
                    string key = PickScenery();
                    bool big = key == "prop_ruins" || key == "prop_hut" || key == "prop_temple";
                    float x = side * (big ? Random.Range(13f, 28f) : Random.Range(7.5f, 22f));
                    float sc = key == "prop_temple" ? Random.Range(0.9f, 1.2f) : Random.Range(0.85f, 1.25f);
                    Spawn(s, key, new Vector3(x, 0f, Random.Range(z0 + 3f, z1 - 3f)), Vector3.one * sc, Random.Range(0f, 360f));
                }
            }
            sinceBridge++;
            if (sinceBridge > 5 && Random.value < 0.12f)
            {
                sinceBridge = 0;
                Spawn(s, "prop_bridge", new Vector3(0f, 0f, s.StartZ + SegmentLength * 0.5f), Vector3.one, 0f);
            }
        }

        static float LaneX(int lane) { return (lane - 1) * PropFactory.LaneWidth; }
        static string RandomLow() { return PropFactory.LowObstacleKeys[Random.Range(0, PropFactory.LowObstacleKeys.Length)]; }
        static string RandomTall() { return PropFactory.TallObstacleKeys[Random.Range(0, PropFactory.TallObstacleKeys.Length)]; }

        void SpawnObstacle(Segment s, string key, int lane, float z)
        {
            Spawn(s, key, new Vector3(LaneX(lane), 0f, z), Vector3.one, 0f);
        }

        void CoinLine(Segment s, int lane, float z, int count, float spacing)
        {
            for (int i = 0; i < count; i++)
                Spawn(s, "coin", new Vector3(LaneX(lane), 1.0f, z + i * spacing), Vector3.one, 0f);
        }

        void CoinArc(Segment s, int lane, float zCenter)
        {
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                Spawn(s, "coin", new Vector3(LaneX(lane), 1.0f + Mathf.Sin(t * Mathf.PI) * 1.0f, zCenter - 4.5f + t * 9f), Vector3.one, 0f);
            }
        }

        static string PickPowerUp()
        {
            float r = Random.value;
            if (r < 0.25f) return "pu_bonus";
            if (r < 0.50f) return "pu_magnet";
            if (r < 0.68f) return "pu_shield";
            if (r < 0.84f) return "pu_multiplier";
            return "pu_speed";
        }

        void PopulateGameplay(Segment s)
        {
            float d = Mathf.Clamp01(s.StartZ / 2500f);
            bool safeZone = s.StartZ < 90f;
            for (int r = 0; r < 3; r++)
            {
                float zr = s.StartZ + 7f + r * 14f;
                int safe = Mathf.Clamp(lastSafe + Random.Range(-1, 2), 0, 2);
                lastSafe = safe;
                bool arc = false;
                bool rowHasObstacles = !safeZone && Random.value < Mathf.Lerp(0.6f, 0.95f, d);
                if (rowHasObstacles)
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        if (lane == safe)
                        {
                            float roll = Random.value;
                            if (roll < 0.12f + 0.25f * d)
                            {
                                SpawnObstacle(s, RandomLow(), lane, zr);
                                CoinArc(s, lane, zr);
                                arc = true;
                            }
                            else if (d > 0.04f && roll < 0.22f + 0.33f * d)
                            {
                                SpawnObstacle(s, PropFactory.GateKey, lane, zr);
                            }
                        }
                        else if (Random.value < Mathf.Lerp(0.55f, 0.9f, d))
                        {
                            bool tall = Random.value < Mathf.Lerp(0.15f, 0.6f, d);
                            SpawnObstacle(s, tall ? RandomTall() : RandomLow(), lane, zr);
                        }
                    }
                }
                if (Random.value < 0.10f)
                    Spawn(s, PickPowerUp(), new Vector3(LaneX(safe), 1.2f, zr + 7f), Vector3.one, 0f);
                else if (Random.value < 0.85f)
                {
                    if (arc) CoinLine(s, safe, zr + 6.5f, 3, 2f);
                    else CoinLine(s, safe, zr + 3.5f, 5, 2f);
                }
            }
        }
    }
}
