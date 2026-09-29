using System.Collections.Generic;
using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>Ингредиенты. Порядок = индекс в массивах запасов и заказов.</summary>
    public enum Ing { Meat, Tomato, Cucumber, Cabbage, Cheese, Jalapeno, Fries, Garlic, Spicy }

    /// <summary>Как пополняется запас ингредиента.</summary>
    public enum Restock { Spit, Chop, Fryer, Sauce }

    public enum Drink { None, Cola, Ayran, Fries } // Fries — картошка фри в коробке (отдельный заказ)

    public enum CustType
    {
        Regular, Student, Business, Granny, Blogger, Critic, Athlete, Tourist, Builder, Kid,
        Police, Doctor, Fireman, Rocker, Gamer, Hipster, Taxi, Grandpa, Fisher, Surfer,
        Pilot, Stewardess, Fan, Clown, Musician, Glutton, Inspector, Haggler, Vegan, Sheikh
    }

    /// <summary>Улучшения. Порядок = индекс в SaveData.upgrades. Уровни 0..3 (в игре "Ур. 1..4").</summary>
    public enum Up { Spit, Knife, Fryer, Grill, Sauce, Bins, Patience, Tips, Price, Pack, Flies, Board, Fire, Queue, Grills, FryBox, VegFeed, FryLoad, FryUnload }

    /// <summary>Где будут есть: здесь (обёртка) или с собой (обёртка + пакет).</summary>
    public enum ServeMode { Here, ToGo }

    /// <summary>Бустеры на один день.</summary>
    public enum Boost { Speed, Ads, Gold }

    public enum DayEvent { None, Rain, Rush }

    public class IngredientDef
    {
        public Ing id;
        public string ru, en, shortRu, shortEn;
        public Color color;
        public int price;
        public int unlockDay;
        public Restock restock;

        public string Name { get { return L.T(ru, en); } }
        public string Short { get { return L.T(shortRu, shortEn); } }

        public IngredientDef(Ing id, string ru, string en, string shortRu, string shortEn,
            string hex, int price, int unlockDay, Restock restock)
        {
            this.id = id; this.ru = ru; this.en = en; this.shortRu = shortRu; this.shortEn = shortEn;
            color = UIKit.Hex(hex);
            this.price = price; this.unlockDay = unlockDay; this.restock = restock;
        }
    }

    public class DrinkDef
    {
        public Drink id;
        public string ru, en;
        public int price;
        public int unlockDay;
        public string Name { get { return L.T(ru, en); } }

        public DrinkDef(Drink id, string ru, string en, int price, int unlockDay)
        {
            this.id = id; this.ru = ru; this.en = en; this.price = price; this.unlockDay = unlockDay;
        }
    }

    public class CustDef
    {
        public CustType id;
        public string ru, en;
        public float patienceMult, payMult, tipMult;
        public int unlockDay;
        public string Name { get { return L.T(ru, en); } }
        /// <summary>Для новых персонажей: базовый вес появления и любимые локации (там ×4).</summary>
        public int w;
        public int[] favLocs;
        /// <summary>Особенный персонаж — со своей механикой (описание — note).</summary>
        public bool special;
        public string noteRu, noteEn;
        public string Note { get { return L.T(noteRu, noteEn); } }

        public CustDef(CustType id, string ru, string en, float patienceMult, float payMult, float tipMult, int unlockDay)
        {
            this.id = id; this.ru = ru; this.en = en;
            this.patienceMult = patienceMult; this.payMult = payMult; this.tipMult = tipMult;
            this.unlockDay = unlockDay;
        }
    }

    /// <summary>Улучшение: 4 уровня (0..3), описание каждого уровня и цена перехода на следующий.</summary>
    public class UpgradeDef
    {
        public Up id;
        public string ru, en;
        public string[] lvRu, lvEn;
        public int[] costs;

        public string Name { get { return L.T(ru, en); } }
        public int MaxLevel { get { return costs.Length; } }
        public string LevelDesc(int lvl) { return L.T(lvRu[lvl], lvEn[lvl]); }
        public int Cost(int lvl) { return costs[lvl]; }
        /// <summary>Для особых улучшений (Грили): на какой локации (индекс) доступен переход на уровень lvl+1. null — общий потолок.</summary>
        public int[] reqLoc;

        /// <summary>
        /// Потолок уровня на локации (сколько переходов можно купить):
        /// 1-я локация — до ур. 2, 2–3-я — до ур. 3, 4–6-я — до ур. 4, с 7-й — ур. 5 «Легенда».
        /// </summary>
        public static int CapAt(int loc) { return loc <= 0 ? 1 : (loc <= 2 ? 2 : (loc <= 5 ? 3 : 4)); }

        /// <summary>Индекс локации, где откроется переход на уровень lvl+1.</summary>
        public int LocFor(int lvl)
        {
            if (reqLoc != null) return reqLoc[lvl];
            if (lvl <= 0) return 0;
            if (lvl == 1) return 1;
            if (lvl == 2) return 3;
            return 6;
        }

        public bool CanReach(int lvl, int loc) { return lvl < MaxLevel && loc >= LocFor(lvl); }

        public UpgradeDef(Up id, string ru, string en, string[] lvRu, string[] lvEn, int[] costs)
        {
            this.id = id; this.ru = ru; this.en = en; this.lvRu = lvRu; this.lvEn = lvEn; this.costs = costs;
        }
    }

    /// <summary>Локация на карте: свои цены, темп, клиенты и оформление.</summary>
    public class LocationDef
    {
        public int id;
        public string ru, en, descRu, descEn;
        public int minDay, cost;
        public float price, patience, spawn, drink;
        public string skyTop, skyBottom, ground, kiosk;
        public bool night;
        public int[] weights; // по CustType
        public float mapX, mapY;
        public string scene = "city";  // city, market, beach, stadium, airport, mall, towers

        public string Name { get { return L.T(ru, en); } }
        public string Desc { get { return L.T(descRu, descEn); } }
    }

    /// <summary>
    /// ВЕСЬ БАЛАНС ИГРЫ ТУТ. Цены, время, дни открытия, локации — меняй здесь.
    /// </summary>
    public static class GameData
    {
        public static string Title { get { return L.T("Шаурма Магнат", "Shawarma Tycoon"); } }
        public static string Subtitle { get { return L.T("Путь к миллиарду", "Road to a Billion"); } }
        public static string FullTitle { get { return Title + ": " + Subtitle; } }

        public const int LavashPrice = 40;
        public const int MaxSlots = 4;
        public const int TrayCapacity = 3;
        public const float ModeKnownChance = 0.5f;
        public const float SwipePerPiece = 90f;   // сколько "провести ножом" на один кусок
        public const float CutCooldown = 0.5f;    // не чаще 2 кусков в секунду
        public const float NewSpitTime = 3.5f;
        public const float AutoCutInterval = 2.5f;
        public const float AutoChopInterval = 5f;
        public const int AutoChopAmount = 2;
        public const float AutoSauceInterval = 1.2f;
        public const float PerfectWindow = 0.6f;  // первые 40% окна "готово" = идеальная прожарка

        public static readonly IngredientDef[] Ingredients =
        {
            new IngredientDef(Ing.Meat,     "Мясо",       "Meat",        "Мясо",    "Meat",    "#B5542D", 60, 1,  Restock.Spit),
            new IngredientDef(Ing.Tomato,   "Помидор",    "Tomato",      "Помидор", "Tomato",  "#E0413A", 15, 1,  Restock.Chop),
            new IngredientDef(Ing.Cucumber, "Огурец",     "Cucumber",    "Огурец",  "Cucumb.", "#3E9B3E", 15, 2,  Restock.Chop),
            new IngredientDef(Ing.Cabbage,  "Капуста",    "Cabbage",     "Капуста", "Cabbage", "#8DBF4A", 10, 5,  Restock.Chop),
            new IngredientDef(Ing.Cheese,   "Сыр",        "Cheese",      "Сыр",     "Cheese",  "#F2C94C", 30, 6,  Restock.Chop),
            new IngredientDef(Ing.Jalapeno, "Халапеньо",  "Jalapeno",    "Халап.",  "Jalap.",  "#2E7D32", 20, 10, Restock.Chop),
            new IngredientDef(Ing.Fries,    "Картошка",   "Fries",       "Картоф.", "Fries",   "#E8B321", 25, 3,  Restock.Fryer),
            new IngredientDef(Ing.Garlic,   "Чесн. соус", "Garlic sauce","Чесн.",   "Garlic",  "#E9E1C8", 10, 1,  Restock.Sauce),
            new IngredientDef(Ing.Spicy,    "Остр. соус", "Hot sauce",   "Острый",  "Hot",     "#C62828", 15, 7,  Restock.Sauce),
        };

        public static readonly DrinkDef[] Drinks =
        {
            null, // Drink.None
            new DrinkDef(Drink.Cola,  "Кола",  "Cola",  50, 4),
            new DrinkDef(Drink.Ayran, "Айран", "Ayran", 40, 8),
            new DrinkDef(Drink.Fries, "Картошка фри", "Fries box", 60, 3),
        };

        public static readonly CustDef[] Customers =
        {
            //                id                 RU           EN            терп. плата чаев. день
            new CustDef(CustType.Regular,  "Прохожий",  "Passer-by",   1.0f, 1.0f, 1.0f, 1),
            new CustDef(CustType.Student,  "Студент",   "Student",     1.4f, 0.8f, 0.5f, 2),
            new CustDef(CustType.Business, "Бизнесмен", "Businessman", 0.6f, 1.5f, 2.0f, 3),
            new CustDef(CustType.Granny,   "Бабушка",   "Granny",      1.3f, 0.9f, 1.2f, 5),
            new CustDef(CustType.Blogger,  "Блогер",    "Blogger",     0.9f, 1.0f, 1.0f, 6),
            new CustDef(CustType.Critic,   "Критик",    "Food critic", 0.8f, 2.5f, 3.0f, 9),
            new CustDef(CustType.Athlete,  "Спортсмен", "Athlete",     0.7f, 1.2f, 1.0f, 4),
            new CustDef(CustType.Tourist,  "Турист",    "Tourist",     1.2f, 1.4f, 1.8f, 3),
            new CustDef(CustType.Builder,  "Строитель", "Builder",     1.3f, 1.3f, 0.8f, 5),
            new CustDef(CustType.Kid,      "Школьник",  "Schoolkid",   0.8f, 0.6f, 0.3f, 2),
            // --- новые обычные ---
            N(CustType.Police,     "Полицейский", "Police officer", 0.9f, 1.1f, 1.0f, 3, 3, 2, 7),
            N(CustType.Doctor,     "Врач",        "Doctor",         0.7f, 1.3f, 1.2f, 4, 3, 4, 9),
            N(CustType.Rocker,     "Рокер",       "Rocker",         0.9f, 1.1f, 1.3f, 5, 3, 7),
            N(CustType.Gamer,      "Геймер",      "Gamer",          1.3f, 0.9f, 0.6f, 3, 3, 4, 8),
            N(CustType.Hipster,    "Хипстер",     "Hipster",        1.0f, 1.3f, 1.4f, 5, 3, 7, 8),
            N(CustType.Taxi,       "Таксист",     "Taxi driver",    0.6f, 1.2f, 1.6f, 4, 3, 2, 6),
            N(CustType.Grandpa,    "Дедушка",     "Grandpa",        1.5f, 0.9f, 1.0f, 2, 3, 0, 1),
            N(CustType.Fisher,     "Рыбак",       "Fisherman",      1.2f, 1.0f, 1.0f, 6, 2, 3),
            N(CustType.Surfer,     "Сёрфер",      "Surfer",         1.2f, 1.1f, 1.2f, 7, 2, 3),
            N(CustType.Pilot,      "Пилот",       "Pilot",          0.7f, 1.6f, 1.8f, 10, 2, 6),
            N(CustType.Stewardess, "Стюардесса",  "Flight attendant", 0.8f, 1.4f, 1.5f, 10, 2, 6),
            N(CustType.Fan,        "Болельщик",   "Sports fan",     0.9f, 1.1f, 0.9f, 7, 3, 5),
            N(CustType.Haggler,    "Торгаш",      "Haggler",        1.5f, 0.7f, 0.3f, 2, 2, 1),
            N(CustType.Sheikh,     "Шейх",        "Sheikh",         1.0f, 2.2f, 3.0f, 14, 1, 6, 9),
            // --- новые ОСОБЕННЫЕ (со своей механикой) ---
            S(N(CustType.Fireman,  "Пожарный",    "Firefighter",    1.0f, 1.1f, 1.0f, 6, 2, 5),
                "Пока он у окна — любой пожар на гриле гаснет сам", "While he's at the window, grill fires go out by themselves"),
            S(N(CustType.Clown,    "Клоун",       "Clown",          1.2f, 1.0f, 1.2f, 8, 2, 1, 8),
                "Рассмешил очередь: всем клиентам +25% терпения", "Made everyone laugh: all customers get +25% patience"),
            S(N(CustType.Musician, "Музыкант",    "Street musician", 1.4f, 0.8f, 0.5f, 6, 2, 1, 7),
                "Пока играет — остальные клиенты теряют терпение медленнее", "While he plays, other customers lose patience slower"),
            S(N(CustType.Glutton,  "Обжора",      "Glutton",        1.3f, 1.0f, 1.0f, 5, 2),
                "Берёт ДВЕ одинаковые шаурмы и платит за обе", "Takes TWO identical shawarmas and pays for both"),
            S(N(CustType.Inspector, "Санинспектор", "Health inspector", 0.9f, 1.0f, 0f, 9, 1),
                "Проверяет чистоту: грязь, муха или пожар — штраф, чисто — премия", "Checks hygiene: dirt, a fly or fire — a fine; clean — a bonus"),
            S(N(CustType.Vegan,    "Веган",       "Vegan",          1.0f, 1.2f, 1.2f, 6, 2, 4, 7),
                "Шаурма БЕЗ мяса — не перепутай!", "Shawarma with NO meat — don't mix it up!"),
        };

        static CustDef N(CustType id, string ru, string en, float pat, float pay, float tip, int day, int w, params int[] fav)
        {
            return new CustDef(id, ru, en, pat, pay, tip, day) { w = w, favLocs = fav };
        }

        static CustDef S(CustDef d, string noteRu, string noteEn)
        {
            d.special = true;
            d.noteRu = noteRu;
            d.noteEn = noteEn;
            return d;
        }

        /// <summary>Вес появления клиента на локации.</summary>
        public static int Weight(LocationDef loc, CustType t)
        {
            int i = (int)t;
            if (i < loc.weights.Length) return loc.weights[i];
            var d = Get(t);
            if (d.favLocs != null)
                foreach (var l in d.favLocs) if (l == loc.id) return d.w * 4;
            return d.w;
        }

        static UpgradeDef U(Up id, string ru, string en, string[] lr, string[] le, int[] c) { return new UpgradeDef(id, ru, en, lr, le, c); }

        // 5 уровней: ур.1 (старт) → ур.2 → ур.3 → ур.4 (автоматика) → ур.5 «Легенда» (с 7-й локации).
        // Цены умножаются на цены локации (см. Game.UpCost).
        public static readonly UpgradeDef[] Upgrades =
        {
            U(Up.Spit, "Вертел", "Spit",
                new[] { "Порция мяса — 3 взмаха ножом", "Порция — 2 взмаха", "Порция — 1 взмах или касание", "АВТО-нарезчик срезает мясо сам", "ЛЕГЕНДА: нарезчик вдвое быстрее" },
                new[] { "A meat portion takes 3 knife swipes", "Portion — 2 swipes", "Portion — 1 swipe or tap", "AUTO-cutter slices meat by itself", "LEGEND: cutter twice as fast" },
                new[] { 600, 3500, 16000, 60000 }),
            U(Up.Knife, "Нож и доска", "Knife & board",
                new[] { "Овощ рубится за 6 ударов", "За 3 удара", "За 1 удар", "АВТО-нож рубит сам всё, что лежит на доске", "ЛЕГЕНДА: авто-нож вдвое быстрее" },
                new[] { "A veggie takes 6 chops", "3 chops", "1 chop", "AUTO-knife chops whatever is on the board", "LEGEND: auto-knife twice as fast" },
                new[] { 600, 3500, 16000, 60000 }),
            U(Up.Fryer, "Фритюр", "Fryer",
                new[] { "Жарит 9 сек, выходит 3 порции", "Жарит 6 сек, 4 порции", "Жарит 4 сек, 6 порций", "Термостат: картошка больше не подгорает", "ЛЕГЕНДА: жарит на 40% быстрее" },
                new[] { "Fries in 9 s, 3 portions", "6 s, 4 portions", "4 s, 6 portions", "Thermostat: fries never burn", "LEGEND: 40% faster" },
                new[] { 900, 5000, 22000, 80000 }),
            U(Up.Grill, "Гриль", "Grill",
                new[] { "Жарит 6 сек", "Жарит 4.5 сек", "Жарит 3.5 сек", "АВТО-таймер: сам переворачивает и снимает", "ЛЕГЕНДА: на 30% быстрее" },
                new[] { "Grills 6 s", "Grills 4.5 s", "Grills 3.5 s", "AUTO-timer: flips and takes off", "LEGEND: 30% faster" },
                new[] { 1200, 6500, 28000, 100000 }),
            U(Up.Sauce, "Соусы", "Sauces",
                new[] { "Бутылка на 6", "Бутылка на 10", "Бутылка на 14", "АВТО-дозатор", "ЛЕГЕНДА: дозатор ×2 быстрее" },
                new[] { "Bottle of 6", "Bottle of 10", "Bottle of 14", "AUTO-dispenser", "LEGEND: dispenser 2× faster" },
                new[] { 500, 2800, 13000, 50000 }),
            U(Up.Bins, "Лотки", "Bins",
                new[] { "4 порции в лотке", "6 порций", "8 порций", "10 порций", "ЛЕГЕНДА: 14 порций" },
                new[] { "4 per bin", "6 per bin", "8 per bin", "10 per bin", "LEGEND: 14 per bin" },
                new[] { 800, 4500, 18000, 70000 }),
            U(Up.Patience, "Уют", "Coziness",
                new[] { "Терпение ×1", "Терпение ×1.15", "Терпение ×1.3", "Терпение ×1.5", "ЛЕГЕНДА: терпение ×1.8" },
                new[] { "Patience ×1", "Patience ×1.15", "Patience ×1.3", "Patience ×1.5", "LEGEND: patience ×1.8" },
                new[] { 1000, 5500, 24000, 90000 }),
            U(Up.Tips, "Сервис", "Service",
                new[] { "Чаевые ×1", "Чаевые ×1.3", "Чаевые ×1.6", "Чаевые ×2 + касса сама спрашивает «здесь или с собой»", "ЛЕГЕНДА: чаевые ×2.5" },
                new[] { "Tips ×1", "Tips ×1.3", "Tips ×1.6", "Tips ×2 + auto-ask “here or to go”", "LEGEND: tips ×2.5" },
                new[] { 1200, 6500, 28000, 100000 }),
            U(Up.Price, "Рецепт", "Recipe",
                new[] { "Цена ×1", "Цена ×1.15", "Цена ×1.3", "Цена ×1.5", "ЛЕГЕНДА: цена ×1.8" },
                new[] { "Price ×1", "Price ×1.15", "Price ×1.3", "Price ×1.5", "LEGEND: price ×1.8" },
                new[] { 2000, 10000, 40000, 150000 }),
            U(Up.Pack, "Упаковка", "Packing",
                new[] { "3 касания на обёртку/пакет", "2 касания", "1 касание", "АВТО-упаковщик (обёртка сама)", "ЛЕГЕНДА: пакует всё разом" },
                new[] { "3 taps per wrap/bag", "2 taps", "1 tap", "AUTO-packer (wraps by itself)", "LEGEND: packs everything at once" },
                new[] { 500, 2800, 13000, 50000 }),
            U(Up.Flies, "Мухи", "Flies",
                new[] { "Муху бить 3 раза", "Мухобойка: 1 удар", "Венерина мухоловка: ловит муху раз в 30 сек", "УФ-ловушка: мухи сгорают сами", "ЛЕГЕНДА: за каждую муху — монеты" },
                new[] { "3 hits per fly", "Swatter: 1 hit", "Venus flytrap: a fly every 30 s", "UV zapper: flies burn by themselves", "LEGEND: coins for every fly" },
                new[] { 600, 3000, 14000, 50000 }),
            U(Up.Board, "Чистота доски", "Clean board",
                new[] { "Грязнится каждые 4 шаурмы", "Губка: трёшь вдвое быстрее", "Антипригарная: грязнится вдвое реже", "Робо-губка: чистит сама", "ЛЕГЕНДА: доска не пачкается" },
                new[] { "Dirty every 4 wraps", "Sponge: wipe 2× faster", "Non-stick: dirty 2× less", "Robo-sponge: cleans itself", "LEGEND: never gets dirty" },
                new[] { 500, 2500, 12000, 45000 }),
            U(Up.Fire, "Пожарная безопасность", "Fire safety",
                new[] { "Тушить 3 нажатиями", "Огнетушитель: 1 нажатие", "Спринклер: тушит сам за 3 сек", "Датчик дыма: пожаров не бывает", "ЛЕГЕНДА: сгоревшее убирается само" },
                new[] { "3 taps to put out", "Extinguisher: 1 tap", "Sprinkler: auto in 3 s", "Smoke sensor: no fires", "LEGEND: burnt food clears itself" },
                new[] { 500, 2500, 12000, 45000 }),
            U(Up.Queue, "Уют для очереди", "Queue comfort",
                new[] { "Очередь ждёт недолго", "Навес: ждут ×1.4", "Лавочка: ждут ×1.8, очередь до 8", "Кофе в очереди: ждут ×2.5", "ЛЕГЕНДА: ждут ×3.5, очередь до 10" },
                new[] { "Queue is impatient", "Awning: wait ×1.4", "Bench: wait ×1.8, queue up to 8", "Free coffee: wait ×2.5", "LEGEND: wait ×3.5, queue up to 10" },
                new[] { 700, 3500, 15000, 55000 }),
            new UpgradeDef(Up.Grills, "Грили", "Grills",
                new[] { "1 гриль", "2 гриля (с 4-й локации)", "3 гриля (с 8-й локации)" },
                new[] { "1 grill", "2 grills (from location 4)", "3 grills (from location 8)" },
                new[] { 4000, 25000 }) { reqLoc = new[] { 3, 7 } },
            U(Up.FryBox, "Коробки фри", "Fries boxes",
                new[] { "Достань коробку и держи над картошкой 1 сек", "Наполняется за 0.5 сек", "Наполняется мгновенно", "АВТО: коробка сама наполняется и встаёт на выдачу", "ЛЕГЕНДА: коробка сама отдаётся клиенту" },
                new[] { "Take a box and hold it over the fries for 1 s", "Fills in 0.5 s", "Fills instantly", "AUTO: box fills itself and goes to the tray", "LEGEND: box is handed to the customer by itself" },
                new[] { 700, 4000, 18000, 65000 }),
            U(Up.VegFeed, "Подача овощей", "Veggie feeder",
                new[] { "Овощ перетаскиваешь из ящика на доску сам", "Касание ящика — овощ сам ложится на доску", "АВТО: помощник кладёт на доску то, что кончается" },
                new[] { "You drag veggies from the crate to the board", "Tap the crate — the veggie lands on the board", "AUTO: a helper puts whatever runs out on the board" },
                new[] { 1500, 20000 }),
            U(Up.FryLoad, "Засыпка фритюра", "Fryer loader",
                new[] { "Сырую картошку перетаскиваешь во фритюр сам", "Касание ящика — картошка высыпается во фритюр", "АВТО: помощник сам засыпает картошку во фритюр" },
                new[] { "You drag raw fries into the fryer", "Tap the crate — fries pour into the fryer", "AUTO: a helper loads the fryer by itself" },
                new[] { 1500, 20000 }),
            U(Up.FryUnload, "Выгрузка фритюра", "Fryer unloader",
                new[] { "Готовую корзину перетаскиваешь в лоток сам", "Касание корзины — картошка пересыпается в лоток", "АВТО: помощник сам пересыпает готовую картошку в лоток" },
                new[] { "You drag the cooked basket to the bin", "Tap the basket — fries pour into the bin", "AUTO: a helper empties cooked fries into the bin" },
                new[] { 1500, 20000 }),
        };

        // Локации идут по очереди. Переезд на следующую = РЕБЁРТ (как в Roblox):
        // монеты, день и улучшения сбрасываются, зато навсегда растёт множитель дохода и даётся Слава.
        //                                      Прох Студ Бизн Баб  Блог Крит Спорт Тур Строй Шк
        static LocationDef Loc(int id, string ru, string en, string dRu, string dEn, int cost, int minDay,
            float price, float pat, float spawn, float drink, string top, string bottom, string ground, string kiosk,
            bool night, string scene, int[] w, float x, float y)
        {
            return new LocationDef { id = id, ru = ru, en = en, descRu = dRu, descEn = dEn, cost = cost, minDay = minDay,
                price = price, patience = pat, spawn = spawn, drink = drink, skyTop = top, skyBottom = bottom, ground = ground,
                kiosk = kiosk, night = night, scene = scene, weights = w, mapX = x, mapY = y };
        }

        public static readonly LocationDef[] Locations =
        {
            Loc(0, "Ларёк во дворе", "Backyard kiosk", "Здесь всё началось. Ларёк дяди Ашота.", "Where it all began. Uncle Ashot's kiosk.",
                0, 1, 1f, 1f, 1f, 1f, "#6EC6F0", "#FDE7C4", "#C4BBAA", "#C0392B", false, "city",
                new[] { 40, 20, 10, 12, 6, 2, 4, 2, 4, 10 }, 0.08f, 0.36f),
            Loc(1, "Рынок", "Market", "Шумно, торгуются, но народу — тьма.", "Noisy, lots of haggling, huge crowds.",
                5000, 5, 1.3f, 0.95f, 0.9f, 1f, "#81D4FA", "#FFF3E0", "#BCAAA4", "#E67E22", false, "market",
                new[] { 35, 10, 10, 20, 5, 3, 3, 4, 10, 8 }, 0.18f, 0.66f),
            Loc(2, "Вокзал", "Train station", "Все торопятся, но платят больше.", "Everyone rushes but pays more.",
                15000, 5, 1.7f, 0.8f, 0.85f, 1.2f, "#9EC9E8", "#F2E3CF", "#A7A7A7", "#2E86AB", false, "city",
                new[] { 25, 8, 35, 8, 4, 4, 4, 8, 4, 4 }, 0.29f, 0.36f),
            Loc(3, "Пляж", "Beach", "Жарко! Все хотят пить. Много блогеров и туристов.", "Hot! Everyone is thirsty. Bloggers and tourists.",
                40000, 6, 2.2f, 1f, 0.85f, 2f, "#29B6F6", "#FFF3C4", "#E8D39A", "#F39C12", false, "beach",
                new[] { 25, 10, 4, 6, 20, 4, 10, 18, 0, 8 }, 0.4f, 0.68f),
            Loc(4, "Кампус", "Campus", "Толпы студентов: платят мало, но их очень много.", "Crowds of students: cheap but many.",
                100000, 6, 2.6f, 1.1f, 0.6f, 1f, "#81C784", "#FFF8E1", "#B8B0A0", "#8E44AD", false, "city",
                new[] { 10, 55, 4, 3, 10, 2, 8, 2, 2, 8 }, 0.5f, 0.36f),
            Loc(5, "Стадион", "Stadium", "Финал кубка! Болельщики и спортсмены.", "Cup final! Fans and athletes.",
                250000, 7, 3.2f, 0.7f, 0.55f, 1.4f, "#4FC3F7", "#E1F5FE", "#9E9E9E", "#1565C0", false, "stadium",
                new[] { 25, 10, 5, 2, 10, 3, 35, 5, 5, 5 }, 0.6f, 0.68f),
            Loc(6, "Аэропорт", "Airport", "Туристы со всего мира и бизнес-класс.", "Tourists from all over the world.",
                600000, 7, 4f, 0.75f, 0.7f, 1.3f, "#B3E5FC", "#ECEFF1", "#B0BEC5", "#00838F", false, "airport",
                new[] { 15, 5, 30, 4, 6, 6, 4, 30, 0, 0 }, 0.7f, 0.36f),
            Loc(7, "Ночной центр", "Downtown night", "Ночь, неон, критики и блогеры.", "Night, neon, critics and bloggers.",
                1500000, 8, 5f, 0.7f, 0.7f, 1.5f, "#0B1633", "#3A2F5B", "#4A4A5A", "#E91E63", true, "city",
                new[] { 30, 10, 15, 0, 25, 10, 5, 5, 0, 0 }, 0.8f, 0.68f),
            Loc(8, "Фудкорт ТЦ", "Mall food court", "Высшая лига. Безумный поток.", "The big league. Insane traffic.",
                4000000, 8, 6.5f, 0.7f, 0.5f, 1.3f, "#F5E6D3", "#E0CDB5", "#D7CCC8", "#16A085", false, "mall",
                new[] { 20, 15, 15, 8, 12, 8, 6, 6, 2, 8 }, 0.88f, 0.36f),
            Loc(9, "Небоскрёб", "Skyscraper", "Вершина города. Отсюда виден миллиард.", "The top of the city. A billion in sight.",
                12000000, 10, 9f, 0.65f, 0.45f, 1.5f, "#1A237E", "#FF8A65", "#546E7A", "#FFC928", true, "towers",
                new[] { 15, 5, 35, 2, 12, 15, 4, 10, 0, 2 }, 0.94f, 0.7f),
        };

        // ---------- ребёрты ----------
        /// <summary>Множитель дохода от переездов: +35% за каждый.</summary>
        public static float RebirthMult(int rebirths) { return 1f + 0.35f * rebirths; }
        /// <summary>Сколько Славы даёт переезд на локацию.</summary>
        public static int FameFor(int newLoc) { return 1 + newLoc / 2; }
        /// <summary>"Эффективный день" для открытия ингредиентов и сложности: на дальних локациях сразу больше всего.</summary>
        public static int EffDay(int day, int loc) { return day + loc * 3; }

        // ---------- путь к миллиарду: титулы по заработку за всё время ----------
        public static readonly long[] RankAt = { 0, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };
        static readonly string[] RankRu = { "Уличный повар", "Хозяин ларька", "Бизнесмен", "Предприниматель", "Ресторатор", "Магнат", "Олигарх шаурмы", "МИЛЛИАРДЕР" };
        static readonly string[] RankEn = { "Street cook", "Kiosk owner", "Businessman", "Entrepreneur", "Restaurateur", "Tycoon", "Shawarma oligarch", "BILLIONAIRE" };
        public static string RankName(int r) { r = Mathf.Clamp(r, 0, RankRu.Length - 1); return L.T(RankRu[r], RankEn[r]); }
        public static int RankOf(long lifetime)
        {
            int r = 0;
            for (int i = 0; i < RankAt.Length; i++) if (lifetime >= RankAt[i]) r = i;
            return r;
        }
        /// <summary>Бонус к цене от титула: +3% за каждый.</summary>
        public static float RankPriceMult(int rank) { return 1f + 0.03f * rank; }

        public static string Money(long v)
        {
            if (v >= 1000000000L) return (v / 1000000000.0).ToString("0.##") + L.T(" млрд", "B");
            if (v >= 1000000L) return (v / 1000000.0).ToString("0.##") + L.T(" млн", "M");
            if (v >= 10000L) return (v / 1000.0).ToString("0.#") + L.T(" тыс", "K");
            return v.ToString();
        }

        // ---------- рейтинг ларька (1..5) ----------
        public static float RepSpawnMult(float rep) { return Mathf.Lerp(1.3f, 0.75f, (rep - 1f) / 4f); }
        public static float RepTipMult(float rep) { return 0.8f + 0.1f * rep; }
        public const float RepHappy = 0.02f, RepPerfect = 0.01f, RepAngry = -0.06f;

        // ---------- золотые клиенты ----------
        public const float GoldenChance = 0.05f;
        public const float GoldenPay = 4f;

        public static IngredientDef Get(Ing id) { return Ingredients[(int)id]; }
        public static DrinkDef Get(Drink id) { return Drinks[(int)id]; }
        public static CustDef Get(CustType id)
        {
            foreach (var c in Customers) if (c.id == id) return c;
            return Customers[0];
        }
        public static int IngCount { get { return Ingredients.Length; } }

        // ---------- параметры дня ----------
        public static float DayDuration(int day)   { return Mathf.Min(100f + day * 4f, 160f); }
        public static float SpawnInterval(int day) { return Mathf.Max(11f - day * 0.6f, 4f); }
        public static float BasePatience(int day)  { return Mathf.Max(55f - day * 1.6f, 26f); }
        public static int   MaxActive(int day)     { return day < 3 ? 2 : (day < 6 ? 3 : 4); }
        public static int   MaxToppings(int day)   { return Mathf.Clamp(1 + day / 3, 1, 5); }
        public static float DoubleMeatChance(int day) { return day < 4 ? 0f : Mathf.Min(0.15f + day * 0.02f, 0.45f); }
        public const float  DrinkChance = 0.3f;

        /// <summary>Цель дня по выручке. Не выполнил — день повторяется.</summary>
        public static int Goal(int day, LocationDef loc)
        {
            return Mathf.RoundToInt((200f + day * 150f) * loc.price / 10f) * 10;
        }

        // ---------- эффекты улучшений (lvl 0..3) ----------
        static readonly int[]   _pieces     = { 3, 2, 1, 1 };
        static readonly int[]   _chops      = { 6, 3, 1, 1 };
        static readonly float[] _fryTime    = { 9f, 6f, 4f, 3f };
        static readonly float[] _fryBurn    = { 4f, 6f, 8f, 9999f };
        static readonly int[]   _friesBatch = { 3, 4, 6, 8 };
        static readonly float[] _grillTime  = { 6f, 4.5f, 3.5f, 2.5f };
        static readonly float[] _grillBurn  = { 4f, 6f, 8f, 9999f };
        static readonly int[]   _sauceCap   = { 6, 10, 14, 20 };
        static readonly float[] _sauceRate  = { 3f, 5f, 8f, 12f };
        static readonly int[]   _binCap     = { 4, 6, 8, 10 };
        static readonly float[] _patience   = { 1f, 1.15f, 1.3f, 1.5f };
        static readonly float[] _tips       = { 1f, 1.3f, 1.6f, 2f };
        static readonly float[] _price      = { 1f, 1.15f, 1.3f, 1.5f };

        static int C(int lvl) { return Mathf.Clamp(lvl, 0, 3); }

        public static int   PiecesPerPortion(int l) { return _pieces[C(l)]; }
        public static bool  SpitTap(int l)          { return l >= 2; }
        public static bool  SpitAuto(int l)         { return l >= 3; }
        public static float Legend2x(int l)         { return l >= 4 ? 0.5f : 1f; }
        public static int   SpitCapacity(int l)     { return 12 + C(l) * 4; }
        public static int   ChopsNeeded(int l)      { return _chops[C(l)]; }
        public static bool  KnifeAuto(int l)        { return l >= 3; }
        public static float FryTime(int l)          { return _fryTime[C(l)] * (l >= 4 ? 0.6f : 1f); }
        public static float FryBurn(int l)          { return _fryBurn[C(l)]; }
        public static int   FriesBatch(int l)       { return _friesBatch[C(l)]; }
        public static bool  FryerAuto(int l)        { return l >= 3; }
        public static int   GrillSlots(int grillsLvl) { return 1 + Mathf.Clamp(grillsLvl, 0, 2); }
        public static float GrillTime(int l)        { return _grillTime[C(l)] * (l >= 4 ? 0.7f : 1f); }
        public static float GrillBurn(int l)        { return _grillBurn[C(l)]; }
        public static bool  GrillAuto(int l)        { return l >= 3; }
        public static int   SauceCap(int l)         { return _sauceCap[C(l)]; }
        public static float SauceRate(int l)        { return _sauceRate[C(l)]; }
        public static bool  SauceAuto(int l)        { return l >= 3; }
        public static int   BinCap(int l)           { return _binCap[C(l)] + (l >= 4 ? 4 : 0); }
        public static float PatienceMult(int l)     { return l >= 4 ? 1.8f : _patience[C(l)]; }
        public static float TipMult(int l)          { return l >= 4 ? 2.5f : _tips[C(l)]; }
        public static float PriceMult(int l)        { return l >= 4 ? 1.8f : _price[C(l)]; }
        static readonly int[] _packTaps = { 3, 2, 1, 1 };
        public static int   PackTaps(int l)         { return _packTaps[C(l)]; }
        public static bool  PackAuto(int l)         { return l >= 3; }
        public static float BoxFillTime(int l)      { return l >= 2 ? 0f : (l == 1 ? 0.5f : 1f); }
        public static bool  BoxAuto(int l)          { return l >= 3; }
        public static bool  BoxDeliver(int l)       { return l >= 4; }
        public static bool  HelperTap(int l)        { return l >= 1; }
        public static bool  HelperAuto(int l)       { return l >= 2; }
        public static bool  AutoAsk(int tipsLvl)    { return tipsLvl >= 3; }
        public static int   FlyHits(int l)          { return l >= 1 ? 1 : 3; }
        public static int   DirtyAfter(int l)       { return l >= 4 ? 99999 : (l >= 2 ? 8 : 4); }
        public static float WipeDistance(int l)     { return l >= 1 ? 350f : 700f; }
        public static int   FireTaps(int l)         { return l >= 1 ? 1 : 3; }
        static readonly float[] _queueWait = { 1f, 1.4f, 1.8f, 2.5f };
        public static float QueueWait(int l)        { return l >= 4 ? 3.5f : _queueWait[C(l)]; }
        public static int   QueueMax(int l)         { return l >= 4 ? 10 : (l >= 2 ? 8 : 6); }
        public static float ComboMult(int combo)    { return 1f + 0.1f * Mathf.Min(combo, 10); }

        public static bool Unlocked(Ing id, int day) { return Get(id).unlockDay <= day; }
        public static bool Unlocked(Drink id, int day) { return id != Drink.None && Get(id).unlockDay <= day; }

        // ---------- заказы: массив количеств по Ing ----------
        public static int[] NewCounts() { return new int[IngCount]; }

        public static bool Same(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public static int Total(int[] a)
        {
            int s = 0;
            foreach (var x in a) s += x;
            return s;
        }

        public static int OrderPrice(int[] counts, Drink drink)
        {
            int sum = LavashPrice;
            for (int i = 0; i < counts.Length; i++) sum += Ingredients[i].price * counts[i];
            if (drink != Drink.None) sum += Get(drink).price;
            return sum;
        }

        public static string ShortList(int[] counts)
        {
            var parts = new List<string>();
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] <= 0) continue;
                string s = Ingredients[i].Short;
                if (counts[i] > 1) s += "×" + counts[i];
                parts.Add(s);
            }
            return string.Join(", ", parts);
        }

        /// <summary>Что нового открывается в этот день.</summary>
        public static string NewStuff(int day)
        {
            var parts = new List<string>();
            foreach (var d in Ingredients) if (d.unlockDay == day && day > 1) parts.Add(d.Name);
            for (int i = 1; i < Drinks.Length; i++) if (Drinks[i].unlockDay == day) parts.Add(Drinks[i].Name);
            foreach (var c in Customers) if (c.unlockDay == day && day > 1) parts.Add(L.T("клиент: ", "customer: ") + c.Name);
            if (day == 4) parts.Add(L.T("двойное мясо в заказах", "double meat orders"));
            return string.Join(", ", parts);
        }
    }
}
