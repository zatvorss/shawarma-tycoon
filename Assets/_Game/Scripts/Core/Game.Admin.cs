using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// ВРЕМЕННАЯ админ-панель для тестов: выдать деньги, славу, опыт пасса, звёзды.
    /// Перед публикацией в Яндекс Играх поставь AdminPanel = false.
    /// </summary>
    public partial class Game
    {
        public const bool AdminPanel = true;

        void AdminButton(RectTransform s)
        {
            if (!AdminPanel) return;
            var b = UIKit.Button(s, L.T("АДМИН", "ADMIN"), UIKit.Hex("#B71C1C"), () => { Sfx.Play(Sound.Click); ShowAdmin(s); }, 0.005f, 0.93f, 0.085f, 0.99f, 24);
            UIKit.AddOutline(b.GetComponent<Image>(), UIKit.Gold, 2f);
        }

        void ShowAdmin(RectTransform s)
        {
            var c = UIKit.Popup(s, L.T("Админ-панель (для тестов)", "Admin panel (testing)"), UIKit.Hex("#B71C1C"), 0.25f, 0.15f, 0.75f, 0.85f, () => ShowMenu());
            int[] money = { 1000, 10000, 100000, 1000000, 10000000 };
            for (int i = 0; i < money.Length; i++)
            {
                int v = money[i];
                float y1 = 0.98f - i * 0.12f;
                UIKit.Button(c, "+" + GameData.Money(v) + L.T(" монет", " coins"), UIKit.Green, () => Grant(() => { _save.money += v; _save.lifetime += v; }, "+" + GameData.Money(v)),
                    0.02f, y1 - 0.105f, 0.49f, y1, 32);
            }
            UIKit.Button(c, L.T("+5 очков славы", "+5 fame"), UIKit.Purple, () => Grant(() => _save.fame += 5, "+5"), 0.51f, 0.875f, 0.98f, 0.98f, 30);
            UIKit.Button(c, L.T("+1000 опыта пасса", "+1000 pass XP"), UIKit.Hex("#6A1B9A"), () => Grant(() => _save.passXp += 1000, "+1000 XP"), 0.51f, 0.755f, 0.98f, 0.86f, 30);
            UIKit.Button(c, L.T("+10 звёзд", "+10 stars"), UIKit.Orange, () => Grant(() => _save.stars += 10, "+10"), 0.51f, 0.635f, 0.98f, 0.74f, 30);
            UIKit.Button(c, L.T("День +1", "Day +1"), UIKit.Blue, () => Grant(() => _save.day += 1, L.T("День ", "Day ") + (_save.day + 1)), 0.51f, 0.515f, 0.98f, 0.62f, 30);
            UIKit.Button(c, L.T("По 1 бустеру", "1 of each booster"), UIKit.Steel, () => Grant(() => { for (int i = 0; i < _save.boosts.Length; i++) _save.boosts[i]++; }, "+1"), 0.51f, 0.395f, 0.98f, 0.5f, 30);
            // смена локации
            UIKit.Button(c, L.T("← Локация", "← Location"), UIKit.Hex("#00838F"), () => AdminLocation(-1), 0.51f, 0.275f, 0.735f, 0.38f, 26);
            UIKit.Button(c, L.T("Локация →", "Location →"), UIKit.Hex("#00838F"), () => AdminLocation(1), 0.755f, 0.275f, 0.98f, 0.38f, 26);
            UIKit.FitLabel(c, L.T("Сейчас: ", "Now: ") + (_save.location + 1) + ". " + _save.Loc.Name, 24, UIKit.Dark, 0.51f, 0.15f, 0.98f, 0.26f);
            UIKit.Label(c, L.T("Выключить: Game.AdminPanel = false в Game.Admin.cs", "Disable: Game.AdminPanel = false in Game.Admin.cs"), 20, UIKit.Gray,
                TextAnchor.MiddleCenter, 0f, 0.0f, 1f, 0.12f, true);
        }

        /// <summary>Перейти на соседнюю локацию (для тестов): улучшения сбрасываются, как при настоящем переезде.</summary>
        void AdminLocation(int dir)
        {
            int to = Mathf.Clamp(_save.location + dir, 0, GameData.Locations.Length - 1);
            if (to == _save.location) { Sfx.Play(Sound.Error); return; }
            _save.location = to;
            _save.rebirths = to;
            for (int i = 0; i <= to && i < _save.locUnlocked.Length; i++) _save.locUnlocked[i] = 1;
            for (int i = 0; i < _save.upgrades.Length; i++) _save.upgrades[i] = 0;
            _save.day = 1;
            SaveSystem.Save(_save);
            Sfx.Play(Sound.Fanfare);
            Notify(L.T("Админ", "Admin"), L.T("Локация: ", "Location: ") + (to + 1) + ". " + _save.Loc.Name);
            ShowMenu();
            ShowAdmin((RectTransform)_screen.transform);
        }

        void Grant(System.Action act, string msg)
        {
            act();
            SaveSystem.Save(_save);
            CheckAchievements();
            Sfx.Play(Sound.Coin);
            Notify(L.T("Админ", "Admin"), msg);
        }
    }
}
