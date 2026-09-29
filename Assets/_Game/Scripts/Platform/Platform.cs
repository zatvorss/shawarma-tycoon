using System;
using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Прослойка между игрой и Яндекс Играми.
    /// Сейчас ЗАГЛУШКИ. На этапе SDK (PluginYG2) меняем только тела этих методов.
    /// </summary>
    public static class Platform
    {
        /// <summary>Игра загрузилась (Яндекс: LoadingAPI.ready()).</summary>
        public static void GameReady() { Debug.Log("[Platform] GameReady"); }

        /// <summary>Начался активный геймплей (Яндекс: GameplayAPI.start()).</summary>
        public static void GameplayStart() { Debug.Log("[Platform] GameplayStart"); }

        /// <summary>Пауза / меню (Яндекс: GameplayAPI.stop()).</summary>
        public static void GameplayStop() { Debug.Log("[Platform] GameplayStop"); }

        /// <summary>Язык площадки ("ru", "en", "tr"...). Пусто = не знаем.</summary>
        public static string Language() { return ""; }

        public static void ShowFullscreen(Action onClose)
        {
            Debug.Log("[Platform] Fullscreen ad (stub)");
            if (onClose != null) onClose();
        }

        /// <summary>Реклама за награду. onReward — только если досмотрели.</summary>
        public static void ShowRewarded(string id, Action onReward, Action onClose)
        {
            Debug.Log("[Platform] Rewarded ad (stub): " + id);
            if (onReward != null) onReward();
            if (onClose != null) onClose();
        }

        /// <summary>Лидерборд "лучший день".</summary>
        public static void SubmitScore(int score)
        {
            Debug.Log("[Platform] Leaderboard score (stub): " + score);
        }

        public static void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public static string LoadString(string key)
        {
            return PlayerPrefs.GetString(key, "");
        }
    }
}
