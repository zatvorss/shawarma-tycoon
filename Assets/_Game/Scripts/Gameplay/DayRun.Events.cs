using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// События дня: хит дня, курьеры бизнесов (оптовые заказы), кот у ларька,
    /// пожар на гриле, значки активных бустеров.
    /// </summary>
    public partial class DayRun
    {

        // --- курьеры ---
        readonly List<float> _courierAt = new List<float>();
        readonly List<int> _courierBiz = new List<int>();
        public int CourierIncome { get; private set; }

        // --- конкурент и мухи ---
        RectTransform _fly, _trapHead, _zapper;
        BinView _flyBin;
        float _flyT, _nextFly = -1f, _trapCd;
        int _flyHits, _flyLvl, _flyFate; // 0 — бить вручную, 1 — съест мухоловка, 2 — сгорит в УФ-ловушке

        // --- босс и жара ---
        float _bossAt = -1f, _feverT;
        GameObject _feverFrame;

        // --- кот ---
        RectTransform _cat;
        float _catT, _nextCat = -1f;
        int _catsLeft;

        // =====================================================================
        void InitEvents()
        {
            // курьеры: до 2 купленных бизнесов пришлют оптовый заказ
            int cl = _save.biz[Meta.BizCourier];
            if (cl > 0)
                for (int k = 0; k < Meta.CourierCount(cl); k++)
                {
                    _courierBiz.Add(Meta.BizCourier);
                    _courierAt.Add(Random.Range(0.12f, 0.78f) * _dayDuration);
                }
            // тандыр-пекарня
            _bakeLvl = _save.biz[Meta.BizBakery];
            _bakeT = 0f;
            if (_bakeLvl > 0)
                for (int k = 0; k < (_bakeLvl >= 3 ? 2 : 1); k++) _bulkAt.Add(Random.Range(0.15f, 0.8f) * _dayDuration);

            // босс: каждый 5-й день приходит сам Мистер Мега
            if (_day % 5 == 0 && GameData.BossOn(_save.location)) _bossAt = Random.Range(0.3f, 0.5f) * _dayDuration;

            // мухи
            _flyLvl = _save.Lvl(Up.Flies);
            if (GameData.FliesOn(_save.location)) _nextFly = Random.Range(12f, 20f);

            // кот
            if (GameData.CatOn(_save.location))
            {
                _catsLeft = Meta.Has(_save, Meta.CharmBowl) ? 3 : 2;
                _nextCat = Random.Range(0.1f, 0.45f) * _dayDuration;
            }
        }

        /// <summary>Строки «Сегодня:» для карточки начала дня: текст и цвет маркера.</summary>
        void EventLines(List<string> text, List<string> col)
        {
            if (_bossAt >= 0f) { text.Add(L.T("БОСС-ДЕНЬ: придёт сам Мистер Мега!", "BOSS DAY: Mr. Mega himself is coming!")); col.Add("#FFD54F"); }
            if (_bakeLvl > 0) { text.Add(L.T("Тандыр горячий: шаурма на свежем лаваше +", "The tandoor is hot: shawarma on fresh lavash +") + Mathf.RoundToInt(Meta.BakeBonus(_bakeLvl) * 100f) + "%"); col.Add("#FFB74D"); }
            if (_bulkAt.Count > 0) { text.Add(L.T("Конкуренты придут за лавашом оптом", "Rivals will come to buy lavash in bulk")); col.Add("#FF8A65"); }
            if (_save.biz[Meta.BizKiosk2] > 0) { text.Add(L.T("Второй ларёк работает: на складе порций — ", "Second kiosk is open: portions in stock — ") + _save.kiosk2Stock); col.Add("#A5D6A7"); }
            if (_courierBiz.Count > 0) { text.Add(L.T("Сегодня приедут курьеры за оптовыми заказами", "Couriers will come for bulk orders today")); col.Add("#81C784"); }
        }

        void TickEvents(float dt)
        {
            // курьеры
            for (int i = _courierAt.Count - 1; i >= 0; i--)
            {
                if (_elapsed < _courierAt[i] || _timeLeft <= 0f) continue;
                if (SpawnCourier(_courierBiz[i]))
                {
                    _courierAt.RemoveAt(i);
                    _courierBiz.RemoveAt(i);
                }
            }

            // кот
            if (_cat != null)
            {
                _catT -= dt;
                if (_catT <= 0f)
                {
                    Destroy(_cat.gameObject);
                    _cat = null;
                    ScheduleCat();
                }
            }
            else if (_nextCat >= 0f && _elapsed >= _nextCat && _timeLeft > 0f)
            {
                _nextCat = -1f;
                SpawnCat();
            }

            // босс
            if (_bossAt >= 0f && _elapsed >= _bossAt && _timeLeft > 0f)
            {
                if (SpawnBoss()) _bossAt = -1f;
            }


            // мухи
            if (_trapCd > 0f) _trapCd -= dt;
            if (_fly != null)
            {
                _flyT -= dt;
                if (_flyT <= 0f)
                {
                    if (_flyFate == 1) TrapEats();
                    else if (_flyFate == 2) ZapFly();
                    else FlySpoils();
                }
            }
            else if (_nextFly >= 0f && _timeLeft > 0f)
            {
                _nextFly -= dt;
                if (_nextFly <= 0f) SpawnFly();
            }

            TickBakery(dt);

            // спринклер тушит сам
            if (_save.Lvl(Up.Fire) >= 2 && _grill != null)
                foreach (var g in _grill)
                    if (g.onFire)
                    {
                        g.sprinkT += dt;
                        if (g.sprinkT >= 3f)
                        {
                            Juice.Burst(g.view, UIKit.Hex("#4FC3F7"), 14, 0.5f, 0.9f, 12f);
                            PutOut(g, L.T("Спринклер потушил пожар!", "The sprinkler put out the fire!"));
                        }
                    }

            // пожар портит рейтинг
            if (AnyFire()) _save.rep = Mathf.Clamp(_save.rep - 0.02f * dt, 1f, 5f);
        }

        // =====================================================================
        //  КУРЬЕРЫ БИЗНЕСОВ
        // =====================================================================
        bool SpawnCourier(int biz)
        {
            var free = new List<int>();
            for (int i = 0; i < _customers.Length; i++) if (_customers[i] == null && SlotOpen(i)) free.Add(i);
            if (free.Count == 0) return false;
            int slot = free[Random.Range(0, free.Count)];
            int lvl = _save.biz[biz];
            int qty = Mathf.Clamp(2 + lvl / 3, 2, 5);
            float patience = _patience * 2.2f;
            var c = new Customer(_slots[slot], CustType.Regular, MakeOrder(), Drink.None, patience, ServeMode.ToGo, true, false, biz, qty, false);
            _customers[slot] = c;
            c.Say(L.T("Заказ для «", "Order for “") + Meta.Businesses[biz].Name + L.T("»!", "”!"), 3f);
            ShowBanner(L.T("Курьер от бизнеса «", "Courier from “") + Meta.Businesses[biz].Name + L.T("»", "”") +
                "\n<size=30>" + L.T("Собери ", "Make ") + qty + L.T(" одинаковых шаурмы с собой — получишь +", " identical to-go shawarmas — get +") +
                GameData.Money(CourierPay()) + "</size>", 3f);
            Sfx.Play(Sound.Fanfare);
            return true;
        }

        // =====================================================================
        //  МУХИ: садятся на лоток — прихлопни за 4 секунды, иначе испортят 2 порции
        // =====================================================================
        /// <summary>Мухоловка и УФ-ловушка на прилавке (улучшение «Мухи» 3 и 4 ур.).</summary>
        void BuildEventProps()
        {
            if (_flyLvl == 2)
            {
                var plant = UIKit.Rect(_root, "Flytrap");
                UIKit.PlaceFixed(plant, 0.035f, 0.535f, 90f, 110f);
                UIKit.Shape(plant, UIKit.RoundedSmall, UIKit.Hex("#BF360C"), 0.5f, 0.12f, 60f, 34f);
                UIKit.Shape(plant, UIKit.RoundedSmall, UIKit.Hex("#2E7D32"), 0.5f, 0.4f, 8f, 50f);
                _trapHead = UIKit.Rect(plant, "Head");
                UIKit.PlaceFixed(_trapHead, 0.5f, 0.72f, 70f, 60f);
                _trapHead.pivot = new Vector2(0.5f, 0f);
                _trapHead.anchoredPosition = new Vector2(0f, -30f);
                UIKit.Shape(_trapHead, UIKit.Circle, UIKit.Hex("#43A047"), 0.5f, 0.6f, 66f, 44f);
                UIKit.Shape(_trapHead, UIKit.Circle, UIKit.Hex("#E53935"), 0.5f, 0.6f, 48f, 26f);
                for (int t = 0; t < 5; t++) UIKit.Shape(_trapHead, UIKit.RoundedSmall, Color.white, 0.25f + t * 0.125f, 0.88f, 5f, 12f);
                var sway = _trapHead.gameObject.AddComponent<Pendulum>();
                sway.angle = 8f;
                sway.speed = 1.5f;
            }
            if (_flyLvl >= 3)
            {
                _zapper = UIKit.Rect(_root, "Zapper");
                UIKit.PlaceFixed(_zapper, 0.965f, 0.54f, 70f, 70f);
                UIKit.Shape(_zapper, UIKit.RoundedSmall, UIKit.Hex("#311B92"), 0.5f, 0.5f, 64f, 64f);
                var glow = UIKit.Shape(_zapper, UIKit.RoundedSmall, UIKit.Hex("#B388FF"), 0.5f, 0.5f, 48f, 48f);
                glow.gameObject.AddComponent<Blinker>().speed = 2f;
                for (int t = 0; t < 4; t++) UIKit.Shape(_zapper, null, UIKit.Hex("#311B92"), 0.25f + t * 0.17f, 0.5f, 3f, 48f);
            }
            if (_save.Lvl(Up.Fire) >= 2)
            {
                for (int k = 0; k < 2; k++)
                {
                    UIKit.Shape(_root, UIKit.RoundedSmall, UIKit.Hex("#B0BEC5"), 0.81f + k * 0.05f, 0.455f, 26f, 10f);
                    UIKit.Shape(_root, UIKit.Circle, UIKit.Hex("#E53935"), 0.81f + k * 0.05f, 0.448f, 10f, 10f);
                }
            }
        }

        // =====================================================================
        //  МУХИ: садятся на лоток. Ур.1 — 3 удара, ур.2 — мухобойка (1 удар),
        //  ур.3 — мухоловка ест муху раз в 30 сек, ур.4 — УФ-ловушка сжигает всех.
        // =====================================================================
        void SpawnFly()
        {
            var options = new List<BinView>();
            foreach (var b in _bins) if (b != null && !b.locked && _stock[(int)b.id] > 0) options.Add(b);
            if (options.Count == 0) { _nextFly = 5f; return; }
            _flyBin = options[Random.Range(0, options.Count)];
            _fly = UIKit.Rect(_flyBin.root, "Fly");
            UIKit.PlaceFixed(_fly, 0.5f, 0.6f, 90f, 90f);
            var body = UIKit.Rect(_fly, "Body");
            UIKit.PlaceFixed(body, 0.5f, 0.5f, 60f, 60f);
            UIKit.Shape(body, UIKit.Circle, new Color(0.7f, 0.9f, 1f, 0.7f), 0.3f, 0.7f, 26f, 18f, 30f).gameObject.AddComponent<Blinker>().speed = 12f;
            UIKit.Shape(body, UIKit.Circle, new Color(0.7f, 0.9f, 1f, 0.7f), 0.7f, 0.7f, 26f, 18f, -30f).gameObject.AddComponent<Blinker>().speed = 12f;
            UIKit.Shape(body, UIKit.Circle, UIKit.Hex("#212121"), 0.5f, 0.45f, 30f, 36f);
            UIKit.Shape(body, UIKit.Circle, UIKit.Hex("#D32F2F"), 0.38f, 0.62f, 10f, 10f);
            UIKit.Shape(body, UIKit.Circle, UIKit.Hex("#D32F2F"), 0.62f, 0.62f, 10f, 10f);
            var bob = body.gameObject.AddComponent<Bobber>();
            bob.amp = 6f;
            bob.speed = 14f;
            var ring = UIKit.Shape(_fly, UIKit.Circle, new Color(1f, 0.3f, 0.3f, 0.25f), 0.5f, 0.5f, 90f, 90f);
            ring.gameObject.AddComponent<Blinker>().speed = 4f;
            ring.transform.SetAsFirstSibling();
            _flyHits = 0;

            if (_flyLvl >= 3) { _flyFate = 2; _flyT = 0.7f; }
            else if (_flyLvl == 2 && _trapCd <= 0f) { _flyFate = 1; _flyT = 1.2f; }
            else
            {
                _flyFate = 0;
                _flyT = 4f;
                var hit = UIKit.HitArea(_fly, "Swat", SwatFly);
                hit.transform.SetAsLastSibling();
                int need = GameData.FlyHits(_flyLvl);
                Toast(L.T("Муха на лотке «", "A fly on the “") + GameData.Get(_flyBin.id).Name + L.T("»! ", "”! ") +
                      (need > 1 ? L.T("Бей 3 раза!", "Hit it 3 times!") : L.T("Прихлопни!", "Swat it!")));
            }
            Sfx.Play(Sound.Spawn);
        }

        void SwatFly()
        {
            if (_fly == null || _paused) return;
            _flyHits++;
            int need = GameData.FlyHits(_flyLvl);
            if (_flyLvl >= 1) SwatterAnim(_fly.position);
            Sfx.Play(Sound.Chop);
            if (_flyHits < need)
            {
                // муха увёртывается
                _fly.anchoredPosition = new Vector2(Random.Range(-25f, 25f), Random.Range(-15f, 15f));
                Juice.Shake(_fly, 10f, 0.15f);
                Juice.FloatText(_flyBin.root, _flyHits + "/" + need, UIKit.Orange, 26, 0.5f, 0.95f);
                return;
            }
            Juice.FloatText(_flyBin.root, L.T("Шлёп!", "Splat!"), UIKit.Green, 30, 0.5f, 0.9f);
            ChangeRep(0.02f);
            KillFly();
        }

        void KillFly()
        {
            if (_fly != null) Destroy(_fly.gameObject);
            _fly = null;
            _nextFly = Random.Range(12f, 22f) * (_eday >= 8 ? 0.7f : 1f);
        }

        void SwatterAnim(Vector3 at)
        {
            var sw = UIKit.Rect(_fx, "Swatter");
            UIKit.PlaceFixed(sw, 0.5f, 0.5f, 90f, 150f);
            UIKit.Shape(sw, UIKit.RoundedSmall, UIKit.Hex("#8D6E63"), 0.5f, 0.2f, 12f, 90f);
            var net = UIKit.Shape(sw, UIKit.RoundedSmall, UIKit.Hex("#E53935"), 0.5f, 0.75f, 80f, 70f);
            for (int k = 0; k < 3; k++) UIKit.Shape(net.transform, null, UIKit.Alpha(Color.black, 0.3f), 0.25f + k * 0.25f, 0.5f, 3f, 60f);
            sw.localRotation = Quaternion.Euler(0, 0, -25f);
            Juice.Fly(sw, at + new Vector3(80f, 160f, 0f), at + new Vector3(0f, 40f, 0f), 0.12f, 0f, null);
        }

        void TrapEats()
        {
            var fly = _fly;
            _fly = null;
            if (fly != null && _trapHead != null)
            {
                fly.SetParent(_fx, true);
                Juice.Fly(fly, fly.position, _trapHead.position, 0.35f, 0f, () =>
                {
                    if (_trapHead != null) Juice.Punch(_trapHead, 0.35f, 0.3f);
                    Sfx.Play(Sound.Chop);
                });
            }
            else if (fly != null) Destroy(fly.gameObject);
            Toast(L.T("Мухоловка поймала муху! Ням!", "The flytrap caught the fly! Yum!"));
            _trapCd = 30f;
            _nextFly = Random.Range(12f, 22f);
        }

        void ZapFly()
        {
            if (_fly != null && _flyLvl >= 4)
            {
                int coins = 10 * _eday;
                _save.money += coins;
                _save.lifetime += coins;
                Juice.FloatText(_flyBin.root, "+" + coins, UIKit.Gold, 28, 0.5f, 0.95f);
            }
            if (_fly != null)
            {
                Juice.Burst(_flyBin.root, UIKit.Hex("#B388FF"), 12, 0.5f, 0.6f, 12f);
                if (_zapper != null) Juice.Punch(_zapper, 0.3f, 0.2f);
                Sfx.Play(Sound.Burn);
            }
            KillFly();
        }

        void FlySpoils()
        {
            int i = (int)_flyBin.id;
            int lost = Mathf.Min(2, _stock[i]);
            _stock[i] -= lost;
            RefreshBin(_flyBin.id);
            Juice.Shake(_flyBin.root, 8f, 0.3f);
            Toast(L.T("Муха испортила: ", "The fly spoiled: ") + GameData.Get(_flyBin.id).Name + " −" + lost);
            Sfx.Play(Sound.Error);
            KillFly();
        }

        // =====================================================================
        //  БОСС: МИСТЕР МЕГА
        // =====================================================================
        bool SpawnBoss()
        {
            int slot = FreeSlot();
            if (slot < 0) return false;
            var order = MakeOrder(CustType.Critic);
            for (int t = 0; t < 10 && GameData.Total(order) < 4; t++) order = MakeOrder(CustType.Critic);
            var c = new Customer(_slots[slot], CustType.Business, order, Drink.None, 70f, ServeMode.ToGo, true, false, -1, 3, false, true);
            _customers[slot] = c;
            c.Say(L.T("Ну-ка, покажи, на что способен твой ларёк!", "Let's see what your little kiosk can do!"), 3f);
            ShowBanner(L.T("БОСС: МИСТЕР МЕГА!\n<size=30>Собери ему 3 одинаковые шаурмы с собой за 70 секунд — получишь огромную награду и очко славы!</size>",
                           "BOSS: MR. MEGA!\n<size=30>Make him 3 identical to-go shawarmas in 70 seconds — huge reward and a Fame point!</size>"), 3.4f);
            Sfx.Play(Sound.Burn);
            Juice.Shake(_root, 14f, 0.5f);
            return true;
        }

        // =====================================================================
        //  ЖАРА: каждые 5 комбо — 20 секунд двойных денег (пока жара идёт, новая не продлевает её)
        // =====================================================================
        const float FeverTime = 20f;

        void StartFever()
        {
            if (_feverT > 0f) return;
            _feverT = FeverTime;
            if (_feverFrame == null)
            {
                var f = UIKit.Full(_root, "Fever");
                f.SetSiblingIndex(_fx.GetSiblingIndex());
                var col = new Color(1f, 0.45f, 0.1f, 0.85f);
                UIKit.Panel(f, "T", col, 0f, 0.985f, 1f, 1f, false);
                UIKit.Panel(f, "B", col, 0f, 0f, 1f, 0.012f, false);
                UIKit.Panel(f, "L", col, 0f, 0f, 0.007f, 1f, false);
                UIKit.Panel(f, "R", col, 0.993f, 0f, 1f, 1f, false);
                foreach (var img in f.GetComponentsInChildren<Image>()) img.gameObject.AddComponent<Blinker>().speed = 5f;
                _feverFrame = f.gameObject;
            }
            _feverFrame.SetActive(true);
            ShowBanner(L.T("ЖАРА! Все деньги ×2 на 20 секунд!", "FEVER! All money ×2 for 20 seconds!"), 1.6f);
            Sfx.Play(Sound.Combo);
            Sfx.Play(Sound.Fanfare);
        }

        void TickFever(float dt)
        {
            if (_feverT <= 0f) return;
            _feverT -= dt;
            if (_feverT <= 0f && _feverFrame != null) _feverFrame.SetActive(false);
        }

        // =====================================================================
        //  КУРЬЕРСКАЯ СЛУЖБА
        // =====================================================================
        int CourierPay()
        {
            int l = _save.biz[Meta.BizCourier];
            return Mathf.RoundToInt(1500f * Meta.CourierPayMult(l) * _loc.price / 10f) * 10;
        }

        // =====================================================================
        //  ТАНДЫР-ПЕКАРНЯ: горячий лаваш печётся сам, шаурма на нём дороже
        // =====================================================================
        int _bakeLvl;
        float _bakeT;
        readonly List<float> _hotLavash = new List<float>();  // сколько секунд ещё горячий
        bool _curFresh;
        RectTransform _tandoor, _tandoorFill;
        Text _tandoorText;

        void BuildTandoor(Transform cell)
        {
            if (_bakeLvl <= 0) return;
            _tandoor = UIKit.Rect(cell, "Tandoor");
            UIKit.Place(_tandoor, 0f, 0f, 1f, 1f);
            UIKit.Shape(_tandoor, UIKit.Circle, UIKit.Hex("#BF360C"), 0.83f, 0.8f, 40f, 34f);
            UIKit.Shape(_tandoor, UIKit.Circle, UIKit.Hex("#FF6F00"), 0.83f, 0.78f, 22f, 16f).gameObject.AddComponent<Blinker>().speed = 2f;
            _tandoorFill = UIKit.Bar(_tandoor, UIKit.Alpha(Color.black, 0.3f), UIKit.Orange, 0.08f, 0.26f, 0.92f, 0.33f);
            _tandoorText = UIKit.FitLabel(_tandoor, "", 20, UIKit.Red, 0.02f, 0.62f, 0.62f, 0.98f);
            RefreshTandoor();
        }

        void TickBakery(float dt)
        {
            if (_bakeLvl <= 0) return;
            bool changed = false;
            for (int i = _hotLavash.Count - 1; i >= 0; i--)
            {
                _hotLavash[i] -= dt;
                if (_hotLavash[i] <= 0f)
                {
                    _hotLavash.RemoveAt(i);
                    changed = true;
                    Toast(L.T("Лаваш из тандыра остыл — бонуса за него уже не будет", "The tandoor lavash cooled down — no bonus for it now"));
                }
            }
            // оптовики: пока ждёт покупатель — тандыр работает на полную
            bool bulk = false;
            foreach (var c in _customers) if (c != null && !c.leaving && c.IsBulk) bulk = true;
            float bakeTime = Meta.BakeTime(_bakeLvl) * (bulk ? 0.25f : 1f);
            int cap = bulk ? 5 : Meta.BakeCap(_bakeLvl);
            for (int i = _bulkAt.Count - 1; i >= 0; i--)
            {
                if (_elapsed < _bulkAt[i] || _timeLeft <= 0f) continue;
                if (SpawnBulk()) _bulkAt.RemoveAt(i);
                break;
            }
            if (_hotLavash.Count < cap && (_timeLeft > 0f || bulk))
            {
                _bakeT += dt;
                if (_bakeT >= bakeTime)
                {
                    _bakeT = 0f;
                    _hotLavash.Add(Meta.LavashHot);
                    changed = true;
                    Sfx.Play(Sound.Done);
                    if (_tandoor != null) Juice.Punch(_tandoor.parent, 0.15f, 0.25f);
                }
            }
            if (_tandoorFill != null) UIKit.SetFill(_tandoorFill, _bakeT / bakeTime);
            if (changed) RefreshTandoor();
        }

        void RefreshTandoor()
        {
            if (_tandoorText == null) return;
            _tandoorText.text = _hotLavash.Count > 0 ? L.T("горячий ×", "hot ×") + _hotLavash.Count : "";
        }

        readonly List<float> _bulkAt = new List<float>();

        /// <summary>Оптовик из соседнего кафе: берёт от 5 до 30 горячих лавашей (в среднем около 10).</summary>
        bool SpawnBulk()
        {
            int slot = FreeSlot();
            if (slot < 0) return false;
            float u = Mathf.Max(0.0001f, Random.value);
            int qty = Mathf.Clamp(5 + Mathf.RoundToInt(-Mathf.Log(u) * 5f), 5, 30);
            float patience = Mathf.Max(_patience, 30f + qty * 4.5f);
            var c = new Customer(_slots[slot], CustType.Business, GameData.NewCounts(), Drink.None, patience, ServeMode.ToGo, true, false,
                -1, 1, false, false, false, qty);
            _customers[slot] = c;
            c.Say(L.T("Нам нужно ", "We need ") + qty + L.T(" горячих лавашей!", " hot lavash!"), 3f);
            ShowBanner(L.T("Оптовый заказ лаваша!\n<size=30>Соседнее кафе берёт ", "Bulk lavash order!\n<size=30>A nearby cafe wants ") + qty +
                L.T(" шт. Тандыр печёт быстрее — жми на покупателя, чтобы отдать готовые</size>", ". The tandoor bakes faster — tap the buyer to hand over ready ones</size>"), 3f);
            Sfx.Play(Sound.Fanfare);
            return true;
        }

        /// <summary>Тап по оптовику: отдаём все горячие лаваши.</summary>
        void GiveBulk(int slot, Customer c)
        {
            int n = Mathf.Min(_hotLavash.Count, c.lavashQty);
            if (n <= 0)
            {
                Toast(L.T("Горячих лавашей пока нет — тандыр печёт", "No hot lavash yet — the tandoor is baking"));
                Sfx.Play(Sound.Error);
                return;
            }
            for (int i = 0; i < n; i++)
            {
                _hotLavash.RemoveAt(0);
                if (_tandoor != null)
                {
                    var ic = Icons.Lavash(_fx, 0.5f, 0.5f, 70f);
                    Juice.Fly(ic, _tandoor.position, c.BubbleWorld, 0.35f, i * 0.07f, null);
                }
            }
            RefreshTandoor();
            c.GiveLavash(n);
            Sfx.Play(Sound.Click);
            if (!c.Done) return;

            int per = Mathf.Max(10, Mathf.RoundToInt(GameData.LavashPrice * _priceMult * 1.2f));
            int total = per * c.lavashTotal;
            Earned += total;
            Served++;
            _save.money += total;
            ChangeRep(GameData.RepHappy);
            c.Leave(true, L.T("Отличный лаваш!", "Great lavash!"));
            LiveStats(c, total, true);
            Juice.FloatText(_slots[slot], "+" + total, UIKit.Gold, 60, 0.7f, 0.65f);
            SpawnCoins(c.BubbleWorld, 8);
            Sfx.Play(Sound.Coin);
            Progress(QuestType.Serve, 1);
            Progress(QuestType.Earn, total);
        }

        /// <summary>Берём лаваш: горячий, если есть.</summary>
        bool TakeHotLavash()
        {
            if (_bakeLvl <= 0 || _hotLavash.Count == 0) return false;
            // берём тот, что остывает быстрее всех
            int best = 0;
            for (int i = 1; i < _hotLavash.Count; i++) if (_hotLavash[i] < _hotLavash[best]) best = i;
            _hotLavash.RemoveAt(best);
            RefreshTandoor();
            return true;
        }

        // =====================================================================
        //  DRIVE-THRU: машины у окна — спешат, сигналят, платят больше
        // =====================================================================
        // =====================================================================
        //  КОТ У ЛАРЬКА
        // =====================================================================
        void ScheduleCat()
        {
            _catsLeft--;
            if (_catsLeft > 0) _nextCat = _elapsed + Random.Range(0.2f, 0.35f) * _dayDuration;
        }

        void SpawnCat()
        {
            _cat = UIKit.Rect(_root, "Cat");
            UIKit.PlaceFixed(_cat, Random.Range(0.1f, 0.9f), 0.527f, 110f, 80f);
            _cat.SetSiblingIndex(_fx.GetSiblingIndex());
            _catT = 9f;

            string[] furs = { "#FF9800", "#9E9E9E", "#3E2723", "#FFE0B2" };
            var fur = UIKit.Hex(furs[Random.Range(0, furs.Length)]);
            var dark = Color.Lerp(fur, Color.black, 0.25f);

            var tail = UIKit.Rect(_cat, "Tail");
            UIKit.PlaceFixed(tail, 0.85f, 0.35f, 14f, 50f);
            tail.pivot = new Vector2(0.5f, 0f);
            tail.anchoredPosition = Vector2.zero;
            UIKit.Shape(tail, UIKit.RoundedSmall, dark, 0.5f, 0.5f, 12f, 50f);
            var tp = tail.gameObject.AddComponent<Pendulum>();
            tp.angle = 30f;
            tp.speed = 4f;

            UIKit.Shape(_cat, UIKit.Circle, fur, 0.55f, 0.35f, 80f, 50f);
            UIKit.Shape(_cat, UIKit.RoundedSmall, UIKit.Hex("#FFFFFF60"), 0.5f, 0.28f, 40f, 12f);
            var head = UIKit.Shape(_cat, UIKit.Circle, fur, 0.25f, 0.62f, 50f, 46f);
            UIKit.Shape(_cat, UIKit.RoundedSmall, dark, 0.14f, 0.9f, 16f, 16f, 45f);
            UIKit.Shape(_cat, UIKit.RoundedSmall, dark, 0.36f, 0.9f, 16f, 16f, 45f);
            UIKit.Shape(head.transform, UIKit.Circle, UIKit.Hex("#AED581"), 0.35f, 0.55f, 10f, 12f);
            UIKit.Shape(head.transform, UIKit.Circle, UIKit.Hex("#AED581"), 0.65f, 0.55f, 10f, 12f);
            UIKit.Shape(head.transform, UIKit.Circle, UIKit.Hex("#F48FB1"), 0.5f, 0.35f, 8f, 6f);

            var bubble = UIKit.Panel(_cat, "Meow", Color.white, -0.2f, 1.0f, 0.9f, 1.45f);
            UIKit.FitLabel(bubble.transform, L.T("Мяу? (погладь)", "Meow? (pet me)"), 18, UIKit.Dark, 0.05f, 0.05f, 0.95f, 0.95f);
            bubble.gameObject.AddComponent<Bobber>().amp = 4f;

            UIKit.HitArea(_cat, "Tap", PetCat);
            Juice.Punch(_cat, 0.3f, 0.3f);
            Sfx.Play(Sound.Spawn);
        }

        void PetCat()
        {
            if (_cat == null || _paused) return;
            int reward = 15 * _eday * (Meta.Has(_save, Meta.CharmBowl) ? 2 : 1);
            _save.money += reward;
            _save.lifetime += reward;
            _save.cats++;
            _save.rep = Mathf.Clamp(_save.rep + 0.1f, 1f, 5f);
            float cx = _cat.anchorMin.x;
            Juice.FloatText(_root, L.T("Мур! +", "Purr! +") + reward, UIKit.Gold, 36, cx, 0.6f);
            Juice.Burst(_root, UIKit.Hex("#F48FB1"), 8, cx, 0.55f, 14f);
            Sfx.Play(Sound.Coin);
            _game.OnStatsChanged();
            Destroy(_cat.gameObject);
            _cat = null;
            ScheduleCat();
        }

        // =====================================================================
        //  ПОЖАР НА ГРИЛЕ
        // =====================================================================
        bool AnyFire()
        {
            if (_grill == null) return false;
            foreach (var g in _grill) if (g.onFire) return true;
            return false;
        }

        void Ignite(GrillSlot g)
        {
            g.onFire = true;
            g.fireTaps = 0;
            g.fire = UIKit.Full(g.view, "Fire");
            for (int i = 0; i < 4; i++)
            {
                var fl = UIKit.Rect(g.fire, "Flame");
                UIKit.PlaceFixed(fl, 0.2f + i * 0.2f, 0.55f, 40f, 60f);
                UIKit.Shape(fl, UIKit.Circle, UIKit.Hex("#FF5722"), 0.5f, 0.45f, 40f, 56f);
                UIKit.Shape(fl, UIKit.Circle, UIKit.Hex("#FFC107"), 0.5f, 0.35f, 24f, 34f);
                var b = fl.gameObject.AddComponent<Bobber>();
                b.amp = 6f;
                b.speed = 8f + i * 2f;
            }
            int taps = GameData.FireTaps(_save.Lvl(Up.Fire));
            var lbl = UIKit.FitLabel(g.fire, L.T("ПОЖАР! Жми ×", "FIRE! Tap ×") + taps, 24, Color.white, 0.02f, 0.02f, 0.98f, 0.24f);
            UIKit.AddOutline(lbl, UIKit.Red, 2f);
            lbl.gameObject.AddComponent<Blinker>().speed = 6f;
            ShowBanner(L.T("ПОЖАР НА ГРИЛЕ!\n<size=30>Жми на горящую плиту. Пока горит — гриль медленнее, рейтинг падает</size>",
                           "GRILL FIRE!\n<size=30>Tap the burning plate. While it burns the grill is slower and rating drops</size>"), 2.5f);
            Sfx.Play(Sound.Burn);
            Juice.Shake(_root, 10f, 0.4f);
        }

        /// <summary>Тап по горящей плите. Возвращает true, если тап ушёл на тушение.</summary>
        bool TapFire(GrillSlot g)
        {
            if (!g.onFire) return false;
            g.fireTaps++;
            Sfx.Play(Sound.Pour);
            Juice.Shake(g.view, 8f, 0.15f);
            Juice.Burst(g.view, Color.white, 6, 0.5f, 0.5f, 16f);
            if (g.fireTaps >= GameData.FireTaps(_save.Lvl(Up.Fire)))
                PutOut(g, L.T("Потушил! Гриль снова свободен", "Fire's out! Grill is free"));
            return true;
        }

        void PutOut(GrillSlot g, string msg)
        {
            g.onFire = false;
            g.sprinkT = 0f;
            if (g.fire != null) Destroy(g.fire.gameObject);
            g.fire = null;
            ClearGrill(g);
            Toast(msg);
            Sfx.Play(Sound.Done);
        }

        // =====================================================================
        //  ЗНАЧКИ АКТИВНЫХ БУСТЕРОВ В HUD
        // =====================================================================
        void BuildBoostIcons(Transform hud)
        {
            int n = 0;
            for (int b = 0; b < _boost.Length; b++)
            {
                if (!_boost[b]) continue;
                float x = 0.575f + n * 0.018f;
                var bg = UIKit.Shape(hud, UIKit.Circle, UIKit.Alpha(UIKit.Gold, 0.35f), x, 0.5f, 34f, 34f);
                if (b == (int)Boost.Speed) Icons.Cup(bg.transform, 0.5f, 0.5f, 28f);
                else if (b == (int)Boost.Ads) Icons.Flyer(bg.transform, 0.5f, 0.5f, 28f);
                else Icons.GoldLavash(bg.transform, 0.5f, 0.5f, 26f);
                bg.gameObject.AddComponent<Blinker>().speed = 1.5f;
                n++;
            }
        }
    }
}
