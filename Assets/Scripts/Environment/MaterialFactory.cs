using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesertRunner
{
    /// <summary>Creates and caches runtime materials and procedural textures (no external assets needed).</summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); textures.Clear(); font = null; }

        public static Font BuiltinFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return font;
            }
        }

        public static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader s = Shader.Find(names[i]);
                if (s != null) return s;
            }
            return Shader.Find("Sprites/Default");
        }

        static bool TryGet(string key, out Material m) { return cache.TryGetValue(key, out m) && m != null; }

        static Material Store(string key, Material m)
        {
            m.name = key;
            m.hideFlags = HideFlags.DontSave;
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        static void SetPBR(Material m, float metallic, float smooth)
        {
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        }

        static void SetEmission(Material m, Color e)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", e);
        }

        public static Material Solid(Color c, float metallic = 0f, float smooth = 0.08f)
        {
            string key = "solid_" + ColorUtility.ToHtmlStringRGBA(c) + "_" + metallic.ToString("F2") + "_" + smooth.ToString("F2");
            Material m;
            if (TryGet(key, out m)) return m;
            m = new Material(FindShader("Standard", "Legacy Shaders/Diffuse", "Sprites/Default"));
            m.color = c;
            SetPBR(m, metallic, smooth);
            return Store(key, m);
        }

        public static Material Glow(string key, Color c, Color emission, float metallic = 0.2f, float smooth = 0.5f)
        {
            Material m;
            if (TryGet(key, out m)) return m;
            m = new Material(FindShader("Standard", "Legacy Shaders/Diffuse", "Sprites/Default"));
            m.color = c;
            SetPBR(m, metallic, smooth);
            SetEmission(m, emission);
            return Store(key, m);
        }

        public static Material Gold { get { return Glow("gold", new Color(1f, 0.78f, 0.18f), new Color(0.40f, 0.28f, 0.03f), 0.3f, 0.6f); } }
        public static Material GoldLight { get { return Glow("gold_light", new Color(1f, 0.92f, 0.45f), new Color(0.45f, 0.36f, 0.08f), 0.3f, 0.6f); } }

        public static Material Textured(string key, Texture2D tex, Color tint, Vector2 tiling, float smooth = 0.05f)
        {
            Material m;
            if (TryGet(key, out m)) return m;
            m = new Material(FindShader("Standard", "Legacy Shaders/Diffuse", "Sprites/Default"));
            m.color = tint;
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            SetPBR(m, 0f, smooth);
            return Store(key, m);
        }

        public static Material Transparent(string key, Color c, Color emission)
        {
            Material m;
            if (TryGet(key, out m)) return m;
            m = new Material(FindShader("Standard", "Legacy Shaders/Transparent/Diffuse", "Sprites/Default"));
            m.color = c;
            if (m.shader.name == "Standard")
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
                SetPBR(m, 0f, 0.8f);
                SetEmission(m, emission);
            }
            return Store(key, m);
        }

        public static Material SpriteMat(string key, Texture2D tex)
        {
            Material m;
            if (TryGet(key, out m)) return m;
            m = new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = tex;
            m.color = Color.white;
            return Store(key, m);
        }

        public static Material ParticleMat()
        {
            Material m;
            if (TryGet("particle_soft", out m)) return m;
            m = new Material(FindShader("Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Sprites/Default"));
            m.mainTexture = SoftCircle();
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return Store("particle_soft", m);
        }

        // ---------- Named environment materials ----------
        public static Material Sand { get { return Textured("sand", SandTexture(), Color.white, new Vector2(70f, 70f)); } }
        public static Material DuneSand { get { return Textured("dune", SandTexture(), new Color(1f, 0.93f, 0.82f), new Vector2(6f, 4f)); } }
        public static Material PathSand { get { return Textured("path", PathTexture(), Color.white, Vector2.one); } }

        // ---------- Textures ----------
        static Texture2D MakeTexture(string key, int w, int h, bool mips, TextureWrapMode wrap, System.Func<float, float, Color> pixel)
        {
            Texture2D t;
            if (textures.TryGetValue(key, out t) && t != null) return t;
            t = new Texture2D(w, h, TextureFormat.RGBA32, mips);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel((x + 0.5f) / w, (y + 0.5f) / h);
            t.SetPixels(px);
            t.wrapMode = wrap;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 4;
            t.Apply(mips);
            t.name = key;
            t.hideFlags = HideFlags.DontSave;
            textures[key] = t;
            return t;
        }

        static float TileNoise(float u, float v, float f)
        {
            float x = u * f + 100f, y = v * f + 100f;
            float a = Mathf.PerlinNoise(x, y), b = Mathf.PerlinNoise(x - f, y);
            float c = Mathf.PerlinNoise(x, y - f), d = Mathf.PerlinNoise(x - f, y - f);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        public static Texture2D SandTexture()
        {
            return MakeTexture("tex_sand", 256, 256, true, TextureWrapMode.Repeat, (u, v) =>
            {
                float n = 0.6f * TileNoise(u, v, 5f) + 0.4f * TileNoise(u, v, 20f);
                float r = 0.5f + 0.5f * Mathf.Sin((u * 8f + v * 3f + n * 2.5f) * Mathf.PI * 2f);
                float t = Mathf.Clamp01(0.25f + 0.5f * n + 0.25f * r);
                return Color.Lerp(new Color(0.74f, 0.55f, 0.32f), new Color(0.93f, 0.78f, 0.52f), t);
            });
        }

        public static Texture2D PathTexture()
        {
            return MakeTexture("tex_path", 128, 512, true, TextureWrapMode.Clamp, (u, v) =>
            {
                float n = 0.6f * Mathf.PerlinNoise(u * 12f, v * 48f) + 0.4f * Mathf.PerlinNoise(u * 40f + 5f, v * 160f);
                Color c = Color.Lerp(new Color(0.66f, 0.49f, 0.29f), new Color(0.82f, 0.66f, 0.42f), n);
                float du = Mathf.Min(Mathf.Abs(u - 0.385f), Mathf.Abs(u - 0.615f));
                bool dash = ((int)(v * 21f)) % 2 == 0;
                if (du < 0.006f && dash) c = Color.Lerp(c, new Color(0.96f, 0.88f, 0.70f), 0.55f);
                if (u < 0.03f || u > 0.97f) c *= 0.85f;
                c.a = 1f;
                return c;
            });
        }

        public static Texture2D SoftCircle()
        {
            return MakeTexture("tex_soft", 64, 64, false, TextureWrapMode.Clamp, (u, v) =>
            {
                float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                return new Color(1f, 1f, 1f, a);
            });
        }

        public static Texture2D SkyGradient()
        {
            return MakeTexture("tex_sky", 8, 256, false, TextureWrapMode.Clamp, (u, v) =>
            {
                Color horizon = new Color(0.93f, 0.76f, 0.52f);
                Color gold = new Color(0.99f, 0.84f, 0.55f);
                Color mid = new Color(0.72f, 0.78f, 0.80f);
                Color top = new Color(0.45f, 0.66f, 0.85f);
                Color c;
                if (v < 0.14f) c = horizon;
                else if (v < 0.30f) c = Color.Lerp(horizon, gold, (v - 0.14f) / 0.16f);
                else if (v < 0.60f) c = Color.Lerp(gold, mid, (v - 0.30f) / 0.30f);
                else c = Color.Lerp(mid, top, (v - 0.60f) / 0.40f);
                c.a = 1f;
                return c;
            });
        }

        public static Texture2D SunGlow()
        {
            return MakeTexture("tex_sun", 128, 128, false, TextureWrapMode.Clamp, (u, v) =>
            {
                float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
                float core = Mathf.Clamp01((0.22f - d) / 0.04f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.5f) * 0.55f;
                float a = Mathf.Clamp01(Mathf.Max(core, halo));
                return new Color(1f, 0.93f, 0.72f, a);
            });
        }
    }
}
