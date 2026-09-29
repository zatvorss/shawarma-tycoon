using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// "Сочность": пружинки, тряска, летящие монетки, всплывающий текст, частицы.
    /// </summary>
    public class Juice : MonoBehaviour
    {
        static Juice _inst;
        static Juice I
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("Juice");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<Juice>();
                }
                return _inst;
            }
        }

        readonly HashSet<Transform> _shaking = new HashSet<Transform>();

        // ---------- пружинка ----------
        public static void Punch(Transform t, float amount = 0.15f, float dur = 0.25f)
        {
            if (t == null) return;
            I.StartCoroutine(I.PunchCo(t, amount, dur));
        }

        IEnumerator PunchCo(Transform t, float a, float d)
        {
            float time = 0f;
            while (time < d)
            {
                if (t == null) yield break;
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / d);
                float s = 1f + a * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        // ---------- тряска ----------
        public static void Shake(RectTransform t, float mag = 10f, float dur = 0.25f)
        {
            if (t == null || I._shaking.Contains(t)) return;
            I.StartCoroutine(I.ShakeCo(t, mag, dur));
        }

        IEnumerator ShakeCo(RectTransform t, float mag, float dur)
        {
            _shaking.Add(t);
            Vector2 basePos = t.anchoredPosition;
            float time = 0f;
            while (time < dur)
            {
                if (t == null) { _shaking.Remove(t); yield break; }
                time += Time.unscaledDeltaTime;
                float k = 1f - time / dur;
                t.anchoredPosition = basePos + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * mag * k;
                yield return null;
            }
            if (t != null) t.anchoredPosition = basePos;
            _shaking.Remove(t);
        }

        // ---------- полёт (монетки и т.п.) ----------
        /// <summary>Перемещает объект из точки from в точку to (мировые координаты), по дуге.</summary>
        public static void Fly(RectTransform item, Vector3 from, Vector3 to, float dur, float delay, Action onDone)
        {
            I.StartCoroutine(I.FlyCo(item, from, to, dur, delay, onDone));
        }

        IEnumerator FlyCo(RectTransform item, Vector3 from, Vector3 to, float dur, float delay, Action onDone)
        {
            if (item == null) yield break;
            item.position = from;
            item.gameObject.SetActive(false);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (item == null) yield break;
            item.gameObject.SetActive(true);
            Vector3 mid = (from + to) * 0.5f + new Vector3(0, Mathf.Abs(to.x - from.x) * 0.25f + 60f, 0);
            float time = 0f;
            while (time < dur)
            {
                if (item == null) yield break;
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / dur);
                k = k * k * (3f - 2f * k);
                Vector3 a = Vector3.Lerp(from, mid, k);
                Vector3 b = Vector3.Lerp(mid, to, k);
                item.position = Vector3.Lerp(a, b, k);
                yield return null;
            }
            if (item != null) Destroy(item.gameObject);
            if (onDone != null) onDone();
        }

        // ---------- всплывающий текст ----------
        public static void FloatText(Transform parent, string text, Color color, int size, float ax, float ay)
        {
            var t = UIKit.Label(parent, text, size, color, TextAnchor.MiddleCenter, ax - 0.5f, ay - 0.1f, ax + 0.5f, ay + 0.1f, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.AddOutline(t, new Color(0.25f, 0.12f, 0f, 0.9f), 3f);
            I.StartCoroutine(I.FloatCo(t));
        }

        IEnumerator FloatCo(Text t)
        {
            float time = 0f;
            var rt = t.rectTransform;
            while (time < 1.1f)
            {
                if (t == null) yield break;
                time += Time.unscaledDeltaTime;
                rt.anchoredPosition = new Vector2(0, time * 110f);
                float s = time < 0.15f ? Mathf.Lerp(0.5f, 1.15f, time / 0.15f) : 1f;
                rt.localScale = new Vector3(s, s, 1f);
                var c = t.color;
                c.a = Mathf.Clamp01(1.4f - time * 1.3f);
                t.color = c;
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // ---------- частицы (кусочки при нарезке и т.п.) ----------
        public static void Burst(Transform parent, Color color, int count, float ax, float ay, float size = 16f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = UIKit.Shape(parent, UIKit.RoundedSmall, color, ax, ay, size, size * 0.7f,
                    UnityEngine.Random.Range(0f, 360f));
                var v = new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(150f, 420f));
                I.StartCoroutine(I.ParticleCo(p.rectTransform, v));
            }
        }

        IEnumerator ParticleCo(RectTransform p, Vector2 v)
        {
            float time = 0f;
            float spin = UnityEngine.Random.Range(-400f, 400f);
            var img = p.GetComponent<Image>();
            while (time < 0.6f)
            {
                if (p == null) yield break;
                float dt = Time.unscaledDeltaTime;
                time += dt;
                v.y -= 1200f * dt;
                p.anchoredPosition += v * dt;
                p.localRotation = Quaternion.Euler(0, 0, p.localEulerAngles.z + spin * dt);
                var c = img.color;
                c.a = 1f - time / 0.6f;
                img.color = c;
                yield return null;
            }
            if (p != null) Destroy(p.gameObject);
        }
    }
    /// <summary>Покачивание вверх-вниз (стрелки, иконки).</summary>
    public class Bobber : MonoBehaviour
    {
        public float amp = 10f, speed = 3f;
        Vector2 _base;
        bool _init;

        void Update()
        {
            var rt = (RectTransform)transform;
            if (!_init) { _base = rt.anchoredPosition; _init = true; }
            rt.anchoredPosition = _base + new Vector2(0f, Mathf.Sin(Time.unscaledTime * speed) * amp);
        }
    }

    /// <summary>Качание как маятник (кубок, который можно забрать).</summary>
    public class Pendulum : MonoBehaviour
    {
        public float angle = 14f, speed = 3.5f;
        void Update() { transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * speed) * angle); }
    }

    /// <summary>Мигание (лампочки, LED).</summary>
    public class Blinker : MonoBehaviour
    {
        public float speed = 3f;
        Graphic _g;
        void Awake() { _g = GetComponent<Graphic>(); }
        void Update()
        {
            if (_g == null) return;
            var c = _g.color;
            c.a = Mathf.PingPong(Time.unscaledTime * speed, 1f) * 0.7f + 0.3f;
            _g.color = c;
        }
    }

    /// <summary>Движение влево-вправо (робо-губка и т.п.).</summary>
    public class Swayer : MonoBehaviour
    {
        public float amp = 60f, speed = 4f;
        Vector2 _base;
        bool _init;

        void Update()
        {
            var rt = (RectTransform)transform;
            if (!_init) { _base = rt.anchoredPosition; _init = true; }
            rt.anchoredPosition = _base + new Vector2(Mathf.Sin(Time.unscaledTime * speed) * amp, 0f);
        }
    }

    /// <summary>Вращение.</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 90f;
        void Update() { transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime); }
    }
}
