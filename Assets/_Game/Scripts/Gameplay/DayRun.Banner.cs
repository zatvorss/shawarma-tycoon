using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Тип объявления: от него зависят цвет ленты и значок.</summary>
    public enum BannerKind { Info, Courier, Bulk, Guest, Boss, Fever, Good, Bad, Fire, Rush, Time }

    /// <summary>
    /// Красивое объявление посреди дня: карточка съезжает сверху, слева — круглый значок
    /// в цвете события, справа — заголовок и пояснение. Плавно исчезает.
    /// </summary>
    public partial class DayRun
    {
        CanvasGroup _bannerGroup;
        RectTransform _bannerCard, _bannerIcon;
        Image _bannerRibbon, _bannerDisc, _bannerGlow;
        Text _bannerTitle, _bannerBody;
        float _bannerDur;

        void BuildBanner()
        {
            _banner = UIKit.Rect(_root, "Banner");
            UIKit.Place(_banner, 0.22f, 0.6f, 0.78f, 0.86f);
            _bannerGroup = _banner.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;

            _bannerCard = UIKit.Rect(_banner, "Card");
            UIKit.Place(_bannerCard, 0f, 0f, 1f, 1f);
            var shadow = UIKit.Panel(_bannerCard, "Shadow", new Color(0, 0, 0, 0.35f), 0.008f, -0.05f, 1.008f, 0.95f);
            shadow.raycastTarget = false;
            var body = UIKit.Panel(_bannerCard, "Body", UIKit.Hex("#2A1C13"), 0f, 0f, 1f, 1f);
            body.raycastTarget = false;
            UIKit.AddOutline(body, UIKit.Gold, 3f);
            var sheen = UIKit.Rect(body.transform, "Sheen");
            UIKit.Place(sheen, 0f, 0.55f, 1f, 1f);
            var si = sheen.gameObject.AddComponent<Image>();
            si.sprite = UIKit.Gradient(new Color(1f, 1f, 1f, 0.1f), new Color(1f, 1f, 1f, 0f));
            si.raycastTarget = false;
            // цветная лента слева под значком
            _bannerRibbon = UIKit.Panel(body.transform, "Ribbon", UIKit.Orange, 0f, 0f, 0.2f, 1f);
            _bannerRibbon.raycastTarget = false;
            UIKit.Panel(_bannerRibbon.transform, "Shade", UIKit.Alpha(Color.black, 0.18f), 0.85f, 0f, 1f, 1f, false);

            // значок в круге
            _bannerIcon = UIKit.Rect(body.transform, "Icon");
            UIKit.PlaceFixed(_bannerIcon, 0.1f, 0.5f, 150f, 150f);
            _bannerGlow = UIKit.Shape(_bannerIcon, UIKit.CircleHD, UIKit.Alpha(Color.white, 0.25f), 0.5f, 0.5f, 150f, 150f);
            _bannerGlow.gameObject.AddComponent<Blinker>().speed = 2.5f;
            UIKit.Shape(_bannerIcon, UIKit.CircleHD, UIKit.Hex("#1A0F08"), 0.5f, 0.5f, 128f, 128f);
            _bannerDisc = UIKit.Shape(_bannerIcon, UIKit.CircleHD, UIKit.Orange, 0.5f, 0.5f, 116f, 116f);

            _bannerTitle = UIKit.Label(body.transform, "", 46, UIKit.Gold, TextAnchor.MiddleLeft, 0.23f, 0.5f, 0.97f, 0.94f, true);
            _bannerTitle.resizeTextForBestFit = true;
            _bannerTitle.resizeTextMinSize = 24;
            _bannerTitle.resizeTextMaxSize = 46;
            UIKit.AddOutline(_bannerTitle, UIKit.Hex("#1A0F08"), 3f);
            UIKit.Panel(body.transform, "Line", UIKit.Alpha(UIKit.Gold, 0.35f), 0.23f, 0.49f, 0.95f, 0.5f, false);
            _bannerBody = UIKit.Label(body.transform, "", 28, UIKit.Cream, TextAnchor.UpperLeft, 0.23f, 0.06f, 0.97f, 0.46f, false);
            _bannerBody.resizeTextForBestFit = true;
            _bannerBody.resizeTextMinSize = 16;
            _bannerBody.resizeTextMaxSize = 28;
            _banner.gameObject.SetActive(false);
        }

        static readonly Regex _sizeTag = new Regex(@"</?size(=\d+)?>");

        void ShowBanner(string text, float dur) { ShowBanner(text, dur, GuessKind(text)); }

        void ShowBanner(string text, float dur, BannerKind kind)
        {
            string clean = _sizeTag.Replace(text ?? "", "");
            int nl = clean.IndexOf('\n');
            string title = nl >= 0 ? clean.Substring(0, nl) : clean;
            string sub = nl >= 0 ? clean.Substring(nl + 1).Trim() : "";
            _bannerTitle.text = title.Trim();
            _bannerBody.text = sub;
            // без пояснения — заголовок по центру карточки
            var tr = _bannerTitle.rectTransform;
            tr.anchorMin = new Vector2(0.23f, sub.Length > 0 ? 0.5f : 0.1f);
            tr.anchorMax = new Vector2(0.97f, sub.Length > 0 ? 0.94f : 0.9f);
            _bannerBody.transform.parent.Find("Line").gameObject.SetActive(sub.Length > 0);

            Color c = KindColor(kind);
            _bannerRibbon.color = c;
            _bannerDisc.color = c;
            // перерисовать значок
            for (int i = _bannerIcon.childCount - 1; i >= 3; i--) Destroy(_bannerIcon.GetChild(i).gameObject);
            DrawBannerIcon(_bannerIcon, kind);

            _bannerT = dur + 0.5f;
            _bannerDur = _bannerT;
            _banner.gameObject.SetActive(true);
            _banner.SetAsLastSibling();
            if (_fx != null) _fx.SetAsLastSibling();
            Juice.Punch(_bannerIcon, 0.2f, 0.35f);
        }

        void UpdateBanner(float dt)
        {
            if (_bannerT <= 0f) return;
            _bannerT -= dt;
            float age = _bannerDur - _bannerT;
            // въезд сверху (0.25 с) и плавное исчезновение (0.35 с)
            float inK = Mathf.Clamp01(age / 0.25f);
            float e = 1f - (1f - inK) * (1f - inK) * (1f - inK);
            float outK = Mathf.Clamp01(_bannerT / 0.35f);
            _bannerCard.anchoredPosition = new Vector2(0f, (1f - e) * 70f);
            _bannerGroup.alpha = Mathf.Min(e, outK);
            if (_bannerT <= 0f) _banner.gameObject.SetActive(false);
        }

        static Color KindColor(BannerKind k)
        {
            switch (k)
            {
                case BannerKind.Courier: return UIKit.Hex("#1E88E5");
                case BannerKind.Bulk:    return UIKit.Hex("#EF6C00");
                case BannerKind.Guest:   return UIKit.Hex("#8E24AA");
                case BannerKind.Boss:    return UIKit.Hex("#C62828");
                case BannerKind.Fever:   return UIKit.Hex("#FF5722");
                case BannerKind.Good:    return UIKit.Hex("#43A047");
                case BannerKind.Bad:     return UIKit.Hex("#D32F2F");
                case BannerKind.Fire:    return UIKit.Hex("#E64A19");
                case BannerKind.Rush:    return UIKit.Hex("#F9A825");
                case BannerKind.Time:    return UIKit.Hex("#5E35B1");
                default:                 return UIKit.Hex("#00897B");
            }
        }

        /// <summary>Если тип не указан — угадываем по тексту.</summary>
        static BannerKind GuessKind(string t)
        {
            if (t == null) return BannerKind.Info;
            if (t.Contains("Курьер") || t.Contains("Courier")) return BannerKind.Courier;
            if (t.Contains("лаваш") || t.Contains("lavash")) return BannerKind.Bulk;
            if (t.StartsWith("★")) return BannerKind.Guest;
            if (t.Contains("МЕГ") || t.Contains("MEGA") || t.Contains("Мега")) return BannerKind.Boss;
            if (t.Contains("ЖАРА") || t.Contains("FEVER")) return BannerKind.Fever;
            if (t.Contains("ПОЖАР") || t.Contains("FIRE")) return BannerKind.Fire;
            if (t.Contains("ЧАС ПИК") || t.Contains("RUSH") || t.Contains("сторис") || t.Contains("story")) return BannerKind.Rush;
            if (t.Contains("закрывается") || t.Contains("Closing")) return BannerKind.Time;
            if (t.Contains("штраф") || t.Contains("fined")) return BannerKind.Bad;
            if (t.Contains("выполнена") || t.Contains("reached") || t.Contains("чистота") || t.Contains("Spotless")) return BannerKind.Good;
            return BannerKind.Info;
        }

        static void DrawBannerIcon(RectTransform h, BannerKind k)
        {
            var W = Color.white;
            switch (k)
            {
                case BannerKind.Courier:
                    // фургончик доставки
                    UIKit.Shape(h, UIKit.RoundedSmall, W, 0.44f, 0.54f, 62f, 40f);
                    UIKit.Shape(h, UIKit.RoundedSmall, W, 0.7f, 0.5f, 26f, 30f);
                    UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#90CAF9"), 0.71f, 0.56f, 16f, 12f);
                    UIKit.Shape(h, UIKit.CircleHD, UIKit.Hex("#1A0F08"), 0.36f, 0.34f, 20f, 20f);
                    UIKit.Shape(h, UIKit.CircleHD, UIKit.Hex("#1A0F08"), 0.66f, 0.34f, 20f, 20f);
                    break;
                case BannerKind.Bulk: Icons.Lavash(h, 0.5f, 0.5f, 90f); break;
                case BannerKind.Guest: Icons.Star(h, 0.5f, 0.52f, 80f, true); break;
                case BannerKind.Boss: Icons.Trophy(h, 0.5f, 0.5f, 80f, true); break;
                case BannerKind.Good: Icons.Coin(h, 0.5f, 0.5f, 70f); break;
                case BannerKind.Rush: Icons.People(h, 0.5f, 0.5f, 84f); break;
                case BannerKind.Time: Icons.Clock(h, 0.5f, 0.5f, 76f); break;
                case BannerKind.Bad:
                    UIKit.Shape(h, UIKit.RoundedSmall, W, 0.5f, 0.58f, 16f, 50f);
                    UIKit.Shape(h, UIKit.CircleHD, W, 0.5f, 0.3f, 16f, 16f);
                    break;
                case BannerKind.Fever:
                case BannerKind.Fire:
                    UIKit.Shape(h, UIKit.CircleHD, UIKit.Hex("#FFD54F"), 0.5f, 0.42f, 60f, 70f);
                    UIKit.Shape(h, UIKit.CircleHD, UIKit.Hex("#FFD54F"), 0.5f, 0.6f, 34f, 54f, 0f);
                    UIKit.Shape(h, UIKit.CircleHD, W, 0.5f, 0.38f, 30f, 36f);
                    break;
                default:
                    UIKit.Shape(h, UIKit.CircleHD, W, 0.5f, 0.72f, 16f, 16f);
                    UIKit.Shape(h, UIKit.RoundedSmall, W, 0.5f, 0.42f, 16f, 46f);
                    break;
            }
        }
    }
}
