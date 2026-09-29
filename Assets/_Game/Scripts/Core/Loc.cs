using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Локализация: L.T("по-русски", "in English").
    /// Язык выбирается автоматически (потом — из SDK Яндекса) или вручную в меню.
    /// </summary>
    public static class L
    {
        public static bool En;

        public static void Init(string saved)
        {
            if (saved == "ru") { En = false; return; }
            if (saved == "en") { En = true; return; }
            string p = Platform.Language();
            if (!string.IsNullOrEmpty(p)) { En = !(p == "ru" || p == "be" || p == "kk" || p == "uk" || p == "uz"); return; }
            var s = Application.systemLanguage;
            En = !(s == SystemLanguage.Russian || s == SystemLanguage.Ukrainian || s == SystemLanguage.Belarusian);
        }

        public static string T(string ru, string en) { return En ? en : ru; }
    }
}
