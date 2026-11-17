using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Иконки, нарисованные простыми фигурами. Когда появится настоящий арт —
    /// заменяем содержимое этих методов на Image со спрайтом, остальной код не меняется.
    /// </summary>
    public static class Icons
    {
        static RectTransform Box(Transform parent, string name, float ax, float ay, float size)
        {
            var c = UIKit.Rect(parent, name);
            UIKit.PlaceFixed(c, ax, ay, size, size);
            return c;
        }

        // фигура внутри иконки: координаты и размеры — доли от размера иконки
        static Image S(RectTransform box, Sprite sp, string hex, float cx, float cy, float w, float h, float rot = 0f)
        {
            float size = box.sizeDelta.x;
            return UIKit.Shape(box, sp, UIKit.Hex(hex), cx, cy, w * size, h * size, rot);
        }

        static Image S(RectTransform box, Sprite sp, Color c, float cx, float cy, float w, float h, float rot = 0f)
        {
            float size = box.sizeDelta.x;
            return UIKit.Shape(box, sp, c, cx, cy, w * size, h * size, rot);
        }

        public static RectTransform Ingredient(Transform parent, Ing id, float ax, float ay, float size)
        {
            var b = Box(parent, "Icon_" + id, ax, ay, size);
            var R = UIKit.RoundedSmall;
            var C = UIKit.Circle;
            switch (id)
            {
                case Ing.Meat:
                    S(b, R, "#7A2E12", 0.5f, 0.36f, 0.86f, 0.24f, -10f);
                    S(b, R, "#A8461F", 0.5f, 0.52f, 0.82f, 0.24f, -10f);
                    S(b, R, "#C8622E", 0.5f, 0.67f, 0.74f, 0.22f, -10f);
                    S(b, R, "#E89A62", 0.42f, 0.71f, 0.36f, 0.06f, -10f);
                    break;
                case Ing.Tomato:
                    S(b, C, "#B7281F", 0.5f, 0.44f, 0.84f, 0.78f);
                    S(b, C, "#E0413A", 0.48f, 0.46f, 0.76f, 0.7f);
                    S(b, C, "#FFFFFF80", 0.34f, 0.6f, 0.16f, 0.12f);
                    S(b, R, "#2E7D32", 0.5f, 0.82f, 0.34f, 0.1f, 30f);
                    S(b, R, "#2E7D32", 0.5f, 0.82f, 0.34f, 0.1f, -30f);
                    break;
                case Ing.Cucumber:
                    S(b, C, "#1E5E20", 0.5f, 0.5f, 0.88f, 0.88f);
                    S(b, C, "#CFE8A9", 0.5f, 0.5f, 0.74f, 0.74f);
                    S(b, C, "#A5CF6E", 0.5f, 0.5f, 0.4f, 0.4f);
                    S(b, C, "#F4FAE8", 0.42f, 0.55f, 0.08f, 0.08f);
                    S(b, C, "#F4FAE8", 0.58f, 0.55f, 0.08f, 0.08f);
                    S(b, C, "#F4FAE8", 0.5f, 0.42f, 0.08f, 0.08f);
                    break;
                case Ing.Cabbage:
                    S(b, C, "#5E9A2E", 0.5f, 0.5f, 0.88f, 0.84f);
                    S(b, C, "#9CCB5E", 0.5f, 0.5f, 0.7f, 0.66f);
                    S(b, C, "#D4EBAF", 0.5f, 0.5f, 0.42f, 0.4f);
                    S(b, R, "#F1F8E4", 0.5f, 0.5f, 0.06f, 0.7f, 20f);
                    break;
                case Ing.Cheese:
                    S(b, R, "#D9A400", 0.5f, 0.42f, 0.86f, 0.52f);
                    S(b, R, "#F7D44C", 0.5f, 0.46f, 0.8f, 0.46f);
                    S(b, C, "#D9A400", 0.32f, 0.5f, 0.14f, 0.14f);
                    S(b, C, "#D9A400", 0.62f, 0.4f, 0.1f, 0.1f);
                    S(b, C, "#D9A400", 0.72f, 0.56f, 0.08f, 0.08f);
                    break;
                case Ing.Onion:
                    // кольца маринованного лука
                    S(b, UIKit.Circle, "#8E24AA", 0.38f, 0.55f, 0.56f, 0.56f);
                    S(b, UIKit.Circle, "#F3E5F5", 0.38f, 0.55f, 0.4f, 0.4f);
                    S(b, UIKit.Circle, "#CE93D8", 0.38f, 0.55f, 0.3f, 0.3f);
                    S(b, UIKit.Circle, "#F3E5F5", 0.38f, 0.55f, 0.16f, 0.16f);
                    S(b, UIKit.Circle, "#AB47BC", 0.66f, 0.4f, 0.46f, 0.46f);
                    S(b, UIKit.Circle, "#F3E5F5", 0.66f, 0.4f, 0.32f, 0.32f);
                    S(b, UIKit.Circle, "#CE93D8", 0.66f, 0.4f, 0.22f, 0.22f);
                    S(b, UIKit.Circle, "#F3E5F5", 0.66f, 0.4f, 0.1f, 0.1f);
                    break;
                case Ing.Fries:
                    // горка картошки БЕЗ коробки — это начинка в шаурму (коробка фри — отдельный гарнир)
                    S(b, R, "#E8B321", 0.28f, 0.4f, 0.1f, 0.5f, 35f);
                    S(b, R, "#F4C542", 0.4f, 0.46f, 0.1f, 0.58f, 12f);
                    S(b, R, "#FFD867", 0.52f, 0.48f, 0.1f, 0.6f, -8f);
                    S(b, R, "#F4C542", 0.64f, 0.44f, 0.1f, 0.54f, -25f);
                    S(b, R, "#E8B321", 0.74f, 0.38f, 0.1f, 0.44f, -40f);
                    S(b, R, "#FFD867", 0.46f, 0.3f, 0.5f, 0.1f, 80f);
                    S(b, R, "#F4C542", 0.56f, 0.24f, 0.5f, 0.1f, 95f);
                    break;
                case Ing.Garlic:
                    Bottle(b, "#F2EDDC", "#8D8D8D", "#C9B98F");
                    break;
                case Ing.Spicy:
                    Bottle(b, "#C62828", "#3E3E3E", "#FFB300");
                    break;
            }
            return b;
        }

        static void Bottle(RectTransform b, string body, string cap, string label)
        {
            var R = UIKit.RoundedSmall;
            S(b, R, cap, 0.5f, 0.86f, 0.14f, 0.16f);
            S(b, R, cap, 0.5f, 0.76f, 0.26f, 0.08f);
            S(b, R, "#00000040", 0.53f, 0.38f, 0.48f, 0.64f);
            S(b, R, body, 0.5f, 0.4f, 0.48f, 0.64f);
            S(b, R, label, 0.5f, 0.38f, 0.48f, 0.22f);
            S(b, R, "#FFFFFF50", 0.38f, 0.45f, 0.06f, 0.4f);
        }

        /// <summary>Коробка для картошки фри: пустая или с картошкой (fill 0..1).</summary>
        public static RectTransform FriesBox(Transform parent, float ax, float ay, float size, float fill)
        {
            var b = Box(parent, "FriesBox", ax, ay, size);
            var R = UIKit.RoundedSmall;
            // палочки картошки торчат из коробки
            if (fill > 0f)
            {
                float[] xs = { 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.35f, 0.55f, 0.65f };
                float[] rs = { 10f, -4f, 6f, -8f, 12f, -12f, 2f, -3f };
                int n = Mathf.Clamp(Mathf.CeilToInt(fill * xs.Length), 1, xs.Length);
                for (int i = 0; i < n; i++)
                {
                    float hgt = 0.34f + (i % 3) * 0.05f;
                    S(b, R, i % 2 == 0 ? "#F4C542" : "#E8B321", xs[i], 0.62f + (i % 3) * 0.03f, 0.08f, hgt, rs[i]);
                }
            }
            S(b, R, "#8E1B12", 0.52f, 0.33f, 0.62f, 0.52f);
            S(b, R, "#D32F2F", 0.5f, 0.35f, 0.62f, 0.52f);
            S(b, R, "#FFC928", 0.5f, 0.35f, 0.62f, 0.08f);
            S(b, UIKit.Circle, "#FFC928", 0.5f, 0.24f, 0.18f, 0.12f);
            S(b, R, "#FFFFFF40", 0.28f, 0.38f, 0.05f, 0.4f);
            return b;
        }

        public static RectTransform DrinkIcon(Transform parent, Drink d, float ax, float ay, float size)
        {
            if (d == Drink.Fries) return FriesBox(parent, ax, ay, size, 1f);
            if (d == Drink.Cola || d == Drink.Juice) return DrinkCup(parent, d, ax, ay, size, 1f);
            var b = Box(parent, "Drink_" + d, ax, ay, size);
            var R = UIKit.RoundedSmall;
            if (d == Drink.Cola)
            {
                S(b, R, "#8E1B12", 0.53f, 0.45f, 0.5f, 0.78f);
                S(b, R, "#C0392B", 0.5f, 0.47f, 0.5f, 0.78f);
                S(b, R, "#BDC3C7", 0.5f, 0.86f, 0.44f, 0.06f);
                S(b, R, "#FFFFFF", 0.5f, 0.46f, 0.5f, 0.08f, -12f);
                S(b, R, "#FFFFFF40", 0.36f, 0.5f, 0.06f, 0.56f);
            }
            else
            {
                S(b, R, "#B0BEC5", 0.53f, 0.43f, 0.5f, 0.72f);
                S(b, R, "#F5F7FA", 0.5f, 0.45f, 0.5f, 0.72f);
                S(b, R, "#3B82C4", 0.5f, 0.42f, 0.5f, 0.2f);
                S(b, R, "#FFFFFF", 0.5f, 0.82f, 0.54f, 0.07f);
            }
            return b;
        }

        public static RectTransform Lavash(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Lavash", ax, ay, size);
            var C = UIKit.Circle;
            S(b, C, "#D9B26F", 0.5f, 0.47f, 0.92f, 0.92f);
            S(b, C, "#F3D9A4", 0.5f, 0.5f, 0.9f, 0.9f);
            S(b, C, "#E2BE7E", 0.35f, 0.62f, 0.12f, 0.1f);
            S(b, C, "#E2BE7E", 0.64f, 0.4f, 0.14f, 0.1f);
            S(b, C, "#E2BE7E", 0.62f, 0.68f, 0.08f, 0.07f);
            return b;
        }

        /// <summary>Шаурма в фольге. Возвращает картинку "булки" — её цвет меняем по степени прожарки.</summary>
        public static Image Shawarma(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Shawarma", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#00000030", 0.52f, 0.46f, 0.9f, 0.44f, -8f);
            var roll = S(b, UIKit.Rounded, "#F3E0B5", 0.5f, 0.5f, 0.9f, 0.42f, -8f);
            roll.gameObject.name = "Roll";
            S(b, R, "#FFFFFF30", 0.45f, 0.6f, 0.6f, 0.06f, -8f);
            // фольга
            S(b, R, "#CFD8DC", 0.26f, 0.47f, 0.34f, 0.44f, -8f);
            S(b, R, "#ECEFF1", 0.24f, 0.52f, 0.26f, 0.08f, -8f);
            // начинка торчит
            S(b, UIKit.Circle, "#B5542D", 0.92f, 0.44f, 0.12f, 0.14f);
            S(b, UIKit.Circle, "#3E9B3E", 0.9f, 0.53f, 0.08f, 0.08f);
            return roll;
        }

        public static RectTransform Coin(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Coin", ax, ay, size);
            var C = UIKit.Circle;
            S(b, C, "#C98A00", 0.52f, 0.47f, 0.96f, 0.96f);
            S(b, C, "#FFC928", 0.5f, 0.5f, 0.94f, 0.94f);
            S(b, C, "#FFE07A", 0.5f, 0.5f, 0.64f, 0.64f);
            S(b, UIKit.RoundedSmall, "#E0A800", 0.5f, 0.5f, 0.14f, 0.42f);
            return b;
        }

        public static RectTransform Star(Transform parent, float ax, float ay, float size, bool on)
        {
            var b = Box(parent, "Star", ax, ay, size);
            string col = on ? "#FFC928" : "#00000040";
            // звезда из двух повернутых квадратов + круг (стилизованно)
            S(b, UIKit.RoundedSmall, col, 0.5f, 0.5f, 0.62f, 0.62f, 0f);
            S(b, UIKit.RoundedSmall, col, 0.5f, 0.5f, 0.62f, 0.62f, 45f);
            if (on) S(b, UIKit.Circle, "#FFE07A", 0.5f, 0.5f, 0.3f, 0.3f);
            return b;
        }

        public static Color Toast(float k)
        {
            // 0 = сырая, 1 = идеально, >1 = горит
            var raw = UIKit.Hex("#F3E0B5");
            var gold = UIKit.Hex("#D99A4E");
            var burnt = UIKit.Hex("#3A2618");
            if (k <= 1f) return Color.Lerp(raw, gold, k);
            return Color.Lerp(gold, burnt, Mathf.Clamp01(k - 1f));
        }
            // ---------------- v0.4: упаковка, мета, магазин ----------------
        /// <summary>Крафт-пакет «с собой»: загнутый верх, ручки-верёвочки, наклейка с логотипом.</summary>
        public static RectTransform Bag(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Bag", ax, ay, size);
            var R = UIKit.RoundedSmall;
            // ручки
            S(b, R, "#6D4C41", 0.38f, 0.8f, 0.05f, 0.26f, 12f);
            S(b, R, "#6D4C41", 0.62f, 0.8f, 0.05f, 0.26f, -12f);
            S(b, R, "#6D4C41", 0.5f, 0.92f, 0.28f, 0.05f);
            // корпус с тенью и боковой складкой
            S(b, R, "#00000033", 0.53f, 0.38f, 0.62f, 0.64f);
            S(b, R, "#C9955E", 0.5f, 0.4f, 0.62f, 0.64f);
            S(b, R, "#B3824F", 0.23f, 0.4f, 0.08f, 0.64f);
            S(b, R, "#B3824F", 0.77f, 0.4f, 0.08f, 0.64f);
            // загнутый верх
            S(b, R, "#A8743F", 0.5f, 0.7f, 0.62f, 0.07f);
            // наклейка
            S(b, UIKit.Circle, "#C62828", 0.5f, 0.4f, 0.3f, 0.3f);
            S(b, UIKit.Circle, "#FFC928", 0.5f, 0.4f, 0.22f, 0.22f);
            S(b, R, "#C62828", 0.5f, 0.4f, 0.1f, 0.03f, 30f);
            return b;
        }

        public static RectTransform Paper(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Paper", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#BDBDBD", 0.5f, 0.5f, 0.7f, 0.5f, -10f);
            S(b, R, "#FAFAFA", 0.48f, 0.53f, 0.7f, 0.5f, -10f);
            S(b, R, "#E53935", 0.48f, 0.53f, 0.7f, 0.08f, -10f);
            return b;
        }

        /// <summary>«Здесь»: шаурма, завёрнутая в фирменную бумагу (вместо тарелки — не сливается с фоном).</summary>
        public static RectTransform Plate(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Wrapped", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#00000033", 0.54f, 0.47f, 0.34f, 0.84f, -20f);
            S(b, UIKit.Rounded, "#C8793D", 0.54f, 0.64f, 0.3f, 0.34f, -20f);       // видна шаурма сверху
            S(b, UIKit.Circle, "#E0413A", 0.5f, 0.76f, 0.08f, 0.08f);
            S(b, UIKit.Circle, "#3E9B3E", 0.6f, 0.74f, 0.07f, 0.07f);
            S(b, R, "#FAFAFA", 0.46f, 0.36f, 0.36f, 0.56f, -20f);                 // бумага
            S(b, R, "#E53935", 0.44f, 0.4f, 0.36f, 0.08f, -20f);                  // красная полоса
            S(b, R, "#E53935", 0.41f, 0.25f, 0.36f, 0.05f, -20f);
            return b;
        }

        /// <summary>Пластиковый стакан с крышкой и трубочкой. fill 0..1 — сколько налито.</summary>
        public static RectTransform DrinkCup(Transform parent, Drink d, float ax, float ay, float size, float fill)
        {
            var b = Box(parent, "Cup_" + d, ax, ay, size);
            var R = UIKit.RoundedSmall;
            string liquid = d == Drink.Juice ? "#FB8C00" : "#5D1F12";
            string band = d == Drink.Juice ? "#43A047" : "#E53935";
            S(b, R, "#00000026", 0.53f, 0.42f, 0.5f, 0.7f);
            S(b, R, "#E3F2FD", 0.5f, 0.44f, 0.5f, 0.7f);                           // стакан
            if (fill > 0f)
            {
                float h = 0.64f * Mathf.Clamp01(fill);
                S(b, R, liquid, 0.5f, 0.12f + h / 2f, 0.44f, h);
            }
            if (d != Drink.None && fill >= 1f)
            {
                S(b, R, band, 0.5f, 0.42f, 0.5f, 0.12f);                           // фирменная полоса
                S(b, R, "#FAFAFA", 0.5f, 0.81f, 0.56f, 0.07f);                     // крышка
                S(b, R, band, 0.6f, 0.93f, 0.05f, 0.26f, -15f);                    // трубочка
            }
            S(b, R, "#FFFFFF55", 0.33f, 0.46f, 0.05f, 0.56f);
            return b;
        }

        public static RectTransform Question(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Question", ax, ay, size);
            S(b, UIKit.Circle, "#8E5CC4", 0.5f, 0.5f, 0.95f, 0.95f);
            var t = UIKit.FitLabel(b, "?", (int)(size * 0.8f), Color.white, 0f, 0f, 1f, 1f);
            t.resizeTextMaxSize = (int)(size * 0.8f);
            return b;
        }

        public static RectTransform Cup(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Cup", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, UIKit.Circle, "#6D4C41", 0.78f, 0.5f, 0.28f, 0.3f);
            S(b, UIKit.Circle, "#FFFFFF", 0.78f, 0.5f, 0.16f, 0.18f);
            S(b, R, "#FFFFFF", 0.46f, 0.45f, 0.6f, 0.6f);
            S(b, R, "#6D4C41", 0.46f, 0.7f, 0.56f, 0.1f);
            S(b, R, "#FFFFFF80", 0.4f, 0.9f, 0.05f, 0.16f, 10f);
            S(b, R, "#FFFFFF80", 0.52f, 0.92f, 0.05f, 0.16f, -10f);
            return b;
        }

        public static RectTransform Flyer(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Flyer", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#FFF59D", 0.5f, 0.5f, 0.66f, 0.84f, 8f);
            S(b, R, "#E53935", 0.5f, 0.72f, 0.5f, 0.12f, 8f);
            for (int i = 0; i < 3; i++) S(b, R, "#9E9E9E", 0.5f, 0.52f - i * 0.12f, 0.46f, 0.05f, 8f);
            return b;
        }

        public static RectTransform GoldLavash(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "GoldLavash", ax, ay, size);
            S(b, UIKit.Circle, "#C98A00", 0.52f, 0.47f, 0.92f, 0.92f);
            S(b, UIKit.Circle, "#FFD54F", 0.5f, 0.5f, 0.9f, 0.9f);
            S(b, UIKit.Circle, "#FFF3C4", 0.38f, 0.62f, 0.18f, 0.12f);
            S(b, UIKit.Circle, "#FFB300", 0.64f, 0.4f, 0.14f, 0.1f);
            return b;
        }

        public static RectTransform Radio(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Radio", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#424242", 0.7f, 0.82f, 0.04f, 0.4f, -30f);
            S(b, R, "#8D6E63", 0.5f, 0.45f, 0.9f, 0.6f);
            S(b, UIKit.Circle, "#3E2723", 0.32f, 0.45f, 0.36f, 0.36f);
            S(b, UIKit.Circle, "#6D4C41", 0.32f, 0.45f, 0.2f, 0.2f);
            S(b, R, "#FFE082", 0.7f, 0.55f, 0.26f, 0.12f);
            S(b, UIKit.Circle, "#212121", 0.64f, 0.35f, 0.08f, 0.08f);
            S(b, UIKit.Circle, "#212121", 0.78f, 0.35f, 0.08f, 0.08f);
            return b;
        }

        public static RectTransform Jar(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Jar", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#90A4AE", 0.5f, 0.84f, 0.5f, 0.1f);
            S(b, R, "#B3E5FC80", 0.5f, 0.45f, 0.66f, 0.72f);
            S(b, UIKit.Circle, "#FFC928", 0.4f, 0.25f, 0.22f, 0.22f);
            S(b, UIKit.Circle, "#FFB300", 0.6f, 0.28f, 0.22f, 0.22f);
            S(b, UIKit.Circle, "#FFC928", 0.5f, 0.42f, 0.22f, 0.22f);
            S(b, R, "#FFFFFF60", 0.32f, 0.5f, 0.05f, 0.5f);
            return b;
        }

        public static RectTransform Book(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Book", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#5D4037", 0.5f, 0.48f, 0.84f, 0.64f);
            S(b, R, "#FFF8E1", 0.29f, 0.5f, 0.38f, 0.58f, -3f);
            S(b, R, "#FFF8E1", 0.71f, 0.5f, 0.38f, 0.58f, 3f);
            for (int i = 0; i < 3; i++)
            {
                S(b, R, "#BCAAA4", 0.29f, 0.62f - i * 0.12f, 0.26f, 0.04f);
                S(b, R, "#BCAAA4", 0.71f, 0.62f - i * 0.12f, 0.26f, 0.04f);
            }
            return b;
        }

        public static RectTransform Trophy(Transform parent, float ax, float ay, float size, bool on)
        {
            var b = Box(parent, "Trophy", ax, ay, size);
            string c = on ? "#FFC928" : "#9E9E9E", d = on ? "#C98A00" : "#757575";
            var R = UIKit.RoundedSmall;
            S(b, R, d, 0.5f, 0.14f, 0.56f, 0.12f);
            S(b, R, c, 0.5f, 0.3f, 0.14f, 0.24f);
            S(b, UIKit.Circle, c, 0.22f, 0.66f, 0.24f, 0.3f);
            S(b, UIKit.Circle, c, 0.78f, 0.66f, 0.24f, 0.3f);
            S(b, UIKit.Circle, c, 0.5f, 0.6f, 0.56f, 0.56f);
            S(b, R, c, 0.5f, 0.78f, 0.56f, 0.24f);
            S(b, R, "#FFFFFF50", 0.4f, 0.66f, 0.06f, 0.3f);
            return b;
        }

        public static RectTransform People(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "People", ax, ay, size);
            S(b, UIKit.Circle, "#90A4AE", 0.32f, 0.62f, 0.26f, 0.26f);
            S(b, UIKit.Rounded, "#90A4AE", 0.32f, 0.3f, 0.4f, 0.36f);
            S(b, UIKit.Circle, "#546E7A", 0.64f, 0.66f, 0.3f, 0.3f);
            S(b, UIKit.Rounded, "#546E7A", 0.64f, 0.32f, 0.46f, 0.4f);
            return b;
        }

        public static RectTransform Clock(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Clock", ax, ay, size);
            S(b, UIKit.Circle, "#455A64", 0.5f, 0.5f, 0.9f, 0.9f);
            S(b, UIKit.Circle, "#FFFFFF", 0.5f, 0.5f, 0.76f, 0.76f);
            var h1 = S(b, UIKit.RoundedSmall, "#263238", 0.5f, 0.5f, 0.06f, 0.3f);
            h1.rectTransform.pivot = new Vector2(0.5f, 0f);
            var h2 = S(b, UIKit.RoundedSmall, "#E53935", 0.5f, 0.5f, 0.05f, 0.24f, -100f);
            h2.rectTransform.pivot = new Vector2(0.5f, 0f);
            return b;
        }

        // ---------------- v0.6 ----------------
        /// <summary>Дисковая пила (для автоматики). Вращать — через Spinner.</summary>
        public static RectTransform Saw(Transform parent, float ax, float ay, float size)
        {
            var b = Box(parent, "Saw", ax, ay, size);
            for (int i = 0; i < 12; i++)
                S(b, null, "#90A4AE", 0.5f, 0.5f, 0.16f, 1.0f, i * 15f);
            S(b, UIKit.Circle, "#CFD8DC", 0.5f, 0.5f, 0.86f, 0.86f);
            S(b, UIKit.Circle, "#ECEFF1", 0.5f, 0.5f, 0.6f, 0.6f);
            S(b, UIKit.Circle, "#546E7A", 0.5f, 0.5f, 0.2f, 0.2f);
            S(b, UIKit.RoundedSmall, "#FFFFFF80", 0.35f, 0.62f, 0.06f, 0.3f, 30f);
            return b;
        }

        public static RectTransform Chest(Transform parent, float ax, float ay, float size, bool open)
        {
            var b = Box(parent, "Chest", ax, ay, size);
            var R = UIKit.RoundedSmall;
            S(b, R, "#00000030", 0.52f, 0.3f, 0.84f, 0.5f);
            S(b, R, "#8D5524", 0.5f, 0.33f, 0.84f, 0.48f);
            if (open)
            {
                S(b, UIKit.Circle, "#FFE082", 0.5f, 0.62f, 0.7f, 0.4f);
                S(b, R, "#A0652E", 0.5f, 0.82f, 0.84f, 0.2f, -8f);
            }
            else S(b, R, "#A0652E", 0.5f, 0.64f, 0.84f, 0.28f);
            S(b, R, "#FFC928", 0.5f, 0.45f, 0.84f, 0.07f);
            S(b, R, "#FFC928", 0.5f, 0.45f, 0.14f, 0.2f);
            return b;
        }
    }
}
