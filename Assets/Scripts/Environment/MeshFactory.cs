using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    /// <summary>Procedural meshes (cones, rocks) and cached Unity primitive meshes.</summary>
    public static class MeshFactory
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        public static void DestroySafe(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        public static Mesh Primitive(PrimitiveType type)
        {
            string key = "prim_" + type;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            GameObject go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            DestroySafe(go);
            cache[key] = m;
            return m;
        }

        /// <summary>Flat-shaded cone, base radius 0.5 at y=0, height 1.</summary>
        public static Mesh Cone(int sides)
        {
            string key = "cone_" + sides;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            Vector3[] verts = new Vector3[sides * 3];
            Vector2[] uvs = new Vector2[sides * 3];
            int[] tris = new int[sides * 3];
            Vector3 apex = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (float)i / sides * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int b = i * 3;
                verts[b] = p0; verts[b + 1] = apex; verts[b + 2] = p1;
                uvs[b] = new Vector2(0f, 0f); uvs[b + 1] = new Vector2(0.5f, 1f); uvs[b + 2] = new Vector2(1f, 0f);
                tris[b] = b; tris[b + 1] = b + 1; tris[b + 2] = b + 2;
            }
            m = new Mesh();
            m.name = key;
            m.vertices = verts; m.uv = uvs; m.triangles = tris;
            m.RecalculateNormals(); m.RecalculateBounds();
            m.hideFlags = HideFlags.DontSave;
            cache[key] = m;
            return m;
        }

        /// <summary>Lumpy rock built from a displaced sphere. Bottom is flattened at y=-0.25.</summary>
        public static Mesh Rock(int seed)
        {
            string key = "rock_" + seed;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            Mesh src = Primitive(PrimitiveType.Sphere);
            Vector3[] vs = src.vertices;
            Vector3[] nv = new Vector3[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                Vector3 d = vs[i].normalized;
                float n = Mathf.PerlinNoise(d.x * 1.8f + 10f + seed * 5.3f, d.y * 1.8f + d.z * 1.6f + 10f + seed * 2.9f);
                float n2 = Mathf.PerlinNoise(d.z * 4f + 20f + seed, d.x * 4f + d.y * 3f + 20f);
                float r = 0.5f * (0.72f + 0.45f * n + 0.12f * n2);
                Vector3 v = d * r;
                if (v.y < -0.25f) v.y = -0.25f;
                nv[i] = v;
            }
            m = new Mesh();
            m.name = key;
            m.vertices = nv;
            m.triangles = src.triangles;
            m.uv = src.uv;
            m.RecalculateNormals(); m.RecalculateBounds();
            m.hideFlags = HideFlags.DontSave;
            cache[key] = m;
            return m;
        }
    }
}
