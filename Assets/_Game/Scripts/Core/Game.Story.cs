using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Сюжетные диалоги, переезды (ребёрты) и магазин Славы.</summary>
    public partial class Game
    {
        // =====================================================================
        //  СЮЖЕТ
        // =====================================================================
        /// <summary>Показывает нужную главу сюжета, если её ещё не видели. true — показали.</summary>
        bool TryStory()
        {
            int ch = -1;
            if (_save.story[0] == 0) ch = 0;
            else if (_save.location == 0 && _save.day >= 3 && _save.story[1] == 0) ch = 1;
            else if (_save.location >= 1 && _save.story[1 + _save.location] == 0) ch = 1 + _save.location;
            else if (_save.lifetime >= 1000000000L && _save.story[11] == 0) ch = 11;
            if (ch < 0) return false;
            _save.story[ch] = 1;
            SaveSystem.Save(_save);
            ShowDialog(Story.Chapter(ch), 0, null);
            return true;
        }

        void ShowDialog(StoryLine[] lines, int i, Action done)
        {
            if (i >= lines.Length) { if (done != null) done(); return; }
            var line = lines[i];
            var ov = UIKit.Panel(_screen.transform, "Dialog", new Color(0, 0, 0, 0.55f), 0, 0, 1, 1, false);
            ov.raycastTarget = true;
            var go = ov.gameObject;

            var box = UIKit.Panel(ov.transform, "Box", UIKit.Cream, 0.05f, 0.04f, 0.95f, 0.36f);
            UIKit.AddShadow(box, 14f, 0.5f);
            UIKit.AddOutline(box, UIKit.Dark, 4f);

            var face = UIKit.Panel(ov.transform, "Face", PortraitBg(line.who), 0.06f, 0.2f, 0.24f, 0.6f);
            UIKit.AddShadow(face, 10f, 0.4f);
            UIKit.AddOutline(face, UIKit.Dark, 4f);
            face.gameObject.AddComponent<RectMask2D>();
            DrawPortrait(face.transform, line.who);
            Juice.Punch(face.rectTransform, 0.08f, 0.3f);

            var nameTag = UIKit.Panel(box.transform, "Name", PortraitBg(line.who), 0.22f, 0.8f, 0.5f, 1.08f);
            UIKit.FitLabel(nameTag.transform, Story.Who(line.who), 34, Color.white, 0.05f, 0.1f, 0.95f, 0.9f);
            var txt = UIKit.Label(box.transform, line.Text, 38, UIKit.Dark, TextAnchor.UpperLeft, 0.22f, 0.12f, 0.97f, 0.78f);
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 20;
            txt.resizeTextMaxSize = 40;
            txt.verticalOverflow = VerticalWrapMode.Truncate;

            var next = UIKit.Label(box.transform, (i + 1 < lines.Length ? L.T("Далее »", "Next »") : L.T("Поехали! »", "Let's go! »")), 30, UIKit.Wood,
                TextAnchor.MiddleRight, 0.6f, 0.02f, 0.97f, 0.16f, true);
            next.gameObject.AddComponent<Blinker>().speed = 2f;

            UIKit.HitArea(ov.transform, "Next", () =>
            {
                Sfx.Play(Sound.Click);
                Destroy(go);
                ShowDialog(lines, i + 1, done);
            }).transform.SetAsLastSibling();
            Sfx.Play(Sound.Tick);
        }

        static Color PortraitBg(int who)
        {
            switch (who)
            {
                case Story.Ashot: return UIKit.Hex("#8D5524");
                case Story.King: return UIKit.Hex("#6A1B9A");
                case Story.Reporter: return UIKit.Hex("#00838F");
                default: return UIKit.Hex("#2E7D32");
            }
        }

        static void DrawPortrait(Transform p, int who)
        {
            var R = UIKit.Rounded;
            var C = UIKit.Circle;
            var skin = UIKit.Hex(who == Story.King ? "#F1C27D" : (who == Story.Reporter ? "#F6D2B0" : "#E0AC69"));
            // плечи
            string shirt = who == Story.Ashot ? "#FFFFFF" : (who == Story.King ? "#FFC928" : (who == Story.Reporter ? "#37474F" : "#FFFFFF"));
            UIKit.Shape(p, R, UIKit.Hex(shirt), 0.5f, 0.05f, 300f, 200f);
            var head = UIKit.Shape(p, C, skin, 0.5f, 0.5f, 190f, 200f);
            var h = head.transform;
            UIKit.Shape(h, C, UIKit.Dark, 0.35f, 0.55f, 18f, 22f);
            UIKit.Shape(h, C, UIKit.Dark, 0.65f, 0.55f, 18f, 22f);
            UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#8C3B2E"), 0.5f, 0.27f, 50f, 12f);
            switch (who)
            {
                case Story.Ashot:
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#9E9E9E"), 0.5f, 0.37f, 100f, 22f);   // усы
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#9E9E9E"), 0.35f, 0.68f, 40f, 8f, -10f);
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#9E9E9E"), 0.65f, 0.68f, 40f, 8f, 10f);
                    // поварской колпак — сидит на голове: пышный верх + плотный околыш
                    UIKit.Shape(h, C, Color.white, 0.3f, 1.12f, 84f, 84f);
                    UIKit.Shape(h, C, Color.white, 0.7f, 1.12f, 84f, 84f);
                    UIKit.Shape(h, C, Color.white, 0.5f, 1.22f, 100f, 96f);
                    UIKit.Shape(h, C, UIKit.Hex("#ECEFF1"), 0.5f, 1.1f, 60f, 40f);
                    UIKit.Shape(h, UIKit.RoundedSmall, Color.white, 0.5f, 0.92f, 176f, 48f);
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#E0E0E0"), 0.5f, 0.84f, 176f, 6f);
                    break;
                case Story.King:
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#111111"), 0.5f, 0.56f, 150f, 36f);   // очки
                    for (int i = 0; i < 3; i++) UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Gold, 0.3f + i * 0.2f, 1.1f, 34f, 34f, 45f);  // корона
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Gold, 0.5f, 0.99f, 156f, 46f);
                    UIKit.Shape(h, C, UIKit.Red, 0.5f, 0.99f, 22f, 22f);
                    break;
                case Story.Reporter:
                    UIKit.Shape(p, R, UIKit.Hex("#5D4037"), 0.5f, 0.72f, 210f, 110f);                    // волосы
                    UIKit.Shape(p, UIKit.RoundedSmall, UIKit.Hex("#212121"), 0.8f, 0.2f, 20f, 110f, -20f); // микрофон
                    UIKit.Shape(p, C, UIKit.Hex("#424242"), 0.84f, 0.33f, 50f, 50f);
                    break;
                default:
                    // кепка: купол плотно на голове + козырёк вперёд
                    UIKit.Shape(h, C, UIKit.Red, 0.5f, 0.97f, 196f, 130f);
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#B71C1C"), 0.5f, 0.7f, 192f, 22f);
                    UIKit.Shape(h, UIKit.Rounded, UIKit.Hex("#B71C1C"), 0.8f, 0.67f, 120f, 26f, -4f);
                    UIKit.Shape(h, C, UIKit.Hex("#B71C1C"), 0.5f, 1.3f, 22f, 16f);
                    UIKit.Shape(h, C, UIKit.Alpha(Color.white, 0.25f), 0.38f, 1.05f, 50f, 26f);
                    break;
            }
        }

        // =====================================================================
        //  ПЕРЕЕЗД (РЕБЁРТ)
        // =====================================================================
        bool CanRebirth(out string why)
        {
            int next = _save.location + 1;
            why = "";
            if (next >= GameData.Locations.Length) { why = L.T("Это последняя локация", "This is the last location"); return false; }
            var l = GameData.Locations[next];
            if (_save.day < l.minDay) { why = L.T("Нужен день ", "Need day ") + l.minDay + L.T(" (сейчас ", " (now ") + _save.day + ")"; return false; }
            if (_save.money < l.cost) { why = L.T("Нужно монет: ", "Need coins: ") + GameData.Money(l.cost); return false; }
            return true;
        }

        void DoRebirth()
        {
            string why;
            if (!CanRebirth(out why)) { Sfx.Play(Sound.Error); return; }
            int next = _save.location + 1;
            float oldMult = GameData.RebirthMult(_save.rebirths);
            int fame = GameData.FameFor(next);
            int keep = Mathf.RoundToInt(_save.money * 0.1f * Meta.P(_save, Perk.Legacy));

            _save.rebirths++;
            _save.fame += fame;
            _save.location = next;
            _save.day = 1;
            _save.money = keep + 1000 * Meta.P(_save, Perk.Capital);
            for (int i = 0; i < _save.upgrades.Length; i++) _save.upgrades[i] = 0;
            for (int i = 0; i < _save.boostOn.Length; i++) _save.boostOn[i] = 0;
            _save.rep = 3f;
            SaveSystem.Save(_save);
            CheckAchievements();
            SaveSystem.Save(_save);
            RebirthCelebration(oldMult, fame);
        }

        void RebirthCelebration(float oldMult, int fame)
        {
            var s = NewScreen("Rebirth");
            var bg = UIKit.Full(s, "Bg").gameObject.AddComponent<Image>();
            bg.sprite = UIKit.Gradient(UIKit.Hex("#4A148C"), UIKit.Hex("#120B07"));
            var rays = UIKit.Rect(s, "Rays");
            UIKit.PlaceFixed(rays, 0.5f, 0.55f, 10f, 10f);
            for (int i = 0; i < 16; i++)
                UIKit.Shape(rays, null, new Color(1f, 0.85f, 0.3f, 0.08f), 0.5f, 0.5f, 110f, 2600f, i * 11.25f);
            rays.gameObject.AddComponent<Spinner>().speed = 20f;

            var loc = _save.Loc;
            var t = UIKit.Label(s, L.T("ПЕРЕЕЗД!", "MOVING UP!"), 140, UIKit.Gold, TextAnchor.MiddleCenter, 0f, 0.72f, 1f, 0.92f, true);
            UIKit.AddOutline(t, UIKit.Hex("#8A3B12"), 5f);
            Juice.Punch(t.transform, 0.4f, 0.6f);
            UIKit.Label(s, L.T("Новая точка: ", "New spot: ") + loc.Name, 56, UIKit.Cream, TextAnchor.MiddleCenter, 0f, 0.6f, 1f, 0.7f, true);

            var box = UIKit.Panel(s, "Box", UIKit.Alpha(UIKit.Cream, 0.95f), 0.25f, 0.2f, 0.75f, 0.56f);
            UIKit.AddShadow(box, 12f, 0.5f);
            string lines =
                L.T("Множитель дохода навсегда:  ", "Permanent income multiplier:  ") + "×" + oldMult.ToString("0.00") + "  →  <color=#2E7D32>×" + GameData.RebirthMult(_save.rebirths).ToString("0.00") + "</color>\n" +
                "+" + Meta.FamePts(fame) + L.T("  (потрать на вечные улучшения)", "  (spend on permanent perks)") + "\n" +
                L.T("Цены на этой точке: ×", "Prices here: ×") + loc.price.ToString("0.0") + "\n" +
                "<color=#C62828>" + L.T("Сброшено: монеты, день, улучшения ларька", "Reset: coins, day, kiosk upgrades") + "</color>";
            UIKit.Label(box.transform, lines, 32, UIKit.Dark, TextAnchor.MiddleCenter, 0.04f, 0.05f, 0.96f, 0.95f);
            Juice.Burst(s, UIKit.Gold, 30, 0.5f, 0.8f, 22f);
            Sfx.Play(Sound.Fanfare);
            Sfx.Play(Sound.Whoosh);

            UIKit.Button(s, L.T("К новой точке!", "To the new spot!"), UIKit.Green, () => { Sfx.Play(Sound.Click); ShowMap(); }, 0.35f, 0.05f, 0.65f, 0.16f, 50);
        }

        // =====================================================================
        //  СЛАВА — вечные улучшения
        // =====================================================================
        void ShowFame()
        {
            var s = NewScreen("Fame");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Dark, 0.02f, 0.84f, 0.98f, 0.92f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("Очки славы", "Fame points"), 44, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.2f, 1, true);
            UIKit.Label(top.transform, L.T("Вечные улучшения — не сбрасываются при переезде", "Permanent perks — never reset when moving"),
                26, UIKit.Alpha(UIKit.Cream, 0.85f), TextAnchor.MiddleCenter, 0.2f, 0, 0.75f, 1, true);
            Crown(top.transform, 0.8f, 0.5f, 50f);
            UIKit.Label(top.transform, _save.fame.ToString(), 40, UIKit.Gold, TextAnchor.MiddleLeft, 0.83f, 0, 0.99f, 1, true);

            for (int i = 0; i < Meta.Perks.Length; i++)
            {
                var d = Meta.Perks[i];
                int lvl = _save.perks[i];
                int col = i % 3, row = i / 3;
                float x0 = 0.02f + col * 0.325f, y1 = 0.82f - row * 0.33f;
                var card = UIKit.Panel(s, "Perk", UIKit.Cream, x0, y1 - 0.31f, x0 + 0.315f, y1);
                UIKit.AddShadow(card, 6f, 0.35f);
                Crown(card.transform, 0.12f, 0.74f, 90f);
                UIKit.Label(card.transform, d.Name, 34, UIKit.Dark, TextAnchor.MiddleLeft, 0.25f, 0.76f, 0.99f, 0.96f, true);
                UIKit.Label(card.transform, d.Desc, 24, UIKit.Wood, TextAnchor.UpperLeft, 0.25f, 0.46f, 0.99f, 0.74f);
                UIKit.Label(card.transform, L.T("Уровень ", "Level ") + lvl + " / " + d.max, 26, UIKit.Purple, TextAnchor.MiddleLeft, 0.05f, 0.3f, 0.6f, 0.46f, true);
                if (lvl >= d.max)
                {
                    UIKit.Label(card.transform, L.T("Максимум!", "Maxed!"), 34, UIKit.Green, TextAnchor.MiddleCenter, 0.05f, 0.04f, 0.95f, 0.28f, true);
                    continue;
                }
                int cost = d.Cost(lvl);
                bool can = _save.fame >= cost;
                int idx = i;
                var b = UIKit.Button(card.transform, L.T("Улучшить за ", "Upgrade for ") + Meta.FamePts(cost), can ? UIKit.Purple : UIKit.Gray, () =>
                {
                    if (_save.fame < Meta.Perks[idx].Cost(_save.perks[idx])) { Sfx.Play(Sound.Error); return; }
                    _save.fame -= Meta.Perks[idx].Cost(_save.perks[idx]);
                    _save.perks[idx]++;
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Fanfare);
                    ShowFame();
                }, 0.05f, 0.04f, 0.95f, 0.28f, 32);
                b.interactable = can;
            }
            UIKit.Button(s, L.T("← На карту", "← To the map"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMap(); }, 0.02f, 0.02f, 0.25f, 0.13f, 40);
        }

        static void Crown(Transform p, float ax, float ay, float size)
        {
            var b = UIKit.Rect(p, "Crown");
            UIKit.PlaceFixed(b, ax, ay, size, size);
            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#C98A00"), 0.5f, 0.3f, size * 0.8f, size * 0.3f);
            for (int i = 0; i < 3; i++)
                UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.25f + i * 0.25f, 0.55f, size * 0.28f, size * 0.28f, 45f);
            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Gold, 0.5f, 0.35f, size * 0.8f, size * 0.3f);
            UIKit.Shape(b, UIKit.Circle, UIKit.Red, 0.5f, 0.35f, size * 0.16f, size * 0.16f);
        }
    }
}
