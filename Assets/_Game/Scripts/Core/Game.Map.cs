using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Карта города: метки локаций, карточка локации (открывается по тапу, закрывается), бустеры.</summary>
    public partial class Game
    {
        static Vector2 MapPx(LocationDef l) { return new Vector2(l.mapX * 1920f, l.mapY * 1080f); }

        void ShowMap()
        {
            BuildMap(-1);
            TryStory();
        }

        void BuildMap(int popupLoc)
        {
            var s = NewScreen("Map");
            var bg = UIKit.Full(s, "Land").gameObject.AddComponent<Image>();
            bg.sprite = UIKit.Gradient(UIKit.Hex("#9CCC65"), UIKit.Hex("#558B2F"));

            // море внизу у пляжа и речка
            UIKit.Shape(s, UIKit.CircleHD, UIKit.Hex("#4FC3F7"), 0.4f, 0.5f, 520f, 120f);
            UIKit.Shape(s, UIKit.CircleHD, UIKit.Hex("#81D4FA"), 0.4f, 0.5f, 400f, 80f);

            var rnd = new System.Random(11);
            for (int i = 0; i < 70; i++)
            {
                float x = (float)rnd.NextDouble(), y = 0.22f + (float)rnd.NextDouble() * 0.66f;
                bool near = false;
                foreach (var l in GameData.Locations)
                    if (Mathf.Abs(l.mapX - x) < 0.06f && Mathf.Abs(l.mapY - y) < 0.13f) near = true;
                if (Mathf.Abs(x - 0.4f) < 0.15f && Mathf.Abs(y - 0.5f) < 0.07f) near = true;
                if (near) continue;
                float sz = 26f + (float)rnd.NextDouble() * 26f;
                UIKit.Shape(s, UIKit.Circle, new Color(0, 0, 0, 0.15f), x + 0.004f, y - 0.008f, sz, sz * 0.8f);
                UIKit.Shape(s, UIKit.Circle, UIKit.Hex(i % 3 == 0 ? "#2E7D32" : "#388E3C"), x, y, sz, sz);
            }

            // дорога-путь через все точки
            var locs = GameData.Locations;
            for (int i = 0; i + 1 < locs.Length; i++)
            {
                Vector2 a = MapPx(locs[i]), b = MapPx(locs[i + 1]);
                Vector2 d = b - a;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                float cx = (locs[i].mapX + locs[i + 1].mapX) * 0.5f, cy = (locs[i].mapY + locs[i + 1].mapY) * 0.5f;
                bool done = i + 1 <= _save.location;
                UIKit.Shape(s, UIKit.RoundedSmall, done ? UIKit.Hex("#FFE082") : UIKit.Hex("#A1887F"), cx, cy, d.magnitude, 26f, ang);
                UIKit.Shape(s, UIKit.RoundedSmall, UIKit.Alpha(Color.white, done ? 0.7f : 0.25f), cx, cy, d.magnitude - 40f, 4f, ang);
            }

            for (int i = 0; i < locs.Length; i++) BuildPin(s, locs[i]);

            // верхняя панель
            var top = UIKit.Panel(s, "Top", UIKit.Alpha(UIKit.Dark, 0.9f), 0.12f, 0.915f, 0.99f, 0.99f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Путь магната", "Tycoon's road"), 38, UIKit.Cream, TextAnchor.MiddleLeft, 0.02f, 0, 0.26f, 1, true);
            UIKit.Label(top.transform, L.T("Переездов: ", "Moves: ") + _save.rebirths + "   " + L.T("Доход ×", "Income ×") + GameData.RebirthMult(_save.rebirths).ToString("0.00"),
                26, UIKit.Hex("#A5D6A7"), TextAnchor.MiddleCenter, 0.26f, 0.45f, 0.62f, 1f, true);
            UIKit.Label(top.transform, GameData.RankName(GameData.RankOf(_save.lifetime)) + "  •  " + GameData.Money(_save.lifetime),
                22, UIKit.Alpha(UIKit.Cream, 0.75f), TextAnchor.MiddleCenter, 0.26f, 0f, 0.62f, 0.5f, true);
            UIKit.Label(top.transform, "<size=15><color=#FFE9A8>" + L.T("звёзды", "stars") + "</color></size>\n★ " + _save.stars, 30, UIKit.Gold, TextAnchor.MiddleCenter, 0.62f, 0, 0.72f, 1, true).lineSpacing = 0.8f;
            Crown(top.transform, 0.745f, 0.5f, 44f);
            UIKit.Label(top.transform, "<size=15><color=#FFE9A8>" + L.T("слава", "fame") + "</color></size>\n" + _save.fame, 30, UIKit.Gold, TextAnchor.MiddleLeft, 0.765f, 0, 0.83f, 1, true).lineSpacing = 0.8f;
            Icons.Coin(top.transform, 0.84f, 0.5f, 44f);
            UIKit.Label(top.transform, GameData.Money(_save.money), 34, UIKit.Gold, TextAnchor.MiddleLeft, 0.86f, 0, 0.99f, 1, true);
            UIKit.Button(s, L.T("Меню", "Menu"), UIKit.Steel, () => { Sfx.Play(Sound.Click); Platform.ShowFullscreen(ShowMenu); }, 0.005f, 0.915f, 0.115f, 0.99f, 32);
            BuildChestButton(s);

            BuildBottomBar(s);

            if (popupLoc >= 0) LocationPopup(s, locs[popupLoc]);
        }

        void BuildPin(RectTransform s, LocationDef l)
        {
            bool past = l.id < _save.location;
            bool here = l.id == _save.location;
            bool next = l.id == _save.location + 1;
            var pin = UIKit.Rect(s, "Pin" + l.id);
            UIKit.PlaceFixed(pin, l.mapX, l.mapY, 150f, 150f);
            var cg = pin.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = here || next || past ? 1f : 0.7f;

            if (here) UIKit.Shape(pin, UIKit.CircleHD, UIKit.Alpha(UIKit.Gold, 0.5f), 0.5f, 0.42f, 170f, 110f).gameObject.AddComponent<Blinker>().speed = 1.5f;
            UIKit.Shape(pin, UIKit.Circle, new Color(0, 0, 0, 0.25f), 0.5f, 0.2f, 124f, 40f);

            var col = here || past || next ? UIKit.Hex(l.kiosk) : UIKit.Gray;
            UIKit.Shape(pin, UIKit.RoundedSmall, UIKit.Hex(here || past ? "#EFE3CB" : "#BDBDBD"), 0.5f, 0.4f, 92f, 68f);
            UIKit.Shape(pin, UIKit.RoundedSmall, UIKit.Dark, 0.5f, 0.62f, 110f, 16f);
            for (int i = 0; i < 4; i++)
                UIKit.Shape(pin, UIKit.RoundedSmall, i % 2 == 0 ? col : Color.white, 0.35f + i * 0.1f, 0.55f, 22f, 18f);
            UIKit.Shape(pin, UIKit.RoundedSmall, UIKit.Hex("#3B2A1E"), 0.5f, 0.38f, 54f, 30f);

            if (past)
            {
                var ok = UIKit.Shape(pin, UIKit.Circle, UIKit.Green, 0.82f, 0.8f, 44f, 44f);
                UIKit.Shape(ok.transform, UIKit.RoundedSmall, Color.white, 0.4f, 0.42f, 6f, 16f, 45f);
                UIKit.Shape(ok.transform, UIKit.RoundedSmall, Color.white, 0.58f, 0.5f, 6f, 26f, -40f);
            }
            else if (!here)
            {
                var lk = UIKit.Rect(pin, "Lock");
                UIKit.PlaceFixed(lk, 0.8f, 0.8f, 50f, 50f);
                UIKit.Shape(lk, UIKit.Circle, UIKit.Dark, 0.5f, 0.68f, 34f, 34f);
                UIKit.Shape(lk, UIKit.Circle, UIKit.Hex("#BDBDBD"), 0.5f, 0.68f, 20f, 20f);
                UIKit.Shape(lk, UIKit.RoundedSmall, next ? UIKit.Gold : UIKit.Gray, 0.5f, 0.38f, 44f, 32f);
            }

            string nm = (l.id + 1) + ". " + l.Name;
            if (next) nm = l.Name + "\n" + L.T("Переезд: ", "Move: ") + GameData.Money(l.cost);
            bool twoLines = next || nm.Length > 20;
            var plate = UIKit.Panel(pin, "Name", UIKit.Alpha(here ? UIKit.Hex("#1B5E20") : UIKit.Dark, 0.92f), -0.3f, twoLines ? -0.26f : -0.1f, 1.3f, 0.1f);
            UIKit.AddShadow(plate, 4f, 0.35f);
            var pl = UIKit.FitLabel(plate.transform, nm, 24, next ? UIKit.Gold : UIKit.Cream, 0.04f, 0.06f, 0.96f, 0.94f);
            pl.resizeTextMinSize = 12;

            if (past && l.id < GameData.Locations.Length - 1) StaffPinTag(pin, l.id);

            if (here || next)
            {
                var arrow = UIKit.Rect(pin, "Arrow");
                UIKit.PlaceFixed(arrow, 0.5f, 1.02f, 34f, 34f);
                UIKit.Shape(arrow, UIKit.RoundedSmall, here ? UIKit.Red : UIKit.Gold, 0.5f, 0.5f, 28f, 28f, 45f);
                arrow.gameObject.AddComponent<Bobber>().amp = 8f;
            }

            var hit = UIKit.HitArea(pin, "Tap", () =>
            {
                Sfx.Play(Sound.Click);
                Juice.Punch(pin, 0.15f, 0.25f);
                LocationPopup((RectTransform)_screen.transform, l);
            });
            hit.transform.SetAsLastSibling();
        }

        /// <summary>Карточка локации: пройдена / ты здесь / переезд (ребёрт) / закрыта.</summary>
        void LocationPopup(RectTransform s, LocationDef l)
        {
            bool past = l.id < _save.location, here = l.id == _save.location, next = l.id == _save.location + 1;
            var c = UIKit.Popup(s, (l.id + 1) + ". " + l.Name, here || past || next ? UIKit.Hex(l.kiosk) : UIKit.Gray, 0.16f, 0.14f, 0.84f, 0.88f, null);

            var prev = UIKit.Rect(c, "Preview");
            UIKit.Place(prev, 0f, 0.22f, 0.44f, 1f);
            prev.gameObject.AddComponent<RectMask2D>();
            Scenery.Build(prev, l, false);
            var frame = UIKit.Panel(c, "Frame", new Color(0, 0, 0, 0), 0f, 0.22f, 0.44f, 1f);
            UIKit.AddOutline(frame, UIKit.Dark, 4f);

            UIKit.Label(c, l.Desc, 28, UIKit.Dark, TextAnchor.UpperLeft, 0.48f, 0.8f, 1f, 1f);
            Stat(c, 0.68f, 0, L.T("Цены", "Prices"), "×" + l.price.ToString("0.0#"), true);
            Stat(c, 0.55f, 1, L.T("Терпение", "Patience"), "×" + l.patience.ToString("0.0#"), l.patience >= 1f);
            Stat(c, 0.42f, 2, L.T("Поток клиентов", "Customer flow"), "×" + (1f / l.spawn).ToString("0.0#"), true);

            int a = 0, b = 1;
            for (int i = 0; i < l.weights.Length; i++) if (l.weights[i] > l.weights[a]) a = i;
            if (b == a) b = 0;
            for (int i = 0; i < l.weights.Length; i++) if (i != a && l.weights[i] > l.weights[b]) b = i;
            string guests = "";
            foreach (var cd in GameData.Customers)
            {
                if (cd.favLocs == null) continue;
                foreach (var fl in cd.favLocs)
                    if (fl == l.id) { guests += (guests.Length > 0 ? ", " : "") + cd.Name; break; }
            }
            string mostly = L.T("Чаще всего: ", "Mostly: ") + GameData.Get((CustType)a).Name + ", " + GameData.Get((CustType)b).Name;
            if (guests.Length > 0) mostly += "\n<size=20>" + L.T("Здесь бывают: ", "Also here: ") + guests + "</size>";
            UIKit.Label(c, mostly, 24, UIKit.Wood, TextAnchor.MiddleLeft, 0.48f, 0.26f, 1f, 0.41f, true);

            if (past)
            {
                StaffPanel(c, l.id);
            }
            else if (here)
            {
                UIKit.Label(c, L.T("Ты работаешь здесь", "You work here"), 30, UIKit.Green, TextAnchor.MiddleCenter, 0.48f, 0.02f, 1f, 0.2f, true);
                UIKit.Button(c, L.T("Начать день ", "Start day ") + _save.day, UIKit.Orange, () =>
                {
                    Sfx.Play(Sound.Click);
                    StartDayCinematic();
                }, 0f, 0f, 0.44f, 0.19f, 40);
            }
            else if (next)
            {
                // ПЕРЕЕЗД
                string why;
                bool can = CanRebirth(out why);
                var info = UIKit.Panel(c, "Rebirth", UIKit.Hex("#EDE7F6"), 0f, 0f, 1f, 0.28f);
                string txt = L.T("Переезд: начнёшь здесь с нуля — монеты, день и улучшения обнулятся.\nЗато навсегда: доход ×", "Moving: you start here from scratch — coins, day and upgrades reset.\nBut forever: income ×") +
                             GameData.RebirthMult(_save.rebirths + 1).ToString("0.00") + L.T(" и +", " and +") + Meta.FamePts(GameData.FameFor(l.id)) +
                             L.T(".\nНужно: ", ".\nNeed: ") + GameData.Money(l.cost) + L.T(" и день ", " and day ") + l.minDay;
                UIKit.Label(info.transform, txt, 24, UIKit.Dark, TextAnchor.MiddleLeft, 0.02f, 0.05f, 0.6f, 0.95f);
                var btn = UIKit.Button(info.transform, can ? L.T("ПЕРЕЕХАТЬ!", "MOVE!") : why, can ? UIKit.Purple : UIKit.Gray, () =>
                {
                    Sfx.Play(Sound.Click);
                    DoRebirth();
                }, 0.62f, 0.1f, 0.98f, 0.9f, 36);
                btn.interactable = can;
                if (can) btn.gameObject.AddComponent<Bobber>().amp = 4f;
            }
            else
            {
                UIKit.Label(c, L.T("Сначала переедь на предыдущие точки", "Move through the previous spots first"), 30, UIKit.Red, TextAnchor.MiddleCenter, 0f, 0f, 1f, 0.2f, true);
            }
        }

        void Stat(RectTransform c, float y, int icon, string name, string value, bool good)
        {
            var row = UIKit.Panel(c, "Stat", UIKit.Alpha(UIKit.Dark, 0.07f), 0.48f, y, 1f, y + 0.11f);
            if (icon == 0) Icons.Coin(row.transform, 0.08f, 0.5f, 44f);
            else if (icon == 1) Icons.Clock(row.transform, 0.08f, 0.5f, 44f);
            else Icons.People(row.transform, 0.08f, 0.5f, 48f);
            UIKit.Label(row.transform, name, 28, UIKit.Dark, TextAnchor.MiddleLeft, 0.17f, 0f, 0.7f, 1f);
            UIKit.Label(row.transform, value, 32, good ? UIKit.Green : UIKit.Red, TextAnchor.MiddleRight, 0.7f, 0f, 0.96f, 1f, true);
        }

        // ---------- нижняя панель: точка, бустеры, слава, улучшения, старт ----------
        void BuildBottomBar(RectTransform s)
        {
            var bar = UIKit.Panel(s, "Bottom", UIKit.Alpha(UIKit.Dark, 0.92f), 0.005f, 0.01f, 0.995f, 0.2f);
            UIKit.AddShadow(bar, 10f, 0.4f);

            var loc = _save.Loc;
            UIKit.Label(bar.transform, loc.Name, 30, UIKit.Cream, TextAnchor.MiddleLeft, 0.01f, 0.5f, 0.18f, 0.95f, true);
            UIKit.Label(bar.transform, L.T("День ", "Day ") + _save.day + "  •  " + L.T("цель ", "goal ") + GameData.Money(GameData.Goal(_save.day, loc)),
                22, UIKit.Gold, TextAnchor.MiddleLeft, 0.01f, 0.08f, 0.18f, 0.5f, true);

            for (int i = 0; i < Meta.Boosts.Length; i++) BoosterCard(bar.transform, i, 0.185f + i * 0.13f);

            var fame = UIKit.Button(bar.transform, "", UIKit.Hex("#6A1B9A"), () => { Sfx.Play(Sound.Click); ShowFame(); }, 0.585f, 0.1f, 0.69f, 0.9f, 30);
            Crown(fame.transform, 0.2f, 0.5f, 52f);
            UIKit.FitLabel(fame.transform, L.T("Очки славы", "Fame points"), 24, UIKit.Cream, 0.36f, 0.52f, 0.97f, 0.92f);
            UIKit.FitLabel(fame.transform, _save.fame.ToString(), 38, UIKit.Gold, 0.36f, 0.08f, 0.97f, 0.54f);
            UIKit.Button(bar.transform, L.T("Улучшить ларёк", "Upgrade kiosk"), UIKit.Purple, () => { Sfx.Play(Sound.Click); ShowShop(-1); }, 0.7f, 0.1f, 0.83f, 0.9f, 32);
            var start = UIKit.Button(bar.transform, L.T("Начать день ", "Start day ") + _save.day, UIKit.Orange, () =>
            {
                Sfx.Play(Sound.Click);
                StartDayCinematic();
            }, 0.84f, 0.08f, 0.995f, 0.92f, 40);
            start.gameObject.AddComponent<Bobber>().amp = 3f;
        }

        void BoosterCard(Transform bar, int i, float x0)
        {
            var def = Meta.Boosts[i];
            int owned = _save.boosts[i];
            bool on = _save.boostOn[i] == 1 && owned > 0;
            var card = UIKit.Panel(bar, "Boost", on ? UIKit.Hex("#FFF3C4") : UIKit.Cream, x0, 0.06f, x0 + 0.125f, 0.9f);
            if (on) UIKit.AddOutline(card, UIKit.Green, 4f);
            UIKit.MakeButton(card, () => { Sfx.Play(Sound.Click); BoosterPopup(i); });

            BoostIcon(card.transform, def.id, 0.2f, 0.62f, 60f);
            UIKit.FitLabel(card.transform, def.Name, 22, UIKit.Dark, 0.38f, 0.62f, 0.98f, 0.95f);
            UIKit.FitLabel(card.transform, L.T("есть: ", "have: ") + owned, 20, UIKit.Wood, 0.38f, 0.36f, 0.98f, 0.6f);
            var st = UIKit.Panel(card.transform, "State", on ? UIKit.Green : (owned > 0 ? UIKit.Gray : UIKit.Orange), 0.06f, 0.06f, 0.94f, 0.32f);
            UIKit.FitLabel(st.transform, on ? L.T("ВКЛ на день", "ON for the day") : (owned > 0 ? L.T("выкл — нажми", "off — tap") : L.T("нажми, чтобы узнать", "tap for info")),
                18, Color.white, 0.04f, 0.05f, 0.96f, 0.95f);
        }

        static void BoostIcon(Transform p, Boost id, float ax, float ay, float size)
        {
            if (id == Boost.Speed) Icons.Cup(p, ax, ay, size);
            else if (id == Boost.Ads) Icons.Flyer(p, ax, ay, size);
            else Icons.GoldLavash(p, ax, ay, size * 0.92f);
        }

        /// <summary>Карточка бустера: что делает, сколько есть, купить / за рекламу / взять на день.</summary>
        void BoosterPopup(int i)
        {
            var def = Meta.Boosts[i];
            var c = UIKit.Popup((RectTransform)_screen.transform, def.Name, UIKit.Orange, 0.26f, 0.2f, 0.74f, 0.82f, null);
            var pic = UIKit.Panel(c, "Pic", UIKit.Hex("#FFF8E1"), 0f, 0.35f, 0.36f, 1f);
            BoostIcon(pic.transform, def.id, 0.5f, 0.55f, 200f);

            UIKit.Label(c, L.T("Что даёт на один день:", "Effect for one day:"), 26, UIKit.Gray, TextAnchor.UpperLeft, 0.4f, 0.86f, 1f, 1f, true);
            UIKit.Label(c, def.Desc, 34, UIKit.Dark, TextAnchor.UpperLeft, 0.4f, 0.55f, 1f, 0.86f, true);
            string how = def.id == Boost.Speed ? L.T("Вертел и нож режут в 2 раза быстрее, гриль и фритюр готовят на 20% быстрее.", "Spit and knife cut 2× faster, grill and fryer cook 20% faster.")
                : (def.id == Boost.Ads ? L.T("Клиенты приходят на 25% чаще — больше заказов и денег за день.", "Customers come 25% more often — more orders and money per day.")
                : L.T("Каждый заказ стоит на 25% дороже. Отлично вместе с золотыми клиентами!", "Every order costs 25% more. Great with golden customers!"));
            UIKit.Label(c, how, 24, UIKit.Wood, TextAnchor.UpperLeft, 0.4f, 0.36f, 1f, 0.56f);
            UIKit.Label(c, L.T("У тебя: ", "You have: ") + _save.boosts[i] + L.T(" шт.  Тратится 1 в начале дня, если включён.", ".  Uses 1 at day start if ON."),
                22, UIKit.Dark, TextAnchor.MiddleLeft, 0f, 0.24f, 1f, 0.34f, true);

            int bcost = Meta.BoostCost(i, _save.location);
            var buy = UIKit.PriceButton(c, L.T("Купить", "Buy"), bcost, UIKit.Green, () =>
            {
                if (_save.money < bcost) { Sfx.Play(Sound.Error); return; }
                _save.money -= bcost;
                _save.boosts[i]++;
                _save.boostOn[i] = 1;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                BuildMap(-1);
                BoosterPopup(i);
            }, 0f, 0f, 0.32f, 0.2f, 30);
            buy.interactable = _save.money >= bcost;
            UIKit.Button(c, L.T("Бесплатно за рекламу", "Free for an ad"), UIKit.Orange, () =>
            {
                Platform.ShowRewarded("boost_" + i, () =>
                {
                    _save.boosts[i]++;
                    _save.boostOn[i] = 1;
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Coin);
                    BuildMap(-1);
                    BoosterPopup(i);
                }, null);
            }, 0.34f, 0f, 0.66f, 0.2f, 26);
            bool on = _save.boostOn[i] == 1 && _save.boosts[i] > 0;
            var tg = UIKit.Button(c, on ? L.T("Не брать", "Turn off") : L.T("Взять на день", "Use today"), on ? UIKit.Gray : UIKit.Blue, () =>
            {
                if (_save.boosts[i] <= 0) { Sfx.Play(Sound.Error); return; }
                _save.boostOn[i] = _save.boostOn[i] == 1 ? 0 : 1;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Click);
                BuildMap(-1);
            }, 0.68f, 0f, 1f, 0.2f, 28);
            tg.interactable = _save.boosts[i] > 0;
        }

        // ---------- сундуки за звёзды ----------
        int ChestsAvailable() { return _save.stars / 10 - _save.chests; }

        void BuildChestButton(RectTransform s)
        {
            int n = ChestsAvailable();
            var holder = UIKit.Rect(s, "Chest");
            UIKit.PlaceFixed(holder, 0.06f, 0.8f, 150f, 130f);
            var bg = UIKit.Shape(holder, UIKit.Circle, UIKit.Alpha(UIKit.Dark, 0.75f), 0.5f, 0.55f, 120f, 120f);
            Icons.Chest(holder, 0.5f, 0.6f, 90f, false);
            int prog = _save.stars % 10;
            UIKit.FitLabel(holder, n > 0 ? L.T("Открыть!", "Open!") : "★ " + prog + "/10", 22, n > 0 ? UIKit.Gold : UIKit.Cream, 0f, 0f, 1f, 0.2f);
            if (n > 0)
            {
                holder.gameObject.AddComponent<Bobber>().amp = 6f;
                var bd = UIKit.Shape(holder, UIKit.Circle, UIKit.Red, 0.85f, 0.9f, 36f, 36f);
                UIKit.FitLabel(bd.transform, n.ToString(), 22, Color.white, 0f, 0f, 1f, 1f);
            }
            UIKit.HitArea(holder, "Tap", () =>
            {
                Sfx.Play(Sound.Click);
                ChestPopup();
            });
        }

        void ChestPopup()
        {
            int n = ChestsAvailable();
            var c = UIKit.Popup((RectTransform)_screen.transform, L.T("Сундук за звёзды", "Star chest"), UIKit.Hex("#8D5524"), 0.3f, 0.22f, 0.7f, 0.8f, null);
            if (n <= 0)
            {
                Icons.Chest(c, 0.5f, 0.6f, 220f, false);
                UIKit.Label(c, L.T("Каждые 10 звёзд — сундук с монетами и бустером.\nСейчас: ", "Every 10 stars — a chest with coins and a booster.\nNow: ") + (_save.stars % 10) + "/10",
                    28, UIKit.Dark, TextAnchor.MiddleCenter, 0f, 0f, 1f, 0.3f, true);
                return;
            }
            // открыть
            _save.chests++;
            long coins = Meta.PassCoins(_save, 10, false);
            int b = Random.Range(0, 3);
            _save.money += coins;
            _save.boosts[b]++;
            SaveSystem.Save(_save);
            var chest = Icons.Chest(c, 0.5f, 0.62f, 220f, true);
            Juice.Punch(chest, 0.3f, 0.5f);
            Juice.Burst(c, UIKit.Gold, 20, 0.5f, 0.7f, 20f);
            Sfx.Play(Sound.Fanfare);
            UIKit.Label(c, "+" + GameData.Money(coins) + L.T(" монет и бустер «", " coins and booster “") + Meta.Boosts[b].Name + L.T("»!", "”!"),
                32, UIKit.Green, TextAnchor.MiddleCenter, 0f, 0.12f, 1f, 0.32f, true);
            UIKit.Button(c, L.T("Отлично!", "Great!"), UIKit.Green, () => BuildMap(-1), 0.25f, 0f, 0.75f, 0.12f, 32);
        }

        // =====================================================================
        //  СОТРУДНИКИ НА КАРТЕ: над пройденной локацией — ценник найма или статус повара
        // =====================================================================
        void StaffPinTag(RectTransform pin, int loc)
        {
            bool hired = Meta.StaffHired(_save, loc);
            var tag = UIKit.Rect(pin, "StaffTag");
            UIKit.PlaceFixed(tag, 0.5f, 1.2f, 146f, 52f);
            if (!hired && !Payments.Available("staff_" + loc)) { Destroy(tag.gameObject); return; }
            if (!hired)
            {
                var bg = UIKit.Shape(tag, UIKit.RoundedSmall, UIKit.Hex("#FF8F00"), 0.5f, 0.5f, 146f, 52f);
                UIKit.AddOutline(bg, UIKit.Hex("#FFF3C4"), 2f);
                UIKit.AddShadow(bg, 4f, 0.35f);
                // поварской колпак
                UIKit.Shape(tag, UIKit.CircleHD, Color.white, 0.16f, 0.62f, 26f, 22f);
                UIKit.Shape(tag, UIKit.RoundedSmall, Color.white, 0.16f, 0.38f, 22f, 10f);
                UIKit.FitLabel(tag, L.T("Нанять повара", "Hire a cook"), 15, Color.white, 0.3f, 0.52f, 0.98f, 0.95f);
                Payments.PriceTag(tag, "staff_" + loc, Meta.StaffYan[Mathf.Clamp(loc, 0, Meta.StaffYan.Length - 1)], 0.3f, 0.04f, 0.98f, 0.54f, 18, UIKit.Hex("#3E1A00"));
                // хвостик-указатель к ларьку
                UIKit.Shape(tag, UIKit.RoundedSmall, UIKit.Hex("#FF8F00"), 0.5f, -0.02f, 16f, 16f, 45f);
                var bb = tag.gameObject.AddComponent<Bobber>(); bb.amp = 3f; bb.speed = 2f;
            }
            else
            {
                long cash = _save.staffCash[loc];
                bool idle = _save.staffStock[loc] == 0;
                var bg = UIKit.Shape(tag, UIKit.RoundedSmall, cash > 0 ? UIKit.Green : (idle ? UIKit.Red : UIKit.Hex("#1565C0")), 0.5f, 0.5f, 146f, 52f);
                UIKit.AddOutline(bg, Color.white, 2f);
                UIKit.Shape(tag, UIKit.CircleHD, UIKit.Hex("#F6D2B0"), 0.15f, 0.45f, 24f, 24f);
                UIKit.Shape(tag, UIKit.CircleHD, Color.white, 0.15f, 0.78f, 24f, 16f);
                string txt = cash > 0 ? "+" + GameData.Money(cash) : (idle ? L.T("Нет продуктов!", "No food!") : L.T("Повар работает", "Cook at work"));
                UIKit.FitLabel(tag, txt, 18, Color.white, 0.28f, 0.1f, 0.97f, 0.9f);
                UIKit.Shape(tag, UIKit.RoundedSmall, bg.color, 0.5f, -0.02f, 16f, 16f, 45f);
                if (cash > 0 || idle) { var bb = tag.gameObject.AddComponent<Bobber>(); bb.amp = 3f; bb.speed = 2.5f; }
            }
        }

        /// <summary>Нижняя часть карточки пройденной локации: найм повара или управление им.</summary>
        void StaffPanel(RectTransform c, int loc)
        {
            var box = UIKit.Panel(c, "StaffBox", UIKit.Hex("#E3F2FD"), 0f, 0f, 1f, 0.24f);
            UIKit.AddOutline(box, UIKit.Hex("#1565C0"), 2f);
            var bt = box.transform;
            if (loc >= GameData.Locations.Length - 1)
            {
                UIKit.Label(bt, L.T("Пройдено!", "Done!"), 30, UIKit.Green, TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f, true);
                return;
            }
            long price = Meta.StaffPrice(_save, loc), unit = Meta.StaffUnitCost(_save, loc);
            long perDay = Meta.StaffPortions * (price - unit);
            // повар
            var chef = UIKit.Rect(bt, "Chef");
            UIKit.PlaceFixed(chef, 0.06f, 0.5f, 90f, 90f);
            UIKit.Shape(chef, UIKit.CircleHD, UIKit.Hex("#BBDEFB"), 0.5f, 0.5f, 90f, 90f);
            UIKit.Shape(chef, UIKit.CircleHD, UIKit.Hex("#F6D2B0"), 0.5f, 0.42f, 44f, 44f);
            UIKit.Shape(chef, UIKit.CircleHD, Color.white, 0.4f, 0.74f, 26f, 24f);
            UIKit.Shape(chef, UIKit.CircleHD, Color.white, 0.6f, 0.74f, 26f, 24f);
            UIKit.Shape(chef, UIKit.CircleHD, Color.white, 0.5f, 0.8f, 30f, 26f);
            UIKit.Shape(chef, UIKit.RoundedSmall, Color.white, 0.5f, 0.64f, 44f, 12f);

            if (!Meta.StaffHired(_save, loc))
            {
                UIKit.Label(bt, L.T("Найми повара — он будет торговать здесь без тебя", "Hire a cook — they'll sell here without you"), 24, UIKit.Dark, TextAnchor.MiddleLeft, 0.13f, 0.52f, 0.66f, 0.95f, true);
                UIKit.Label(bt, L.T("Прибыль ≈ ", "Profit ≈ ") + GameData.Money(perDay) + L.T(" за каждый твой рабочий день (растёт с твоей локацией)", " per work day of yours (grows with your location)"),
                    20, UIKit.Hex("#1565C0"), TextAnchor.MiddleLeft, 0.13f, 0.08f, 0.66f, 0.5f, false);
                int yan = Meta.StaffYan[Mathf.Clamp(loc, 0, Meta.StaffYan.Length - 1)];
                if (!Payments.Available("staff_" + loc))
                {
                    UIKit.FitLabel(bt, Payments.Ready ? L.T("Найм пока недоступен", "Hiring is not available yet") : L.T("Загрузка магазина...", "Loading the shop..."),
                        22, UIKit.Gray, 0.68f, 0.1f, 0.98f, 0.9f);
                    return;
                }
                var hb = UIKit.Button(bt, "", UIKit.Hex("#FF8F00"), () =>
                {
                    Payments.Buy("staff_" + loc, yan, () =>
                    {
                        _save.staffHired[loc] = 1;
                        _save.staffStock[loc] = Meta.StaffPortions;
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Fanfare);
                        Notify(L.T("Нанят повар", "Cook hired"), GameData.Locations[loc].Name);
                        BuildMap(loc);
                    });
                }, 0.68f, 0.1f, 0.98f, 0.9f, 30);
                UIKit.SetLabel(hb, "");
                UIKit.FitLabel(hb.transform, L.T("Нанять", "Hire"), 32, Color.white, 0.05f, 0.45f, 0.95f, 0.95f);
                Payments.PriceTag(hb.transform, "staff_" + loc, yan, 0.1f, 0.04f, 0.9f, 0.46f, 24, UIKit.Hex("#FFF3C4"));
                hb.gameObject.AddComponent<Bobber>().amp = 2f;
                return;
            }

            int stock = _save.staffStock[loc];
            UIKit.Label(bt, L.T("Повар работает. Склад: ", "Cook at work. Stock: ") + stock + "/" + Meta.StaffStorage +
                        (stock == 0 ? L.T("  — нет продуктов!", "  — no food!") : ""), 22, stock == 0 ? UIKit.Red : UIKit.Dark, TextAnchor.MiddleLeft, 0.13f, 0.55f, 0.52f, 0.95f, true);
            var bar = UIKit.Bar(bt, UIKit.Alpha(UIKit.Dark, 0.15f), stock > 0 ? UIKit.Green : UIKit.Red, 0.13f, 0.38f, 0.52f, 0.52f);
            UIKit.SetFill(bar, (float)stock / Meta.StaffStorage);
            UIKit.Label(bt, L.T("Вчера продал: ", "Sold yesterday: ") + _save.staffSold[loc] + L.T(" • ≈", " • ≈") + GameData.Money(perDay) + L.T("/день", "/day"),
                18, UIKit.Gray, TextAnchor.MiddleLeft, 0.13f, 0.05f, 0.52f, 0.36f, false);

            int room = Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - stock);
            long cost = room * unit;
            var fb = UIKit.PriceButton(bt, L.T("Закупить", "Buy food"), cost, UIKit.Blue, () =>
            {
                int r = Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - _save.staffStock[loc]);
                long cc = r * Meta.StaffUnitCost(_save, loc);
                if (r <= 0 || _save.money < cc) { Sfx.Play(Sound.Error); return; }
                _save.money -= cc;
                _save.staffStock[loc] += r;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                BuildMap(loc);
            }, 0.54f, 0.1f, 0.75f, 0.9f, 22);
            fb.interactable = room > 0 && _save.money >= cost;
            long cash = _save.staffCash[loc];
            var cb = UIKit.Button(bt, cash > 0 ? L.T("Забрать +", "Collect +") + GameData.Money(cash) : L.T("Касса пуста", "No cash"), cash > 0 ? UIKit.Green : UIKit.Gray, () =>
            {
                long got = _save.staffCash[loc];
                if (got <= 0) { Sfx.Play(Sound.Error); return; }
                _save.money += got; _save.lifetime += got;
                _save.staffCash[loc] = 0;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                CheckAchievements();
                BuildMap(loc);
            }, 0.77f, cash > 0 ? 0.52f : 0.1f, 0.98f, 0.9f, 22);
            if (cash > 0)
            {
                cb.gameObject.AddComponent<Bobber>().amp = 2f;
                // выручка ×2 за рекламу
                UIKit.Button(bt, L.T("×2 за рекламу", "×2 for an ad"), UIKit.Orange, () =>
                {
                    Platform.ShowRewarded("staff_x2", () =>
                    {
                        long got = _save.staffCash[loc] * 2;
                        if (got <= 0) return;
                        _save.money += got; _save.lifetime += got;
                        _save.staffCash[loc] = 0;
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Coin);
                        Notify(L.T("Выручка ×2", "Cash ×2"), "+" + GameData.Money(got));
                        CheckAchievements();
                        BuildMap(loc);
                    }, null);
                }, 0.77f, 0.1f, 0.98f, 0.48f, 20);
            }
        }
    }
}
