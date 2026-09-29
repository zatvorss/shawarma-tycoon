using UnityEngine;

namespace ValourGames.Shawarma
{
    public enum Sound { Click, Coin, Error, Chop, Slice, Done, Spawn, Burn, Combo, Pour, Wrap, Fanfare, Whoosh, Tick, Ding }

    /// <summary>
    /// Звуки и музыка, синтезированные кодом (ассеты не нужны).
    /// Позже можно заменить на нормальные .ogg — поменяй только этот файл.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx _inst;
        AudioSource _src, _music;
        AudioClip[] _clips;

        public static void Init(GameObject host)
        {
            _inst = host.AddComponent<Sfx>();
            _inst._src = host.AddComponent<AudioSource>();
            _inst._src.playOnAwake = false;
            _inst._music = host.AddComponent<AudioSource>();
            _inst._music.playOnAwake = false;
            _inst._music.loop = true;
            _inst._music.volume = 0.3f;

            _inst._clips = new AudioClip[]
            {
                Tone("click", new float[] { 900f }, 0.04f, 0.22f, 0),
                Tone("coin",  new float[] { 988f, 1319f }, 0.07f, 0.28f, 1),
                Tone("error", new float[] { 220f, 175f }, 0.11f, 0.25f, 1),
                Noise("chop", 0.07f, 0.35f, 30f),
                Noise("slice", 0.16f, 0.2f, 8f),
                Tone("done",  new float[] { 660f, 880f }, 0.07f, 0.22f, 0),
                Tone("spawn", new float[] { 523f, 659f, 784f }, 0.05f, 0.14f, 2),
                Noise("burn", 0.4f, 0.25f, 4f),
                Tone("combo", new float[] { 784f, 988f, 1175f, 1568f }, 0.05f, 0.2f, 1),
                Noise("pour", 0.12f, 0.1f, 12f),
                Tone("wrap",  new float[] { 330f, 440f }, 0.05f, 0.2f, 2),
                Tone("fanfare", new float[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.1f, 0.25f, 1),
                Noise("whoosh", 0.9f, 0.35f, 1.5f),
                Tone("tick", new float[] { 1400f }, 0.03f, 0.15f, 0),
                Bell("ding", 1760f, 0.9f, 0.3f),
            };
            _inst._music.clip = Music();
        }

        public static void Play(Sound s)
        {
            if (_inst == null || _inst._src == null) return;
            _inst._src.PlayOneShot(_inst._clips[(int)s]);
        }

        public static void SetSound(bool on)
        {
            if (_inst != null) _inst._src.mute = !on;
        }

        public static void SetMusic(bool on)
        {
            if (_inst == null) return;
            _inst._music.mute = !on;
            if (!_inst._music.isPlaying) _inst._music.Play();
        }

        // ---------------------------------------------------------------------
        // wave: 0 синус, 1 квадрат, 2 треугольник
        static float Wave(int wave, float phase)
        {
            phase -= Mathf.Floor(phase);
            switch (wave)
            {
                case 1: return phase < 0.5f ? 0.5f : -0.5f;
                case 2: return 4f * Mathf.Abs(phase - 0.5f) - 1f;
                default: return Mathf.Sin(phase * 2f * Mathf.PI);
            }
        }

        static AudioClip Tone(string name, float[] freqs, float segDur, float vol, int wave)
        {
            const int rate = 44100;
            int seg = (int)(rate * segDur);
            int n = seg * freqs.Length;
            var data = new float[n];
            for (int f = 0; f < freqs.Length; f++)
            {
                for (int i = 0; i < seg; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Min(1f, i / 150f) * (1f - (float)i / seg);
                    data[f * seg + i] = Wave(wave, t * freqs[f]) * env * vol;
                }
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Звоночек «бзыньк»: как у колокольчика на кухне — основной тон и звенящие обертоны.</summary>
        static AudioClip Bell(string name, float freq, float dur, float vol)
        {
            const int rate = 44100;
            int n = (int)(rate * dur);
            var data = new float[n];
            float[] mul = { 1f, 2.76f, 5.4f, 8.93f };
            float[] amp = { 1f, 0.5f, 0.25f, 0.12f };
            float[] dec = { 4f, 6f, 9f, 13f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float v = 0f;
                for (int k = 0; k < mul.Length; k++)
                    v += Mathf.Sin(2f * Mathf.PI * freq * mul[k] * t) * amp[k] * Mathf.Exp(-dec[k] * t);
                data[i] = v * vol * Mathf.Min(1f, i / 40f) * 0.55f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float dur, float vol, float decay)
        {
            const int rate = 44100;
            int n = (int)(rate * dur);
            var data = new float[n];
            var rnd = new System.Random(name.GetHashCode());
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Exp(-decay * t * dur * 10f) * Mathf.Min(1f, i / 80f);
                float x = (float)rnd.NextDouble() * 2f - 1f;
                prev = prev * 0.6f + x * 0.4f; // чуть мягче
                data[i] = prev * env * vol * 2f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------------------------------------------------------------
        //  Музыка: весёлый луп 8 тактов (C - Am - F - G)
        // ---------------------------------------------------------------------
        static AudioClip Music()
        {
            const int rate = 22050;
            const float bpm = 112f;
            float beat = 60f / bpm;
            int bars = 8;
            float len = bars * 4 * beat;
            int n = (int)(len * rate);
            var data = new float[n];

            int[] roots = { 0, 9, 5, 7, 0, 9, 5, 7 };
            bool[] minor = { false, true, false, false, false, true, false, false };
            int[] penta = { 0, 2, 4, 7, 9, 12, 14, 16 };
            var rnd = new System.Random(7);

            for (int b = 0; b < bars; b++)
            {
                int r = roots[b];
                int third = minor[b] ? 3 : 4;
                for (int k = 0; k < 4; k++)
                {
                    float t0 = (b * 4 + k) * beat;
                    // бас
                    AddNote(data, rate, t0, beat * 0.8f, Semi(r, 65.41f) * (k % 2 == 1 ? 2f : 1f), 0.22f, 2);
                    // аккорд на слабую долю
                    float ts = t0 + beat * 0.5f;
                    AddNote(data, rate, ts, beat * 0.22f, Semi(r, 261.63f), 0.05f, 1);
                    AddNote(data, rate, ts, beat * 0.22f, Semi(r + third, 261.63f), 0.05f, 1);
                    AddNote(data, rate, ts, beat * 0.22f, Semi(r + 7, 261.63f), 0.05f, 1);
                    // "хай-хэт"
                    AddHat(data, rate, ts, 0.03f, 0.04f, rnd);
                }
                // мелодия восьмыми
                for (int e = 0; e < 8; e++)
                {
                    if (rnd.NextDouble() < 0.35) continue;
                    float t0 = (b * 4) * beat + e * beat * 0.5f;
                    int note = penta[rnd.Next(penta.Length)];
                    AddNote(data, rate, t0, beat * 0.42f, Semi(note, 523.25f), 0.07f, 2);
                }
            }

            for (int i = 0; i < n; i++)
            {
                float x = data[i];
                data[i] = x / (1f + Mathf.Abs(x)); // мягкий лимитер
            }

            var clip = AudioClip.Create("music", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Semi(float semis, float baseHz) { return baseHz * Mathf.Pow(2f, semis / 12f); }

        static void AddNote(float[] data, int rate, float t0, float dur, float freq, float vol, int wave)
        {
            int s0 = (int)(t0 * rate);
            int sn = (int)(dur * rate);
            for (int i = 0; i < sn; i++)
            {
                int idx = s0 + i;
                if (idx >= data.Length) break;
                float t = (float)i / rate;
                float env = Mathf.Min(1f, i / 100f) * Mathf.Exp(-3f * t / dur);
                data[idx] += Wave(wave, t * freq) * env * vol;
            }
        }

        static void AddHat(float[] data, int rate, float t0, float dur, float vol, System.Random rnd)
        {
            int s0 = (int)(t0 * rate);
            int sn = (int)(dur * rate);
            for (int i = 0; i < sn; i++)
            {
                int idx = s0 + i;
                if (idx >= data.Length) break;
                float env = 1f - (float)i / sn;
                data[idx] += ((float)rnd.NextDouble() * 2f - 1f) * env * vol;
            }
        }
    }
}
