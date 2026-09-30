using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Шаурма-Пасс (50 уровней, бесплатная дорожка и Пасс+), талисманы и награды за время в игре.</summary>
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
                case Meta.CharmClover:
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (45f + k * 90f) * Mathf.Deg2Rad;
                        UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#1B5E20"), 0.5f + Mathf.Cos(a) * 0.12f, 0.47f + Mathf.Sin(a) * 0.12f, size * 0.26f, size * 0.26f);
                    }
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#1B5E20"), 0.56f, 0.28f, size * 0.05f, size * 0.2f, 25f);
                    break;
                case Meta.CharmHourglass:
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#5D4037"), 0.5f, 0.68f, size * 0.42f, size * 0.07f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#5D4037"), 0.5f, 0.22f, size * 0.42f, size * 0.07f);
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#E1F5FE"), 0.5f, 0.56f, size * 0.3f, size * 0.2f);
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#FFE082"), 0.5f, 0.34f, size * 0.3f, size * 0.2f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FFE082"), 0.5f, 0.45f, size * 0.05f, size * 0.12f);
                    break;
                case Meta.CharmPepper:
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#C62828"), 0.48f, 0.42f, size * 0.22f, size * 0.46f, -30f);
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Alpha(Color.white, 0.4f), 0.44f, 0.46f, size * 0.05f, size * 0.2f, -30f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#2E7D32"), 0.6f, 0.66f, size * 0.06f, size * 0.16f, -40f);
                    break;
                case Meta.CharmPiggy:
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#F8BBD0"), 0.5f, 0.44f, size * 0.5f, size * 0.4f);
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#F48FB1"), 0.72f, 0.44f, size * 0.14f, size * 0.12f);
                    UIKit.Shape(b, UIKit.CircleHD, UIKit.Hex("#F48FB1"), 0.38f, 0.64f, size * 0.12f, size * 0.12f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#AD1457"), 0.5f, 0.62f, size * 0.14f, size * 0.03f);
                    UIKit.Shape(b, UIKit.Circle, UIKit.Dark, 0.6f, 0.5f, size * 0.05f, size * 0.05f);
                    break;
                case Meta.CharmChefStar: Icons.Star(b, 0.5f, 0.46f, size * 0.55f, true); break;
                case Meta.CharmGoldKnife:
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#FFF8E1"), 0.56f, 0.52f, size * 0.14f, size * 0.42f, -35f);
                    UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex("#6D4C41"), 0.36f, 0.26f, size * 0.1f, size * 0.2f, -35f);
                    break;
                default: Crown(b, 0.5f, 0.45f, size * 0.55f); break;
            }
        }

        /// <summary>Коллекция талисманов: шесть карточек-«витрин».</summary>
        void ShowCharms(RectTransform s)
        {
            var ov = UIKit.Panel(s, "Charms", new Color(0, 0, 0, 0.65f), 0, 0, 1, 1, false);
            ov.raycastTarget = true;
            var overlay = ov.gameObject;
            UnityEngine.Events.UnityAction close = () => { Sfx.Play(Sound.Click); Destroy(overlay); };
            UIKit.HitArea(ov.transform, "Outside", close);

            var card = UIKit.Panel(ov.transform, "Card", UIKit.Hex("#2A1446"), 0.1f, 0.08f, 0.9f, 0.92f);
            card.raycastTarget = true;
            UIKit.AddShadow(card, 16f, 0.5f);
            UIKit.AddOutline(card, UIKit.Gold, 3f);
            var ct = card.transform;
            Juice.Punch(card.rectTransform, 0.06f, 0.25f);
            var t = UIKit.Label(ct, L.T("ТАЛИСМАНЫ", "CHARMS"), 54, UIKit.Gold, TextAnchor.MiddleCenter, 0.1f, 0.86f, 0.9f, 0.97f, true);
            UIKit.AddOutline(t, UIKit.Hex("#12091F"), 3f);
            var st = UIKit.Label(ct, L.T("Вечные бонусы из Шаурма-Пасса. Работают всегда, даже после переезда.", "Permanent bonuses from the Shawarma Pass. Always active, even after moving."),
                24, UIKit.Alpha(UIKit.Cream, 0.85f), TextAnchor.MiddleCenter, 0.05f, 0.8f, 0.95f, 0.87f, true);
            UIKit.AddShadow(st, 2f, 0.5f);
            UIKit.Button(ct, "X", UIKit.Red, close, 0.925f, 0.88f, 0.98f, 0.97f, 34);

            for (int i = 0; i < Meta.Charms.Length; i++)
            {
                var d = Meta.Charms[i];
                bool has = Meta.Has(_save, i);
                int col = i % 4, row = i / 4;
                float x0 = 0.025f + col * 0.24f, y1 = 0.785f - row * 0.255f;
                var c = UIKit.Panel(ct, "Charm", has ? UIKit.Hex("#43286B") : UIKit.Hex("#2E2240"), x0, y1 - 0.24f, x0 + 0.23f, y1);
                UIKit.AddShadow(c, 6f, 0.4f);
                if (has) UIKit.AddOutline(c, UIKit.Hex(d.color), 3f);
                var cc = c.transform;
                // витрина с подсветкой
                if (has)
                {
                    var glow = UIKit.Shape(cc, UIKit.CircleHD, UIKit.Alpha(UIKit.Hex(d.color), 0.25f), 0.5f, 0.66f, 120f, 120f);
                    glow.gameObject.AddComponent<Blinker>().speed = 1.2f;
                }
                CharmIcon(cc, i, 0.5f, 0.68f, 84f);
                if (!has)
                {
                    UIKit.Shape(cc, UIKit.CircleHD, UIKit.Alpha(Color.black, 0.6f), 0.5f, 0.64f, 78f, 78f);
                    UIKit.Shape(cc, UIKit.Circle, UIKit.Gold, 0.5f, 0.7f, 26f, 26f);
                    UIKit.Shape(cc, UIKit.Circle, UIKit.Hex("#2E2240"), 0.5f, 0.7f, 14f, 14f);
                    UIKit.Shape(cc, UIKit.RoundedSmall, UIKit.Gold, 0.5f, 0.61f, 34f, 26f);
                }
                if (d.source == 1)
                {
                    var tag = UIKit.Panel(cc, "Plus", UIKit.Hex("#FFB300"), 0.66f, 0.84f, 0.97f, 0.97f);
                    UIKit.FitLabel(tag.transform, L.T("ПАСС+", "PASS+"), 16, UIKit.Dark, 0.05f, 0.08f, 0.95f, 0.92f);
                }
                var nm = UIKit.FitLabel(cc, d.Name, 26, has ? UIKit.Gold : UIKit.Alpha(UIKit.Cream, 0.7f), 0.05f, 0.33f, 0.95f, 0.46f);
                UIKit.AddShadow(nm, 2f, 0.6f);
                var ds = UIKit.FitLabel(cc, d.Desc, 18, has ? UIKit.Cream : UIKit.Alpha(UIKit.Cream, 0.5f), 0.06f, 0.17f, 0.94f, 0.33f, false);
                UIKit.AddShadow(ds, 2f, 0.6f);
                var pill = UIKit.Panel(cc, "Pill", has ? UIKit.Green : UIKit.Hex("#5E4B7A"), 0.12f, 0.03f, 0.88f, 0.15f);
                UIKit.FitLabel(pill.transform, has ? L.T("ЕСТЬ!", "OWNED!") : Meta.CharmSource(i), 18, Color.white, 0.05f, 0.08f, 0.95f, 0.92f);
            }
        }

        // =====================================================================
        //  ШАУРМА-ПАСС: 50 уровней, две дорожки (бесплатная и Пасс+), листается вбок.
        //  Под ним — награды за время в игре (5 минут … 12 часов).
        // =====================================================================
        void ShowPass()
        {
            var s = NewScreen("Pass");
            Backdrop(s, true);
            int lvl = Meta.PassLevel(_save.passXp);

            // шапка
            var top = UIKit.Panel(s, "Top", UIKit.Hex("#4A148C"), 0.02f, 0.85f, 0.98f, 0.95f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.AddOutline(top, UIKit.Gold, 2f);
            var tt = UIKit.Label(top.transform, L.T("ШАУРМА-ПАСС", "SHAWARMA PASS"), 42, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0, 0.28f, 1, true);
            UIKit.AddOutline(tt, UIKit.Hex("#1A0B2E"), 2f);
            UIKit.Label(top.transform, L.T("Уровень ", "Level ") + lvl + " / " + Meta.PassLevels, 30, Color.white, TextAnchor.MiddleLeft, 0.28f, 0.45f, 0.5f, 1, true);
            var bar = UIKit.Bar(top.transform, UIKit.Alpha(Color.white, 0.2f), UIKit.Gold, 0.28f, 0.14f, 0.55f, 0.42f);
            UIKit.SetFill(bar, Meta.PassProgress(_save.passXp));
            UIKit.Label(top.transform, L.T("опыт — за каждого довольного клиента", "XP for every happy customer"), 18, UIKit.Alpha(Color.white, 0.8f),
                TextAnchor.MiddleLeft, 0.3f, 0.14f, 0.55f, 0.42f, false);
            if (_save.passPlus)
            {
                var ok = UIKit.Panel(top.transform, "PlusOn", UIKit.Hex("#FFB300"), 0.6f, 0.15f, 0.98f, 0.85f);
                Crown(ok.transform, 0.1f, 0.5f, 50f);
                UIKit.FitLabel(ok.transform, L.T("ШАУРМА-ПАСС+ АКТИВЕН", "SHAWARMA PASS+ ACTIVE"), 28, UIKit.Dark, 0.2f, 0.1f, 0.97f, 0.9f);
            }
            else
            {
                var buy = UIKit.Button(top.transform, "", UIKit.Hex("#FF8F00"), () =>
                {
                    Payments.Buy("pass_plus", Meta.PassPlusYan, () =>
                    {
                        _save.passPlus = true;
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Fanfare);
                        Notify(L.T("Шаурма-Пасс+", "Shawarma Pass+"), L.T("Открыты все награды второй дорожки!", "The whole premium track is unlocked!"));
                        ShowPass();
                    });
                }, 0.6f, 0.12f, 0.98f, 0.88f, 26);
                Crown(buy.transform, 0.09f, 0.5f, 50f);
                UIKit.SetLabel(buy, "");
                UIKit.FitLabel(buy.transform, L.T("Купить Шаурма-Пасс+", "Get Shawarma Pass+"), 28, Color.white, 0.18f, 0.42f, 0.98f, 0.95f);
                UIKit.FitLabel(buy.transform, Payments.Price(Meta.PassPlusYan) + (Payments.Free ? "  •  " + Payments.FreeNote : ""), 20, UIKit.Hex("#FFF3C4"), 0.18f, 0.06f, 0.98f, 0.45f);
                buy.gameObject.AddComponent<Bobber>().amp = 2f;
            }

            // подписи дорожек
            var lf = UIKit.Panel(s, "FreeTag", UIKit.Hex("#2E7D32"), 0.02f, 0.62f, 0.1f, 0.78f);
            UIKit.FitLabel(lf.transform, L.T("Бес-\nплатно", "Free"), 22, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
            var lp = UIKit.Panel(s, "PlusTag", UIKit.Hex("#FF8F00"), 0.02f, 0.4f, 0.1f, 0.6f);
            Crown(lp.transform, 0.5f, 0.72f, 44f);
            UIKit.FitLabel(lp.transform, L.T("Пасс+", "Pass+"), 24, Color.white, 0.05f, 0.08f, 0.95f, 0.45f);

            // лента уровней (листается)
            var view = UIKit.Rect(s, "PassView");
            UIKit.Place(view, 0.11f, 0.38f, 0.98f, 0.84f);
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            view.gameObject.AddComponent<RectMask2D>();
            var content = UIKit.Rect(view, "Content");
            const float colW = 150f;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(colW * Meta.PassLevels + 20f, 0f);
            content.anchoredPosition = Vector2.zero;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 60f;
            sr.content = content;
            sr.viewport = view;

            int focus = Mathf.Clamp(lvl, 1, Meta.PassLevels);
            for (int i = 1; i <= Meta.PassLevels; i++)
            {
                if (!Meta.PassClaimed(_save, i, false) && i <= lvl) { focus = i; break; }
            }
            for (int i = 1; i <= Meta.PassLevels; i++)
            {
                var col = UIKit.Rect(content, "Col" + i);
                col.anchorMin = new Vector2(0f, 0f);
                col.anchorMax = new Vector2(0f, 1f);
                col.pivot = new Vector2(0f, 0.5f);
                col.sizeDelta = new Vector2(colW - 10f, 0f);
                col.anchoredPosition = new Vector2(10f + (i - 1) * colW, 0f);
                bool reached = i <= lvl;
                var num = UIKit.Panel(col, "Num", reached ? UIKit.Gold : UIKit.Hex("#5E4B7A"), 0.2f, 0.88f, 0.8f, 0.98f);
                UIKit.FitLabel(num.transform, i.ToString(), 26, reached ? UIKit.Dark : Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
                PassCell(col, i, false, reached, 0.5f, 0.86f);
                PassCell(col, i, true, reached, 0.03f, 0.47f);
            }
            // сразу показать ближайшую награду
            float target = Mathf.Max(0f, (focus - 3) * colW);
            content.anchoredPosition = new Vector2(-target, 0f);

            // награды за время в игре
            BuildPlayTime(s);

            // коллекция талисманов
            int got = 0;
            for (int i = 0; i < Meta.Charms.Length; i++) if (Meta.Has(_save, i)) got++;
            var cb = UIKit.Button(s, "", UIKit.Hex("#6A1B9A"), () => { Sfx.Play(Sound.Click); ShowCharms(s); }, 0.46f, 0.02f, 0.98f, 0.11f, 30);
            for (int i = 0; i < Meta.Charms.Length; i++)
            {
                float x = 0.05f + i * 0.047f;
                CharmIcon(cb.transform, i, x, 0.5f, 44f);
                if (!Meta.Has(_save, i)) UIKit.Shape(cb.transform, UIKit.Circle, UIKit.Alpha(Color.black, 0.55f), x, 0.45f, 42f, 42f);
            }
            var cl = UIKit.FitLabel(cb.transform, L.T("Талисманы ", "Charms ") + got + "/" + Meta.Charms.Length, 26, UIKit.Gold, 0.6f, 0.1f, 0.98f, 0.9f);
            UIKit.AddShadow(cl, 2f, 0.6f);
            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.02f, 0.2f, 0.11f, 36);
        }

        /// <summary>Карточка награды одной дорожки пасса.</summary>
        void PassCell(RectTransform col, int i, bool plus, bool reached, float y0, float y1)
        {
            bool claimed = Meta.PassClaimed(_save, i, plus);
            bool lockedPlus = plus && !_save.passPlus;
            bool ready = reached && !claimed && !lockedPlus;
            Color bg = claimed ? UIKit.Hex("#E8F5E9") : (plus ? UIKit.Hex("#FFE0B2") : (reached ? UIKit.Hex("#FFF3C4") : UIKit.Hex("#E0E0E0")));
            var card = UIKit.Panel(col, plus ? "Plus" : "Free", bg, 0.02f, y0, 0.98f, y1);
            UIKit.AddShadow(card, 4f, 0.3f);
            if (plus) UIKit.AddOutline(card, UIKit.Hex("#FF8F00"), 2f);
            if (ready) UIKit.AddOutline(card, UIKit.Gold, 4f);
            var ct = card.transform;
            int t, v;
            Meta.PassReward(i, plus, out t, out v);
            if (t == 0) { Icons.Coin(ct, 0.5f, 0.58f, 56f); if (plus) Icons.Coin(ct, 0.62f, 0.64f, 40f); }
            else if (t == 1)
            {
                if (v == 0) Icons.Cup(ct, 0.5f, 0.58f, 62f);
                else if (v == 1) Icons.Flyer(ct, 0.5f, 0.58f, 62f);
                else Icons.GoldLavash(ct, 0.5f, 0.58f, 56f);
            }
            else if (t == 2)
            {
                var gl = UIKit.Shape(ct, UIKit.CircleHD, UIKit.Alpha(UIKit.Hex(Meta.Charms[v].color), 0.35f), 0.5f, 0.6f, 96f, 96f);
                gl.gameObject.AddComponent<Blinker>().speed = 1.5f;
                CharmIcon(ct, v, 0.5f, 0.6f, 70f);
            }
            else if (t == 4) Icons.Chest(ct, 0.5f, 0.58f, plus ? 70f : 60f, false);
            else if (t == 5)
            {
                UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#E53935"), 0.5f, 0.6f, 60f, 60f);
                UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#FDD835"), 0.5f, 0.6f, 40f, 40f);
                UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#1E88E5"), 0.5f, 0.6f, 18f, 18f);
            }
            else Crown(ct, 0.5f, 0.58f, 56f);
            UIKit.FitLabel(ct, Meta.PassRewardText(_save, i, plus), 18, UIKit.Dark, 0.04f, 0.03f, 0.96f, 0.28f);

            if (claimed)
            {
                var ok = UIKit.Shape(ct, UIKit.Circle, UIKit.Green, 0.84f, 0.86f, 30f, 30f);
                UIKit.FitLabel(ok.transform, "OK", 14, Color.white, 0f, 0f, 1f, 1f);
            }
            else if (lockedPlus)
            {
                UIKit.Panel(ct, "Dim", UIKit.Alpha(UIKit.Hex("#3E2723"), 0.35f), 0, 0, 1, 1);
                var lk = UIKit.Rect(ct, "Lock");
                UIKit.PlaceFixed(lk, 0.84f, 0.85f, 30f, 30f);
                UIKit.Shape(lk, UIKit.Circle, UIKit.Hex("#FF8F00"), 0.5f, 0.66f, 18f, 18f);
                UIKit.Shape(lk, UIKit.Circle, UIKit.Hex("#FFE0B2"), 0.5f, 0.66f, 9f, 9f);
                UIKit.Shape(lk, UIKit.RoundedSmall, UIKit.Hex("#FF8F00"), 0.5f, 0.36f, 24f, 18f);
            }
            else if (ready)
            {
                var gb = UIKit.Panel(ct, "Take", UIKit.Green, 0.12f, 0.3f, 0.88f, 0.44f);
                UIKit.FitLabel(gb.transform, L.T("ЗАБРАТЬ", "CLAIM"), 18, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
                gb.gameObject.AddComponent<Bobber>().amp = 3f;
                int lv = i;
                UIKit.MakeButton(card, () =>
                {
                    Meta.GivePassReward(_save, lv, plus);
                    if (!plus) _save.passClaimed = Mathf.Max(_save.passClaimed, lv);
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Fanfare);
                    Notify(plus ? L.T("Награда Пасс+", "Pass+ reward") : L.T("Награда пасса", "Pass reward"), Meta.PassRewardText(_save, lv, plus));
                    ShowPass();
                });
            }
            else if (!reached)
                UIKit.Panel(ct, "Lock", UIKit.Alpha(Color.black, 0.12f), 0, 0, 1, 1);
        }

        /// <summary>Лента наград за время в игре: 5 минут … 12 часов.</summary>
        void BuildPlayTime(RectTransform s)
        {
            var box = UIKit.Panel(s, "PlayTime", UIKit.Hex("#1A237E"), 0.02f, 0.13f, 0.98f, 0.36f);
            UIKit.AddShadow(box, 6f, 0.4f);
            UIKit.AddOutline(box, UIKit.Hex("#7986CB"), 2f);
            var bt = box.transform;
            int mins = Mathf.FloorToInt(_save.playSeconds / 60f);
            UIKit.Label(bt, L.T("НАГРАДЫ ЗА ВРЕМЯ В ИГРЕ", "PLAY TIME REWARDS"), 26, UIKit.Gold, TextAnchor.MiddleLeft, 0.015f, 0.8f, 0.5f, 0.98f, true);
            UIKit.Label(bt, L.T("Ты играешь: ", "You've played: ") + Meta.PlayTimeText(mins), 22, Color.white, TextAnchor.MiddleRight, 0.5f, 0.8f, 0.985f, 0.98f, true);
            int n = Meta.PlayMinutes.Length;
            for (int i = 0; i < n; i++)
            {
                float x0 = 0.012f + i * (0.976f / n), x1 = x0 + 0.976f / n - 0.008f;
                bool claimed = _save.playClaimed[i] == 1;
                bool ready = Meta.PlayReady(_save, i);
                float need = Meta.PlayMinutes[i] * 60f;
                bool special = i == 4 || i == 7 || i == 8;
                var c = UIKit.Panel(bt, "Play", claimed ? UIKit.Hex("#C8E6C9") : (ready ? UIKit.Hex("#FFF3C4") : UIKit.Hex(special ? "#3949AB" : "#283593")), x0, 0.05f, x1, 0.78f);
                if (ready) UIKit.AddOutline(c, UIKit.Gold, 3f);
                else if (special) UIKit.AddOutline(c, UIKit.Hex("#FFD54F"), 2f);
                var ct = c.transform;
                Color txt = claimed || ready ? UIKit.Dark : Color.white;
                UIKit.FitLabel(ct, Meta.PlayTimeText(Meta.PlayMinutes[i]), 22, txt, 0.04f, 0.8f, 0.96f, 0.97f);
                // значок награды
                switch (i)
                {
                    case 0: Icons.Cup(ct, 0.36f, 0.55f, 40f); Icons.Flyer(ct, 0.64f, 0.55f, 40f); break;
                    case 1: Icons.Chest(ct, 0.5f, 0.55f, 56f, false); break;
                    case 2:
                        UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#E53935"), 0.5f, 0.55f, 50f, 50f);
                        UIKit.Shape(ct, UIKit.CircleHD, UIKit.Hex("#FDD835"), 0.5f, 0.55f, 32f, 32f);
                        break;
                    case 3: case 6: Crown(ct, 0.5f, 0.55f, 50f); break;
                    case 4: CharmIcon(ct, Meta.CharmHourglass, 0.5f, 0.56f, 56f); break;
                    case 5: Icons.Chest(ct, 0.5f, 0.55f, 64f, true); break;
                    case 7: CharmIcon(ct, Meta.CharmChefStar, 0.5f, 0.56f, 56f); break;
                    default:
                        for (int k = 0; k < 5; k++)
                            UIKit.Shape(ct, UIKit.RoundedSmall, k % 2 == 0 ? UIKit.Hex("#FFC928") : UIKit.Hex("#FFF8E1"), 0.3f + k * 0.1f, 0.58f, 16f, 30f);
                        break;
                }
                UIKit.FitLabel(ct, Meta.PlayRewardText(i), 16, txt, 0.04f, 0.2f, 0.96f, 0.34f);
                if (claimed)
                    UIKit.FitLabel(ct, L.T("получено", "claimed"), 14, UIKit.Hex("#2E7D32"), 0.04f, 0.03f, 0.96f, 0.18f);
                else if (ready)
                {
                    var gb = UIKit.Panel(ct, "Take", UIKit.Green, 0.08f, 0.03f, 0.92f, 0.19f);
                    UIKit.FitLabel(gb.transform, L.T("ЗАБРАТЬ", "CLAIM"), 16, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
                    c.gameObject.AddComponent<Bobber>().amp = 3f;
                    int idx = i;
                    UIKit.MakeButton(c, () =>
                    {
                        Meta.GivePlayReward(_save, idx);
                        SaveSystem.Save(_save);
                        Sfx.Play(Sound.Fanfare);
                        Notify(L.T("Награда за время в игре", "Play time reward"), Meta.PlayRewardText(idx) + " — " + Meta.PlayRewardDesc(idx));
                        ShowPass();
                    });
                }
                else
                {
                    var pb = UIKit.Bar(ct, UIKit.Alpha(Color.white, 0.2f), UIKit.Hex("#7986CB"), 0.08f, 0.05f, 0.92f, 0.16f);
                    UIKit.SetFill(pb, Mathf.Clamp01(_save.playSeconds / need));
                }
            }
        }
    }
}
