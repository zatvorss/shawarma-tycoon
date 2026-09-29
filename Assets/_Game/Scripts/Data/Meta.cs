using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValourGames.Shawarma
{
    // =========================================================================
    //  ЗАДАНИЯ ДНЯ
    // =========================================================================
    public enum QuestType { Serve, ServeType, Perfect, Combo, Drinks, ToGo, NoBurn, Earn }

    public class Quest
    {
        public QuestType type;
        public CustType cust;
        public int target;
        public int progress;
        public int reward;
        public bool done;

        public string Text
        {
            get
            {
                switch (type)
                {
                    case QuestType.Serve:     return L.T("Обслужи клиентов: ", "Serve customers: ") + target;
                    case QuestType.ServeType: return L.T("Обслужи: ", "Serve: ") + GameData.Get(cust).Name + " ×" + target;
                    case QuestType.Perfect:   return L.T("Идеальная прожарка ×", "Perfect grill ×") + target;
                    case QuestType.Combo:     return L.T("Комбо до ", "Reach combo ") + target;
                    case QuestType.Drinks:    return L.T("Продай напитков: ", "Sell drinks: ") + target;
                    case QuestType.ToGo:      return L.T("Заказы с собой: ", "To-go orders: ") + target;
                    case QuestType.NoBurn:    return L.T("Ничего не сжечь за день", "Burn nothing all day");
                    default:                  return L.T("Заработай за день: ", "Earn today: ") + target;
                }
            }
        }

        public string Progress { get { return type == QuestType.NoBurn ? "" : Mathf.Min(progress, target) + "/" + target; } }
    }

    // =========================================================================
    //  ДОСТИЖЕНИЯ
    // =========================================================================
    /// <summary>Достижение с 5 уровнями: цели и награды по уровням.</summary>
    public class AchDef
    {
        public string ru, en, descRu, descEn; // в описании {0} — цель уровня
        public int[] targets, rewards;
        public Func<SaveData, int> value;
        public long scale = 1;   // цели хранятся в тысячах (Путь к миллиарду) — при показе умножаем
        public string Name { get { return L.T(ru, en); } }
        public string Desc(int tier)
        {
            int t = targets[Mathf.Clamp(tier, 0, targets.Length - 1)];
            return string.Format(L.T(descRu, descEn), Fmt(t * scale));
        }
        public static string Fmt(long v) { return v >= 1000 ? GameData.Money(v) : v.ToString(); }
        public const int Tiers = 5;
    }

    // =========================================================================
    //  БУСТЕРЫ
    // =========================================================================
    public class BoostDef
    {
        public Boost id;
        public string ru, en, descRu, descEn;
        public int cost;
        public string Name { get { return L.T(ru, en); } }
        public string Desc { get { return L.T(descRu, descEn); } }
    }

    // =========================================================================
    //  БИЗНЕСЫ (пассивный доход в конце каждого дня)
    // =========================================================================
    /// <summary>
    /// Бизнес — уникальная механика в игровом дне. Открывается на своей локации (каждые 3),
    /// покупается один раз и прокачивается до 5 уровня. Не сбрасывается при переезде.
    /// </summary>
    public class BizDef
    {
        public string ru, en, descRu, descEn;
        public string[] lvRu, lvEn;
        public int unlockLoc;
        public int[] costs;
        public string color;
        public string Name { get { return L.T(ru, en); } }
        public string Desc { get { return L.T(descRu, descEn); } }
        public string LevelDesc(int lvl) { return L.T(lvRu[lvl - 1], lvEn[lvl - 1]); }
        public const int MaxLevel = 5;
        public long Cost(int lvl) { return costs[lvl]; }
    }

    /// <summary>Талисман из Шаурма-Пасса — вечный бонус.</summary>
    public class CharmDef
    {
        public string ru, en, descRu, descEn, color;
        public int passLevel;
        public string Name { get { return L.T(ru, en); } }
        public string Desc { get { return L.T(descRu, descEn); } }
    }

    // =========================================================================
    //  СЛАВА — вечные улучшения за переезды (не сбрасываются)
    // =========================================================================
    public enum Perk { Recipe, Hands, Capital, Word, Calm, Legacy }

    public class PerkDef
    {
        public Perk id;
        public string ru, en, descRu, descEn;
        public int max;
        public string Name { get { return L.T(ru, en); } }
        public string Desc { get { return L.T(descRu, descEn); } }
        public int Cost(int lvl) { return lvl + 1; }
    }

    public class SkinDef
    {
        public string ru, en, a, b;
        public int coins, pass;
        public string Name { get { return L.T(ru, en); } }
    }

    /// <summary>Мета-игра: задания, достижения, бустеры, бизнесы, колесо, бонус, фразы клиентов.</summary>
    public static class Meta
    {
        // ---------------- бустеры ----------------
        public static readonly BoostDef[] Boosts =
        {
            new BoostDef { id = Boost.Speed, ru = "Эспрессо", en = "Espresso", cost = 300,
                descRu = "Режешь вдвое быстрее, гриль и фритюр −20%", descEn = "Cut 2× faster, grill & fryer −20%" },
            new BoostDef { id = Boost.Ads, ru = "Листовки", en = "Flyers", cost = 400,
                descRu = "Больше клиентов (+1 место у окна)", descEn = "More customers (+1 at the window)" },
            new BoostDef { id = Boost.Gold, ru = "Золотой лаваш", en = "Golden lavash", cost = 500,
                descRu = "Все цены ×1.25 на день", descEn = "All prices ×1.25 for the day" },
        };

        /// <summary>Цена бустера растёт вместе с доходами локации.</summary>
        public static int BoostCost(int i, int loc)
        {
            return Mathf.RoundToInt(Boosts[i].cost * GameData.EconScale(loc) / 10f) * 10;
        }

        // ---------------- скины ларька ----------------
        public static readonly SkinDef[] Skins =
        {
            new SkinDef { ru = "Классика", en = "Classic", a = "", b = "#FFFFFF", coins = 0, pass = 0 },
            new SkinDef { ru = "Арбуз", en = "Watermelon", a = "#43A047", b = "#E53935", coins = 0, pass = 5 },
            new SkinDef { ru = "Небо", en = "Sky", a = "#1E88E5", b = "#FFFFFF", coins = 3000, pass = 0 },
            new SkinDef { ru = "Неон", en = "Neon", a = "#FF4FA3", b = "#22E3FF", coins = 0, pass = 10 },
            new SkinDef { ru = "Зебра", en = "Zebra", a = "#212121", b = "#FFFFFF", coins = 6000, pass = 0 },
            new SkinDef { ru = "Золото", en = "Gold", a = "#FFC928", b = "#212121", coins = 0, pass = 20 },
            new SkinDef { ru = "Мята", en = "Mint", a = "#26A69A", b = "#E0F2F1", coins = 10000, pass = 0 },
            new SkinDef { ru = "Радуга", en = "Rainbow", a = "rainbow", b = "#FFFFFF", coins = 0, pass = 30 },
        };

        static readonly string[] Rainbow = { "#E53935", "#FB8C00", "#FDD835", "#43A047", "#1E88E5", "#8E24AA" };

        /// <summary>Цвет i-й полоски навеса с учётом скина.</summary>
        public static Color Stripe(SaveData s, LocationDef loc, int i)
        {
            var d = Skins[0]; // скины убраны: всегда цвет локации
            if (i % 2 == 1) return UIKit.Hex(d.b);
            if (d.a == "rainbow") return UIKit.Hex(Rainbow[(i / 2) % Rainbow.Length]);
            return UIKit.Hex(string.IsNullOrEmpty(d.a) ? loc.kiosk : d.a);
        }

        // ---------------- Шаурма-Пасс: 30 уровней наград ----------------
        public const int PassLevels = 30;
        public static int PassXpFor(int lvl) { return 100 + lvl * 25; }   // опыт на переход lvl -> lvl+1

        public static int PassLevel(int xp)
        {
            int l = 0;
            while (l < PassLevels && xp >= PassXpFor(l)) { xp -= PassXpFor(l); l++; }
            return l;
        }

        public static float PassProgress(int xp)
        {
            int l = 0;
            while (l < PassLevels && xp >= PassXpFor(l)) { xp -= PassXpFor(l); l++; }
            return l >= PassLevels ? 1f : (float)xp / PassXpFor(l);
        }

        /// <summary>Награда за уровень (1..30): 0 монеты, 1 бустер, 2 талисман, 3 очки славы, 4 сундук.</summary>
        public static void PassReward(int lvl, out int type, out int value)
        {
            for (int i = 0; i < Charms.Length; i++) if (Charms[i].passLevel == lvl) { type = 2; value = i; return; }
            if (lvl == 8 || lvl == 18 || lvl == 28) { type = 3; value = 2; return; }
            if (lvl % 4 == 2) { type = 4; value = 1; return; }
            if (lvl % 3 == 0) { type = 1; value = (lvl / 3) % 3; return; }
            type = 0; value = 200 * lvl;
        }

        public static string PassRewardText(int lvl)
        {
            int t, v;
            PassReward(lvl, out t, out v);
            switch (t)
            {
                case 1: return Boosts[v].Name;
                case 2: return Charms[v].Name;
                case 3: return "+" + FamePts(v);
                case 4: return L.T("Сундук", "Chest");
                default: return "+" + v;
            }
        }

        public static void GivePassReward(SaveData s, int lvl)
        {
            int t, v;
            PassReward(lvl, out t, out v);
            if (t == 0) s.money += v;
            else if (t == 1) s.boosts[v]++;
            else if (t == 2) s.charms[v] = 1;
            else if (t == 3) s.fame += v;
            else
            {
                s.money += 300 + lvl * 150;
                s.boosts[UnityEngine.Random.Range(0, 3)]++;
            }
        }

        // ---------------- слава ----------------
        public static readonly PerkDef[] Perks =
        {
            new PerkDef { id = Perk.Recipe,  ru = "Семейный рецепт", en = "Family recipe", descRu = "+10% к цене за уровень", descEn = "+10% price per level", max = 10 },
            new PerkDef { id = Perk.Hands,   ru = "Опытные руки", en = "Skilled hands", descRu = "Нарезка −8%, гриль и фритюр −6% за уровень", descEn = "Cutting −8%, grill & fryer −6% per level", max = 5 },
            new PerkDef { id = Perk.Capital, ru = "Стартовый капитал", en = "Seed money", descRu = "+1000 монет после переезда за уровень", descEn = "+1000 coins after moving per level", max = 10 },
            new PerkDef { id = Perk.Word,    ru = "Сарафанное радио", en = "Word of mouth", descRu = "Клиенты приходят на 6% чаще за уровень", descEn = "Customers come 6% more often per level", max = 5 },
            new PerkDef { id = Perk.Calm,    ru = "Уютная очередь", en = "Cozy queue", descRu = "+7% терпения за уровень", descEn = "+7% patience per level", max = 5 },
            new PerkDef { id = Perk.Legacy,  ru = "Наследство", en = "Legacy", descRu = "При переезде сохраняется 10% монет за уровень", descEn = "Keep 10% coins when moving per level", max = 5 },
        };

        public static int P(SaveData s, Perk p) { return s.perks[(int)p]; }

        // ---------------- бизнесы ----------------
        public const int BizCourier = 0, BizBakery = 1, BizSauce = 2;

        public static readonly BizDef[] Businesses =
        {
            new BizDef { ru = "Курьерская служба", en = "Courier service", color = "#1E88E5", unlockLoc = 2,
                descRu = "Курьеры приезжают за оптовыми заказами: собери несколько одинаковых шаурм с собой — получи крупную оплату.",
                descEn = "Couriers come for bulk orders: make several identical to-go shawarmas and get a big payout.",
                lvRu = new[] { "1 курьер в день", "1 курьер, оплата ×2", "2 курьера в день", "2 курьера, оплата ×4", "3 курьера, оплата ×5" },
                lvEn = new[] { "1 courier a day", "1 courier, pay ×2", "2 couriers a day", "2 couriers, pay ×4", "3 couriers, pay ×5" },
                costs = new[] { 6000, 15000, 40000, 90000, 200000 } },
            new BizDef { ru = "Тандыр-пекарня", en = "Tandoor bakery", color = "#E65100", unlockLoc = 5,
                descRu = "Свой тандыр печёт лаваш прямо в ларьке. Шаурма на горячем лаваше стоит дороже, а соседние кафе приходят покупать его оптом.",
                descEn = "Your own tandoor bakes lavash right in the kiosk. Shawarma on hot lavash costs more, and nearby cafes come to buy it in bulk.",
                lvRu = new[] { "Лаваш за 14 сек, шаурма +20%, 1 оптовик в день", "12 сек, шаурма +25%", "10 сек, +30%, 2 лаваша сразу, 2 оптовика", "8 сек, шаурма +40%", "6 сек, +50%, 3 лаваша сразу" },
                lvEn = new[] { "Lavash in 14 s, shawarma +20%, 1 bulk buyer a day", "12 s, shawarma +25%", "10 s, +30%, 2 at once, 2 buyers", "8 s, shawarma +40%", "6 s, +50%, 3 at once" },
                costs = new[] { 40000, 100000, 250000, 600000, 1500000 } },
            new BizDef { ru = "Фирменный соус", en = "Signature sauce", color = "#C62828", unlockLoc = 8,
                descRu = "Как в лучших шаурмичных: свой соус «Секрет Ашота» в бутылках. Довольные гости просят бутылочку с собой — успей нажать на неё и продать.",
                descEn = "Like the best shawarma places: your own “Ashot's Secret” sauce in bottles. Happy guests ask for a bottle to go — tap it quickly to sell.",
                lvRu = new[] { "Бутылку просит каждый 4-й гость", "Каждый 3-й, бутылка дороже ×1.5", "Каждый 3-й, ×2", "Каждый 2-й, ×2.5", "Каждый 2-й, ×3 и ждут дольше" },
                lvEn = new[] { "Every 4th guest asks for a bottle", "Every 3rd, bottle ×1.5", "Every 3rd, ×2", "Every 2nd, ×2.5", "Every 2nd, ×3, waits longer" },
                costs = new[] { 200000, 500000, 1200000, 3000000, 7000000 } },
        };

        public static int CourierCount(int l) { return l >= 5 ? 3 : (l >= 3 ? 2 : 1); }
        public static float CourierPayMult(int l) { float[] m = { 0f, 1f, 2f, 2f, 4f, 5f }; return m[Mathf.Clamp(l, 0, 5)]; }
        public static float BakeTime(int l) { float[] t = { 99f, 14f, 12f, 10f, 8f, 6f }; return t[Mathf.Clamp(l, 0, 5)]; }
        public static float BakeBonus(int l) { float[] b = { 0f, 0.2f, 0.25f, 0.3f, 0.4f, 0.5f }; return b[Mathf.Clamp(l, 0, 5)]; }
        public static int BakeCap(int l) { return l >= 5 ? 3 : (l >= 3 ? 2 : 1); }
        public const float LavashHot = 14f;
        public static float SauceChance(int l) { return l <= 0 ? 0f : (l >= 4 ? 0.5f : (l >= 2 ? 0.33f : 0.25f)); }
        public static float SauceMult(int l) { float[] m = { 0f, 1f, 1.5f, 2f, 2.5f, 3f }; return m[Mathf.Clamp(l, 0, 5)]; }
        public static float SauceOfferTime(int l) { return l >= 5 ? 4f : 2.8f; }

        // ---------------- талисманы Шаурма-Пасса ----------------
        public const int CharmCoin = 0, CharmBell = 1, CharmBowl = 2, CharmSpatula = 3, CharmMagnet = 4, CharmCrown = 5;

        public static readonly CharmDef[] Charms =
        {
            new CharmDef { ru = "Счастливая монетка", en = "Lucky coin", descRu = "Цены +5% навсегда", descEn = "Prices +5% forever", passLevel = 5, color = "#FFC928" },
            new CharmDef { ru = "Колокольчик", en = "Door bell", descRu = "Терпение клиентов +8%", descEn = "Customer patience +8%", passLevel = 10, color = "#FFB300" },
            new CharmDef { ru = "Кошачья миска", en = "Cat bowl", descRu = "Кот приходит 3 раза и даёт вдвое больше", descEn = "The cat comes 3 times and gives double", passLevel = 15, color = "#F48FB1" },
            new CharmDef { ru = "Золотая лопатка", en = "Golden spatula", descRu = "Окно «идеально» на гриле шире", descEn = "Wider “perfect” grill window", passLevel = 20, color = "#FFD54F" },
            new CharmDef { ru = "Магнит для гостей", en = "Guest magnet", descRu = "Золотые клиенты вдвое чаще", descEn = "Golden customers twice as often", passLevel = 25, color = "#E53935" },
            new CharmDef { ru = "Корона магната", en = "Tycoon crown", descRu = "Все доходы +15% навсегда", descEn = "All income +15% forever", passLevel = 30, color = "#FFC928" },
        };

        public static bool Has(SaveData s, int charm) { return charm < s.charms.Length && s.charms[charm] == 1; }

        /// <summary>"1 очко славы", "3 очка славы", "5 очков славы".</summary>
        public static string FamePts(int n)
        {
            if (L.En) return n + (n == 1 ? " Fame point" : " Fame points");
            int m10 = n % 10, m100 = n % 100;
            string w = (m10 == 1 && m100 != 11) ? "очко" : (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14) ? "очка" : "очков");
            return n + " " + w + " славы";
        }

        // ---------------- колесо удачи ----------------
        // тип: 0 — монеты, 1..3 — бустер (Boost + 1)
        public static readonly int[] WheelType = { 0, 0, 1, 0, 2, 0, 3, 0 };
        public static readonly int[] WheelValue = { 50, 150, 1, 300, 1, 600, 1, 1500 };
        public static readonly string[] WheelColor = { "#E53935", "#FB8C00", "#8E24AA", "#43A047", "#1E88E5", "#F4511E", "#FDD835", "#00897B" };

        public static bool WheelFree(SaveData s) { return s.wheelDate != DateTime.Now.ToString("yyyy-MM-dd"); }

        // ---------------- ежедневный бонус ----------------
        public static readonly int[] DailyRewards = { 50, 100, 150, 250, 400, 600, 1000 };

        /// <summary>Возвращает номер дня серии (0..6), если сегодня бонус ещё не получен, иначе -1.</summary>
        public static int DailyAvailable(SaveData s)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (s.lastLogin == today) return -1;
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            int streak = s.lastLogin == yesterday ? s.streak + 1 : 1;
            return (streak - 1) % DailyRewards.Length;
        }

        public static int ClaimDaily(SaveData s)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            s.streak = s.lastLogin == yesterday ? s.streak + 1 : 1;
            s.lastLogin = today;
            int reward = DailyRewards[(s.streak - 1) % DailyRewards.Length];
            s.money += reward;
            return reward;
        }

        // ---------------- достижения ----------------
        static int OpenLocs(SaveData s)
        {
            int n = 0;
            for (int i = 0; i < GameData.Locations.Length; i++) if (s.LocOpen(i)) n++;
            return n;
        }

        static int AutoStations(SaveData s)
        {
            int n = 0;
            Up[] autos = { Up.Spit, Up.Knife, Up.Fryer, Up.Grill, Up.Sauce, Up.Pack };
            foreach (var u in autos) if (s.upgrades[(int)u] >= 3) n++;
            return n;
        }

        static int BizLevels(SaveData s)
        {
            int n = 0;
            foreach (var b in s.biz) n += b;
            return n;
        }

        static AchDef A(string ru, string en, string dRu, string dEn, int[] t, int[] r, Func<SaveData, int> v)
        {
            return new AchDef { ru = ru, en = en, descRu = dRu, descEn = dEn, targets = t, rewards = r, value = v };
        }

        static AchDef Scaled(AchDef d, long scale) { d.scale = scale; return d; }

        public static readonly AchDef[] Achievements =
        {
            A("Шаурмист", "Shawarmist", "Обслужи {0} клиентов", "Serve {0} customers",
                new[] { 10, 100, 500, 2000, 10000 }, new[] { 100, 500, 2000, 8000, 30000 }, s => s.totalServed),
            A("Мастер гриля", "Grill master", "{0} идеальных прожарок", "{0} perfect grills",
                new[] { 5, 25, 100, 400, 1500 }, new[] { 100, 400, 1500, 6000, 20000 }, s => s.totalPerfect),
            A("Комбо-машина", "Combo machine", "Комбо {0} за день", "Combo of {0} in a day",
                new[] { 3, 6, 10, 15, 25 }, new[] { 100, 300, 1000, 4000, 15000 }, s => s.bestCombo),
            A("Путешественник", "Traveler", "Открой локаций: {0}", "Open {0} locations",
                new[] { 2, 3, 4, 5, 6 }, new[] { 300, 1000, 3000, 10000, 40000 }, OpenLocs),
            A("Любимец критиков", "Critics' darling", "Оценок 5/5 от критика: {0}", "Perfect critic reviews: {0}",
                new[] { 1, 5, 20, 60, 150 }, new[] { 200, 800, 3000, 10000, 40000 }, s => s.criticWins),
            A("Навынос", "Takeaway king", "Заказов с собой: {0}", "To-go orders: {0}",
                new[] { 10, 50, 200, 800, 3000 }, new[] { 100, 400, 1500, 6000, 20000 }, s => s.toGoServed),
            A("Упорство", "Persistence", "Дойди до дня {0}", "Reach day {0}",
                new[] { 3, 7, 14, 25, 40 }, new[] { 150, 600, 2500, 9000, 30000 }, s => s.day),
            A("Звездочёт", "Stargazer", "Собери звёзд: {0}", "Collect {0} stars",
                new[] { 5, 20, 50, 100, 200 }, new[] { 150, 600, 2500, 9000, 30000 }, s => s.stars),
            A("Автоматизация", "Automation", "Автоматических станций: {0}", "Automated stations: {0}",
                new[] { 1, 2, 3, 4, 6 }, new[] { 1000, 3000, 8000, 20000, 60000 }, AutoStations),
            A("Инвестор", "Investor", "Уровней бизнесов: {0}", "Business levels: {0}",
                new[] { 1, 3, 6, 10, 15 }, new[] { 1000, 5000, 20000, 60000, 200000 }, BizLevels),
            Scaled(A("Путь к миллиарду", "Road to a billion", "Заработай за всё время {0}", "Earn {0} in total",
                new[] { 10, 100, 1000, 10000, 1000000 }, new[] { 500, 3000, 20000, 150000, 1000000 },
                s => (int)System.Math.Min(s.lifetime / 1000L, int.MaxValue)), 1000),
            A("Курьерская служба", "Courier service", "Заказов для бизнесов: {0}", "Business orders: {0}",
                new[] { 1, 10, 50, 150, 500 }, new[] { 200, 1000, 5000, 15000, 50000 }, s => s.couriers),
            A("Друг котов", "Cat friend", "Погладь кота {0} раз", "Pet the cat {0} times",
                new[] { 1, 10, 30, 100, 300 }, new[] { 50, 300, 1000, 4000, 12000 }, s => s.cats),
            A("Особые гости", "Special guests", "Обслужи особенных гостей ★: {0}", "Serve special guests ★: {0}",
                new[] { 5, 25, 100, 300, 1000 }, new[] { 150, 700, 3000, 10000, 35000 }, s => s.specials),
        };

        public static int MaxTiers { get { return Achievements.Length * AchDef.Tiers; } }

        /// <summary>Можно ли забрать следующий уровень достижения.</summary>
        public static bool Ready(SaveData s, int i)
        {
            int tier = s.achTier[i];
            if (tier >= AchDef.Tiers) return false;
            return Achievements[i].value(s) >= Achievements[i].targets[tier];
        }

        public static int ReadyCount(SaveData s)
        {
            int n = 0;
            for (int i = 0; i < Achievements.Length; i++) if (Ready(s, i)) n++;
            return n;
        }

        public static int TiersClaimed(SaveData s)
        {
            int n = 0;
            foreach (var t in s.achTier) n += t;
            return n;
        }

        /// <summary>
        /// Проверка новых выполненных уровней (для уведомлений). Возвращает строки "Название — ур. N".
        /// </summary>
        public static List<string> CheckAchievements(SaveData s)
        {
            var list = new List<string>();
            for (int i = 0; i < Achievements.Length; i++)
            {
                var a = Achievements[i];
                int v = a.value(s);
                while (s.achSeen[i] < AchDef.Tiers && v >= a.targets[s.achSeen[i]])
                {
                    s.achSeen[i]++;
                    list.Add(a.Name + " — " + L.T("уровень ", "level ") + s.achSeen[i]);
                }
            }
            return list;
        }

        /// <summary>Забрать награду за следующий уровень.</summary>
        public static int Claim(SaveData s, int i)
        {
            if (!Ready(s, i)) return 0;
            int reward = Achievements[i].rewards[s.achTier[i]];
            s.achTier[i]++;
            s.money += reward;
            return reward;
        }

        // ---------------- задания ----------------
        public static List<Quest> MakeQuests(int day, LocationDef loc)
        {
            var rnd = new System.Random(day * 7919 + loc.id * 31);
            var pool = new List<QuestType> { QuestType.Serve, QuestType.Perfect, QuestType.Combo, QuestType.ToGo, QuestType.NoBurn, QuestType.Earn };
            if (day >= 2) pool.Add(QuestType.ServeType);
            if (GameData.Unlocked(Drink.Cola, day)) pool.Add(QuestType.Drinks);

            var result = new List<Quest>();
            int reward = 60 + day * 15;
            while (result.Count < 3 && pool.Count > 0)
            {
                int pi = rnd.Next(pool.Count);
                var t = pool[pi];
                pool.RemoveAt(pi);
                var q = new Quest { type = t, reward = Mathf.RoundToInt(reward * loc.price / 10f) * 10 };
                switch (t)
                {
                    case QuestType.Serve: q.target = 4 + day; break;
                    case QuestType.Perfect: q.target = 2 + day / 3; break;
                    case QuestType.Combo: q.target = 3 + day / 3; break;
                    case QuestType.ToGo: q.target = 2 + day / 2; break;
                    case QuestType.NoBurn: q.target = 1; break;
                    case QuestType.Drinks: q.target = 2 + day / 3; break;
                    case QuestType.Earn: q.target = Mathf.RoundToInt(GameData.Goal(day, loc) * 1.3f / 10f) * 10; break;
                    case QuestType.ServeType:
                        var types = new List<CustType>();
                        foreach (var c in GameData.Customers)
                            if (c.unlockDay <= day && GameData.Weight(loc, c.id) >= 10) types.Add(c.id);
                        if (types.Count == 0) types.Add(CustType.Regular);
                        q.cust = types[rnd.Next(types.Count)];
                        q.target = 2 + day / 4;
                        break;
                }
                result.Add(q);
            }
            return result;
        }

        // ---------------- фразы клиентов ----------------
        static readonly string[][] HelloRu =
        {
            new[] { "Одну шаурму, пожалуйста!", "Пахнет вкусно!", "Мне как обычно", "Можно побыстрее?" },
            new[] { "А скидка студентам есть?", "Последние деньги...", "Сессия, надо поесть!", "Мне побольше!" },
            new[] { "Быстрее, у меня созвон!", "Время — деньги!", "Сдачи не надо", "У меня 5 минут" },
            new[] { "Внучок, мне как обычно", "Ой, а что это такое?", "В моё время было дешевле", "Только не острое!" },
            new[] { "Всем привет, я в эфире!", "Снимаю для подписчиков!", "Ставьте лайки!", "Это будет вирусно" },
            new[] { "Посмотрим, что вы умеете...", "Хм. Интересно.", "Я пишу для журнала", "Удивите меня" },
            new[] { "Мне белка побольше!", "Только с тренировки!", "Двойное мясо, как обычно", "Быстрее, у меня разминка" },
            new[] { "Hello! Шаурма, please!", "Я фоткаю всё подряд!", "У вас так вкусно пахнет", "Это местный деликатес?" },
            new[] { "Смена была тяжёлая...", "Чтоб сытно было!", "Мне как бригадиру", "Побольше мяса, хозяин" },
            new[] { "А с чем самая дешёвая?", "Мама дала на обед!", "Можно без лука?", "Быстрее, звонок!" },
            new[] { "Одну с собой, я на патруле", "Документы... шучу, шаурму!", "Всё по закону, без острого", "Быстро и чётко!" },
            new[] { "У меня пять минут между операциями", "Руки мыли? Отлично!", "Врач прописал шаурму", "Главное — свежесть" },
            new[] { "Если что — потушу!", "После смены голодный как волк", "Горячее — это по моей части", "Двойное, пожалуйста" },
            new[] { "Рок-н-ролл и шаурма!", "Сделай погромче... то есть посочнее", "Йе-е-е!", "Мне с луком, конечно" },
            new[] { "Я на паузе, быстрее!", "Шаурма даёт +10 к силе", "GG, давай заказ", "Катка через 5 минут" },
            new[] { "А лаваш у вас крафтовый?", "Я ел шаурму, когда это не было мейнстримом", "Мне на альтернативном соусе", "Сфоткаю для блога" },
            new[] { "Счётчик тикает!", "Заказ с собой, я на смене", "Шеф, побыстрее", "Клиент в машине ждёт" },
            new[] { "Эх, молодёжь...", "Мне как в 70-е", "Помню, шаурма была по рублю", "Не торопись, внучок" },
            new[] { "Клевало отлично!", "Рыбу не продаёте? Ну ладно", "Мне бы посытнее", "С утра на озере" },
            new[] { "Волна была огонь!", "Чувак, шаурмы хочу", "Алоха!", "Можно с собой на пляж?" },
            new[] { "У меня рейс через 20 минут", "Экипаж голоден!", "Мягкой посадки моей шаурме", "Внимание, заказ!" },
            new[] { "Для экипажа, пожалуйста", "Пристегните шаурму!", "Время вылета поджимает", "Спасибо за полёт... то есть еду" },
            new[] { "Наши сегодня выиграют!", "Го-о-ол!", "Шаурму до перерыва!", "Оле-оле-оле!" },
            new[] { "Ха-ха! Кто заказал смех?", "Шаурма с сюрпризом?", "Бип-бип!", "Улыбнитесь!" },
            new[] { "Сыграю, пока жду", "Это песня про шаурму", "Музыка за шаурму!", "Послушайте!" },
            new[] { "Мне ДВЕ! Одинаковые!", "Одной мало...", "Я очень голодный", "Двойная порция счастья" },
            new[] { "Санитарная проверка!", "Покажите руки. Хм.", "Где журнал уборки?", "Проверим вашу доску" },
            new[] { "А подешевле?", "Давай скидку, брат!", "На рынке дешевле было", "Торг уместен?" },
            new[] { "Только без мяса!", "Я веган, запомните", "Побольше овощей", "Мясо — ни кусочка!" },
            new[] { "Самую лучшую шаурму!", "Деньги не проблема", "Удиви меня, друг", "Золотой лаваш есть?" },
        };
        static readonly string[][] HelloEn =
        {
            new[] { "One shawarma, please!", "Smells great!", "The usual", "Could you hurry?" },
            new[] { "Student discount?", "My last coins...", "Exam week, need food!", "Make it big!" },
            new[] { "Hurry, I have a call!", "Time is money!", "Keep the change", "I have 5 minutes" },
            new[] { "Dear, the usual please", "Oh, what's this?", "It used to be cheaper", "Nothing spicy!" },
            new[] { "Hi chat, I'm live!", "Filming for my followers!", "Smash that like!", "This will go viral" },
            new[] { "Let's see what you've got...", "Hm. Interesting.", "I write for a magazine", "Surprise me" },
            new[] { "Extra protein please!", "Straight from the gym!", "Double meat, as usual", "Hurry, warm-up soon" },
            new[] { "Hello! Shawarma, please!", "I photograph everything!", "Smells amazing here", "Is this a local delicacy?" },
            new[] { "Tough shift today...", "Make it filling!", "One for the foreman", "More meat, boss" },
            new[] { "What's the cheapest?", "Mom gave me lunch money!", "No onions please?", "Hurry, the bell!" },
            new[] { "One to go, I'm on patrol", "License... kidding, shawarma!", "By the book, nothing spicy", "Quick and clean!" },
            new[] { "Five minutes between surgeries", "Washed hands? Good!", "Doctor's orders: shawarma", "Freshness first" },
            new[] { "If anything burns — I've got it!", "Hungry after my shift", "Hot stuff is my job", "Double, please" },
            new[] { "Rock'n'roll and shawarma!", "Turn it up... I mean juicier", "Yeah-h-h!", "Onion, of course" },
            new[] { "I'm on pause, hurry!", "Shawarma gives +10 strength", "GG, take my order", "Match in 5 minutes" },
            new[] { "Is your lavash craft?", "I ate shawarma before it was cool", "Alternative sauce, please", "Posting this on my blog" },
            new[] { "The meter's running!", "To go, I'm on shift", "Boss, hurry up", "My passenger is waiting" },
            new[] { "Ah, young people...", "Make it like in the 70s", "Shawarma used to cost a coin", "Take your time, kid" },
            new[] { "The fish were biting!", "No fish? Fine", "Something filling", "Been on the lake since dawn" },
            new[] { "Epic waves today!", "Dude, I want shawarma", "Aloha!", "Can I take it to the beach?" },
            new[] { "My flight leaves in 20", "The crew is hungry!", "Smooth landing for my shawarma", "Attention, order!" },
            new[] { "For the crew, please", "Fasten your shawarma!", "Departure is soon", "Thanks for flying... I mean the food" },
            new[] { "We'll win today!", "Go-o-oal!", "Shawarma before halftime!", "Ole-ole-ole!" },
            new[] { "Ha-ha! Who ordered a laugh?", "Shawarma with a surprise?", "Honk-honk!", "Smile!" },
            new[] { "I'll play while I wait", "This song is about shawarma", "Music for shawarma!", "Listen!" },
            new[] { "TWO! Identical ones!", "One is not enough...", "I'm sooo hungry", "Double portion of happiness" },
            new[] { "Health inspection!", "Show me your hands. Hm.", "Where's the cleaning log?", "Let's check your board" },
            new[] { "Any cheaper?", "Give me a discount!", "It was cheaper at the market", "Can we haggle?" },
            new[] { "No meat please!", "I'm vegan, remember", "Extra veggies", "Not a single piece of meat!" },
            new[] { "Your very best shawarma!", "Money is no problem", "Impress me, friend", "Is there a golden lavash?" },
        };

        public static string Hello(CustType t)
        {
            var arr = (L.En ? HelloEn : HelloRu)[(int)t];
            return arr[UnityEngine.Random.Range(0, arr.Length)];
        }

        public static string ModeLine(ServeMode m)
        {
            if (m == ServeMode.Here)
                return UnityEngine.Random.value < 0.5f ? L.T("Здесь поем!", "I'll eat here!") : L.T("Здесь, пожалуйста", "For here, please");
            return UnityEngine.Random.value < 0.5f ? L.T("С собой!", "To go!") : L.T("Заверните с собой", "Pack it to go");
        }
    }
}
