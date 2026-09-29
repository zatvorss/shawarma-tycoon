using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Внешний вид станций по уровням: рамка (сталь → золото), и анимированная
    /// автоматика 4-го уровня — пила на манипуляторе, робо-рука, LED-таймер, насос.
    /// Используется и на кухне, и на экране улучшений.
    /// </summary>
    public static class StationLook
    {
        public static Color FrameColor(int lvl)
        {
            switch (lvl)
            {
                case 1: return UIKit.Hex("#B0BEC5");
                case 2: return UIKit.Hex("#FFD54F");
                case 3: return UIKit.Hex("#FFC928");
                default: return new Color(0, 0, 0, 0);
            }
        }

        /// <summary>Рамка вокруг станции в зависимости от уровня.</summary>
        public static void Frame(Graphic g, int lvl)
        {
            if (lvl <= 0) return;
            UIKit.AddOutline(g, FrameColor(lvl), lvl >= 3 ? 5f : 3f);
        }

        /// <summary>Анимированная автоматика станции (4-й уровень).</summary>
        public static RectTransform Auto(Transform parent, Up id, float ax, float ay, float size)
        {
            var holder = UIKit.Rect(parent, "Auto_" + id);
            UIKit.PlaceFixed(holder, ax, ay, size, size);
            var steel = UIKit.Hex("#607D8B");
            switch (id)
            {
                case Up.Spit:
                case Up.Knife:
                {
                    // манипулятор ходит вверх-вниз, пила крутится
                    var arm = UIKit.Full(holder, "Arm");
                    UIKit.Shape(arm, UIKit.RoundedSmall, steel, 0.9f, 0.5f, size * 0.55f, size * 0.14f);
                    UIKit.Shape(arm, UIKit.Circle, UIKit.Hex("#37474F"), 0.62f, 0.5f, size * 0.22f, size * 0.22f);
                    var saw = Icons.Saw(arm, 0.35f, 0.5f, size * 0.75f);
                    saw.gameObject.AddComponent<Spinner>().speed = -900f;
                    var bob = arm.gameObject.AddComponent<Bobber>();
                    bob.amp = size * (id == Up.Spit ? 0.9f : 0.25f);
                    bob.speed = id == Up.Spit ? 2.2f : 6f;
                    break;
                }
                case Up.Fryer:
                {
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#455A64"), 0.5f, 0.08f, size * 0.6f, size * 0.14f);
                    var rod = UIKit.Full(holder, "Rod");
                    UIKit.Shape(rod, UIKit.RoundedSmall, UIKit.Hex("#90A4AE"), 0.5f, 0.5f, size * 0.12f, size * 0.8f);
                    UIKit.Shape(rod, UIKit.RoundedSmall, steel, 0.25f, 0.88f, size * 0.5f, size * 0.1f);
                    UIKit.Shape(rod, UIKit.RoundedSmall, UIKit.Gold, 0.05f, 0.8f, size * 0.1f, size * 0.22f);
                    var b = rod.gameObject.AddComponent<Bobber>();
                    b.amp = size * 0.12f;
                    b.speed = 2.5f;
                    UIKit.Shape(holder, UIKit.Circle, UIKit.Hex("#FF5252"), 0.5f, 0.16f, size * 0.1f, size * 0.1f).gameObject.AddComponent<Blinker>();
                    break;
                }
                case Up.Grill:
                {
                    var led = UIKit.Panel(holder, "Led", UIKit.Hex("#1B1B1B"), 0.05f, 0.3f, 0.95f, 0.7f);
                    UIKit.AddOutline(led, steel, 2f);
                    var t = UIKit.FitLabel(led.transform, "AUTO", 30, UIKit.Hex("#FF5252"), 0.05f, 0.1f, 0.95f, 0.9f);
                    t.gameObject.AddComponent<Blinker>();
                    break;
                }
                case Up.Sauce:
                {
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#455A64"), 0.5f, 0.2f, size * 0.7f, size * 0.2f);
                    var piston = UIKit.Full(holder, "Piston");
                    UIKit.Shape(piston, UIKit.RoundedSmall, UIKit.Hex("#B0BEC5"), 0.5f, 0.55f, size * 0.14f, size * 0.5f);
                    UIKit.Shape(piston, UIKit.RoundedSmall, UIKit.Red, 0.5f, 0.82f, size * 0.5f, size * 0.14f);
                    var b = piston.gameObject.AddComponent<Bobber>();
                    b.amp = size * 0.1f;
                    b.speed = 7f;
                    break;
                }
                case Up.Pack:
                {
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#455A64"), 0.5f, 0.08f, size * 0.5f, size * 0.14f);
                    var arm = UIKit.Rect(holder, "Arm");
                    UIKit.PlaceFixed(arm, 0.5f, 0.12f, size * 0.14f, size * 0.8f);
                    arm.pivot = new Vector2(0.5f, 0f);
                    arm.anchoredPosition = Vector2.zero;
                    UIKit.Shape(arm, UIKit.RoundedSmall, UIKit.Hex("#90A4AE"), 0.5f, 0.5f, size * 0.12f, size * 0.8f);
                    Icons.Bag(arm, 0.5f, 1.05f, size * 0.45f);
                    var p = arm.gameObject.AddComponent<Pendulum>();
                    p.angle = 25f;
                    p.speed = 3f;
                    break;
                }
                case Up.FryBox:
                {
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#455A64"), 0.5f, 0.08f, size * 0.5f, size * 0.14f);
                    var arm = UIKit.Rect(holder, "Arm");
                    UIKit.PlaceFixed(arm, 0.5f, 0.12f, size * 0.14f, size * 0.8f);
                    arm.pivot = new Vector2(0.5f, 0f);
                    arm.anchoredPosition = Vector2.zero;
                    UIKit.Shape(arm, UIKit.RoundedSmall, UIKit.Hex("#90A4AE"), 0.5f, 0.5f, size * 0.12f, size * 0.8f);
                    Icons.FriesBox(arm, 0.5f, 1.05f, size * 0.5f, 1f);
                    var p = arm.gameObject.AddComponent<Pendulum>();
                    p.angle = 30f;
                    p.speed = 3.5f;
                    break;
                }
                case Up.Flies:
                {
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#311B92"), 0.5f, 0.5f, size * 0.8f, size * 0.8f);
                    var g = UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#B388FF"), 0.5f, 0.5f, size * 0.6f, size * 0.6f);
                    g.gameObject.AddComponent<Blinker>().speed = 3f;
                    for (int i = 0; i < 4; i++) UIKit.Shape(holder, null, UIKit.Hex("#311B92"), 0.28f + i * 0.15f, 0.5f, size * 0.04f, size * 0.6f);
                    break;
                }
                case Up.Board:
                {
                    var sp = UIKit.Rect(holder, "Sponge");
                    UIKit.PlaceFixed(sp, 0.5f, 0.5f, size * 0.6f, size * 0.4f);
                    UIKit.Shape(sp, UIKit.RoundedSmall, UIKit.Hex("#FDD835"), 0.5f, 0.4f, size * 0.55f, size * 0.28f);
                    UIKit.Shape(sp, UIKit.RoundedSmall, UIKit.Hex("#43A047"), 0.5f, 0.78f, size * 0.55f, size * 0.1f);
                    var sw = sp.gameObject.AddComponent<Swayer>();
                    sw.amp = size * 0.2f;
                    sw.speed = 5f;
                    break;
                }
                case Up.Fire:
                {
                    UIKit.Shape(holder, UIKit.Circle, Color.white, 0.5f, 0.6f, size * 0.7f, size * 0.7f);
                    UIKit.Shape(holder, UIKit.Circle, UIKit.Hex("#CFD8DC"), 0.5f, 0.6f, size * 0.45f, size * 0.45f);
                    UIKit.Shape(holder, UIKit.Circle, UIKit.Hex("#00E676"), 0.5f, 0.6f, size * 0.14f, size * 0.14f).gameObject.AddComponent<Blinker>().speed = 2f;
                    break;
                }
                default:
                {
                    // касса, которая сама спрашивает
                    UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#37474F"), 0.5f, 0.35f, size * 0.8f, size * 0.5f);
                    var scr = UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Hex("#80DEEA"), 0.5f, 0.4f, size * 0.6f, size * 0.3f);
                    UIKit.FitLabel(scr.transform, "?", 30, UIKit.Dark, 0f, 0f, 1f, 1f).gameObject.AddComponent<Blinker>();
                    break;
                }
            }
            return holder;
        }
    }
}
