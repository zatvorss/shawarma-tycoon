using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>
    /// Помощники для сборки интерфейса из кода.
    /// Координаты x0,y0,x1,y1 — доли от родителя (0..1): левый-нижний и правый-верхний угол.
    /// </summary>
    public static class UIKit
    {
        // ---------- палитра ----------
        public static readonly Color Dark       = Hex("#2B1D14");
        public static readonly Color Cream      = Hex("#FFF4E0");
        public static readonly Color Wall       = Hex("#F3C98B");
        public static readonly Color Wood       = Hex("#7A4A2A");
        public static readonly Color WoodLight  = Hex("#C8935E");
        public static readonly Color Steel      = Hex("#55626B");
        public static readonly Color SteelLight = Hex("#A9B4BB");
        public static readonly Color Green      = Hex("#3FA34D");
        public static readonly Color Orange     = Hex("#F08A24");
        public static readonly Color Red        = Hex("#D64541");
        public static readonly Color Blue       = Hex("#3B82C4");
        public static readonly Color Gold       = Hex("#FFC928");
        public static readonly Color Gray       = Hex("#8F8F8F");
        public static readonly Color Purple     = Hex("#8E5CC4");

        public static Color Hex(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            return c;
        }

        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        // ---------- шрифт и спрайты (генерируются кодом — ассеты не нужны) ----------
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        static Sprite _rounded, _roundedSmall, _circle, _tile;

        /// <summary>Скруглённый прямоугольник (9-slice).</summary>
        public static Sprite Rounded
        {
            get { if (_rounded == null) _rounded = MakeRounded(96, 30, true); return _rounded; }
        }

        /// <summary>Слегка скруглённый (для мелочей).</summary>
        public static Sprite RoundedSmall
        {
            get { if (_roundedSmall == null) _roundedSmall = MakeRounded(48, 12, true); return _roundedSmall; }
        }

        static Sprite _circleHD;
        /// <summary>Круг в высоком разрешении — для больших фигур (колесо удачи и т.п.), чтобы не было пикселей.</summary>
        public static Sprite CircleHD
        {
            get { if (_circleHD == null) _circleHD = MakeRounded(512, 256, false); return _circleHD; }
        }

        public static Sprite Circle
        {
            get { if (_circle == null) _circle = MakeRounded(128, 64, false); return _circle; }
        }

        /// <summary>Кафельная плитка для стены кухни.</summary>
        public static Sprite Tile
        {
            get
            {
                if (_tile != null) return _tile;
                const int s = 64;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        bool grout = x < 2 || y < 2;
                        float shade = 1f - 0.05f * ((float)y / s);
                        byte v = grout ? (byte)200 : (byte)(255 * shade);
                        px[y * s + x] = new Color32(v, v, v, 255);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                _tile = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _tile;
            }
        }

        static Sprite _arrow;
        /// <summary>Ровная сплошная стрелка вправо (древко + наконечник), со сглаженными краями. Влево — поверни на 180°.</summary>
        public static Sprite Arrow
        {
            get
            {
                if (_arrow != null) return _arrow;
                const int s = 256;
                var poly = new[]
                {
                    new Vector2(28, 100), new Vector2(128, 100), new Vector2(128, 38), new Vector2(232, 128),
                    new Vector2(128, 218), new Vector2(128, 156), new Vector2(28, 156)
                };
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        var pt = new Vector2(x + 0.5f, y + 0.5f);
                        bool inside = false;
                        float best = float.MaxValue;
                        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                        {
                            var a = poly[i]; var b = poly[j];
                            if (((a.y > pt.y) != (b.y > pt.y)) && (pt.x < (b.x - a.x) * (pt.y - a.y) / (b.y - a.y) + a.x)) inside = !inside;
                            var ab = b - a;
                            float t = Mathf.Clamp01(Vector2.Dot(pt - a, ab) / ab.sqrMagnitude);
                            best = Mathf.Min(best, (a + ab * t - pt).magnitude);
                        }
                        float alpha = inside ? Mathf.Clamp01(best + 0.5f) : Mathf.Clamp01(0.5f - best);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                _arrow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
                return _arrow;
            }
        }

        /// <summary>Вертикальный градиент сверху вниз.</summary>
        public static Sprite Gradient(Color top, Color bottom)
        {
            const int h = 64;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, (float)y / (h - 1));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRounded(int size, int radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, r, size - r);
                    float cy = Mathf.Clamp(fy, r, size - r);
                    float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        // ---------- базовые элементы ----------
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Фиксированный размер (в единицах канваса), центр — в точке (ax, ay) родителя.</summary>
        public static void PlaceFixed(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        public static RectTransform Full(Transform parent, string name)
        {
            var rt = Rect(parent, name);
            Place(rt, 0, 0, 1, 1);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color,
            float x0 = 0, float y0 = 0, float x1 = 1, float y1 = 1, bool rounded = true)
        {
            var rt = Rect(parent, name);
            Place(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Фигура фиксированного размера (для рисования иконок и персонажей).</summary>
        public static Image Shape(Transform parent, Sprite sprite, Color color,
            float ax, float ay, float w, float h, float rot = 0f)
        {
            var rt = Rect(parent, "Shape");
            PlaceFixed(rt, ax, ay, w, h);
            if (rot != 0f) rt.localRotation = Quaternion.Euler(0, 0, rot);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image CircleImg(Transform parent, string name, Color color, float ax, float ay, float size)
        {
            var img = Shape(parent, Circle, color, ax, ay, size, size);
            img.gameObject.name = name;
            return img;
        }

        public static void AddShadow(Graphic g, float dist = 6f, float alpha = 0.3f)
        {
            var sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, alpha);
            sh.effectDistance = new Vector2(dist * 0.5f, -dist);
        }

        public static void AddOutline(Graphic g, Color c, float dist = 2f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(dist, -dist);
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter,
            float x0 = 0, float y0 = 0, float x1 = 1, float y1 = 1, bool bold = false)
        {
            var rt = Rect(parent, "Text");
            Place(rt, x0, y0, x1, y1);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (bold) t.fontStyle = FontStyle.Bold;
            rt.gameObject.AddComponent<WordFit>();
            return t;
        }

        /// <summary>Текст, который сам уменьшается, если не влезает.</summary>
        public static Text FitLabel(Transform parent, string text, int maxSize, Color color,
            float x0 = 0, float y0 = 0, float x1 = 1, float y1 = 1, bool bold = true)
        {
            var t = Label(parent, text, maxSize, color, TextAnchor.MiddleCenter, x0, y0, x1, y1, bold);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;
            t.resizeTextMaxSize = maxSize;
            return t;
        }

        /// <summary>Сумма денег в одну строку: чем длиннее число, тем мельче шрифт (миллиарды не переносятся).</summary>
        public static void SetMoney(Text t, long v, int maxSize)
        {
            string s = GameData.Money(v);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.resizeTextForBestFit = false;
            t.fontSize = s.Length >= 10 ? Mathf.RoundToInt(maxSize * 0.62f) : (s.Length >= 8 ? Mathf.RoundToInt(maxSize * 0.75f) : maxSize);
            t.text = s;
        }

        /// <summary>Делает из картинки кнопку: реакция на нажатие + "пружинка".</summary>
        public static Button MakeButton(Image img, UnityAction onClick)
        {
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            cb.fadeDuration = 0.05f;
            b.colors = cb;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(onClick);
            img.gameObject.AddComponent<ButtonJuice>();
            return b;
        }

        public static Button Button(Transform parent, string label, Color color, UnityAction onClick,
            float x0 = 0, float y0 = 0, float x1 = 1, float y1 = 1, int fontSize = 44)
        {
            var img = Panel(parent, "Btn", color, x0, y0, x1, y1);
            AddShadow(img, 6f, 0.3f);
            // светлая полоска сверху — объём
            var gloss = Panel(img.transform, "Gloss", new Color(1, 1, 1, 0.15f), 0.04f, 0.55f, 0.96f, 0.94f);
            gloss.raycastTarget = false;
            var b = MakeButton(img, onClick);

            var t = FitLabel(img.transform, label, fontSize, Color.white, 0.05f, 0.08f, 0.95f, 0.92f);
            AddShadow(t, 3f, 0.4f);
            return b;
        }

        /// <summary>
        /// Кнопка с ценой: [подпись] [монетка] [число] — монетка никогда не налезает на цифры.
        /// </summary>
        public static Button PriceButton(Transform parent, string label, long cost, Color color, UnityAction onClick,
            float x0, float y0, float x1, float y1, int fontSize = 34)
        {
            var b = Button(parent, "", color, onClick, x0, y0, x1, y1, fontSize);
            var old = b.GetComponentInChildren<Text>();
            if (old != null) Object.Destroy(old.gameObject);
            bool hasLabel = !string.IsNullOrEmpty(label);
            float coinX = hasLabel ? 0.56f : 0.32f;
            if (hasLabel)
            {
                var l = FitLabel(b.transform, label, fontSize, Color.white, 0.04f, 0.1f, 0.5f, 0.9f);
                l.alignment = TextAnchor.MiddleRight;
                AddShadow(l, 3f, 0.4f);
            }
            Icons.Coin(b.transform, coinX, 0.5f, fontSize * 1.1f);
            var n = FitLabel(b.transform, GameData.Money(cost), fontSize, Color.white, coinX + 0.05f, 0.1f, 0.97f, 0.9f);
            n.alignment = TextAnchor.MiddleLeft;
            AddShadow(n, 3f, 0.4f);
            return b;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        /// <summary>Прозрачная кликабельная область на весь родитель.</summary>
        public static Button HitArea(Transform parent, string name, UnityAction onClick)
        {
            var rt = Full(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Полоска прогресса. Возвращает заливку — её меняем через SetFill.</summary>
        public static RectTransform Bar(Transform parent, Color back, Color fill,
            float x0, float y0, float x1, float y1)
        {
            var bg = Panel(parent, "Bar", back, x0, y0, x1, y1, true);
            bg.sprite = RoundedSmall;
            // заполнение «обрезкой», а не сжатием: полоска во всю ширину, видна только часть слева —
            // при малом значении она не превращается в кривой овал
            var clip = Rect(bg.transform, "Clip");
            Place(clip, 0, 0, 1, 1);
            clip.gameObject.AddComponent<RectMask2D>();
            var f = Panel(clip, "Fill", fill, 0, 0, 0, 1, true);
            f.sprite = RoundedSmall;
            f.rectTransform.pivot = new Vector2(0f, 0.5f);
            f.gameObject.AddComponent<MatchWidth>().target = bg.rectTransform;
            return f.rectTransform;
        }

        public static void SetFill(RectTransform fill, float v)
        {
            v = Mathf.Clamp01(v);
            var clip = fill.parent as RectTransform;
            if (clip != null && clip.name == "Clip")
            {
                clip.anchorMax = new Vector2(v, 1f);
                clip.offsetMax = Vector2.zero;
                fill.gameObject.SetActive(v > 0.001f);
                return;
            }
            fill.anchorMax = new Vector2(v, 1f);
            fill.offsetMax = Vector2.zero;
            fill.gameObject.SetActive(v > 0.001f);
        }

        /// <summary>
        /// Модальное окно: затемнение (тап мимо — закрыть), карточка с цветной шапкой и кнопкой X.
        /// Возвращает область для содержимого.
        /// </summary>
        public static RectTransform Popup(Transform parent, string title, Color header,
            float x0, float y0, float x1, float y1, UnityAction onClose)
        {
            var ov = Panel(parent, "Popup", new Color(0, 0, 0, 0.55f), 0, 0, 1, 1, false);
            ov.raycastTarget = true;
            var overlay = ov.gameObject;
            UnityAction close = () =>
            {
                Sfx.Play(Sound.Click);
                Object.Destroy(overlay);
                if (onClose != null) onClose();
            };
            HitArea(ov.transform, "Outside", close);
            var card = Panel(ov.transform, "Card", Cream, x0, y0, x1, y1);
            card.raycastTarget = true;
            AddShadow(card, 14f, 0.45f);
            var head = Panel(card.transform, "Header", header, 0, 0.84f, 1, 1);
            var t = FitLabel(head.transform, title, 46, Color.white, 0.04f, 0.1f, 0.84f, 0.9f);
            AddShadow(t, 3f, 0.4f);
            Button(head.transform, "X", Red, close, 0.87f, 0.14f, 0.98f, 0.86f, 34);
            var content = Rect(card.transform, "Content");
            Place(content, 0.03f, 0.03f, 0.97f, 0.82f);
            Juice.Punch(card.rectTransform, 0.08f, 0.25f);
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>Держит ширину элемента равной ширине другого (заполнение полосок).</summary>
    public class MatchWidth : MonoBehaviour
    {
        public RectTransform target;
        void LateUpdate()
        {
            if (target == null) return;
            var rt = (RectTransform)transform;
            float w = target.rect.width;
            if (!Mathf.Approximately(rt.sizeDelta.x, w)) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        }
    }

    /// <summary>
    /// Не даёт Unity переносить слово по буквам («Предпринимател-ь»): если самое длинное слово
    /// не влезает в ширину, шрифт уменьшается ровно настолько, чтобы слово встало целиком.
    /// </summary>
    public class WordFit : MonoBehaviour
    {
        static readonly System.Text.RegularExpressions.Regex Tags = new System.Text.RegularExpressions.Regex("<[^>]+>");
        Text _t;
        string _last;
        float _lastW = -1f;
        int _base = -1, _baseMin;
        bool _bestFit;

        void Awake() { _t = GetComponent<Text>(); }

        void LateUpdate()
        {
            if (_t == null || _t.font == null) return;
            if (_t.horizontalOverflow == HorizontalWrapMode.Overflow) return;
            float w = ((RectTransform)transform).rect.width;
            if (w <= 1f) return;
            if (_t.text == _last && Mathf.Approximately(w, _lastW)) return;
            _last = _t.text;
            _lastW = w;
            if (_base < 0)
            {
                _bestFit = _t.resizeTextForBestFit;
                _base = _bestFit ? _t.resizeTextMaxSize : _t.fontSize;
                _baseMin = _t.resizeTextMinSize;
            }
            if (string.IsNullOrEmpty(_last)) return;
            string plain = _t.supportRichText ? Tags.Replace(_last, "") : _last;
            var words = plain.Split(new[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            var st = _t.GetGenerationSettings(new Vector2(10000f, 10000f));
            st.fontSize = _base;
            st.resizeTextForBestFit = false;
            st.horizontalOverflow = HorizontalWrapMode.Overflow;
            st.verticalOverflow = VerticalWrapMode.Overflow;
            st.richText = false;
            st.scaleFactor = 1f;
            float widest = 0f;
            var gen = _t.cachedTextGeneratorForLayout;
            foreach (var word in words)
                widest = Mathf.Max(widest, gen.GetPreferredWidth(word, st));
            int size = _base;
            if (widest > w * 0.98f && widest > 0f) size = Mathf.Max(8, Mathf.FloorToInt(_base * w * 0.96f / widest));
            if (_bestFit)
            {
                _t.resizeTextMaxSize = size;
                _t.resizeTextMinSize = Mathf.Min(_baseMin, size);
            }
            else _t.fontSize = size;
        }
    }
}
