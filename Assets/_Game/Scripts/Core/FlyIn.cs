using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Кинематографичный старт дня: вид на ларёк с улицы → камера отъезжает (замах) →
    /// стремительно влетает в окно → белая вспышка → день начинается.
    /// </summary>
    public class FlyIn : MonoBehaviour
    {
        const float Anticipation = 0.45f;
        const float Zoom = 1.1f;
        const float Base = 1.16f;   // мир чуть больше экрана: при «замахе» камеры края не видны

        Action _onDone;
        RectTransform _world;
        Image _flash;
        float _t;
        bool _done, _whoosh;

        SaveData _save;

        public static void Play(RectTransform screen, SaveData save, Action onDone)
        {
            var f = screen.gameObject.AddComponent<FlyIn>();
            f._onDone = onDone;
            f._save = save;
            f.Build(screen, save.Loc, save.day);
        }

        void Build(RectTransform s, LocationDef loc, int day)
        {
            // фон под «миром»: даже если камера качнётся, чёрных краёв не будет
            var bg = UIKit.Full(s, "Backdrop").gameObject.AddComponent<Image>();
            bg.sprite = UIKit.Gradient(UIKit.Hex(loc.skyTop), UIKit.Hex(loc.ground));
            bg.raycastTarget = false;
            _world = UIKit.Full(s, "World");
            Scenery.Build(_world, loc, false);

            var kioskCol = UIKit.Hex(loc.kiosk);
            var dark = Color.Lerp(kioskCol, Color.black, 0.35f);

            // корпус ларька
            var body = UIKit.Panel(_world, "Body", UIKit.Hex("#E9D8B8"), 0.34f, 0.1f, 0.66f, 0.62f, false);
            UIKit.AddShadow(body, 14f, 0.35f);
            UIKit.Panel(body.transform, "Base", dark, 0f, 0f, 1f, 0.12f, false);
            // крыша
            UIKit.Panel(_world, "Roof", UIKit.Dark, 0.32f, 0.62f, 0.68f, 0.66f, false);
            // вывеска
            var sign = UIKit.Panel(_world, "Sign", UIKit.Hex("#2B1D14"), 0.36f, 0.66f, 0.64f, 0.8f);
            UIKit.AddShadow(sign, 8f, 0.4f);
            var st = UIKit.Label(sign.transform, L.T("ШАУРМА", "SHAWARMA"), 90, UIKit.Gold, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f, true);
            UIKit.AddOutline(st, UIKit.Hex("#8A3B12"), 4f);
            for (int i = 0; i < 12; i++)
                UIKit.Shape(sign.transform, UIKit.Circle, UIKit.Hex("#FFE9A8"), (i + 0.5f) / 12f, 0.06f, 14f, 14f);

            // окно выдачи
            var win = UIKit.Panel(_world, "Window", UIKit.Hex("#3B2A1E"), 0.39f, 0.3f, 0.61f, 0.52f, false);
            var light = UIKit.Full(win.transform, "Light").gameObject.AddComponent<Image>();
            light.sprite = UIKit.Gradient(UIKit.Hex("#FFB74D"), UIKit.Hex("#5D4037"));
            light.raycastTarget = false;
            // повар в окне
            UIKit.Shape(win.transform, UIKit.Rounded, Color.white, 0.35f, 0.25f, 110f, 90f);
            UIKit.Shape(win.transform, UIKit.Circle, UIKit.Hex("#F6D2B0"), 0.35f, 0.55f, 70f, 70f);
            UIKit.Shape(win.transform, UIKit.Circle, UIKit.Dark, 0.31f, 0.56f, 8f, 10f);
            UIKit.Shape(win.transform, UIKit.Circle, UIKit.Dark, 0.39f, 0.56f, 8f, 10f);
            UIKit.Shape(win.transform, UIKit.RoundedSmall, UIKit.Hex("#8C3B2E"), 0.35f, 0.48f, 20f, 6f);
            // поварской колпак ровно на голове: околыш + пышный верх
            UIKit.Shape(win.transform, UIKit.Circle, Color.white, 0.31f, 0.765f, 34f, 34f);
            UIKit.Shape(win.transform, UIKit.Circle, Color.white, 0.39f, 0.765f, 34f, 34f);
            UIKit.Shape(win.transform, UIKit.Circle, Color.white, 0.35f, 0.8f, 40f, 38f);
            UIKit.Shape(win.transform, UIKit.RoundedSmall, Color.white, 0.35f, 0.685f, 62f, 20f);
            UIKit.Shape(win.transform, UIKit.RoundedSmall, UIKit.Hex("#E0E0E0"), 0.35f, 0.65f, 62f, 3f);
            // вертел в окне
            for (int i = 0; i < 5; i++)
                UIKit.Shape(win.transform, UIKit.Rounded, UIKit.Hex(i % 2 == 0 ? "#A8461F" : "#8E3A18"), 0.72f, 0.2f + i * 0.13f, 70f - Mathf.Abs(i - 2) * 10f, 26f);
            // навес над окном
            for (int i = 0; i < 8; i++)
            {
                var col = Meta.Stripe(_save, loc, i);
                UIKit.Panel(_world, "Awning", col, 0.37f + i * 0.0325f, 0.52f, 0.37f + (i + 1) * 0.0325f, 0.58f, false);
                UIKit.Shape(_world, UIKit.Circle, col, 0.37f + (i + 0.5f) * 0.0325f, 0.52f, 62f, 24f);
            }
            // прилавок
            UIKit.Panel(_world, "Counter", UIKit.Wood, 0.37f, 0.27f, 0.63f, 0.31f, false);
            // меню-доска
            var menu = UIKit.Panel(_world, "MenuBoard", UIKit.Hex("#263238"), 0.67f, 0.12f, 0.76f, 0.34f);
            UIKit.FitLabel(menu.transform, L.T("МЕНЮ\nШаурма\nКола\nАйран", "MENU\nShawarma\nCola\nAyran"), 26, Color.white, 0.08f, 0.05f, 0.92f, 0.95f);

            // табличка дня
            var tag = UIKit.Panel(s, "DayTag", UIKit.Alpha(UIKit.Dark, 0.85f), 0.36f, 0.86f, 0.64f, 0.96f);
            UIKit.FitLabel(tag.transform, loc.Name + " • " + L.T("День ", "Day ") + day, 44, UIKit.Cream, 0.04f, 0.05f, 0.96f, 0.95f);
            Juice.Punch(tag.rectTransform, 0.2f, 0.4f);

            // точка "камеры" — центр окна
            _world.pivot = new Vector2(0.5f, 0.41f);

            _flash = UIKit.Panel(s, "Flash", Color.white, 0, 0, 1, 1, false);
            _flash.color = new Color(1, 1, 1, 0);
            _world.localScale = new Vector3(Base, Base, 1f);
        }

        void Update()
        {
            if (_done) return;
            _t += Time.unscaledDeltaTime;
            float sc;
            if (_t < Anticipation)
            {
                float k = _t / Anticipation;
                sc = Base - 0.06f * Mathf.Sin(k * Mathf.PI * 0.5f);
            }
            else
            {
                if (!_whoosh) { _whoosh = true; Sfx.Play(Sound.Whoosh); }
                float k = Mathf.Clamp01((_t - Anticipation) / Zoom);
                float e = k * k * k;
                sc = Mathf.Lerp(Base - 0.06f, 9f, e);
                _world.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI) * 2f);
                float f = Mathf.Clamp01((k - 0.7f) / 0.3f);
                _flash.color = new Color(1, 1, 1, f);
                if (k >= 1f)
                {
                    _done = true;
                    if (_onDone != null) _onDone();
                    return;
                }
            }
            _world.localScale = new Vector3(sc, sc, 1f);
        }
    }
}
