using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Прослойка между игрой и SDK Яндекс Игр.
    /// В WebGL-сборке всё идёт через Plugins/WebGL/YandexGames.jslib (SDK инициализируется в index.html
    /// шаблона WebGLTemplates/YandexGames). В редакторе — заглушки: реклама «досматривается» сразу,
    /// сохранения только локальные.
    /// </summary>
    public static class Platform
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int YG_IsReady();
        [DllImport("__Internal")] static extern int YG_SdkFailed();
        [DllImport("__Internal")] static extern string YG_Lang();
        [DllImport("__Internal")] static extern void YG_GameReady();
        [DllImport("__Internal")] static extern void YG_GameplayStart();
        [DllImport("__Internal")] static extern void YG_GameplayStop();
        [DllImport("__Internal")] static extern void YG_ShowFullscreen();
        [DllImport("__Internal")] static extern void YG_ShowRewarded(string id);
        [DllImport("__Internal")] static extern void YG_InitPlayer();
        [DllImport("__Internal")] static extern void YG_SaveCloud(string json, bool flush);
        [DllImport("__Internal")] static extern void YG_OpenAuth();
        [DllImport("__Internal")] static extern void YG_SetLeader(string name, double score);
        [DllImport("__Internal")] static extern void YG_GetLeader(string name);
        const bool Web = true;
#else
        const bool Web = false;
#endif

        /// <summary>SDK готов (или точно недоступен) и облачное сохранение получено — можно строить игру.</summary>
        public static bool Initialized { get; private set; }
        /// <summary>Работаем внутри Яндекс Игр с рабочим SDK.</summary>
        public static bool HasSdk { get; private set; }
        /// <summary>Игрок вошёл с Яндекс ID (для лидербордов и облака на всех устройствах).</summary>
        public static bool Authorized { get; internal set; }
        /// <summary>Сохранение из облака (JSON) — SaveSystem выбирает более свежее из облака и локального.</summary>
        public static string CloudJson { get; internal set; }

        static YandexBridge _bridge;
        static bool _gameplay;

        /// <summary>Запуск: мост, ожидание SDK и облачных данных. Вызывается первым делом из Game.Awake.</summary>
        public static void Boot()
        {
            if (_bridge != null) return;
            var go = new GameObject("YandexBridge");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _bridge = go.AddComponent<YandexBridge>();
            CloudJson = "";
            if (!Web) { Initialized = true; return; }
            _bridge.StartCoroutine(BootRoutine());
        }

        static IEnumerator BootRoutine()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float t = 0f;
            while (YG_IsReady() == 0 && YG_SdkFailed() == 0 && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            HasSdk = YG_IsReady() == 1;
            if (HasSdk)
            {
                _cloudDone = false;
                YG_InitPlayer();
                t = 0f;
                while (!_cloudDone && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            }
#endif
            Initialized = true;
            yield break;
        }

        internal static bool _cloudDone;

        /// <summary>Язык площадки ("ru", "en", "tr"...). Пусто = не знаем.</summary>
        public static string Language()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) { try { return YG_Lang() ?? ""; } catch { return ""; } }
#endif
            return "";
        }

        /// <summary>Игра загрузилась и готова к игре (Яндекс: LoadingAPI.ready()).</summary>
        public static void GameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) YG_GameReady();
#endif
            Debug.Log("[Platform] GameReady");
        }

        /// <summary>Начался активный геймплей (Яндекс: GameplayAPI.start()).</summary>
        public static void GameplayStart()
        {
            if (_gameplay) return;
            _gameplay = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) YG_GameplayStart();
#endif
        }

        /// <summary>Пауза / меню / итоги (Яндекс: GameplayAPI.stop()).</summary>
        public static void GameplayStop()
        {
            if (!_gameplay) return;
            _gameplay = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) YG_GameplayStop();
#endif
        }

        // ---------------- реклама ----------------
        static Action _fullscreenClose;
        static readonly Dictionary<string, Action> _rewardOk = new Dictionary<string, Action>();
        static readonly Dictionary<string, Action> _rewardClose = new Dictionary<string, Action>();
        static readonly HashSet<string> _rewarded = new HashSet<string>();

        /// <summary>Полноэкранная реклама в логической паузе (частоту ограничивает сам Яндекс).</summary>
        public static void ShowFullscreen(Action onClose)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) { _fullscreenClose = onClose; YG_ShowFullscreen(); return; }
#endif
            if (onClose != null) onClose();
        }

        /// <summary>Реклама за награду. onReward — только если досмотрели (выдаётся после закрытия ролика).</summary>
        public static void ShowRewarded(string id, Action onReward, Action onClose)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk)
            {
                _rewardOk[id] = onReward;
                _rewardClose[id] = onClose;
                _rewarded.Remove(id);
                YG_ShowRewarded(id);
                return;
            }
#endif
            Debug.Log("[Platform] Rewarded ad (editor): " + id);
            if (onReward != null) onReward();
            if (onClose != null) onClose();
        }

        internal static void OnAdOpen() { SetAdPause(true); }

        internal static void OnFullscreenClose()
        {
            SetAdPause(false);
            var a = _fullscreenClose;
            _fullscreenClose = null;
            if (a != null) a();
        }

        internal static void OnRewarded(string id) { _rewarded.Add(id); }

        internal static void OnRewardedClose(string id)
        {
            SetAdPause(false);
            Action ok, close;
            _rewardOk.TryGetValue(id, out ok);
            _rewardClose.TryGetValue(id, out close);
            _rewardOk.Remove(id);
            _rewardClose.Remove(id);
            bool got = _rewarded.Remove(id);
            if (got && ok != null) ok();
            if (close != null) close();
        }

        // ---------------- пауза: реклама, потеря фокуса, game_api_pause ----------------
        static bool _adPause, _apiPause, _focusLost;

        internal static void SetAdPause(bool p) { _adPause = p; ApplyPause(); }
        internal static void SetApiPause(bool p) { _apiPause = p; ApplyPause(); }
        internal static void SetFocusLost(bool lost) { _focusLost = lost; ApplyPause(); }

        /// <summary>Звук и игровой процесс стоят, пока идёт реклама / вкладка не в фокусе / площадка просит паузу.</summary>
        static void ApplyPause()
        {
            bool p = _adPause || _apiPause || _focusLost;
            AudioListener.pause = p;
            Time.timeScale = p ? 0f : 1f;
        }

        // ---------------- лидерборды (в консоли создать таблицы с такими техническими названиями) ----------------
        public const string LbMoney = "money";     // заработано за всё время
        public const string LbServed = "served";   // обслужено клиентов

        public class LeaderEntry { public int rank; public string name; public long score; public bool me; }

        static readonly Dictionary<string, Action<List<LeaderEntry>>> _lbWait = new Dictionary<string, Action<List<LeaderEntry>>>();

        /// <summary>Отправить результат (только для вошедших игроков — проверяет сам SDK).</summary>
        public static void SubmitLeader(string board, long score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) YG_SetLeader(board, score);
#endif
        }

        /// <summary>Загрузить топ-10. null — таблица недоступна (редактор, нет сети).</summary>
        public static void GetLeaderboard(string board, int top, Action<List<LeaderEntry>> onDone)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) { _lbWait[board] = onDone; YG_GetLeader(board); return; }
#endif
            if (onDone != null) onDone(null);
        }

        [Serializable] class LbEntryJ { public int rank; public string name; public double score; public bool me; }
        [Serializable] class LbJ { public string board; public bool ok; public LbEntryJ[] entries; }

        internal static void OnLeaderboard(string json)
        {
            LbJ d = null;
            try { d = JsonUtility.FromJson<LbJ>(json); } catch { }
            if (d == null || d.board == null) return;
            Action<List<LeaderEntry>> cb;
            if (!_lbWait.TryGetValue(d.board, out cb)) return;
            _lbWait.Remove(d.board);
            if (cb == null) return;
            if (!d.ok || d.entries == null) { cb(null); return; }
            var list = new List<LeaderEntry>();
            foreach (var e in d.entries)
                list.Add(new LeaderEntry { rank = e.rank, name = string.IsNullOrEmpty(e.name) ? L.T("Игрок", "Player") : e.name, score = (long)e.score, me = e.me });
            cb(list);
        }

        /// <summary>Старый рекорд «лучший день» — больше не используется.</summary>
        public static void SubmitScore(int score) { }

        // ---------------- авторизация (только по нажатию кнопки игроком) ----------------
        static Action _authDone;

        public static void OpenAuth(Action onDone)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) { _authDone = onDone; YG_OpenAuth(); return; }
#endif
            if (onDone != null) onDone();
        }

        internal static void OnAuth(string v)
        {
            Authorized = v == "1";
            var a = _authDone;
            _authDone = null;
            if (a != null) a();
        }

        // ---------------- сохранения: локально сразу, в облако — не чаще раза в 5 сек ----------------
        static string _cloudPending;
        static float _cloudT;

        public static void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            if (HasSdk) _cloudPending = value;
        }

        public static string LoadString(string key)
        {
            return PlayerPrefs.GetString(key, "");
        }

        internal static void TickCloud(float dt)
        {
            if (_cloudPending == null) return;
            _cloudT += dt;
            if (_cloudT >= 5f) FlushCloud(false);
        }

        /// <summary>Отправить сохранение в облако сейчас (при сворачивании/уходе со страницы — с flush).</summary>
        public static void FlushCloud(bool force)
        {
            _cloudT = 0f;
            if (_cloudPending == null) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasSdk) YG_SaveCloud(_cloudPending, force);
#endif
            _cloudPending = null;
        }
    }

    /// <summary>Объект, который получает ответы от SDK (window.ygSend в index.html → SendMessage).</summary>
    public class YandexBridge : MonoBehaviour
    {
        public void OnAdOpen(string _) { Platform.OnAdOpen(); }
        public void OnFullscreenClose(string _) { Platform.OnFullscreenClose(); }
        public void OnRewarded(string id) { Platform.OnRewarded(id); }
        public void OnRewardedClose(string id) { Platform.OnRewardedClose(id); }
        public void OnApiPause(string _) { Platform.SetApiPause(true); }
        public void OnApiResume(string _) { Platform.SetApiPause(false); }
        public void OnAuth(string v) { Platform.OnAuth(v); }
        public void OnLeaderboard(string json) { Platform.OnLeaderboard(json); }
        public void OnPaymentsReady(string json) { Payments.OnReady(json); }
        public void OnPurchase(string json) { Payments.OnPurchase(json); }
        public void OnPurchaseFail(string id) { Payments.OnPurchaseFail(id); }

        public void OnCloudData(string json)
        {
            Platform.CloudJson = json ?? "";
            Platform._cloudDone = true;
        }

        void Update() { Platform.TickCloud(Time.unscaledDeltaTime); }

        void OnApplicationFocus(bool focus)
        {
            Platform.SetFocusLost(!focus);
            if (!focus) Platform.FlushCloud(true);
        }

        void OnApplicationPause(bool paused) { OnApplicationFocus(!paused); }
    }
}
