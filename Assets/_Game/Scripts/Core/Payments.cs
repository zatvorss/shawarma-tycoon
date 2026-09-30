using System;
using UnityEngine;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Покупки за яны (внутриигровая валюта Яндекс Игр).
    /// ВРЕМЕННО всё выдаётся бесплатно: ID товаров ещё не заведены в консоли Яндекса.
    /// Когда ID появятся — поставь Free = false и вызови здесь ysdk.getPayments().purchase({ id })
    /// (через jslib или плагин YG2), а onOk — в колбэке успешной покупки.
    ///
    /// ID товаров:  pass_plus (99 ян),  staff_0 … staff_11 (цена — Meta.StaffYan).
    /// </summary>
    public static class Payments
    {
        public static readonly bool Free = true;

        public static void Buy(string productId, int yan, Action onOk)
        {
            Debug.Log("[Payments] purchase " + productId + " for " + yan + " yan" + (Free ? " (free for now)" : ""));
            // TODO: реальная покупка через SDK Яндекса, пока — сразу успех
            if (onOk != null) onOk();
        }

        /// <summary>Текст цены на кнопке: «99 ян».</summary>
        public static string Price(int yan) { return yan + L.T(" ян", " yan"); }

        public static string FreeNote { get { return Free ? L.T("временно бесплатно", "free for now") : ""; } }
    }
}
