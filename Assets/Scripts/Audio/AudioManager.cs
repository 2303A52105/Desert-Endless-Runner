using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    public enum Sfx { Coin, Jump, Slide, Land, Hit, PowerUp, Click, GameOver, Revive, ShieldBreak }

    /// <summary>All audio is synthesised procedurally at runtime (no audio files required).</summary>
    public class AudioManager : MonoBehaviour
    {
        const int Rate = 22050;
        const float TwoPi = Mathf.PI * 2f;
        const float MusicVolume = 0.38f;

        public static AudioManager Instance { get; private set; }

        static readonly Dictionary<Sfx, AudioClip> sfxClips = new Dictionary<Sfx, AudioClip>();
        static AudioClip musicClip, windClip;

        AudioSource music, wind, sfx;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = GetMusic(); music.loop = true; music.volume = MusicVolume; music.playOnAwake = false;
            music.Play();
            wind = gameObject.AddComponent<AudioSource>();
            wind.clip = GetWind(); wind.loop = true; wind.volume = 0.12f; wind.playOnAwake = false;
            wind.Play();
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            SaveData.ApplyAudio();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Play(Sfx s, float volume = 1f)
        {
            if (Instance == null || Instance.sfx == null) return;
            AudioClip c = GetClip(s);
            if (c != null) Instance.sfx.PlayOneShot(c, volume);
        }

        public static void SetMusicVolume(float v)
        {
            if (Instance != null && Instance.music != null) Instance.music.volume = v;
        }

        // ------------------------------------------------------------------ synthesis
        static AudioClip Make(string name, float dur, System.Func<float, float, float> gen)
        {
            int n = (int)(dur * Rate);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Mathf.Clamp(gen(t, t / dur), -1f, 1f);
            }
            AudioClip c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            c.hideFlags = HideFlags.DontSave;
            return c;
        }

        static float Env(float t, float decay) { return (1f - Mathf.Exp(-t * 600f)) * Mathf.Exp(-t * decay); }

        public static AudioClip GetClip(Sfx s)
        {
            AudioClip c;
            if (sfxClips.TryGetValue(s, out c) && c != null) return c;
            c = BuildSfx(s);
            sfxClips[s] = c;
            return c;
        }

        static AudioClip BuildSfx(Sfx s)
        {
            switch (s)
            {
                case Sfx.Coin:
                    return Make("coin", 0.28f, (t, p) =>
                    {
                        float f = t < 0.07f ? 988f : 1319f;
                        return Mathf.Sin(TwoPi * f * t) * 0.45f * Env(t, 11f);
                    });
                case Sfx.Jump:
                    return Make("jump", 0.25f, (t, p) =>
                    {
                        float ph = TwoPi * (300f * t + 400f * t * t / (2f * 0.25f));
                        return Mathf.Sin(ph) * 0.4f * Env(t, 9f);
                    });
                case Sfx.Land:
                    return Make("land", 0.12f, (t, p) => Mathf.Sin(TwoPi * 90f * t) * 0.5f * Env(t, 30f));
                case Sfx.Slide:
                {
                    float lp = 0f;
                    return Make("slide", 0.45f, (t, p) =>
                    {
                        float nz = Random.value * 2f - 1f;
                        lp += (nz - lp) * 0.2f;
                        return lp * Mathf.Sin(Mathf.PI * p) * 0.9f;
                    });
                }
                case Sfx.Hit:
                {
                    float lp = 0f;
                    return Make("hit", 0.55f, (t, p) =>
                    {
                        float nz = Random.value * 2f - 1f;
                        lp += (nz - lp) * 0.12f;
                        return lp * Mathf.Exp(-t * 7f) * 1.4f + Mathf.Sin(TwoPi * 80f * t) * Mathf.Exp(-t * 6f) * 0.6f;
                    });
                }
                case Sfx.ShieldBreak:
                {
                    float lp = 0f;
                    return Make("shield_break", 0.4f, (t, p) =>
                    {
                        float nz = Random.value * 2f - 1f;
                        lp += (nz - lp) * 0.5f;
                        return lp * Mathf.Exp(-t * 10f) * 0.8f + Mathf.Sin(TwoPi * (900f - 1200f * t) * t) * Mathf.Exp(-t * 9f) * 0.3f;
                    });
                }
                case Sfx.PowerUp:
                    return Make("powerup", 0.5f, (t, p) =>
                    {
                        float[] f = { 523.25f, 659.25f, 783.99f, 1046.5f };
                        int i = Mathf.Min(3, (int)(t / 0.09f));
                        float lt = t - i * 0.09f;
                        return (Mathf.Sin(TwoPi * f[i] * t) + 0.3f * Mathf.Sin(TwoPi * f[i] * 2f * t)) * 0.3f * Env(lt, 6f) * (1f - p * 0.5f);
                    });
                case Sfx.Click:
                    return Make("click", 0.06f, (t, p) => Mathf.Sin(TwoPi * 820f * t) * 0.4f * Env(t, 50f));
                case Sfx.GameOver:
                    return Make("gameover", 0.9f, (t, p) =>
                    {
                        float[] f = { 392f, 330f, 262f, 196f };
                        int i = Mathf.Min(3, (int)(t / 0.22f));
                        float lt = t - i * 0.22f;
                        float v = Mathf.Sin(TwoPi * f[i] * t) + 0.5f * Mathf.Sin(TwoPi * f[i] * 0.5f * t);
                        return v * 0.3f * Env(lt, 4f);
                    });
                default: // Revive
                    return Make("revive", 0.6f, (t, p) =>
                    {
                        float ph = TwoPi * (200f * t + 700f * t * t / (2f * 0.6f));
                        return (Mathf.Sin(ph) + 0.4f * Mathf.Sin(ph * 2f)) * 0.3f * Env(t, 2f);
                    });
            }
        }

        public static AudioClip GetWind()
        {
            if (windClip != null) return windClip;
            float lp = 0f;
            windClip = Make("wind", 4f, (t, p) =>
            {
                float nz = Random.value * 2f - 1f;
                lp += (nz - lp) * 0.04f;
                float mod = 0.6f + 0.4f * Mathf.Sin(TwoPi * t / 4f * 2f);
                float fade = Mathf.Clamp01(Mathf.Min(t, 4f - t) / 0.15f);
                return lp * 4f * mod * fade;
            });
            return windClip;
        }

        static void AddNote(float[] buf, float start, float dur, float freq, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = (float)i / Rate;
                float env = (1f - Mathf.Exp(-t * 400f)) * Mathf.Exp(-t * decay);
                float v = Mathf.Sin(TwoPi * freq * t) + 0.5f * Mathf.Sin(TwoPi * freq * 2f * t) * Mathf.Exp(-t * 8f) + 0.25f * Mathf.Sin(TwoPi * freq * 3f * t);
                buf[idx] += v * env * amp;
            }
        }

        static void AddKick(float[] buf, float start, float amp)
        {
            int s0 = (int)(start * Rate);
            int len = (int)(0.18f * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = (float)i / Rate;
                float f = 50f + 90f * Mathf.Exp(-t * 28f);
                buf[idx] += Mathf.Sin(TwoPi * f * t) * Mathf.Exp(-t * 18f) * amp;
            }
        }

        static void AddTak(float[] buf, float start, float amp, System.Random rng)
        {
            int s0 = (int)(start * Rate);
            int len = (int)(0.05f * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = (float)i / Rate;
                buf[idx] += ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 70f) * amp;
            }
        }

        /// <summary>16-second looping desert theme (Phrygian dominant pluck melody, bass drone, darbuka-style percussion).</summary>
        public static AudioClip GetMusic()
        {
            if (musicClip != null) return musicClip;
            float bpm = 120f;
            float beat = 60f / bpm;
            int bars = 8;
            int n = (int)(bars * 4 * beat * Rate);
            float[] buf = new float[n];
            System.Random rng = new System.Random(11);
            int[] scale = { 0, 1, 4, 5, 7, 8, 10, 12, 13, 16 };
            int[] motif = new int[16];
            for (int i = 0; i < 16; i++)
            {
                motif[i] = rng.NextDouble() < 0.62 ? rng.Next(0, 7) : -1;
                if (i % 4 == 0) motif[i] = rng.Next(0, 5);
            }
            motif[0] = 0;
            int steps = bars * 8;
            for (int s = 0; s < steps; s++)
            {
                int m = motif[s % 16];
                if (s >= steps - 8 && s % 2 == 1) m = (m < 0) ? -1 : Mathf.Min(9, m + 2);
                if (m < 0) continue;
                float f = 293.66f * Mathf.Pow(2f, scale[m] / 12f);
                AddNote(buf, s * beat * 0.5f, beat * 0.9f, f, 0.16f, 5.5f);
            }
            float[] roots = { 73.42f, 73.42f, 77.78f, 73.42f, 98f, 73.42f, 82.41f, 73.42f };
            for (int b = 0; b < bars; b++)
            {
                AddNote(buf, b * 4 * beat, beat * 1.8f, roots[b], 0.22f, 2.5f);
                AddNote(buf, b * 4 * beat + 2 * beat, beat * 1.8f, roots[b] * 1.5f, 0.12f, 2.5f);
                AddKick(buf, b * 4 * beat, 0.5f);
                AddKick(buf, b * 4 * beat + 1.5f * beat, 0.35f);
                AddKick(buf, b * 4 * beat + 2f * beat, 0.45f);
                AddTak(buf, b * 4 * beat + 1f * beat, 0.25f, rng);
                AddTak(buf, b * 4 * beat + 3f * beat, 0.25f, rng);
                AddTak(buf, b * 4 * beat + 3.5f * beat, 0.18f, rng);
            }
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float lfo = 0.7f + 0.3f * Mathf.Sin(TwoPi * t / 8f);
                buf[i] += (Mathf.Sin(TwoPi * 146.83f * t) + 0.6f * Mathf.Sin(TwoPi * 220f * t)) * 0.035f * lfo;
            }
            float peak = 0.001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            float g = 0.8f / peak;
            int fade = (int)(0.05f * Rate);
            for (int i = 0; i < n; i++)
            {
                buf[i] *= g;
                if (i < fade) buf[i] *= (float)i / fade;
                if (i > n - fade) buf[i] *= (float)(n - i) / fade;
            }
            musicClip = AudioClip.Create("desert_theme", n, 1, Rate, false);
            musicClip.SetData(buf, 0);
            musicClip.hideFlags = HideFlags.DontSave;
            return musicClip;
        }
    }
}
