using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Обучение «на деле»: экран затемняется, нужный предмет подсвечивается золотой рамкой,
    /// стрелка показывает, куда нажать, а шаг засчитывается только когда игрок реально сделал действие.
    /// Пока идёт шаг — время дня стоит и клиенты не теряют терпение.
    /// Базовое обучение — в самый первый день; когда открывается новое (упаковка, гриль, фритюр, мухи...) —
    /// короткое обучение именно по новинке.
    /// </summary>
    public partial class DayRun
    {
        class TutorStep
        {
            public string key;                       // сохраняется как пройденный (null — шаг сценария)
            public string text;
            public Func<RectTransform[]> targets;    // что подсветить (null — ничего)
            public Func<bool> done;                  // когда шаг выполнен (null — кнопка «Понятно»)
            public Action onStart, onEnd;
        }

        readonly List<TutorStep> _tutorQueue = new List<TutorStep>();
        TutorStep _tutor;
        RectTransform _tutorRoot, _tutorBubble;
        Text _tutorText;
        bool _basicsRunning;
        int _tutorSlot = -1;

        bool TutorFrozen { get { return _tutor != null; } }
        float _tutorAge;
        bool _tutorNextShown;

        // =====================================================================
        //  БАЗОВОЕ ОБУЧЕНИЕ: первый день первой локации
        // =====================================================================
        bool NeedBasics { get { return _save.location == 0 && !_save.Tutored("basics"); } }

        void StartBasics()
        {
            _basicsRunning = true;
            _spawnTimer = 9999f;
            _stock[(int)Ing.Meat] = 0;
            _stock[(int)Ing.Tomato] = 0;
            RefreshAllKitchen();

            Add(null, L.T("Добро пожаловать в ларёк!\nСейчас покажу, как готовить шаурму. Делай то, что подсвечено.",
                          "Welcome to the kiosk!\nI'll show you how to make shawarma. Do what's highlighted."), null, null, null, SpawnTutorCustomer);
            Add(null, L.T("Это твой первый клиент. В пузыре — его заказ: мясо и помидор.", "This is your first customer. The bubble shows the order: meat and tomato."),
                () => Targets(_tutorSlot >= 0 ? _slots[_tutorSlot] : null), null);
            Add(null, L.T("Нарежь мясо: води пальцем по вертелу вверх-вниз, пока не наберётся порция.", "Slice the meat: swipe up and down over the spit until a portion is ready."),
                () => Targets(_spitPanel), () => _stock[(int)Ing.Meat] >= 1);
            Add(null, L.T("Перетащи помидор из ящика на разделочную доску.", "Drag a tomato from the crate onto the cutting board."),
                () => Targets(_vegCells != null ? _vegCells[(int)Ing.Tomato] : null, _chopBoard),
                () => _boardVeg == (int)Ing.Tomato || _stock[(int)Ing.Tomato] >= 1);
            Add(null, L.T("Стучи по доске, чтобы нарезать помидор.", "Tap the board to chop the tomato."),
                () => Targets(_chopBoard), () => _stock[(int)Ing.Tomato] >= 1);
            Add(null, L.T("Нажми на лаваш — он ляжет на стол для сборки.", "Tap the lavash — it goes onto the assembly table."),
                () => Targets(_lavashCell), () => _hasLavash);
            Add(null, L.T("Добавь мясо: нажми на лоток с мясом.", "Add meat: tap the meat bin."),
                () => Targets(BinRoot(Ing.Meat), _boardRt), () => _cur[(int)Ing.Meat] >= 1);
            Add(null, L.T("Теперь помидор: нажми на лоток с помидорами.", "Now the tomato: tap the tomato bin."),
                () => Targets(BinRoot(Ing.Tomato), _boardRt), () => _cur[(int)Ing.Tomato] >= 1);
            Add(null, L.T("Всё как в заказе! Жми «Завернуть».", "Just like the order! Tap “Wrap”."),
                () => Targets(_wrapShawarmaBtn), () => _tray.Count > 0);
            Add(null, L.T("Шаурма на выдаче. Нажми на клиента — отдашь ему заказ.", "The shawarma is on the tray. Tap the customer to serve."),
                () => Targets(_trayRoot, _tutorSlot >= 0 ? _slots[_tutorSlot] : null), () => Served >= 1);
            Add("basics", L.T("Отлично, ты справился!\nДальше клиенты пойдут сами. Следи за полоской терпения и за целью дня наверху.",
                              "Great job!\nNow customers will keep coming. Watch the patience bar and the daily goal at the top."),
                () => Targets(_goalFill != null ? _goalFill.parent as RectTransform : null), null, null, () =>
                {
                    _basicsRunning = false;
                    _spawnTimer = 2f;
                });
        }

        void SpawnTutorCustomer()
        {
            int slot = 1;
            var order = GameData.NewCounts();
            order[(int)Ing.Meat] = 1;
            order[(int)Ing.Tomato] = 1;
            var c = new Customer(_slots[slot], CustType.Regular, order, Drink.None, 999f, ServeMode.Here, true, false);
            _customers[slot] = c;
            c.Say(L.T("Одну шаурму, пожалуйста!", "One shawarma, please!"), 3f);
            _tutorSlot = slot;
            Sfx.Play(Sound.Spawn);
        }

        // =====================================================================
        //  ОБУЧЕНИЕ НОВИНКАМ: срабатывает, когда новое впервые появилось в игре
        // =====================================================================
        void CheckTutorTriggers()
        {
            if (_basicsRunning || _tutorQueue.Count > 0) return;

            // спросить «здесь или с собой»
            if (GameData.PackingOn(_save.location) && !_save.Tutored("ask"))
                for (int i = 0; i < _customers.Length; i++)
                {
                    var c = _customers[i];
                    if (c == null || c.leaving || c.modeKnown || !c.needShawarma) continue;
                    int si = i;
                    Add("ask", L.T("Новинка: упаковка!\nЭтот клиент не сказал, здесь он ест или с собой. Нажми на него и спроси.",
                                   "New: packing!\nThis customer didn't say — here or to go. Tap them to ask."),
                        () => Targets(_slots[si]), () => _customers[si] == null || _customers[si].modeKnown || _customers[si].leaving);
                    return;
                }
            // обёртка и пакет
            if (GameData.PackingOn(_save.location) && !_save.Tutored("pack") && _wrapBtn != null)
                foreach (var it in _tray)
                    if (!it.isDrink && it.pack == 0)
                    {
                        Add("pack", L.T("Упакуй шаурму! «Обёртка» — если едят здесь.\n«Обёртка», а потом «Пакет» — если с собой.",
                                        "Pack the shawarma! “Wrap” — for here.\n“Wrap” and then “Bag” — to go."),
                            () => Targets(_wrapBtn.transform as RectTransform, _bagBtn.transform as RectTransform, _trayRoot), () => !TrayHasUnpacked());
                        return;
                    }
            // соус кончился — показать, как долить
            if (!_save.Tutored("sauce") && !GameData.SauceAuto(_lvSauce))
                foreach (var b in _bins)
                {
                    if (b == null || b.zone == null || b.locked || b.root == null) continue;
                    int si = (int)b.id;
                    if (_stock[si] > 0) continue;
                    int need = Mathf.Min(_cap[si], 3);
                    string nm = b.id == Ing.Garlic ? L.T("Чесночный соус", "Garlic sauce") : L.T("Острый соус", "Spicy sauce");
                    Add("sauce", nm + L.T(" кончился!\nЗажми лоток с соусом пальцем и держи — бутылка сама дольёт соус. Отпусти, когда наберётся.",
                                          " ran out!\nPress and HOLD the sauce bin — the bottle refills it. Let go when it's full enough."),
                        () => Targets(b.root), () => _stock[si] >= need);
                    return;
                }
            // муха
            if (_fly != null && !_save.Tutored("fly"))
            {
                Add("fly", L.T("Муха села на лоток! Прихлопни её, пока она не испортила продукты.", "A fly landed on a bin! Swat it before it spoils the food."),
                    () => Targets(_fly), () => _fly == null);
                return;
            }
            // грязная доска
            if (_dirty && !_save.Tutored("dirt"))
            {
                Add("dirt", L.T("Доска запачкалась! Протри её — проведи пальцем по доске туда-сюда.", "The board got dirty! Wipe it — swipe back and forth over the board."),
                    () => Targets(_boardRt), () => !_dirty);
                return;
            }
            // гриль: перевернуть и снять
            if (_grillOn && _grill != null)
                for (int s = 0; s < _grill.Length; s++)
                {
                    var g = _grill[s];
                    if (g.counts == null || g.view == null) continue;
                    if (!_save.Tutored("grill") && !g.flipped && g.t < _grillTime)
                    {
                        var gg = g;
                        Add("grill", L.T("Новинка: гриль! Шаурма жарится. Когда появится «ПЕРЕВЕРНИ» — нажми на плиту.",
                                         "New: the grill! The shawarma is grilling. When “FLIP IT” appears — tap the plate."),
                            () => Targets(gg.view), () => gg.flipped || gg.counts == null);
                        return;
                    }
                    if (!_save.Tutored("grilltake") && g.t >= _grillTime && g.t < _grillTime + Burn(g))
                    {
                        var gg = g;
                        Add("grilltake", L.T("Готово! Нажми на плиту и сними шаурму. Снимешь сразу — будет «идеально».",
                                             "Ready! Tap the plate to take it off. Take it right away for a “perfect” grill."),
                            () => Targets(gg.view), () => gg.counts == null);
                        return;
                    }
                    if (!_save.Tutored("fire") && g.onFire)
                    {
                        var gg = g;
                        Add("fire", L.T("Пожар на гриле! Жми на горящую плиту, чтобы потушить.", "Grill fire! Tap the burning plate to put it out."),
                            () => Targets(gg.view), () => !gg.onFire);
                        return;
                    }
                }
            // фритюр
            if (_friesUnlocked && !GameData.HelperAuto(_lvFryLoad) && !_save.Tutored("fryload") && _fry == FryState.Empty)
            {
                Add("fryload", L.T("Новинка: фритюр! Перетащи сырую картошку из ящика во фритюр.", "New: the fryer! Drag raw fries from the crate into the fryer."),
                    () => Targets(_rawCrate, _fryBody), () => _fry != FryState.Empty);
                return;
            }
            if (_friesUnlocked && _fry == FryState.Ready && !_save.Tutored("fryunload"))
            {
                Add("fryunload", L.T("Картошка готова! Перетащи корзину в лоток «Картошка» — оттуда её кладут в шаурму и в коробки.",
                                     "Fries are ready! Drag the basket to the Fries bin — from there it goes into wraps and boxes."),
                    () => Targets(_fryBody, BinRoot(Ing.Fries)), () => _fry != FryState.Ready);
                return;
            }
            // гарнир и напитки
            for (int i = 0; i < _customers.Length; i++)
            {
                var c = _customers[i];
                if (c == null || c.leaving) continue;
                int si = i;
                if (c.WantsDrink(Drink.Fries) && !_save.Tutored("friesbox") && _boxCell != null && _stock[(int)Ing.Fries] > 0)
                {
                    Add("friesbox", L.T("Клиент хочет картошку фри отдельно. Возьми коробку и держи её над лотком «Картошка», пока не наполнится.",
                                        "The customer wants fries on the side. Take a box and hold it over the Fries bin until it fills."),
                        () => Targets(_boxCell, BinRoot(Ing.Fries)), () => TrayHas(Drink.Fries) || _customers[si] == null || !_customers[si].needDrink);
                    return;
                }
                if ((c.WantsDrink(Drink.Cola) || c.WantsDrink(Drink.Juice)) && !_save.Tutored("cup") && _cupsCell != null)
                {
                    var tap = c.WantsDrink(Drink.Cola) ? _tapCola : _tapJuice;
                    Add("cup", L.T("Клиент хочет напиток. Возьми стакан из стопки и держи под краном, пока не нальётся.",
                                   "The customer wants a drink. Take a cup and hold it under the tap until it fills."),
                        () => Targets(_cupsCell, tap), () => TrayHasDrink() || _customers[si] == null || !_customers[si].needDrink);
                    return;
                }
                if (c.IsCourier && !_save.Tutored("courier"))
                {
                    Add("courier", L.T("Курьер! Ему нужно несколько ОДИНАКОВЫХ шаурм с собой — посмотри количество в пузыре.",
                                       "A courier! They need several IDENTICAL to-go shawarmas — check the count in the bubble."),
                        () => Targets(_slots[si]), null);
                    return;
                }
                if (c.IsBulk && !_save.Tutored("bulk"))
                {
                    Add("bulk", L.T("Оптовик за лавашом. Тандыр печёт быстрее — нажимай на покупателя, чтобы отдать горячие лаваши.",
                                    "A bulk buyer for lavash. The tandoor bakes faster — tap the buyer to hand over hot lavash."),
                        () => Targets(_slots[si], _lavashCell), null);
                    return;
                }
            }
            // кот
            if (_cat != null && !_save.Tutored("cat"))
            {
                Add("cat", L.T("К ларьку пришёл кот! Погладь его — получишь монеты и рейтинг.", "A cat came to the kiosk! Pet it for coins and rating."),
                    () => Targets(_cat), () => _cat == null);
                return;
            }
        }

        bool TrayHasUnpacked() { foreach (var it in _tray) if (!it.isDrink && it.pack == 0) return true; return false; }
        bool TrayHas(Drink d) { foreach (var it in _tray) if (it.isDrink && it.drink == d) return true; return false; }
        bool TrayHasDrink() { foreach (var it in _tray) if (it.isDrink && it.drink != Drink.Fries) return true; return false; }
        RectTransform BinRoot(Ing id) { var b = BinOf(id); return b != null ? b.root : null; }
        static RectTransform[] Targets(params RectTransform[] t) { return t; }

        void Add(string key, string text, Func<RectTransform[]> targets, Func<bool> done, Action onStart = null, Action onEnd = null)
        {
            _tutorQueue.Add(new TutorStep { key = key, text = text, targets = targets, done = done, onStart = onStart, onEnd = onEnd });
        }

        // =====================================================================
        //  ПОКАЗ ШАГА
        // =====================================================================
        void TickTutor()
        {
            if (_tutor == null)
            {
                if (_tutorQueue.Count == 0 && _introT <= 0f) CheckTutorTriggers();
                if (_tutorQueue.Count == 0) return;
                _tutor = _tutorQueue[0];
                _tutorQueue.RemoveAt(0);
                BuildTutorUI();
                _tutorAge = 0f;
                _tutorNextShown = false;
                if (_tutor.onStart != null) _tutor.onStart();
                Sfx.Play(Sound.Tick);
            }
            LayoutTutor();
            // страховка: если шаг долго не получается выполнить — появляется «Дальше»
            _tutorAge += Time.unscaledDeltaTime;
            if (_tutor != null && _tutor.done != null && !_tutorNextShown && _tutorAge > 15f && _tutorBubble != null)
            {
                _tutorNextShown = true;
                var nb = UIKit.Button(_tutorBubble, L.T("Дальше →", "Next →"), UIKit.Steel, FinishTutor, 0.74f, -0.3f, 0.99f, -0.04f, 24);
                Juice.Punch(nb.transform as RectTransform, 0.2f, 0.3f);
            }
            if (_tutor != null && _tutor.done != null && _tutor.done()) FinishTutor();
        }

        void FinishTutor()
        {
            var t = _tutor;
            _tutor = null;
            if (_tutorRoot != null) Destroy(_tutorRoot.gameObject);
            _tutorRoot = null;
            if (t.key != null) { _save.MarkTutored(t.key); SaveSystem.Save(_save); }
            Sfx.Play(Sound.Done);
            if (t.onEnd != null) t.onEnd();
        }

        void SkipAllTutor()
        {
            foreach (var k in new[] { "basics", "ask", "pack", "fly", "dirt", "grill", "grilltake", "fire", "fryload", "fryunload", "friesbox", "cup", "courier", "bulk", "cat", "sauce" })
                _save.MarkTutored(k);
            SaveSystem.Save(_save);
            _tutorQueue.Clear();
            bool basics = _basicsRunning;
            var t = _tutor;
            _tutor = null;
            if (_tutorRoot != null) Destroy(_tutorRoot.gameObject);
            _tutorRoot = null;
            if (basics)
            {
                _basicsRunning = false;
                _spawnTimer = 1f;
                if (_tutorSlot < 0) SpawnTutorCustomer();
                foreach (var v in Choppables) _stock[(int)v] = Mathf.Max(_stock[(int)v], 1);
                _stock[(int)Ing.Meat] = Mathf.Max(_stock[(int)Ing.Meat], 1);
                RefreshAllKitchen();
            }
        }

        // пулы элементов подсветки: несколько отдельных «окон», у каждого своя рамка и своя стрелка
        readonly List<Image> _shadePool = new List<Image>();
        readonly List<RectTransform> _framePool = new List<RectTransform>();
        readonly List<RectTransform> _arrowPool = new List<RectTransform>();

        void BuildTutorUI()
        {
            _tutorRoot = UIKit.Full(_root, "Tutor");
            _tutorRoot.SetAsLastSibling();
            _shadePool.Clear();
            _framePool.Clear();
            _arrowPool.Clear();
            _tutorShadeLayer = UIKit.Full(_tutorRoot, "ShadeLayer");
            _tutorMarks = UIKit.Full(_tutorRoot, "Marks");

            // пузырь с текстом
            var bub = UIKit.Panel(_tutorRoot, "Bubble", UIKit.Hex("#2A1C13"), 0, 0, 1, 1);
            bub.raycastTarget = true;
            UIKit.AddOutline(bub, UIKit.Gold, 3f);
            UIKit.AddShadow(bub, 8f, 0.5f);
            _tutorBubble = bub.rectTransform;
            UIKit.PlaceFixed(_tutorBubble, 0.5f, 0.5f, BubbleW, BubbleH);
            var chef = UIKit.Rect(_tutorBubble, "Chef");
            UIKit.PlaceFixed(chef, 0.08f, 0.5f, 100f, 100f);
            UIKit.Shape(chef, UIKit.Circle, UIKit.Hex("#E0AC69"), 0.5f, 0.42f, 80f, 80f);
            UIKit.Shape(chef, UIKit.Circle, UIKit.Dark, 0.38f, 0.46f, 10f, 12f);
            UIKit.Shape(chef, UIKit.Circle, UIKit.Dark, 0.62f, 0.46f, 10f, 12f);
            UIKit.Shape(chef, UIKit.RoundedSmall, UIKit.Hex("#9E9E9E"), 0.5f, 0.3f, 44f, 10f);
            UIKit.Shape(chef, UIKit.Circle, Color.white, 0.35f, 0.9f, 36f, 36f);
            UIKit.Shape(chef, UIKit.Circle, Color.white, 0.65f, 0.9f, 36f, 36f);
            UIKit.Shape(chef, UIKit.Circle, Color.white, 0.5f, 0.98f, 42f, 40f);
            UIKit.Shape(chef, UIKit.RoundedSmall, Color.white, 0.5f, 0.78f, 76f, 20f);
            float textBottom = _tutor.done == null ? 0.34f : 0.08f;
            _tutorText = UIKit.Label(_tutorBubble, _tutor.text, 28, UIKit.Cream, TextAnchor.MiddleLeft, 0.17f, textBottom, 0.97f, 0.94f, false);
            _tutorText.resizeTextForBestFit = true;
            _tutorText.resizeTextMinSize = 16;
            _tutorText.resizeTextMaxSize = 28;
            UIKit.AddShadow(_tutorText, 2f, 0.6f);
            if (_tutor.done == null)
            {
                var ok = UIKit.Button(_tutorBubble, L.T("Понятно!", "Got it!"), UIKit.Green, FinishTutor, 0.6f, 0.06f, 0.96f, 0.3f, 30);
                ok.gameObject.AddComponent<Bobber>().amp = 2f;
            }
            UIKit.Button(_tutorRoot, L.T("Пропустить обучение", "Skip tutorial"), UIKit.Alpha(UIKit.Steel, 0.9f), SkipAllTutor, 0.82f, 0.94f, 0.99f, 0.99f, 20);
            Juice.Punch(_tutorBubble, 0.08f, 0.3f);
        }

        RectTransform _tutorShadeLayer, _tutorMarks;
        float BubbleW { get { return 820f; } }
        float BubbleH { get { return _tutor != null && _tutor.done == null ? 220f : 170f; } }

        Image ShadeAt(int i)
        {
            while (_shadePool.Count <= i)
            {
                var sh = UIKit.Panel(_tutorShadeLayer, "Shade", new Color(0f, 0f, 0f, 0.62f), 0, 0, 1, 1, false);
                sh.raycastTarget = true;          // клики мимо подсветки не проходят
                _shadePool.Add(sh);
            }
            return _shadePool[i];
        }

        RectTransform FrameAt(int i)
        {
            while (_framePool.Count <= i)
            {
                var f = UIKit.Rect(_tutorMarks, "Frame");
                for (int e = 0; e < 4; e++)
                {
                    var bar = UIKit.Panel(f, "Edge", UIKit.Gold, 0, 0, 1, 1, false);
                    if (e == 0) UIKit.Place(bar.rectTransform, 0f, 1f, 1f, 1f);
                    if (e == 1) UIKit.Place(bar.rectTransform, 0f, 0f, 1f, 0f);
                    if (e == 2) UIKit.Place(bar.rectTransform, 0f, 0f, 0f, 1f);
                    if (e == 3) UIKit.Place(bar.rectTransform, 1f, 0f, 1f, 1f);
                    var rt = bar.rectTransform;
                    rt.offsetMin = new Vector2(e == 3 ? 0f : -6f, e == 0 ? 0f : -6f);
                    rt.offsetMax = new Vector2(e == 2 ? 0f : 6f, e == 1 ? 0f : 6f);
                    bar.gameObject.AddComponent<Blinker>().speed = 3f;
                }
                _framePool.Add(f);
            }
            return _framePool[i];
        }

        RectTransform ArrowAt(int i)
        {
            while (_arrowPool.Count <= i)
            {
                var a = UIKit.Rect(_tutorMarks, "Arrow");
                UIKit.PlaceFixed(a, 0.5f, 0.5f, 90f, 90f);
                var inner = UIKit.Rect(a, "Inner");
                UIKit.PlaceFixed(inner, 0.5f, 0.5f, 90f, 90f);
                UIKit.Shape(inner, UIKit.Arrow, UIKit.Alpha(UIKit.Dark, 0.5f), 0.53f, 0.47f, 90f, 90f);
                UIKit.Shape(inner, UIKit.Arrow, UIKit.Gold, 0.5f, 0.5f, 90f, 90f);
                var sw = inner.gameObject.AddComponent<Swayer>();
                sw.amp = 12f;
                sw.speed = 6f;
                _arrowPool.Add(a);
            }
            return _arrowPool[i];
        }

        static bool Hit(Rect a, Rect b) { return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin; }
        static float OverlapArea(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0f && h > 0f ? w * h : 0f;
        }

        /// <summary>Каждый кадр: подсветка следует за целями (клиенты подъезжают, мухи летают).</summary>
        void LayoutTutor()
        {
            if (_tutorRoot == null || _tutor == null) return;
            var r = _tutorRoot.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            // 1) отдельное «окно» для каждой цели (в долях экрана)
            var holes = new List<Rect>();
            var list = _tutor.targets != null ? _tutor.targets() : null;
            if (list != null)
            {
                var corners = new Vector3[4];
                foreach (var t in list)
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    t.GetWorldCorners(corners);
                    Vector2 mn = new Vector2(float.MaxValue, float.MaxValue), mx = new Vector2(float.MinValue, float.MinValue);
                    foreach (var c in corners)
                    {
                        Vector2 lp = _tutorRoot.InverseTransformPoint(c);
                        mn = Vector2.Min(mn, lp);
                        mx = Vector2.Max(mx, lp);
                    }
                    const float pad = 12f;
                    float x0 = Mathf.Clamp01((mn.x - pad - r.xMin) / r.width), x1 = Mathf.Clamp01((mx.x + pad - r.xMin) / r.width);
                    float y0 = Mathf.Clamp01((mn.y - pad - r.yMin) / r.height), y1 = Mathf.Clamp01((mx.y + pad - r.yMin) / r.height);
                    if (x1 - x0 < 0.002f || y1 - y0 < 0.002f) continue;
                    var h = Rect.MinMaxRect(x0, y0, x1, y1);
                    // вложенные/перекрывающиеся окна сливаем, чтобы не было «дырявых» рамок
                    bool merged = false;
                    for (int k = 0; k < holes.Count; k++)
                        if (Hit(holes[k], h))
                        {
                            var o = holes[k];
                            holes[k] = Rect.MinMaxRect(Mathf.Min(o.xMin, h.xMin), Mathf.Min(o.yMin, h.yMin), Mathf.Max(o.xMax, h.xMax), Mathf.Max(o.yMax, h.yMax));
                            merged = true;
                            break;
                        }
                    if (!merged) holes.Add(h);
                }
            }

            // 2) затемнение сеткой: всё, что не попало ни в одно окно
            var xs = new List<float> { 0f, 1f };
            var ys = new List<float> { 0f, 1f };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            xs.Sort(); ys.Sort();
            int used = 0;
            for (int j = 0; j < ys.Count - 1; j++)
            {
                float ya = ys[j], yb = ys[j + 1];
                if (yb - ya < 0.0005f) continue;
                float ym = (ya + yb) * 0.5f;
                int i = 0;
                while (i < xs.Count - 1)
                {
                    // склеиваем подряд идущие тёмные клетки ряда в одну полосу
                    float xa = xs[i];
                    int k = i;
                    while (k < xs.Count - 1 && !InHole(holes, (xs[k] + xs[k + 1]) * 0.5f, ym)) k++;
                    if (k > i && xs[k] - xa > 0.0005f)
                    {
                        var sh = ShadeAt(used++);
                        sh.gameObject.SetActive(true);
                        UIKit.Place(sh.rectTransform, xa, ya, xs[k], yb);
                    }
                    i = Mathf.Max(k + 1, i + 1);
                }
            }
            for (int i = used; i < _shadePool.Count; i++) _shadePool[i].gameObject.SetActive(false);

            // 3) пузырь — туда, где он не закрывает ни одну цель (с запасом под стрелки)
            float bw = BubbleW / r.width, bh = BubbleH / r.height;
            float mxg = 110f / r.width, myg = 110f / r.height;
            Vector2 best = new Vector2(0.5f, 0.55f);
            if (holes.Count > 0)
            {
                float bestScore = float.MaxValue;
                float[] cys = { 0.86f, 0.14f, 0.7f, 0.3f, 0.5f };
                float[] cxs = { 0.5f, 0.27f, 0.73f };
                foreach (var cy in cys)
                    foreach (var cx in cxs)
                    {
                        var br = Rect.MinMaxRect(cx - bw / 2f - mxg, cy - bh / 2f - myg, cx + bw / 2f + mxg, cy + bh / 2f + myg);
                        var core = Rect.MinMaxRect(cx - bw / 2f, cy - bh / 2f, cx + bw / 2f, cy + bh / 2f);
                        float score = 0f;
                        foreach (var h in holes) score += OverlapArea(core, h) * 100f + OverlapArea(br, h);
                        // при равенстве — поближе к целям по вертикали
                        float hc = 0f; foreach (var h in holes) hc += h.center.y; hc /= holes.Count;
                        score += Mathf.Abs(cy - hc) * 0.001f;
                        if (score < bestScore - 1e-6f) { bestScore = score; best = new Vector2(cx, cy); }
                    }
            }
            _tutorBubble.anchorMin = _tutorBubble.anchorMax = best;
            _tutorBubble.anchoredPosition = Vector2.zero;
            var bubRect = Rect.MinMaxRect(best.x - bw / 2f, best.y - bh / 2f, best.x + bw / 2f, best.y + bh / 2f);

            // 4) у каждого окна своя рамка и своя стрелка, остриём точно в цель
            float aw = 60f / r.width, ah = 60f / r.height;
            for (int i = 0; i < holes.Count; i++)
            {
                var h = holes[i];
                var f = FrameAt(i);
                f.gameObject.SetActive(true);
                UIKit.Place(f, h.xMin, h.yMin, h.xMax, h.yMax);

                // кандидаты: сверху (вниз), снизу (вверх), слева (вправо), справа (влево)
                var cand = new[]
                {
                    new Vector3(h.center.x, h.yMax + ah, -90f),
                    new Vector3(h.center.x, h.yMin - ah, 90f),
                    new Vector3(h.xMin - aw, h.center.y, 0f),
                    new Vector3(h.xMax + aw, h.center.y, 180f),
                };
                // стрелка смотрит со стороны пузыря, если это возможно
                bool bubbleAbove = best.y > h.center.y;
                if (!bubbleAbove) { var t0 = cand[0]; cand[0] = cand[1]; cand[1] = t0; }
                Vector3 pick = cand[0];
                float pickScore = float.MaxValue;
                for (int c = 0; c < cand.Length; c++)
                {
                    var p = cand[c];
                    var ar = Rect.MinMaxRect(p.x - aw, p.y - ah, p.x + aw, p.y + ah);
                    float sc = c * 0.01f;
                    if (ar.xMin < 0f || ar.xMax > 1f || ar.yMin < 0f || ar.yMax > 1f) sc += 10f;
                    if (Hit(ar, bubRect)) sc += 5f;
                    for (int k = 0; k < holes.Count; k++) if (k != i && Hit(ar, holes[k])) sc += 3f;
                    if (sc < pickScore) { pickScore = sc; pick = p; }
                }
                var a = ArrowAt(i);
                a.gameObject.SetActive(true);
                a.anchorMin = a.anchorMax = new Vector2(pick.x, pick.y);
                a.anchoredPosition = Vector2.zero;
                a.localRotation = Quaternion.Euler(0, 0, pick.z);
            }
            for (int i = holes.Count; i < _framePool.Count; i++) _framePool[i].gameObject.SetActive(false);
            for (int i = holes.Count; i < _arrowPool.Count; i++) _arrowPool[i].gameObject.SetActive(false);
        }

        static bool InHole(List<Rect> holes, float x, float y)
        {
            foreach (var h in holes) if (x > h.xMin && x < h.xMax && y > h.yMin && y < h.yMax) return true;
            return false;
        }
    }
}
