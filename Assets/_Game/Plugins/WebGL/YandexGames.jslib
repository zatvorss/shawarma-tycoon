// Мост к SDK Яндекс Игр. SDK подключается и инициализируется в index.html шаблона YandexGames
// (YaGames.init()), здесь — только вызовы. Ответы уходят в Unity через window.ygSend(метод, строка)
// на объект "YandexBridge". Код в стиле ES5 — для старых iOS/Android.
mergeInto(LibraryManager.library, {

  YG_IsReady: function () { return (window.ysdk ? 1 : 0); },

  YG_SdkFailed: function () { return (window.ygFailed ? 1 : 0); },

  YG_Lang: function () {
    var l = "";
    try { if (window.ysdk && ysdk.environment && ysdk.environment.i18n) l = ysdk.environment.i18n.lang || ""; } catch (e) {}
    var n = lengthBytesUTF8(l) + 1; var b = _malloc(n); stringToUTF8(l, b, n); return b;
  },

  YG_GameReady: function () {
    try { if (window.ysdk && ysdk.features && ysdk.features.LoadingAPI) ysdk.features.LoadingAPI.ready(); } catch (e) {}
  },

  YG_GameplayStart: function () {
    try { if (window.ysdk && ysdk.features && ysdk.features.GameplayAPI) ysdk.features.GameplayAPI.start(); } catch (e) {}
  },

  YG_GameplayStop: function () {
    try { if (window.ysdk && ysdk.features && ysdk.features.GameplayAPI) ysdk.features.GameplayAPI.stop(); } catch (e) {}
  },

  YG_ShowFullscreen: function () {
    if (!window.ysdk) { window.ygSend("OnFullscreenClose", ""); return; }
    ysdk.adv.showFullscreenAdv({ callbacks: {
      onOpen: function () { window.ygSend("OnAdOpen", ""); },
      onClose: function () { window.ygSend("OnFullscreenClose", ""); },
      onError: function () { window.ygSend("OnFullscreenClose", ""); },
      onOffline: function () { window.ygSend("OnFullscreenClose", ""); }
    }});
  },

  YG_ShowRewarded: function (idPtr) {
    var id = UTF8ToString(idPtr);
    if (!window.ysdk) { window.ygSend("OnRewardedClose", id); return; }
    ysdk.adv.showRewardedVideo({ callbacks: {
      onOpen: function () { window.ygSend("OnAdOpen", ""); },
      onRewarded: function () { window.ygSend("OnRewarded", id); },
      onClose: function () { window.ygSend("OnRewardedClose", id); },
      onError: function () { window.ygSend("OnRewardedClose", id); }
    }});
  },

  // ---------- игрок и облачные сохранения ----------
  YG_InitPlayer: function () {
    if (!window.ysdk) { window.ygSend("OnCloudData", ""); return; }
    ysdk.getPlayer({ scopes: false }).then(function (p) {
      window.ygPlayer = p;
      window.ygSend("OnAuth", p.getMode() === "lite" ? "0" : "1");
      return p.getData(["save"]);
    }).then(function (d) {
      window.ygSend("OnCloudData", (d && d.save) ? d.save : "");
    }).catch(function () { window.ygSend("OnCloudData", ""); });
  },

  YG_SaveCloud: function (jsonPtr, flush) {
    var json = UTF8ToString(jsonPtr);
    try { if (window.ygPlayer) ygPlayer.setData({ save: json }, flush ? true : false); } catch (e) {}
  },

  YG_OpenAuth: function () {
    if (!window.ysdk) return;
    ysdk.auth.openAuthDialog().then(function () {
      return ysdk.getPlayer({ scopes: false });
    }).then(function (p) {
      window.ygPlayer = p;
      window.ygSend("OnAuth", p.getMode() === "lite" ? "0" : "1");
    }).catch(function () { window.ygSend("OnAuth", "0"); });
  },

  // ---------- лидерборды ----------
  YG_SetLeader: function (namePtr, score) {
    var name = UTF8ToString(namePtr);
    var v = Math.floor(score);
    if (!window.ysdk) return;
    try {
      // новый API (ysdk.leaderboards) или прежний (getLeaderboards)
      if (ysdk.leaderboards && ysdk.leaderboards.setScore) {
        ysdk.isAvailableMethod("leaderboards.setScore").then(function (ok) {
          if (ok) return ysdk.leaderboards.setScore(name, v);
        }).catch(function () {});
      } else {
        ysdk.isAvailableMethod("leaderboards.setLeaderboardScore").then(function (ok) {
          if (!ok) return;
          return ysdk.getLeaderboards().then(function (lb) { return lb.setLeaderboardScore(name, v); });
        }).catch(function () {});
      }
    } catch (e) {}
  },

  YG_GetLeader: function (namePtr) {
    var name = UTF8ToString(namePtr);
    function fail() { window.ygSend("OnLeaderboard", JSON.stringify({ board: name, ok: false, entries: [] })); }
    function done(res) {
      var me = res.userRank || 0;
      var list = [];
      for (var i = 0; i < res.entries.length; i++) {
        var e = res.entries[i];
        list.push({ rank: e.rank, name: (e.player && e.player.publicName) ? e.player.publicName : "", score: e.score, me: e.rank === me });
      }
      window.ygSend("OnLeaderboard", JSON.stringify({ board: name, ok: true, entries: list }));
    }
    if (!window.ysdk) { fail(); return; }
    var opts = { quantityTop: 10, includeUser: true, quantityAround: 1 };
    try {
      if (ysdk.leaderboards && ysdk.leaderboards.getEntries) {
        ysdk.leaderboards.getEntries(name, opts).then(done).catch(fail);
      } else {
        ysdk.getLeaderboards().then(function (lb) { return lb.getLeaderboardEntries(name, opts); }).then(done).catch(fail);
      }
    } catch (e) { fail(); }
  },

  // ---------- покупки ----------
  YG_InitPayments: function () {
    function empty() { window.ygSend("OnPaymentsReady", JSON.stringify({ ok: false, catalog: [], purchases: [] })); }
    if (!window.ysdk) { empty(); return; }
    ysdk.getPayments({ signed: false }).then(function (pay) {
      window.ygPay = pay;
      return pay.getCatalog().then(function (cat) {
        return pay.getPurchases().then(function (pur) { return { cat: cat, pur: pur }; });
      });
    }).then(function (r) {
      var catalog = [], purchases = [];
      for (var i = 0; i < r.cat.length; i++) {
        var c = r.cat[i];
        var img = "";
        try { img = c.getPriceCurrencyImage("medium"); } catch (e) {}
        catalog.push({ id: c.id, title: c.title, price: c.price, priceValue: c.priceValue, currency: c.priceCurrencyCode, image: img });
      }
      for (var j = 0; j < r.pur.length; j++) purchases.push({ id: r.pur[j].productID, token: r.pur[j].purchaseToken });
      window.ygSend("OnPaymentsReady", JSON.stringify({ ok: true, catalog: catalog, purchases: purchases }));
    }).catch(empty);
  },

  YG_Purchase: function (idPtr) {
    var id = UTF8ToString(idPtr);
    if (!window.ygPay) { window.ygSend("OnPurchaseFail", id); return; }
    ygPay.purchase({ id: id }).then(function (p) {
      window.ygSend("OnPurchase", JSON.stringify({ id: p.productID, token: p.purchaseToken }));
    }).catch(function () { window.ygSend("OnPurchaseFail", id); });
  },

  YG_Consume: function (tokenPtr) {
    var token = UTF8ToString(tokenPtr);
    try { if (window.ygPay) ygPay.consumePurchase(token); } catch (e) {}
  }
});
