using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Детальная отрисовка шаурмы: раскрытый лаваш со всей начинкой слоями
    /// (полоски мяса, кружки помидоров, кольца огурцов, соус зигзагом...).
    /// Раскладка детерминированная — при добавлении ингредиента старые не "прыгают".
    /// </summary>
    public static class ShawarmaView
    {
        // порядок слоёв: мясо снизу, соусы сверху
        static readonly Ing[] Layers = { Ing.Meat, Ing.Cabbage, Ing.Tomato, Ing.Cucumber, Ing.Onion, Ing.Fries, Ing.Cheese, Ing.Garlic, Ing.Spicy };

        /// <summary>Рисует раскрытый лаваш размером w×h с начинкой в центре (ax, ay) родителя.</summary>
        public static RectTransform DrawOpen(Transform parent, int[] counts, float ax, float ay, float w, float h)
        {
            var box = UIKit.Rect(parent, "OpenShawarma");
            UIKit.PlaceFixed(box, ax, ay, w, h);

            // лаваш: тень, основа, подпалины
            UIKit.Shape(box, UIKit.Circle, new Color(0, 0, 0, 0.18f), 0.51f, 0.46f, w, h);
            UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#E9C98B"), 0.5f, 0.5f, w, h);
            UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#F6E2B3"), 0.5f, 0.52f, w * 0.94f, h * 0.9f);
            var rnd0 = new System.Random(3);
            for (int i = 0; i < 7; i++)
                UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#E2BE7E"),
                    0.15f + (float)rnd0.NextDouble() * 0.7f, 0.2f + (float)rnd0.NextDouble() * 0.6f, 14f, 10f);

            float s = Mathf.Min(w / 280f, h / 160f); // масштаб деталей
            foreach (var ing in Layers)
            {
                int n = counts[(int)ing];
                for (int p = 0; p < n; p++) DrawPortion(box, ing, p, s);
            }
            return box;
        }

        static void DrawPortion(RectTransform box, Ing ing, int portion, float s)
        {
            var rnd = new System.Random(1000 + (int)ing * 97 + portion * 13);
            switch (ing)
            {
                case Ing.Meat:
                    string[] meatCols = { "#8E3A18", "#A8461F", "#C8622E", "#7A2E12" };
                    for (int i = 0; i < 4; i++)
                    {
                        var pos = InEllipse(rnd, 0.55f);
                        var strip = UIKit.Shape(box, UIKit.RoundedSmall, UIKit.Hex(meatCols[rnd.Next(meatCols.Length)]),
                            pos.x, pos.y, 50f * s, 13f * s, rnd.Next(-60, 60));
                        UIKit.Shape(strip.transform, UIKit.RoundedSmall, UIKit.Hex("#E89A62"), 0.4f, 0.75f, 20f * s, 3f * s);
                    }
                    break;
                case Ing.Tomato:
                    for (int i = 0; i < 2; i++)
                    {
                        var pos = InEllipse(rnd, 0.7f);
                        var t = UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#C62828"), pos.x, pos.y, 30f * s, 30f * s);
                        UIKit.Shape(t.transform, UIKit.Circle, UIKit.Hex("#EF5350"), 0.5f, 0.5f, 22f * s, 22f * s);
                        for (int k = 0; k < 3; k++)
                            UIKit.Shape(t.transform, UIKit.Circle, UIKit.Hex("#FFE082"), 0.5f + Mathf.Cos(k * 2.1f) * 0.2f, 0.5f + Mathf.Sin(k * 2.1f) * 0.2f, 4f * s, 5f * s);
                    }
                    break;
                case Ing.Cucumber:
                    for (int i = 0; i < 3; i++)
                    {
                        var pos = InEllipse(rnd, 0.7f);
                        var c = UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#1E5E20"), pos.x, pos.y, 24f * s, 24f * s);
                        UIKit.Shape(c.transform, UIKit.Circle, UIKit.Hex("#CFE8A9"), 0.5f, 0.5f, 18f * s, 18f * s);
                        UIKit.Shape(c.transform, UIKit.Circle, UIKit.Hex("#A5CF6E"), 0.5f, 0.5f, 8f * s, 8f * s);
                    }
                    break;
                case Ing.Cabbage:
                    for (int i = 0; i < 7; i++)
                    {
                        var pos = InEllipse(rnd, 0.75f);
                        UIKit.Shape(box, UIKit.RoundedSmall, UIKit.Hex(i % 2 == 0 ? "#C5E1A5" : "#9CCC65"), pos.x, pos.y, 28f * s, 5f * s, rnd.Next(0, 180));
                    }
                    break;
                case Ing.Cheese:
                    for (int i = 0; i < 10; i++)
                    {
                        var pos = InEllipse(rnd, 0.7f);
                        UIKit.Shape(box, UIKit.RoundedSmall, UIKit.Hex("#FFD54F"), pos.x, pos.y, 12f * s, 5f * s, rnd.Next(0, 180));
                    }
                    break;
                case Ing.Onion:
                    for (int i = 0; i < 3; i++)
                    {
                        var pos = InEllipse(rnd, 0.7f);
                        var j = UIKit.Shape(box, UIKit.Circle, UIKit.Hex("#8E24AA"), pos.x, pos.y, 20f * s, 20f * s);
                        UIKit.Shape(j.transform, UIKit.Circle, UIKit.Hex("#F3E5F5"), 0.5f, 0.5f, 14f * s, 14f * s);
                        UIKit.Shape(j.transform, UIKit.Circle, UIKit.Hex("#CE93D8"), 0.5f, 0.5f, 9f * s, 9f * s);
                    }
                    break;
                case Ing.Fries:
                    for (int i = 0; i < 5; i++)
                    {
                        var pos = InEllipse(rnd, 0.65f);
                        UIKit.Shape(box, UIKit.RoundedSmall, UIKit.Hex(i % 2 == 0 ? "#F4C542" : "#FFD867"), pos.x, pos.y, 34f * s, 8f * s, rnd.Next(-70, 70));
                    }
                    break;
                case Ing.Garlic:
                    Drizzle(box, rnd, UIKit.Hex("#FFF8E7"), s, 0.46f + portion * 0.1f);
                    break;
                case Ing.Spicy:
                    Drizzle(box, rnd, UIKit.Hex("#E53935"), s, 0.56f - portion * 0.1f);
                    break;
            }
        }

        /// <summary>Соус зигзагом через весь лаваш.</summary>
        static void Drizzle(RectTransform box, System.Random rnd, Color col, float s, float y)
        {
            int n = 7;
            for (int i = 0; i < n; i++)
            {
                float x = 0.2f + i * (0.6f / (n - 1));
                float dy = (i % 2 == 0 ? 0.06f : -0.06f);
                UIKit.Shape(box, UIKit.RoundedSmall, col, x, y + dy * 0.5f, 34f * s, 5f * s, i % 2 == 0 ? -35f : 35f);
            }
        }

        static Vector2 InEllipse(System.Random rnd, float r)
        {
            double a = rnd.NextDouble() * Mathf.PI * 2f;
            double d = System.Math.Sqrt(rnd.NextDouble()) * r;
            return new Vector2(0.5f + (float)(System.Math.Cos(a) * d) * 0.45f, 0.5f + (float)(System.Math.Sin(a) * d) * 0.4f);
        }

        /// <summary>Полоски гриля поверх свёрнутой шаурмы (ролл — картинка из Icons.Shawarma).</summary>
        public static UnityEngine.UI.Image[] GrillMarks(UnityEngine.UI.Image roll)
        {
            var marks = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                var m = UIKit.Panel(roll.transform, "Mark", new Color(0.25f, 0.12f, 0.05f, 0f), 0.3f + i * 0.2f, 0.12f, 0.36f + i * 0.2f, 0.88f, false);
                m.rectTransform.localRotation = Quaternion.Euler(0, 0, 20f);
                marks[i] = m;
            }
            return marks;
        }
    }
}
