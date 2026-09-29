using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Улучшения — вид внутрь твоего ларька. Нажми на вертел, доску с помидором, фритюр, гриль,
    /// соусы, лотки, упаковку, радио, банку для чаевых или книгу рецептов — откроется карточка улучшения.
    /// </summary>
    public partial class Game
    {
        struct Spot { public Up id; public float x, y, size; }

        static readonly Spot[] Spots =
        {
            new Spot { id = Up.Spit,     x = 0.075f, y = 0.315f, size = 180f },
            new Spot { id = Up.Knife,    x = 0.21f,  y = 0.315f, size = 180f },
            new Spot { id = Up.Fryer,    x = 0.345f, y = 0.315f, size = 180f },
            new Spot { id = Up.Grill,    x = 0.48f,  y = 0.315f, size = 180f },
            new Spot { id = Up.Sauce,    x = 0.615f, y = 0.315f, size = 180f },
            new Spot { id = Up.Bins,     x = 0.75f,  y = 0.315f, size = 180f },
            new Spot { id = Up.Pack,     x = 0.9f,   y = 0.315f, size = 180f },
            new Spot { id = Up.Patience, x = 0.245f, y = 0.675f, size = 130f },
            new Spot { id = Up.Price,    x = 0.77f,  y = 0.675f, size = 130f },
            new Spot { id = Up.Tips,     x = 0.875f, y = 0.675f, size = 130f },
        };

        static readonly Spot[] Spots2 =
        {
            new Spot { id = Up.Flies,  x = 0.155f, y = 0.33f, size = 165f },
            new Spot { id = Up.Board,  x = 0.3f,   y = 0.33f, size = 165f },
            new Spot { id = Up.Fire,   x = 0.445f, y = 0.33f, size = 165f },
            new Spot { id = Up.Queue,  x = 0.59f,  y = 0.33f, size = 165f },
            new Spot { id = Up.Grills, x = 0.735f, y = 0.33f, size = 165f },
            new Spot { id = Up.FryBox, x = 0.88f,  y = 0.33f, size = 165f },
        };

        int _shopTab;

        static bool Automatable(Up id) { return id <= Up.Sauce || id == Up.Pack || id == Up.Tips || id == Up.Flies || id == Up.Board || id == Up.Fire || id == Up.FryBox; }

        /// <summary>Цена улучшения растёт вместе с ценами локации: на дорогих локациях и шаурма дороже, и прокачка.</summary>
        int UpCost(Up id, int lvl)
        {
            var def = GameData.Upgrades[(int)id];
            return Mathf.RoundToInt(def.Cost(lvl) * _save.Loc.price / 10f) * 10;
        }

        /// <summary>Можно ли сейчас (на этой локации) купить следующий уровень.</summary>
        bool UpOpen(Up id)
        {
            var def = GameData.Upgrades[(int)id];
            return def.CanReach(_save.upgrades[(int)id], _save.location);
        }

        bool UpReady(Up id)
        {
            int lvl = _save.upgrades[(int)id];
            return UpOpen(id) && _save.money >= UpCost(id, lvl);
        }

        static string LocName(int idx)
        {
            idx = Mathf.Clamp(idx, 0, GameData.Locations.Length - 1);
            return GameData.Locations[idx].Name;
        }

        void ShowShop(int openId)
        {
            if (openId >= (int)Up.Flies) _shopTab = 1;
            else if (openId >= 0) _shopTab = 0;
            var s = NewScreen("Shop");

            BuildKioskInterior(s);

            // верх
            var top = UIKit.Panel(s, "Top", UIKit.Alpha(UIKit.Dark, 0.92f), 0.01f, 0.915f, 0.99f, 0.99f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Мой ларёк", "My kiosk"), 40, UIKit.Cream, TextAnchor.MiddleLeft, 0.02f, 0, 0.22f, 1, true);
            // какой экран сейчас
            var pageBox = UIKit.Panel(top.transform, "Page", UIKit.Alpha(Color.black, 0.3f), 0.2f, 0.1f, 0.42f, 0.9f);
            UIKit.FitLabel(pageBox.transform, (_shopTab == 0 ? L.T("Кухня", "Kitchen") : L.T("Порядок", "Order")) + "  " + (_shopTab + 1) + "/2",
                30, UIKit.Cream, 0.04f, 0.1f, 0.96f, 0.9f);
            int cap = UpgradeDef.CapAt(_save.location) + 1;
            UIKit.Label(top.transform, (_shopTab == 0
                    ? L.T("Нажми на предмет — откроется улучшение.", "Tap an item to upgrade it.")
                    : L.T("Мухи, грязь, пожары, очередь, грили и коробки фри", "Flies, dirt, fires, queue, grills and fries boxes")) +
                    "\n<size=18><color=#FFE082>" + L.T("На этой локации — максимум до ур. ", "Max level at this location: ") + cap + "</color></size>",
                22, UIKit.Gold, TextAnchor.MiddleCenter, 0.46f, 0, 0.81f, 1, true);
            Icons.Coin(top.transform, 0.83f, 0.5f, 46f);
            UIKit.Label(top.transform, _save.money.ToString(), 38, UIKit.Gold, TextAnchor.MiddleLeft, 0.85f, 0, 0.99f, 1, true);

            if (_shopTab == 0) foreach (var sp in Spots) BuildSpot(s, sp);
            else foreach (var sp in Spots2) BuildSpot(s, sp);

            // большая стрелка прямо в ларьке: → на экран «Порядок», ← обратно на «Кухню»
            int readyOther = 0;
            foreach (var sp2 in (_shopTab == 0 ? Spots2 : Spots)) if (UpReady(sp2.id)) readyOther++;
            BigArrow(s, _shopTab == 0, readyOther);

            // низ
            UIKit.Button(s, L.T("← На карту", "← To the map"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMap(); }, 0.01f, 0.015f, 0.2f, 0.1f, 36);
            UIKit.Button(s, L.T("Начать день ", "Start day ") + _save.day, UIKit.Orange, () => { Sfx.Play(Sound.Click); StartDayCinematic(); },
                0.78f, 0.015f, 0.99f, 0.1f, 42);

            // общий прогресс прокачки
            int have = 0, max = 0;
            for (int i = 0; i < GameData.Upgrades.Length; i++) { have += _save.upgrades[i]; max += GameData.Upgrades[i].MaxLevel; }
            var prog = UIKit.Panel(s, "Progress", UIKit.Alpha(UIKit.Dark, 0.88f), 0.3f, 0.02f, 0.7f, 0.095f);
            UIKit.Label(prog.transform, L.T("Прокачка ларька", "Kiosk upgrades"), 24, UIKit.Cream, TextAnchor.MiddleLeft, 0.03f, 0f, 0.35f, 1f, true);
            var pf = UIKit.Bar(prog.transform, UIKit.Alpha(Color.white, 0.15f), UIKit.Gold, 0.36f, 0.25f, 0.97f, 0.75f);
            UIKit.SetFill(pf, (float)have / max);
            UIKit.FitLabel(prog.transform, have + " / " + max, 22, Color.white, 0.36f, 0.25f, 0.97f, 0.75f);

            if (openId >= 0) UpgradePopup(s, (Up)openId, true);
        }

        /// <summary>Жирная стрелка-кнопка внутри ларька для перелистывания экранов улучшений.</summary>
        void BigArrow(RectTransform s, bool right, int badge)
        {
            float ax = right ? 0.955f : 0.045f;
            var holder = UIKit.Rect(s, right ? "ArrowRight" : "ArrowLeft");
            UIKit.PlaceFixed(holder, ax, 0.475f, 140f, 140f);
            var body = UIKit.Rect(holder, "Body");
            UIKit.PlaceFixed(body, 0.5f, 0.5f, 140f, 140f);
            UIKit.Shape(body, UIKit.CircleHD, new Color(0, 0, 0, 0.3f), 0.5f, 0.45f, 136f, 136f);
            UIKit.Shape(body, UIKit.CircleHD, UIKit.Hex("#3E2723"), 0.5f, 0.5f, 136f, 136f);
            UIKit.Shape(body, UIKit.CircleHD, UIKit.Orange, 0.5f, 0.5f, 120f, 120f);
            UIKit.Shape(body, UIKit.CircleHD, UIKit.Alpha(Color.white, 0.18f), 0.5f, 0.62f, 90f, 50f);
            float cx = right ? 0.47f : 0.53f, off = 0.15f, ang = right ? -45f : 45f;
            // тень шеврона
            UIKit.Shape(body, UIKit.RoundedSmall, UIKit.Alpha(UIKit.Dark, 0.35f), cx + 0.02f, 0.5f + off - 0.03f, 64f, 24f, ang);
            UIKit.Shape(body, UIKit.RoundedSmall, UIKit.Alpha(UIKit.Dark, 0.35f), cx + 0.02f, 0.5f - off - 0.03f, 64f, 24f, -ang);
            // сам шеврон
            UIKit.Shape(body, UIKit.RoundedSmall, Color.white, cx, 0.5f + off, 64f, 24f, ang);
            UIKit.Shape(body, UIKit.RoundedSmall, Color.white, cx, 0.5f - off, 64f, 24f, -ang);
            var sw = body.gameObject.AddComponent<Swayer>();
            sw.amp = 7f;
            sw.speed = 3f;

            var plate = UIKit.Panel(holder, "Plate", UIKit.Alpha(UIKit.Dark, 0.9f), -0.05f, -0.28f, 1.05f, -0.04f);
            UIKit.FitLabel(plate.transform, right ? L.T("Порядок", "Order") : L.T("Кухня", "Kitchen"), 24, UIKit.Cream, 0.05f, 0.08f, 0.95f, 0.92f);

            if (badge > 0)
            {
                var bd = UIKit.Shape(holder, UIKit.Circle, UIKit.Red, right ? 0.15f : 0.85f, 0.88f, 44f, 44f);
                UIKit.AddOutline(bd, Color.white, 2f);
                UIKit.FitLabel(bd.transform, badge.ToString(), 24, Color.white, 0f, 0f, 1f, 1f);
                bd.gameObject.AddComponent<Bobber>().amp = 4f;
            }
            UIKit.HitArea(holder, "Tap", () =>
            {
                _shopTab = right ? 1 : 0;
                Sfx.Play(Sound.Click);
                ShowShop(-1);
            }).transform.SetAsLastSibling();
        }

        /// <summary>Интерьер: деревянная стена, окно на улицу, меловое меню, полки, лампы, гирлянда, стальной фартук, стол и шкафчики.</summary>
        void BuildKioskInterior(RectTransform s)
        {
            // стена — доски
            UIKit.Panel(s, "Wall", UIKit.Hex("#8D5B3A"), 0, 0, 1, 1, false);
            for (int i = 0; i < 32; i++)
            {
                float x0 = i / 32f;
                UIKit.Panel(s, "Plank", UIKit.Hex(i % 3 == 0 ? "#9C6A47" : (i % 3 == 1 ? "#A87550" : "#94633F")), x0, 0.2f, x0 + 1f / 32f - 0.001f, 1f, false);
            }
            // потолочная балка и гирлянда
            UIKit.Panel(s, "Beam", UIKit.Hex("#4E342E"), 0, 0.9f, 1, 0.92f, false);
            string[] bulbs = { "#FFD54F", "#FF8A65", "#81D4FA", "#AED581", "#F48FB1" };
            for (int i = 0; i < 26; i++)
            {
                float x = (i + 0.5f) / 26f;
                float y = 0.885f - Mathf.Abs(Mathf.Sin(x * Mathf.PI * 3f)) * 0.03f;
                var b = UIKit.Shape(s, UIKit.Circle, UIKit.Hex(bulbs[i % bulbs.Length]), x, y, 14f, 18f);
                UIKit.Shape(b.transform, UIKit.Circle, UIKit.Alpha(Color.white, 0.5f), 0.4f, 0.6f, 5f, 5f);
            }

            // окно на улицу
            var win = UIKit.Rect(s, "Window");
            UIKit.Place(win, 0.31f, 0.56f, 0.69f, 0.85f);
            win.gameObject.AddComponent<RectMask2D>();
            Scenery.Build(win, _save.Loc, false);
            var frame = UIKit.Panel(s, "Frame", new Color(0, 0, 0, 0), 0.31f, 0.56f, 0.69f, 0.85f, false);
            UIKit.AddOutline(frame, UIKit.Hex("#3E2723"), 10f);
            UIKit.Panel(s, "Mullion", UIKit.Hex("#3E2723"), 0.4985f, 0.56f, 0.5015f, 0.85f, false);
            UIKit.Panel(s, "Sill", UIKit.Hex("#5D4037"), 0.3f, 0.54f, 0.7f, 0.565f);
            var kc = UIKit.Hex(_save.Loc.kiosk);
            for (int i = 0; i < 10; i++)
            {
                var col = Meta.Stripe(_save, _save.Loc, i);
                float x0 = 0.3f + i * 0.04f;
                UIKit.Panel(s, "Awning", col, x0, 0.845f, x0 + 0.04f, 0.875f, false);
                UIKit.Shape(s, UIKit.Circle, col, x0 + 0.02f, 0.845f, 76f, 22f);
            }

            // меловое меню
            var board = UIKit.Panel(s, "Chalk", UIKit.Hex("#5D4037"), 0.02f, 0.56f, 0.19f, 0.86f);
            UIKit.AddShadow(board, 8f, 0.4f);
            var chalk = UIKit.Panel(board.transform, "Green", UIKit.Hex("#2E4A3A"), 0.05f, 0.05f, 0.95f, 0.95f);
            UIKit.Label(chalk.transform, L.T("МЕНЮ", "MENU"), 40, UIKit.Hex("#FFF9C4"), TextAnchor.MiddleCenter, 0f, 0.8f, 1f, 0.98f, true);
            BuildChalkMenu(chalk.transform);

            // полки
            UIKit.Panel(s, "ShelfL", UIKit.Hex("#4E342E"), 0.2f, 0.6f, 0.295f, 0.615f, false);
            UIKit.Panel(s, "ShelfR", UIKit.Hex("#4E342E"), 0.715f, 0.6f, 0.93f, 0.615f, false);
            // цветок на полке
            UIKit.Panel(s, "Pot", UIKit.Hex("#BF360C"), 0.94f, 0.6f, 0.975f, 0.65f);
            for (int i = 0; i < 5; i++)
                UIKit.Shape(s, UIKit.Rounded, UIKit.Hex("#43A047"), 0.957f, 0.68f, 20f, 70f, -50f + i * 25f);

            // лампы
            float[] lx = { 0.16f, 0.5f, 0.84f };
            foreach (var x in lx)
            {
                UIKit.Shape(s, null, UIKit.Hex("#212121"), x, 0.905f, 3f, 40f);
                UIKit.Shape(s, UIKit.CircleHD, new Color(1f, 0.9f, 0.6f, 0.18f), x, 0.8f, 360f, 220f);
                UIKit.Shape(s, UIKit.RoundedSmall, UIKit.Hex("#1B5E20"), x, 0.875f, 80f, 26f);
                UIKit.Shape(s, UIKit.Circle, UIKit.Hex("#FFF8E1"), x, 0.862f, 26f, 18f);
            }

            // стальной фартук за станциями
            var apron = UIKit.Rect(s, "Apron");
            UIKit.Place(apron, 0f, 0.21f, 1f, 0.52f);
            var ai = apron.gameObject.AddComponent<Image>();
            ai.sprite = UIKit.Gradient(UIKit.Hex("#B0BEC5"), UIKit.Hex("#78909C"));
            ai.raycastTarget = false;
            for (int i = 0; i < 12; i++)
                UIKit.Panel(apron, "Seam", UIKit.Alpha(Color.black, 0.12f), i / 12f, 0f, i / 12f + 0.002f, 1f, false);
            UIKit.Panel(s, "ApronTop", UIKit.Hex("#546E7A"), 0f, 0.515f, 1f, 0.525f, false);

            // стол и шкафчики
            UIKit.Panel(s, "Worktop", UIKit.Hex("#CFD8DC"), 0f, 0.185f, 1f, 0.215f, false);
            UIKit.Panel(s, "WorktopEdge", UIKit.Hex("#90A4AE"), 0f, 0.175f, 1f, 0.188f, false);
            UIKit.Panel(s, "Cabinet", UIKit.Hex("#6D4C41"), 0f, 0f, 1f, 0.175f, false);
            for (int i = 0; i < 8; i++)
            {
                float x0 = i / 8f;
                UIKit.Panel(s, "Door", UIKit.Hex("#7B5646"), x0 + 0.008f, 0.02f, x0 + 1f / 8f - 0.008f, 0.16f);
                UIKit.Shape(s, UIKit.RoundedSmall, UIKit.Hex("#FFD54F"), x0 + 1f / 16f, 0.14f, 40f, 8f);
            }
        }

        /// <summary>Меловое меню с настоящими ценами: локация, книга рецептов, ранг, переезды, слава и талисманы.</summary>
        float MenuPriceMult()
        {
            float m = GameData.PriceMult(_save.Lvl(Up.Price)) * _save.Loc.price * GameData.RankPriceMult(_save.rank);
            m *= GameData.RebirthMult(_save.rebirths) * (1f + 0.1f * Meta.P(_save, Perk.Recipe));
            if (Meta.Has(_save, Meta.CharmCoin)) m *= 1.05f;
            if (Meta.Has(_save, Meta.CharmCrown)) m *= 1.15f;
            return m;
        }

        void BuildChalkMenu(Transform chalk)
        {
            float m = MenuPriceMult();
            int eday = _save.EffDay;
            int meat = GameData.Get(Ing.Meat).price, tom = GameData.Get(Ing.Tomato).price;
            var names = new System.Collections.Generic.List<string>();
            var prices = new System.Collections.Generic.List<int>();
            names.Add(L.T("Шаурма", "Shawarma")); prices.Add(GameData.LavashPrice + meat + tom);
            names.Add(L.T("Двойная", "Double")); prices.Add(GameData.LavashPrice + meat * 2 + tom);
            if (GameData.Unlocked(Ing.Cheese, eday)) { names.Add(L.T("С сыром", "Cheesy")); prices.Add(GameData.LavashPrice + meat + tom + GameData.Get(Ing.Cheese).price); }
            if (GameData.Unlocked(Drink.Fries, eday)) { names.Add(L.T("Фри", "Fries")); prices.Add(GameData.Get(Drink.Fries).price); }
            if (GameData.Unlocked(Drink.Cola, eday)) { names.Add(L.T("Кола", "Cola")); prices.Add(GameData.Get(Drink.Cola).price); }
            if (GameData.Unlocked(Drink.Ayran, eday)) { names.Add(L.T("Айран", "Ayran")); prices.Add(GameData.Get(Drink.Ayran).price); }
            int n = Mathf.Min(names.Count, 6);
            float h = 0.74f / 6f;
            for (int i = 0; i < n; i++)
            {
                float y1 = 0.78f - i * h, y0 = y1 - h;
                UIKit.FitLabel(chalk, names[i], 22, UIKit.Alpha(Color.white, 0.9f), 0.06f, y0, 0.55f, y1, false).alignment = TextAnchor.MiddleLeft;
                UIKit.Panel(chalk, "Dots", UIKit.Alpha(Color.white, 0.25f), 0.52f, y0 + h * 0.3f, 0.6f, y0 + h * 0.36f, false);
                UIKit.FitLabel(chalk, GameData.Money(Mathf.RoundToInt(prices[i] * m)), 22, UIKit.Hex("#FFF59D"), 0.6f, y0, 0.96f, y1, false).alignment = TextAnchor.MiddleRight;
            }
            UIKit.FitLabel(chalk, L.T("цены с учётом всех бонусов", "prices incl. all bonuses"), 14, UIKit.Alpha(Color.white, 0.55f), 0.04f, 0.0f, 0.96f, 0.06f, false);
        }

        void BuildSpot(RectTransform s, Spot sp)
        {
            var def = GameData.Upgrades[(int)sp.id];
            int lvl = _save.upgrades[(int)sp.id];
            bool maxed = lvl >= def.MaxLevel;
            bool open = UpOpen(sp.id);
            bool canBuy = !maxed && open && _save.money >= UpCost(sp.id, lvl);
            bool capped = !maxed && !open;

            var holder = UIKit.Rect(s, "Spot_" + sp.id);
            UIKit.PlaceFixed(holder, sp.x, sp.y, sp.size, sp.size);

            // подсветка-кружок под предметом
            if (canBuy)
            {
                var glow = UIKit.Shape(holder, UIKit.Circle, UIKit.Alpha(UIKit.Gold, 0.3f), 0.5f, 0.5f, sp.size * 1.1f, sp.size * 1.0f);
                glow.gameObject.AddComponent<Bobber>().amp = 2f;
            }
            // тень на столе
            UIKit.Shape(holder, UIKit.Circle, new Color(0, 0, 0, 0.18f), 0.5f, 0.08f, sp.size * 0.8f, sp.size * 0.14f);
            DrawStation(holder, sp.id, lvl, 0.5f, 0.5f, sp.size * 0.9f);

            // табличка: название + уровень
            var plate = UIKit.Panel(holder, "Plate", UIKit.Alpha(UIKit.Dark, 0.9f), -0.1f, -0.22f, 1.1f, -0.02f);
            UIKit.AddShadow(plate, 4f, 0.35f);
            UIKit.FitLabel(plate.transform, def.Name, 24, UIKit.Cream, 0.04f, 0.45f, 0.96f, 0.98f);
            for (int l = 0; l <= def.MaxLevel; l++)
                UIKit.Shape(plate.transform, UIKit.Circle, l <= lvl ? UIKit.Green : UIKit.Alpha(Color.white, 0.25f),
                    0.5f + (l - def.MaxLevel * 0.5f) * 0.14f, 0.24f, 14f, 14f);

            // значок "можно улучшить" / "максимум"
            if (canBuy)
            {
                var badge = UIKit.Rect(holder, "Badge");
                UIKit.PlaceFixed(badge, 0.88f, 0.9f, 48f, 48f);
                UIKit.Shape(badge, UIKit.Circle, UIKit.Green, 0.5f, 0.5f, 46f, 46f);
                UIKit.FitLabel(badge, "!", 34, Color.white, 0f, 0f, 1f, 1f);
                badge.gameObject.AddComponent<Bobber>().amp = 6f;
            }
            else if (maxed)
                Icons.Star(holder, 0.88f, 0.9f, 44f, true);
            else if (capped)
            {
                var lockB = UIKit.Rect(holder, "Lock");
                UIKit.PlaceFixed(lockB, 0.88f, 0.9f, 44f, 44f);
                UIKit.Shape(lockB, UIKit.Circle, UIKit.Alpha(UIKit.Dark, 0.85f), 0.5f, 0.5f, 44f, 44f);
                UIKit.Shape(lockB, UIKit.Circle, UIKit.Cream, 0.5f, 0.62f, 18f, 18f);
                UIKit.Shape(lockB, UIKit.Circle, UIKit.Alpha(UIKit.Dark, 0.95f), 0.5f, 0.62f, 10f, 10f);
                UIKit.Shape(lockB, UIKit.RoundedSmall, UIKit.Cream, 0.5f, 0.4f, 24f, 18f);
            }

            var id = sp.id;
            UIKit.HitArea(holder, "Tap", () =>
            {
                Sfx.Play(Sound.Click);
                Juice.Punch(holder, 0.15f, 0.25f);
                UpgradePopup((RectTransform)_screen.transform, id, false);
            }).transform.SetAsLastSibling();
        }

        void UpgradePopup(RectTransform s, Up id, bool justBought)
        {
            var def = GameData.Upgrades[(int)id];
            int lvl = _save.upgrades[(int)id];
            bool maxed = lvl >= def.MaxLevel;
            var c = UIKit.Popup(s, def.Name + "  •  " + L.T("уровень ", "level ") + (lvl + 1), UIKit.Purple, 0.16f, 0.12f, 0.84f, 0.88f, null);

            var pic = UIKit.Panel(c, "Pic", UIKit.Hex("#EDE6DA"), 0f, 0.2f, 0.36f, 1f);
            UIKit.AddShadow(pic, 5f, 0.25f);
            DrawStation(pic.transform, id, lvl, 0.5f, 0.64f, 240f);
            if (!maxed)
            {
                UIKit.Label(pic.transform, L.T("Следующий уровень:", "Next level:"), 22, UIKit.Wood, TextAnchor.MiddleLeft, 0.05f, 0.1f, 0.6f, 0.3f, true);
                DrawStation(pic.transform, id, lvl + 1, 0.8f, 0.2f, 110f);
            }
            if (justBought)
            {
                Juice.Punch(pic.rectTransform, 0.2f, 0.4f);
                Juice.Burst(pic.transform, UIKit.Gold, 14, 0.5f, 0.5f, 18f);
            }

            // все уровни
            int nRows = def.MaxLevel + 1;
            float rh = 0.8f / nRows;
            for (int l = 0; l <= def.MaxLevel; l++)
            {
                float y1 = 1f - l * rh, y0 = y1 - rh + 0.012f;
                bool done = l < lvl, now = l == lvl, next = l == lvl + 1;
                bool reach = l == 0 || _save.location >= def.LocFor(l - 1);
                bool legend = l == 4 && def.reqLoc == null;
                bool auto = l == 3 && Automatable(id) && def.reqLoc == null;
                Color bg = now ? UIKit.Hex("#FFE082") : (done ? UIKit.Hex("#C8E6C9") : (reach ? Color.white : UIKit.Hex("#D6D6D6")));
                if (legend && !done && !now && reach) bg = UIKit.Hex("#FFF3E0");
                var row = UIKit.Panel(c, "Lvl", bg, 0.39f, y0, 1f, y1);
                if (next && reach) UIKit.AddOutline(row, UIKit.Green, 3f);
                if (legend) UIKit.AddOutline(row, UIKit.Gold, 2f);
                UIKit.Shape(row.transform, UIKit.Circle, done || now ? UIKit.Green : UIKit.Alpha(UIKit.Dark, 0.25f), 0.06f, 0.5f, 46f, 46f);
                UIKit.FitLabel(row.transform, (l + 1).ToString(), 26, Color.white, 0.02f, 0.2f, 0.1f, 0.8f);
                string desc = def.LevelDesc(l);
                if (!reach)
                    desc += "\n<size=18><color=#6D4C41>" + L.T("откроется на локации ", "unlocks at location ") + (def.LocFor(l - 1) + 1) +
                            " — " + LocName(def.LocFor(l - 1)) + "</color></size>";
                UIKit.Label(row.transform, desc, 24, reach ? UIKit.Dark : UIKit.Alpha(UIKit.Dark, 0.6f), TextAnchor.MiddleLeft, 0.13f, 0f, 0.8f, 1f, now);
                string tag = now ? L.T("СЕЙЧАС", "NOW") : (next && reach ? L.T("ДАЛЕЕ", "NEXT") : "");
                Color tagCol = now ? UIKit.Orange : UIKit.Green;
                if (auto) { tag = L.T("АВТО", "AUTO"); tagCol = UIKit.Gold; }
                if (legend) { tag = L.T("ЛЕГЕНДА", "LEGEND"); tagCol = UIKit.Hex("#FFB300"); }
                if (!reach && !done && !now) { tag = L.T("ЗАКРЫТО", "LOCKED"); tagCol = UIKit.Gray; }
                if (tag.Length > 0)
                {
                    var t = UIKit.Panel(row.transform, "Tag", tagCol, 0.8f, 0.2f, 0.98f, 0.8f);
                    UIKit.FitLabel(t.transform, tag, 18, UIKit.Dark, 0.05f, 0.1f, 0.95f, 0.9f);
                }
            }

            if (maxed)
            {
                UIKit.Label(c, L.T("Максимальный уровень!", "Max level!"), 36, UIKit.Green, TextAnchor.MiddleCenter, 0.39f, 0f, 1f, 0.17f, true);
            }
            else
            {
                int cost = UpCost(id, lvl);
                if (!UpOpen(id))
                {
                    int need = def.LocFor(lvl);
                    var lk = UIKit.Panel(c, "Locked", UIKit.Gray, 0.39f, 0f, 1f, 0.17f);
                    UIKit.FitLabel(lk.transform, L.T("Следующий уровень — на локации ", "Next level — at location ") + (need + 1) + " (" + LocName(need) + ")",
                        30, Color.white, 0.03f, 0.1f, 0.97f, 0.9f);
                }
                else
                {
                    bool can = _save.money >= cost;
                    var b = UIKit.PriceButton(c, L.T("Улучшить", "Upgrade"), cost, can ? UIKit.Green : UIKit.Gray, () => BuyUpgrade(id), 0.39f, 0f, 1f, 0.17f, 38);
                    b.interactable = can;
                    if (!can)
                        UIKit.Label(c, L.T("Не хватает: ", "Need more: ") + GameData.Money(cost - _save.money), 24, UIKit.Red, TextAnchor.MiddleCenter, 0f, 0.08f, 0.36f, 0.18f, true);
                }
            }
            UIKit.Label(c, L.T("У тебя: ", "You have: ") + GameData.Money(_save.money), 26, UIKit.Dark, TextAnchor.MiddleCenter, 0f, 0f, 0.36f, 0.09f, true);
        }

        void BuyUpgrade(Up id)
        {
            var def = GameData.Upgrades[(int)id];
            int lvl = _save.upgrades[(int)id];
            if (lvl >= def.MaxLevel) return;
            int cost = UpCost(id, lvl);
            if (_save.money < cost || !UpOpen(id)) { Sfx.Play(Sound.Error); return; }
            _save.money -= cost;
            _save.upgrades[(int)id]++;
            SaveSystem.Save(_save);
            Sfx.Play(lvl + 1 >= 3 ? Sound.Fanfare : Sound.Coin);
            CheckAchievements();
            ShowShop((int)id);
        }

        /// <summary>Рисует станцию/предмет ларька (для экрана улучшений).</summary>
        static void DrawStation(Transform parent, Up id, int lvl, float ax, float ay, float size)
        {
            var b = UIKit.Rect(parent, "Draw_" + id);
            UIKit.PlaceFixed(b, ax, ay, size, size);
            float u = size;
            // подставка меняет цвет с уровнем: дерево → сталь → золото
            string[] stand = { "#8D6E63", "#B0BEC5", "#FFD54F", "#FFC928", "#FF8F00" };
            if (id == Up.Grills) lvl = Mathf.Min(lvl, 2);
            var st = UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex(stand[Mathf.Clamp(lvl, 0, 4)]), 0.5f, 0.06f, u * 0.9f, u * 0.08f);
            if (lvl >= 4 && id != Up.Grills)
            {
                var halo = UIKit.Shape(b, UIKit.CircleHD, UIKit.Alpha(UIKit.Gold, 0.25f), 0.5f, 0.5f, u * 1.05f, u * 1.05f);
                halo.gameObject.AddComponent<Blinker>().speed = 2f;
            }
            if (lvl >= 3) st.gameObject.AddComponent<Blinker>().speed = 1.2f;
            DrawStationBody(b, id, lvl, u);
            if (lvl >= 3 && Automatable(id)) StationLook.Auto(b, id, 0.82f, 0.78f, u * 0.4f);
            if (lvl >= 4 && id != Up.Grills) Icons.Star(b, 0.14f, 0.86f, u * 0.22f, true);
        }

        static void DrawStationBody(RectTransform b, Up id, int lvl, float u)
        {
            switch (id)
            {
                case Up.Spit:
                    if (lvl >= 2) UIKit.Panel(b, "Heater2", UIKit.Hex("#2A2A2A"), 0.9f, 0.2f, 0.98f, 0.8f);
                    UIKit.Panel(b, "Heater", UIKit.Hex("#2A2A2A"), 0.08f, 0.1f, 0.28f, 0.9f);
                    for (int i = 0; i < 4; i++) UIKit.Panel(b, "Glow", UIKit.Hex("#FF6A2B"), 0.12f, 0.16f + i * 0.19f, 0.24f, 0.26f + i * 0.19f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.SteelLight, 0.6f, 0.5f, u * 0.04f, u * 0.95f);
                    for (int i = 0; i < 6; i++)
                        UIKit.Shape(b, UIKit.Rounded, UIKit.Hex(i % 2 == 0 ? "#A8461F" : "#8E3A18"), 0.6f, 0.18f + i * 0.13f,
                            u * (0.5f - Mathf.Abs(i - 2.5f) * 0.05f), u * 0.13f);
                    break;
                case Up.Knife:
                    UIKit.Panel(b, "Board", UIKit.Hex("#D7B07A"), 0.05f, 0.2f, 0.95f, 0.6f);
                    Icons.Ingredient(b, Ing.Tomato, 0.35f, 0.5f, u * 0.38f);
                    Icons.Ingredient(b, Ing.Cucumber, 0.6f, 0.38f, u * 0.2f);
                    if (lvl >= 2) UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#ECEFF1"), 0.2f, 0.72f, u * 0.3f, u * 0.16f, 15f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#DDE3E7"), 0.68f, 0.7f, u * 0.5f, u * 0.1f, -30f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#4E342E"), 0.9f, 0.86f, u * 0.2f, u * 0.08f, -30f);
                    break;
                case Up.Fryer:
                    UIKit.Panel(b, "Body", lvl >= 1 ? UIKit.Hex("#CFD8DC") : UIKit.SteelLight, 0.1f, 0.08f, 0.9f, 0.62f);
                    UIKit.Panel(b, "Oil", UIKit.Hex("#C9951B"), 0.16f, 0.14f, 0.84f, 0.45f);
                    UIKit.Panel(b, "Basket", UIKit.Hex("#6D7B84"), 0.22f, 0.35f, 0.78f, 0.7f);
                    Icons.Ingredient(b, Ing.Fries, 0.5f, 0.66f, u * 0.5f);
                    break;
                case Up.Grill:
                    UIKit.Panel(b, "Plate", UIKit.Hex("#262626"), 0.04f, 0.18f, 0.96f, 0.62f);
                    for (int i = 0; i < 4; i++) UIKit.Panel(b, "Line", UIKit.Hex("#3A3A3A"), 0.08f, 0.24f + i * 0.09f, 0.92f, 0.27f + i * 0.09f, false);
                    var roll = Icons.Shawarma(b, 0.5f, 0.46f, u * 0.75f);
                    roll.color = Icons.Toast(1f);
                    var marks = ShawarmaView.GrillMarks(roll);
                    foreach (var m in marks) m.color = new Color(0.25f, 0.12f, 0.05f, 0.55f);
                    break;
                case Up.Sauce:
                    float bs = u * (0.55f + 0.06f * lvl);
                    Icons.Ingredient(b, Ing.Garlic, 0.35f, 0.5f, bs);
                    Icons.Ingredient(b, Ing.Spicy, 0.68f, 0.48f, bs);
                    break;
                case Up.Bins:
                    for (int i = 0; i < 3; i++)
                    {
                        float x0 = 0.04f + i * 0.32f;
                        UIKit.Panel(b, "Bin", UIKit.Hex("#E4E9EC"), x0, 0.25f, x0 + 0.29f, 0.6f);
                        var ing = i == 0 ? Ing.Meat : (i == 1 ? Ing.Tomato : Ing.Cucumber);
                        Icons.Ingredient(b, ing, x0 + 0.145f, 0.45f, u * 0.24f);
                    }
                    break;
                case Up.Pack:
                    Icons.Paper(b, 0.33f, 0.4f, u * 0.55f);
                    Icons.Bag(b, 0.68f, 0.5f, u * 0.62f);
                    break;
                case Up.Patience:
                    Icons.Radio(b, 0.5f, 0.45f, u * 0.8f);
                    UIKit.Shape(b, UIKit.Circle, UIKit.Dark, 0.8f, 0.8f, u * 0.08f, u * 0.07f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Dark, 0.83f, 0.9f, u * 0.02f, u * 0.14f);
                    break;
                case Up.Tips:
                    Icons.Jar(b, 0.5f, 0.45f, u * 0.85f);
                    break;
                case Up.Price:
                    Icons.Book(b, 0.5f, 0.45f, u * 0.85f);
                    break;
                case Up.Flies:
                    if (lvl == 0)
                    {
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#ECEFF1"), 0.4f, 0.4f, u * 0.2f, u * 0.6f, 30f);
                        UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#212121"), 0.7f, 0.7f, u * 0.14f, u * 0.16f);
                    }
                    else if (lvl == 1)
                    {
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#8D6E63"), 0.5f, 0.3f, u * 0.06f, u * 0.45f, -15f);
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#E53935"), 0.57f, 0.68f, u * 0.4f, u * 0.34f, -15f);
                    }
                    else
                    {
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#BF360C"), 0.5f, 0.18f, u * 0.4f, u * 0.2f);
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#2E7D32"), 0.5f, 0.38f, u * 0.05f, u * 0.3f);
                        UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#43A047"), 0.5f, 0.62f, u * 0.44f, u * 0.3f);
                        UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#E53935"), 0.5f, 0.62f, u * 0.32f, u * 0.16f);
                    }
                    break;
                case Up.Board:
                    UIKit.Panel(b, "Board", UIKit.Hex(lvl >= 2 ? "#ECEFF1" : "#D7B07A"), 0.05f, 0.18f, 0.95f, 0.55f);
                    if (lvl == 0)
                        for (int i = 0; i < 4; i++) UIKit.Shape(b, UIKit.Circle, new Color(0.35f, 0.2f, 0.08f, 0.5f), 0.2f + i * 0.2f, 0.35f + (i % 2) * 0.08f, u * 0.14f, u * 0.1f);
                    if (lvl >= 1)
                    {
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FDD835"), 0.7f, 0.68f, u * 0.34f, u * 0.16f);
                        UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#43A047"), 0.7f, 0.78f, u * 0.34f, u * 0.06f);
                    }
                    break;
                case Up.Fire:
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#C62828"), 0.45f, 0.42f, u * 0.3f, u * 0.6f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#212121"), 0.45f, 0.76f, u * 0.14f, u * 0.1f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#212121"), 0.6f, 0.8f, u * 0.24f, u * 0.05f, -20f);
                    UIKit.Shape(b, UIKit.RoundedSmall, Color.white, 0.45f, 0.45f, u * 0.3f, u * 0.08f);
                    if (lvl >= 2)
                        for (int i = 0; i < 5; i++) UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#4FC3F7"), 0.15f + i * 0.07f, 0.85f - (i % 2) * 0.06f, u * 0.05f, u * 0.07f);
                    break;
                case Up.Grills:
                    {
                        int n = lvl + 1;
                        for (int i = 0; i < n; i++)
                        {
                            float cy = n == 1 ? 0.42f : 0.2f + i * (0.56f / (n - 1));
                            float ph = 0.62f / n + 0.06f;
                            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#262626"), 0.5f, cy, u * 0.86f, u * ph);
                            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#3A3A3A"), 0.5f, cy, u * 0.76f, u * 0.03f);
                            var r = Icons.Shawarma(b, 0.5f, cy, u * Mathf.Min(0.6f, 0.9f / n));
                            r.color = Icons.Toast(0.8f);
                        }
                    }
                    break;
                case Up.FryBox:
                    Icons.FriesBox(b, 0.3f, 0.3f, u * 0.4f, 0f);
                    Icons.FriesBox(b, 0.34f, 0.42f, u * 0.4f, 0f);
                    Icons.FriesBox(b, 0.66f, 0.45f, u * 0.6f, 1f);
                    break;
                case Up.Queue:
                    Icons.People(b, 0.35f, 0.4f, u * 0.5f);
                    if (lvl >= 1)
                        for (int i = 0; i < 6; i++)
                            UIKit.Shape(b, UIKit.RoundedSmall, i % 2 == 0 ? UIKit.Red : Color.white, 0.2f + i * 0.12f, 0.85f, u * 0.12f, u * 0.1f);
                    if (lvl >= 2) UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#8D6E63"), 0.5f, 0.18f, u * 0.8f, u * 0.08f);
                    if (lvl >= 3) Icons.Cup(b, 0.75f, 0.45f, u * 0.3f);
                    break;
            }
        }
    }
}
