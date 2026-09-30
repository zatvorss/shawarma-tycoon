using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Рекорды: два топа — по деньгам, заработанным за всё время, и по числу обслуженных клиентов.
    /// Данные приходят из лидербордов Яндекс Игр (Platform.GetLeaderboard). Пока SDK не подключён,
    /// показываем твой результат и подсказку, что таблица появится после публикации.
    /// </summary>
    public partial class Game
    {
        void ShowLeaders()
        {
            var s = NewScreen("Leaders");
            Backdrop(s, true);
            var top = UIKit.Panel(s, "Top", UIKit.Hex("#006064"), 0.02f, 0.84f, 0.98f, 0.94f);
            UIKit.AddShadow(top, 8f, 0.4f);
            UIKit.AddOutline(top, UIKit.Gold, 2f);
            var t = UIKit.Label(top.transform, L.T("РЕКОРДЫ", "LEADERBOARD"), 46, UIKit.Gold, TextAnchor.MiddleLeft, 0.03f, 0, 0.4f, 1, true);
            UIKit.AddOutline(t, UIKit.Hex("#00262A"), 2f);
            UIKit.Label(top.transform, L.T("Лучшие шаурмичные магнаты всех игроков", "The best shawarma tycoons among all players"), 24, Color.white, TextAnchor.MiddleRight, 0.4f, 0, 0.97f, 1, false);

            LeaderColumn(s, 0.02f, 0.49f, Platform.LbMoney, L.T("Заработано за всё время", "Earned all-time"), UIKit.Gold, _save.lifetime, true);
            LeaderColumn(s, 0.51f, 0.98f, Platform.LbServed, L.T("Обслужено клиентов", "Customers served"), UIKit.Hex("#4DD0E1"), _save.totalServed, false);

            UIKit.Button(s, L.T("← Назад", "← Back"), UIKit.Steel, () => { Sfx.Play(Sound.Click); ShowMenu(); }, 0.02f, 0.02f, 0.2f, 0.11f, 36);
        }

        void LeaderColumn(RectTransform s, float x0, float x1, string board, string title, Color accent, long mine, bool money)
        {
            var col = UIKit.Panel(s, "Board_" + board, UIKit.Hex("#1B2631"), x0, 0.13f, x1, 0.82f);
            UIKit.AddShadow(col, 8f, 0.45f);
            UIKit.AddOutline(col, accent, 2f);
            var ct = col.transform;
            var head = UIKit.Panel(ct, "Head", UIKit.Alpha(accent, 0.2f), 0f, 0.88f, 1f, 1f);
            if (money) Icons.Coin(head.transform, 0.07f, 0.5f, 50f); else Icons.People(head.transform, 0.07f, 0.5f, 56f);
            UIKit.FitLabel(head.transform, title, 32, accent, 0.14f, 0.1f, 0.97f, 0.9f);

            // «ты» — всегда внизу колонки
            var me = UIKit.Panel(ct, "Me", UIKit.Alpha(accent, 0.28f), 0.02f, 0.02f, 0.98f, 0.12f);
            UIKit.AddOutline(me, accent, 2f);
            UIKit.Label(me.transform, L.T("Ты", "You"), 30, Color.white, TextAnchor.MiddleLeft, 0.04f, 0f, 0.4f, 1f, true);
            UIKit.Label(me.transform, money ? GameData.Money(mine) : mine.ToString("N0").Replace(",", " "), 32, accent, TextAnchor.MiddleRight, 0.4f, 0f, 0.96f, 1f, true);

            var list = UIKit.Rect(ct, "List");
            UIKit.Place(list, 0.02f, 0.14f, 0.98f, 0.86f);
            var loading = UIKit.Label(list, L.T("Загрузка...", "Loading..."), 26, UIKit.Alpha(Color.white, 0.6f), TextAnchor.MiddleCenter, 0f, 0f, 1f, 1f, false);
            Platform.GetLeaderboard(board, 10, entries =>
            {
                if (list == null) return;
                if (entries == null || entries.Count == 0)
                {
                    loading.text = L.T("Таблица рекордов появится,\nкогда игра выйдет на Яндекс Играх.\n\nТвой результат уже считается!",
                                       "The leaderboard appears\nonce the game is live on Yandex Games.\n\nYour score is already counted!");
                    return;
                }
                loading.gameObject.SetActive(false);
                int n = Mathf.Min(10, entries.Count);
                for (int i = 0; i < n; i++)
                {
                    var e = entries[i];
                    float y1 = 1f - i * 0.1f;
                    var row = UIKit.Panel(list, "Row", e.me ? UIKit.Alpha(accent, 0.3f) : UIKit.Alpha(Color.white, i % 2 == 0 ? 0.06f : 0.02f), 0f, y1 - 0.095f, 1f, y1);
                    Color medal = e.rank == 1 ? UIKit.Gold : (e.rank == 2 ? UIKit.Hex("#CFD8DC") : (e.rank == 3 ? UIKit.Hex("#D7A77A") : UIKit.Alpha(Color.white, 0.15f)));
                    var m = UIKit.Shape(row.transform, UIKit.CircleHD, medal, 0.06f, 0.5f, 40f, 40f);
                    UIKit.FitLabel(m.transform, e.rank.ToString(), 20, e.rank <= 3 ? UIKit.Dark : Color.white, 0f, 0f, 1f, 1f);
                    UIKit.Label(row.transform, e.name, 26, Color.white, TextAnchor.MiddleLeft, 0.13f, 0f, 0.62f, 1f, e.me);
                    UIKit.Label(row.transform, money ? GameData.Money(e.score) : e.score.ToString(), 26, accent, TextAnchor.MiddleRight, 0.62f, 0f, 0.97f, 1f, true);
                }
            });
        }
    }
}
