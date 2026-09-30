using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Экран загрузки: лучи, логотип Valour Games по буквам, крутящаяся шаурма,
    /// полоска загрузки с шутками. Тап — пропустить (после 1 сек).
    /// </summary>
    public class Loading : MonoBehaviour
    {
        const float Duration = 3.6f;

        Action _onDone;
        float _t;
        bool _done;
        readonly List<Text> _letters = new List<Text>();
        RectTransform _rays, _title, _hero, _barFill;
        Text _tip;
        Image _fade;
        int _shown;
        int _tipIndex = -1;

        static readonly string[] TipsRu =
        {
            "Точим ножи...", "Разогреваем гриль...", "Нарезаем помидоры...", "Уговариваем бабушку не передумывать...",
            "Прячемся от санинспектора...", "Крутим вертел...", "Взбиваем чесночный соус..."
        };
        static readonly string[] TipsEn =
        {
            "Sharpening knives...", "Heating the grill...", "Slicing tomatoes...", "Convincing granny not to change her order...",
            "Hiding from the health inspector...", "Spinning the spit...", "Whipping garlic sauce..."
        };

        public static void Show(RectTransform screen, Action onDone)
        {
            var l = screen.gameObject.AddComponent<Loading>();
            l._onDone = onDone;
            l.Build(screen);
        }

        void Build(RectTransform s)
        {
            var bg = UIKit.Full(s, "Bg").gameObject.AddComponent<Image>();
            bg.sprite = UIKit.Gradient(UIKit.Hex("#3A2416"), UIKit.Hex("#120B07"));

            // лучи
            _rays = UIKit.Rect(s, "Rays");
            UIKit.PlaceFixed(_rays, 0.5f, 0.6f, 10f, 10f);
            for (int i = 0; i < 12; i++)
                UIKit.Shape(_rays, null, new Color(1f, 0.75f, 0.3f, 0.07f), 0.5f, 0.5f, 90f, 2400f, i * 15f);
            _rays.gameObject.AddComponent<Spinner>().speed = 12f;

            // логотип студии
            string word = "VALOUR GAMES";
            float step = 0.05f;
            float start = 0.5f - step * (word.Length - 1) / 2f;
            for (int i = 0; i < word.Length; i++)
            {
                var t = UIKit.Label(s, word[i].ToString(), 110, UIKit.Gold, TextAnchor.MiddleCenter,
                    start + i * step - 0.04f, 0.62f, start + i * step + 0.04f, 0.82f, true);
                UIKit.AddOutline(t, UIKit.Hex("#7A2E12"), 4f);
                UIKit.AddShadow(t, 8f, 0.5f);
                t.gameObject.SetActive(false);
                _letters.Add(t);
            }

            // название игры + шаурма
            var title = UIKit.Label(s, GameData.FullTitle, 56, UIKit.Cream, TextAnchor.MiddleCenter, 0.2f, 0.5f, 0.8f, 0.6f, true);
            _title = title.rectTransform;
            _title.gameObject.SetActive(false);

            var hero = Icons.Shawarma(s, 0.5f, 0.36f, 200f);
            hero.color = Icons.Toast(1f);
            _hero = (RectTransform)hero.transform.parent;
            _hero.gameObject.AddComponent<Spinner>().speed = -140f;
            _hero.gameObject.SetActive(false);

            // полоса загрузки
            var bar = UIKit.Panel(s, "BarBg", UIKit.Alpha(Color.black, 0.5f), 0.3f, 0.16f, 0.7f, 0.2f);
            _barFill = UIKit.Panel(bar.transform, "Fill", UIKit.Orange, 0, 0, 0, 1).rectTransform;
            _tip = UIKit.Label(s, "", 32, UIKit.Alpha(UIKit.Cream, 0.8f), TextAnchor.MiddleCenter, 0.1f, 0.08f, 0.9f, 0.15f);

            _fade = UIKit.Panel(s, "Fade", Color.black, 0, 0, 1, 1, false);
            _fade.color = new Color(0, 0, 0, 0);
            _fade.raycastTarget = true;
            _fade.gameObject.AddComponent<Button>().onClick.AddListener(Skip);
        }

        void Skip()
        {
            if (_t > 1f && _t < Duration) _t = Duration;
        }

        void Update()
        {
            if (_done) return;
            _t += Time.unscaledDeltaTime;

            // буквы по одной
            int want = Mathf.Min(_letters.Count, Mathf.FloorToInt(_t / 0.07f));
            while (_shown < want)
            {
                var l = _letters[_shown];
                l.gameObject.SetActive(true);
                Juice.Punch(l.transform, 0.6f, 0.3f);
                if (l.text != " ") Sfx.Play(Sound.Tick);
                _shown++;
            }
            if (_t > 1.0f && !_title.gameObject.activeSelf)
            {
                _title.gameObject.SetActive(true);
                _hero.gameObject.SetActive(true);
                Juice.Punch(_title, 0.3f, 0.4f);
                Sfx.Play(Sound.Done);
            }

            float k = Mathf.Clamp01(_t / Duration);
            float eased = 1f - Mathf.Pow(1f - k, 2f);
            _barFill.anchorMax = new Vector2(eased, 1f);
            _barFill.offsetMax = Vector2.zero;

            int tip = Mathf.FloorToInt(_t / 0.9f);
            if (tip != _tipIndex)
            {
                _tipIndex = tip;
                var arr = L.En ? TipsEn : TipsRu;
                _tip.text = arr[UnityEngine.Random.Range(0, arr.Length)];
            }

            // не уходим с загрузки, пока не готов SDK Яндекса и облачное сохранение
            if (_t > Duration && Platform.Initialized)
            {
                float f = Mathf.Clamp01((_t - Duration) / 0.35f);
                _fade.color = new Color(0, 0, 0, f);
                if (f >= 1f)
                {
                    _done = true;
                    if (_onDone != null) _onDone();
                }
            }
        }
    }
}
