using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Кухня. Уровни прокачки 1..4 (в коде 0..3), на 4-м станция работает сама:
    /// - Вертел: свайп → кусочки (не чаще 2/сек) → порция; 3/2/1 кусок на порцию; авто-нарезчик.
    /// - Нарезка: ящик → доска → удары (6/3/1); авто-слайсер.
    /// - Фритюр: засыпать → вынуть вовремя; авто-корзина.
    /// - Соусы: тап — добавить, зажать — долить; авто-дозатор.
    /// - Сборка: лаваш → начинка (видно детально) → «Завернуть» → гриль.
    /// - Гриль: снять вовремя (первые 40% окна — идеальная прожарка); авто-таймер.
    /// </summary>
    public partial class DayRun
    {
        enum FryState { Empty, Frying, Ready, Burnt }

        class GrillSlot
        {
            public int[] counts;
            public float t;
            public bool burntPlayed;
            public RectTransform view;
            public Image roll;
            public Image[] marks;
            public RectTransform fill;
            public Text label, content;
            public float fireT;
            public bool flipped, uneven;
            public float sprinkT;
            public bool onFire;
            public bool fresh;
            public int fireTaps;
            public RectTransform fire;
        }

        class TrayItem
        {
            public bool isDrink;
            public Drink drink;
            public int[] counts;
            public bool perfect;
            public bool fresh;       // на горячем лаваше из тандыра
            public int pack;         // 0 — не упакована, 1 — обёртка, 2 — обёртка + пакет
            public int packProgress;
        }

        class BinView
        {
            public Ing id;
            public RectTransform root;
            public Text count;
            public RectTransform fill;
            public DragZone zone;
            public float holdT;
            public float pourAcc;
            public bool locked;
        }

        static readonly Ing[] Choppables = { Ing.Tomato, Ing.Cucumber, Ing.Cabbage, Ing.Cheese, Ing.Jalapeno };
        static readonly Ing[] BinOrder = { Ing.Meat, Ing.Tomato, Ing.Cucumber, Ing.Cabbage, Ing.Cheese, Ing.Jalapeno, Ing.Fries, Ing.Garlic, Ing.Spicy };
        const int MaxPerIngredient = 2;

        // --- уровни ---
        int _lvSpit, _lvKnife, _lvFryer, _lvGrill, _lvSauce;

        // --- запасы ---
        int[] _stock, _cap;

        // --- вертел ---
        int _spitLeft, _spitCap, _piecesNeeded, _pieces;
        float _swipeAcc, _spitReloadT = -1f, _cutCd, _autoCutT;
        Image[] _cone;
        float[] _coneW;
        Text _spitText;
        Button _newSpitBtn;
        RectTransform _spitPanel, _knife, _pieceFill;
        DragZone _spitZone;

        // --- нарезка ---
        int _chopsNeeded, _chopsDone;
        int _boardVeg = -1;
        float _chopCd, _autoChopT;
        RectTransform _chopPanel, _chopHolder;
        Text _chopText;

        // --- фритюр ---
        FryState _fry;
        float _fryT, _fryTime, _fryBurn;
        int _friesBatch;
        bool _friesUnlocked;
        RectTransform _fryPanel, _fryFill, _basketContent, _fryBody, _rawCrate, _chopBoard;
        DragSource _fryDrag;

        // --- коробки фри ---
        int _lvBox;
        DragSource _boxSrc;
        RectTransform _boxCell;
        float _boxFill, _autoBoxT;
        bool _boxFilled;
        int _boxDrawn = -1;
        Image _basket, _fryTint;
        Text _fryText;

        // --- соусы ---
        float _sauceRate, _autoSauceT;

        // --- лотки и сборка ---
        BinView[] _bins;
        bool _hasLavash;
        int[] _cur;
        RectTransform _boardContent, _boardRt;
        Text _boardHint, _boardList;

        // --- гриль ---
        float _grillTime, _grillBurn;
        GrillSlot[] _grill;

        // --- выдача ---
        readonly List<TrayItem> _tray = new List<TrayItem>();
        int _selected = -1;
        RectTransform _trayRoot;
        bool _suppressClick;
        int _wrapsSinceClean;
        bool _dirty;
        float _wipeAcc;
        RectTransform _dirtRt;
        Button _wrapBtn, _bagBtn;
        int DirtyAfter { get { return GameData.DirtyAfter(_save.Lvl(Up.Board)); } }
        float WipeNeed { get { return GameData.WipeDistance(_save.Lvl(Up.Board)); } }
        float _roboT = -1f;
        int _packTaps;
        bool _packAuto;

        // =====================================================================
        void InitKitchen()
        {
            _lvSpit = _save.Lvl(Up.Spit);
            _lvKnife = _save.Lvl(Up.Knife);
            _lvFryer = _save.Lvl(Up.Fryer);
            _lvGrill = _save.Lvl(Up.Grill);
            _lvSauce = _save.Lvl(Up.Sauce);
            _lvBox = _save.Lvl(Up.FryBox);

            int n = GameData.IngCount;
            _stock = new int[n];
            _cap = new int[n];
            _cur = GameData.NewCounts();
            int binCap = GameData.BinCap(_save.Lvl(Up.Bins));
            int sauceCap = GameData.SauceCap(_lvSauce);
            foreach (var d in GameData.Ingredients)
                _cap[(int)d.id] = d.restock == Restock.Sauce ? sauceCap : binCap;

            _spitCap = GameData.SpitCapacity(_lvSpit);
            _spitLeft = _spitCap;
            _piecesNeeded = GameData.PiecesPerPortion(_lvSpit);
            _chopsNeeded = GameData.ChopsNeeded(_lvKnife);
            _fryTime = GameData.FryTime(_lvFryer);
            _fryBurn = GameData.FryBurn(_lvFryer);
            _friesBatch = GameData.FriesBatch(_lvFryer);
            _friesUnlocked = GameData.Unlocked(Ing.Fries, _eday);
            _sauceRate = GameData.SauceRate(_lvSauce);
            _grillTime = GameData.GrillTime(_lvGrill);
            _packTaps = GameData.PackTaps(_save.Lvl(Up.Pack));
            _packAuto = GameData.PackAuto(_save.Lvl(Up.Pack));
            float hands = 1f - 0.06f * Meta.P(_save, Perk.Hands);
            _fryTime *= hands;
            _grillTime *= hands;
            if (_boost[(int)Boost.Speed])
            {
                _fryTime *= 0.8f;
                _grillTime *= 0.8f;
            }
            _grillBurn = GameData.GrillBurn(_lvGrill);
            _grill = new GrillSlot[GameData.GrillSlots(_save.Lvl(Up.Grills))];
            for (int i = 0; i < _grill.Length; i++) _grill[i] = new GrillSlot();

            // стартовые запасы: почти пусто — сразу за работу
            _stock[(int)Ing.Meat] = 1;
            foreach (var v in Choppables) _stock[(int)v] = 1;
            _stock[(int)Ing.Fries] = 0;
            _stock[(int)Ing.Garlic] = sauceCap / 2;
            _stock[(int)Ing.Spicy] = sauceCap / 2;
        }

        bool IsUnlocked(Ing id) { return GameData.Unlocked(id, _eday); }

        // =====================================================================
        //  ТИК
        // =====================================================================
        void TickKitchen(float dt)
        {
            if (_cutCd > 0f) _cutCd -= dt;
            if (_dirty && _roboT > 0f)
            {
                _roboT -= dt;
                if (_roboT <= 0f) CleanBoard();
            }
            if (_chopCd > 0f) _chopCd -= dt;

            // --- вертел ---
            if (_spitReloadT >= 0f)
            {
                _spitReloadT += dt;
                if (_spitReloadT >= GameData.NewSpitTime)
                {
                    _spitReloadT = -1f;
                    _spitLeft = _spitCap;
                    Sfx.Play(Sound.Done);
                    Juice.Punch(_spitPanel, 0.06f, 0.25f);
                    RefreshSpit();
                }
            }
            if (GameData.SpitAuto(_lvSpit))
            {
                if (_spitLeft <= 0 && _spitReloadT < 0f) { _spitReloadT = 0f; RefreshSpit(); }
                _autoCutT += dt;
                if (_autoCutT >= GameData.AutoCutInterval * GameData.Legend2x(_lvSpit))
                {
                    _autoCutT = 0f;
                    int m = (int)Ing.Meat;
                    if (_spitLeft > 0 && _stock[m] < _cap[m])
                    {
                        _spitLeft--;
                        _stock[m]++;
                        Sfx.Play(Sound.Slice);
                        Juice.Burst(_spitPanel, GameData.Get(Ing.Meat).color, 3, 0.55f, 0.5f, 14f);
                        RefreshSpit();
                        RefreshBin(Ing.Meat);
                    }
                }
            }
            if (_knife.gameObject.activeSelf && !_spitZone.Held) _knife.gameObject.SetActive(false);

            // --- авто-слайсер ---
            if (GameData.KnifeAuto(_lvKnife))
            {
                _autoChopT += dt;
                if (_autoChopT >= GameData.AutoChopInterval * GameData.Legend2x(_lvKnife))
                {
                    _autoChopT = 0f;
                    int best = -1;
                    float bestK = 1f;
                    foreach (var v in Choppables)
                    {
                        if (!IsUnlocked(v)) continue;
                        int i = (int)v;
                        float k = (float)_stock[i] / _cap[i];
                        if (k < bestK) { bestK = k; best = i; }
                    }
                    if (best >= 0)
                    {
                        _stock[best] = Mathf.Min(_cap[best], _stock[best] + GameData.AutoChopAmount);
                        Sfx.Play(Sound.Chop);
                        Juice.Burst(_chopPanel, GameData.Ingredients[best].color, 4, 0.5f, 0.4f, 12f);
                        RefreshBin((Ing)best);
                        PunchBin((Ing)best);
                    }
                }
            }

            // --- соусы: зажать бутылку — доливается; авто-дозатор ---
            bool autoSauce = GameData.SauceAuto(_lvSauce);
            if (autoSauce) _autoSauceT += dt;
            foreach (var b in _bins)
            {
                if (b.zone == null || b.locked) continue;
                int i = (int)b.id;
                if (b.zone.Held)
                {
                    b.holdT += dt;
                    if (b.holdT > 0.35f && _stock[i] < _cap[i])
                    {
                        _suppressClick = true;
                        b.pourAcc += dt * _sauceRate;
                        while (b.pourAcc >= 1f && _stock[i] < _cap[i])
                        {
                            b.pourAcc -= 1f;
                            _stock[i]++;
                            Sfx.Play(Sound.Pour);
                            RefreshBin(b.id);
                        }
                    }
                }
                else
                {
                    if (b.holdT > 0f) _suppressClick = false;
                    b.holdT = 0f;
                    b.pourAcc = 0f;
                }
                if (autoSauce && _autoSauceT >= GameData.AutoSauceInterval * GameData.Legend2x(_lvSauce) && _stock[i] < _cap[i])
                {
                    _stock[i]++;
                    RefreshBin(b.id);
                }
            }
            if (autoSauce && _autoSauceT >= GameData.AutoSauceInterval * GameData.Legend2x(_lvSauce)) _autoSauceT = 0f;

            // --- фритюр ---
            bool autoFry = GameData.FryerAuto(_lvFryer);
            int f = (int)Ing.Fries;
            if (_friesUnlocked && autoFry && _fry == FryState.Empty && _stock[f] < _cap[f]) LoadFryer(true);
            if (_fry == FryState.Frying)
            {
                _fryT += dt;
                UIKit.SetFill(_fryFill, _fryT / _fryTime);
                if (_fryT >= _fryTime)
                {
                    _fry = FryState.Ready;
                    _fryT = 0f;
                    Sfx.Play(Sound.Done);
                    Juice.Punch(_basket.transform, 0.15f, 0.3f);
                    RefreshFryer();
                    if (autoFry && _stock[f] < _cap[f]) UnloadFryer(true);
                }
            }
            else if (_fry == FryState.Ready)
            {
                _fryT += dt;
                UIKit.SetFill(_fryFill, 1f - _fryT / _fryBurn);
                float blink = Mathf.PingPong(Time.time * 4f, 1f);
                _fryTint.color = new Color(1f, 0.85f, 0.2f, 0.15f + 0.25f * blink);
                if (autoFry && _stock[f] < _cap[f]) UnloadFryer(true);
                else if (_fryT >= _fryBurn)
                {
                    _fry = FryState.Burnt;
                    BurntCount++;
                    Sfx.Play(Sound.Burn);
                    Toast(L.T("Картошка сгорела! Нажми на фритюр, чтобы почистить", "Fries burnt! Tap the fryer to clean"));
                    Juice.Shake(_fryPanel, 8f, 0.3f);
                    RefreshFryer();
                }
            }

            TickFriesBox(dt);

            // --- гриль ---
            bool autoGrill = GameData.GrillAuto(_lvGrill);
            for (int s = 0; s < _grill.Length; s++)
            {
                var g = _grill[s];
                if (g.counts == null) continue;
                g.t += (AnyFire() && !g.onFire) ? dt * 0.5f : dt;
                float k = g.t / _grillTime;
                for (int m = 0; m < g.marks.Length; m++)
                {
                    var mc = g.marks[m].color;
                    mc.a = Mathf.Clamp01(k) * 0.55f;
                    g.marks[m].color = mc;
                }
                if (k < 1f)
                {
                    g.roll.color = Icons.Toast(k);
                    UIKit.SetFill(g.fill, k);
                    g.fill.GetComponent<Image>().color = UIKit.Orange;
                    if (autoGrill && !g.flipped && k >= 0.5f) { g.flipped = true; FlipVisual(g); }
                    if (!g.flipped && k >= 0.4f && k <= FlipEnd)
                    {
                        g.label.text = L.T("ПЕРЕВЕРНИ! Жми", "FLIP IT! Tap");
                        g.label.color = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f ? UIKit.Gold : UIKit.Red;
                    }
                    else if (!g.flipped && k > FlipEnd)
                    {
                        if (!g.uneven) { g.uneven = true; Toast(L.T("Не перевернул — подгорает с одной стороны!", "Not flipped — one side is burning!")); Sfx.Play(Sound.Error); }
                        g.label.text = L.T("Подгорает!", "Burning side!");
                        g.label.color = UIKit.Red;
                    }
                    else
                    {
                        g.label.text = L.T("Жарится", "Grilling");
                        g.label.color = UIKit.Cream;
                    }
                }
                else if (g.t < _grillTime + Burn(g))
                {
                    if (autoGrill && _tray.Count < GameData.TrayCapacity) { TakeFromGrill(s, true); continue; }
                    float q = (g.t - _grillTime) / Burn(g);
                    bool perfect = q < PerfectLimit && !g.uneven;
                    g.roll.color = Icons.Toast(1f + q * 0.35f);
                    UIKit.SetFill(g.fill, 1f - q);
                    g.fill.GetComponent<Image>().color = perfect ? UIKit.Gold : (q < 0.7f ? UIKit.Green : UIKit.Red);
                    g.label.text = perfect ? L.T("ИДЕАЛЬНО! Жми", "PERFECT! Tap") : L.T("Готово — жми", "Ready — tap");
                    g.label.color = perfect ? UIKit.Gold : UIKit.Green;
                    float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 10f);
                    g.roll.transform.parent.localScale = new Vector3(pulse, pulse, 1f);
                }
                else
                {
                    if (!g.burntPlayed)
                    {
                        g.burntPlayed = true;
                        BurntCount++;
                        Sfx.Play(Sound.Burn);
                        Toast(L.T("Шаурма сгорела!", "Shawarma burnt!"));
                        Juice.Shake(g.view, 8f, 0.3f);
                    }
                    g.roll.color = Icons.Toast(2f);
                    g.roll.transform.parent.localScale = Vector3.one;
                    UIKit.SetFill(g.fill, 0f);
                    g.label.text = L.T("Сгорела — убрать", "Burnt — clear");
                    g.label.color = UIKit.Red;
                    if (!g.onFire)
                    {
                        g.fireT += dt;
                        if (_save.Lvl(Up.Fire) >= 4 && g.fireT > 1f)
                        {
                            ClearGrill(g);
                            Toast(L.T("Сгоревшее убрано автоматически", "Burnt food cleared automatically"));
                            continue;
                        }
                        if (g.fireT > 3f && _save.Lvl(Up.Fire) < 3) Ignite(g);
                    }
                }
            }
        }

        // =====================================================================
        //  ВЕРТЕЛ
        // =====================================================================
        void OnSpitDown()
        {
            if (_paused || _ended) return;
            if (GameData.SpitTap(_lvSpit)) TryCutPiece();
        }

        void OnSpitDrag(float dist, Vector2 screenPos)
        {
            if (_paused || _ended) return;
            _knife.gameObject.SetActive(true);
            _knife.position = new Vector3(screenPos.x, screenPos.y, 0f);
            _swipeAcc = Mathf.Min(_swipeAcc + dist, GameData.SwipePerPiece);
            if (_swipeAcc >= GameData.SwipePerPiece && _cutCd <= 0f)
            {
                _swipeAcc = 0f;
                TryCutPiece();
            }
        }

        void TryCutPiece()
        {
            if (_cutCd > 0f) return;
            if (_spitReloadT >= 0f) return;
            if (_spitLeft <= 0) { Toast(L.T("Вертел пуст — поставь новый", "Spit is empty — load a new one")); return; }
            int m = (int)Ing.Meat;
            if (_stock[m] >= _cap[m]) { Toast(L.T("Лоток с мясом полон", "Meat bin is full")); return; }

            _cutCd = GameData.CutCooldown * _cutMult;
            _pieces++;
            Sfx.Play(Sound.Slice);
            Juice.Burst(_spitPanel, GameData.Get(Ing.Meat).color, 3, 0.55f, 0.55f, 14f);
            if (_pieces >= _piecesNeeded)
            {
                _pieces = 0;
                _spitLeft--;
                _stock[m]++;
                Juice.FloatText(_spitPanel, "+1", UIKit.Gold, 40, 0.6f, 0.6f);
                RefreshBin(Ing.Meat);
                PunchBin(Ing.Meat);
            }
            RefreshSpit();
        }

        void OnNewSpit()
        {
            if (_spitReloadT >= 0f || _spitLeft > 0) return;
            _spitReloadT = 0f;
            Sfx.Play(Sound.Click);
            RefreshSpit();
        }

        void RefreshSpit()
        {
            float frac = (float)_spitLeft / _spitCap;
            for (int i = 0; i < _cone.Length; i++)
            {
                _cone[i].gameObject.SetActive(_spitLeft > 0);
                var rt = _cone[i].rectTransform;
                rt.sizeDelta = new Vector2(_coneW[i] * Mathf.Lerp(0.3f, 1f, frac), rt.sizeDelta.y);
            }
            UIKit.SetFill(_pieceFill, _piecesNeeded > 1 ? (float)_pieces / _piecesNeeded : 0f);
            bool empty = _spitLeft <= 0;
            _newSpitBtn.gameObject.SetActive(empty && _spitReloadT < 0f);
            _spitText.gameObject.SetActive(!empty || _spitReloadT >= 0f);
            if (_spitReloadT >= 0f) _spitText.text = L.T("Ставим...", "Loading...");
            else if (_piecesNeeded > 1) _spitText.text = L.T("Кусок ", "Piece ") + _pieces + "/" + _piecesNeeded + "  •  " + _spitLeft;
            else _spitText.text = L.T("Порций: ", "Portions: ") + _spitLeft;
        }

        // =====================================================================
        //  НАРЕЗКА
        // =====================================================================
        void OnPickVeg(Ing id)
        {
            if (_paused || _ended) return;
            if (_boardVeg >= 0) { Toast(L.T("Сначала дорежь то, что на доске", "Finish chopping first")); Sfx.Play(Sound.Error); return; }
            if (_stock[(int)id] >= _cap[(int)id]) { Toast(L.T("Этот лоток уже полон", "That bin is already full")); Sfx.Play(Sound.Error); return; }
            _boardVeg = (int)id;
            _chopsDone = 0;
            Sfx.Play(Sound.Click);
            RefreshChop();
            Juice.Punch(_chopHolder, 0.2f, 0.25f);
        }

        void OnChop()
        {
            if (_paused || _ended) return;
            if (_boardVeg < 0) { Toast(L.T("Перетащи овощ из ящика сверху на доску", "Drag a veggie from the crate onto the board")); Sfx.Play(Sound.Error); return; }
            if (_chopCd > 0f) return;
            _chopCd = 0.22f * _cutMult;
            _chopsDone++;
            Sfx.Play(Sound.Chop);
            var id = (Ing)_boardVeg;
            Juice.Burst(_chopPanel, GameData.Get(id).color, 4, 0.5f, 0.4f, 14f);
            Juice.Shake(_chopHolder, 6f, 0.12f);
            if (_chopsDone >= _chopsNeeded)
            {
                int i = _boardVeg;
                int added = _cap[i] - _stock[i];
                _stock[i] = _cap[i];
                _boardVeg = -1;
                Sfx.Play(Sound.Done);
                Juice.FloatText(_chopPanel, "+" + added, UIKit.Green, 44, 0.5f, 0.45f);
                RefreshBin(id);
                PunchBin(id);
            }
            RefreshChop();
        }

        void RefreshChop()
        {
            UIKit.Clear(_chopHolder);
            if (_boardVeg < 0)
            {
                _chopText.text = GameData.KnifeAuto(_lvKnife) ? L.T("Слайсер режет сам", "Slicer works by itself") : L.T("Тащи овощ из ящика ↑", "Drag a veggie from the crate ↑");
                return;
            }
            var id = (Ing)_boardVeg;
            float k = 1f - (float)_chopsDone / _chopsNeeded;
            Icons.Ingredient(_chopHolder, id, 0.4f, 0.5f, 60f + 70f * k);
            for (int c = 0; c < _chopsDone && c < 6; c++)
                Icons.Ingredient(_chopHolder, id, 0.72f + (c % 2) * 0.12f, 0.25f + (c / 2) * 0.22f, 30f);
            _chopText.text = L.T("Руби! ", "Chop! ") + _chopsDone + "/" + _chopsNeeded;
        }

        // =====================================================================
        //  ФРИТЮР
        // =====================================================================
        /// <summary>Сырая картошка из ящика — во фритюр.</summary>
        void LoadFryer(bool anim)
        {
            if (_paused || _ended || !_friesUnlocked) return;
            int f = (int)Ing.Fries;
            if (_fry == FryState.Burnt) { Toast(L.T("Сначала почисти фритюр — нажми на него", "Clean the fryer first — tap it")); Sfx.Play(Sound.Error); return; }
            if (_fry != FryState.Empty) { Toast(L.T("Во фритюре уже жарится", "The fryer is busy")); return; }
            if (_stock[f] >= _cap[f]) { Toast(L.T("Лоток картошки и так полон", "Fries bin is already full")); return; }
            _fry = FryState.Frying;
            _fryT = 0f;
            Sfx.Play(Sound.Pour);
            if (anim && _rawCrate != null && _fryBody != null)
                Juice.Fly(RawFries(_fx, 80f), _rawCrate.position, _fryBody.position, 0.3f, 0f, null);
            Juice.Punch(_fryBody, 0.1f, 0.2f);
            RefreshFryer();
        }

        /// <summary>Готовая картошка из корзины — в лоток «Картошка».</summary>
        void UnloadFryer(bool anim)
        {
            if (_fry != FryState.Ready) return;
            int f = (int)Ing.Fries;
            int added = Mathf.Min(_friesBatch, _cap[f] - _stock[f]);
            if (added <= 0) { Toast(L.T("Лоток картошки полон", "Fries bin is full")); return; }
            _stock[f] += added;
            _fry = FryState.Empty;
            Sfx.Play(Sound.Done);
            var bin = BinOf(Ing.Fries);
            if (anim && bin != null)
            {
                var ic = Icons.Ingredient(_fx, Ing.Fries, 0.5f, 0.5f, 80f);
                Juice.Fly(ic, _fryBody.position, bin.root.position, 0.3f, 0f, null);
            }
            Juice.FloatText(_fryPanel, "+" + added, UIKit.Green, 44, 0.5f, 0.6f);
            RefreshBin(Ing.Fries);
            PunchBin(Ing.Fries);
            RefreshFryer();
        }

        void OnFryerTap()
        {
            if (_paused || _ended || !_friesUnlocked) return;
            switch (_fry)
            {
                case FryState.Empty:
                    if (GameData.FryerTap(_lvFryer)) LoadFryer(true);
                    else { Toast(L.T("Перетащи сырую картошку из ящика во фритюр", "Drag raw fries from the crate into the fryer")); Juice.Punch(_rawCrate, 0.15f, 0.3f); }
                    break;
                case FryState.Frying:
                    Toast(L.T("Ещё жарится...", "Still frying..."));
                    break;
                case FryState.Ready:
                    if (GameData.FryerTap(_lvFryer)) UnloadFryer(true);
                    else { Toast(L.T("Перетащи готовую картошку в лоток «Картошка»", "Drag the cooked fries to the Fries bin")); var bin = BinOf(Ing.Fries); if (bin != null) Juice.Punch(bin.root, 0.15f, 0.3f); }
                    break;
                case FryState.Burnt:
                    _fry = FryState.Empty;
                    Sfx.Play(Sound.Click);
                    RefreshFryer();
                    break;
            }
        }

        void OnRawCrateTap()
        {
            if (_paused || _ended || !_friesUnlocked) return;
            if (GameData.FryerTap(_lvFryer)) LoadFryer(true);
            else Toast(L.T("Потяни ящик с картошкой во фритюр", "Drag the fries crate into the fryer"));
        }

        /// <summary>Сырая картошка (бледные палочки) — для ящика и полёта.</summary>
        static RectTransform RawFries(Transform parent, float size)
        {
            var b = UIKit.Rect(parent, "RawFries");
            UIKit.PlaceFixed(b, 0.5f, 0.5f, size, size);
            float[] xs = { 0.25f, 0.38f, 0.5f, 0.62f, 0.75f, 0.32f, 0.56f, 0.68f };
            for (int i = 0; i < xs.Length; i++)
                UIKit.Shape(b, UIKit.RoundedSmall, UIKit.Hex(i % 2 == 0 ? "#FFF3C4" : "#F5E6A8"), xs[i], 0.45f + (i % 3) * 0.06f,
                    size * 0.09f, size * 0.55f, (i * 37 % 30) - 15f);
            return b;
        }

        // =====================================================================
        //  КОРОБКИ ФРИ: достань коробку, наведи на лоток с картошкой — наполнится
        // =====================================================================
        RectTransform MakeBoxGhost()
        {
            var g = UIKit.Rect(_fx, "BoxGhost");
            UIKit.PlaceFixed(g, 0.5f, 0.5f, 110f, 110f);
            _boxFill = 0f;
            _boxFilled = false;
            _boxDrawn = -1;
            DrawBoxGhost(g, 0);
            return g;
        }

        void DrawBoxGhost(RectTransform g, int step)
        {
            if (g == null || step == _boxDrawn) return;
            _boxDrawn = step;
            UIKit.Clear(g);
            Icons.FriesBox(g, 0.5f, 0.5f, 110f, step / 4f);
            if (step > 0 && step < 4)
            {
                var bar = UIKit.Bar(g, UIKit.Alpha(Color.black, 0.5f), UIKit.Gold, 0.1f, -0.12f, 0.9f, -0.02f);
                UIKit.SetFill(bar, step / 4f);
            }
        }

        void TickFriesBox(float dt)
        {
            if (_boxSrc == null) return;
            var bin = BinOf(Ing.Fries);
            int f = (int)Ing.Fries;
            if (_boxSrc.Dragging && !_boxFilled && bin != null && DragSource.Over(bin.root, _boxSrc.Pos))
            {
                if (_stock[f] <= 0)
                {
                    Toast(L.T("В лотке нет картошки — пожарь во фритюре", "No fries in the bin — use the fryer"));
                }
                else
                {
                    _boxFill += dt;
                    float need = GameData.BoxFillTime(_lvBox);
                    int step = need <= 0f ? 4 : Mathf.Clamp(Mathf.FloorToInt(_boxFill / need * 4f), 0, 4);
                    DrawBoxGhost(_boxSrc.Ghost, step);
                    if (step >= 4)
                    {
                        _boxFilled = true;
                        _stock[f]--;
                        RefreshBin(Ing.Fries);
                        PunchBin(Ing.Fries);
                        Sfx.Play(Sound.Pour);
                    }
                }
            }

            // авто-коробки
            if (!GameData.BoxAuto(_lvBox) || !_friesUnlocked) return;
            _autoBoxT += dt;
            if (_autoBoxT < 1.2f) return;
            _autoBoxT = 0f;
            if (_stock[f] <= 0) return;
            int onTray = 0;
            foreach (var it in _tray) if (it.isDrink && it.drink == Drink.Fries) onTray++;
            for (int i = 0; i < _customers.Length; i++)
            {
                var c = _customers[i];
                if (c == null || c.leaving || !c.WantsDrink(Drink.Fries)) continue;
                if (GameData.BoxDeliver(_lvBox))
                {
                    _stock[f]--;
                    RefreshBin(Ing.Fries);
                    var ic = Icons.FriesBox(_fx, 0.5f, 0.5f, 80f, 1f);
                    Juice.Fly(ic, bin.root.position, c.BubbleWorld, 0.35f, 0f, null);
                    c.Deliver(true);
                    Sfx.Play(Sound.Click);
                    if (c.Done) Pay(i, c);
                    return;
                }
                if (onTray > 0) { onTray--; continue; }
                if (_tray.Count >= GameData.TrayCapacity) return;
                _stock[f]--;
                RefreshBin(Ing.Fries);
                var ic2 = Icons.FriesBox(_fx, 0.5f, 0.5f, 80f, 1f);
                Juice.Fly(ic2, bin.root.position, _trayRoot.position, 0.35f, 0f, null);
                _tray.Add(new TrayItem { isDrink = true, drink = Drink.Fries });
                RefreshTray();
                return;
            }
        }

        void OnBoxDrop(Vector2 pos)
        {
            if (!_boxFilled)
            {
                Toast(L.T("Держи коробку над лотком с картошкой, пока не наполнится", "Hold the box over the fries bin until it fills"));
                var bin = BinOf(Ing.Fries);
                if (bin != null) Juice.Punch(bin.root, 0.15f, 0.3f);
                return;
            }
            _boxFilled = false;
            if (_tray.Count >= GameData.TrayCapacity)
            {
                _stock[(int)Ing.Fries]++;
                RefreshBin(Ing.Fries);
                Toast(L.T("Выдача заполнена — картошка вернулась в лоток", "Tray is full — fries went back to the bin"));
                Sfx.Play(Sound.Error);
                return;
            }
            _tray.Add(new TrayItem { isDrink = true, drink = Drink.Fries });
            Sfx.Play(Sound.Done);
            RefreshTray();
        }

        void RefreshFryer()
        {
            UIKit.Clear(_basketContent);
            UIKit.SetFill(_fryFill, 0f);
            _fryTint.color = new Color(0, 0, 0, 0);
            if (!_friesUnlocked) { _fryText.text = ""; return; }
            switch (_fry)
            {
                case FryState.Empty:
                    _fryText.text = GameData.FryerAuto(_lvFryer) ? L.T("Авто-корзина", "Auto-basket")
                        : L.T("Тащи сырую сюда ↑", "Drag raw fries here ↑");
                    break;
                case FryState.Frying:
                    Icons.Ingredient(_basketContent, Ing.Fries, 0.5f, 0.5f, 80f);
                    _fryText.text = L.T("Жарится...", "Frying...");
                    break;
                case FryState.Ready:
                    Icons.Ingredient(_basketContent, Ing.Fries, 0.5f, 0.5f, 90f);
                    _fryText.text = L.T("ГОТОВО! Тащи в лоток", "READY! Drag to the bin");
                    break;
                case FryState.Burnt:
                    Icons.Ingredient(_basketContent, Ing.Fries, 0.5f, 0.5f, 80f);
                    _fryTint.color = new Color(0.1f, 0.05f, 0f, 0.8f);
                    _fryText.text = L.T("Сгорело! Почистить", "Burnt! Clean");
                    break;
            }
        }

        // =====================================================================
        //  ЛОТКИ И СБОРКА
        // =====================================================================
        void OnBin(Ing id)
        {
            if (_suppressClick) { _suppressClick = false; return; }
            if (_paused || _ended) return;
            var b = BinOf(id);
            if (b == null || b.locked) return;
            int i = (int)id;

            if (!_hasLavash)
            {
                Toast(L.T("Сначала положи лаваш", "Put a lavash first"));
                Sfx.Play(Sound.Error);
                Juice.Shake(b.root, 6f, 0.2f);
                return;
            }
            if (_cur[i] >= MaxPerIngredient) { Toast(L.T("Хватит, больше не влезет!", "That's enough of that!")); Sfx.Play(Sound.Error); return; }
            if (_stock[i] <= 0)
            {
                Toast(EmptyHint(id));
                Sfx.Play(Sound.Error);
                Juice.Shake(b.root, 6f, 0.2f);
                return;
            }
            _stock[i]--;
            _cur[i]++;
            Sfx.Play(Sound.Click);
            RefreshBin(id);
            PunchBin(id);
            RefreshBoard();
            Juice.Punch(_boardRt, 0.04f, 0.15f);
        }

        string EmptyHint(Ing id)
        {
            var d = GameData.Get(id);
            switch (d.restock)
            {
                case Restock.Spit: return L.T("Нет мяса: режь на вертеле", "No meat: cut on the spit");
                case Restock.Chop: return L.T("Кончилось. Нарежь на доске: ", "Empty. Chop on the board: ") + d.Name;
                case Restock.Fryer: return L.T("Нет картошки: сырую — во фритюр, готовую — в лоток", "No fries: raw into the fryer, cooked into the bin");
                default: return L.T("Соус кончился: зажми бутылку", "Out of sauce: hold the bottle");
            }
        }

        void OnLavash()
        {
            if (_paused || _ended) return;
            if (_hasLavash) { Toast(L.T("Лаваш уже на доске", "Lavash is already on the board")); return; }
            if (_dirty) { Toast(L.T("Доска грязная! Протри её — проведи пальцем по доске", "The board is dirty! Swipe it clean")); Sfx.Play(Sound.Error); Juice.Shake(_boardRt, 8f, 0.2f); return; }
            _hasLavash = true;
            System.Array.Clear(_cur, 0, _cur.Length);
            _curFresh = TakeHotLavash();
            if (_curFresh) Juice.FloatText(_boardRt, L.T("Горячий лаваш!", "Hot lavash!"), UIKit.Orange, 34, 0.33f, 0.8f);
            Sfx.Play(Sound.Click);
            RefreshBoard();
        }

        void OnWrap()
        {
            if (_paused || _ended) return;
            if (!_hasLavash || GameData.Total(_cur) == 0) { Toast(L.T("Сначала собери шаурму", "Build a shawarma first")); Sfx.Play(Sound.Error); return; }
            int slot = -1;
            for (int s = 0; s < _grill.Length; s++) if (_grill[s].counts == null) { slot = s; break; }
            if (slot < 0) { Toast(L.T("Гриль занят — сними готовое", "Grill is full — take one off")); Sfx.Play(Sound.Error); return; }

            var g = _grill[slot];
            g.counts = (int[])_cur.Clone();
            g.fresh = _curFresh;
            _curFresh = false;
            g.t = 0f;
            g.burntPlayed = false;
            g.roll.color = Icons.Toast(0f);
            g.content.text = GameData.ShortList(g.counts);

            // полёт свёрнутой шаурмы с доски на гриль
            var flyer = Icons.Shawarma(_fx, 0.5f, 0.5f, 120f);
            flyer.color = Icons.Toast(0f);
            var gv = g;
            Juice.Fly((RectTransform)flyer.transform.parent, _boardRt.position, g.view.position, 0.3f, 0f, () =>
            {
                if (gv.counts != null && gv.roll != null) gv.roll.transform.parent.gameObject.SetActive(true);
                if (gv.view != null) Juice.Punch(gv.view, 0.1f, 0.2f);
            });

            _hasLavash = false;
            System.Array.Clear(_cur, 0, _cur.Length);
            Sfx.Play(Sound.Wrap);
            _wrapsSinceClean++;
            if (_wrapsSinceClean >= DirtyAfter) MakeDirty();
            RefreshBoard();
        }

        void MakeDirty()
        {
            _dirty = true;
            _wipeAcc = 0f;
            _dirtRt = UIKit.Full(_boardRt, "Dirt");
            var rnd = new System.Random(_wrapsSinceClean * 7 + (int)_elapsed);
            for (int i = 0; i < 9; i++)
                UIKit.Shape(_dirtRt, UIKit.Circle, new Color(0.35f, 0.2f, 0.08f, 0.45f),
                    0.1f + (float)rnd.NextDouble() * 0.8f, 0.15f + (float)rnd.NextDouble() * 0.7f, 30f + (float)rnd.NextDouble() * 40f, 20f + (float)rnd.NextDouble() * 30f);
            var t = UIKit.FitLabel(_dirtRt, L.T("ГРЯЗНО! Протри свайпом", "DIRTY! Swipe to clean"), 30, Color.white, 0.05f, 0.35f, 0.95f, 0.65f);
            UIKit.AddOutline(t, UIKit.Dark, 3f);
            t.gameObject.AddComponent<Blinker>();
            if (_save.Lvl(Up.Board) >= 3)
            {
                // робо-губка сама ездит по доске
                t.text = L.T("Робо-губка чистит...", "Robo-sponge cleaning...");
                var sp = UIKit.Rect(_dirtRt, "Sponge");
                UIKit.PlaceFixed(sp, 0.5f, 0.5f, 90f, 60f);
                UIKit.Shape(sp, UIKit.RoundedSmall, UIKit.Hex("#FDD835"), 0.5f, 0.45f, 80f, 40f);
                UIKit.Shape(sp, UIKit.RoundedSmall, UIKit.Hex("#43A047"), 0.5f, 0.75f, 80f, 14f);
                UIKit.Shape(sp, UIKit.Circle, UIKit.Hex("#E53935"), 0.5f, 1f, 12f, 12f).gameObject.AddComponent<Blinker>().speed = 6f;
                sp.gameObject.AddComponent<Swayer>().amp = 140f;
                _roboT = 1.6f;
                return;
            }
            Toast(L.T("Доска запачкалась — протри её!", "The board got dirty — wipe it!"));
        }

        void OnBoardWipe(float dist, Vector2 pos)
        {
            if (!_dirty || _paused) return;
            _wipeAcc += dist;
            var cg = _dirtRt.GetComponent<CanvasGroup>();
            if (cg == null) cg = _dirtRt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f - _wipeAcc / WipeNeed;
            if (_wipeAcc > 150f && Random.value < 0.15f) Sfx.Play(Sound.Pour);
            if (_wipeAcc >= WipeNeed) CleanBoard();
        }

        void CleanBoard()
        {
            if (!_dirty) return;
            _dirty = false;
            _wrapsSinceClean = 0;
            _roboT = -1f;
            if (_dirtRt != null) Destroy(_dirtRt.gameObject);
            _dirtRt = null;
            Sfx.Play(Sound.Done);
            Toast(L.T("Чисто!", "Clean!"));
            Juice.Burst(_boardRt, Color.white, 10, 0.5f, 0.5f, 14f);
        }

        void OnTrash()
        {
            if (_paused || _ended) return;
            if (_selected >= 0 && _selected < _tray.Count)
            {
                _tray.RemoveAt(_selected);
                _selected = -1;
                Sfx.Play(Sound.Error);
                RefreshTray();
                return;
            }
            if (!_hasLavash) { Toast(L.T("Нечего выбрасывать", "Nothing to throw away")); return; }
            _hasLavash = false;
            _curFresh = false;
            System.Array.Clear(_cur, 0, _cur.Length);
            Sfx.Play(Sound.Error);
            RefreshBoard();
        }

        BinView BinOf(Ing id)
        {
            foreach (var b in _bins) if (b.id == id) return b;
            return null;
        }

        void PunchBin(Ing id)
        {
            var b = BinOf(id);
            if (b != null) Juice.Punch(b.root, 0.12f, 0.2f);
        }

        void RefreshBin(Ing id)
        {
            var b = BinOf(id);
            if (b == null || b.locked) return;
            int i = (int)id;
            b.count.text = _stock[i].ToString();
            b.count.color = _stock[i] == 0 ? UIKit.Red : UIKit.Dark;
            UIKit.SetFill(b.fill, (float)_stock[i] / _cap[i]);
        }

        void RefreshBoard()
        {
            UIKit.Clear(_boardContent);
            if (!_hasLavash)
            {
                _boardHint.text = L.T("Нажми на лаваш →", "Tap the lavash →");
                _boardList.text = "";
                return;
            }
            _boardHint.text = "";
            ShawarmaView.DrawOpen(_boardContent, _cur, 0.33f, 0.5f, 270f, 150f);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _cur.Length; i++)
            {
                if (_cur[i] <= 0) continue;
                sb.Append("• ").Append(GameData.Ingredients[i].Name);
                if (_cur[i] > 1) sb.Append("  <color=#C62828>×").Append(_cur[i]).Append("</color>");
                sb.Append('\n');
            }
            if (_curFresh) sb.Insert(0, "<color=#E65100>" + L.T("Горячий лаваш!", "Hot lavash!") + "</color>\n");
            _boardList.text = sb.Length > 0 ? sb.ToString() : L.T("Добавь начинку\nиз лотков", "Add fillings\nfrom the bins");
        }

        // =====================================================================
        //  ГРИЛЬ И ВЫДАЧА
        // =====================================================================
        void OnGrill(int s)
        {
            if (_paused || _ended) return;
            var g = _grill[s];
            if (TapFire(g)) return;
            if (g.counts == null) { Toast(L.T("Собери шаурму и нажми «Завернуть»", "Build a shawarma and tap Wrap")); return; }
            if (g.t < _grillTime)
            {
                float k = g.t / _grillTime;
                if (!g.flipped && k >= 0.4f && k <= FlipEnd)
                {
                    g.flipped = true;
                    FlipVisual(g);
                    Sfx.Play(Sound.Wrap);
                    Juice.FloatText(g.view, L.T("Перевернул!", "Flipped!"), UIKit.Gold, 28, 0.5f, 0.75f);
                    return;
                }
                Toast(g.flipped || k < 0.4f ? L.T("Ещё жарится...", "Still grilling...") : L.T("Поздно переворачивать", "Too late to flip"));
                return;
            }

            if (g.t >= _grillTime + Burn(g))
            {
                ClearGrill(g);
                Sfx.Play(Sound.Error);
                Toast(L.T("Сгоревшую — в мусор", "Burnt one goes to the trash"));
                return;
            }
            if (_tray.Count >= GameData.TrayCapacity) { Toast(L.T("Выдача заполнена", "Serving tray is full")); Sfx.Play(Sound.Error); return; }
            float q = (g.t - _grillTime) / Burn(g);
            TakeFromGrill(s, q < PerfectLimit && !g.uneven);
        }

        void FlipVisual(GrillSlot g)
        {
            var box = g.roll.transform.parent;
            box.localRotation = Quaternion.Euler(0, 0, 180f);
            Juice.Punch(box, 0.2f, 0.25f);
        }

        void TakeFromGrill(int s, bool perfect)
        {
            var g = _grill[s];
            _tray.Add(new TrayItem { isDrink = false, counts = g.counts, perfect = perfect, fresh = g.fresh, pack = _packAuto ? 1 : 0 });
            if (perfect) Juice.FloatText(g.view, L.T("Идеально!", "Perfect!"), UIKit.Gold, 34, 0.5f, 0.7f);
            ClearGrill(g);
            Sfx.Play(Sound.Done);
            RefreshTray();
        }

        /// <summary>Окно "готово" до пригорания. Не перевернул вовремя — окно вдвое короче.</summary>
        float Burn(GrillSlot g) { return g.uneven ? _grillBurn * 0.5f : _grillBurn; }

        float FlipEnd { get { return _lvGrill >= 1 ? 0.9f : 0.8f; } }
        float PerfectLimit { get { return 1f - GameData.PerfectWindow + (Meta.Has(_save, Meta.CharmSpatula) ? 0.15f : 0f); } }

        void ClearGrill(GrillSlot g)
        {
            g.counts = null;
            g.fresh = false;
            g.fireT = 0f;
            g.flipped = false;
            g.uneven = false;
            g.t = 0f;
            g.roll.transform.parent.gameObject.SetActive(false);
            g.roll.transform.parent.localScale = Vector3.one;
            g.roll.transform.parent.localRotation = Quaternion.identity;
            foreach (var m in g.marks) m.color = new Color(0.25f, 0.12f, 0.05f, 0f);
            UIKit.SetFill(g.fill, 0f);
            g.label.text = L.T("Пусто", "Empty");
            g.label.color = UIKit.Alpha(UIKit.Cream, 0.6f);
            g.content.text = "";
        }

        void OnDrink(Drink d)
        {
            if (_paused || _ended) return;
            if (_tray.Count >= GameData.TrayCapacity) { Toast(L.T("Выдача заполнена", "Serving tray is full")); Sfx.Play(Sound.Error); return; }
            _tray.Add(new TrayItem { isDrink = true, drink = d });
            Sfx.Play(Sound.Click);
            RefreshTray();
        }

        void OnTray(int i)
        {
            if (i >= _tray.Count) return;
            _selected = _selected == i ? -1 : i;
            Sfx.Play(Sound.Click);
            RefreshTray();
        }

        /// <summary>Какую шаурму упаковывать: выбранную, иначе первую подходящую.</summary>
        int PackTarget(bool bag)
        {
            if (_selected >= 0 && _selected < _tray.Count && !_tray[_selected].isDrink)
            {
                var it = _tray[_selected];
                if (bag ? it.pack == 1 : it.pack == 0) return _selected;
            }
            for (int i = 0; i < _tray.Count; i++)
            {
                var it = _tray[i];
                if (it.isDrink) continue;
                if (bag ? it.pack == 1 : it.pack == 0) return i;
            }
            return -1;
        }

        void OnPack(bool bag)
        {
            if (_paused || _ended) return;
            if (_save.Lvl(Up.Pack) >= 4)
            {
                // ЛЕГЕНДА: пакует всё разом
                int n = 0;
                foreach (var t in _tray)
                    if (!t.isDrink && (bag ? t.pack == 1 : t.pack == 0)) { t.pack = bag ? 2 : 1; t.packProgress = 0; n++; }
                if (n > 0) { Sfx.Play(Sound.Wrap); RefreshTray(); return; }
            }
            int i = PackTarget(bag);
            if (i < 0)
            {
                Toast(bag ? L.T("Нечего класть в пакет: сначала «Обёртка»", "Nothing to bag: wrap first") : L.T("Нечего заворачивать", "Nothing to wrap"));
                Sfx.Play(Sound.Error);
                return;
            }
            var it = _tray[i];
            it.packProgress++;
            Sfx.Play(Sound.Pour);
            if (it.packProgress >= _packTaps)
            {
                it.packProgress = 0;
                it.pack = bag ? 2 : 1;
                Sfx.Play(Sound.Wrap);
            }
            RefreshTray();
            Juice.Punch(bag ? _bagBtn.transform : _wrapBtn.transform, 0.12f, 0.2f);
        }

        void RefreshTray()
        {
            UIKit.Clear(_trayRoot);
            int n = GameData.TrayCapacity;
            for (int i = 0; i < n; i++)
            {
                float y1 = 1f - i * (1f / n), y0 = y1 - (1f / n) + 0.03f;
                if (i >= _tray.Count)
                {
                    var empty = UIKit.Panel(_trayRoot, "Empty", new Color(1, 1, 1, 0.08f), 0.03f, y0, 0.97f, y1);
                    UIKit.FitLabel(empty.transform, L.T("пусто", "empty"), 18, UIKit.Alpha(Color.white, 0.3f), 0.1f, 0.3f, 0.9f, 0.7f);
                    continue;
                }
                var it = _tray[i];
                bool sel = i == _selected;
                var bg = UIKit.Panel(_trayRoot, "Item", sel ? UIKit.Gold : UIKit.Cream, 0.03f, y0, 0.97f, y1);
                UIKit.AddShadow(bg, 4f, 0.25f);
                int idx = i;
                UIKit.MakeButton(bg, () => OnTray(idx));
                if (it.isDrink)
                {
                    Icons.DrinkIcon(bg.transform, it.drink, 0.2f, 0.5f, 52f);
                    UIKit.FitLabel(bg.transform, GameData.Get(it.drink).Name, 24, UIKit.Dark, 0.38f, 0.1f, 0.98f, 0.9f);
                    continue;
                }
                if (it.pack == 2) Icons.Bag(bg.transform, 0.17f, 0.5f, 50f);
                else
                {
                    var roll = Icons.Shawarma(bg.transform, 0.17f, 0.5f, 56f);
                    roll.color = Icons.Toast(1f);
                    if (it.pack == 1)
                    {
                        UIKit.Panel(roll.transform, "Wrap", UIKit.Hex("#FAFAFA"), 0.05f, -0.1f, 0.6f, 1.1f);
                        UIKit.Panel(roll.transform, "WrapLine", UIKit.Hex("#E53935"), 0.05f, 0.42f, 0.6f, 0.56f, false);
                    }
                }
                if (it.perfect) Icons.Star(bg.transform, 0.06f, 0.85f, 22f, true);
                if (it.fresh) UIKit.Shape(bg.transform, UIKit.Circle, UIKit.Orange, 0.06f, 0.18f, 16f, 20f).gameObject.AddComponent<Blinker>().speed = 3f;
                string state = it.pack == 0 ? L.T("не упакована", "not packed") : (it.pack == 1 ? L.T("в обёртке", "wrapped") : L.T("в пакете", "bagged"));
                UIKit.FitLabel(bg.transform, GameData.ShortList(it.counts), 16, UIKit.Dark, 0.34f, 0.45f, 0.98f, 0.97f);
                UIKit.FitLabel(bg.transform, state, 16, it.pack == 0 ? UIKit.Red : UIKit.Green, 0.34f, 0.05f, 0.98f, 0.45f);
            }
            RefreshPackButtons();
        }

        void RefreshPackButtons()
        {
            if (_wrapBtn == null) return;
            int w = PackTarget(false), b = PackTarget(true);
            string wl = L.T("ОБЁРТКА", "WRAP");
            if (w >= 0 && _packTaps > 1) wl += "\n" + _tray[w].packProgress + "/" + _packTaps;
            string bl = L.T("ПАКЕТ", "BAG");
            if (b >= 0 && _packTaps > 1) bl += "\n" + _tray[b].packProgress + "/" + _packTaps;
            UIKit.SetLabel(_wrapBtn, wl);
            UIKit.SetLabel(_bagBtn, bl);
            _wrapBtn.interactable = w >= 0;
            _bagBtn.interactable = b >= 0;
        }

        // =====================================================================
        //  ПОДСКАЗКИ (дни 1–2)
        // =====================================================================
        string Hint()
        {
            if (_eday > 2) return _eday <= 5 ? FriesHint() : "";
            foreach (var it in _tray)
                if (!it.isDrink && it.pack == 0) return L.T("Упакуй: «Обёртка» — здесь, потом «Пакет» — с собой", "Pack it: Wrap for here, then Bag to go");
            foreach (var c in _customers)
                if (c != null && !c.leaving && !c.modeKnown) return L.T("Нажми на клиента с «?», чтобы спросить: здесь или с собой", "Tap a customer with “?” to ask: here or to go");
            if (_tray.Count > 0) return L.T("Нажми на клиента — отдашь заказ", "Tap the customer to serve");
            foreach (var g in _grill)
                if (g.counts != null && g.t >= _grillTime && g.t < _grillTime + Burn(g))
                    return L.T("Готово! Сними с гриля сразу — будет «идеально»", "Ready! Take it right away for a perfect grill");
            foreach (var g in _grill)
                if (g.counts != null && g.t < _grillTime) return L.T("Жарится... Следи, чтобы не сгорела", "Grilling... don't let it burn");
            if (_stock[(int)Ing.Meat] == 0 && _spitLeft > 0)
                return L.T("Нет мяса: води ножом по вертелу (" + _piecesNeeded + " куска = порция)", "No meat: swipe the spit (" + _piecesNeeded + " pieces = portion)");
            if (_boardVeg >= 0) return L.T("Стучи по доске, чтобы нарезать", "Tap the board to chop");
            foreach (var v in Choppables)
                if (IsUnlocked(v) && _stock[(int)v] == 0)
                    return L.T("Кончилось: ", "Out of: ") + GameData.Get(v).Name + L.T(". Перетащи из ящика на доску и нарежь", ". Drag it from the crate to the board and chop");
            if (_fry == FryState.Ready) return L.T("Картошка готова — перетащи корзину в лоток «Картошка»", "Fries ready — drag the basket to the Fries bin");
            foreach (var c in _customers)
                if (c != null && !c.leaving && c.WantsDrink(Drink.Fries))
                    return L.T("Клиент ждёт фри: тащи коробку из-под стола на лоток «Картошка»", "A customer wants fries: drag a box onto the Fries bin");
            if (!_hasLavash) return L.T("Смотри заказ в пузыре и нажми на лаваш", "Check the order bubble, then tap the lavash");
            if (GameData.Total(_cur) == 0) return L.T("Добавь начинку из лотков, как в заказе", "Add fillings from the bins");
            return L.T("Всё положил? Жми «Завернуть»", "All in? Tap Wrap");
        }

        /// <summary>Подсказки про картошку в первые дни после её открытия.</summary>
        string FriesHint()
        {
            if (!_friesUnlocked) return "";
            if (_fry == FryState.Ready) return L.T("Картошка готова — перетащи корзину в лоток «Картошка»", "Fries ready — drag the basket to the Fries bin");
            foreach (var c in _customers)
                if (c != null && !c.leaving && c.WantsDrink(Drink.Fries))
                    return L.T("Клиент ждёт фри: тащи коробку из-под стола на лоток «Картошка»", "A customer wants fries: drag a box onto the Fries bin");
            if (_stock[(int)Ing.Fries] == 0 && _fry == FryState.Empty)
                return L.T("Нет картошки: перетащи сырую из ящика во фритюр", "No fries: drag raw fries into the fryer");
            return "";
        }

        // =====================================================================
        //  ПОСТРОЕНИЕ КУХНИ
        // =====================================================================
        /// <summary>Панель станции: тёмная шапка с названием, точки уровня (1–4), значок АВТО.</summary>
        RectTransform Station(string name, string title, float x0, float x1, Color color, int level, bool auto)
        {
            var p = UIKit.Panel(_root, name, color, x0, 0.015f, x1, 0.448f);
            UIKit.AddShadow(p, 8f, 0.3f);
            StationLook.Frame(p, level);
            var head = UIKit.Panel(p.transform, "Head", UIKit.Alpha(Color.black, 0.35f), 0.02f, 0.895f, 0.98f, 0.985f);
            var t = UIKit.Label(head.transform, title.ToUpper(), 22, UIKit.Cream, TextAnchor.MiddleLeft, 0.06f, 0f, 0.7f, 1f, true);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = 22;
            UIKit.AddShadow(t, 2f, 0.6f);
            if (level >= 0)
            {
                for (int i = 0; i < 4; i++)
                    UIKit.Shape(head.transform, UIKit.Circle, i <= level ? UIKit.Gold : UIKit.Alpha(Color.white, 0.2f),
                        0.74f + i * 0.07f, 0.5f, 11f, 11f);
            }
            if (auto)
            {
                var badge = UIKit.Panel(p.transform, "Auto", UIKit.Gold, 0.6f, 0.83f, 0.97f, 0.885f);
                UIKit.FitLabel(badge.transform, L.T("АВТО", "AUTO"), 18, UIKit.Dark, 0.05f, 0.05f, 0.95f, 0.95f);
            }
            return p.rectTransform;
        }

        void BuildKitchen()
        {
            var wall = UIKit.Full(_root, "KitchenWall");
            UIKit.Place(wall, 0, 0, 1, 0.46f);
            var tiles = wall.gameObject.AddComponent<Image>();
            tiles.sprite = UIKit.Tile;
            tiles.type = Image.Type.Tiled;
            tiles.color = UIKit.Hex("#EDE6DA");
            tiles.raycastTarget = false;

            var stationCol = UIKit.Hex("#3F4A52");
            BuildSpit(Station("Spit", L.T("Вертел", "Spit"), 0.006f, 0.118f, stationCol, _lvSpit, GameData.SpitAuto(_lvSpit)));
            BuildChop(Station("Chop", L.T("Нарезка", "Chopping"), 0.124f, 0.3f, stationCol, _lvKnife, GameData.KnifeAuto(_lvKnife)));
            BuildFryer(Station("Fryer", L.T("Фритюр", "Fryer"), 0.306f, 0.412f, stationCol, _lvFryer, GameData.FryerAuto(_lvFryer)));
            BuildAssembly(Station("Assembly", L.T("Сборка", "Assembly"), 0.418f, 0.772f, UIKit.Hex("#56636C"), -1, GameData.SauceAuto(_lvSauce)));
            BuildGrill(Station("Grill", L.T("Гриль", "Grill"), 0.778f, 0.888f, stationCol, _lvGrill, GameData.GrillAuto(_lvGrill)));
            BuildServe(Station("Serve", L.T("Выдача", "Serve"), 0.894f, 0.994f, UIKit.Hex("#5B4636"), -1, false));
        }

        void BuildSpit(RectTransform p)
        {
            _spitPanel = p;
            var heat = UIKit.Panel(p, "Heater", UIKit.Hex("#2A2A2A"), 0.06f, 0.2f, 0.26f, 0.86f);
            for (int i = 0; i < 5; i++)
                UIKit.Panel(heat.transform, "Glow", UIKit.Hex("#FF6A2B"), 0.2f, 0.08f + i * 0.18f, 0.8f, 0.16f + i * 0.18f);
            UIKit.Shape(p, UIKit.RoundedSmall, UIKit.SteelLight, 0.6f, 0.54f, 8f, 320f);
            float[] w = { 110f, 126f, 136f, 140f, 134f, 122f, 104f };
            _cone = new Image[w.Length];
            _coneW = w;
            for (int i = 0; i < w.Length; i++)
            {
                float y = 0.27f + i * 0.08f;
                var col = i % 2 == 0 ? UIKit.Hex("#A8461F") : UIKit.Hex("#8E3A18");
                _cone[i] = UIKit.Shape(p, UIKit.Rounded, col, 0.6f, y, w[i], 44f);
                UIKit.Shape(_cone[i].transform, UIKit.RoundedSmall, UIKit.Hex("#D9824F"), 0.3f, 0.7f, 30f, 6f);
            }
            var zoneRt = UIKit.Rect(p, "SwipeZone");
            UIKit.Place(zoneRt, 0.02f, 0.18f, 0.98f, 0.88f);
            var zimg = zoneRt.gameObject.AddComponent<Image>();
            zimg.color = new Color(0, 0, 0, 0);
            _spitZone = zoneRt.gameObject.AddComponent<DragZone>();
            _spitZone.onDrag = OnSpitDrag;
            _spitZone.onDown = OnSpitDown;

            // вид по уровням
            if (_lvSpit >= 1) UIKit.Panel(p, "Drip", UIKit.Hex("#90A4AE"), 0.33f, 0.19f, 0.88f, 0.225f);
            if (_lvSpit >= 2)
            {
                var h2 = UIKit.Panel(p, "Heater2", UIKit.Hex("#2A2A2A"), 0.9f, 0.3f, 0.97f, 0.8f);
                for (int i = 0; i < 4; i++)
                    UIKit.Panel(h2.transform, "Glow", UIKit.Hex("#FF6A2B"), 0.2f, 0.08f + i * 0.23f, 0.8f, 0.2f + i * 0.23f);
                UIKit.Shape(p, UIKit.Circle, UIKit.Hex("#CFD8DC"), 0.6f, 0.88f, 60f, 16f);
            }
            if (GameData.SpitAuto(_lvSpit)) StationLook.Auto(p, Up.Spit, 0.8f, 0.55f, 72f);

            _knife = UIKit.Rect(_root, "Knife");
            UIKit.PlaceFixed(_knife, 0.5f, 0.5f, 120f, 30f);
            UIKit.Shape(_knife, UIKit.RoundedSmall, UIKit.Hex("#DDE3E7"), 0.62f, 0.5f, 80f, 22f);
            UIKit.Shape(_knife, UIKit.RoundedSmall, UIKit.Hex("#4E342E"), 0.14f, 0.5f, 36f, 16f);
            _knife.localRotation = Quaternion.Euler(0, 0, -30f);
            _knife.gameObject.SetActive(false);

            _pieceFill = UIKit.Bar(p, UIKit.Alpha(Color.black, 0.4f), UIKit.Gold, 0.08f, 0.135f, 0.92f, 0.17f);
            _spitText = UIKit.FitLabel(p, "", 24, UIKit.Cream, 0.04f, 0.02f, 0.96f, 0.12f);
            _newSpitBtn = UIKit.Button(p, L.T("Новый вертел", "New spit"), UIKit.Orange, OnNewSpit, 0.06f, 0.02f, 0.94f, 0.13f, 26);
        }

        void BuildChop(RectTransform p)
        {
            _chopPanel = p;
            var list = new List<Ing>();
            foreach (var v in Choppables) if (IsUnlocked(v)) list.Add(v);
            var crate = UIKit.Panel(p, "Crate", UIKit.Hex("#8D6E63"), 0.04f, 0.66f, 0.96f, 0.86f);
            for (int i = 0; i < list.Count; i++)
            {
                float cw = 1f / Mathf.Max(list.Count, 3);
                float x0 = i * cw;
                var cell = UIKit.Panel(crate.transform, "Veg", UIKit.Hex("#A1887F"), x0 + 0.02f, 0.08f, x0 + cw - 0.02f, 0.92f);
                var id = list[i];
                Icons.Ingredient(cell.transform, id, 0.38f, 0.5f, 50f);
                Icons.Ingredient(cell.transform, id, 0.62f, 0.45f, 46f);
                cell.raycastTarget = true;
                var cellRt = cell.rectTransform;
                var ds = cell.gameObject.AddComponent<DragSource>();
                ds.canDrag = () => !_paused && !_ended;
                ds.makeGhost = () => Icons.Ingredient(_fx, id, 0.5f, 0.5f, 90f);
                ds.onDrop = pos =>
                {
                    if (DragSource.Over(_chopBoard, pos)) OnPickVeg(id);
                    else Toast(L.T("Брось овощ на разделочную доску", "Drop the veggie on the cutting board"));
                };
                ds.onTap = () =>
                {
                    if (_paused || _ended) return;
                    if (GameData.KnifeTapCrate(_lvKnife))
                    {
                        if (_boardVeg < 0) Juice.Fly(Icons.Ingredient(_fx, id, 0.5f, 0.5f, 70f), cellRt.position, _chopBoard.position, 0.25f, 0f, null);
                        OnPickVeg(id);
                    }
                    else
                    {
                        Toast(L.T("Перетащи овощ из ящика на доску", "Drag the veggie from the crate onto the board"));
                        Juice.Punch(cellRt, 0.15f, 0.25f);
                    }
                };
            }

            var board = UIKit.Panel(p, "Board", UIKit.Hex("#D7B07A"), 0.05f, 0.13f, 0.95f, 0.62f);
            UIKit.AddShadow(board, 5f, 0.3f);
            _chopBoard = board.rectTransform;
            UIKit.Panel(board.transform, "Grain", UIKit.Alpha(Color.black, 0.06f), 0.05f, 0.45f, 0.95f, 0.48f);
            UIKit.Panel(board.transform, "Grain", UIKit.Alpha(Color.black, 0.06f), 0.05f, 0.25f, 0.95f, 0.27f);
            _chopHolder = UIKit.Full(board.transform, "Veg");
            UIKit.HitArea(board.transform, "Tap", OnChop);
            if (_lvKnife >= 1) UIKit.AddOutline(board, UIKit.Hex("#8D6E63"), 3f);
            if (_lvKnife >= 2)
            {
                UIKit.Shape(board.transform, UIKit.RoundedSmall, UIKit.Hex("#CFD8DC"), 0.12f, 0.85f, 70f, 34f, 10f);
                UIKit.Shape(board.transform, UIKit.RoundedSmall, UIKit.Hex("#4E342E"), 0.03f, 0.83f, 30f, 12f, 10f);
            }
            if (GameData.KnifeAuto(_lvKnife)) StationLook.Auto(board.transform, Up.Knife, 0.85f, 0.72f, 64f);

            _chopText = UIKit.FitLabel(p, "", 26, UIKit.Cream, 0.04f, 0.02f, 0.96f, 0.12f);
        }

        void BuildFryer(RectTransform p)
        {
            _fryPanel = p;
            var body = UIKit.Panel(p, "Body", UIKit.SteelLight, 0.07f, 0.4f, 0.93f, 0.86f);
            UIKit.AddShadow(body, 4f, 0.3f);
            _fryBody = body.rectTransform;
            UIKit.Panel(body.transform, "Oil", UIKit.Hex("#C9951B"), 0.08f, 0.08f, 0.92f, 0.55f);
            _basket = UIKit.Panel(body.transform, "Basket", UIKit.Hex("#6D7B84"), 0.14f, 0.3f, 0.86f, 0.92f);
            for (int i = 0; i < 4; i++)
                UIKit.Panel(_basket.transform, "Mesh", UIKit.Alpha(Color.black, 0.18f), 0.1f + i * 0.22f, 0.08f, 0.14f + i * 0.22f, 0.92f, false);
            _basketContent = UIKit.Full(_basket.transform, "Content");
            _fryTint = UIKit.Panel(body.transform, "Tint", new Color(0, 0, 0, 0), 0f, 0f, 1f, 1f);
            if (_lvFryer >= 1) body.color = UIKit.Hex("#CFD8DC");
            if (_lvFryer >= 2) UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Dark, 0.5f, 0.97f, 90f, 10f);
            if (GameData.FryerAuto(_lvFryer))
            {
                StationLook.Auto(p, Up.Fryer, 0.85f, 0.62f, 70f);
                var bb = _basket.gameObject.AddComponent<Bobber>();
                bb.amp = 8f;
                bb.speed = 2.5f;
            }
            // корзину с готовой картошкой можно тащить в лоток
            var hit = UIKit.Full(body.transform, "Drag");
            var himg = hit.gameObject.AddComponent<Image>();
            himg.color = new Color(0, 0, 0, 0);
            _fryDrag = hit.gameObject.AddComponent<DragSource>();
            _fryDrag.canDrag = () => !_paused && !_ended && _fry == FryState.Ready;
            _fryDrag.makeGhost = () => Icons.Ingredient(_fx, Ing.Fries, 0.5f, 0.5f, 100f);
            _fryDrag.onDrop = pos =>
            {
                var bin = BinOf(Ing.Fries);
                if (bin != null && DragSource.Over(bin.root, pos)) UnloadFryer(false);
                else Toast(L.T("Брось картошку в лоток «Картошка» на сборке", "Drop the fries into the Fries bin"));
            };
            _fryDrag.onTap = OnFryerTap;

            // ящик сырой картошки
            var crate = UIKit.Panel(p, "RawCrate", UIKit.Hex("#8D6E63"), 0.07f, 0.19f, 0.93f, 0.37f);
            UIKit.AddShadow(crate, 3f, 0.3f);
            UIKit.Panel(crate.transform, "Slat", UIKit.Hex("#6D4C41"), 0f, 0.42f, 1f, 0.52f, false);
            var raw = RawFries(crate.transform, 70f);
            ((RectTransform)raw).anchoredPosition = new Vector2(-20f, 8f);
            var raw2 = RawFries(crate.transform, 64f);
            ((RectTransform)raw2).anchoredPosition = new Vector2(26f, 4f);
            UIKit.FitLabel(crate.transform, L.T("сырая", "raw"), 16, UIKit.Cream, 0.05f, 0.0f, 0.95f, 0.3f);
            _rawCrate = crate.rectTransform;
            crate.raycastTarget = true;
            var rds = crate.gameObject.AddComponent<DragSource>();
            rds.canDrag = () => !_paused && !_ended && _friesUnlocked;
            rds.makeGhost = () => RawFries(_fx, 100f);
            rds.onDrop = pos =>
            {
                if (DragSource.Over(_fryBody, pos)) LoadFryer(false);
                else Toast(L.T("Брось сырую картошку во фритюр", "Drop the raw fries into the fryer"));
            };
            rds.onTap = OnRawCrateTap;

            _fryFill = UIKit.Bar(p, new Color(0, 0, 0, 0.4f), UIKit.Gold, 0.08f, 0.145f, 0.92f, 0.175f);
            UIKit.SetFill(_fryFill, 0f);
            _fryText = UIKit.FitLabel(p, "", 22, UIKit.Cream, 0.04f, 0.02f, 0.96f, 0.14f);

            if (!_friesUnlocked)
            {
                var lk = UIKit.Panel(p, "Lock", new Color(0, 0, 0, 0.72f), 0, 0, 1, 0.88f);
                lk.raycastTarget = true;
                UIKit.FitLabel(lk.transform, L.T("Откроется\nна 3-й день", "Unlocks\non day 3"), 30, UIKit.Cream, 0.05f, 0.3f, 0.95f, 0.7f);
            }
        }

        void BuildAssembly(RectTransform p)
        {
            _bins = new BinView[BinOrder.Length];
            for (int k = 0; k < 10; k++)
            {
                int col = k % 5, row = k / 5;
                float x0 = 0.015f + col * 0.197f;
                float y1 = 0.885f - row * 0.235f;
                var bg = UIKit.Panel(p, "Bin", UIKit.Hex("#E4E9EC"), x0, y1 - 0.215f, x0 + 0.185f, y1);
                UIKit.AddShadow(bg, 4f, 0.3f);

                if (k == 9)
                {
                    bg.color = UIKit.Hex("#F6E7C8");
                    Icons.Lavash(bg.transform, 0.5f, 0.55f, 76f);
                    Icons.Lavash(bg.transform, 0.5f, 0.6f, 76f);
                    UIKit.FitLabel(bg.transform, L.T("Лаваш", "Lavash"), 22, UIKit.Dark, 0.05f, 0.02f, 0.95f, 0.24f);
                    UIKit.MakeButton(bg, OnLavash);
                    BuildTandoor(bg.transform);
                    continue;
                }

                var id = BinOrder[k];
                var b = new BinView { id = id, root = bg.rectTransform };
                _bins[k] = b;

                if (!IsUnlocked(id))
                {
                    b.locked = true;
                    bg.color = UIKit.Hex("#7D878E");
                    UIKit.FitLabel(bg.transform, L.T("День ", "Day ") + Mathf.Max(2, GameData.Get(id).unlockDay - _save.location * 3), 24, UIKit.Alpha(Color.white, 0.7f), 0.05f, 0.3f, 0.95f, 0.7f);
                    continue;
                }

                Icons.Ingredient(bg.transform, id, 0.5f, 0.58f, 64f);
                b.fill = UIKit.Bar(bg.transform, UIKit.Alpha(Color.black, 0.15f), UIKit.Green, 0.08f, 0.06f, 0.92f, 0.15f);
                b.count = UIKit.Label(bg.transform, "", 26, UIKit.Dark, TextAnchor.UpperRight, 0.5f, 0.6f, 0.95f, 0.97f, true);
                UIKit.MakeButton(bg, () => OnBin(id));
                if (GameData.Get(id).restock == Restock.Sauce)
                {
                    b.zone = bg.gameObject.AddComponent<DragZone>();
                    if (GameData.SauceAuto(_lvSauce)) StationLook.Auto(bg.transform, Up.Sauce, 0.16f, 0.82f, 40f);
                }
            }

            var board = UIKit.Panel(p, "Board", UIKit.Hex("#D7B07A"), 0.015f, 0.03f, 0.68f, 0.39f);
            UIKit.AddShadow(board, 5f, 0.3f);
            _boardRt = board.rectTransform;
            board.raycastTarget = true;
            var bz = board.gameObject.AddComponent<DragZone>();
            bz.onDrag = OnBoardWipe;
            _boardContent = UIKit.Full(board.transform, "Content");
            _boardHint = UIKit.Label(board.transform, "", 32, UIKit.Alpha(UIKit.Dark, 0.55f), TextAnchor.MiddleCenter, 0, 0, 1, 1, true);
            _boardList = UIKit.Label(board.transform, "", 20, UIKit.Dark, TextAnchor.MiddleLeft, 0.64f, 0.04f, 0.99f, 0.96f, true);
            _boardList.verticalOverflow = VerticalWrapMode.Truncate;
            _boardList.resizeTextForBestFit = true;
            _boardList.resizeTextMinSize = 12;
            _boardList.resizeTextMaxSize = 22;

            UIKit.Button(p, L.T("Завернуть", "Wrap"), UIKit.Green, OnWrap, 0.695f, 0.215f, 0.985f, 0.39f, 38);
            UIKit.Button(p, L.T("В мусор", "Trash"), UIKit.Red, OnTrash, 0.695f, 0.03f, 0.985f, 0.2f, 34);
        }

        void BuildGrill(RectTransform p)
        {
            int n = _grill.Length;
            float top = 0.87f, h = 0.84f / n;
            for (int s = 0; s < n; s++)
            {
                float y1 = top - s * h, y0 = y1 - h + 0.02f;
                var plate = UIKit.Panel(p, "Plate", UIKit.Hex("#262626"), 0.06f, y0, 0.94f, y1);
                for (int l = 0; l < 4; l++)
                    UIKit.Panel(plate.transform, "Line", UIKit.Hex("#3A3A3A"), 0.06f, 0.3f + l * 0.14f, 0.94f, 0.34f + l * 0.14f, false);
                var g = _grill[s];
                g.view = plate.rectTransform;
                float size = Mathf.Min(120f, 360f / n);
                g.roll = Icons.Shawarma(plate.transform, 0.5f, 0.58f, size);
                g.marks = ShawarmaView.GrillMarks(g.roll);
                g.fill = UIKit.Bar(plate.transform, UIKit.Alpha(Color.black, 0.5f), UIKit.Orange, 0.08f, 0.22f, 0.92f, 0.29f);
                g.label = UIKit.FitLabel(plate.transform, "", 22, UIKit.Cream, 0.04f, 0.01f, 0.96f, 0.2f);
                g.content = UIKit.FitLabel(plate.transform, "", 16, UIKit.Alpha(UIKit.Cream, 0.8f), 0.04f, 0.84f, 0.96f, 0.99f, false);
                if (GameData.GrillAuto(_lvGrill)) StationLook.Auto(plate.transform, Up.Grill, 0.87f, 0.72f, 50f);
                int idx = s;
                UIKit.HitArea(plate.transform, "Tap", () => OnGrill(idx));
                ClearGrill(g);
            }
        }

        void BuildServe(RectTransform p)
        {
            var tray = UIKit.Panel(p, "Tray", UIKit.Alpha(Color.black, 0.2f), 0.03f, 0.46f, 0.97f, 0.885f);
            _trayRoot = UIKit.Full(tray.transform, "Items");
            // большие кнопки упаковки (удобно на телефоне)
            _wrapBtn = UIKit.Button(p, L.T("ОБЁРТКА", "WRAP"), UIKit.Blue, () => OnPack(false), 0.03f, 0.25f, 0.49f, 0.445f, 24);
            _bagBtn = UIKit.Button(p, L.T("ПАКЕТ", "BAG"), UIKit.Hex("#B07A4A"), () => OnPack(true), 0.51f, 0.25f, 0.97f, 0.445f, 24);
            if (_packAuto) StationLook.Auto(p, Up.Pack, 0.9f, 0.9f, 44f);

            var fridge = UIKit.Panel(p, "Fridge", UIKit.Hex("#CFE8F5"), 0.04f, 0.02f, 0.96f, 0.24f);
            UIKit.AddShadow(fridge, 4f, 0.3f);
            var drinks = new List<Drink>();
            if (GameData.Unlocked(Drink.Cola, _eday)) drinks.Add(Drink.Cola);
            if (GameData.Unlocked(Drink.Ayran, _eday)) drinks.Add(Drink.Ayran);
            bool boxes = _friesUnlocked;
            int cells = drinks.Count + (boxes ? 1 : 0);
            if (cells == 0)
            {
                UIKit.FitLabel(fridge.transform, L.T("Напитки\nс 4-го дня", "Drinks\nfrom day 4"), 22, UIKit.Alpha(UIKit.Dark, 0.6f), 0.05f, 0.1f, 0.95f, 0.9f);
                return;
            }
            float w = 0.9f / cells;
            for (int i = 0; i < drinks.Count; i++)
            {
                float x0 = 0.05f + i * w;
                var cell = UIKit.Panel(fridge.transform, "Drink", Color.white, x0, 0.08f, x0 + w - 0.03f, 0.92f);
                var d = drinks[i];
                UIKit.MakeButton(cell, () => OnDrink(d));
                Icons.DrinkIcon(cell.transform, d, 0.5f, 0.5f, 62f);
            }
            if (boxes)
            {
                // стопка коробок для фри «под столом»: тащи коробку на лоток с картошкой
                float x0 = 0.05f + drinks.Count * w;
                var cell = UIKit.Panel(fridge.transform, "Boxes", UIKit.Hex("#FFE0B2"), x0, 0.08f, x0 + w - 0.03f, 0.92f);
                Icons.FriesBox(cell.transform, 0.42f, 0.44f, 58f, 0f);
                Icons.FriesBox(cell.transform, 0.58f, 0.56f, 58f, 0f);
                _boxCell = cell.rectTransform;
                cell.raycastTarget = true;
                _boxSrc = cell.gameObject.AddComponent<DragSource>();
                _boxSrc.canDrag = () => !_paused && !_ended;
                _boxSrc.makeGhost = MakeBoxGhost;
                _boxSrc.onDrop = OnBoxDrop;
                _boxSrc.onTap = () =>
                {
                    Toast(L.T("Тащи коробку на лоток «Картошка» и держи, пока не наполнится", "Drag a box over the Fries bin and hold until it fills"));
                    var bin = BinOf(Ing.Fries);
                    if (bin != null) Juice.Punch(bin.root, 0.15f, 0.3f);
                };
                if (GameData.BoxAuto(_lvBox)) StationLook.Auto(cell.transform, Up.FryBox, 0.85f, 0.8f, 30f);
            }
        }

        void RefreshAllKitchen()
        {
            RefreshSpit();
            RefreshChop();
            RefreshFryer();
            foreach (var b in _bins) if (b != null) RefreshBin(b.id);
            RefreshBoard();
            RefreshTray();
        }
    }
}
