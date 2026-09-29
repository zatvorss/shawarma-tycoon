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
            if (loc.scene == "gov") { BuildGov(area); return; }

            if (night)
            {
                for (int i = 0; i < 40; i++)
                {
                    var st = UIKit.Shape(area, UIKit.Circle, new Color(1, 1, 1, Random.Range(0.3f, 0.9f)),
                        Random.value, Random.Range(0.5f, 1f), 5f, 5f);
                    _twinkle.Add(st);
                }
                // полная луна с кратерами и мягким ореолом
                UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 1f, 0.9f, 0.12f), 0.14f, 0.8f, 190f, 190f);
                UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 1f, 0.9f, 0.18f), 0.14f, 0.8f, 140f, 140f);
                var moon = UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#FFF6D5"), 0.14f, 0.8f, 100f, 100f);
                UIKit.Shape(moon.transform, UIKit.Circle, UIKit.Hex("#EDE3BE"), 0.35f, 0.62f, 24f, 24f);
                UIKit.Shape(moon.transform, UIKit.Circle, UIKit.Hex("#EDE3BE"), 0.64f, 0.4f, 16f, 16f);
                UIKit.Shape(moon.transform, UIKit.Circle, UIKit.Hex("#EDE3BE"), 0.6f, 0.72f, 10f, 10f);
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

            if (loc.scene == "dubai") BuildDubai(area);
            else if (beach) BuildBeach(area);
            else if (loc.scene == "towers") BuildTowers(area);
            else
            {
                BuildCity(area, night, rain, loc.id == 2, loc.id);
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

        void BuildCity(RectTransform area, bool night, bool rain, bool station, int id = 0)
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
                    string[] words = { "КАФЕ", "БАР", "ОТЕЛЬ", "24/7", "КИНО", "ПИЦЦА" };
                    var nc = UIKit.Hex(neon[n / 2 % neon.Length]);
                    var sign = UIKit.Panel(house.transform, "Neon", UIKit.Hex("#120E1E"), 0.1f, 0.6f, 0.9f, 0.74f);
                    UIKit.AddOutline(sign, nc, 2f);
                    var nt = UIKit.FitLabel(sign.transform, words[(n / 2 + id) % words.Length], 24, nc, 0.06f, 0.08f, 0.94f, 0.92f);
                    UIKit.AddOutline(nt, UIKit.Alpha(nc, 0.4f), 2f);
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
            // мачты прожекторов стоят на земле: от тротуара до самого верха
            float[] xs = { 0.12f, 0.88f };
            foreach (var x in xs)
            {
                UIKit.Panel(area, "Mast", UIKit.Hex("#455A64"), x - 0.003f, 0.16f, x + 0.003f, 0.86f, false);
                for (int k = 0; k < 6; k++)
                    UIKit.Panel(area, "Brace", UIKit.Hex("#546E7A"), x - 0.008f, 0.22f + k * 0.1f, x + 0.008f, 0.225f + k * 0.1f, false);
                UIKit.Panel(area, "Frame", UIKit.Hex("#37474F"), x - 0.03f, 0.85f, x + 0.03f, 0.95f);
                for (int k = 0; k < 6; k++)
                    UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFFDE7"), x - 0.02f + (k % 3) * 0.02f, 0.875f + (k / 3) * 0.05f, 22f, 18f);
                UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 1f, 0.85f, 0.15f), x, 0.9f, 220f, 140f);
            }
        }

        void BuildAirport(RectTransform area)
        {
            // терминал со стеклянным фасадом
            var term = UIKit.Panel(area, "Terminal", UIKit.Hex("#CFD8DC"), 0.55f, 0.15f, 1.02f, 0.5f, false);
            UIKit.Panel(term.transform, "Glass", UIKit.Hex("#4FC3F7"), 0.02f, 0.18f, 0.98f, 0.82f, false);
            for (int i = 0; i < 12; i++)
                UIKit.Panel(term.transform, "Mullion", UIKit.Hex("#90A4AE"), i / 12f, 0.18f, i / 12f + 0.006f, 0.82f, false);
            UIKit.Panel(term.transform, "Roof", UIKit.Hex("#607D8B"), -0.02f, 0.82f, 1.02f, 0.94f, false);
            var tsign = UIKit.Panel(term.transform, "Sign", UIKit.Hex("#1565C0"), 0.25f, 0.84f, 0.75f, 0.93f, false);
            UIKit.FitLabel(tsign.transform, L.T("АЭРОПОРТ", "AIRPORT"), 30, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
            // диспетчерская вышка: ствол, стеклянная кабина, антенна
            UIKit.Panel(area, "TowerShaft", UIKit.Hex("#ECEFF1"), 0.405f, 0.15f, 0.435f, 0.62f, false);
            UIKit.Panel(area, "TowerShade", UIKit.Hex("#CFD8DC"), 0.425f, 0.15f, 0.435f, 0.62f, false);
            UIKit.Panel(area, "Cab", UIKit.Hex("#546E7A"), 0.39f, 0.62f, 0.45f, 0.72f);
            UIKit.Panel(area, "CabGlass", UIKit.Hex("#80DEEA"), 0.395f, 0.645f, 0.445f, 0.7f, false);
            UIKit.Panel(area, "CabRoof", UIKit.Hex("#37474F"), 0.385f, 0.72f, 0.455f, 0.74f);
            UIKit.Panel(area, "Antenna", UIKit.Hex("#37474F"), 0.418f, 0.74f, 0.422f, 0.82f, false);
            var tip = UIKit.Shape(area, UIKit.Circle, UIKit.Red, 0.42f, 0.825f, 12f, 12f);
            _twinkle.Add(tip);

            // самолёт в профиль: фюзеляж, хвостовой киль, стреловидное крыло снизу, иллюминаторы
            var plane = UIKit.Rect(area, "Plane");
            UIKit.PlaceFixed(plane, 0.2f, 0.86f, 240f, 90f);
            plane.SetAsLastSibling();
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#B0BEC5"), 0.56f, 0.62f, 56f, 12f, 18f);            // дальнее крыло
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#E53935"), 0.1f, 0.72f, 30f, 46f, -28f);            // киль
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#E53935"), 0.1f, 0.5f, 34f, 10f);                   // стабилизатор
            UIKit.Shape(plane, UIKit.Rounded, UIKit.Hex("#37474F"), 0.52f, 0.5f, 228f, 36f);                      // контур
            UIKit.Shape(plane, UIKit.Rounded, UIKit.Hex("#FAFAFA"), 0.52f, 0.52f, 222f, 30f);                     // фюзеляж
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#1E88E5"), 0.52f, 0.44f, 212f, 7f);                 // полоса
            for (int i = 0; i < 9; i++) UIKit.Shape(plane, UIKit.Circle, UIKit.Hex("#263238"), 0.3f + i * 0.065f, 0.56f, 7f, 8f);
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#263238"), 0.95f, 0.57f, 14f, 8f);                  // кабина
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#90A4AE"), 0.5f, 0.33f, 80f, 14f, -18f);             // ближнее крыло
            UIKit.Shape(plane, UIKit.RoundedSmall, UIKit.Hex("#607D8B"), 0.5f, 0.3f, 26f, 12f, -18f);              // двигатель
            _clouds.Add(new Mover { rt = plane, x = 0.2f, y = 0.86f, vx = 0.04f });
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
            // пальмы: изогнутый ствол из сегментов, листья растут из одной точки кроны и свисают
            float[] xs = { 0.06f, 0.29f, 0.73f, 0.95f };
            float[] lean = { 0.035f, -0.03f, 0.03f, -0.035f };
            for (int p = 0; p < xs.Length; p++) Palm(area, xs[p], lean[p]);
            // зонтик
            UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#EEEEEE"), 0.5f, 0.28f, 8f, 120f);
            UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#E53935"), 0.5f, 0.42f, 220f, 70f);
        }

        /// <summary>
        /// Пальма: плавный сужающийся ствол из перекрывающихся овалов (без щелей и «полосок»),
        /// крона — изогнутые вниз перистые листья из цепочки овалов, в центре — кокосы.
        /// </summary>
        void Palm(RectTransform area, float x, float lean)
        {
            const int seg = 24;
            float y0 = 0.15f, y1 = 0.6f;
            float cx = x, cy = y1;
            var bark = UIKit.Hex("#9C7A5B");
            var barkDark = UIKit.Hex("#7B5B40");
            var barkLight = UIKit.Hex("#B89474");
            // три прохода (контур → кора → блик), иначе контур следующего сегмента ляжет полосой поверх предыдущего
            for (int pass = 0; pass < 3; pass++)
                for (int i = 0; i < seg; i++)
                {
                    float t = (float)i / (seg - 1);
                    float px = x + lean * t * t;
                    float py = Mathf.Lerp(y0, y1, t);
                    float w = 34f - t * 12f;
                    // сегмент повёрнут по изгибу ствола и перекрывает соседей — ствол гладкий, без щелей
                    float rot = -Mathf.Atan2(2f * lean * t * 1920f, (y1 - y0) * 1080f) * Mathf.Rad2Deg;
                    if (pass == 0) UIKit.Shape(area, UIKit.RoundedSmall, barkDark, px, py, w + 5f, 34f, rot);
                    else if (pass == 1) UIKit.Shape(area, UIKit.RoundedSmall, bark, px, py, w, 34f, rot);
                    else
                    {
                        UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Alpha(barkLight, 0.7f), px - 0.002f, py, w * 0.3f, 34f, rot);
                        if (i % 3 == 1) UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Alpha(barkDark, 0.35f), px, py, w * 0.8f, 3f, rot);
                    }
                    cx = px; cy = py;
                }

            // крона строится в пикселях вокруг верхушки ствола
            var crown = UIKit.Rect(area, "Crown");
            UIKit.PlaceFixed(crown, cx, cy, 10f, 10f);
            float[] ang = { 172f, 145f, 118f, 90f, 62f, 35f, 8f, 205f, -25f };
            float[] len = { 150f, 160f, 140f, 110f, 140f, 160f, 150f, 120f, 120f };
            var dark = UIKit.Hex("#1B5E20");
            for (int i = 0; i < ang.Length; i++)
            {
                float a = ang[i] * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float L0 = len[i];
                float droop = 0.55f + 0.2f * Mathf.Abs(Mathf.Cos(a));   // боковые листья свисают сильнее
                var col = UIKit.Hex(i % 2 == 0 ? "#2E7D32" : "#43A047");
                const int n = 10;
                for (int pass = 0; pass < 2; pass++)
                    for (int k = 0; k < n; k++)
                    {
                        float s = (k + 0.5f) / n;
                        // точка на изогнутой вниз дуге и касательная к ней
                        Vector2 pnt = d * (s * L0) + new Vector2(0f, -droop * L0 * s * s);
                        Vector2 tan = d * L0 + new Vector2(0f, -2f * droop * L0 * s);
                        float rot = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg;
                        float lw = L0 / n * 2.4f;
                        float lh = Mathf.Lerp(34f, 10f, s);
                        var img = UIKit.Shape(crown, UIKit.CircleHD, pass == 0 ? dark : col, 0.5f, 0.5f,
                            lw + (pass == 0 ? 6f : 0f), lh + (pass == 0 ? 6f : 0f), rot);
                        img.rectTransform.anchoredPosition = pnt;
                    }
            }
            // кокосы
            for (int i = 0; i < 3; i++)
            {
                var co = UIKit.Shape(crown, UIKit.CircleHD, UIKit.Hex("#5D4037"), 0.5f, 0.5f, 22f, 22f);
                co.rectTransform.anchoredPosition = new Vector2(-12f + i * 12f, -12f - (i == 1 ? 6f : 0f));
                var hl = UIKit.Shape(crown, UIKit.CircleHD, UIKit.Hex("#8D6E63"), 0.5f, 0.5f, 7f, 7f);
                hl.rectTransform.anchoredPosition = new Vector2(-15f + i * 12f, -8f - (i == 1 ? 6f : 0f));
            }
        }

        /// <summary>
        /// Фудкорт в торговом центре: стеклянная крыша, второй этаж с перилами, ряд фуд-стендов
        /// с подсвеченными вывесками и меню-бордами, плиточный пол и столики с кашпо.
        /// </summary>
        void BuildMall(RectTransform area)
        {
            // стеклянная крыша
            UIKit.Panel(area, "Ceiling", UIKit.Hex("#ECEFF1"), 0f, 0.88f, 1f, 1f, false);
            for (int i = 0; i < 10; i++)
                UIKit.Panel(area, "Sky", UIKit.Hex("#B3E5FC"), i / 10f + 0.006f, 0.9f, (i + 1) / 10f - 0.006f, 0.99f, false);
            UIKit.Panel(area, "Beam", UIKit.Hex("#B0BEC5"), 0f, 0.875f, 1f, 0.89f, false);

            // второй этаж: витрины магазинов и стеклянные перила
            string[] up = { "#F8BBD0", "#C5CAE9", "#DCEDC8", "#FFE0B2", "#B2EBF2", "#E1BEE7", "#FFF9C4" };
            for (int i = 0; i < 7; i++)
            {
                float x0 = i / 7f + 0.004f, x1 = (i + 1) / 7f - 0.004f;
                var shop = UIKit.Panel(area, "Upper", UIKit.Hex("#FAFAFA"), x0, 0.66f, x1, 0.86f, false);
                UIKit.Panel(shop.transform, "Sign", UIKit.Hex(up[i]), 0.08f, 0.78f, 0.92f, 0.95f, false);
                UIKit.Panel(shop.transform, "Glass", UIKit.Hex("#E3F2FD"), 0.08f, 0.05f, 0.92f, 0.72f, false);
            }
            UIKit.Panel(area, "Slab", UIKit.Hex("#9E9E9E"), 0f, 0.6f, 1f, 0.635f, false);
            UIKit.Panel(area, "Railing", new Color(0.85f, 0.95f, 1f, 0.45f), 0f, 0.635f, 1f, 0.68f, false);
            UIKit.Panel(area, "Handrail", UIKit.Hex("#B0BEC5"), 0f, 0.675f, 1f, 0.685f, false);
            for (int i = 0; i < 24; i++)
                UIKit.Panel(area, "Post", UIKit.Hex("#B0BEC5"), i / 24f, 0.635f, i / 24f + 0.003f, 0.68f, false);

            // подвесные лампы
            for (int i = 0; i < 6; i++)
            {
                float x = (i + 0.5f) / 6f;
                UIKit.Panel(area, "Cord", UIKit.Hex("#616161"), x - 0.001f, 0.55f, x + 0.001f, 0.6f, false);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFB300"), x, 0.545f, 40f, 22f);
                UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 0.9f, 0.6f, 0.18f), x, 0.52f, 150f, 70f);
            }

            // ряд фуд-стендов
            string[] names = { "ПИЦЦА", "СУШИ", "БУРГЕРЫ", "ВОК", "КОФЕ" };
            string[] namesEn = { "PIZZA", "SUSHI", "BURGERS", "WOK", "COFFEE" };
            string[] brand = { "#D32F2F", "#00897B", "#F57C00", "#6A1B9A", "#5D4037" };
            string[] light = { "#FFEB3B", "#E0F2F1", "#FFF3E0", "#F3E5F5", "#FFE0B2" };
            for (int i = 0; i < 5; i++)
            {
                float x0 = i / 5f + 0.006f, x1 = (i + 1) / 5f - 0.006f;
                var bc = UIKit.Hex(brand[i]);
                var stall = UIKit.Panel(area, "Stall", Color.Lerp(bc, Color.white, 0.75f), x0, 0.2f, x1, 0.52f, false);
                var st = stall.transform;
                // подсвеченная вывеска
                var sign = UIKit.Panel(st, "Sign", bc, 0f, 0.8f, 1f, 1f, false);
                var sl = UIKit.FitLabel(sign.transform, L.T(names[i], namesEn[i]), 34, UIKit.Hex(light[i]), 0.05f, 0.08f, 0.95f, 0.92f);
                UIKit.AddOutline(sl, UIKit.Alpha(Color.black, 0.35f), 2f);
                // меню-борд: картинки блюд и строчки цен
                var menu = UIKit.Panel(st, "Menu", UIKit.Hex("#212121"), 0.08f, 0.5f, 0.92f, 0.77f, false);
                for (int k = 0; k < 3; k++)
                {
                    float mx = 0.06f + k * 0.32f;
                    UIKit.Panel(menu.transform, "Pic", Color.Lerp(bc, UIKit.Hex(light[i]), 0.4f), mx, 0.42f, mx + 0.26f, 0.9f, false);
                    UIKit.Panel(menu.transform, "Line", UIKit.Alpha(Color.white, 0.7f), mx, 0.14f, mx + 0.26f, 0.22f, false);
                }
                // стойка
                UIKit.Panel(st, "CounterTop", UIKit.Hex("#CFD8DC"), -0.01f, 0.36f, 1.01f, 0.42f, false);
                UIKit.Panel(st, "CounterFront", bc, 0f, 0f, 1f, 0.36f, false);
                UIKit.Panel(st, "Stripe", UIKit.Alpha(Color.white, 0.35f), 0f, 0.16f, 1f, 0.2f, false);
                UIKit.Panel(st, "Till", UIKit.Hex("#37474F"), 0.7f, 0.42f, 0.86f, 0.5f);
            }

            // плиточный пол
            UIKit.Panel(area, "Floor", UIKit.Hex("#E0D6CC"), 0f, 0f, 1f, 0.2f, false);
            for (int i = 0; i < 24; i++)
                for (int r = 0; r < 2; r++)
                    if ((i + r) % 2 == 0)
                        UIKit.Panel(area, "Tile", UIKit.Hex("#D2C6BA"), i / 24f, r * 0.1f, (i + 1) / 24f, (r + 1) * 0.1f, false);
            UIKit.Panel(area, "Skirting", UIKit.Hex("#8D6E63"), 0f, 0.195f, 1f, 0.205f, false);

            // столики со стульями и растения
            float[] tx = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
            foreach (var x in tx)
            {
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#FF8A65"), x - 0.03f, 0.13f, 30f, 44f);
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#FF8A65"), x + 0.03f, 0.13f, 30f, 44f);
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#9E9E9E"), x, 0.1f, 8f, 50f);
                UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#FAFAFA"), x, 0.16f, 110f, 22f);
                UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFCC80"), x - 0.01f, 0.18f, 18f, 10f);
            }
            float[] px = { 0.2f, 0.8f };
            foreach (var x in px)
            {
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#795548"), x, 0.07f, 50f, 44f);
                for (int k = 0; k < 5; k++)
                    UIKit.Shape(area, UIKit.Rounded, UIKit.Hex(k % 2 == 0 ? "#43A047" : "#2E7D32"), x, 0.17f, 22f, 70f, -50f + k * 25f);
            }
        }

        /// <summary>
        /// Правительственная столовая: дубовые панели, мраморные колонны, флаги, люстры,
        /// длинные столы с белыми скатертями и красная ковровая дорожка.
        /// </summary>
        void BuildGov(RectTransform area)
        {
            UIKit.Panel(area, "Wall", UIKit.Hex("#EFE6D2"), 0f, 0.35f, 1f, 1f, false);
            UIKit.Panel(area, "Cornice", UIKit.Hex("#C8B48A"), 0f, 0.9f, 1f, 0.94f, false);
            UIKit.Panel(area, "Ceiling", UIKit.Hex("#F7F1E3"), 0f, 0.94f, 1f, 1f, false);
            // дубовые панели
            UIKit.Panel(area, "Wood", UIKit.Hex("#6D4C41"), 0f, 0.16f, 1f, 0.42f, false);
            for (int i = 0; i < 16; i++)
                UIKit.Panel(area, "Frame", UIKit.Hex("#5D4037"), i / 16f + 0.006f, 0.2f, (i + 1) / 16f - 0.006f, 0.38f);
            UIKit.Panel(area, "Rail", UIKit.Hex("#FFD54F"), 0f, 0.415f, 1f, 0.425f, false);
            // колонны
            for (int i = 0; i < 5; i++)
            {
                float x = 0.1f + i * 0.2f;
                UIKit.Panel(area, "Column", UIKit.Hex("#FAFAFA"), x - 0.02f, 0.16f, x + 0.02f, 0.9f, false);
                for (int k = 0; k < 3; k++) UIKit.Panel(area, "Flute", UIKit.Hex("#E0E0E0"), x - 0.012f + k * 0.01f, 0.2f, x - 0.008f + k * 0.01f, 0.86f, false);
                UIKit.Panel(area, "Cap", UIKit.Hex("#E0D5B7"), x - 0.03f, 0.86f, x + 0.03f, 0.9f);
                UIKit.Panel(area, "Base", UIKit.Hex("#E0D5B7"), x - 0.028f, 0.16f, x + 0.028f, 0.2f);
            }
            // гербы между колоннами
            for (int i = 0; i < 4; i++)
            {
                float x = 0.2f + i * 0.2f;
                var crest = UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#FFC107"), x, 0.64f, 70f, 70f);
                UIKit.Shape(crest.transform, UIKit.CircleHD, UIKit.Hex("#B71C1C"), 0.5f, 0.5f, 52f, 52f);
                UIKit.Shape(crest.transform, UIKit.RoundedSmall, UIKit.Hex("#FFC107"), 0.5f, 0.5f, 18f, 30f);
            }
            // люстры
            for (int i = 0; i < 3; i++)
            {
                float x = 0.2f + i * 0.3f;
                UIKit.Panel(area, "Chain", UIKit.Hex("#B8860B"), x - 0.001f, 0.84f, x + 0.001f, 0.94f, false);
                UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 0.93f, 0.7f, 0.25f), x, 0.8f, 200f, 110f);
                UIKit.Shape(area, UIKit.Rounded, UIKit.Hex("#FFD54F"), x, 0.82f, 110f, 22f);
                for (int k = 0; k < 5; k++) UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#FFF8E1"), x - 0.024f + k * 0.012f, 0.795f, 12f, 16f);
            }
            // пол с ковровой дорожкой и столы с белыми скатертями
            UIKit.Panel(area, "Floor", UIKit.Hex("#A1887F"), 0f, 0f, 1f, 0.16f, false);
            UIKit.Panel(area, "Carpet", UIKit.Hex("#B71C1C"), 0f, 0.04f, 1f, 0.12f, false);
            UIKit.Panel(area, "CarpetEdge", UIKit.Hex("#FFC107"), 0f, 0.115f, 1f, 0.12f, false);
            float[] tx = { 0.15f, 0.5f, 0.85f };
            foreach (var x in tx)
            {
                UIKit.Panel(area, "Table", Color.white, x - 0.1f, 0.14f, x + 0.1f, 0.2f);
                UIKit.Panel(area, "Cloth", UIKit.Hex("#ECEFF1"), x - 0.1f, 0.12f, x + 0.1f, 0.15f, false);
                for (int k = 0; k < 3; k++) UIKit.Shape(area, UIKit.Circle, UIKit.Hex("#E0E0E0"), x - 0.06f + k * 0.06f, 0.2f, 34f, 12f);
                UIKit.Shape(area, UIKit.RoundedSmall, UIKit.Hex("#90CAF9"), x + 0.08f, 0.225f, 10f, 30f);
            }
        }

        /// <summary>Дубай: закатное небо, Бурдж-Халифа и небоскрёбы, море, пальмы и песок.</summary>
        void BuildDubai(RectTransform area)
        {
            // далёкие небоскрёбы
            string[] glass = { "#90A4AE", "#78909C", "#B0BEC5", "#8DA3AE" };
            float px = -0.02f;
            int n = 0;
            while (px < 1f)
            {
                float w = Random.Range(0.05f, 0.09f), h = Random.Range(0.3f, 0.6f);
                if (Mathf.Abs(px + w * 0.5f - 0.5f) > 0.08f)
                {
                    var t = UIKit.Panel(area, "Tower", UIKit.Hex(glass[n % glass.Length]), px, 0.3f, px + w - 0.006f, 0.3f + h, false);
                    for (int r = 0; r < 10; r++)
                        UIKit.Panel(t.transform, "Band", UIKit.Alpha(Color.white, 0.25f), 0.1f, 0.05f + r * 0.095f, 0.9f, 0.07f + r * 0.095f, false);
                }
                px += w;
                n++;
            }
            // Бурдж-Халифа: ступенчатая игла в центре
            float[] ww = { 0.07f, 0.055f, 0.042f, 0.03f, 0.02f, 0.011f };
            float y = 0.3f;
            for (int i = 0; i < ww.Length; i++)
            {
                float h = 0.1f;
                UIKit.Panel(area, "Burj", UIKit.Hex(i % 2 == 0 ? "#CFD8DC" : "#B0BEC5"), 0.5f - ww[i] / 2f, y, 0.5f + ww[i] / 2f, y + h, false);
                y += h;
            }
            UIKit.Panel(area, "Spire", UIKit.Hex("#ECEFF1"), 0.498f, y, 0.502f, y + 0.1f, false);
            var tip = UIKit.Shape(area, UIKit.Circle, UIKit.Red, 0.5f, y + 0.1f, 10f, 10f);
            _twinkle.Add(tip);
            // солнце у горизонта и море
            UIKit.Shape(area, UIKit.CircleHD, new Color(1f, 0.8f, 0.4f, 0.35f), 0.82f, 0.45f, 240f, 240f);
            UIKit.Shape(area, UIKit.CircleHD, UIKit.Hex("#FFCA28"), 0.82f, 0.45f, 130f, 130f);
            var sea = UIKit.Rect(area, "Sea");
            UIKit.Place(sea, 0, 0.15f, 1, 0.32f);
            var img = sea.gameObject.AddComponent<Image>();
            img.sprite = UIKit.Gradient(UIKit.Hex("#26A69A"), UIKit.Hex("#80CBC4"));
            img.raycastTarget = false;
            for (int i = 0; i < 5; i++)
                UIKit.Shape(sea, UIKit.RoundedSmall, new Color(1, 1, 1, 0.35f), Random.Range(0.05f, 0.95f), 0.25f + i * 0.14f, Random.Range(60f, 140f), 5f);
            // яхта
            var yacht = UIKit.Rect(area, "Yacht");
            UIKit.PlaceFixed(yacht, 0.3f, 0.24f, 160f, 60f);
            UIKit.Shape(yacht, UIKit.Rounded, Color.white, 0.5f, 0.35f, 160f, 26f);
            UIKit.Shape(yacht, UIKit.RoundedSmall, UIKit.Hex("#ECEFF1"), 0.45f, 0.65f, 80f, 20f);
            UIKit.Shape(yacht, UIKit.RoundedSmall, UIKit.Hex("#263238"), 0.45f, 0.65f, 60f, 6f);
            _clouds.Add(new Mover { rt = yacht, x = 0.3f, y = 0.24f, vx = 0.01f });
            // пальмы
            Palm(area, 0.07f, 0.03f);
            Palm(area, 0.93f, -0.03f);
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
