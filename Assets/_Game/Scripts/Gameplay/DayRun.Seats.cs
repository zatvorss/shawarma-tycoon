using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Места у прилавка. Пока улучшение «Места у прилавка» не куплено, крайние места закрыты
    /// стенкой в стиле локации: кирпич во дворе, доски на рынке, бамбук на пляже, мрамор в небоскрёбе,
    /// золотая мозаика в Дубае... 2 места → 3 (с 3-й локации) → 4 (с 5-й локации).
    /// </summary>
    public partial class DayRun
    {
        /// <summary>Открыто ли место: 2 места — два средних, 3 — три левых, 4 — все.</summary>
        bool SlotOpen(int i)
        {
            if (_maxActive >= 4) return true;
            if (_maxActive == 3) return i <= 2;
            return i == 1 || i == 2;
        }

        void BuildSeatWalls()
        {
            for (int i = 0; i < _slots.Length; i++)
                if (!SlotOpen(i)) SeatWall(_slots[i]);
        }

        void SeatWall(RectTransform slot)
        {
            var wall = UIKit.Rect(slot, "SeatWall");
            UIKit.Place(wall, -0.03f, 0f, 1.03f, 1.02f);
            var bgImg = wall.gameObject.AddComponent<Image>();
            bgImg.raycastTarget = true;
            var w = wall.transform;

            switch (_save.location)
            {
                case 0:  bgImg.color = UIKit.Hex("#B5533C"); Bricks(w, "#C8654A", "#9E4631", 9, 4); break;          // двор — кирпич
                case 1:  bgImg.color = UIKit.Hex("#6D4C41"); Planks(w, "#8D6E63", "#A1887F", 5, true); break;       // рынок — доски
                case 2:  bgImg.color = UIKit.Hex("#90A4AE"); Tiles(w, "#CFD8DC", "#B0BEC5", 4, 8); break;          // вокзал — плитка
                case 3:  bgImg.color = UIKit.Hex("#8D6E3F"); Bamboo(w); break;                                     // пляж — бамбук
                case 4:  bgImg.color = UIKit.Hex("#8E7CC3"); Bricks(w, "#9C8AD0", "#7E6BB5", 9, 4); Posters(w); break; // кампус — крашеный кирпич
                case 5:  bgImg.color = UIKit.Hex("#1565C0"); Planks(w, "#1E88E5", "#FAFAFA", 6, false); break;     // стадион — щиты в цветах клуба
                case 6:  bgImg.color = UIKit.Alpha(UIKit.Hex("#B3E5FC"), 0.92f); Glass(w); break;                // аэропорт — матовое стекло
                case 7:  bgImg.color = UIKit.Hex("#2B2440"); Bricks(w, "#3A3157", "#241E38", 9, 4); Neon(w); break; // ночь — тёмный кирпич + неон
                case 8:  bgImg.color = UIKit.Hex("#D7B98E"); Planks(w, "#E3C9A3", "#CDAE80", 4, true); break;     // ТЦ — фанера «скоро открытие»
                case 9:  bgImg.color = UIKit.Hex("#ECEFF1"); Marble(w, "#B0BEC5"); break;                          // небоскрёб — мрамор
                case 10: bgImg.color = UIKit.Hex("#4E342E"); Planks(w, "#5D4037", "#6D4C41", 3, true); Crest(w); break; // гос. столовая — дубовые панели
                default: bgImg.color = UIKit.Hex("#C9A227"); Mosaic(w); break;                                    // Дубай — золотая мозаика
            }
            // рамка стенки
            UIKit.AddOutline(bgImg, UIKit.Alpha(Color.black, 0.35f), 4f);

            // табличка: закрыто + зелёный плюс
            var sign = UIKit.Panel(w, "Sign", UIKit.Alpha(UIKit.Hex("#1A120C"), 0.88f), 0.08f, 0.42f, 0.92f, 0.62f);
            UIKit.AddOutline(sign, UIKit.Gold, 2f);
            UIKit.FitLabel(sign.transform, L.T("Место закрыто", "Seat closed"), 26, UIKit.Cream, 0.05f, 0.5f, 0.95f, 0.94f);
            var def = GameData.Upgrades[(int)Up.Seats];
            int lvl = _save.Lvl(Up.Seats);
            bool reach = def.CanReach(lvl, _save.location);
            string hint = reach ? L.T("улучши «Места у прилавка»", "upgrade “Counter seats”")
                                : L.T("откроется на локации ", "unlocks at location ") + (def.LocFor(lvl) + 1);
            UIKit.FitLabel(sign.transform, hint, 18, UIKit.Alpha(UIKit.Gold, 0.9f), 0.04f, 0.08f, 0.96f, 0.44f);

            UIKit.MakeButton(bgImg, () =>
            {
                Sfx.Play(Sound.Click);
                Juice.Punch(sign.rectTransform, 0.12f, 0.25f);
                Toast(reach ? L.T("Больше мест — в «Моём ларьке»: зелёные плюсы прямо на ларьке", "More seats — in “My kiosk”: the green pluses right on the kiosk")
                            : L.T(lvl == 0 ? "Третье место откроется на локации " : "Четвёртое место откроется на локации ", "The next seat unlocks at location ") + (def.LocFor(lvl) + 1) + " — " + GameData.Locations[def.LocFor(lvl)].Name);
            });
        }

        // ---------------- узоры стенок ----------------
        static void Bricks(Transform w, string a, string b, int rows, int cols)
        {
            float h = 1f / rows;
            for (int r = 0; r < rows; r++)
            {
                float off = r % 2 == 0 ? 0f : 0.5f / cols;
                for (int c = -1; c <= cols; c++)
                {
                    float x0 = c / (float)cols + off + 0.012f, x1 = (c + 1) / (float)cols + off - 0.012f;
                    x0 = Mathf.Clamp01(x0); x1 = Mathf.Clamp01(x1);
                    if (x1 - x0 < 0.02f) continue;
                    UIKit.Panel(w, "Brick", UIKit.Hex((r + c) % 3 == 0 ? b : a), x0, r * h + 0.008f, x1, (r + 1) * h - 0.008f, false);
                }
            }
        }

        static void Planks(Transform w, string a, string b, int n, bool vertical)
        {
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n + 0.006f, t1 = (i + 1) / (float)n - 0.006f;
                var col = UIKit.Hex(i % 2 == 0 ? a : b);
                if (vertical)
                {
                    UIKit.Panel(w, "Plank", col, t0, 0f, t1, 1f, false);
                    UIKit.Shape(w, UIKit.Circle, UIKit.Alpha(Color.black, 0.35f), (t0 + t1) * 0.5f, 0.9f, 8f, 8f);
                    UIKit.Shape(w, UIKit.Circle, UIKit.Alpha(Color.black, 0.35f), (t0 + t1) * 0.5f, 0.1f, 8f, 8f);
                }
                else UIKit.Panel(w, "Plank", col, 0f, t0, 1f, t1, false);
            }
        }

        static void Tiles(Transform w, string a, string b, int cols, int rows)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    UIKit.Panel(w, "Tile", UIKit.Hex((r + c) % 2 == 0 ? a : b), c / (float)cols + 0.01f, r / (float)rows + 0.006f,
                        (c + 1) / (float)cols - 0.01f, (r + 1) / (float)rows - 0.006f, false);
        }

        static void Bamboo(Transform w)
        {
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n;
                var col = UIKit.Hex(i % 2 == 0 ? "#C9B26B" : "#B89D55");
                UIKit.Panel(w, "Stem", col, x - 0.06f, 0f, x + 0.06f, 1f, false);
                UIKit.Panel(w, "Shine", UIKit.Alpha(Color.white, 0.18f), x - 0.035f, 0f, x - 0.015f, 1f, false);
                for (int k = 0; k < 4; k++)
                {
                    float y = 0.14f + k * 0.24f + (i % 2) * 0.06f;
                    UIKit.Panel(w, "Node", UIKit.Hex("#8A7436"), x - 0.065f, y - 0.008f, x + 0.065f, y + 0.008f, false);
                }
            }
            UIKit.Panel(w, "Rope", UIKit.Hex("#6D4C41"), 0f, 0.8f, 1f, 0.82f, false);
            UIKit.Panel(w, "Rope", UIKit.Hex("#6D4C41"), 0f, 0.2f, 1f, 0.22f, false);
        }

        static void Glass(Transform w)
        {
            UIKit.Panel(w, "Frame", UIKit.Hex("#78909C"), 0.48f, 0f, 0.52f, 1f, false);
            for (int i = 0; i < 3; i++)
                UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Alpha(Color.white, 0.35f), 0.25f + i * 0.08f, 0.6f, 12f, 240f, -25f);
            var t = UIKit.Panel(w, "Gate", UIKit.Hex("#FFC107"), 0.1f, 0.78f, 0.9f, 0.9f, false);
            UIKit.FitLabel(t.transform, L.T("ВЫХОД ЗАКРЫТ", "GATE CLOSED"), 18, UIKit.Dark, 0.04f, 0.05f, 0.96f, 0.95f);
        }

        static void Posters(Transform w)
        {
            UIKit.Panel(w, "Poster", UIKit.Hex("#FFF59D"), 0.1f, 0.72f, 0.45f, 0.92f, false);
            UIKit.Panel(w, "Poster", UIKit.Hex("#80DEEA"), 0.55f, 0.1f, 0.9f, 0.3f, false);
        }

        static void Neon(Transform w)
        {
            var n = UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Hex("#FF4081"), 0.5f, 0.8f, 150f, 8f);
            n.gameObject.AddComponent<Blinker>().speed = 1.5f;
            var m = UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Hex("#40C4FF"), 0.5f, 0.2f, 150f, 8f);
            m.gameObject.AddComponent<Blinker>().speed = 1.1f;
        }

        static void Marble(Transform w, string vein)
        {
            for (int i = 0; i < 7; i++)
                UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Alpha(UIKit.Hex(vein), 0.55f), 0.15f + (i * 0.37f) % 0.8f, 0.1f + i * 0.13f, 140f, 3f, 20f + i * 23f);
            UIKit.Panel(w, "Seam", UIKit.Alpha(UIKit.Hex(vein), 0.6f), 0f, 0.5f, 1f, 0.505f, false);
            UIKit.Panel(w, "Gold", UIKit.Hex("#D4AF37"), 0f, 0.97f, 1f, 1f, false);
            UIKit.Panel(w, "Gold", UIKit.Hex("#D4AF37"), 0f, 0f, 1f, 0.03f, false);
        }

        static void Crest(Transform w)
        {
            UIKit.Panel(w, "Rail", UIKit.Hex("#3E2723"), 0f, 0.7f, 1f, 0.72f, false);
            UIKit.Shape(w, UIKit.CircleHD, UIKit.Hex("#D4AF37"), 0.5f, 0.84f, 60f, 60f);
            UIKit.Shape(w, UIKit.CircleHD, UIKit.Hex("#8D6E63"), 0.5f, 0.84f, 44f, 44f);
            Icons.Star(w, 0.5f, 0.84f, 30f, true);
        }

        static void Mosaic(Transform w)
        {
            const int cols = 5, rows = 10;
            string[] c = { "#D4AF37", "#E6C35C", "#B8912A", "#F2D675" };
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < cols; k++)
                {
                    float x = (k + 0.5f + (r % 2) * 0.5f) / cols, y = (r + 0.5f) / rows;
                    if (x > 1f) continue;
                    UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Hex(c[(r * 3 + k) % c.Length]), x, y, 30f, 30f, 45f);
                }
        }
    }
}
