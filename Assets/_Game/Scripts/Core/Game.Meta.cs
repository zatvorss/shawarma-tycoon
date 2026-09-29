using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Достижения (кубки-маятники, забрать награду), бизнесы, колесо удачи.</summary>
    public partial class Game
    {
        // =====================================================================
        //  ДОСТИЖЕНИЯ
        // =====================================================================
        void ShowAchievements()
        {
            var s = NewScreen("Achievements");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Dark, 0.02f, 0.84f, 0.98f, 0.92f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Достижения", "Achievements"), 40, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.3f, 1, true);
            Icons.Star(top.transform, 0.62f, 0.5f, 40f, true);
            UIKit.Label(top.transform, Meta.TiersClaimed(_save) + " / " + Meta.MaxTiers, 34, UIKit.Cream, TextAnchor.MiddleLeft, 0.64f, 0, 0.8f, 1, true);
            int ready = Meta.ReadyCount(_save);
            if (ready > 0)
                UIKit.Label(top.transform, L.T("Готово к получению: ", "Ready to claim: ") + ready, 28, UIKit.Green, TextAnchor.MiddleRight, 0.78f, 0, 0.99f, 1, true);

            var list = Meta.Achievements;
            for (int i = 0; i < list.Length; i++)
            {
                var a = list[i];
                int tier = _save.achTier[i];
                bool can = Meta.Ready(_save, i);
                bool maxed = tier >= AchDef.Tiers;
                int col = i % 4, row = i / 4;
                float x0 = 0.015f + col * 0.244f, y1 = 0.825f - row * 0.18f;
                var card = UIKit.Panel(s, "Ach", can ? UIKit.Hex("#FFF3C4") : (maxed ? UIKit.Hex("#F1F8E9") : UIKit.Cream), x0, y1 - 0.17f, x0 + 0.236f, y1);
                UIKit.AddShadow(card, 5f, 0.3f);
                if (can) UIKit.AddOutline(card, UIKit.Gold, 4f);

                // кубок: серый, пока нет ни одного уровня; качается, когда можно забрать
                var trophy = Icons.Trophy(card.transform, 0.15f, 0.42f, 84f, tier > 0 || can);
                if (can)
                {
                    trophy.pivot = new Vector2(0.5f, 1f);
                    trophy.anchoredPosition = new Vector2(0f, 42f);
                    trophy.gameObject.AddComponent<Pendulum>();
                }
                // звёзды уровней над кубком
                for (int t = 0; t < AchDef.Tiers; t++)
                {
                    float ang = Mathf.Lerp(150f, 30f, t / 4f) * Mathf.Deg2Rad;
                    Icons.Star(card.transform, 0.15f + Mathf.Cos(ang) * 0.1f, 0.62f + Mathf.Sin(ang) * 0.25f, 22f, t < tier);
                }

                UIKit.Label(card.transform, a.Name, 26, UIKit.Dark, TextAnchor.MiddleLeft, 0.3f, 0.66f, 0.99f, 0.96f, true);
                if (maxed)
                {
                    UIKit.Label(card.transform, L.T("Все 5 уровней пройдены!", "All 5 levels done!"), 20, UIKit.Hex("#558B2F"), TextAnchor.MiddleLeft, 0.3f, 0.36f, 0.99f, 0.66f, true);
                    continue;
                }
                UIKit.Label(card.transform, L.T("Ур. ", "Lv. ") + (tier + 1) + ": " + a.Desc(tier), 20, UIKit.Wood, TextAnchor.MiddleLeft, 0.3f, 0.4f, 0.99f, 0.66f);

                if (can)
                {
                    var lbl = UIKit.FitLabel(card.transform, L.T("Нажми — забрать +", "Tap — claim +") + GameData.Money(Meta.AchReward(_save, i, tier)), 22, UIKit.Green, 0.3f, 0.06f, 0.99f, 0.38f);
                    lbl.gameObject.AddComponent<Bobber>().amp = 3f;
                    int idx = i;
                    UIKit.MakeButton(card, () =>
                    {
                        long got = Meta.Claim(_save, idx);
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Coin);
                        Sfx.Play(Sound.Fanfare);
                        Notify(a.Name + " — " + L.T("уровень ", "level ") + _save.achTier[idx], "+" + GameData.Money(got));
                        ShowAchievements();
                    });
                }
                else
                {
                    int v = Mathf.Min(a.value(_save), a.targets[tier]);
                    var fill = UIKit.Bar(card.transform, UIKit.Alpha(UIKit.Dark, 0.15f), UIKit.Green, 0.3f, 0.1f, 0.72f, 0.3f);
                    UIKit.SetFill(fill, (float)v / a.targets[tier]);
                    UIKit.FitLabel(card.transform, AchDef.Fmt(v * a.scale) + "/" + AchDef.Fmt(a.targets[tier] * a.scale), 16, UIKit.Dark, 0.3f, 0.1f, 0.72f, 0.3f);
                    UIKit.FitLabel(card.transform, "+" + GameData.Money(Meta.AchReward(_save, i, tier)), 20, UIKit.Gray, 0.74f, 0.08f, 0.99f, 0.32f);
                }
            }
            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.01f, 0.2f, 0.1f, 36);
        }

        // =====================================================================
        //  БИЗНЕСЫ
        // =====================================================================
        void ShowBusinesses()
        {
            var s = NewScreen("Business");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Dark, 0.02f, 0.84f, 0.98f, 0.92f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Бизнесы", "Businesses"), 40, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.2f, 1, true);
            UIKit.Label(top.transform, L.T("Каждый бизнес — своё событие в рабочем дне. Остаются навсегда, даже после переезда!",
                "Each business adds its own event to your work day. They stay forever, even after moving!"),
                24, UIKit.Green, TextAnchor.MiddleCenter, 0.2f, 0, 0.78f, 1, true);
            Icons.Coin(top.transform, 0.8f, 0.5f, 46f);
            UIKit.Label(top.transform, GameData.Money(_save.money), 38, UIKit.Gold, TextAnchor.MiddleLeft, 0.82f, 0, 0.99f, 1, true);

            BillionBar(s, 0.25f, 0.755f, 0.75f, 0.815f);

            var list = Meta.Businesses;
            for (int i = 0; i < list.Length; i++)
            {
                var b = list[i];
                int lvl = _save.biz[i];
                bool locked = _save.location < b.unlockLoc;
                float x0 = 0.02f + i * 0.325f;
                var card = UIKit.Panel(s, "Biz", locked ? UIKit.Hex("#D7D2CB") : UIKit.Cream, x0, 0.13f, x0 + 0.315f, 0.73f);
                UIKit.AddShadow(card, 6f, 0.35f);
                if (!locked && lvl > 0) UIKit.AddOutline(card, UIKit.Hex(b.color), 4f);

                var pic = UIKit.Panel(card.transform, "Pic", UIKit.Hex("#BBDEFB"), 0.04f, 0.56f, 0.96f, 0.97f);
                DrawBiz(pic.transform, i, !locked && lvl > 0, locked);
                if (i == Meta.BizKiosk2 && lvl > 0)
                {
                    var mb = UIKit.Button(pic.transform, L.T("Управлять →", "Manage →"), UIKit.Green, () => { Sfx.Play(Sound.Click); ShowKiosk2(); }, 0.52f, 0.04f, 0.97f, 0.26f, 24);
                    if (_save.kiosk2Cash > 0 || _save.kiosk2Stock == 0) mb.gameObject.AddComponent<Bobber>().amp = 3f;
                }

                UIKit.FitLabel(card.transform, b.Name, 34, UIKit.Dark, 0.04f, 0.47f, 0.96f, 0.555f);
                UIKit.Label(card.transform, b.Desc, 20, UIKit.Wood, TextAnchor.UpperLeft, 0.05f, 0.3f, 0.95f, 0.47f);

                // уровни — точки
                for (int l = 1; l <= BizDef.MaxLevel; l++)
                    UIKit.Shape(card.transform, UIKit.Circle, l <= lvl ? UIKit.Hex(b.color) : UIKit.Alpha(UIKit.Dark, 0.2f),
                        0.5f + (l - 3) * 0.08f, 0.28f, 22f, 22f);

                if (locked)
                {
                    var lk = UIKit.Panel(card.transform, "Locked", UIKit.Gray, 0.05f, 0.03f, 0.95f, 0.24f);
                    UIKit.FitLabel(lk.transform, L.T("Откроется на локации ", "Unlocks at location ") + (b.unlockLoc + 1) + "\n" +
                        GameData.Locations[b.unlockLoc].Name, 28, Color.white, 0.04f, 0.06f, 0.96f, 0.94f);
                    continue;
                }

                string now = lvl > 0 ? L.T("Сейчас: ", "Now: ") + b.LevelDesc(lvl) : L.T("Ещё не куплен", "Not owned yet");
                string nxt = lvl < BizDef.MaxLevel ? "\n<color=#2E7D32>" + L.T("Далее: ", "Next: ") + b.LevelDesc(lvl + 1) + "</color>" : "";
                UIKit.Label(card.transform, now + nxt, 20, UIKit.Blue, TextAnchor.MiddleCenter, 0.03f, 0.19f, 0.97f, 0.26f, true);

                if (lvl >= BizDef.MaxLevel)
                {
                    UIKit.Label(card.transform, L.T("Максимум!", "Maxed!"), 32, UIKit.Gold, TextAnchor.MiddleCenter, 0.05f, 0.03f, 0.95f, 0.17f, true);
                    continue;
                }
                long cost = Meta.BizCost(i, lvl, _save.location);
                bool can = _save.money >= cost;
                int idx = i;
                string txt = lvl == 0 ? L.T("Открыть", "Open") : L.T("Улучшить", "Upgrade");
                var btn = UIKit.PriceButton(card.transform, txt, cost, can ? UIKit.Green : UIKit.Gray, () =>
                {
                    long c = Meta.BizCost(idx, _save.biz[idx], _save.location);
                    if (_save.money < c) { Sfx.Play(Sound.Error); return; }
                    _save.money -= c;
                    _save.biz[idx]++;
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Fanfare);
                    CheckAchievements();
                    ShowBusinesses();
                }, 0.05f, 0.03f, 0.95f, 0.17f, 30);
                btn.interactable = can;
            }
            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.01f, 0.2f, 0.1f, 36);
        }

        // =====================================================================
        //  ВТОРОЙ ЛАРЁК: закупка продуктов, склад, касса
        // =====================================================================
        void ShowKiosk2()
        {
            var s = NewScreen("Kiosk2");
            Backdrop(s, true);
            int l = _save.biz[Meta.BizKiosk2];
            int cap = Meta.Kiosk2Cap(l), storage = Meta.Kiosk2Storage(l);
            long price = Meta.Kiosk2Price(_save.location), unit = Meta.Kiosk2UnitCost(l, _save.location);

            var top = UIKit.Panel(s, "Top", UIKit.Hex("#1B5E20"), 0.02f, 0.84f, 0.98f, 0.92f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Второй ларёк", "Second kiosk"), 42, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.4f, 1, true);
            UIKit.Label(top.transform, L.T("Уровень ", "Level ") + l + " / " + BizDef.MaxLevel, 30, Color.white, TextAnchor.MiddleCenter, 0.4f, 0, 0.62f, 1, true);
            Icons.Coin(top.transform, 0.8f, 0.5f, 46f);
            UIKit.Label(top.transform, GameData.Money(_save.money), 38, UIKit.Gold, TextAnchor.MiddleLeft, 0.82f, 0, 0.99f, 1, true);

            // картинка
            var pic = UIKit.Panel(s, "Pic", UIKit.Hex("#BBDEFB"), 0.03f, 0.3f, 0.36f, 0.8f);
            UIKit.AddShadow(pic, 6f, 0.35f);
            DrawBiz(pic.transform, Meta.BizKiosk2, true, false);

            // цифры
            var info = UIKit.Panel(s, "Info", UIKit.Cream, 0.39f, 0.3f, 0.97f, 0.8f);
            UIKit.AddShadow(info, 6f, 0.35f);
            var it = info.transform;
            string[] names = { L.T("Продаёт в день", "Sells per day"), L.T("Цена порции", "Portion price"), L.T("Закупка порции", "Portion cost"),
                               L.T("Прибыль с порции", "Profit per portion"), L.T("Вчера продано", "Sold yesterday") };
            string[] vals = { cap + L.T(" порций", " portions"), GameData.Money(price), GameData.Money(unit), GameData.Money(price - unit),
                              _save.kiosk2LastSold + L.T(" порций", " portions") };
            for (int i = 0; i < names.Length; i++)
            {
                float y1 = 0.96f - i * 0.1f;
                UIKit.Label(it, names[i], 28, UIKit.Wood, TextAnchor.MiddleLeft, 0.04f, y1 - 0.09f, 0.6f, y1, false);
                UIKit.Label(it, vals[i], 30, UIKit.Dark, TextAnchor.MiddleRight, 0.55f, y1 - 0.09f, 0.96f, y1, true);
            }
            // склад
            UIKit.Label(it, L.T("Склад продуктов: ", "Food in stock: ") + _save.kiosk2Stock + " / " + storage, 28, UIKit.Dark, TextAnchor.MiddleLeft, 0.04f, 0.36f, 0.96f, 0.45f, true);
            var bar = UIKit.Bar(it, UIKit.Alpha(UIKit.Dark, 0.15f), UIKit.Green, 0.04f, 0.3f, 0.96f, 0.36f);
            UIKit.SetFill(bar, storage > 0 ? (float)_save.kiosk2Stock / storage : 0f);
            int days = cap > 0 ? _save.kiosk2Stock / cap : 0;
            UIKit.Label(it, _save.kiosk2Stock == 0 ? L.T("Склад пуст — ларёк простаивает! Закупи продукты.", "Stock is empty — the kiosk is idle! Buy food.")
                                                  : L.T("Хватит примерно на дней: ", "Enough for about days: ") + Mathf.Max(1, days),
                22, _save.kiosk2Stock == 0 ? UIKit.Red : UIKit.Gray, TextAnchor.MiddleLeft, 0.04f, 0.2f, 0.96f, 0.29f, true);

            // закупка
            int room = storage - _save.kiosk2Stock;
            int buyDay = Mathf.Min(cap, room);
            long costDay = buyDay * unit;
            var b1 = UIKit.PriceButton(it, L.T("На 1 день", "For 1 day"), costDay, UIKit.Blue, () => BuyKiosk2(buyDay), 0.04f, 0.03f, 0.48f, 0.17f, 28);
            b1.interactable = buyDay > 0 && _save.money >= costDay;
            long costFull = room * unit;
            var b2 = UIKit.PriceButton(it, L.T("Заполнить склад", "Fill the stock"), costFull, UIKit.Blue, () => BuyKiosk2(room), 0.52f, 0.03f, 0.96f, 0.17f, 28);
            b2.interactable = room > 0 && _save.money >= costFull;

            // касса
            var cash = UIKit.Button(s, "", _save.kiosk2Cash > 0 ? UIKit.Green : UIKit.Gray, () =>
            {
                if (_save.kiosk2Cash <= 0) { Sfx.Play(Sound.Error); return; }
                long got = _save.kiosk2Cash;
                _save.money += got;
                _save.lifetime += got;
                _save.kiosk2Cash = 0;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                Notify(L.T("Выручка второго ларька", "Second kiosk cash"), "+" + GameData.Money(got));
                CheckAchievements();
                ShowKiosk2();
            }, 0.39f, 0.13f, 0.97f, 0.27f, 36);
            Icons.Coin(cash.transform, 0.1f, 0.5f, 60f);
            UIKit.FitLabel(cash.transform, _save.kiosk2Cash > 0 ? L.T("Забрать выручку +", "Collect cash +") + GameData.Money(_save.kiosk2Cash)
                                                               : L.T("Касса пуста — выручка появится после рабочего дня", "No cash yet — it appears after a work day"),
                34, Color.white, 0.18f, 0.1f, 0.97f, 0.9f);
            if (_save.kiosk2Cash > 0) cash.gameObject.AddComponent<Bobber>().amp = 3f;

            UIKit.Label(s, L.T("Каждый твой рабочий день второй ларёк продаёт порции со склада. Цена порции растёт вместе с твоей локацией.",
                "Every work day the second kiosk sells portions from its stock. The portion price grows with your location."),
                22, UIKit.Cream, TextAnchor.MiddleCenter, 0.03f, 0.13f, 0.36f, 0.28f, false);
            UIKit.Button(s, L.T("← Бизнесы", "← Businesses"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowBusinesses(); }, 0.02f, 0.01f, 0.22f, 0.1f, 34);
        }

        void BuyKiosk2(int portions)
        {
            int l = _save.biz[Meta.BizKiosk2];
            long cost = portions * Meta.Kiosk2UnitCost(l, _save.location);
            if (portions <= 0 || _save.money < cost) { Sfx.Play(Sound.Error); return; }
            _save.money -= cost;
            _save.kiosk2Stock += portions;
            SaveSystem.Save(_save);
            Sfx.Play(Sound.Coin);
            ShowKiosk2();
        }

        /// <summary>Картинка бизнеса: курьер на скутере, фермерский фургон с ящиками, телестудия с камерой.</summary>
        static void DrawBiz(Transform p, int i, bool owned, bool locked)
        {
            var col = UIKit.Hex(Meta.Businesses[i].color);
            if (locked) col = Color.Lerp(col, Color.gray, 0.6f);
            var art = UIKit.Rect(p, "Art");
            UIKit.PlaceFixed(art, 0.5f, 0.5f, 300f, 220f);
            switch (i)
            {
                case Meta.BizCourier:
                    UIKit.Panel(p, "Road", UIKit.Hex("#78909C"), 0f, 0f, 1f, 0.2f, false);
                    for (int k = 0; k < 5; k++) UIKit.Panel(p, "Mark", Color.white, 0.05f + k * 0.2f, 0.09f, 0.15f + k * 0.2f, 0.11f, false);
                    // скутер
                    UIKit.Shape(art, UIKit.Circle, UIKit.Dark, 0.28f, 0.2f, 60f, 60f);
                    UIKit.Shape(art, UIKit.Circle, UIKit.Dark, 0.72f, 0.2f, 60f, 60f);
                    UIKit.Shape(art, UIKit.Circle, UIKit.Gray, 0.28f, 0.2f, 26f, 26f);
                    UIKit.Shape(art, UIKit.Circle, UIKit.Gray, 0.72f, 0.2f, 26f, 26f);
                    UIKit.Shape(art, UIKit.Rounded, col, 0.5f, 0.33f, 190f, 40f);
                    UIKit.Shape(art, UIKit.RoundedSmall, col, 0.74f, 0.5f, 18f, 80f, -15f);
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Dark, 0.78f, 0.68f, 50f, 10f);
                    // курьер + короб
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#FFB300"), 0.28f, 0.6f, 90f, 80f);
                    UIKit.FitLabel(art, "Ш", 40, UIKit.Dark, 0.18f, 0.48f, 0.38f, 0.72f);
                    UIKit.Shape(art, UIKit.Rounded, col, 0.52f, 0.58f, 44f, 70f);
                    UIKit.Shape(art, UIKit.Circle, UIKit.Hex("#FFCC80"), 0.53f, 0.8f, 44f, 44f);
                    UIKit.Shape(art, UIKit.Circle, UIKit.Hex("#E53935"), 0.53f, 0.86f, 48f, 30f);
                    if (owned) art.gameObject.AddComponent<Swayer>();
                    break;
                case Meta.BizBakery:
                    UIKit.Panel(p, "Wall", UIKit.Hex("#FFE0B2"), 0f, 0.22f, 1f, 1f, false);
                    for (int k = 0; k < 6; k++) UIKit.Panel(p, "Brick", UIKit.Alpha(UIKit.Hex("#D7A77A"), 0.5f), 0f, 0.3f + k * 0.12f, 1f, 0.31f + k * 0.12f, false);
                    UIKit.Panel(p, "Floor", UIKit.Hex("#8D6E63"), 0f, 0f, 1f, 0.22f, false);
                    // тандыр — глиняный кувшин с огнём в горловине
                    UIKit.Shape(art, UIKit.CircleHD, new Color(0, 0, 0, 0.25f), 0.36f, 0.08f, 190f, 40f);
                    UIKit.Shape(art, UIKit.Rounded, UIKit.Hex("#A84A22"), 0.36f, 0.42f, 180f, 170f);
                    UIKit.Shape(art, UIKit.Rounded, UIKit.Hex("#C8683A"), 0.34f, 0.44f, 150f, 150f);
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#8E3B1A"), 0.36f, 0.3f, 176f, 10f);
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#8E3B1A"), 0.36f, 0.56f, 176f, 10f);
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Alpha(Color.white, 0.2f), 0.22f, 0.45f, 16f, 110f);
                    // горловина с жаром
                    UIKit.Shape(art, UIKit.CircleHD, UIKit.Hex("#6D2A10"), 0.36f, 0.8f, 150f, 50f);
                    UIKit.Shape(art, UIKit.CircleHD, UIKit.Hex("#2B130A"), 0.36f, 0.8f, 124f, 36f);
                    var glowT = UIKit.Shape(art, UIKit.CircleHD, UIKit.Hex("#FF8F00"), 0.36f, 0.8f, 96f, 24f);
                    if (owned) glowT.gameObject.AddComponent<Blinker>().speed = 3f;
                    for (int k = 0; k < 3; k++)
                    {
                        var fl = UIKit.Shape(art, UIKit.Circle, UIKit.Hex(k == 1 ? "#FFD54F" : "#FF7043"), 0.3f + k * 0.06f, 0.9f, 20f, 30f);
                        if (owned) { var fb = fl.gameObject.AddComponent<Bobber>(); fb.amp = 4f; fb.speed = 5f + k * 2f; }
                    }
                    // доска с горячим лавашом
                    UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#6D4C41"), 0.8f, 0.16f, 120f, 16f);
                    for (int k = 0; k < 3; k++) Icons.Lavash(art, 0.8f, 0.24f + k * 0.06f, 96f);
                    if (owned)
                        for (int k = 0; k < 3; k++)
                        {
                            var steam = UIKit.Shape(art, UIKit.Circle, UIKit.Alpha(Color.white, 0.5f), 0.74f + k * 0.06f, 0.5f + k * 0.04f, 16f, 24f);
                            var sb = steam.gameObject.AddComponent<Bobber>();
                            sb.amp = 6f;
                            sb.speed = 2f + k;
                        }
                    break;
                default:
                    // второй ларёк: маленькая будка с навесом, наёмный повар и ящики с продуктами
                    UIKit.Panel(p, "Street", UIKit.Hex("#BCAAA4"), 0f, 0f, 1f, 0.2f, false);
                    var kb = UIKit.Rect(p, "Kiosk2");
                    UIKit.PlaceFixed(kb, 0.42f, 0.5f, 220f, 190f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, UIKit.Hex("#E9D8B8"), 0.5f, 0.38f, 200f, 130f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, UIKit.Hex("#3B2A1E"), 0.5f, 0.46f, 130f, 70f);
                    for (int k = 0; k < 6; k++)
                        UIKit.Shape(kb, UIKit.RoundedSmall, k % 2 == 0 ? col : Color.white, 0.12f + k * 0.152f, 0.76f, 36f, 26f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, UIKit.Dark, 0.5f, 0.9f, 170f, 30f);
                    UIKit.FitLabel(kb, L.T("ЛАРЁК №2", "KIOSK #2"), 20, UIKit.Gold, 0.12f, 0.82f, 0.88f, 0.98f);
                    // повар в окне
                    UIKit.Shape(kb, UIKit.Circle, UIKit.Hex("#F6D2B0"), 0.45f, 0.52f, 40f, 40f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, Color.white, 0.45f, 0.66f, 36f, 16f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, Color.white, 0.45f, 0.36f, 56f, 30f);
                    UIKit.Shape(kb, UIKit.RoundedSmall, UIKit.Wood, 0.5f, 0.24f, 210f, 14f);
                    // ящики с продуктами
                    for (int k = 0; k < 2; k++)
                    {
                        var cr = UIKit.Shape(p, UIKit.RoundedSmall, UIKit.Hex("#A1887F"), 0.82f, 0.22f + k * 0.16f, 70f, 40f);
                        Icons.Ingredient(cr.transform, k == 0 ? Ing.Tomato : Ing.Meat, 0.5f, 0.8f, 30f);
                    }
                    if (owned) kb.gameObject.AddComponent<Bobber>().amp = 2f;
                    break;
            }
            if (locked)
            {
                UIKit.Panel(p, "Dim", new Color(0, 0, 0, 0.35f), 0, 0, 1, 1, false);
                UIKit.Shape(p, UIKit.Circle, UIKit.Gold, 0.5f, 0.58f, 56f, 56f);
                UIKit.Shape(p, UIKit.Circle, UIKit.Dark, 0.5f, 0.58f, 34f, 34f);
                UIKit.Shape(p, UIKit.RoundedSmall, UIKit.Gold, 0.5f, 0.42f, 76f, 58f);
                UIKit.Shape(p, UIKit.Circle, UIKit.Dark, 0.5f, 0.44f, 16f, 16f);
            }
        }

        // =====================================================================
        //  КОЛЕСО УДАЧИ
        // =====================================================================
        void ShowWheel()
        {
            var s = NewScreen("Wheel");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Dark, 0.02f, 0.84f, 0.98f, 0.92f);
            UIKit.Label(top.transform, L.T("Колесо удачи", "Lucky wheel"), 40, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.4f, 1, true);
            UIKit.Label(top.transform, L.T("Раз в день — бесплатно, дальше — за рекламу", "Free once a day, then for an ad"),
                26, UIKit.Alpha(UIKit.Cream, 0.8f), TextAnchor.MiddleRight, 0.4f, 0, 0.98f, 1, true);

            // колесо
            float size = 620f, r = size / 2f;
            var holder = UIKit.Rect(s, "WheelHolder");
            UIKit.PlaceFixed(holder, 0.38f, 0.45f, size + 40f, size + 40f);
            // ровное концентрическое кольцо: тень, тёмный обод, золотой обод, лампочки
            UIKit.Shape(holder, UIKit.CircleHD, new Color(0, 0, 0, 0.35f), 0.5f, 0.47f, size + 50f, size + 50f);
            UIKit.Shape(holder, UIKit.CircleHD, UIKit.Hex("#3E2723"), 0.5f, 0.5f, size + 48f, size + 48f);
            UIKit.Shape(holder, UIKit.CircleHD, UIKit.Gold, 0.5f, 0.5f, size + 26f, size + 26f);
            UIKit.Shape(holder, UIKit.CircleHD, UIKit.Hex("#C98A00"), 0.5f, 0.5f, size + 6f, size + 6f);
            for (int k = 0; k < 20; k++)
            {
                float ang = k * Mathf.PI * 2f / 20f;
                float rr = (size + 37f) / 2f / (size + 40f);
                var bulb = UIKit.Shape(holder, UIKit.Circle, UIKit.Hex("#FFF8E1"), 0.5f + Mathf.Cos(ang) * rr, 0.5f + Mathf.Sin(ang) * rr, 14f, 14f);
                var bl = bulb.gameObject.AddComponent<Blinker>();
                bl.speed = k % 2 == 0 ? 2f : 2.6f;
            }

            var wheelImg = UIKit.Shape(holder, UIKit.CircleHD, Color.white, 0.5f, 0.5f, size, size);
            var wheel = wheelImg.rectTransform;

            int n = Meta.WheelType.Length;
            float seg = 360f / n;
            for (int i = 0; i < n; i++)
            {
                float center = i * seg;
                // настоящий сектор: круг с радиальной заливкой 1/n
                var sector = UIKit.Rect(wheel, "Sector");
                UIKit.Place(sector, 0, 0, 1, 1);
                var simg = sector.gameObject.AddComponent<Image>();
                simg.sprite = UIKit.CircleHD;
                simg.type = Image.Type.Filled;
                simg.fillMethod = Image.FillMethod.Radial360;
                simg.fillOrigin = (int)Image.Origin360.Right;
                simg.fillClockwise = false;
                simg.fillAmount = 1f / n;
                simg.color = UIKit.Hex(Meta.WheelColor[i]);
                simg.raycastTarget = false;
                // заливка идёт от 0° против часовой на seg градусов — центрируем на center
                sector.localRotation = Quaternion.Euler(0, 0, center - seg / 2f);

                // подпись
                var arm = UIKit.Rect(wheel, "Arm");
                arm.anchorMin = arm.anchorMax = new Vector2(0.5f, 0.5f);
                arm.pivot = new Vector2(0f, 0.5f);
                arm.sizeDelta = new Vector2(r, 80f);
                arm.anchoredPosition = Vector2.zero;
                arm.localRotation = Quaternion.Euler(0, 0, center);
                int t = Meta.WheelType[i];
                if (t == 0)
                {
                    var lbl = UIKit.Label(arm, GameData.Money(Meta.WheelCoins(_save, i)), 44, Color.white, TextAnchor.MiddleCenter, 0.45f, 0f, 0.95f, 1f, true);
                    UIKit.AddOutline(lbl, UIKit.Alpha(Color.black, 0.5f), 2f);
                }
                else
                {
                    var ic = UIKit.Rect(arm, "Ic");
                    UIKit.PlaceFixed(ic, 0.7f, 0.5f, 70f, 70f);
                    ic.localRotation = Quaternion.Euler(0, 0, -90f);
                    if (t == 1) Icons.Cup(ic, 0.5f, 0.5f, 70f);
                    else if (t == 2) Icons.Flyer(ic, 0.5f, 0.5f, 70f);
                    else Icons.GoldLavash(ic, 0.5f, 0.5f, 66f);
                }
            }
            for (int i = 0; i < n; i++)
            {
                var line = UIKit.Rect(wheel, "Line");
                line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
                line.pivot = new Vector2(0f, 0.5f);
                line.sizeDelta = new Vector2(r, 4f);
                line.localRotation = Quaternion.Euler(0, 0, i * seg + seg / 2f);
                var img = line.gameObject.AddComponent<Image>();
                img.color = UIKit.Alpha(Color.white, 0.8f);
                img.raycastTarget = false;
            }
            var hub = UIKit.Shape(holder, UIKit.Circle, UIKit.Gold, 0.5f, 0.5f, 90f, 90f);
            UIKit.Shape(hub.transform, UIKit.Circle, UIKit.Hex("#C98A00"), 0.5f, 0.5f, 50f, 50f);
            // стрелка сверху
            var ptr = UIKit.Shape(holder, UIKit.RoundedSmall, UIKit.Red, 0.5f, 0.99f, 56f, 56f, 45f);
            UIKit.AddShadow(ptr, 4f, 0.4f);

            var spin = wheel.gameObject.AddComponent<WheelSpin>();

            // правая панель
            var panel = UIKit.Panel(s, "Side", UIKit.Alpha(UIKit.Cream, 0.96f), 0.64f, 0.18f, 0.97f, 0.78f);
            UIKit.AddShadow(panel, 10f, 0.4f);
            UIKit.Label(panel.transform, L.T("Призы: монеты до 2500 и бустеры на день", "Prizes: up to 2500 coins and day boosters"),
                28, UIKit.Dark, TextAnchor.MiddleCenter, 0.05f, 0.72f, 0.95f, 0.95f, true);
            var result = UIKit.Label(panel.transform, "", 40, UIKit.Green, TextAnchor.MiddleCenter, 0.05f, 0.42f, 0.95f, 0.7f, true);

            bool free = Meta.WheelFree(_save);
            Button spinBtn = null;
            spinBtn = UIKit.Button(panel.transform, free ? L.T("Крутить бесплатно!", "Spin for free!") : L.T("Крутить за рекламу", "Spin for an ad"),
                free ? UIKit.Green : UIKit.Orange, () =>
                {
                    if (spin.Spinning) return;
                    Action go = () =>
                    {
                        spinBtn.interactable = false;
                        int target = UnityEngine.Random.Range(0, n);
                        // джекпот реже
                        if (target == 7 && UnityEngine.Random.value < 0.6f) target = 1;
                        spin.Spin(target, seg, () =>
                        {
                            result.text = GiveWheelPrize(target);
                            Juice.Punch(result.transform, 0.3f, 0.4f);
                            Sfx.Play(Sound.Fanfare);
                            SaveSystem.Save(_save);
                            spinBtn.interactable = true;
                            UIKit.SetLabel(spinBtn, L.T("Ещё раз за рекламу", "Again for an ad"));
                            ((Image)spinBtn.targetGraphic).color = UIKit.Orange;
                        });
                    };
                    if (Meta.WheelFree(_save))
                    {
                        _save.wheelDate = DateTime.Now.ToString("yyyy-MM-dd");
                        SaveSystem.Save(_save);
                        go();
                    }
                    else Platform.ShowRewarded("wheel", go, null);
                }, 0.08f, 0.08f, 0.92f, 0.34f, 40);
            spinBtn.gameObject.AddComponent<Bobber>().amp = 4f;

            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.02f, 0.2f, 0.12f, 38);
        }

        string GiveWheelPrize(int i)
        {
            int t = Meta.WheelType[i];
            if (t == 0)
            {
                long wc = Meta.WheelCoins(_save, i);
                _save.money += wc;
                _save.lifetime += wc;
                Sfx.Play(Sound.Coin);
                return "+" + GameData.Money(wc) + L.T(" монет!", " coins!");
            }
            int b = t - 1;
            _save.boosts[b]++;
            return L.T("Бустер: ", "Booster: ") + Meta.Boosts[b].Name + "!";
        }
    }

    /// <summary>Вращение колеса с замедлением и щелчками.</summary>
    public class WheelSpin : MonoBehaviour
    {
        float _from, _to, _t, _dur;
        float _lastTickAngle;
        Action _done;
        public bool Spinning { get; private set; }

        public void Spin(int target, float seg, Action done)
        {
            float cur = transform.localEulerAngles.z;
            // сегмент target должен встать под стрелку (90° — верх)
            float want = 90f - target * seg;
            float delta = Mathf.Repeat(want - cur, 360f);
            _from = cur;
            _to = cur + 360f * 5f + delta;
            _t = 0f;
            _dur = 4.2f;
            _done = done;
            _lastTickAngle = cur;
            Spinning = true;
        }

        void Update()
        {
            if (!Spinning) return;
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / _dur);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            float a = Mathf.Lerp(_from, _to, e);
            transform.localRotation = Quaternion.Euler(0, 0, a);
            if (a - _lastTickAngle >= 45f)
            {
                _lastTickAngle = a;
                Sfx.Play(Sound.Tick);
            }
            if (k >= 1f)
            {
                Spinning = false;
                if (_done != null) _done();
            }
        }
    }
}
