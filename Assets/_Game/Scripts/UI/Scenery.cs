using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Фон локации: небо, облака/звёзды, дома/море/магазины, тротуар, дождь. Сам себя анимирует.
    /// </summary>
    public class Scenery : MonoBehaviour
    {
        class Mover
        {
            public RectTransform rt;
            public float x, y, vx, vy;
        }

        readonly List<Mover> _clouds = new List<Mover>();
        readonly List<Mover> _drops = new List<Mover>();
        readonly List<Image> _twinkle = new List<Image>();

        public static Scenery Build(RectTransform area, LocationDef loc, bool rain)
        {
            var s = area.gameObject.AddComponent<Scenery>();
            s.Make(area, loc, rain);
            return s;
        }

        void Make(RectTransform area, LocationDef loc, bool rain)
        {
            bool night = loc.night;
            bool beach = loc.scene == "beach";
            bool mall = loc.scene == "mall";

            // небо
            var sky = UIKit.Full(area, "Sky").gameObject.AddComponent<Image>();
            sky.sprite = rain && !night && !mall
                ? UIKit.Gradient(UIKit.Hex("#6E7F8D"), UIKit.Hex("#B8C2C9"))
                : UIKit.Gradient(UIKit.Hex(loc.skyTop), UIKit.Hex(loc.skyBottom));
            sky.raycastTarget = false;

            if (mall) { BuildMall(area); return; }

            if (night)
            {
                for (int i = 0; i < 40; i++)
                {
                    var st = UIKit.Shape(area, UIKit.Circle, new Color(1, 1, 1, Random.Range(0.3f, 0.9f)),
                        Random.value, Random.Range(0.5f, 1f), 5f, 5f);
                    _twinkle.Add(st);
                }
                UIKit.Shape(area, UIKit.Circle, new Color(1f, 1f, 0.9f, 0.2f), 0.14f, 0.8f, 170f, 170f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFF6D5"), 0.14f, 0.8f, 100f, 100f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex(loc.skyTop), 0.155f, 0.82f, 80f, 80f);
            }
            else if (!rain)
            {
                UIKit.Shape(area, UIKit.Circle, new Color(1f, 0.95f, 0.6f, 0.35f), 0.86f, 0.78f, 200f, 200f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFE27A"), 0.86f, 0.78f, 110f, 110f);
            }

            // облака
            if (!night)
            {
                for (int i = 0; i < 4; i++)
                {
                    var c = UIKit.Rect(area, "Cloud");
                    float x = Random.Range(0f, 1f), y = Random.Range(0.72f, 0.92f);
                    UIKit.PlaceFixed(c, x, y, 220f, 80f);
                    var col = rain ? UIKit.Hex("#8C99A3") : new Color(1f, 1f, 1f, 0.9f);
                    UIKit.Shape(c, UIKit.Circle, col, 0.3f, 0.4f, 90f, 70f);
                    UIKit.Shape(c, UIKit.Circle, col, 0.55f, 0.55f, 110f, 90f);
                    UIKit.Shape(c, UIKit.Circle, col, 0.78f, 0.4f, 80f, 60f);
                    UIKit.Shape(c, UIKit.Rounded, col, 0.52f, 0.25f, 180f, 40f);
                    _clouds.Add(new Mover { rt = c, x = x, y = y, vx = Random.Range(0.004f, 0.01f) });
                }
            }

            if (beach) BuildBeach(area);
            else if (loc.scene == "towers") BuildTowers(area);
            else
            {
                BuildCity(area, night, rain, loc.id == 2);
                if (loc.scene == "market") BuildMarket(area);
                if (loc.scene == "stadium") BuildStadium(area);
                if (loc.scene == "airport") BuildAirport(area);
            }

            // тротуар
            var ground = UIKit.Hex(loc.ground);
            if (rain) ground = Color.Lerp(ground, UIKit.Hex("#6E6A64"), 0.35f);
            UIKit.Panel(area, "Sidewalk", ground, 0, 0, 1, 0.16f, false);
            UIKit.Panel(area, "Curb", Color.Lerp(ground, Color.black, 0.2f), 0, 0.15f, 1, 0.17f, false);
            if (!beach)
                for (int i = 0; i < 12; i++)
                    UIKit.Panel(area, "Seam", UIKit.Alpha(Color.black, 0.08f), i / 12f, 0, i / 12f + 0.003f, 0.15f, false);

            if (rain)
            {
                for (int i = 0; i < 70; i++)
                {
                    var d = UIKit.Shape(area, UIKit.RoundedSmall, new Color(0.85f, 0.92f, 1f, 0.55f), 0, 0, 4f, 34f, 15f).rectTransform;
                    var m = new Mover { rt = d, x = Random.value, y = Random.value, vx = -0.12f, vy = -Random.Range(1.1f, 1.6f) };
                    _drops.Add(m);
                    SetPos(m);
                }
            }
        }

        void BuildCity(RectTransform area, bool night, bool rain, bool station)
        {
            Color far = night ? UIKit.Hex("#1C2240") : (rain ? UIKit.Hex("#7D8A96") : UIKit.Hex("#A9BCD0"));
            float px = -0.02f;
            while (px < 1f)
            {
                float w = Random.Range(0.06f, 0.11f), h = Random.Range(0.3f, 0.55f);
                UIKit.Panel(area, "FarHouse", far, px, 0.15f, px + w, 0.15f + h, false);
                px += w;
            }

            string[] dayCols = { "#D9A06A", "#C9837B", "#E3C07A", "#B98B6E", "#D6A48C" };
            string[] nightCols = { "#2A2F4F", "#33294A", "#26344A", "#3B2E3E" };
            px = -0.03f;
            int n = 0;
            while (px < 1f)
            {
                float w = Random.Range(0.1f, 0.16f), h = Random.Range(0.22f, 0.42f);
                string hex = night ? nightCols[Random.Range(0, nightCols.Length)] : dayCols[Random.Range(0, dayCols.Length)];
                var house = UIKit.Panel(area, "House", UIKit.Hex(hex), px, 0.15f, px + w - 0.005f, 0.15f + h, false);
                if (rain && !night) house.color = Color.Lerp(house.color, UIKit.Hex("#6E7F8D"), 0.35f);
                int rows = Mathf.Max(2, (int)(h * 10f));
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        float wx = 0.12f + c * 0.3f, wy = 0.1f + r * (0.85f / rows);
                        bool lit = Random.value < (night ? 0.7f : (rain ? 0.6f : 0.3f));
                        UIKit.Panel(house.transform, "Win", lit ? UIKit.Hex("#FFE9A8") : UIKit.Hex(night ? "#141A2E" : "#5D7A96"),
                            wx, wy, wx + 0.18f, wy + 0.5f / rows, false);
                    }
                UIKit.Panel(house.transform, "Roof", UIKit.Alpha(Color.black, 0.2f), -0.02f, 0.96f, 1.02f, 1.02f, false);
                if (night && n % 2 == 0)
                {
                    // неоновая вывеска
                    string[] neon = { "#FF4FA3", "#22E3FF", "#B388FF", "#FFD740" };
                    var sign = UIKit.Panel(house.transform, "Neon", UIKit.Hex(neon[Random.Range(0, neon.Length)]), 0.15f, 0.62f, 0.85f, 0.72f);
                    _twinkle.Add(sign);
                }
                if (station && n == 2)
                {
                    // часы вокзала
                    UIKit.Shape(house.transform, UIKit.Circle, UIKit.Cream, 0.5f, 0.8f, 70f, 70f);
                    UIKit.Shape(house.transform, UIKit.RoundedSmall, UIKit.Dark, 0.5f, 0.82f, 5f, 28f);
                    UIKit.Shape(house.transform, UIKit.RoundedSmall, UIKit.Dark, 0.54f, 0.8f, 22f, 5f);
                }
                px += w;
                n++;
            }
        }

        void BuildMarket(RectTransform area)
        {
            string[] cols = { "#E53935", "#43A047", "#1E88E5", "#FB8C00", "#8E24AA" };
            for (int i = 0; i < 6; i++)
            {
                float x = 0.08f + i * 0.17f;
                var c = UIKit.Hex(cols[i % cols.Length]);
                UIKit.Panel(area, "Stall", UIKit.Hex("#8D6E63"), x - 0.06f, 0.16f, x + 0.06f, 0.3f, false);
                for (int k = 0; k < 4; k++)
                    UIKit.Panel(area, "Tent", k % 2 == 0 ? c : Color.white, x - 0.07f + k * 0.035f, 0.3f, x - 0.035f + k * 0.035f, 0.36f, false);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FF7043"), x - 0.03f, 0.2f, 22f, 20f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFEE58"), x, 0.2f, 22f, 20f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#9CCC65"), x + 0.03f, 0.2f, 22f, 20f);
            }
        }

        void BuildStadium(RectTransform area)
        {
            UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#90A4AE"), 0.5f, 0.3f, 1500f, 330f);
            UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#B0BEC5"), 0.5f, 0.32f, 1350f, 250f);
            for (int i = 0; i < 40; i++)
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex(i % 3 == 0 ? "#E53935" : (i % 3 == 1 ? "#1E88E5" : "#FFEB3B")),
                    0.1f + (i % 20) * 0.042f, 0.36f + (i / 20) * 0.06f, 14f, 14f);
            float[] xs = { 0.12f, 0.88f };
            foreach (var x in xs)
            {
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#455A64"), x, 0.55f, 10f, 330f);
                var lamp = UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#FFFDE7"), x, 0.9f, 90f, 40f);
                _twinkle.Add(lamp);
            }
        }

        void BuildAirport(RectTransform area)
        {
            UIKit.Panel(area, "Tower", UIKit.Hex("#ECEFF1"), 0.82f, 0.15f, 0.86f, 0.7f, false);
            UIKit.Panel(area, "TowerTop", UIKit.Hex("#546E7A"), 0.8f, 0.7f, 0.88f, 0.78f);
            var plane = UIKit.Rect(area, "Plane");
            UIKit.PlaceFixed(plane, 0.2f, 0.82f, 180f, 60f);
            UIKit.Shape(plane, UIKit.Rounded, Color.white, 0.5f, 0.5f, 180f, 30f);
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#CFD8DC"), 0.5f, 0.5f, 30f, 90f, 20f);
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#E53935"), 0.08f, 0.8f, 20f, 40f, 20f);
            _clouds.Add(new Mover { rt = plane, x = 0.2f, y = 0.82f, vx = 0.04f });
        }

        void BuildTowers(RectTransform area)
        {
            string[] glass = { "#3949AB", "#283593", "#5C6BC0", "#1A237E" };
            float px = -0.02f;
            int n = 0;
            while (px < 1f)
            {
                float w = Random.Range(0.07f, 0.12f), h = Random.Range(0.45f, 0.82f);
                var t = UIKit.Panel(area, "Tower", UIKit.Hex(glass[n % glass.Length]), px, 0.15f, px + w - 0.006f, 0.15f + h, false);
                for (int r = 0; r < 14; r++)
                    for (int c = 0; c < 3; c++)
                        if (Random.value < 0.55f)
                            UIKit.Panel(t.transform, "Win", UIKit.Hex("#FFE9A8"), 0.1f + c * 0.3f, 0.04f + r * 0.068f, 0.28f + c * 0.3f, 0.08f + r * 0.068f, false);
                if (n % 3 == 1)
                {
                    var tip = UIKit.Shape(t.transform, UIKit.Circle, UIKit.Red, 0.5f, 1.02f, 12f, 12f);
                    _twinkle.Add(tip);
                }
                px += w;
                n++;
            }
        }

        void BuildBeach(RectTransform area)
        {
            var sea = UIKit.Rect(area, "Sea");
            UIKit.Place(sea, 0, 0.15f, 1, 0.42f);
            var img = sea.gameObject.AddComponent<Image>();
            img.sprite = UIKit.Gradient(UIKit.Hex("#1E88E5"), UIKit.Hex("#4DD0E1"));
            img.raycastTarget = false;
            for (int i = 0; i < 6; i++)
                UIKit.Shape(sea, UIKit.RoundedSmall, new Color(1, 1, 1, 0.35f), Random.Range(0.05f, 0.95f), 0.2f + i * 0.13f,
                    Random.Range(60f, 140f), 6f);
            // пальмы
            float[] xs = { 0.08f, 0.3f, 0.72f, 0.93f };
            foreach (var x in xs)
            {
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#8D6E63"), x, 0.34f, 22f, 190f, 6f);
                for (int l = 0; l < 5; l++)
                    UIKit.Shape(area, UIKit.Rounded, UIKit.Hex("#2E7D32"), x + 0.01f, 0.56f, 140f, 34f, -60f + l * 30f);
            }
            // зонтик
            UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#EEEEEE"), 0.5f, 0.28f, 8f, 120f);
            UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#E53935"), 0.5f, 0.42f, 220f, 70f);
        }

        void BuildMall(RectTransform area)
        {
            string[] cols = { "#E57373", "#64B5F6", "#81C784", "#FFB74D", "#BA68C8", "#4DB6AC" };
            string[] names = { "SHOES", "TECH", "FRESH", "TOYS", "BEAUTY", "BOOKS" };
            for (int i = 0; i < 6; i++)
            {
                float x0 = i / 6f + 0.005f, x1 = (i + 1) / 6f - 0.005f;
                var shop = UIKit.Panel(area, "Shop", UIKit.Hex("#FAFAFA"), x0, 0.17f, x1, 0.8f, false);
                var sign = UIKit.Panel(shop.transform, "Sign", UIKit.Hex(cols[i]), 0.05f, 0.82f, 0.95f, 0.97f);
                UIKit.FitLabel(sign.transform, names[i], 30, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
                UIKit.Panel(shop.transform, "Glass", UIKit.Hex("#B3E5FC"), 0.08f, 0.05f, 0.92f, 0.76f);
            }
            for (int i = 0; i < 8; i++)
                UIKit.Shape(area, UIKit.Circle, new Color(1f, 1f, 0.85f, 0.9f), (i + 0.5f) / 8f, 0.93f, 40f, 16f);
        }

        static void SetPos(Mover m)
        {
            m.rt.anchorMin = m.rt.anchorMax = new Vector2(m.x, m.y);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var c in _clouds)
            {
                c.x += c.vx * dt;
                if (c.x > 1.15f) c.x = -0.15f;
                SetPos(c);
            }
            foreach (var d in _drops)
            {
                d.x += d.vx * dt;
                d.y += d.vy * dt;
                if (d.y < -0.05f) { d.y = 1.05f; d.x = Random.Range(0f, 1.1f); }
                SetPos(d);
            }
            for (int i = 0; i < _twinkle.Count; i++)
            {
                var img = _twinkle[i];
                if (img == null) continue;
                var col = img.color;
                col.a = 0.55f + 0.45f * Mathf.Sin(Time.time * (1.5f + i % 5) + i);
                img.color = col;
            }
        }
    }
}
