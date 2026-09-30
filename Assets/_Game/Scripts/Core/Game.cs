using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Точка входа. Висит на объекте "Game" и строит всё сама:
    /// загрузка → меню (бонус дня) → карта (бустеры) → ларёк-улучшения → залёт в ларёк → день → итоги.
    /// </summary>
    public partial class Game : MonoBehaviour
    {
        SaveData _save;
        RectTransform _root, _notifyLayer;
        GameObject _screen, _rotateHint;
        DayRun _run;
        bool _readySent, _dailyChecked;
        long _lastBizIncome;
        int _lastKiosk2, _lastStaff;
        long _lastPiggy;

        // уведомления о достижениях
        class Note { public string title, text; }
        readonly Queue<Note> _notes = new Queue<Note>();
        RectTransform _noteRt;
        float _noteT = -1f;

        // =====================================================================
        void Awake()
        {
            Application.targetFrameRate = 60;
            _save = SaveSystem.Load();
            _passSeen = Meta.PassLevel(_save.passXp);
            L.Init(_save.lang);
            Sfx.Init(gameObject);
            ApplyAudio();
            BuildCanvas();
            var s = NewScreen("Loading");
            Loading.Show(s, ShowMenu);
        }

        void Update()
        {
            bool portrait = Screen.height > Screen.width;
            if (_rotateHint.activeSelf != portrait)
            {
                _rotateHint.SetActive(portrait);
                if (portrait && _run != null) _run.SetPaused(true);
            }
            UpdateNotes(Time.unscaledDeltaTime);
            TrackPlayTime(Time.unscaledDeltaTime);
        }

        // --- время в игре (для наград под Шаурма-Пассом) ---
        bool _focused = true;
        float _playSaveT;
        int _playSeenReady = -1;

        void TrackPlayTime(float dt)
        {
            if (!_focused || _save == null || dt > 1f) return;
            _save.playSeconds += dt;
            _playSaveT += dt;
            if (_playSaveT >= 30f) { _playSaveT = 0f; SaveSystem.Save(_save); }
            // впервые созрела награда за время — короткое уведомление
            int ready = 0;
            for (int i = 0; i < Meta.PlayMinutes.Length; i++) if (Meta.PlayReady(_save, i)) ready++;
            if (_playSeenReady < 0) _playSeenReady = ready;
            if (ready > _playSeenReady)
                Notify(L.T("Награда за время в игре!", "Play time reward!"), L.T("Забери её в Шаурма-Пассе", "Claim it in the Shawarma Pass"));
            _playSeenReady = ready;
        }

        void OnApplicationFocus(bool focus)
        {
            _focused = focus;
            AudioListener.pause = !focus;
            if (!focus && _run != null) _run.SetPaused(true);
        }

        void OnApplicationPause(bool paused) { OnApplicationFocus(!paused); }

        void ApplyAudio()
        {
            Sfx.SetSound(_save.sound);
            Sfx.SetMusic(_save.music);
        }

        // =====================================================================
        void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            UIKit.Panel(canvasGo.transform, "Backdrop", UIKit.Hex("#1E140E"), 0, 0, 1, 1, false);

            _root = UIKit.Rect(canvasGo.transform, "GameRoot");
            UIKit.Place(_root, 0, 0, 1, 1);
            var fitter = _root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1920f / 1080f;
            _root.gameObject.AddComponent<RectMask2D>();

            _notifyLayer = UIKit.Rect(canvasGo.transform, "Notify");
            UIKit.Place(_notifyLayer, 0, 0, 1, 1);
            var fit2 = _notifyLayer.gameObject.AddComponent<AspectRatioFitter>();
            fit2.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit2.aspectRatio = 1920f / 1080f;

            var rot = UIKit.Panel(canvasGo.transform, "Rotate", UIKit.Hex("#1E140E"), 0, 0, 1, 1, false);
            rot.raycastTarget = true;
            var phone = UIKit.Shape(rot.transform, UIKit.Rounded, UIKit.Cream, 0.5f, 0.6f, 180f, 320f, 90f);
            UIKit.Shape(phone.transform, UIKit.RoundedSmall, UIKit.Dark, 0.5f, 0.5f, 150f, 260f);
            UIKit.Label(rot.transform, L.T("Поверни телефон горизонтально", "Rotate your phone to landscape"), 60, UIKit.Cream,
                TextAnchor.MiddleCenter, 0.05f, 0.2f, 0.95f, 0.4f, true);
            _rotateHint = rot.gameObject;
            _rotateHint.SetActive(false);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        RectTransform NewScreen(string name)
        {
            if (_screen != null) Destroy(_screen);
            _run = null;
            var rt = UIKit.Full(_root, name);
            _screen = rt.gameObject;
            return rt;
        }

        void Backdrop(RectTransform s, bool dim)
        {
            var street = UIKit.Full(s, "Street");
            Scenery.Build(street, _save.Loc, false);
            var kc = UIKit.Hex(_save.Loc.kiosk);
            for (int i = 0; i < 16; i++)
            {
                var col = Meta.Stripe(_save, _save.Loc, i);
                UIKit.Panel(s, "Stripe", col, i / 16f, 0.93f, (i + 1) / 16f, 1f, false);
                UIKit.Shape(s, UIKit.Circle, col, (i + 0.5f) / 16f, 0.935f, 120f, 34f);
            }
            if (dim) UIKit.Panel(s, "Dim", new Color(0, 0, 0, 0.4f), 0, 0, 1, 0.92f, false);
        }

        // =====================================================================
        //  УВЕДОМЛЕНИЯ (достижения)
        // =====================================================================
        void Notify(string title, string text)
        {
            _notes.Enqueue(new Note { title = title, text = text });
        }

        void CheckAchievements()
        {
            foreach (var a in Meta.CheckAchievements(_save))
                Notify(L.T("Достижение: ", "Achievement: ") + a,
                    L.T("Забери награду в меню «Достижения» — кубок качается!", "Claim the reward in Achievements!"));
            int r = GameData.RankOf(_save.lifetime);
            if (r > _save.rank)
            {
                _save.rank = r;
                Notify(L.T("Новый титул: ", "New title: ") + GameData.RankName(r),
                    L.T("Все цены +3% навсегда. Путь к миллиарду продолжается!", "All prices +3% forever. The road to a billion goes on!"));
            }
        }

        /// <summary>Вызывается из дня при каждом обслуженном клиенте.</summary>
        public void OnStatsChanged()
        {
            CheckAchievements();
            int pl = Meta.PassLevel(_save.passXp);
            if (_passSeen < 0) _passSeen = pl;
            while (_passSeen < pl)
            {
                _passSeen++;
                Notify(L.T("Шаурма-Пасс: уровень ", "Shawarma Pass: level ") + _passSeen + "!",
                    L.T("Награда: ", "Reward: ") + Meta.PassRewardText(_save, _passSeen, false) + L.T(" — забери в меню", " — claim in menu"));
            }
        }

        int _passSeen = -1;

        void UpdateNotes(float dt)
        {
            if (_noteT < 0f)
            {
                if (_notes.Count == 0) return;
                var n = _notes.Dequeue();
                if (_noteRt != null) Destroy(_noteRt.gameObject);
                var p = UIKit.Panel(_notifyLayer, "Note", UIKit.Alpha(UIKit.Dark, 0.95f), 0.3f, 0.86f, 0.7f, 0.98f);
                UIKit.AddShadow(p, 10f, 0.5f);
                UIKit.AddOutline(p, UIKit.Gold, 3f);
                Icons.Trophy(p.transform, 0.08f, 0.5f, 90f, true);
                UIKit.FitLabel(p.transform, n.title, 34, UIKit.Gold, 0.16f, 0.5f, 0.98f, 0.95f);
                UIKit.FitLabel(p.transform, n.text, 26, UIKit.Cream, 0.16f, 0.08f, 0.98f, 0.5f, false);
                _noteRt = p.rectTransform;
                _noteT = 0f;
                Sfx.Play(Sound.Fanfare);
            }
            _noteT += dt;
            float y;
            if (_noteT < 0.3f) y = Mathf.Lerp(200f, 0f, _noteT / 0.3f);
            else if (_noteT < 2.8f) y = 0f;
            else y = Mathf.Lerp(0f, 200f, (_noteT - 2.8f) / 0.3f);
            if (_noteRt != null) _noteRt.anchoredPosition = new Vector2(0f, y);
            if (_noteT > 3.1f)
            {
                if (_noteRt != null) Destroy(_noteRt.gameObject);
                _noteRt = null;
                _noteT = -1f;
            }
        }

        // =====================================================================
        //  ГЛАВНОЕ МЕНЮ
        // =====================================================================
        void ShowMenu()
        {
            if (!_readySent) { _readySent = true; Platform.GameReady(); }
            var s = NewScreen("Menu");
            Backdrop(s, false);

            var sign = UIKit.Panel(s, "Sign", UIKit.Dark, 0.2f, 0.64f, 0.8f, 0.9f);
            UIKit.AddShadow(sign, 12f, 0.4f);
            UIKit.Panel(sign.transform, "Inner", UIKit.Hex("#3A281C"), 0.012f, 0.04f, 0.988f, 0.96f);
            for (int i = 0; i < 18; i++)
                UIKit.Shape(sign.transform, UIKit.Circle, UIKit.Hex("#FFE9A8"), (i + 0.5f) / 18f, 0.045f, 14f, 14f);
            var title = UIKit.Label(sign.transform, GameData.Title, 104, UIKit.Gold, TextAnchor.MiddleCenter, 0f, 0.4f, 1f, 0.97f, true);
            UIKit.AddOutline(title, UIKit.Hex("#8A3B12"), 4f);
            UIKit.AddShadow(title, 8f, 0.5f);
            var sub = UIKit.Label(sign.transform, GameData.Subtitle.ToUpper(), 44, UIKit.Cream, TextAnchor.MiddleCenter, 0f, 0.14f, 1f, 0.42f, true);
            UIKit.AddOutline(sub, UIKit.Hex("#8A3B12"), 2f);
            Juice.Punch(sign.rectTransform, 0.08f, 0.5f);

            // путь к миллиарду
            BillionBar(s, 0.3f, 0.575f, 0.7f, 0.63f);

            var hero = Icons.Shawarma(s, 0.5f, 0.465f, 230f);
            hero.color = Icons.Toast(1f);
            hero.transform.parent.gameObject.AddComponent<Bobber>().amp = 6f;

            // плашка с прогрессом: тёмная, с обводкой — читается на любом фоне
            var info = UIKit.Panel(s, "Info", UIKit.Alpha(UIKit.Dark, 0.92f), 0.31f, 0.315f, 0.69f, 0.385f);
            UIKit.AddShadow(info, 8f, 0.45f);
            UIKit.AddOutline(info, UIKit.Gold, 3f);
            UIKit.Label(info.transform, L.T("День ", "Day ") + _save.day, 32, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 0.28f, 1f, true);
            Icons.Coin(info.transform, 0.36f, 0.5f, 40f);
            UIKit.Label(info.transform, GameData.Money(_save.money), 32, UIKit.Gold, TextAnchor.MiddleLeft, 0.4f, 0f, 0.7f, 1f, true);
            Icons.Star(info.transform, 0.76f, 0.5f, 38f, true);
            UIKit.Label(info.transform, _save.stars.ToString(), 32, UIKit.Gold, TextAnchor.MiddleLeft, 0.8f, 0f, 0.98f, 1f, true);

            bool fresh = _save.day == 1 && _save.money == 0;
            var play = UIKit.Button(s, fresh ? L.T("Играть", "Play") : L.T("Продолжить", "Continue"), UIKit.Green, () =>
            {
                Sfx.Play(Sound.Click);
                ShowMap();
            }, 0.38f, 0.18f, 0.62f, 0.3f, 66);
            UIKit.AddOutline(play.GetComponent<Image>(), UIKit.Alpha(Color.white, 0.6f), 3f);

            // боковые кнопки: достижения, бизнесы, колесо
            SideButton(s, 0.83f, 0.46f, L.T("Достижения", "Achievements"), UIKit.Orange, 0, Meta.ReadyCount(_save), () => ShowAchievements());
            SideButton(s, 0.83f, 0.34f, L.T("Бизнесы", "Businesses"), UIKit.Green, 1, 0, () => ShowBusinesses());
            SideButton(s, 0.83f, 0.22f, L.T("Колесо удачи", "Lucky wheel"), UIKit.Purple, 2, Meta.WheelFree(_save) ? 1 : 0, () => ShowWheel());
            int passReady = Meta.PassReadyCount(_save);
            SideButton(s, 0.01f, 0.46f, L.T("Шаурма-Пасс", "Shawarma Pass"), UIKit.Hex("#6A1B9A"), 3, passReady, () => ShowPass());
            SideButton(s, 0.01f, 0.34f, L.T("Рекорды", "Leaderboard"), UIKit.Hex("#00838F"), 5, 0, () => ShowLeaders());
            AdminButton(s);

            Button soundBtn = null, musicBtn = null;
            soundBtn = UIKit.Button(s, SoundLabel(), UIKit.Blue, () =>
            {
                _save.sound = !_save.sound; ApplyAudio(); SaveSystem.Save(_save);
                UIKit.SetLabel(soundBtn, SoundLabel());
            }, 0.02f, 0.03f, 0.16f, 0.11f, 28);
            musicBtn = UIKit.Button(s, MusicLabel(), UIKit.Blue, () =>
            {
                _save.music = !_save.music; ApplyAudio(); SaveSystem.Save(_save);
                UIKit.SetLabel(musicBtn, MusicLabel());
            }, 0.17f, 0.03f, 0.31f, 0.11f, 28);
            UIKit.Button(s, L.En ? "Language: EN" : "Язык: RU", UIKit.Purple, () =>
            {
                _save.lang = L.En ? "ru" : "en";
                L.Init(_save.lang);
                SaveSystem.Save(_save);
                ShowMenu();
            }, 0.32f, 0.03f, 0.46f, 0.11f, 28);

            bool confirm = false;
            Button resetBtn = null;
            resetBtn = UIKit.Button(s, L.T("Сбросить прогресс", "Reset progress"), UIKit.Gray, () =>
            {
                if (!confirm)
                {
                    confirm = true;
                    UIKit.SetLabel(resetBtn, L.T("Точно? Жми ещё раз", "Sure? Tap again"));
                    return;
                }
                string lang = _save.lang;
                SaveSystem.Reset();
                _save = SaveSystem.Load();
                _save.lang = lang;
                ShowMenu();
            }, 0.8f, 0.03f, 0.98f, 0.11f, 26);

            if (!_dailyChecked)
            {
                _dailyChecked = true;
                int d = Meta.DailyAvailable(_save);
                if (d >= 0) ShowDaily(s, d);
            }
        }

        void SideButton(RectTransform s, float x, float y, string text, Color col, int icon, int badge, UnityEngine.Events.UnityAction act)
        {
            var b = UIKit.Button(s, "     " + text, col, () => { Sfx.Play(Sound.Click); act(); }, x, y, x + 0.16f, y + 0.1f, 30);
            if (icon == 0) Icons.Trophy(b.transform, 0.14f, 0.5f, 60f, true);
            else if (icon == 1) Icons.Coin(b.transform, 0.14f, 0.5f, 54f);
            else if (icon == 3) Icons.Star(b.transform, 0.14f, 0.5f, 54f, true);
            else if (icon == 5)
            {
                // пьедестал 2-1-3
                UIKit.Shape(b.transform, UIKit.RoundedSmall, UIKit.Hex("#B0BEC5"), 0.08f, 0.4f, 20f, 28f);
                UIKit.Shape(b.transform, UIKit.RoundedSmall, UIKit.Gold, 0.14f, 0.45f, 20f, 40f);
                UIKit.Shape(b.transform, UIKit.RoundedSmall, UIKit.Hex("#D7A77A"), 0.2f, 0.37f, 20f, 22f);
                Icons.Star(b.transform, 0.14f, 0.78f, 24f, true);
            }
            else if (icon == 4)
            {
                for (int i = 0; i < 4; i++)
                    UIKit.Shape(b.transform, UIKit.RoundedSmall, Meta.Stripe(_save, _save.Loc, i), 0.07f + i * 0.045f, 0.5f, 16f, 50f);
            }
            else
            {
                var w = UIKit.Rect(b.transform, "Wheel");
                UIKit.PlaceFixed(w, 0.14f, 0.5f, 56f, 56f);
                for (int i = 0; i < 8; i++)
                    UIKit.Shape(w, UIKit.RoundedSmall, UIKit.Hex(Meta.WheelColor[i]), 0.5f, 0.5f, 54f, 14f, i * 22.5f);
                UIKit.Shape(w, UIKit.Circle, Color.white, 0.5f, 0.5f, 14f, 14f);
                w.gameObject.AddComponent<Spinner>().speed = 40f;
            }
            if (badge > 0)
            {
                var bd = UIKit.Shape(b.transform, UIKit.Circle, UIKit.Red, 0.96f, 0.92f, 40f, 40f);
                UIKit.FitLabel(bd.transform, badge.ToString(), 24, Color.white, 0f, 0f, 1f, 1f);
                bd.gameObject.AddComponent<Bobber>().amp = 5f;
            }
        }

        /// <summary>Полоска "Путь к миллиарду": титул и прогресс до следующего.</summary>
        void BillionBar(Transform s, float x0, float y0, float x1, float y1)
        {
            var p = UIKit.Panel(s, "Billion", UIKit.Alpha(UIKit.Dark, 0.85f), x0, y0, x1, y1);
            UIKit.AddShadow(p, 6f, 0.4f);
            int r = GameData.RankOf(_save.lifetime);
            long from = GameData.RankAt[r];
            long to = r + 1 < GameData.RankAt.Length ? GameData.RankAt[r + 1] : GameData.RankAt[r];
            float k = to > from ? (float)((_save.lifetime - from) / (double)(to - from)) : 1f;
            UIKit.Label(p.transform, GameData.RankName(r), 26, UIKit.Gold, TextAnchor.MiddleLeft, 0.02f, 0f, 0.32f, 1f, true);
            var fill = UIKit.Bar(p.transform, UIKit.Alpha(Color.white, 0.15f), UIKit.Gold, 0.33f, 0.2f, 0.98f, 0.8f);
            UIKit.SetFill(fill, k);
            string next = r + 1 < GameData.RankAt.Length ? " → " + GameData.RankName(r + 1) : "";
            UIKit.FitLabel(p.transform, GameData.Money(_save.lifetime) + " / " + GameData.Money(to) + next, 20, Color.white, 0.34f, 0.2f, 0.97f, 0.8f);
        }

        string SoundLabel() { return _save.sound ? L.T("Звук: вкл", "Sound: on") : L.T("Звук: выкл", "Sound: off"); }
        string MusicLabel() { return _save.music ? L.T("Музыка: вкл", "Music: on") : L.T("Музыка: выкл", "Music: off"); }

        // ---------- ежедневный бонус ----------
        void ShowDaily(RectTransform s, int streakDay)
        {
            var c = UIKit.Popup(s, L.T("Ежедневный бонус", "Daily bonus"), UIKit.Orange, 0.15f, 0.2f, 0.85f, 0.8f, null);
            UIKit.Label(c, L.T("Заходи каждый день — завтра будет сюрприз!", "Come back every day — a surprise awaits tomorrow!"), 32, UIKit.Dark,
                TextAnchor.MiddleCenter, 0f, 0.82f, 1f, 1f, true);
            long reward = Meta.DailyReward(_save, streakDay);
            int first = Mathf.Max(1, streakDay - 2);   // показываем пару прошлых дней, сегодня и будущие
            for (int i = 0; i < Meta.DailyCells; i++)
            {
                int day = first + i;
                float x0 = 0.01f + i * 0.1414f;
                bool past = day < streakDay, now = day == streakDay;
                var cell = UIKit.Panel(c, "Cell", now ? UIKit.Gold : (past ? UIKit.Hex("#C8E6C9") : Color.white), x0, 0.34f, x0 + 0.13f, 0.78f);
                UIKit.AddShadow(cell, 5f, 0.3f);
                UIKit.FitLabel(cell.transform, L.T("День ", "Day ") + day, 26, UIKit.Dark, 0.05f, 0.78f, 0.95f, 0.97f);
                if (now)
                {
                    Icons.Coin(cell.transform, 0.5f, 0.48f, 64f);
                    UIKit.FitLabel(cell.transform, "+" + GameData.Money(reward), 30, UIKit.Dark, 0.05f, 0.04f, 0.95f, 0.22f);
                    Juice.Punch(cell.rectTransform, 0.15f, 0.6f);
                }
                else if (past)
                {
                    var ok = UIKit.Shape(cell.transform, UIKit.Circle, UIKit.Green, 0.5f, 0.48f, 60f, 60f);
                    UIKit.FitLabel(ok.transform, "OK", 26, Color.white, 0.1f, 0.2f, 0.9f, 0.8f);
                }
                else
                {
                    var q = UIKit.Shape(cell.transform, UIKit.Circle, UIKit.Purple, 0.5f, 0.48f, 64f, 64f);
                    UIKit.FitLabel(q.transform, "?", 44, Color.white, 0.1f, 0.1f, 0.9f, 0.9f);
                    UIKit.FitLabel(cell.transform, L.T("сюрприз", "surprise"), 22, UIKit.Gray, 0.05f, 0.04f, 0.95f, 0.22f);
                }
            }
            var popup = c.parent.parent.gameObject;
            UIKit.Button(c, L.T("Забрать +", "Claim +") + GameData.Money(reward), UIKit.Green, () =>
            {
                long got = Meta.ClaimDaily(_save);
                SaveSystem.Save(_save);
                Sfx.Play(Sound.Coin);
                Destroy(popup);
                Notify(L.T("Бонус получен", "Bonus claimed"), "+" + GameData.Money(got));
                ShowMenu();
            }, 0.08f, 0.03f, 0.48f, 0.26f, 40);
            UIKit.Button(c, L.T("×2 за рекламу", "×2 for an ad"), UIKit.Orange, () =>
            {
                Platform.ShowRewarded("daily_x2", () =>
                {
                    long got = Meta.ClaimDaily(_save);
                    _save.money += got;
                    _save.lifetime += got;
                    SaveSystem.Save(_save);
                    Sfx.Play(Sound.Coin);
                    Destroy(popup);
                    Notify(L.T("Бонус ×2 получен", "Bonus ×2 claimed"), "+" + GameData.Money(got * 2));
                    ShowMenu();
                }, null);
            }, 0.52f, 0.03f, 0.92f, 0.26f, 40);
        }

        // =====================================================================
        //  ДЕНЬ
        // =====================================================================
        void StartDayCinematic()
        {
            var s = NewScreen("FlyIn");
            FlyIn.Play(s, _save, StartDay);
        }

        void StartDay()
        {
            var active = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                if (_save.boostOn[i] == 1 && _save.boosts[i] > 0)
                {
                    active[i] = true;
                    _save.boosts[i]--;
                }
                _save.boostOn[i] = 0;
            }
            SaveSystem.Save(_save);

            var s = NewScreen("Day");
            _run = s.gameObject.AddComponent<DayRun>();
            _run.Init(this, _save, s, active);
            Platform.GameplayStart();
        }

        public void ExitDayToMap()
        {
            Platform.GameplayStop();
            string lang = _save.lang;
            _save = SaveSystem.Load(); // деньги недоигранного дня не засчитываются
            _save.lang = lang;
            ShowMap();
        }

        public void OnDayFinished(DayRun run)
        {
            Platform.GameplayStop();
            int total = run.Earned + run.Tips;
            int stars = 0;
            if (run.GoalReached)
            {
                stars = 1;
                if (total >= run.Goal * 3 / 2) stars = 2;
                if (total >= run.Goal * 2 && run.Lost == 0) stars = 3;
                _save.day++;
            }
            _save.stars += stars;
            _save.bestStarsDay = Mathf.Max(_save.bestStarsDay, stars);
            _lastBizIncome = run.CourierIncome;
            _lastKiosk2 = Meta.Kiosk2Day(_save);
            _lastStaff = Meta.StaffDay(_save);
            _lastPiggy = 0;
            if (Meta.Has(_save, Meta.CharmPiggy) && total > 0)
            {
                _lastPiggy = (long)(total * 0.05f);
                _save.money += _lastPiggy;
                _save.lifetime += _lastPiggy;
            }
            if (total > _save.bestDay)
            {
                _save.bestDay = total;
                Platform.SubmitScore(total);
            }
            SaveSystem.Save(_save);
            Platform.SubmitLeader(Platform.LbMoney, _save.lifetime);
            Platform.SubmitLeader(Platform.LbServed, _save.totalServed);
            ShowResults(run, stars);
            CheckAchievements();
        }

        // =====================================================================
        //  ИТОГИ ДНЯ
        // =====================================================================
        void ShowResults(DayRun run, int stars)
        {
            var s = NewScreen("Results");
            Backdrop(s, true);
            var box = UIKit.Panel(s, "Box", UIKit.Cream, 0.2f, 0.04f, 0.8f, 0.9f);
            UIKit.AddShadow(box, 14f, 0.45f);
            bool ok = run.GoalReached;
            UIKit.Label(box.transform, ok ? L.T("День ", "Day ") + run.Day + L.T(" закрыт!", " complete!") : L.T("Цель не выполнена", "Goal not reached"),
                58, ok ? UIKit.Dark : UIKit.Red, TextAnchor.MiddleCenter, 0, 0.89f, 1, 0.98f, true);

            for (int i = 0; i < 3; i++)
            {
                var st = Icons.Star(box.transform, 0.38f + i * 0.12f, 0.82f, 80f, i < stars);
                if (i < stars) Juice.Punch(st, 0.3f, 0.4f + i * 0.15f);
            }
            Sfx.Play(ok ? Sound.Fanfare : Sound.Error);

            int total = run.Earned + run.Tips;
            string stats =
                L.T("Цель дня", "Daily goal") + ":  " + total + " / " + run.Goal + "\n" +
                L.T("Выручка", "Revenue") + ":  " + run.Earned + "   " + L.T("Чаевые", "Tips") + ":  " + run.Tips + "\n" +
                L.T("Обслужено", "Served") + ":  " + run.Served + "   " + L.T("Ушли", "Left") + ":  " + run.Lost + "   " +
                L.T("Сгорело", "Burnt") + ":  " + run.BurntCount + "   " + L.T("Комбо", "Combo") + ":  " + run.BestCombo;
            UIKit.Label(box.transform, stats, 30, UIKit.Dark, TextAnchor.MiddleCenter, 0.03f, 0.6f, 0.97f, 0.76f);

            // задания
            var qp = UIKit.Panel(box.transform, "Quests", UIKit.Alpha(UIKit.Dark, 0.08f), 0.05f, 0.36f, 0.95f, 0.59f);
            UIKit.Label(qp.transform, L.T("Задания дня", "Daily quests"), 26, UIKit.Purple, TextAnchor.MiddleLeft, 0.03f, 0.76f, 0.97f, 0.98f, true);
            for (int i = 0; i < run.Quests.Count; i++)
            {
                var q = run.Quests[i];
                float y1 = 0.74f - i * 0.245f;
                UIKit.Shape(qp.transform, UIKit.Circle, q.done ? UIKit.Green : UIKit.Alpha(UIKit.Dark, 0.25f), 0.04f, y1 - 0.11f, 26f, 26f);
                UIKit.Label(qp.transform, q.Text + (q.done ? "" : "  (" + q.Progress + ")"), 24, q.done ? UIKit.Dark : UIKit.Gray,
                    TextAnchor.MiddleLeft, 0.08f, y1 - 0.22f, 0.8f, y1, q.done);
                UIKit.Label(qp.transform, "+" + q.reward, 24, q.done ? UIKit.Hex("#C98A00") : UIKit.Gray, TextAnchor.MiddleRight, 0.8f, y1 - 0.22f, 0.97f, y1, true);
            }

            string sub = ok
                ? L.T("★ 2 — цель ×1.5, ★ 3 — цель ×2 без потерь", "★2 — goal ×1.5, ★3 — goal ×2 with no losses")
                : L.T("День придётся повторить. Деньги остаются — прокачайся!", "You'll replay this day. Money is kept — upgrade!");
            UIKit.Label(box.transform, sub, 22, ok ? UIKit.Gray : UIKit.Red, TextAnchor.MiddleCenter, 0.04f, 0.3f, 0.96f, 0.36f, true);

            string tot = "+" + total + "   <size=28><color=#6A1B9A>" + L.T("Пасс +", "Pass +") + run.DayXp + " XP</color></size>";
            if (_lastKiosk2 > 0) tot += "\n<size=28><color=#2E7D32>" + L.T("Второй ларёк продал порций: ", "Second kiosk sold: ") + _lastKiosk2 + L.T(" — выручка ждёт в «Бизнесах»", " — cash waits in Businesses") + "</color></size>";
            else if (_save.biz[Meta.BizKiosk2] > 0) tot += "\n<size=28><color=#C62828>" + L.T("Второй ларёк простаивает — закупи продукты!", "Second kiosk is idle — buy food!") + "</color></size>";
            if (_lastStaff > 0) tot += "\n<size=26><color=#1565C0>" + L.T("Сотрудники продали порций: ", "Staff sold portions: ") + _lastStaff + "</color></size>";
            if (_lastPiggy > 0) tot += "\n<size=26><color=#AD1457>" + L.T("Копилка: +", "Piggy bank: +") + GameData.Money(_lastPiggy) + "</color></size>";
            if (_lastBizIncome > 0) tot += "   <size=30><color=#6A1B9A>" + L.T("(из них курьеры ", "(couriers ") + GameData.Money(_lastBizIncome) + ")</color></size>";
            var tl = UIKit.Label(box.transform, tot, 54, UIKit.Green, TextAnchor.MiddleCenter, 0, 0.21f, 1, 0.3f, true);
            Juice.Punch(tl.transform, 0.2f, 0.5f);

            Button doubleBtn = null;
            doubleBtn = UIKit.Button(box.transform, L.T("×2 монеты за рекламу", "×2 coins for an ad"), UIKit.Orange, () =>
            {
                doubleBtn.interactable = false;
                Platform.ShowRewarded("double_day", () =>
                {
                    _save.money += total;
                    SaveSystem.Save(_save);
                    UIKit.SetLabel(doubleBtn, L.T("Получено +", "Got +") + total);
                    Sfx.Play(Sound.Coin);
                }, null);
            }, 0.1f, 0.12f, 0.9f, 0.2f, 38);
            doubleBtn.interactable = total > 0;

            // не дотянул до цели, но заработал хотя бы половину — можно засчитать день за рекламу
            if (!ok && run.Goal > 0 && total * 2 >= run.Goal)
            {
                var drt = doubleBtn.transform as RectTransform;
                UIKit.Place(drt, 0.1f, 0.12f, 0.49f, 0.2f);
                Button passBtn = null;
                passBtn = UIKit.Button(box.transform, L.T("Засчитать день за рекламу", "Count the day for an ad"), UIKit.Purple, () =>
                {
                    passBtn.interactable = false;
                    Platform.ShowRewarded("pass_day", () =>
                    {
                        _save.day++;
                        SaveSystem.Save(_save);
                        UIKit.SetLabel(passBtn, L.T("День засчитан!", "Day counted!"));
                        Sfx.Play(Sound.Fanfare);
                    }, null);
                }, 0.51f, 0.12f, 0.9f, 0.2f, 30);
                passBtn.gameObject.AddComponent<Bobber>().amp = 2f;
            }

            UIKit.Button(box.transform, L.T("На карту", "To the map"), UIKit.Green, () =>
            {
                Sfx.Play(Sound.Click);
                Platform.ShowFullscreen(ShowMap);
            }, 0.1f, 0.02f, 0.9f, 0.1f, 44);
        }
    }
}
