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
            new Spot { id = Up.Flies,  x = 0.17f,  y = 0.33f, size = 180f },
            new Spot { id = Up.Board,  x = 0.335f, y = 0.33f, size = 180f },
            new Spot { id = Up.Fire,   x = 0.5f,   y = 0.33f, size = 180f },
            new Spot { id = Up.Queue,  x = 0.665f, y = 0.33f, size = 180f },
            new Spot { id = Up.Grills, x = 0.83f,  y = 0.33f, size = 180f },
        };

        // экран «Помощники»: всё, что переносит продукты само
        static readonly Spot[] Spots3 =
        {
            new Spot { id = Up.VegFeed,   x = 0.17f,  y = 0.33f, size = 180f },
            new Spot { id = Up.FryLoad,   x = 0.335f, y = 0.33f, size = 180f },
            new Spot { id = Up.FryUnload, x = 0.5f,   y = 0.33f, size = 180f },
            new Spot { id = Up.FryBox,    x = 0.665f, y = 0.33f, size = 180f },
            new Spot { id = Up.Drinks,    x = 0.83f,  y = 0.33f, size = 180f },
        };

        // экран «Люкс»: улучшения для Бизнес-клуба и Дубая
        static readonly Spot[] Spots4 =
        {
            new Spot { id = Up.Premium,    x = 0.3f, y = 0.33f, size = 200f },
            new Spot { id = Up.Lounge,     x = 0.5f, y = 0.33f, size = 200f },
            new Spot { id = Up.TurboGrill, x = 0.7f, y = 0.33f, size = 200f },
        };

        int _shopTab;
        const int ShopPages = 4;

        static Spot[] PageSpots(int page) { return page == 0 ? Spots : (page == 1 ? Spots2 : (page == 2 ? Spots3 : Spots4)); }

        static string PageName(int page)
        {
            return page == 0 ? L.T("Кухня", "Kitchen") : (page == 1 ? L.T("Порядок", "Order") : (page == 2 ? L.T("Помощники", "Helpers") : L.T("Люкс", "Luxury")));
        }

        static int PageOf(Up id)
        {
            for (int p = 0; p < ShopPages; p++)
                foreach (var sp in PageSpots(p)) if (sp.id == id) return p;
            return -1;
        }

        /// <summary>На каком уровне (индекс) предмет начинает работать сам. -1 — никогда.</summary>
        static int AutoLevel(Up id)
        {
            switch (id)
            {
                case Up.Spit: case Up.Knife: case Up.Grill: case Up.Sauce: case Up.Pack: case Up.Tips:
                case Up.Flies: case Up.Board: case Up.Fire: case Up.FryBox: case Up.Drinks:
                    return 3;
                case Up.VegFeed: case Up.FryLoad: case Up.FryUnload:
                    return 2;
            }
            return -1;
        }

        static bool IsAuto(Up id, int lvl) { int a = AutoLevel(id); return a >= 0 && lvl >= a; }

        /// <summary>Цена улучшения растёт вместе с ценами локации: на дорогих локациях и шаурма дороже, и прокачка.</summary>
        int UpCost(Up id, int lvl)
        {
            return GameData.UpgradeCost(GameData.Upgrades[(int)id], lvl, _save.location);
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
            if (openId >= 0 && PageOf((Up)openId) >= 0) _shopTab = PageOf((Up)openId);
            _shopTab = Mathf.Clamp(_shopTab, 0, ShopPages - 1);
            var s = NewScreen("Shop");

            BuildKioskInterior(s);

            // верх
            var top = UIKit.Panel(s, "Top", UIKit.Alpha(UIKit.Dark, 0.92f), 0.01f, 0.915f, 0.99f, 0.99f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Мой ларёк", "My kiosk"), 40, UIKit.Cream, TextAnchor.MiddleLeft, 0.02f, 0, 0.22f, 1, true);
            // какой экран сейчас
            var pageBox = UIKit.Panel(top.transform, "Page", UIKit.Alpha(Color.black, 0.3f), 0.2f, 0.1f, 0.42f, 0.9f);
            UIKit.FitLabel(pageBox.transform, PageName(_shopTab) + "  " + (_shopTab + 1) + "/" + ShopPages,
                30, UIKit.Cream, 0.04f, 0.1f, 0.96f, 0.9f);
            int cap = UpgradeDef.CapAt(_save.location) + 1;
            string sub = _shopTab == 0 ? L.T("Нажми на предмет — откроется улучшение", "Tap an item to upgrade it")
                : (_shopTab == 1 ? L.T("Мухи, грязь, пожары, очередь и грили", "Flies, dirt, fires, queue and grills")
                : (_shopTab == 2 ? L.T("Помощники, напитки и коробки фри", "Helpers, drinks and fries boxes")
                                 : L.T("Роскошь для бизнес-клуба и Дубая", "Luxury for the business club and Dubai")));
            UIKit.Label(top.transform, sub +
                    "\n<size=18><color=#FFE082>" + L.T("На этой локации — максимум до ур. ", "Max level at this location: ") + cap + "</color></size>",
                22, UIKit.Gold, TextAnchor.MiddleCenter, 0.46f, 0, 0.81f, 1, true);
            Icons.Coin(top.transform, 0.83f, 0.5f, 46f);
            UIKit.Label(top.transform, GameData.Money(_save.money), 38, UIKit.Gold, TextAnchor.MiddleLeft, 0.85f, 0, 0.99f, 1, true);

            foreach (var sp in PageSpots(_shopTab)) BuildSpot(s, sp);

            // большие стрелки прямо в ларьке: листать экраны улучшений
            if (_shopTab < ShopPages - 1)
            {
                int ready = 0;
                for (int pg = _shopTab + 1; pg < ShopPages; pg++) foreach (var sp2 in PageSpots(pg)) if (UpReady(sp2.id)) ready++;
                BigArrow(s, true, ready);
            }
            if (_shopTab > 0)
            {
                int ready = 0;
                for (int pg = 0; pg < _shopTab; pg++) foreach (var sp2 in PageSpots(pg)) if (UpReady(sp2.id)) ready++;
                BigArrow(s, false, ready);
            }

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

        /// <summary>Жирная ровная стрелка-кнопка внутри ларька для перелистывания экранов улучшений.</summary>
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
            float rot = right ? 0f : 180f;
            UIKit.Shape(body, UIKit.Arrow, UIKit.Alpha(UIKit.Dark, 0.35f), 0.52f, 0.46f, 96f, 96f, rot);
            UIKit.Shape(body, UIKit.Arrow, Color.white, 0.5f, 0.5f, 96f, 96f, rot);
            var sw = body.gameObject.AddComponent<Swayer>();
            sw.amp = 7f;
            sw.speed = 3f;

            if (badge > 0)
            {
                var bd = UIKit.Shape(holder, UIKit.Circle, UIKit.Red, right ? 0.15f : 0.85f, 0.88f, 44f, 44f);
                UIKit.AddOutline(bd, Color.white, 2f);
                UIKit.FitLabel(bd.transform, badge.ToString(), 24, Color.white, 0f, 0f, 1f, 1f);
                bd.gameObject.AddComponent<Bobber>().amp = 4f;
            }
            UIKit.HitArea(holder, "Tap", () =>
            {
                _shopTab = Mathf.Clamp(_shopTab + (right ? 1 : -1), 0, ShopPages - 1);
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

            BuildSeatPluses(s);
        }

        /// <summary>
        /// «Места у прилавка»: два зелёных плюса на стенах по бокам окна. Нажал — карточка улучшения.
        /// Серые с замком — следующий уровень откроется на другой локации; все места открыты — плюсов нет.
        /// </summary>
        void BuildSeatPluses(RectTransform s)
        {
            var def = GameData.Upgrades[(int)Up.Seats];
            int lvl = _save.upgrades[(int)Up.Seats];
            int seats = GameData.Seats(lvl);
            // табличка «мест: N из 4» под окном
            var tag = UIKit.Panel(s, "SeatsTag", UIKit.Alpha(UIKit.Dark, 0.85f), 0.43f, 0.495f, 0.57f, 0.535f);
            UIKit.FitLabel(tag.transform, L.T("Мест у прилавка: ", "Counter seats: ") + seats + "/4", 20, UIKit.Cream, 0.04f, 0.05f, 0.96f, 0.95f);
            if (lvl >= def.MaxLevel) return;
            bool open = UpOpen(Up.Seats);
            int cost = UpCost(Up.Seats, lvl);
            bool can = open && _save.money >= cost;
            // две вывески «+1 место» висят на ларьке по бокам окна
            float[] xs = { 0.335f, 0.665f };
            foreach (var x in xs)
            {
                var h = UIKit.Rect(s, "SeatCard");
                UIKit.PlaceFixed(h, x, 0.705f, 160f, 210f);
                var card = UIKit.Rect(h, "Card");
                UIKit.PlaceFixed(card, 0.5f, 0.5f, 160f, 210f);
                card.pivot = new Vector2(0.5f, 1f);          // висит на шнурках — качается от верха
                card.anchoredPosition = new Vector2(0f, 105f);
                // подвес: два шнурка от навеса
                UIKit.Shape(card, UIKit.RoundedSmall, UIKit.Hex("#3E2723"), 0.3f, 1.08f, 4f, 40f);
                UIKit.Shape(card, UIKit.RoundedSmall, UIKit.Hex("#3E2723"), 0.7f, 1.08f, 4f, 40f);
                UIKit.Shape(card, UIKit.RoundedSmall, new Color(0, 0, 0, 0.35f), 0.53f, 0.47f, 156f, 206f);
                UIKit.Shape(card, UIKit.RoundedSmall, UIKit.Hex("#5D4037"), 0.5f, 0.5f, 156f, 206f);            // деревянная рамка
                var face = UIKit.Shape(card, UIKit.RoundedSmall, open ? UIKit.Hex("#FFF8E1") : UIKit.Hex("#CFD8DC"), 0.5f, 0.52f, 138f, 186f);
                // табурет у прилавка
                Color seat = open ? UIKit.Hex("#E53935") : UIKit.Hex("#90A4AE");
                Color leg = open ? UIKit.Hex("#6D4C41") : UIKit.Hex("#78909C");
                UIKit.Shape(card, UIKit.RoundedSmall, leg, 0.38f, 0.5f, 8f, 56f, 8f);
                UIKit.Shape(card, UIKit.RoundedSmall, leg, 0.62f, 0.5f, 8f, 56f, -8f);
                UIKit.Shape(card, UIKit.RoundedSmall, leg, 0.5f, 0.47f, 50f, 6f);
                UIKit.Shape(card, UIKit.CircleHD, UIKit.Alpha(Color.black, 0.2f), 0.5f, 0.635f, 80f, 26f);
                UIKit.Shape(card, UIKit.CircleHD, seat, 0.5f, 0.65f, 78f, 26f);
                UIKit.Shape(card, UIKit.CircleHD, UIKit.Alpha(Color.white, 0.35f), 0.44f, 0.665f, 30f, 8f);
                // заголовок
                UIKit.FitLabel(card, L.T("+1 МЕСТО", "+1 SEAT"), 24, open ? UIKit.Hex("#1B5E20") : UIKit.Hex("#455A64"), 0.08f, 0.8f, 0.92f, 0.95f);
                // низ карточки: цена или когда откроется
                if (open)
                {
                    var pb = UIKit.Rect(card, "PriceBar");
                    UIKit.Place(pb, 0.08f, 0.06f, 0.92f, 0.26f);
                    var pbi = pb.gameObject.AddComponent<Image>();
                    pbi.sprite = UIKit.RoundedSmall;
                    pbi.type = Image.Type.Sliced;
                    pbi.color = can ? UIKit.Green : UIKit.Hex("#8D6E63");
                    pbi.raycastTarget = false;
                    Icons.Coin(pb, 0.17f, 0.5f, 26f);
                    UIKit.FitLabel(pb, GameData.Money(cost), 24, Color.white, 0.3f, 0.08f, 0.96f, 0.92f);
                }
                else
                {
                    // серая плашка того же размера, что и цена: замок + название локации одной строкой
                    var lb = UIKit.Rect(card, "LockBar");
                    UIKit.Place(lb, 0.08f, 0.06f, 0.92f, 0.26f);
                    var lbi = lb.gameObject.AddComponent<Image>();
                    lbi.sprite = UIKit.RoundedSmall;
                    lbi.type = Image.Type.Sliced;
                    lbi.color = UIKit.Hex("#78909C");
                    lbi.raycastTarget = false;
                    var lk = UIKit.Rect(lb, "Lock");
                    UIKit.PlaceFixed(lk, 0.15f, 0.5f, 26f, 30f);
                    UIKit.Shape(lk, UIKit.Circle, UIKit.Hex("#ECEFF1"), 0.5f, 0.7f, 16f, 16f);
                    UIKit.Shape(lk, UIKit.Circle, UIKit.Hex("#78909C"), 0.5f, 0.7f, 8f, 8f);
                    UIKit.Shape(lk, UIKit.RoundedSmall, UIKit.Hex("#ECEFF1"), 0.5f, 0.36f, 22f, 16f);
                    UIKit.FitLabel(lb, LocName(def.LocFor(lvl)), 20, Color.white, 0.28f, 0.1f, 0.96f, 0.9f);
                    UIKit.FitLabel(card, L.T("откроется на локации", "unlocks at"), 14, UIKit.Hex("#546E7A"), 0.06f, 0.27f, 0.94f, 0.36f, false);
                }
                if (can)
                {
                    var pend = card.gameObject.AddComponent<Pendulum>();
                    pend.angle = 4f;
                    pend.speed = 2.2f;
                }
                UIKit.HitArea(h, "Tap", () =>
                {
                    Sfx.Play(Sound.Click);
                    Juice.Punch(h, 0.15f, 0.25f);
                    UpgradePopup((RectTransform)_screen.transform, Up.Seats, false);
                }).transform.SetAsLastSibling();
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
            if (GameData.IngOpen(Ing.Cheese, _save.location)) { names.Add(L.T("С сыром", "Cheesy")); prices.Add(GameData.LavashPrice + meat + tom + GameData.Get(Ing.Cheese).price); }
            if (GameData.DrinkOpen(Drink.Fries, eday, _save.location)) { names.Add(L.T("Фри", "Fries")); prices.Add(GameData.Get(Drink.Fries).price); }
            if (GameData.DrinkOpen(Drink.Cola, eday, _save.location)) { names.Add(L.T("Кола", "Cola")); prices.Add(GameData.Get(Drink.Cola).price); }
            if (GameData.DrinkOpen(Drink.Juice, eday, _save.location)) { names.Add(L.T("Апельсин", "Orange")); prices.Add(GameData.Get(Drink.Juice).price); }
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

        /// <summary>Карточка улучшения: тёмная «вывеска», прожектор на предмете, лестница уровней.</summary>
        void UpgradePopup(RectTransform s, Up id, bool justBought)
        {
            var def = GameData.Upgrades[(int)id];
            int lvl = _save.upgrades[(int)id];
            bool maxed = lvl >= def.MaxLevel;
            int total = def.MaxLevel + 1;

            // затемнение: тап мимо — закрыть
            var ov = UIKit.Panel(s, "Popup", new Color(0, 0, 0, 0.6f), 0, 0, 1, 1, false);
            ov.raycastTarget = true;
            var overlay = ov.gameObject;
            UnityEngine.Events.UnityAction close = () => { Sfx.Play(Sound.Click); Destroy(overlay); };
            UIKit.HitArea(ov.transform, "Outside", close);

            var card = UIKit.Panel(ov.transform, "Card", UIKit.Hex("#2A1C13"), 0.13f, 0.08f, 0.87f, 0.92f);
            card.raycastTarget = true;
            UIKit.AddShadow(card, 16f, 0.5f);
            UIKit.AddOutline(card, UIKit.Hex("#C98A2E"), 3f);
            var ct = card.transform;
            UIKit.Panel(ct, "Inner", UIKit.Hex("#3A2819"), 0.012f, 0.015f, 0.988f, 0.985f);
            Juice.Punch(card.rectTransform, 0.06f, 0.25f);

            // шапка: название + «уровень N из M»
            var title = UIKit.Label(ct, def.Name.ToUpper(), 50, UIKit.Gold, TextAnchor.MiddleLeft, 0.04f, 0.87f, 0.8f, 0.97f, true);
            UIKit.AddOutline(title, UIKit.Hex("#1A0F08"), 3f);
            UIKit.AddShadow(title, 4f, 0.6f);
            var sub = UIKit.Label(ct, L.T("Уровень ", "Level ") + (lvl + 1) + L.T(" из ", " of ") + total, 26, UIKit.Alpha(UIKit.Cream, 0.8f),
                TextAnchor.MiddleLeft, 0.04f, 0.81f, 0.6f, 0.87f, true);
            UIKit.AddShadow(sub, 2f, 0.5f);
            UIKit.Panel(ct, "Line", UIKit.Alpha(UIKit.Gold, 0.35f), 0.03f, 0.805f, 0.97f, 0.81f, false);
            var xb = UIKit.Button(ct, "X", UIKit.Red, close, 0.92f, 0.885f, 0.975f, 0.965f, 34);
            xb.transform.SetAsLastSibling();

            // слева — предмет под «прожектором»
            UIKit.Shape(ct, UIKit.CircleHD, UIKit.Alpha(UIKit.Gold, 0.16f), 0.19f, 0.52f, 420f, 420f);
            UIKit.Shape(ct, UIKit.CircleHD, UIKit.Alpha(UIKit.Gold, 0.14f), 0.19f, 0.52f, 300f, 300f);
            var pic = UIKit.Rect(ct, "Pic");
            UIKit.Place(pic, 0.03f, 0.2f, 0.35f, 0.8f);
            DrawStation(pic, id, lvl, 0.5f, 0.55f, 260f);
            if (justBought)
            {
                Juice.Punch(pic, 0.2f, 0.4f);
                Juice.Burst(pic, UIKit.Gold, 16, 0.5f, 0.55f, 18f);
            }
            if (!maxed)
            {
                var nb = UIKit.Panel(ct, "Next", UIKit.Alpha(Color.black, 0.35f), 0.05f, 0.2f, 0.33f, 0.33f);
                var nl = UIKit.Label(nb.transform, L.T("Дальше:", "Next:"), 24, UIKit.Cream, TextAnchor.MiddleLeft, 0.06f, 0f, 0.45f, 1f, true);
                UIKit.AddShadow(nl, 2f, 0.5f);
                UIKit.Shape(nb.transform, UIKit.Arrow, UIKit.Gold, 0.5f, 0.5f, 44f, 44f);
                DrawStation(nb.transform, id, lvl + 1, 0.8f, 0.5f, 100f);
            }

            // справа — лестница уровней
            float top = 0.79f, bottom = 0.19f;
            float rh = (top - bottom) / total;
            UIKit.Panel(ct, "Rail", UIKit.Alpha(UIKit.Gold, 0.3f), 0.397f, bottom + rh * 0.5f, 0.403f, top - rh * 0.5f, false);
            for (int l = 0; l <= def.MaxLevel; l++)
            {
                float y1 = top - l * rh, y0 = y1 - rh + 0.01f;
                bool done = l < lvl, now = l == lvl, next = l == lvl + 1;
                bool reach = l == 0 || _save.location >= def.LocFor(l - 1);
                bool legend = l == 4 && def.MaxLevel == 4;
                bool auto = AutoLevel(id) == l;

                Color bg = now ? UIKit.Hex("#6B4A12") : (done ? UIKit.Hex("#2F4A2B") : (reach ? UIKit.Hex("#4A3524") : UIKit.Hex("#2E2620")));
                var row = UIKit.Panel(ct, "Lvl", bg, 0.425f, y0, 0.97f, y1);
                if (now) UIKit.AddOutline(row, UIKit.Gold, 3f);
                else if (next && reach) UIKit.AddOutline(row, UIKit.Hex("#7CB342"), 2f);
                // узел на рельсе
                Color node = done || now ? UIKit.Gold : (reach ? UIKit.Hex("#8D6E63") : UIKit.Hex("#4E4038"));
                float ny = (y0 + y1) * 0.5f;
                UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#1A0F08"), 0.4f, ny, 58f, 58f);
                UIKit.Shape(ct, UIKit.CircleHD, node, 0.4f, ny, 48f, 48f);
                var num = UIKit.FitLabel(ct, (l + 1).ToString(), 28, done || now ? UIKit.Dark : UIKit.Cream, 0.385f, ny - 0.025f, 0.415f, ny + 0.025f);

                string desc = def.LevelDesc(l);
                var dl = UIKit.Label(row.transform, desc, 26, reach ? UIKit.Cream : UIKit.Alpha(UIKit.Cream, 0.4f), TextAnchor.MiddleLeft, 0.04f, reach ? 0.05f : 0.35f, 0.76f, 0.95f, now);
                UIKit.AddShadow(dl, 2f, 0.6f);
                if (!reach)
                {
                    var ul = UIKit.Label(row.transform, L.T("откроется на локации ", "unlocks at location ") + (def.LocFor(l - 1) + 1) + " — " + LocName(def.LocFor(l - 1)),
                        18, UIKit.Hex("#FFB74D"), TextAnchor.MiddleLeft, 0.04f, 0.02f, 0.76f, 0.4f, false);
                    UIKit.AddShadow(ul, 2f, 0.6f);
                }

                string tag = now ? L.T("СЕЙЧАС", "NOW") : (next && reach ? L.T("ДАЛЕЕ", "NEXT") : "");
                Color tagCol = now ? UIKit.Gold : UIKit.Hex("#7CB342");
                if (auto && !now) { tag = L.T("АВТО", "AUTO"); tagCol = UIKit.Hex("#4FC3F7"); }
                if (legend && !now) { tag = L.T("ЛЕГЕНДА", "LEGEND"); tagCol = UIKit.Hex("#FF8F00"); }
                if (!reach && !done && !now) { tag = L.T("ЗАКРЫТО", "LOCKED"); tagCol = UIKit.Hex("#6D6D6D"); }
                if (tag.Length > 0)
                {
                    var t = UIKit.Panel(row.transform, "Tag", tagCol, 0.78f, 0.22f, 0.97f, 0.78f);
                    UIKit.FitLabel(t.transform, tag, 18, UIKit.Dark, 0.06f, 0.1f, 0.94f, 0.9f);
                }
            }

            // низ: кошелёк и кнопка
            var wal = UIKit.Panel(ct, "Wallet", UIKit.Alpha(Color.black, 0.35f), 0.05f, 0.04f, 0.33f, 0.15f);
            Icons.Coin(wal.transform, 0.14f, 0.5f, 46f);
            var wl = UIKit.Label(wal.transform, GameData.Money(_save.money), 34, UIKit.Gold, TextAnchor.MiddleLeft, 0.26f, 0f, 0.98f, 1f, true);
            UIKit.AddShadow(wl, 2f, 0.6f);

            if (maxed)
            {
                var mx = UIKit.Label(ct, L.T("МАКСИМУМ!", "MAXED!"), 40, UIKit.Gold, TextAnchor.MiddleCenter, 0.425f, 0.04f, 0.97f, 0.15f, true);
                UIKit.AddOutline(mx, UIKit.Dark, 3f);
            }
            else
            {
                int cost = UpCost(id, lvl);
                if (!UpOpen(id))
                {
                    int need = def.LocFor(lvl);
                    var lk = UIKit.Panel(ct, "Locked", UIKit.Hex("#4E4038"), 0.425f, 0.04f, 0.97f, 0.15f);
                    UIKit.FitLabel(lk.transform, L.T("Следующий уровень — на локации ", "Next level — at location ") + (need + 1) + " (" + LocName(need) + ")",
                        28, UIKit.Cream, 0.03f, 0.1f, 0.97f, 0.9f);
                }
                else
                {
                    bool can = _save.money >= cost;
                    var bt = UIKit.PriceButton(ct, L.T("Улучшить", "Upgrade"), cost, can ? UIKit.Green : UIKit.Gray, () => BuyUpgrade(id), 0.425f, 0.04f, 0.97f, 0.15f, 38);
                    bt.interactable = can;
                    if (can) bt.gameObject.AddComponent<Bobber>().amp = 2f;
                    else
                    {
                        var nm = UIKit.Label(ct, L.T("не хватает ", "need ") + GameData.Money(cost - _save.money), 22, UIKit.Hex("#FF8A80"),
                            TextAnchor.MiddleCenter, 0.425f, 0.15f, 0.97f, 0.19f, true);
                        UIKit.AddShadow(nm, 2f, 0.6f);
                    }
                }
            }
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
            if (IsAuto(id, lvl)) StationLook.Auto(b, id, 0.82f, 0.78f, u * 0.4f);
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
                case Up.VegFeed:
                    UIKit.Panel(b, "Crate", UIKit.Hex("#8D6E63"), 0.05f, 0.5f, 0.5f, 0.8f);
                    Icons.Ingredient(b, Ing.Tomato, 0.2f, 0.72f, u * 0.2f);
                    Icons.Ingredient(b, Ing.Cucumber, 0.36f, 0.7f, u * 0.18f);
                    UIKit.Panel(b, "Board", UIKit.Hex("#D7B07A"), 0.45f, 0.15f, 0.95f, 0.42f);
                    Icons.Ingredient(b, Ing.Tomato, 0.7f, 0.32f, u * 0.22f);
                    UIKit.Shape(b, UIKit.Arrow, UIKit.Hex("#43A047"), 0.52f, 0.56f, u * 0.3f, u * 0.3f, -35f);
                    break;
                case Up.FryLoad:
                    UIKit.Panel(b, "Crate", UIKit.Hex("#8D6E63"), 0.05f, 0.55f, 0.45f, 0.8f);
                    for (int i = 0; i < 5; i++) UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FFF3C4"), 0.12f + i * 0.06f, 0.78f, u * 0.04f, u * 0.2f, i * 7f - 14f);
                    UIKit.Panel(b, "Fryer", UIKit.SteelLight, 0.45f, 0.1f, 0.95f, 0.45f);
                    UIKit.Panel(b, "Oil", UIKit.Hex("#C9951B"), 0.5f, 0.14f, 0.9f, 0.32f);
                    UIKit.Shape(b, UIKit.Arrow, UIKit.Hex("#43A047"), 0.5f, 0.55f, u * 0.3f, u * 0.3f, -35f);
                    break;
                case Up.FryUnload:
                    UIKit.Panel(b, "Fryer", UIKit.SteelLight, 0.05f, 0.45f, 0.5f, 0.8f);
                    Icons.Ingredient(b, Ing.Fries, 0.27f, 0.7f, u * 0.3f);
                    UIKit.Panel(b, "Bin", UIKit.Hex("#E4E9EC"), 0.5f, 0.1f, 0.95f, 0.4f);
                    Icons.Ingredient(b, Ing.Fries, 0.72f, 0.28f, u * 0.3f);
                    UIKit.Shape(b, UIKit.Arrow, UIKit.Hex("#43A047"), 0.5f, 0.5f, u * 0.3f, u * 0.3f, -35f);
                    break;
                case Up.Drinks:
                    UIKit.Panel(b, "Machine", UIKit.Hex("#455A64"), 0.1f, 0.1f, 0.6f, 0.85f);
                    UIKit.Panel(b, "Label", UIKit.Hex("#E53935"), 0.15f, 0.6f, 0.55f, 0.78f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#B0BEC5"), 0.35f, 0.5f, u * 0.08f, u * 0.1f);
                    Icons.DrinkCup(b, Drink.Cola, 0.35f, 0.3f, u * 0.3f, 1f);
                    Icons.DrinkCup(b, Drink.Juice, 0.78f, 0.3f, u * 0.34f, 1f);
                    break;
                case Up.Premium:
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#ECEFF1"), 0.5f, 0.4f, u * 0.9f, u * 0.5f);
                    UIKit.Shape(b, UIKit.CircleHD, Color.white, 0.5f, 0.42f, u * 0.7f, u * 0.36f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.12f, 0.45f, u * 0.05f, u * 0.5f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.88f, 0.45f, u * 0.05f, u * 0.5f);
                    Icons.Shawarma(b, 0.5f, 0.46f, u * 0.5f).color = Icons.Toast(1f);
                    break;
                case Up.Lounge:
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.15f, 0.35f, u * 0.05f, u * 0.5f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.85f, 0.35f, u * 0.05f, u * 0.5f);
                    UIKit.Shape(b, UIKit.Rounded, UIKit.Hex("#B71C1C"), 0.5f, 0.52f, u * 0.7f, u * 0.08f);
                    UIKit.Shape(b, UIKit.Rounded, UIKit.Hex("#6A1B9A"), 0.5f, 0.2f, u * 0.7f, u * 0.2f);
                    UIKit.Shape(b, UIKit.Rounded, UIKit.Hex("#8E24AA"), 0.5f, 0.3f, u * 0.64f, u * 0.12f);
                    break;
                case Up.TurboGrill:
                    UIKit.Panel(b, "Plate", UIKit.Hex("#263238"), 0.05f, 0.18f, 0.95f, 0.55f);
                    for (int i = 0; i < 4; i++) UIKit.Shape(b, UIKit.Circle, UIKit.Hex(lvl >= 2 ? "#40C4FF" : "#FF7043"), 0.2f + i * 0.2f, 0.3f, u * 0.14f, u * 0.14f);
                    Icons.Shawarma(b, 0.5f, 0.45f, u * 0.6f).color = Icons.Toast(1f);
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
                case Up.Seats:
                {
                    // прилавок на 4 окошка: открытые — с клиентом, закрытые — заложены кирпичом
                    int seats = GameData.Seats(lvl);
                    UIKit.Panel(b, "Roof", UIKit.Hex("#C0392B"), 0.02f, 0.78f, 0.98f, 0.9f);
                    UIKit.Panel(b, "Body", UIKit.Hex("#E9D8B8"), 0.04f, 0.14f, 0.96f, 0.78f);
                    for (int i = 0; i < 4; i++)
                    {
                        bool open = seats >= 4 || (seats == 3 ? i <= 2 : (i == 1 || i == 2));
                        float x0 = 0.07f + i * 0.225f, x1 = x0 + 0.2f;
                        if (open)
                        {
                            UIKit.Panel(b, "Win", UIKit.Hex("#FFE0B2"), x0, 0.3f, x1, 0.72f);
                            UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#F6D2B0"), (x0 + x1) * 0.5f, 0.5f, u * 0.11f, u * 0.11f);
                            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex(i % 2 == 0 ? "#42A5F5" : "#66BB6A"), (x0 + x1) * 0.5f, 0.36f, u * 0.15f, u * 0.08f);
                        }
                        else
                        {
                            UIKit.Panel(b, "Wall", UIKit.Hex("#B5533C"), x0, 0.3f, x1, 0.72f);
                            for (int r = 0; r < 4; r++)
                                UIKit.Panel(b, "Mortar", UIKit.Hex("#8E3F2C"), x0, 0.3f + (r + 1) * 0.084f, x1, 0.3f + (r + 1) * 0.084f + 0.008f, false);
                        }
                    }
                    UIKit.Panel(b, "Counter", UIKit.Wood, 0.02f, 0.24f, 0.98f, 0.3f);
                    break;
                }
            }
        }
    }
}
