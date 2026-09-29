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
        RectTransform _tutorRoot, _tutorFrame, _tutorBubble, _tutorArrow;
        Image[] _tutorShade;
        Text _tutorText;
        bool _basicsRunning;
        int _tutorSlot = -1;

        bool TutorFrozen { get { return _tutor != null; } }

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
                if (c.WantsDrink(Drink.Fries) && !_save.Tutored("friesbox") && _boxCell != null)
                {
                    Add("friesbox", L.T("Клиент хочет картошку фри отдельно. Возьми коробку и держи её над лотком «Картошка», пока не наполнится.",
                                        "The customer wants fries on the side. Take a box and hold it over the Fries bin until it fills."),
                        () => Targets(_boxCell, BinRoot(Ing.Fries)), () => TrayHas(Drink.Fries) || _customers[si] == null || !_customers[si].needDrink);
                    return;
                }
                if ((c.WantsDrink(Drink.Cola) || c.WantsDrink(Drink.Ayran)) && !_save.Tutored("cup") && _cupsCell != null)
                {
                    var tap = c.WantsDrink(Drink.Cola) ? _tapCola : _tapAyran;
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
                if (_tutor.onStart != null) _tutor.onStart();
                Sfx.Play(Sound.Tick);
            }
            LayoutTutor();
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
            foreach (var k in new[] { "basics", "ask", "pack", "fly", "dirt", "grill", "grilltake", "fire", "fryload", "fryunload", "friesbox", "cup", "courier", "bulk", "cat" })
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

        void BuildTutorUI()
        {
            _tutorRoot = UIKit.Full(_root, "Tutor");
            _tutorRoot.SetAsLastSibling();
            _tutorShade = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var sh = UIKit.Panel(_tutorRoot, "Shade", new Color(0f, 0f, 0f, 0.62f), 0, 0, 1, 1, false);
                sh.raycastTarget = true;          // клики мимо подсветки не проходят
                _tutorShade[i] = sh;
            }
            // золотая рамка вокруг цели
            _tutorFrame = UIKit.Rect(_tutorRoot, "Frame");
            for (int i = 0; i < 4; i++)
            {
                var bar = UIKit.Panel(_tutorFrame, "Edge", UIKit.Gold, 0, 0, 1, 1, false);
                if (i == 0) UIKit.Place(bar.rectTransform, 0f, 1f, 1f, 1f);
                if (i == 1) UIKit.Place(bar.rectTransform, 0f, 0f, 1f, 0f);
                if (i == 2) UIKit.Place(bar.rectTransform, 0f, 0f, 0f, 1f);
                if (i == 3) UIKit.Place(bar.rectTransform, 1f, 0f, 1f, 1f);
                var rt = bar.rectTransform;
                rt.offsetMin = new Vector2(i == 3 ? 0f : -6f, i == 0 ? 0f : -6f);
                rt.offsetMax = new Vector2(i == 2 ? 0f : 6f, i == 1 ? 0f : 6f);
                bar.gameObject.AddComponent<Blinker>().speed = 3f;
            }
            // стрелка-указатель
            _tutorArrow = UIKit.Rect(_tutorRoot, "Arrow");
            UIKit.PlaceFixed(_tutorArrow, 0.5f, 0.5f, 90f, 90f);
            var inner = UIKit.Rect(_tutorArrow, "Inner");
            UIKit.PlaceFixed(inner, 0.5f, 0.5f, 90f, 90f);
            UIKit.Shape(inner, UIKit.Arrow, UIKit.Alpha(UIKit.Dark, 0.5f), 0.53f, 0.47f, 90f, 90f);
            UIKit.Shape(inner, UIKit.Arrow, UIKit.Gold, 0.5f, 0.5f, 90f, 90f);
            var sw = inner.gameObject.AddComponent<Swayer>();
            sw.amp = 12f;
            sw.speed = 6f;

            // пузырь с текстом
            var bub = UIKit.Panel(_tutorRoot, "Bubble", UIKit.Hex("#2A1C13"), 0, 0, 1, 1);
            bub.raycastTarget = true;
            UIKit.AddOutline(bub, UIKit.Gold, 3f);
            UIKit.AddShadow(bub, 8f, 0.5f);
            _tutorBubble = bub.rectTransform;
            UIKit.PlaceFixed(_tutorBubble, 0.5f, 0.5f, 820f, _tutor.done == null ? 220f : 170f);
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

        /// <summary>Каждый кадр: подсветка следует за целью (клиенты подъезжают, мухи летают).</summary>
        void LayoutTutor()
        {
            if (_tutorRoot == null || _tutor == null) return;
            var r = _tutorRoot.rect;
            bool has = false;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            var list = _tutor.targets != null ? _tutor.targets() : null;
            if (list != null)
            {
                var corners = new Vector3[4];
                foreach (var t in list)
                {
                    if (t == null) continue;
                    t.GetWorldCorners(corners);
                    foreach (var c in corners)
                    {
                        Vector2 lp = _tutorRoot.InverseTransformPoint(c);
                        min = Vector2.Min(min, lp);
                        max = Vector2.Max(max, lp);
                    }
                    has = true;
                }
            }
            float x0, y0, x1, y1;
            if (has)
            {
                const float pad = 12f;
                x0 = Mathf.Clamp01((min.x - pad - r.xMin) / r.width);
                x1 = Mathf.Clamp01((max.x + pad - r.xMin) / r.width);
                y0 = Mathf.Clamp01((min.y - pad - r.yMin) / r.height);
                y1 = Mathf.Clamp01((max.y + pad - r.yMin) / r.height);
            }
            else { x0 = x1 = 0.5f; y0 = y1 = 0.5f; }

            // четыре тёмные панели вокруг «окна»
            UIKit.Place(_tutorShade[0].rectTransform, 0f, y1, 1f, 1f);
            UIKit.Place(_tutorShade[1].rectTransform, 0f, 0f, 1f, y0);
            UIKit.Place(_tutorShade[2].rectTransform, 0f, y0, x0, y1);
            UIKit.Place(_tutorShade[3].rectTransform, x1, y0, 1f, y1);
            _tutorFrame.gameObject.SetActive(has);
            _tutorArrow.gameObject.SetActive(has);
            if (has) UIKit.Place(_tutorFrame, x0, y0, x1, y1);

            // пузырь — выше цели, если цель внизу, и ниже, если вверху
            float cy = (y0 + y1) * 0.5f, cx = Mathf.Clamp((x0 + x1) * 0.5f, 0.24f, 0.76f);
            float by;
            if (!has) by = 0.55f;
            else if (cy < 0.55f) by = Mathf.Min(0.86f, y1 + 0.13f);
            else by = Mathf.Max(0.14f, y0 - 0.13f);
            _tutorBubble.anchorMin = _tutorBubble.anchorMax = new Vector2(cx, by);
            _tutorBubble.anchoredPosition = Vector2.zero;

            // стрелка между пузырём и целью, остриём к цели
            if (has)
            {
                bool below = by > cy;
                float ay = below ? Mathf.Min(1f, y1 + 0.05f) : Mathf.Max(0f, y0 - 0.05f);
                _tutorArrow.anchorMin = _tutorArrow.anchorMax = new Vector2((x0 + x1) * 0.5f, ay);
                _tutorArrow.anchoredPosition = Vector2.zero;
                _tutorArrow.localRotation = Quaternion.Euler(0, 0, below ? -90f : 90f);
            }
        }
    }
}
