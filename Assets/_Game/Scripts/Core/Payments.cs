using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Покупки за яны — только через SDK Яндекс Игр.
    /// Товары в консоли разработчика (вкладка «Инап-покупки»):  pass_plus,  staff_0 … staff_10.
    /// Цена и значок валюты берутся из каталога SDK (getCatalog). Товара нет в каталоге — кнопки покупки нет.
    /// Каждая покупка сразу выдаётся, сохраняется (в том числе в облако) и консумируется;
    /// при запуске неконсумированные покупки (оплатил, но вкладка закрылась) выдаются и консумируются.
    /// В редакторе покупки выдаются бесплатно — для проверки.
    /// </summary>
    public static class Payments
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void YG_InitPayments();
        [DllImport("__Internal")] static extern void YG_Purchase(string id);
        [DllImport("__Internal")] static extern void YG_Consume(string token);
        public static readonly bool Free = false;
#else
        /// <summary>В редакторе покупки бесплатны (для проверки). В сборке — всегда через Яндекс.</summary>
        public static readonly bool Free = true;
#endif

        [Serializable] class ProductJ { public string id, title, price, priceValue, currency, image; }
        [Serializable] class PurchaseJ { public string id, token; }
        [Serializable] class PayJ { public bool ok; public ProductJ[] catalog; public PurchaseJ[] purchases; }

        static readonly Dictionary<string, ProductJ> _catalog = new Dictionary<string, ProductJ>();
        static readonly Dictionary<string, Action> _pending = new Dictionary<string, Action>();
        static bool _inited;

        /// <summary>Каталог получен (или покупки недоступны).</summary>
        public static bool Ready { get; private set; }
        /// <summary>Значок портальной валюты из SDK (может быть null, пока грузится).</summary>
        public static Texture2D CurrencyIcon { get; private set; }
        /// <summary>Вызывается, когда что-то выдано при запуске (восстановление покупок) — обновить экран.</summary>
        public static Action OnRestored;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Platform.HasSdk) { YG_InitPayments(); return; }
#endif
            Ready = true;
        }

        /// <summary>Есть ли товар в каталоге (только такие покупки показываются в игре).</summary>
        public static bool Available(string id)
        {
            if (Free) return true;
            return _catalog.ContainsKey(id);
        }

        public static void Buy(string productId, int yanFallback, Action onOk)
        {
            if (Free)
            {
                Grant(SaveSystem.Current, productId);
                SaveSystem.Save(SaveSystem.Current);
                if (onOk != null) onOk();
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            _pending[productId] = onOk;
            YG_Purchase(productId);
#endif
        }

        /// <summary>Выдать купленное (одинаково для новой покупки и восстановления).</summary>
        public static void Grant(SaveData s, string id)
        {
            if (s == null || string.IsNullOrEmpty(id)) return;
            if (id == "pass_plus") { s.passPlus = true; return; }
            if (id.StartsWith("staff_"))
            {
                int loc;
                if (int.TryParse(id.Substring(6), out loc) && loc >= 0 && loc < s.staffHired.Length && s.staffHired[loc] != 1)
                {
                    s.staffHired[loc] = 1;
                    s.staffStock[loc] = Mathf.Max(s.staffStock[loc], Meta.StaffPortions);   // первый день продуктов — в подарок
                }
            }
        }

        internal static void OnReady(string json)
        {
            Ready = true;
            PayJ d = null;
            try { d = JsonUtility.FromJson<PayJ>(json); } catch { }
            if (d == null || !d.ok) return;
            _catalog.Clear();
            string icon = null;
            if (d.catalog != null)
                foreach (var p in d.catalog)
                {
                    if (p == null || string.IsNullOrEmpty(p.id)) continue;
                    _catalog[p.id] = p;
                    if (icon == null && !string.IsNullOrEmpty(p.image)) icon = p.image;
                }
            // неконсумированные покупки: выдать и консумировать
            bool any = false;
            if (d.purchases != null)
                foreach (var pu in d.purchases)
                {
                    if (pu == null) continue;
                    Grant(SaveSystem.Current, pu.id);
                    any = true;
#if UNITY_WEBGL && !UNITY_EDITOR
                    if (!string.IsNullOrEmpty(pu.token)) YG_Consume(pu.token);
#endif
                }
            if (any)
            {
                SaveSystem.Save(SaveSystem.Current);
                Platform.FlushCloud(true);
                if (OnRestored != null) OnRestored();
            }
            if (icon != null && CurrencyIcon == null) LoadIcon(icon);
        }

        internal static void OnPurchase(string json)
        {
            PurchaseJ p = null;
            try { p = JsonUtility.FromJson<PurchaseJ>(json); } catch { }
            if (p == null || string.IsNullOrEmpty(p.id)) return;
            Grant(SaveSystem.Current, p.id);
            SaveSystem.Save(SaveSystem.Current);
            Platform.FlushCloud(true);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(p.token)) YG_Consume(p.token);
#endif
            Action ok;
            if (_pending.TryGetValue(p.id, out ok)) { _pending.Remove(p.id); if (ok != null) ok(); }
        }

        internal static void OnPurchaseFail(string id) { _pending.Remove(id); }

        // ---------------- цена: число + название и значок валюты из SDK ----------------

        /// <summary>Цена числом из каталога (например «99»).</summary>
        public static string PriceValue(string id, int fallback)
        {
            ProductJ p;
            if (_catalog.TryGetValue(id, out p) && !string.IsNullOrEmpty(p.priceValue)) return p.priceValue;
            return fallback.ToString();
        }

        /// <summary>Цена строкой из SDK (например «99 ян»), в редакторе — «99 ян».</summary>
        public static string Price(string id, int fallback)
        {
            ProductJ p;
            if (_catalog.TryGetValue(id, out p) && !string.IsNullOrEmpty(p.price)) return p.price;
            return fallback + L.T(" ян", " yan");
        }

        /// <summary>
        /// Ценник: значок валюты из SDK и число. Пока значок не загрузился (или его нет) —
        /// цена целиком текстом из каталога («99 ян»).
        /// </summary>
        public static void PriceTag(Transform parent, string id, int fallback, float x0, float y0, float x1, float y1, int size, Color color)
        {
            var box = UIKit.Rect(parent, "Price");
            UIKit.Place(box, x0, y0, x1, y1);
            if (CurrencyIcon != null)
            {
                var ic = UIKit.Rect(box, "Currency");
                UIKit.PlaceFixed(ic, 0.2f, 0.5f, size * 1.1f, size * 1.1f);
                var ri = ic.gameObject.AddComponent<RawImage>();
                ri.texture = CurrencyIcon;
                ri.raycastTarget = false;
                UIKit.FitLabel(box, PriceValue(id, fallback), size, color, 0.32f, 0.05f, 0.98f, 0.95f);
            }
            else UIKit.FitLabel(box, Price(id, fallback), size, color, 0.02f, 0.05f, 0.98f, 0.95f);
        }

        static void LoadIcon(string url)
        {
            var bridge = GameObject.Find("YandexBridge");
            if (bridge == null) return;
            var mb = bridge.GetComponent<YandexBridge>();
            if (mb != null) mb.StartCoroutine(LoadIconRoutine(url));
        }

        static IEnumerator LoadIconRoutine(string url)
        {
            if (url.StartsWith("//")) url = "https:" + url;
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    CurrencyIcon = DownloadHandlerTexture.GetContent(req);
                    if (OnRestored != null) OnRestored();
                }
            }
        }
    }
}
