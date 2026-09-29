using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Шаурма-Пасс (30 уровней наград за опыт) и скины ларька.</summary>
    public partial class Game
    {
        /// <summary>Талисман: амулет-кружок с символом.</summary>
        static void CharmIcon(Transform p, int i, float ax, float ay, float size)
        {
            var b = UIKit.Rect(p, "Charm" + i);
            UIKit.PlaceFixed(b, ax, ay, size, size);
            UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#8D6E63"), 0.5f, 0.95f, size * 0.08f, size * 0.25f);
            UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#5D4037"), 0.5f, 0.45f, size * 0.9f, size * 0.9f);
            UIKit.Shape(b, UIKit.Circle, UIKit.Hex(Meta.Charms[i].color), 0.5f, 0.45f, size * 0.76f, size * 0.76f);
            switch (i)
            {
                case Meta.CharmCoin: Icons.Coin(b, 0.5f, 0.45f, size * 0.5f); break;
                case Meta.CharmBell:
                    UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#FFF8E1"), 0.5f, 0.5f, size * 0.4f, size * 0.36f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FFF8E1"), 0.5f, 0.34f, size * 0.5f, size * 0.08f);
                    break;
                case Meta.CharmBowl:
                    UIKit.Shape(b, UIKit.Circle, Color.white, 0.5f, 0.38f, size * 0.5f, size * 0.22f);
                    UIKit.Shape(b, UIKit.Circle, UIKit.Hex("#8D6E63"), 0.5f, 0.44f, size * 0.36f, size * 0.1f);
                    break;
                case Meta.CharmSpatula:
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FFF8E1"), 0.5f, 0.58f, size * 0.3f, size * 0.22f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#6D4C41"), 0.5f, 0.3f, size * 0.07f, size * 0.3f);
                    break;
                case Meta.CharmMagnet:
                    UIKit.Shape(b, UIKit.RoundedSmall, Color.white, 0.36f, 0.45f, size * 0.1f, size * 0.36f);
                    UIKit.Shape(b, UIKit.RoundedSmall, Color.white, 0.64f, 0.45f, size * 0.1f, size * 0.36f);
                    UIKit.Shape(b, UIKit.RoundedSmall, Color.white, 0.5f, 0.3f, size * 0.38f, size * 0.1f);
                    break;
                default: Crown(b, 0.5f, 0.45f, size * 0.55f); break;
            }
        }

        // =====================================================================
        //  ШАУРМА-ПАСС
        // =====================================================================
        void ShowPass()
        {
            var s = NewScreen("Pass");
            Backdrop(s, true);
            int lvl = Meta.PassLevel(_save.passXp);

            var top = UIKit.Panel(s, "Top", UIKit.Hex("#4A148C"), 0.02f, 0.82f, 0.98f, 0.92f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.Label(top.transform, L.T("ШАУРМА-ПАСС", "SHAWARMA PASS"), 44, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.3f, 1, true);
            UIKit.Label(top.transform, L.T("Уровень ", "Level ") + lvl + " / " + Meta.PassLevels, 34, Color.white, TextAnchor.MiddleLeft, 0.3f, 0, 0.47f, 1, true);
            var bar = UIKit.Bar(top.transform, UIKit.Alpha(Color.white, 0.2f), UIKit.Gold, 0.48f, 0.3f, 0.78f, 0.7f);
            UIKit.SetFill(bar, Meta.PassProgress(_save.passXp));
            UIKit.Label(top.transform, L.T("Опыт — за каждого довольного клиента", "XP for every happy customer"), 24, Color.white,
                TextAnchor.MiddleCenter, 0.79f, 0.05f, 0.99f, 0.95f, true);

            for (int i = 1; i <= Meta.PassLevels; i++)
            {
                int col = (i - 1) % 10, row = (i - 1) / 10;
                float x0 = 0.02f + col * 0.0965f, y1 = 0.79f - row * 0.225f;
                bool claimed = i <= _save.passClaimed;
                bool ready = !claimed && i <= lvl && i == _save.passClaimed + 1;
                bool reached = i <= lvl;
                var card = UIKit.Panel(s, "Lvl", claimed ? UIKit.Hex("#E8F5E9") : (reached ? UIKit.Hex("#FFF3C4") : UIKit.Hex("#E0E0E0")), x0, y1 - 0.21f, x0 + 0.09f, y1);
                UIKit.AddShadow(card, 4f, 0.3f);
                if (ready) UIKit.AddOutline(card, UIKit.Gold, 4f);
                var num = UIKit.Panel(card.transform, "Num", reached ? UIKit.Purple : UIKit.Gray, 0.05f, 0.8f, 0.95f, 0.97f);
                UIKit.FitLabel(num.transform, i.ToString(), 24, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);

                int t, v;
                Meta.PassReward(i, out t, out v);
                if (t == 0) Icons.Coin(card.transform, 0.5f, 0.52f, 60f);
                else if (t == 1)
                {
                    if (v == 0) Icons.Cup(card.transform, 0.5f, 0.52f, 64f);
                    else if (v == 1) Icons.Flyer(card.transform, 0.5f, 0.52f, 64f);
                    else Icons.GoldLavash(card.transform, 0.5f, 0.52f, 58f);
                }
                else if (t == 2) CharmIcon(card.transform, v, 0.5f, 0.52f, 66f);
                else if (t == 4) Icons.Chest(card.transform, 0.5f, 0.52f, 64f, false);
                else Crown(card.transform, 0.5f, 0.52f, 60f);
                UIKit.FitLabel(card.transform, Meta.PassRewardText(i), 18, UIKit.Dark, 0.03f, 0.03f, 0.97f, 0.24f);

                if (claimed)
                {
                    var ok = UIKit.Shape(card.transform, UIKit.Circle, UIKit.Green, 0.82f, 0.68f, 32f, 32f);
                    UIKit.FitLabel(ok.transform, "OK", 14, Color.white, 0f, 0f, 1f, 1f);
                }
                else if (ready)
                {
                    card.gameObject.AddComponent<Bobber>().amp = 5f;
                    int lv = i;
                    UIKit.MakeButton(card, () =>
                    {
                        Meta.GivePassReward(_save, lv);
                        _save.passClaimed = lv;
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Fanfare);
                        Notify(L.T("Награда пасса", "Pass reward"), Meta.PassRewardText(lv));
                        ShowPass();
                    });
                }
                else if (!reached)
                    UIKit.Panel(card.transform, "Lock", UIKit.Alpha(Color.black, 0.15f), 0, 0, 1, 1);
            }
            // полученные талисманы
            var ch = UIKit.Panel(s, "Charms", UIKit.Alpha(UIKit.Dark, 0.85f), 0.22f, 0.015f, 0.98f, 0.115f);
            UIKit.Label(ch.transform, L.T("Талисманы:", "Charms:"), 24, UIKit.Gold, TextAnchor.MiddleLeft, 0.01f, 0f, 0.12f, 1f, true);
            for (int i = 0; i < Meta.Charms.Length; i++)
            {
                bool has = Meta.Has(_save, i);
                float x0 = 0.12f + i * 0.147f;
                var cg = UIKit.Rect(ch.transform, "Charm");
                UIKit.Place(cg, x0, 0.05f, x0 + 0.14f, 0.95f);
                CharmIcon(cg, i, 0.12f, 0.5f, 44f);
                if (!has) UIKit.Panel(cg, "Dim", UIKit.Alpha(Color.black, 0.5f), 0f, 0f, 0.25f, 1f);
                UIKit.Label(cg, Meta.Charms[i].Desc, 16, has ? Color.white : UIKit.Alpha(Color.white, 0.4f), TextAnchor.MiddleLeft, 0.25f, 0f, 1f, 1f);
            }
            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.02f, 0.2f, 0.11f, 36);
        }

    }
}
