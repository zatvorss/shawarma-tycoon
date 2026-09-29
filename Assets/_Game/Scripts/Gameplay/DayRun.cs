using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Один игровой день. Этот файл — клиенты, деньги, цель дня, комбо, события, HUD.
    /// Кухня — в DayRun.Kitchen.cs.
    /// </summary>
    public partial class DayRun : MonoBehaviour
    {
        Game _game;
        SaveData _save;
        LocationDef _loc;
        int _day, _eday;

        // --- параметры дня ---
        float _dayDuration, _spawnInterval, _patience, _tipMult, _priceMult, _drinkChance;
        int _maxActive, _goal;
        DayEvent _event;

        // --- состояние ---
        float _timeLeft, _elapsed, _spawnTimer = 2.5f, _introT = 0.7f;
        bool _closedAnnounced, _ended, _paused, _goalAnnounced;
        int _combo, _bestCombo;
        float _hypeT;
        readonly Customer[] _customers = new Customer[GameData.MaxSlots];
        bool[] _boost = new bool[3];
        bool _autoAsk;
        float _cutMult = 1f;
        List<Quest> _quests;
        Image _nightTint;
        readonly List<Image> _lampGlows = new List<Image>();

        // --- статистика ---
        public int Day { get { return _day; } }
        public int Goal { get { return _goal; } }
        public bool GoalReached { get { return Earned + Tips >= _goal; } }
        public int Earned { get; private set; }
        public int Tips { get; private set; }
        public int Served { get; private set; }
        public int Lost { get; private set; }
        public int BurntCount { get; private set; }
        public int BestCombo { get { return _bestCombo; } }
        public int DayXp { get; private set; }
        public bool BossWon { get; private set; }
        public List<Quest> Quests { get { return _quests; } }
        public int PerfectServed { get; private set; }
        public int CriticWins { get; private set; }
        public int ToGoServed { get; private set; }

        // --- UI ---
        RectTransform _root, _street, _fx, _moneyIcon, _banner, _goalFill, _clockHand;
        RectTransform[] _slots;
        Text _dayText, _eventText, _clockText, _moneyText, _comboText, _hintText, _toastText, _bannerText, _goalText, _repText;
        Image _flash;
        GameObject _hintBulb;
        Transform _questBtn;
        Text _questText;
        GameObject _pauseOverlay;
        float _toastT, _bannerT;

        bool Rain { get { return _event == DayEvent.Rain; } }
        bool RushNow
        {
            get
            {
                if (_event != DayEvent.Rush) return false;
                float k = _elapsed / _dayDuration;
                return k > 0.35f && k < 0.6f;
            }
        }

        // =====================================================================
        public void Init(Game game, SaveData save, RectTransform root, bool[] boosts)
        {
            if (boosts != null) _boost = boosts;
            _game = game;
            _save = save;
            _loc = save.Loc;
            _day = save.day;
            _eday = save.EffDay;
            _root = root;

            _dayDuration   = GameData.DayDuration(_day);
            _spawnInterval = GameData.SpawnInterval(_day) * _loc.spawn;
            _patience      = GameData.BasePatience(_day) * _loc.patience * GameData.PatienceMult(save.Lvl(Up.Patience));
            _tipMult       = GameData.TipMult(save.Lvl(Up.Tips));
            _priceMult     = GameData.PriceMult(save.Lvl(Up.Price)) * _loc.price * GameData.RankPriceMult(save.rank);
            _drinkChance   = Mathf.Min(0.8f, GameData.DrinkChance * _loc.drink);
            _maxActive     = GameData.MaxActive(_eday);
            _goal          = GameData.Goal(_day, _loc);
            _timeLeft      = _dayDuration;
            _autoAsk       = GameData.AutoAsk(save.Lvl(Up.Tips));
            _quests        = Meta.MakeQuests(_day, _loc);
            InitEvents();

            // слава и переезды — навсегда
            _priceMult *= GameData.RebirthMult(save.rebirths) * (1f + 0.1f * Meta.P(save, Perk.Recipe));
            if (Meta.Has(save, Meta.CharmCoin)) _priceMult *= 1.05f;
            if (Meta.Has(save, Meta.CharmCrown)) _priceMult *= 1.15f;
            _spawnInterval *= 1f - 0.06f * Meta.P(save, Perk.Word);
            _patience *= 1f + 0.07f * Meta.P(save, Perk.Calm);
            if (Meta.Has(save, Meta.CharmBell)) _patience *= 1.08f;
            _cutMult = 1f - 0.08f * Meta.P(save, Perk.Hands);

            if (_boost[(int)Boost.Speed]) _cutMult *= 0.5f;
            if (_boost[(int)Boost.Ads]) { _spawnInterval *= 0.75f; _maxActive = Mathf.Min(_maxActive + 1, GameData.MaxSlots); }
            if (_boost[(int)Boost.Gold]) _priceMult *= 1.25f;

            _event = DayEvent.None;
            if (_day >= 3)
            {
                float r = Random.value;
                if (r < 0.3f && !_loc.night && _loc.scene != "mall") _event = DayEvent.Rain;
                else if (r < 0.6f) _event = DayEvent.Rush;
            }

            InitKitchen();

            BuildStreet();
            BuildHud();
            BuildCounter();
            BuildKitchen();
            BuildEventProps();
            _fx = UIKit.Full(_root, "FX");
            BuildBanner();
            BuildPause();

            // вспышка после "залёта камеры"
            _flash = UIKit.Panel(_root, "Flash", Color.white, 0, 0, 1, 1, false);
            _root.localScale = new Vector3(1.12f, 1.12f, 1f);

            RefreshAllKitchen();
            RefreshHud();

            string msg = _loc.Name + " • " + L.T("День ", "Day ") + _day;
            msg += "\n<size=36>" + L.T("Цель: ", "Goal: ") + _goal + "</size>";
            string news = GameData.NewStuff(_eday);
            if (news.Length > 0) msg += "\n<size=30>" + L.T("Новое: ", "New: ") + news + "</size>";
            if (_event == DayEvent.Rain) msg += "\n<size=30>" + L.T("Дождь: клиентов меньше, чаевые ×1.5", "Rain: fewer customers, tips ×1.5") + "</size>";
            if (_event == DayEvent.Rush) msg += "\n<size=30>" + L.T("Сегодня будет час пик!", "Rush hour today!") + "</size>";
            for (int b = 0; b < _boost.Length; b++)
                if (_boost[b]) msg += "\n<size=28><color=#FFC928>" + L.T("Бустер: ", "Booster: ") + Meta.Boosts[b].Name + "</color></size>";
            msg += EventsBannerText();
            ShowBanner(msg, 4.2f);
        }

        // =====================================================================
        //  ЦИКЛ
        // =====================================================================
        void Update()
        {
            float udt = Time.unscaledDeltaTime;
            if (_introT > 0f)
            {
                _introT -= udt;
                float k = Mathf.Clamp01(_introT / 0.7f);
                _flash.color = new Color(1f, 1f, 1f, k);
                float sc = 1f + 0.12f * k * k;
                _root.localScale = new Vector3(sc, sc, 1f);
                if (_introT <= 0f)
                {
                    _flash.gameObject.SetActive(false);
                    _root.localScale = Vector3.one;
                }
            }
            UpdateToast(udt);
            UpdateBanner(udt);
            if (_ended || _paused) return;

            float dt = Time.deltaTime;
            _elapsed += dt;
            if (_hypeT > 0f) _hypeT -= dt;
            TickFever(dt);
            UpdateDayNight();

            if (_timeLeft > 0f)
            {
                _timeLeft -= dt;
                _spawnTimer -= dt;
                if (_spawnTimer <= 0f)
                {
                    TrySpawn();
                    float k = 1f;
                    if (Rain) k *= 1.25f;
                    if (RushNow) k *= 0.5f;
                    if (_hypeT > 0f) k *= 0.6f;
                    _spawnTimer = _spawnInterval * k * GameData.RepSpawnMult(_save.rep) * Random.Range(0.8f, 1.2f);
                }
            }
            else if (!_closedAnnounced)
            {
                _closedAnnounced = true;
                if (_queue.Count > 0) Toast(L.T("Ларёк закрыт — очередь разошлась", "Closed — the queue went home"));
                _queue.Clear();
                ShowBanner(L.T("Ларёк закрывается!\n<size=34>Обслужи оставшихся</size>", "Closing time!\n<size=34>Serve the last customers</size>"), 2.2f);
            }

            TickKitchen(dt);
            TickEvents(dt);
            if (_timeLeft > 0f) TickQueue(dt);
            TickCustomers(dt);
            RefreshHud();

            if (!_goalAnnounced && GoalReached)
            {
                _goalAnnounced = true;
                ShowBanner(L.T("Цель дня выполнена!", "Daily goal reached!"), 1.8f);
                Sfx.Play(Sound.Fanfare);
            }

            if (_timeLeft <= 0f && NoCustomers() && _queue.Count == 0)
            {
                _ended = true;
                foreach (var q in _quests)
                    if (q.type == QuestType.NoBurn && BurntCount == 0) CompleteQuest(q);
                _game.OnDayFinished(this);
            }
        }

        bool NoCustomers()
        {
            foreach (var c in _customers) if (c != null) return false;
            return true;
        }

        int ActiveCount()
        {
            int n = 0;
            foreach (var c in _customers) if (c != null && !c.leaving) n++;
            return n;
        }

        bool _musicianHere;
        readonly HashSet<CustType> _specialSeen = new HashSet<CustType>();

        void TickCustomers(float dt)
        {
            // особенные гости: музыкант успокаивает очередь, пожарный тушит гриль
            _musicianHere = false;
            bool fireman = false;
            foreach (var cc in _customers)
            {
                if (cc == null || cc.leaving) continue;
                if (cc.type == CustType.Musician) _musicianHere = true;
                if (cc.type == CustType.Fireman) fireman = true;
            }
            if (fireman && AnyFire())
                foreach (var g in _grill)
                    if (g.onFire)
                    {
                        Juice.Burst(g.view, UIKit.Hex("#4FC3F7"), 14, 0.5f, 0.9f, 12f);
                        PutOut(g, L.T("Пожарный потушил огонь! Спасибо!", "The firefighter put out the fire!"));
                    }

            for (int i = 0; i < _customers.Length; i++)
            {
                var c = _customers[i];
                if (c == null) continue;
                c.Tick(dt);
                if (_rivalT > 0f && !c.leaving) c.patience -= dt * 0.6f;
                if (_musicianHere && !c.leaving && c.type != CustType.Musician) c.patience = Mathf.Min(c.maxPatience, c.patience + dt * 0.4f);

                if (c.leaving)
                {
                    if (c.leaveT > 1.3f)
                    {
                        c.Destroy();
                        _customers[i] = null;
                    }
                    continue;
                }

                if (c.type == CustType.Granny && !c.changedMind && c.needShawarma && c.PatienceFrac < 0.6f)
                {
                    c.ChangeOrder(MakeOrder(c.type));
                    Juice.FloatText(_slots[i], L.T("Ой, передумала!", "Oh, changed my mind!"), UIKit.Purple, 36, 0.5f, 0.85f);
                    Sfx.Play(Sound.Spawn);
                }

                if (c.patience <= 0f)
                {
                    if (c.boss)
                    {
                        ShowBanner(L.T("Мистер Кинг ушёл, смеясь...\n<size=30>«Я же говорил — твой ларёк не для больших дел!»</size>",
                                       "Mr. King left laughing...\n<size=30>“Told you — your kiosk can't handle big business!”</size>"), 2.8f);
                        ChangeRep(-0.3f);
                    }
                    Lost++;
                    ChangeRep(GameData.RepAngry);
                    c.Leave(false, L.T("Слишком долго!", "Too slow!"));
                    Sfx.Play(Sound.Error);
                    ResetCombo();
                }
            }
        }

        // --- очередь на улице: если у окна нет места, люди ждут и нервничают ---
        readonly List<float> _queue = new List<float>();
        int MaxQueue { get { return GameData.QueueMax(_save.Lvl(Up.Queue)); } }

        int FreeSlot()
        {
            var free = new List<int>();
            for (int i = 0; i < _customers.Length; i++) if (_customers[i] == null) free.Add(i);
            return free.Count == 0 ? -1 : free[Random.Range(0, free.Count)];
        }

        void TrySpawn()
        {
            if (ActiveCount() >= _maxActive || FreeSlot() < 0 || _queue.Count > 0)
            {
                if (_queue.Count < MaxQueue)
                {
                    _queue.Add(0f);
                    Sfx.Play(Sound.Tick);
                }
                return;
            }
            SpawnCustomer(0f);
        }

        void TickQueue(float dt)
        {
            float limit = _patience * 0.7f * GameData.QueueWait(_save.Lvl(Up.Queue));
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                _queue[i] += dt * (_rivalT > 0f ? 1.6f : 1f);
                if (_queue[i] > limit)
                {
                    _queue.RemoveAt(i);
                    Lost++;
                    ChangeRep(GameData.RepAngry);
                    ResetCombo();
                    Toast(L.T("Клиент не дождался в очереди и ушёл!", "A customer left the queue!"));
                    Sfx.Play(Sound.Error);
                }
            }
            if (_queue.Count > 0 && ActiveCount() < _maxActive && FreeSlot() >= 0)
            {
                float waited = _queue[0];
                _queue.RemoveAt(0);
                SpawnCustomer(waited);
            }
        }

        void SpawnCustomer(float waited)
        {
            int slot = FreeSlot();
            if (slot < 0) return;

            var type = PickType();
            var def = GameData.Get(type);
            Drink drink = Drink.None;
            var drinks = new List<Drink>();
            if (GameData.Unlocked(Drink.Cola, _eday)) drinks.Add(Drink.Cola);
            if (GameData.Unlocked(Drink.Ayran, _eday)) drinks.Add(Drink.Ayran);
            if (GameData.Unlocked(Drink.Fries, _eday)) drinks.Add(Drink.Fries);
            if (drinks.Count > 0 && Random.value < Mathf.Min(0.8f, _drinkChance + 0.1f)) drink = drinks[Random.Range(0, drinks.Count)];

            float patience = _patience * def.patienceMult * Random.Range(0.9f, 1.1f);
            patience = Mathf.Max(patience * 0.35f, patience - waited * 0.5f);
            var mode = Random.value < 0.5f ? ServeMode.Here : ServeMode.ToGo;
            bool known = _autoAsk || (type != CustType.Tourist && Random.value < GameData.ModeKnownChance);
            bool golden = _eday >= 3 && Random.value < GameData.GoldenChance * (Meta.Has(_save, Meta.CharmMagnet) ? 2f : 1f);
            bool special = _special != null && type != CustType.Vegan && type != CustType.Glutton && Random.value < 0.25f;
            var order = special ? (int[])_special.Clone() : MakeOrder(type);
            int qty = type == CustType.Glutton ? 2 : 1;
            var c = new Customer(_slots[slot], type, order, drink, patience, mode, known, golden, -1, qty, special);
            _customers[slot] = c;
            c.Say(golden ? L.T("Я очень богат! Удиви меня!", "I'm very rich! Impress me!") : Meta.Hello(type), 2.5f);
            OnSpecialArrived(slot, c);
            if (golden) Sfx.Play(Sound.Fanfare);
            Sfx.Play(Sound.Spawn);
        }

        /// <summary>Особенный гость пришёл: подсказка и его эффект.</summary>
        void OnSpecialArrived(int slot, Customer c)
        {
            var def = GameData.Get(c.type);
            if (!def.special || c.golden) return;
            if (!_specialSeen.Contains(c.type))
            {
                _specialSeen.Add(c.type);
                ShowBanner("★ " + def.Name + "\n<size=30>" + def.Note + "</size>", 2.4f);
            }
            if (c.type == CustType.Clown)
            {
                int n = 0;
                foreach (var o in _customers)
                    if (o != null && o != c && !o.leaving) { o.patience = Mathf.Min(o.maxPatience, o.patience + o.maxPatience * 0.25f); n++; }
                Juice.FloatText(_slots[slot], L.T("Ха-ха-ха!", "Ha-ha-ha!"), UIKit.Gold, 44, 0.3f, 0.9f);
                Sfx.Play(Sound.Combo);
                if (n > 0) Toast(L.T("Клоун развеселил всех: терпение +25%", "The clown cheered everyone up: patience +25%"));
            }
            if (c.type == CustType.Musician) Toast(L.T("Музыкант играет — все ждут спокойнее", "The musician plays — everyone waits calmly"));
        }

        CustType PickType()
        {
            int total = 0;
            foreach (var c in GameData.Customers)
                if (c.unlockDay <= _eday) total += GameData.Weight(_loc, c.id);
            if (total <= 0) return CustType.Regular;
            int r = Random.Range(0, total);
            foreach (var c in GameData.Customers)
            {
                if (c.unlockDay > _eday) continue;
                int w = GameData.Weight(_loc, c.id);
                if (r < w) return c.id;
                r -= w;
            }
            return CustType.Regular;
        }

        int[] MakeOrder(CustType t = CustType.Regular)
        {
            var toppings = new List<Ing>();
            foreach (var d in GameData.Ingredients)
                if (d.id != Ing.Meat && d.unlockDay <= _eday) toppings.Add(d.id);
            for (int i = toppings.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = toppings[i]; toppings[i] = toppings[j]; toppings[j] = tmp;
            }
            int k = Random.Range(1, Mathf.Min(GameData.MaxToppings(_eday), toppings.Count) + 1);
            var order = GameData.NewCounts();
            if (t == CustType.Kid) k = 1;
            order[(int)Ing.Meat] = Random.value < GameData.DoubleMeatChance(_eday) ? 2 : 1;
            if (t == CustType.Athlete || t == CustType.Builder || t == CustType.Fan || t == CustType.Fireman) order[(int)Ing.Meat] = 2;
            if (t == CustType.Vegan)
            {
                order[(int)Ing.Meat] = 0;
                k = Mathf.Min(Mathf.Max(k, 2), toppings.Count);
            }
            for (int i = 0; i < k; i++) order[(int)toppings[i]] = 1;
            if (t == CustType.Builder && GameData.Unlocked(Ing.Fries, _eday)) order[(int)Ing.Fries] = 1;
            return order;
        }

        // =====================================================================
        //  ВЫДАЧА ЗАКАЗА
        // =====================================================================
        bool Fits(Customer c, TrayItem item)
        {
            return item.isDrink ? c.WantsDrink(item.drink) : c.WantsShawarma(item.counts);
        }

        void OnCustomer(int slot)
        {
            if (_paused || _ended) return;
            var c = _customers[slot];
            if (c == null || c.leaving) return;

            // сначала узнать: здесь или с собой
            if (!c.modeKnown && c.needShawarma)
            {
                c.Reveal();
                Sfx.Play(Sound.Tick);
                return;
            }

            if (_tray.Count == 0)
            {
                Toast(L.T("На выдаче пусто — сначала приготовь", "Serving tray is empty"));
                Sfx.Play(Sound.Error);
                return;
            }

            int idx = -1;
            if (_selected >= 0 && _selected < _tray.Count) idx = _selected;
            else idx = BestItemFor(c);
            if (idx < 0)
            {
                Toast(L.T("На выдаче нет того, что он заказал", "Nothing on the tray matches this order"));
                Sfx.Play(Sound.Error);
                return;
            }

            var item = _tray[idx];
            if (!item.isDrink && item.pack == 0 && Fits(c, item))
            {
                Toast(L.T("Сначала упакуй: нажми «Обёртка» на выдаче", "Pack it first: tap Wrap on the tray"));
                Sfx.Play(Sound.Error);
                return;
            }
            _tray.RemoveAt(idx);
            _selected = -1;
            RefreshTray();

            if (!Fits(c, item))
            {
                c.LosePatience(0.25f);
                ChangeRep(GameData.RepAngry * 0.5f);
                Toast(L.T("Не то! Клиент злится", "Wrong item! Customer is upset"));
                Sfx.Play(Sound.Error);
                ResetCombo();
                return;
            }

            if (!item.isDrink)
            {
                c.gotPerfect = item.perfect;
                if (item.fresh) c.gotFresh = true;
                c.packOk = c.mode == ServeMode.Here ? item.pack == 1 : item.pack == 2;
            }
            else if (item.drink != Drink.Fries) Progress(QuestType.Drinks, 1);
            c.Deliver(item.isDrink);
            Sfx.Play(Sound.Click);
            if (c.Done) Pay(slot, c);
        }

        void Pay(int slot, Customer c)
        {
            var def = GameData.Get(c.type);
            float mult = _priceMult * def.payMult * GameData.ComboMult(_combo) * (c.golden ? GameData.GoldenPay : 1f);
            if (c.special) mult *= 1.8f;
            if (_feverT > 0f) mult *= 2f;
            if (c.gotFresh) mult *= 1f + Meta.BakeBonus(_save.biz[Meta.BizBakery]);
            if (c.car) mult *= Meta.DriveMult(_save.biz[Meta.BizDrive]);
            int price = Mathf.RoundToInt(GameData.OrderPrice(c.order, c.drink) * mult);
            if (c.type == CustType.Glutton && !c.IsCourier && !c.boss) price *= c.qtyTotal;
            if (c.IsCourier)
            {
                int bizPay = CourierPay();
                price = price * c.qtyTotal + bizPay;
                CourierIncome += bizPay;
                Toast(Meta.Businesses[c.bizIndex].Name + L.T(" доволен заказом! +", " is happy! +") + bizPay);
            }
            if (c.boss)
            {
                int bonus = Mathf.RoundToInt(_goal * 0.6f);
                price = price * c.qtyTotal + bonus;
                _save.fame += 1;
                BossWon = true;
                ShowBanner(L.T("ТЫ ПОБЕДИЛ МИСТЕРА КИНГА!\n<size=32>+", "YOU BEAT MR. KING!\n<size=32>+") + bonus + " + " + Meta.FamePts(1) + "</size>", 3f);
                Sfx.Play(Sound.Fanfare);
            }
            float tipK = 0.3f * c.PatienceFrac * _tipMult * def.tipMult;
            if (Rain) tipK *= 1.5f;
            if (_hypeT > 0f) tipK *= 1.5f;
            if (c.gotPerfect) tipK *= 1.25f;
            if (c.golden) tipK *= 2f;
            tipK *= GameData.RepTipMult(_save.rep);
            int tip = Mathf.RoundToInt(price * tipK);

            // санинспектор проверяет чистоту
            if (c.type == CustType.Inspector && !c.IsCourier && !c.boss && !c.golden)
            {
                bool dirty = _dirty || _fly != null || AnyFire();
                if (dirty)
                {
                    int fine = Mathf.RoundToInt(_goal * 0.15f / 10f) * 10;
                    _save.money = Mathf.Max(0, _save.money - fine);
                    Served++;
                    ChangeRep(-0.3f);
                    ResetCombo();
                    c.Leave(false, L.T("Нарушения! Штраф", "Violations! Fine"));
                    Juice.FloatText(_slots[slot], "-" + fine, UIKit.Red, 56, 0.7f, 0.65f);
                    ShowBanner(L.T("Санинспектор выписал штраф −", "The inspector fined you −") + fine +
                        L.T("\n<size=30>Грязная доска, муха или пожар — держи ларёк в чистоте!</size>", "\n<size=30>Dirty board, a fly or a fire — keep the kiosk clean!</size>"), 2.6f);
                    Sfx.Play(Sound.Error);
                    LiveStats(c, 0, false);
                    return;
                }
                price *= 2;
                ChangeRep(0.3f);
                ShowBanner(L.T("Образцовая чистота!\n<size=30>Санинспектор доволен: двойная оплата и +рейтинг</size>",
                               "Spotless!\n<size=30>The inspector is pleased: double pay and +rating</size>"), 2.2f);
            }

            // не так упаковано
            if (!c.packOk)
            {
                price = Mathf.RoundToInt(price * 0.6f);
                Earned += price;
                Served++;
                _save.money += price;
                c.Leave(false, c.mode == ServeMode.ToGo ? L.T("Я же с собой просил!", "I said to go!") : L.T("Зачем пакет? Я здесь ем!", "Why a bag? I'm eating here!"));
                Juice.FloatText(_slots[slot], "+" + price, UIKit.Gold, 50, 0.7f, 0.65f);
                Sfx.Play(Sound.Error);
                ResetCombo();
                ChangeRep(GameData.RepAngry);
                LiveStats(c, price, false);
                Progress(QuestType.Serve, 1);
                return;
            }

            // критик недоволен неидеальной прожаркой
            if (c.type == CustType.Critic && !c.gotPerfect)
            {
                price = Mathf.RoundToInt(price * 0.3f);
                tip = 0;
                Earned += price;
                Served++;
                _save.money += price;
                c.Leave(false, L.T("Пересушено! 1/5", "Overcooked! 1/5"));
                Juice.FloatText(_slots[slot], "+" + price, UIKit.Gold, 50, 0.7f, 0.65f);
                Sfx.Play(Sound.Error);
                ResetCombo();
                ChangeRep(GameData.RepAngry);
                LiveStats(c, price, false);
                return;
            }

            int total = price + tip;
            Earned += price;
            Tips += tip;
            Served++;
            _save.money += total;
            if (c.gotPerfect) PerfectServed++;
            if (c.type == CustType.Critic) CriticWins++;
            if (c.mode == ServeMode.ToGo) ToGoServed++;
            ChangeRep(GameData.RepHappy + (c.gotPerfect ? GameData.RepPerfect : 0f));

            Progress(QuestType.Serve, 1);
            if (c.gotPerfect) Progress(QuestType.Perfect, 1);
            if (c.mode == ServeMode.ToGo) Progress(QuestType.ToGo, 1);
            foreach (var q in _quests) if (q.type == QuestType.ServeType && q.cust == c.type) Progress(q, 1);
            Progress(QuestType.Earn, total);

            _combo++;
            if (_combo > _bestCombo) _bestCombo = _combo;
            if (_combo % 5 == 0) StartFever();
            foreach (var q in _quests) if (q.type == QuestType.Combo && _combo > q.progress) { q.progress = _combo - 1; Progress(q, 1); }
            if (_combo >= 3)
            {
                Sfx.Play(Sound.Combo);
                Juice.Punch(_comboText.transform, 0.35f, 0.3f);
            }

            if (c.car) CarsServed++;
            string thanks = c.type == CustType.Critic ? L.T("Шедевр! 5/5", "Masterpiece! 5/5")
                : (tip > price / 5 ? L.T("Супер!", "Awesome!") : (tip > 0 ? L.T("Спасибо!", "Thanks!") : L.T("Норм", "OK")));
            c.Leave(true, thanks);
            LiveStats(c, total, true);
            Juice.FloatText(_slots[slot], "+" + total, UIKit.Gold, 60, 0.7f, 0.65f);
            SpawnCoins(c.BubbleWorld, Mathf.Clamp(total / 40, 3, 8));
            Sfx.Play(Sound.Coin);

            if (c.type == CustType.Blogger && c.PatienceFrac > 0.5f)
            {
                _hypeT = 25f;
                ShowBanner(L.T("Блогер выложил сторис!\n<size=34>Наплыв клиентов и чаевые ×1.5</size>",
                               "A blogger posted a story!\n<size=34>More customers, tips ×1.5</size>"), 2.5f);
                Sfx.Play(Sound.Fanfare);
            }
        }

        int BestItemFor(Customer c)
        {
            int best = -1, bestScore = -1;
            for (int i = 0; i < _tray.Count; i++)
            {
                var it = _tray[i];
                if (!Fits(c, it)) continue;
                int score = 1;
                if (!it.isDrink)
                {
                    bool packOk = c.mode == ServeMode.Here ? it.pack == 1 : it.pack == 2;
                    if (packOk) score += 4;
                    else if (it.pack > 0) score += 1;
                    if (it.perfect && c.type == CustType.Critic) score += 2;
                }
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        // ---------- задания ----------
        void Progress(QuestType t, int amount)
        {
            foreach (var q in _quests) if (q.type == t) Progress(q, amount);
        }

        void Progress(Quest q, int amount)
        {
            if (q.done) return;
            q.progress += amount;
            if (q.progress >= q.target) CompleteQuest(q);
        }

        void CompleteQuest(Quest q)
        {
            if (q.done) return;
            q.done = true;
            q.progress = q.target;
            _save.money += q.reward;
            Toast(L.T("Задание выполнено! +", "Quest complete! +") + q.reward);
            Sfx.Play(Sound.Fanfare);
            if (_questBtn != null) Juice.Punch(_questBtn, 0.3f, 0.3f);
        }

        void ShowQuests()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(L.T("Задания дня", "Daily quests")).Append('\n');
            foreach (var q in _quests)
            {
                sb.Append("<size=30>").Append(q.done ? "<color=#7CFC00>[OK] </color>" : "• ").Append(q.Text);
                if (!q.done && q.Progress.Length > 0) sb.Append("  (").Append(q.Progress).Append(")");
                sb.Append("  <color=#FFC928>+").Append(q.reward).Append("</color></size>\n");
            }
            ShowBanner(sb.ToString().TrimEnd('\n'), 3.5f);
        }

        int QuestsDone()
        {
            int n = 0;
            foreach (var q in _quests) if (q.done) n++;
            return n;
        }

        // ---------- вечер: небо темнеет, фонари зажигаются ----------
        void UpdateDayNight()
        {
            if (_nightTint == null) return;
            float k = Mathf.Clamp01(_elapsed / _dayDuration);
            Color c;
            if (_loc.night || _loc.scene == "mall") c = new Color(0, 0, 0, 0);
            else if (k < 0.55f) c = new Color(1f, 0.6f, 0.2f, 0f);
            else if (k < 0.8f) c = new Color(1f, 0.55f, 0.2f, Mathf.Lerp(0f, 0.22f, (k - 0.55f) / 0.25f));
            else c = Color.Lerp(new Color(1f, 0.55f, 0.2f, 0.22f), new Color(0.05f, 0.07f, 0.25f, 0.5f), (k - 0.8f) / 0.2f);
            _nightTint.color = c;
            float glow = _loc.night ? 1f : Mathf.Clamp01((k - 0.7f) / 0.15f);
            for (int i = 0; i < _lampGlows.Count; i++)
            {
                var g = _lampGlows[i].color;
                g.a = glow * (i % 2 == 0 ? 0.35f : 1f);
                _lampGlows[i].color = g;
            }
        }

        void ChangeRep(float d)
        {
            float before = _save.rep;
            _save.rep = Mathf.Clamp(_save.rep + d, 1f, 5f);
            if (Mathf.FloorToInt(before * 2f) != Mathf.FloorToInt(_save.rep * 2f) && _repText != null)
                Juice.Punch(_repText.transform, 0.3f, 0.3f);
        }

        /// <summary>Статистика для достижений и "пути к миллиарду" — сразу, а не в конце дня.</summary>
        void LiveStats(Customer c, int money, bool happy)
        {
            _save.totalServed++;
            if (happy)
            {
                int xp = 10 + (c.gotPerfect ? 5 : 0) + (c.golden ? 20 : 0) + (c.IsCourier ? 30 : 0) + (c.boss ? 100 : 0) + (c.car ? 15 : 0) + (_feverT > 0f ? 5 : 0);
                _save.passXp += xp;
                DayXp += xp;
            }
            if (happy && c.IsCourier) _save.couriers++;
            if (happy && c.special) _save.specials++;
            _save.lifetime += money;
            if (happy)
            {
                if (c.gotPerfect) _save.totalPerfect++;
                if (c.type == CustType.Critic) _save.criticWins++;
                if (c.mode == ServeMode.ToGo) _save.toGoServed++;
            }
            _save.bestCombo = Mathf.Max(_save.bestCombo, _combo);
            _game.OnStatsChanged();
        }

        void ResetCombo()
        {
            if (_combo >= 3) Toast(L.T("Комбо сброшено", "Combo lost"));
            _combo = 0;
        }

        void SpawnCoins(Vector3 from, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var coin = Icons.Coin(_fx, 0.5f, 0.5f, 44f);
                var offset = new Vector3(Random.Range(-40f, 40f), Random.Range(-30f, 30f), 0f);
                Juice.Fly(coin, from + offset, _moneyIcon.position, 0.55f, i * 0.06f, () =>
                {
                    if (_moneyIcon != null) Juice.Punch(_moneyIcon, 0.25f, 0.15f);
                });
            }
        }

        // =====================================================================
        //  ПАУЗА
        // =====================================================================
        public void SetPaused(bool p)
        {
            if (_ended) return;
            _paused = p;
            if (_pauseOverlay != null) _pauseOverlay.SetActive(p);
            if (p) Platform.GameplayStop(); else Platform.GameplayStart();
        }

        // =====================================================================
        //  UI: улица, HUD, прилавок, баннер, пауза
        // =====================================================================
        void BuildStreet()
        {
            _street = UIKit.Rect(_root, "Street");
            UIKit.Place(_street, 0, 0.5f, 1, 0.93f);
            Scenery.Build(_street, _loc, Rain);

            _nightTint = UIKit.Panel(_street, "NightTint", new Color(0, 0, 0, 0), 0, 0, 1, 1, false);
            if (_loc.scene != "mall")
            {
                float[] lampX = { 0.2575f, 0.5025f, 0.7475f };
                foreach (var lx in lampX)
                {
                    UIKit.Shape(_street, UIKit.RoundedSmall, UIKit.Hex("#37474F"), lx, 0.33f, 9f, 290f);
                    UIKit.Shape(_street, UIKit.RoundedSmall, UIKit.Hex("#37474F"), lx, 0.66f, 44f, 12f);
                    _lampGlows.Add(UIKit.Shape(_street, UIKit.Circle, new Color(1f, 0.85f, 0.4f, 0f), lx, 0.63f, 170f, 120f));
                    _lampGlows.Add(UIKit.Shape(_street, UIKit.Circle, new Color(1f, 0.95f, 0.7f, 0f), lx, 0.64f, 30f, 22f));
                }
            }

            _slots = new RectTransform[GameData.MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                var s = UIKit.Rect(_street, "Slot" + i);
                float x0 = 0.02f + i * 0.245f;
                UIKit.Place(s, x0, 0.03f, x0 + 0.23f, 0.9f);
                int idx = i;
                UIKit.HitArea(s, "Tap", () => OnCustomer(idx));
                _slots[i] = s;
            }

            var awning = UIKit.Rect(_root, "Awning");
            UIKit.Place(awning, 0, 0.895f, 1, 0.935f);
            var kioskCol = UIKit.Hex(_loc.kiosk);
            for (int i = 0; i < 16; i++)
            {
                var col = Meta.Stripe(_save, _loc, i);
                UIKit.Panel(awning, "Stripe", col, i / 16f, 0.3f, (i + 1) / 16f, 1f, false);
                UIKit.Shape(awning, UIKit.Circle, col, (i + 0.5f) / 16f, 0.3f, 120f, 34f);
            }
            UIKit.Panel(_root, "PostL", UIKit.Wood, 0, 0.5f, 0.008f, 0.93f, false);
            UIKit.Panel(_root, "PostR", UIKit.Wood, 0.992f, 0.5f, 1f, 0.93f, false);

        }

        void BuildHud()
        {
            var hud = UIKit.Panel(_root, "HUD", UIKit.Dark, 0, 0.93f, 1, 1, false);
            _dayText = UIKit.Label(hud.transform, "", 30, UIKit.Cream, TextAnchor.MiddleLeft, 0.01f, 0.42f, 0.12f, 1, true);
            _eventText = UIKit.Label(hud.transform, "", 22, UIKit.Hex("#9FD3FF"), TextAnchor.MiddleLeft, 0.01f, 0f, 0.12f, 0.48f, true);
            var qb = UIKit.Button(hud.transform, "", UIKit.Purple, ShowQuests, 0.125f, 0.12f, 0.265f, 0.88f, 26);
            _questBtn = qb.transform;
            _questText = qb.GetComponentInChildren<Text>();

            UIKit.CircleImg(hud.transform, "Clock", UIKit.Cream, 0.29f, 0.5f, 44f);
            var hand = UIKit.Shape(hud.transform, UIKit.RoundedSmall, UIKit.Dark, 0.29f, 0.5f, 5f, 17f);
            hand.rectTransform.pivot = new Vector2(0.5f, 0f);
            _clockHand = hand.rectTransform;
            _clockText = UIKit.Label(hud.transform, "", 36, UIKit.Cream, TextAnchor.MiddleLeft, 0.305f, 0, 0.42f, 1, true);

            // цель дня
            _goalFill = UIKit.Bar(hud.transform, UIKit.Alpha(Color.white, 0.15f), UIKit.Green, 0.43f, 0.22f, 0.56f, 0.78f);
            _goalText = UIKit.FitLabel(hud.transform, "", 24, Color.white, 0.43f, 0.22f, 0.56f, 0.78f);
            BuildBoostIcons(hud.transform);

            // рейтинг ларька
            Icons.Star(hud.transform, 0.705f, 0.5f, 40f, true);
            _repText = UIKit.Label(hud.transform, "", 30, UIKit.Gold, TextAnchor.MiddleLeft, 0.72f, 0, 0.78f, 1, true);

            _comboText = UIKit.Label(hud.transform, "", 30, UIKit.Orange, TextAnchor.MiddleCenter, 0.625f, 0, 0.69f, 1, true);

            _moneyIcon = Icons.Coin(hud.transform, 0.8f, 0.5f, 50f);
            _moneyText = UIKit.Label(hud.transform, "", 40, UIKit.Gold, TextAnchor.MiddleLeft, 0.82f, 0, 0.93f, 1, true);

            UIKit.Button(hud.transform, "II", UIKit.Steel, () => SetPaused(true), 0.938f, 0.1f, 0.992f, 0.9f, 34);
        }

        void BuildCounter()
        {
            var counter = UIKit.Panel(_root, "Counter", UIKit.Wood, 0, 0.46f, 1, 0.5f, false);
            UIKit.Panel(counter.transform, "Edge", UIKit.WoodLight, 0, 0.75f, 1, 1, false);
            UIKit.AddShadow(counter, 8f, 0.35f);
            // слева — подсказка (лампочка), справа — сообщения
            var bulb = UIKit.Shape(counter.transform, UIKit.Circle, UIKit.Gold, 0.012f, 0.4f, 24f, 24f);
            _hintBulb = bulb.gameObject;
            _hintText = UIKit.Label(counter.transform, "", 24, UIKit.Hex("#FFE9A8"), TextAnchor.MiddleLeft, 0.025f, 0, 0.52f, 0.78f, true);
            _hintText.verticalOverflow = VerticalWrapMode.Truncate;
            _hintText.resizeTextForBestFit = true;
            _hintText.resizeTextMinSize = 12;
            _hintText.resizeTextMaxSize = 24;
            _toastText = UIKit.Label(counter.transform, "", 26, UIKit.Cream, TextAnchor.MiddleRight, 0.5f, 0, 0.985f, 0.78f, true);
        }

        void BuildBanner()
        {
            var b = UIKit.Panel(_root, "Banner", new Color(0.12f, 0.08f, 0.05f, 0.88f), 0.22f, 0.58f, 0.78f, 0.86f);
            UIKit.AddShadow(b, 10f, 0.4f);
            _banner = b.rectTransform;
            _bannerText = UIKit.Label(b.transform, "", 54, UIKit.Cream, TextAnchor.MiddleCenter, 0.03f, 0.05f, 0.97f, 0.95f, true);
            _banner.gameObject.SetActive(false);
        }

        void ShowBanner(string text, float dur)
        {
            _bannerText.text = text;
            _bannerT = dur;
            _banner.gameObject.SetActive(true);
            Juice.Punch(_banner, 0.12f, 0.3f);
        }

        void UpdateBanner(float dt)
        {
            if (_bannerT <= 0f) return;
            _bannerT -= dt;
            if (_bannerT <= 0f) _banner.gameObject.SetActive(false);
        }

        void BuildPause()
        {
            var ov = UIKit.Panel(_root, "Pause", new Color(0, 0, 0, 0.7f), 0, 0, 1, 1, false);
            ov.raycastTarget = true;
            var box = UIKit.Panel(ov.transform, "Box", UIKit.Cream, 0.33f, 0.28f, 0.67f, 0.72f);
            UIKit.AddShadow(box, 12f, 0.4f);
            UIKit.Label(box.transform, L.T("Пауза", "Paused"), 70, UIKit.Dark, TextAnchor.MiddleCenter, 0, 0.76f, 1, 0.96f, true);
            UIKit.Button(box.transform, L.T("Продолжить", "Continue"), UIKit.Green, () => SetPaused(false), 0.1f, 0.5f, 0.9f, 0.7f, 48);
            UIKit.Button(box.transform, L.T("На карту (день заново)", "Map (restart day)"), UIKit.Red, () => _game.ExitDayToMap(), 0.1f, 0.26f, 0.9f, 0.44f, 36);
            UIKit.Label(box.transform, L.T("Деньги этого дня не сохранятся", "Money from this day won't be saved"), 24, UIKit.Gray, TextAnchor.MiddleCenter, 0.05f, 0.06f, 0.95f, 0.2f);
            _pauseOverlay = ov.gameObject;
            _pauseOverlay.SetActive(false);
        }

        void RefreshHud()
        {
            _dayText.text = L.T("День ", "Day ") + _day;
            string ev = "";
            if (Rain) ev = L.T("Дождь", "Rain");
            if (_event == DayEvent.Rush) ev = RushNow ? L.T("ЧАС ПИК!", "RUSH HOUR!") : L.T("Час пик скоро", "Rush later");
            if (_hypeT > 0f) ev = L.T("Хайп! ", "Hype! ") + Mathf.CeilToInt(_hypeT);
            if (_rivalT > 0f) ev = L.T("Акция конкурента! ", "Rival promo! ") + Mathf.CeilToInt(_rivalT);
            if (_queue.Count > 0) ev = "<color=" + (_queue.Count >= 3 ? "#FF5252" : "#FFE082") + ">" + L.T("Очередь: ", "Queue: ") + _queue.Count + "</color>  " + ev;
            _eventText.text = ev;
            _questText.text = L.T("Задания ", "Quests ") + QuestsDone() + "/" + _quests.Count;

            if (_timeLeft > 0f)
            {
                float k = Mathf.Clamp01(_elapsed / _dayDuration);
                int minutes = 600 + Mathf.FloorToInt(k * 720f / 10f) * 10;
                _clockText.text = (minutes / 60) + ":" + (minutes % 60).ToString("00");
                _clockHand.localRotation = Quaternion.Euler(0, 0, -k * 360f * 3f);
            }
            else _clockText.text = L.T("Закрыто", "Closed");

            int got = Earned + Tips;
            UIKit.SetFill(_goalFill, (float)got / _goal);
            _goalFill.GetComponent<Image>().color = got >= _goal ? UIKit.Gold : UIKit.Green;
            _goalText.text = L.T("Цель ", "Goal ") + got + " / " + _goal;

            _moneyText.text = _save.money.ToString();
            _comboText.text = _feverT > 0f ? L.T("ЖАРА ×2 ", "FEVER ×2 ") + Mathf.CeilToInt(_feverT)
                : (_combo >= 2 ? "×" + GameData.ComboMult(_combo).ToString("0.0") : "");
            _comboText.color = _feverT > 0f ? (Mathf.PingPong(Time.time * 6f, 1f) > 0.5f ? UIKit.Gold : UIKit.Red) : UIKit.Orange;

            string hint = Hint();
            _hintBulb.SetActive(hint.Length > 0);
            _hintText.text = hint;
            _repText.text = _save.rep.ToString("0.0");
        }

        public void Toast(string msg)
        {
            if (_toastText == null) return;
            if (_toastText.text == msg && _toastT > 1.2f) return;
            _toastText.text = msg;
            _toastT = 2.2f;
            _toastText.color = UIKit.Cream;
            Juice.Punch(_toastText.transform, 0.08f, 0.2f);
        }

        void UpdateToast(float dt)
        {
            if (_toastT <= 0f) return;
            _toastT -= dt;
            var c = _toastText.color;
            c.a = Mathf.Clamp01(_toastT / 0.5f);
            _toastText.color = c;
            if (_toastT <= 0f) _toastText.text = "";
        }
    }
}
