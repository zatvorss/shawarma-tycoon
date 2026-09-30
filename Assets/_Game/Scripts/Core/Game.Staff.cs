using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Сотрудники на прошлых локациях: нанимаешь повара (за яны) в свой старый ларёк —
    /// закупаешь ему продукты, каждый твой рабочий день он продаёт порции, ты забираешь выручку.
    /// Чем дальше была локация — тем больше он приносит и тем дороже найм.
    /// </summary>
    public partial class Game
    {
        void ShowStaff()
        {
            var s = NewScreen("Staff");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Hex("#0D47A1"), 0.02f, 0.84f, 0.98f, 0.93f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.AddOutline(top, UIKit.Gold, 2f);
            UIKit.Label(top.transform, L.T("Сотрудники", "Staff"), 42, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.22f, 1, true);
            UIKit.Label(top.transform, L.T("Найми повара в свой прошлый ларёк: закупай продукты — он продаёт каждый твой рабочий день.",
                "Hire a cook for your previous kiosks: buy food — they sell every work day of yours."),
                22, Color.white, TextAnchor.MiddleCenter, 0.22f, 0, 0.78f, 1, false);
            Icons.Coin(top.transform, 0.8f, 0.5f, 46f);
            UIKit.Label(top.transform, GameData.Money(_save.money), 36, UIKit.Gold, TextAnchor.MiddleLeft, 0.82f, 0, 0.99f, 1, true);

            // общие кнопки
            long allCash = 0; long allFood = 0; int hired = 0;
            for (int i = 0; i < _save.staffHired.Length; i++)
                if (Meta.StaffHired(_save, i))
                {
                    hired++;
                    allCash += _save.staffCash[i];
                    int room = Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - _save.staffStock[i]);
                    allFood += room * Meta.StaffUnitCost(_save, i);
                }
            if (hired > 0)
            {
                var cashAll = UIKit.Button(s, "", allCash > 0 ? UIKit.Green : UIKit.Gray, () =>
                {
                    long got = 0;
                    for (int i = 0; i < _save.staffCash.Length; i++) { got += _save.staffCash[i]; _save.staffCash[i] = 0; }
                    if (got <= 0) { Sfx.Play(Sound.Error); return; }
                    _save.money += got; _save.lifetime += got;
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Coin);
                    Notify(L.T("Выручка сотрудников", "Staff cash"), "+" + GameData.Money(got));
                    CheckAchievements();
                    ShowStaff();
                }, 0.5f, 0.01f, 0.73f, 0.1f, 28);
                UIKit.SetLabel(cashAll, allCash > 0 ? L.T("Забрать всё +", "Collect all +") + GameData.Money(allCash) : L.T("Касса пуста", "No cash yet"));
                if (allCash > 0) cashAll.gameObject.AddComponent<Bobber>().amp = 3f;
                var foodAll = UIKit.PriceButton(s, L.T("Всем на день", "Food for all"), allFood, UIKit.Blue, () =>
                {
                    long cost = 0;
                    for (int i = 0; i < _save.staffHired.Length; i++)
                        if (Meta.StaffHired(_save, i))
                            cost += Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - _save.staffStock[i]) * Meta.StaffUnitCost(_save, i);
                    if (cost <= 0 || _save.money < cost) { Sfx.Play(Sound.Error); return; }
                    _save.money -= cost;
                    for (int i = 0; i < _save.staffHired.Length; i++)
                        if (Meta.StaffHired(_save, i))
                            _save.staffStock[i] += Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - _save.staffStock[i]);
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Coin);
                    ShowStaff();
                }, 0.75f, 0.01f, 0.98f, 0.1f, 26);
                foodAll.interactable = allFood > 0 && _save.money >= allFood;
            }

            // карточки локаций (все, кроме Дубая — туда переезжают в последнюю очередь)
            int n = GameData.Locations.Length - 1;
            for (int i = 0; i < n; i++)
            {
                int col = i % 4, row = i / 4;
                float x0 = 0.02f + col * 0.24f, y1 = 0.82f - row * 0.235f;
                StaffCard(s, i, x0, y1 - 0.225f, x0 + 0.23f, y1);
            }
            UIKit.Button(s, L.T("← Бизнесы", "← Businesses"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowBusinesses(); }, 0.02f, 0.01f, 0.22f, 0.1f, 34);
        }

        void StaffCard(RectTransform s, int loc, float x0, float y0, float x1, float y1)
        {
            var L0 = GameData.Locations[loc];
            bool passed = loc < _save.location;
            bool hired = Meta.StaffHired(_save, loc);
            var card = UIKit.Panel(s, "Staff" + loc, passed ? UIKit.Cream : UIKit.Hex("#CFC8BE"), x0, y0, x1, y1);
            UIKit.AddShadow(card, 5f, 0.35f);
            if (hired) UIKit.AddOutline(card, UIKit.Hex("#1565C0"), 3f);
            var c = card.transform;
            // шапка в цвет ларька локации
            var head = UIKit.Panel(c, "Head", UIKit.Hex(L0.kiosk), 0f, 0.8f, 1f, 1f);
            UIKit.FitLabel(head.transform, (loc + 1) + ". " + L0.Name, 22, Color.white, 0.04f, 0.08f, 0.96f, 0.92f);

            // мини-ларёк с поваром
            var art = UIKit.Rect(c, "Art");
            UIKit.PlaceFixed(art, 0.17f, 0.5f, 90f, 90f);
            UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#E9D8B8"), 0.5f, 0.38f, 80f, 58f);
            UIKit.Shape(art, UIKit.RoundedSmall, UIKit.Hex("#3B2A1E"), 0.5f, 0.44f, 54f, 32f);
            for (int k = 0; k < 4; k++)
                UIKit.Shape(art, UIKit.RoundedSmall, k % 2 == 0 ? UIKit.Hex(L0.kiosk) : Color.white, 0.2f + k * 0.2f, 0.74f, 18f, 14f);
            if (hired)
            {
                UIKit.Shape(art, UIKit.Circle, UIKit.Hex("#F6D2B0"), 0.5f, 0.47f, 20f, 20f);
                UIKit.Shape(art, UIKit.RoundedSmall, Color.white, 0.5f, 0.58f, 20f, 8f);
            }

            if (!passed)
            {
                UIKit.FitLabel(c, L.T("Станет доступно,\nкогда переедешь дальше", "Available once\nyou move on"), 18, UIKit.Gray, 0.34f, 0.25f, 0.97f, 0.75f);
                return;
            }

            long price = Meta.StaffPrice(_save, loc), unit = Meta.StaffUnitCost(_save, loc);
            long perDay = Meta.StaffPortions * (price - unit);
            if (!hired)
            {
                UIKit.FitLabel(c, L.T("Прибыль ≈ ", "Profit ≈ ") + GameData.Money(perDay) + L.T(" в день", " a day"), 20, UIKit.Dark, 0.34f, 0.5f, 0.97f, 0.75f);
                UIKit.FitLabel(c, L.T("растёт с твоей локацией", "grows with your location"), 16, UIKit.Gray, 0.34f, 0.34f, 0.97f, 0.5f, false);
                int yan = Meta.StaffYan[Mathf.Clamp(loc, 0, Meta.StaffYan.Length - 1)];
                int li = loc;
                if (!Payments.Available("staff_" + li))
                {
                    UIKit.FitLabel(c, Payments.Ready ? L.T("Найм пока недоступен", "Hiring is not available yet") : L.T("Загрузка магазина...", "Loading the shop..."),
                        18, UIKit.Gray, 0.05f, 0.04f, 0.95f, 0.3f);
                    return;
                }
                var hb = UIKit.Button(c, L.T("Нанять", "Hire"), UIKit.Hex("#FF8F00"), () =>
                {
                    Payments.Buy("staff_" + li, yan, () =>
                    {
                        _save.staffHired[li] = 1;
                        _save.staffStock[li] = Meta.StaffPortions;   // первый день продуктов — в подарок
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Fanfare);
                        Notify(L.T("Нанят сотрудник", "Staff hired"), GameData.Locations[li].Name);
                        ShowStaff();
                    });
                }, 0.05f, 0.04f, 0.95f, 0.3f, 22);
                var hl = hb.GetComponentInChildren<Text>();
                if (hl != null) { var hr = hl.rectTransform; hr.anchorMin = new Vector2(0.04f, 0.08f); hr.anchorMax = new Vector2(0.5f, 0.92f); }
                Payments.PriceTag(hb.transform, "staff_" + li, yan, 0.5f, 0.08f, 0.97f, 0.92f, 22, UIKit.Hex("#FFF3C4"));
                return;
            }

            // нанят: склад, вчерашние продажи, закупка и касса
            int stock = _save.staffStock[loc];
            UIKit.Label(c, L.T("Склад ", "Stock ") + stock + "/" + Meta.StaffStorage, 18, UIKit.Dark, TextAnchor.MiddleLeft, 0.34f, 0.66f, 0.97f, 0.78f, true);
            var bar = UIKit.Bar(c, UIKit.Alpha(UIKit.Dark, 0.15f), stock > 0 ? UIKit.Green : UIKit.Red, 0.34f, 0.6f, 0.95f, 0.66f);
            UIKit.SetFill(bar, (float)stock / Meta.StaffStorage);
            UIKit.Label(c, stock == 0 ? L.T("Простаивает — закупи!", "Idle — buy food!") : L.T("Вчера продал: ", "Sold yesterday: ") + _save.staffSold[loc],
                16, stock == 0 ? UIKit.Red : UIKit.Gray, TextAnchor.MiddleLeft, 0.34f, 0.48f, 0.97f, 0.6f, false);

            int room = Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - stock);
            long cost = room * unit;
            int lc = loc;
            var fb = UIKit.PriceButton(c, L.T("Закупить", "Buy food"), cost, UIKit.Blue, () =>
            {
                int r = Mathf.Min(Meta.StaffPortions, Meta.StaffStorage - _save.staffStock[lc]);
                long cc = r * Meta.StaffUnitCost(_save, lc);
                if (r <= 0 || _save.money < cc) { Sfx.Play(Sound.Error); return; }
                _save.money -= cc;
                _save.staffStock[lc] += r;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                ShowStaff();
            }, 0.04f, 0.04f, 0.5f, 0.3f, 18);
            fb.interactable = room > 0 && _save.money >= cost;

            long cash = _save.staffCash[loc];
            var cb = UIKit.Button(c, cash > 0 ? "+" + GameData.Money(cash) : L.T("Касса 0", "Cash 0"), cash > 0 ? UIKit.Green : UIKit.Gray, () =>
            {
                long got = _save.staffCash[lc];
                if (got <= 0) { Sfx.Play(Sound.Error); return; }
                _save.money += got; _save.lifetime += got;
                _save.staffCash[lc] = 0;
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                CheckAchievements();
                ShowStaff();
            }, 0.52f, 0.04f, 0.96f, 0.3f, 20);
            if (cash > 0) cb.gameObject.AddComponent<Bobber>().amp = 2f;
            UIKit.FitLabel(c, L.T("≈ ", "≈ ") + GameData.Money(perDay) + L.T("/день", "/day"), 16, UIKit.Hex("#1565C0"), 0.34f, 0.34f, 0.97f, 0.47f);
        }
    }
}
