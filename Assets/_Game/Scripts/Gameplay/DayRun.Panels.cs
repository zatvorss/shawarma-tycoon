using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Красивые карточки рабочего дня: начало дня и задания.</summary>
    public partial class DayRun
    {
        GameObject _introCard, _questCard;

        /// <summary>Тёмная карточка-вывеска с золотой рамкой поверх улицы. Тап — закрыть, сама прячется через hide секунд.</summary>
        RectTransform Card(string name, float x0, float y0, float x1, float y1, float hide, out GameObject go)
        {
            var c = UIKit.Panel(_root, name, UIKit.Hex("#2A1C13"), x0, y0, x1, y1);
            c.rectTransform.SetSiblingIndex(_banner.GetSiblingIndex());
            UIKit.AddShadow(c, 14f, 0.5f);
            UIKit.AddOutline(c, UIKit.Hex("#C98A2E"), 3f);
            UIKit.Panel(c.transform, "Inner", UIKit.Hex("#3A2819"), 0.012f, 0.02f, 0.988f, 0.98f);
            go = c.gameObject;
            var g = go;
            UIKit.MakeButton(c, () => { if (g != null) Destroy(g); });
            Juice.Punch(c.rectTransform, 0.08f, 0.3f);
            if (hide > 0f) StartCoroutine(FadeAway(go, hide));
            return c.rectTransform;
        }

        IEnumerator FadeAway(GameObject go, float after)
        {
            yield return new WaitForSecondsRealtime(after);
            if (go == null) yield break;
            var cg = go.AddComponent<CanvasGroup>();
            float t = 0f;
            while (t < 0.35f && go != null)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - t / 0.35f;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        static Text Shadowed(Text t, float d = 2f) { UIKit.AddShadow(t, d, 0.6f); return t; }

        // =====================================================================
        //  НАЧАЛО ДНЯ: локация, день, цель и что сегодня будет
        // =====================================================================
        void ShowDayIntro()
        {
            var lines = new List<string>();
            var cols = new List<string>();
            string news = GameData.NewStuff(_eday);
            if (news.Length > 0) { lines.Add(L.T("Новое: ", "New: ") + news); cols.Add("#80DEEA"); }
            if (_event == DayEvent.Rain) { lines.Add(L.T("Дождь: клиентов меньше, зато чаевые ×1.5", "Rain: fewer customers, but tips ×1.5")); cols.Add("#90CAF9"); }
            if (_event == DayEvent.Rush) { lines.Add(L.T("Сегодня будет час пик — готовься заранее!", "Rush hour today — get ready!")); cols.Add("#FF8A80"); }
            EventLines(lines, cols);
            int n = Mathf.Min(lines.Count, 5);

            float h = 0.25f + n * 0.045f;
            var c = Card("DayIntro", 0.26f, 0.9f - h, 0.74f, 0.9f, 4.5f, out _introCard);
            float head = 0.2f / h;         // доля высоты под шапку
            float goalH = 0.12f / h;

            // шапка цвета ларька: название локации и «день N»
            var rib = UIKit.Panel(c, "Ribbon", UIKit.Hex(_loc.kiosk), 0.012f, 1f - head, 0.988f, 0.98f);
            UIKit.Panel(rib.transform, "Shade", UIKit.Alpha(Color.black, 0.18f), 0f, 0f, 1f, 0.12f, false);
            var ln = UIKit.Label(rib.transform, _loc.Name.ToUpper(), 50, Color.white, TextAnchor.MiddleLeft, 0.04f, 0.1f, 0.72f, 0.95f, true);
            UIKit.AddOutline(ln, UIKit.Alpha(UIKit.Dark, 0.8f), 3f);
            var dp = UIKit.Panel(rib.transform, "Day", UIKit.Gold, 0.74f, 0.2f, 0.97f, 0.8f);
            UIKit.AddShadow(dp, 4f, 0.4f);
            UIKit.FitLabel(dp.transform, L.T("ДЕНЬ ", "DAY ") + _day, 36, UIKit.Dark, 0.05f, 0.08f, 0.95f, 0.92f);

            // цель дня
            float gy1 = 1f - head - 0.02f / h, gy0 = gy1 - goalH;
            var goal = UIKit.Panel(c, "Goal", UIKit.Alpha(Color.black, 0.3f), 0.04f, gy0, 0.96f, gy1);
            Icons.Coin(goal.transform, 0.07f, 0.5f, 56f);
            Shadowed(UIKit.Label(goal.transform, L.T("Цель дня", "Daily goal"), 26, UIKit.Alpha(UIKit.Cream, 0.8f), TextAnchor.MiddleLeft, 0.13f, 0f, 0.4f, 1f, true));
            var gv = Shadowed(UIKit.Label(goal.transform, GameData.Money(_goal), 46, UIKit.Gold, TextAnchor.MiddleLeft, 0.38f, 0f, 0.7f, 1f, true));
            UIKit.AddOutline(gv, UIKit.Dark, 2f);
            for (int st = 0; st < 3; st++) Icons.Star(goal.transform, 0.76f + st * 0.075f, 0.5f, 40f, true);

            // что сегодня будет
            if (n > 0)
            {
                float top = gy0 - 0.03f / h;
                float rowH = (top - 0.05f / h) / n;
                for (int i = 0; i < n; i++)
                {
                    float y1 = top - i * rowH, y0 = y1 - rowH;
                    UIKit.Shape(c, UIKit.Circle, UIKit.Hex(cols[i]), 0.07f, (y0 + y1) * 0.5f, 18f, 18f);
                    var t = UIKit.Label(c, lines[i], 24, UIKit.Cream, TextAnchor.MiddleLeft, 0.1f, y0, 0.96f, y1, false);
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 14;
                    t.resizeTextMaxSize = 24;
                    Shadowed(t);
                }
            }
        }

        // =====================================================================
        //  ЗАДАНИЯ ДНЯ
        // =====================================================================
        void ShowQuests()
        {
            if (_questCard != null) { Destroy(_questCard); _questCard = null; return; }
            int n = _quests.Count;
            float h = 0.1f + n * 0.075f;
            var c = Card("Quests", 0.25f, 0.9f - h, 0.75f, 0.9f, 6f, out _questCard);
            float head = 0.08f / h;
            var rib = UIKit.Panel(c, "Ribbon", UIKit.Purple, 0.012f, 1f - head, 0.988f, 0.97f);
            var tt = UIKit.Label(rib.transform, L.T("ЗАДАНИЯ ДНЯ", "DAILY QUESTS"), 40, Color.white, TextAnchor.MiddleLeft, 0.04f, 0f, 0.7f, 1f, true);
            UIKit.AddOutline(tt, UIKit.Alpha(UIKit.Dark, 0.8f), 3f);
            Shadowed(UIKit.Label(rib.transform, QuestsDone() + " / " + n, 34, UIKit.Gold, TextAnchor.MiddleRight, 0.7f, 0f, 0.96f, 1f, true));

            float top = 1f - head - 0.015f / h;
            float rowH = (top - 0.02f / h) / n;
            for (int i = 0; i < n; i++)
            {
                var q = _quests[i];
                float y1 = top - i * rowH, y0 = y1 - rowH + 0.008f / h;
                var row = UIKit.Panel(c, "Q", q.done ? UIKit.Hex("#2F4A2B") : UIKit.Alpha(Color.black, 0.3f), 0.03f, y0, 0.97f, y1);
                if (q.done) UIKit.AddOutline(row, UIKit.Hex("#7CB342"), 2f);
                var r = row.transform;
                var dot = UIKit.Shape(r, UIKit.CircleHD, q.done ? UIKit.Green : UIKit.Hex("#5D4037"), 0.05f, 0.5f, 44f, 44f);
                UIKit.FitLabel(dot.transform, q.done ? "OK" : (i + 1).ToString(), 22, Color.white, 0.1f, 0.15f, 0.9f, 0.85f);
                Shadowed(UIKit.Label(r, q.Text, 26, q.done ? UIKit.Hex("#C5E1A5") : UIKit.Cream, TextAnchor.MiddleLeft, 0.1f, 0.42f, 0.72f, 0.98f, true));
                // прогресс
                var bar = UIKit.Bar(r, UIKit.Alpha(Color.white, 0.12f), q.done ? UIKit.Green : UIKit.Orange, 0.1f, 0.14f, 0.62f, 0.36f);
                UIKit.SetFill(bar, q.target > 0 ? (float)Mathf.Min(q.progress, q.target) / q.target : (q.done ? 1f : 0f));
                if (q.Progress.Length > 0)
                    Shadowed(UIKit.Label(r, q.Progress, 20, UIKit.Cream, TextAnchor.MiddleLeft, 0.63f, 0.08f, 0.74f, 0.42f, true));
                // награда
                var rw = UIKit.Panel(r, "Reward", UIKit.Alpha(Color.black, 0.35f), 0.75f, 0.14f, 0.98f, 0.86f);
                Icons.Coin(rw.transform, 0.2f, 0.5f, 34f);
                Shadowed(UIKit.FitLabel(rw.transform, "+" + GameData.Money(q.reward), 26, UIKit.Gold, 0.36f, 0.1f, 0.97f, 0.9f));
            }
        }
    }
}
