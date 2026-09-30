using System;
using UnityEngine;

namespace ValourGames.Shawarma
{
    [Serializable]
    public class SaveData
    {
        public int day = 1;
        public long money = 0;
        public int totalServed = 0;
        public int bestDay = 0;
        public int stars = 0;
        public int location = 0;
        public int[] locUnlocked = new int[6];
        public bool sound = true;
        public bool music = true;
        public string lang = "";   // "" = авто, "ru", "en"
        public int[] upgrades = new int[24];

        // статистика для достижений
        public int totalPerfect = 0;
        public int bestCombo = 0;
        public int criticWins = 0;
        public int toGoServed = 0;
        public int bestStarsDay = 0;
        public int[] achDone = new int[0];

        // бустеры: сколько куплено и что включено на следующий день
        public int[] boosts = new int[3];
        public int[] boostOn = new int[3];

        // v0.5: путь к миллиарду, бизнесы, рейтинг, колесо
        public long lifetime = 0;
        public int rank = 0;
        public int[] biz = new int[6];
        public float rep = 3f;
        public string wheelDate = "";
        public int achVer = 0;
        public int[] achTier = new int[0];   // сколько уровней достижения забрано (0..5)
        public int[] achSeen = new int[0];   // о скольких уровнях уже показали уведомление
        public int couriers = 0, cats = 0, specials = 0, chests = 0;

        // v0.7: ребёрты, слава, сюжет
        public int rebirths = 0;
        public int fame = 0;
        public int[] perks = new int[6];
        public int[] story = new int[16];

        // v0.8: Шаурма-Пасс, скины
        public int passXp = 0;
        public int passClaimed = 0;   // сколько уровней пасса забрано
        public int skin = 0;
        public int[] skins = new int[8];
        public int[] charms = new int[6];   // талисманы из Шаурма-Пасса (навсегда)
        public int bizVer = 0;       // 1 = состояния достижений: 0 нет, 1 можно забрать, 2 забрано

        // v0.13: второй ларёк (бизнес без работы: закупаешь продукты — он продаёт, забираешь выручку)
        public int kiosk2Stock = 0;
        public long kiosk2Cash = 0;
        public int kiosk2LastSold = 0;

        // v0.14: пройденные шаги обучения ("|basics|ask|fly|...")
        public string tutor = "";
        public bool Tutored(string key) { return tutor != null && tutor.Contains("|" + key + "|"); }
        public void MarkTutored(string key) { if (!Tutored(key)) tutor = (string.IsNullOrEmpty(tutor) ? "|" : tutor) + key + "|"; }

        // v0.17: Шаурма-Пасс на 50 уровней с дорожкой Пасс+, время в игре, сотрудники
        public int passVer = 0;
        public bool passPlus = false;
        public int[] passFree = new int[51];   // 1 — награда бесплатной дорожки забрана
        public int[] passPrem = new int[51];   // 1 — награда Пасс+ забрана
        public int bonusSpins = 0;             // запас бесплатных вращений колеса
        public float playSeconds = 0f;         // сколько всего сыграно
        public int[] playClaimed = new int[9];
        public bool goldAwning = false;
        public bool feverSeen = false;         // большое объявление «ЖАРА» показывается один раз
        public long stamp = 0;                 // когда сохранено (UTC ticks) — выбор между облаком и локальным
        public int[] staffHired = new int[12];
        public int[] staffStock = new int[12];
        public long[] staffCash = new long[12];
        public int[] staffSold = new int[12];

        // ежедневный бонус
        public string lastLogin = "";
        public int streak = 0;

        public int Lvl(Up u) { return upgrades[(int)u]; }
        public bool LocOpen(int i) { return i <= location; }
        public int EffDay { get { return GameData.EffDay(day, location); } }
        public LocationDef Loc { get { return GameData.Locations[Mathf.Clamp(location, 0, GameData.Locations.Length - 1)]; } }

        public void Fix()
        {
            upgrades = Resize(upgrades, GameData.Upgrades.Length);
            locUnlocked = Resize(locUnlocked, GameData.Locations.Length);
            achDone = Resize(achDone, Meta.Achievements.Length);
            achTier = Resize(achTier, Meta.Achievements.Length);
            achSeen = Resize(achSeen, Meta.Achievements.Length);
            boosts = Resize(boosts, 3);
            boostOn = Resize(boostOn, 3);
            if (lastLogin == null) lastLogin = "";
            if (wheelDate == null) wheelDate = "";
            biz = Resize(biz, Meta.Businesses.Length);
            if (bizVer < 1) { biz = new int[Meta.Businesses.Length]; bizVer = 1; }
            if (rep < 1f || rep > 5f) rep = 3f;
            if (achVer < 1)
            {
                for (int i = 0; i < achDone.Length; i++) if (achDone[i] == 1) achDone[i] = 2;
                achVer = 1;
            }
            locUnlocked[0] = 1;
            if (location < 0 || location >= GameData.Locations.Length) location = 0;
            perks = Resize(perks, Meta.Perks.Length);
            story = Resize(story, 16);
            skins = Resize(skins, Meta.Skins.Length);
            charms = Resize(charms, Meta.Charms.Length);
            biz = Resize(biz, Meta.Businesses.Length);
            skins[0] = 1;
            if (skin < 0 || skin >= skins.Length || skins[skin] == 0) skin = 0;
            if (day < 1) day = 1;
            if (lang == null) lang = "";
            passFree = Resize(passFree, Meta.PassLevels + 1);
            passPrem = Resize(passPrem, Meta.PassLevels + 1);
            if (passVer < 1)
            {
                for (int i = 1; i <= passClaimed && i < passFree.Length; i++) passFree[i] = 1;
                passVer = 1;
            }
            playClaimed = Resize(playClaimed, Meta.PlayMinutes.Length);
            int nl = GameData.Locations.Length;
            staffHired = Resize(staffHired, nl);
            staffStock = Resize(staffStock, nl);
            staffSold = Resize(staffSold, nl);
            staffCash = ResizeL(staffCash, nl);
            if (playSeconds < 0f || float.IsNaN(playSeconds)) playSeconds = 0f;
        }

        static long[] ResizeL(long[] a, int n)
        {
            if (a != null && a.Length == n) return a;
            var r = new long[n];
            if (a != null) Array.Copy(a, r, Mathf.Min(n, a.Length));
            return r;
        }

        static int[] Resize(int[] a, int n)
        {
            if (a != null && a.Length == n) return a;
            var r = new int[n];
            if (a != null) Array.Copy(a, r, Mathf.Min(n, a.Length));
            return r;
        }
    }

    /// <summary>
    /// Сохранение. Сейчас — PlayerPrefs (в WebGL = IndexedDB браузера).
    /// После подключения SDK Яндекса — облако (см. Platform.cs).
    /// </summary>
    public static class SaveSystem
    {
        const string Key = "vg_shawarma_save_v3"; // v0.4 совместим с v0.3

        /// <summary>Текущее сохранение игры (для выдачи покупок из SDK).</summary>
        public static SaveData Current { get; private set; }

        static SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e) { Debug.LogWarning("Save corrupted: " + e.Message); return null; }
        }

        /// <summary>Локальное сохранение или облачное (Яндекс) — что свежее.</summary>
        public static SaveData Load()
        {
            var local = Parse(Platform.LoadString(Key));
            var cloud = Parse(Platform.CloudJson);
            SaveData data = local;
            if (cloud != null && (local == null || cloud.stamp > local.stamp)) data = cloud;
            if (data == null) data = new SaveData();
            data.Fix();
            Current = data;
            return data;
        }

        public static void Save(SaveData data)
        {
            if (data == null) return;
            data.stamp = DateTime.UtcNow.Ticks;
            Current = data;
            Platform.SaveString(Key, JsonUtility.ToJson(data));
        }

        /// <summary>Сброс прогресса: чистое сохранение (и локально, и в облаке).</summary>
        public static void Reset()
        {
            var fresh = new SaveData();
            fresh.Fix();
            Save(fresh);
            Platform.FlushCloud(true);
        }
    }
}
