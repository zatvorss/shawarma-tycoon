using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValourGames.Shawarma
{
    /// <summary>Клиент: тип, заказ (шаурма + напиток), терпение, рисованный персонаж с эмоциями.</summary>
    public class Customer
    {
        static readonly string[] Skins = { "#F6D2B0", "#E9BC94", "#C68A5E", "#8D5A3B" };
        static readonly string[] Shirts = { "#3B82C4", "#8E44AD", "#16A085", "#E67E22", "#2E86AB", "#C0392B", "#6C5B7B" };
        static readonly string[] Hairs = { "#2C1B10", "#5B3A1E", "#C9A15B", "#A0391E", "#1A1A1A" };

        public readonly CustType type;
        public int[] order;
        public readonly Drink drink;
        public bool needShawarma = true;
        public bool needDrink;
        public readonly float maxPatience;
        public float patience;
        public bool leaving;
        public bool changedMind;
        public bool gotPerfect;
        public ServeMode mode;
        public bool modeKnown;
        public bool packOk = true;
        public readonly bool golden;
        public readonly int bizIndex;   // >= 0 — курьер бизнеса
        public int qty;                 // сколько шаурм ещё нужно (у курьера несколько)
        public readonly int qtyTotal;
        public readonly bool special;   // заказал "Хит дня"
        public readonly bool boss;      // сам Мистер Мега
        public bool IsCourier { get { return bizIndex >= 0; } }
        public readonly bool car;       // машина у окна Drive-Thru
        public bool honked;
        public bool gotFresh;           // шаурма на горячем лаваше из тандыра
        public int lavashQty;           // оптовик из соседнего кафе: сколько горячих лавашей ещё нужно
        public readonly int lavashTotal;
        public bool IsBulk { get { return lavashTotal > 0; } }
        string rivalName = "";
        static readonly string[] RivalRu = { "«Донер №1»", "«Шаурма у Гоши»", "«Кебаб-Хаус»", "«Восточный дворик»", "«Лаваш-Экспресс»", "«Гриль-Бар»" };
        Image _aura;
        public float leaveT;
        public RectTransform root;

        CanvasGroup _cg;
        RectTransform _char, _bubble, _content, _strip, _sayBox;
        Text _say;
        float _sayT;
        RectTransform _barFill;
        Image _barImg, _head, _mouth, _browL, _browR;
        RectTransform _eyeL, _eyeR;
        Text _reaction;
        Color _skin;
        float _appear, _blinkT = 2f, _blinkHold;
        bool _happyLeave;

        public float PatienceFrac { get { return Mathf.Clamp01(patience / maxPatience); } }
        public bool Done { get { return !needShawarma && !needDrink && lavashQty <= 0; } }
        public Vector3 BubbleWorld { get { return _bubble.position; } }

        public Customer(RectTransform slot, CustType type, int[] order, Drink drink, float patienceSeconds, ServeMode mode, bool modeKnown, bool golden,
            int bizIndex = -1, int qty = 1, bool special = false, bool boss = false, bool car = false, int lavash = 0)
        {
            this.car = car;
            lavashTotal = lavash;
            lavashQty = lavash;
            if (lavash > 0) rivalName = RivalRu[Random.Range(0, RivalRu.Length)];
            this.type = type;
            this.order = order;
            this.drink = drink;
            this.mode = mode;
            this.modeKnown = modeKnown;
            this.golden = golden;
            this.bizIndex = bizIndex;
            this.qty = Mathf.Max(1, qty);
            this.qtyTotal = this.qty;
            this.special = special;
            this.boss = boss;
            needDrink = drink != Drink.None;
            if (lavash > 0) needShawarma = false;
            maxPatience = patienceSeconds;
            patience = patienceSeconds;
            Build(slot);
        }

        // =====================================================================
        void Build(RectTransform slot)
        {
            root = UIKit.Full(slot, "Customer");
            root.SetAsFirstSibling(); // зона клика слота остаётся поверх
            _cg = root.gameObject.AddComponent<CanvasGroup>();
            _cg.blocksRaycasts = false;
            _cg.alpha = 0f;

            if (golden)
            {
                _aura = UIKit.Shape(root, UIKit.CircleHD, new Color(1f, 0.85f, 0.2f, 0.22f), 0.22f, 0.47f, 230f, 230f);
                for (int i = 0; i < 6; i++)
                    UIKit.Shape(root, UIKit.Circle, UIKit.Gold, 0.22f + Mathf.Cos(i * 1.05f) * 0.18f, 0.45f + Mathf.Sin(i * 1.05f) * 0.28f, 10f, 10f);
            }
            BuildCharacter();
            if (car) BuildCar();

            // --- пузырь заказа ---
            var bubble = UIKit.Panel(root, "Bubble", Color.white, 0.42f, 0.22f, 0.99f, 0.98f);
            UIKit.AddShadow(bubble, 6f, 0.2f);
            if (golden) UIKit.AddOutline(bubble, UIKit.Gold, 4f);
            else if (special) UIKit.AddOutline(bubble, UIKit.Red, 3f);
            _bubble = bubble.rectTransform;
            UIKit.Shape(_bubble, UIKit.RoundedSmall, Color.white, 0f, 0.3f, 30f, 30f, 45f);
            _strip = UIKit.Rect(_bubble, "Strip");
            UIKit.Place(_strip, 0.03f, 0.8f, 0.97f, 0.99f);
            _content = UIKit.Rect(_bubble, "Content");
            UIKit.Place(_content, 0f, 0f, 1f, 0.8f);
            BuildOrderView();
            if (type == CustType.Vegan && !IsCourier && !boss)
            {
                var vt = UIKit.Panel(_bubble, "Vegan", UIKit.Hex("#2E7D32"), 0.5f, -0.02f, 0.99f, 0.07f);
                UIKit.AddShadow(vt, 3f, 0.3f);
                UIKit.FitLabel(vt.transform, L.T("БЕЗ МЯСА!", "NO MEAT!"), 18, Color.white, 0.05f, 0.05f, 0.95f, 0.95f);
            }

            // --- терпение ---
            _barFill = UIKit.Bar(root, new Color(0, 0, 0, 0.3f), UIKit.Green, 0.44f, 0.12f, 0.97f, 0.18f);
            _barImg = _barFill.GetComponent<Image>();

            // --- реакция ---
            _reaction = UIKit.Label(root, "", 44, UIKit.Dark, TextAnchor.MiddleCenter, 0.4f, 0.4f, 1f, 0.8f, true);
            UIKit.AddOutline(_reaction, Color.white, 3f);

            // реплика
            // реплика — над головой, строго левее пузыря заказа (не залезает на него)
            var sayBg = UIKit.Panel(root, "Say", new Color(1f, 0.99f, 0.95f, 0.97f), 0.005f, 0.8f, 0.405f, 0.985f);
            UIKit.AddShadow(sayBg, 4f, 0.25f);
            UIKit.AddOutline(sayBg, UIKit.Alpha(UIKit.Dark, 0.35f), 2f);
            _sayBox = sayBg.rectTransform;
            UIKit.Shape(_sayBox, UIKit.RoundedSmall, new Color(1f, 0.99f, 0.95f, 0.97f), 0.5f, 0f, 22f, 22f, 45f);
            _say = UIKit.FitLabel(sayBg.transform, "", 20, UIKit.Dark, 0.04f, 0.06f, 0.96f, 0.94f);
            _sayBox.gameObject.SetActive(false);

            // --- кто это ---
            string nm = golden ? L.T("Золотой клиент", "Golden customer") : GameData.Get(type).Name;
            if (IsCourier) nm = L.T("Курьер: ", "Courier: ") + Meta.Businesses[bizIndex].Name;
            if (boss) nm = L.T("БОСС: Мистер Мега", "BOSS: Mr. Mega");
            if (car) nm = L.T("ШаурмаАвто", "ShawarmaAuto");
            if (IsBulk) nm = L.T("Оптовик: ", "Buyer: ") + rivalName;
            bool sp = GameData.Get(type).special && !IsCourier && !boss && !golden;
            if (sp) nm = "★ " + nm;
            var name = UIKit.Label(root, nm, 22, golden ? UIKit.Gold : (sp ? UIKit.Hex("#E1BEE7") : UIKit.Cream), TextAnchor.MiddleCenter, 0f, 0f, 0.44f, 0.08f, true);
            UIKit.AddShadow(name, 3f, 0.6f);
        }

        void BuildCharacter()
        {
            _char = UIKit.Rect(root, "Char");
            UIKit.Place(_char, 0f, 0.07f, 0.44f, 0.8f);

            _skin = UIKit.Hex(Skins[Random.Range(0, Skins.Length)]);
            string shirt = Shirts[Random.Range(0, Shirts.Length)];
            string hair = Hairs[Random.Range(0, Hairs.Length)];
            if (type == CustType.Business) shirt = "#2C3E50";
            if (bizIndex >= 0) shirt = Meta.Businesses[bizIndex].color;
            if (boss) { shirt = "#FFC928"; hair = "#111111"; }
            if (type == CustType.Athlete) shirt = Random.value < 0.5f ? "#D32F2F" : "#1565C0";
            if (type == CustType.Builder) shirt = "#FF6F00";
            if (type == CustType.Tourist) shirt = "#26C6DA";
            if (type == CustType.Kid) shirt = Shirts[Random.Range(0, Shirts.Length)];
            if (type == CustType.Critic) { shirt = "#4E342E"; hair = "#1A1A1A"; }
            if (type == CustType.Granny) hair = "#D5D5D5";
            if (type == CustType.Blogger) hair = Random.value < 0.5f ? "#FF4FA3" : "#22C7B8";
            NewShirt(ref shirt, ref hair);

            var R = UIKit.Rounded;
            var C = UIKit.Circle;
            var shirtC = UIKit.Hex(shirt);

            // тень на земле
            UIKit.Shape(_char, C, new Color(0, 0, 0, 0.22f), 0.5f, 0.02f, 170f, 30f);
            // руки
            UIKit.Shape(_char, R, Color.Lerp(shirtC, Color.black, 0.15f), 0.14f, 0.2f, 40f, 140f, 8f);
            UIKit.Shape(_char, R, Color.Lerp(shirtC, Color.black, 0.15f), 0.86f, 0.2f, 40f, 140f, -8f);
            // тело
            var body = UIKit.Shape(_char, R, shirtC, 0.5f, 0.2f, type == CustType.Glutton ? 200f : 150f, 180f);
            // шея
            UIKit.Shape(_char, UIKit.RoundedSmall, Color.Lerp(_skin, Color.black, 0.08f), 0.5f, 0.42f, 44f, 36f);

            if (type == CustType.Business)
            {
                UIKit.Shape(body.transform, UIKit.RoundedSmall, Color.white, 0.5f, 0.78f, 46f, 60f);
                UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Red, 0.5f, 0.66f, 16f, 80f);
            }
            if (type == CustType.Builder)
            {
                UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Hex("#E0E0E0"), 0.5f, 0.55f, 150f, 12f);
                UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Hex("#E0E0E0"), 0.5f, 0.35f, 150f, 12f);
            }
            if (type == CustType.Tourist)
            {
                for (int f = 0; f < 4; f++)
                    UIKit.Shape(body.transform, UIKit.Circle, UIKit.Hex("#FFEB3B"), 0.25f + (f % 2) * 0.5f, 0.3f + (f / 2) * 0.35f, 26f, 22f);
                var cam = UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Hex("#263238"), 0.5f, 0.55f, 56f, 38f);
                UIKit.Shape(cam.transform, UIKit.Circle, UIKit.Hex("#90CAF9"), 0.5f, 0.5f, 22f, 22f);
            }
            if (type == CustType.Athlete)
                UIKit.Shape(body.transform, UIKit.RoundedSmall, Color.white, 0.5f, 0.6f, 20f, 150f);
            if (type == CustType.Student || type == CustType.Kid)
            {
                UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Hex("#3A3A3A"), 0.26f, 0.5f, 14f, 170f);
                UIKit.Shape(body.transform, UIKit.RoundedSmall, UIKit.Hex("#3A3A3A"), 0.74f, 0.5f, 14f, 170f);
            }

            NewBody(body.transform);

            // платок бабушки — позади головы
            if (type == CustType.Granny)
            {
                UIKit.Shape(_char, C, UIKit.Hex("#B83B5E"), 0.5f, 0.58f, 158f, 158f);
                UIKit.Shape(_char, R, UIKit.Hex("#B83B5E"), 0.5f, 0.44f, 60f, 30f, 45f);
            }
            // длинные волосы — позади головы
            if (type == CustType.Surfer || type == CustType.Stewardess || type == CustType.Socialite || (type != CustType.Granny && type != CustType.Business && !HasHat() && Random.value < 0.4f))
                UIKit.Shape(_char, R, UIKit.Hex(hair), 0.5f, 0.54f, 150f, 130f);

            // голова
            _head = UIKit.Shape(_char, C, _skin, 0.5f, 0.58f, 130f, 130f);
            var h = _head.transform;

            // волосы/шапки
            if (type == CustType.Granny)
            {
                UIKit.Shape(h, R, UIKit.Hex("#B83B5E"), 0.5f, 0.9f, 130f, 44f);
                UIKit.Shape(h, C, UIKit.Hex("#F8C8D8"), 0.3f, 0.92f, 14f, 14f);
                UIKit.Shape(h, C, UIKit.Hex("#F8C8D8"), 0.6f, 0.95f, 12f, 12f);
            }
            else if (type == CustType.Builder)
            {
                UIKit.Shape(h, C, UIKit.Hex("#FDD835"), 0.5f, 0.86f, 140f, 80f);
                UIKit.Shape(h, R, UIKit.Hex("#F9A825"), 0.5f, 0.8f, 160f, 18f);
            }
            else if (type == CustType.Tourist)
            {
                UIKit.Shape(h, R, UIKit.Hex("#FFF8E1"), 0.5f, 0.84f, 170f, 22f);
                UIKit.Shape(h, R, UIKit.Hex("#FFF8E1"), 0.5f, 0.94f, 110f, 44f);
                UIKit.Shape(h, R, UIKit.Hex("#EF5350"), 0.5f, 0.88f, 112f, 10f);
            }
            else if ((type == CustType.Student || type == CustType.Kid) && Random.value < 0.7f)
            {
                var cap = UIKit.Hex(Shirts[Random.Range(0, Shirts.Length)]);
                UIKit.Shape(h, R, cap, 0.5f, 0.88f, 132f, 50f);
                UIKit.Shape(h, R, Color.Lerp(cap, Color.black, 0.2f), 0.8f, 0.8f, 70f, 16f);
            }
            else if (NewHead(h, hair)) { }
            else
            {
                UIKit.Shape(h, R, UIKit.Hex(hair), 0.5f, 0.88f, 124f, 48f);
                if (type == CustType.Blogger) UIKit.Shape(h, R, UIKit.Hex(hair), 0.3f, 0.78f, 60f, 30f, 25f);
            }

            // щёки
            UIKit.Shape(h, C, new Color(1f, 0.4f, 0.4f, 0.25f), 0.24f, 0.36f, 26f, 18f);
            UIKit.Shape(h, C, new Color(1f, 0.4f, 0.4f, 0.25f), 0.76f, 0.36f, 26f, 18f);

            // глаза
            _eyeL = UIKit.Shape(h, C, UIKit.Dark, 0.36f, 0.52f, 16f, 20f).rectTransform;
            _eyeR = UIKit.Shape(h, C, UIKit.Dark, 0.64f, 0.52f, 16f, 20f).rectTransform;
            UIKit.Shape(_eyeL, C, Color.white, 0.65f, 0.7f, 6f, 6f);
            UIKit.Shape(_eyeR, C, Color.white, 0.65f, 0.7f, 6f, 6f);

            if (type == CustType.Granny)
            {
                // очки: оправа-кольца
                var ringL = UIKit.Shape(h, C, UIKit.Hex("#5D4037"), 0.36f, 0.52f, 38f, 38f);
                ringL.transform.SetSiblingIndex(_eyeL.GetSiblingIndex());
                UIKit.Shape(ringL.transform, C, UIKit.Alpha(Color.white, 0.55f), 0.5f, 0.5f, 30f, 30f);
                var ringR = UIKit.Shape(h, C, UIKit.Hex("#5D4037"), 0.64f, 0.52f, 38f, 38f);
                ringR.transform.SetSiblingIndex(_eyeR.GetSiblingIndex());
                UIKit.Shape(ringR.transform, C, UIKit.Alpha(Color.white, 0.55f), 0.5f, 0.5f, 30f, 30f);
            }

            if (type == CustType.Athlete)
                UIKit.Shape(h, R, UIKit.Hex("#FFFFFF"), 0.5f, 0.78f, 132f, 16f);

            // брови и рот
            _browL = UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Dark, 0.34f, 0.7f, 28f, 7f);
            _browR = UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Dark, 0.66f, 0.7f, 28f, 7f);
            _mouth = UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#8C3B2E"), 0.5f, 0.28f, 40f, 12f);
            NewFace(h, hair);

            // критик: берет, усы, блокнот
            if (type == CustType.Critic)
            {
                UIKit.Shape(h, C, UIKit.Hex("#B71C1C"), 0.46f, 0.92f, 120f, 46f, 8f);
                UIKit.Shape(h, C, UIKit.Hex("#B71C1C"), 0.46f, 1.02f, 14f, 14f);
                UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Dark, 0.4f, 0.36f, 30f, 9f, 12f);
                UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Dark, 0.6f, 0.36f, 30f, 9f, -12f);
                var pad = UIKit.Shape(_char, UIKit.RoundedSmall, UIKit.Hex("#FFF8E1"), 0.86f, 0.3f, 46f, 60f, 8f);
                for (int l = 0; l < 3; l++)
                    UIKit.Shape(pad.transform, UIKit.RoundedSmall, UIKit.Alpha(UIKit.Dark, 0.4f), 0.5f, 0.75f - l * 0.22f, 32f, 3f);
            }

            if (boss)
            {
                UIKit.Shape(h, UIKit.RoundedSmall, UIKit.Hex("#111111"), 0.5f, 0.54f, 120f, 26f);
                // Мистер Мега: зализанный чуб, золотая цепь с медальоном «М»
                UIKit.Shape(h, UIKit.Rounded, UIKit.Hex("#111111"), 0.6f, 0.95f, 110f, 40f, -8f);
                var med = UIKit.Shape(_char, UIKit.Circle, UIKit.Gold, 0.5f, 0.3f, 46f, 46f);
                UIKit.AddOutline(med, UIKit.Hex("#B8860B"), 2f);
                UIKit.FitLabel(med.transform, "M", 30, UIKit.Hex("#6A1B9A"), 0.1f, 0.1f, 0.9f, 0.9f);
                for (int ch = 0; ch < 5; ch++) UIKit.Shape(_char, UIKit.Circle, UIKit.Gold, 0.36f + ch * 0.07f, 0.4f - Mathf.Abs(ch - 2) * 0.025f, 10f, 10f);
            }
            if (type == CustType.Kid) { _char.pivot = new Vector2(0.5f, 0f); _char.localScale = new Vector3(0.82f, 0.82f, 1f); }

            NewProps();

            // телефон блогера
            if (type == CustType.Blogger)
            {
                var phone = UIKit.Shape(_char, UIKit.RoundedSmall, UIKit.Hex("#222222"), 0.88f, 0.34f, 40f, 70f, -10f);
                UIKit.Shape(phone.transform, UIKit.RoundedSmall, UIKit.Hex("#7FD8FF"), 0.5f, 0.52f, 30f, 52f);
            }
        }

        /// <summary>Машина у окна Drive-Thru: водитель сидит внутри, видна голова в окне.</summary>
        void BuildCar()
        {
            string[] cols = { "#E53935", "#1E88E5", "#43A047", "#FDD835", "#8E24AA", "#212121", "#ECEFF1", "#FF7043" };
            var col = UIKit.Hex(cols[Random.Range(0, cols.Length)]);
            var dark = Color.Lerp(col, Color.black, 0.3f);
            var car = UIKit.Rect(root, "Car");
            UIKit.Place(car, -0.03f, 0.04f, 0.47f, 0.66f);
            // крыша и стойки (окно прозрачное — видно водителя)
            UIKit.Panel(car, "Roof", dark, 0.14f, 0.9f, 0.86f, 0.98f);
            UIKit.Panel(car, "PillarL", dark, 0.12f, 0.5f, 0.2f, 0.95f);
            UIKit.Panel(car, "PillarR", dark, 0.8f, 0.5f, 0.88f, 0.95f);
            UIKit.Panel(car, "Glass", new Color(0.7f, 0.9f, 1f, 0.18f), 0.2f, 0.52f, 0.8f, 0.9f);
            // кузов
            var body = UIKit.Panel(car, "Body", col, 0f, 0.1f, 1f, 0.55f);
            UIKit.AddShadow(body, 5f, 0.3f);
            UIKit.Panel(car, "Stripe", UIKit.Alpha(Color.white, 0.35f), 0.02f, 0.36f, 0.98f, 0.4f, false);
            UIKit.Panel(car, "Handle", dark, 0.55f, 0.3f, 0.68f, 0.34f);
            UIKit.Shape(car, UIKit.Circle, UIKit.Hex("#FFF59D"), 0.05f, 0.22f, 26f, 18f);
            UIKit.Panel(car, "Under", UIKit.Hex("#263238"), 0.04f, -0.02f, 0.96f, 0.14f);
            // колёса
            foreach (var wx in new[] { 0.2f, 0.8f })
            {
                UIKit.Shape(car, UIKit.CircleHD, UIKit.Hex("#212121"), wx, 0.1f, 64f, 64f);
                UIKit.Shape(car, UIKit.CircleHD, UIKit.Hex("#9E9E9E"), wx, 0.1f, 28f, 28f);
            }
            // рука водителя с купюрой
            var bill = UIKit.Shape(car, UIKit.RoundedSmall, UIKit.Hex("#81C784"), 0.93f, 0.56f, 40f, 22f, -10f);
            bill.gameObject.AddComponent<Bobber>().amp = 3f;
        }

        // =====================================================================
        //  НОВЫЕ ПЕРСОНАЖИ: одежда, головные уборы, лица, предметы
        // =====================================================================
        bool HasHat()
        {
            switch (type)
            {
                case CustType.Police: case CustType.Fireman: case CustType.Rocker: case CustType.Hipster:
                case CustType.Taxi: case CustType.Grandpa: case CustType.Fisher: case CustType.Pilot:
                case CustType.Clown: case CustType.Musician: case CustType.Haggler: case CustType.Sheikh: case CustType.Millionaire:
                    return true;
            }
            return false;
        }

        void NewShirt(ref string shirt, ref string hair)
        {
            switch (type)
            {
                case CustType.Police: shirt = "#1A237E"; break;
                case CustType.Doctor: shirt = "#F5F5F5"; break;
                case CustType.Fireman: shirt = "#C62828"; break;
                case CustType.Rocker: shirt = "#212121"; hair = "#E91E63"; break;
                case CustType.Gamer: shirt = "#7C4DFF"; break;
                case CustType.Hipster: shirt = "#8D3B2B"; hair = "#5B3A1E"; break;
                case CustType.Taxi: shirt = "#FFD600"; break;
                case CustType.Grandpa: shirt = "#795548"; hair = "#D5D5D5"; break;
                case CustType.Fisher: shirt = "#558B2F"; break;
                case CustType.Surfer: shirt = "#00BCD4"; hair = "#F2D16B"; break;
                case CustType.Pilot: shirt = "#0D47A1"; break;
                case CustType.Stewardess: shirt = "#C62828"; break;
                case CustType.Fan: shirt = Random.value < 0.5f ? "#2E7D32" : "#1565C0"; break;
                case CustType.Clown: shirt = "#FFEB3B"; break;
                case CustType.Musician: shirt = "#4E342E"; break;
                case CustType.Glutton: shirt = "#FF7043"; break;
                case CustType.Inspector: shirt = "#607D8B"; hair = "#3E2723"; break;
                case CustType.Haggler: shirt = "#FF8F00"; hair = "#1A1A1A"; break;
                case CustType.Vegan: shirt = "#66BB6A"; break;
                case CustType.Sheikh: shirt = "#0D2A4F"; hair = "#6D4C41"; break;
                case CustType.Deputy: shirt = "#263238"; break;
                case CustType.Minister: shirt = "#1A237E"; hair = "#BDBDBD"; break;
                case CustType.Millionaire: shirt = "#111111"; break;
                case CustType.Socialite: shirt = "#EC407A"; hair = "#F2D16B"; break;
            }
        }

        void NewBody(Transform body)
        {
            var R = UIKit.RoundedSmall;
            var C = UIKit.Circle;
            switch (type)
            {
                case CustType.Police:
                    UIKit.Shape(body, C, UIKit.Gold, 0.3f, 0.7f, 22f, 22f);
                    UIKit.Shape(body, R, UIKit.Hex("#0D1540"), 0.5f, 0.6f, 12f, 90f);
                    break;
                case CustType.Doctor:
                    UIKit.Shape(body, C, UIKit.Hex("#455A64"), 0.5f, 0.7f, 76f, 70f);
                    UIKit.Shape(body, C, UIKit.Hex("#F5F5F5"), 0.5f, 0.74f, 62f, 58f);
                    UIKit.Shape(body, C, UIKit.Hex("#90A4AE"), 0.5f, 0.42f, 18f, 18f);
                    UIKit.Shape(body, R, UIKit.Red, 0.75f, 0.72f, 8f, 22f);
                    UIKit.Shape(body, R, UIKit.Red, 0.75f, 0.72f, 22f, 8f);
                    break;
                case CustType.Fireman:
                    UIKit.Shape(body, R, UIKit.Hex("#FFEB3B"), 0.5f, 0.35f, 150f, 12f);
                    UIKit.Shape(body, R, UIKit.Hex("#FFEB3B"), 0.5f, 0.6f, 150f, 12f);
                    break;
                case CustType.Rocker:
                    UIKit.Shape(body, C, Color.white, 0.5f, 0.55f, 40f, 36f);
                    UIKit.Shape(body, C, UIKit.Dark, 0.42f, 0.58f, 10f, 10f);
                    UIKit.Shape(body, C, UIKit.Dark, 0.58f, 0.58f, 10f, 10f);
                    for (int i = 0; i < 3; i++) UIKit.Shape(body, C, UIKit.Hex("#B0BEC5"), 0.2f, 0.4f + i * 0.14f, 8f, 8f);
                    break;
                case CustType.Gamer:
                    UIKit.Shape(body, R, UIKit.Hex("#00E5FF"), 0.5f, 0.55f, 60f, 10f);
                    UIKit.Shape(body, R, UIKit.Hex("#00E5FF"), 0.5f, 0.55f, 10f, 40f);
                    break;
                case CustType.Hipster:
                    for (int i = 0; i < 3; i++)
                    {
                        UIKit.Shape(body, R, UIKit.Alpha(Color.black, 0.25f), 0.2f + i * 0.3f, 0.5f, 10f, 170f);
                        UIKit.Shape(body, R, UIKit.Alpha(Color.black, 0.25f), 0.5f, 0.25f + i * 0.25f, 150f, 10f);
                    }
                    break;
                case CustType.Taxi:
                    for (int i = 0; i < 6; i++) UIKit.Shape(body, R, UIKit.Dark, 0.12f + i * 0.155f, 0.82f, 18f, 18f);
                    break;
                case CustType.Grandpa:
                    for (int i = 0; i < 3; i++) UIKit.Shape(body, C, UIKit.Hex("#D7CCC8"), 0.5f, 0.3f + i * 0.2f, 12f, 12f);
                    break;
                case CustType.Fisher:
                    UIKit.Shape(body, R, UIKit.Hex("#33691E"), 0.3f, 0.45f, 36f, 30f);
                    UIKit.Shape(body, R, UIKit.Hex("#33691E"), 0.7f, 0.45f, 36f, 30f);
                    break;
                case CustType.Surfer:
                    UIKit.Shape(body, C, UIKit.Hex("#FF80AB"), 0.35f, 0.6f, 30f, 30f);
                    UIKit.Shape(body, C, UIKit.Hex("#FFEB3B"), 0.35f, 0.6f, 10f, 10f);
                    UIKit.Shape(body, C, UIKit.Hex("#FF80AB"), 0.68f, 0.35f, 26f, 26f);
                    break;
                case CustType.Pilot:
                    UIKit.Shape(body, R, UIKit.Gold, 0.15f, 0.9f, 40f, 10f);
                    UIKit.Shape(body, R, UIKit.Gold, 0.85f, 0.9f, 40f, 10f);
                    UIKit.Shape(body, R, UIKit.Gold, 0.3f, 0.72f, 30f, 8f);
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.8f, 36f, 50f);
                    break;
                case CustType.Stewardess:
                    // белый воротник-стойка, аккуратный синий бантик и золотой значок-крылышки
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.9f, 48f, 22f);
                    UIKit.Shape(body, R, UIKit.Hex("#1565C0"), 0.43f, 0.86f, 20f, 12f, 20f);
                    UIKit.Shape(body, R, UIKit.Hex("#1565C0"), 0.57f, 0.86f, 20f, 12f, -20f);
                    UIKit.Shape(body, C, UIKit.Hex("#0D47A1"), 0.5f, 0.86f, 10f, 10f);
                    UIKit.Shape(body, R, UIKit.Gold, 0.28f, 0.66f, 24f, 5f);
                    break;
                case CustType.Fan:
                    UIKit.Shape(body, C, Color.white, 0.5f, 0.55f, 60f, 60f);
                    UIKit.FitLabel(body, Random.Range(7, 99).ToString(), 30, UIKit.Dark, 0.3f, 0.35f, 0.7f, 0.75f);
                    break;
                case CustType.Clown:
                    string[] dots = { "#E53935", "#1E88E5", "#43A047", "#8E24AA" };
                    for (int i = 0; i < 6; i++) UIKit.Shape(body, C, UIKit.Hex(dots[i % 4]), 0.2f + (i % 3) * 0.3f, 0.3f + (i / 3) * 0.35f, 24f, 24f);
                    UIKit.Shape(body, C, UIKit.Hex("#E53935"), 0.4f, 0.92f, 40f, 30f);
                    UIKit.Shape(body, C, UIKit.Hex("#E53935"), 0.6f, 0.92f, 40f, 30f);
                    break;
                case CustType.Musician:
                    UIKit.Shape(body, R, UIKit.Hex("#FFB300"), 0.5f, 0.6f, 150f, 8f, 30f);
                    break;
                case CustType.Glutton:
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.72f, 90f, 70f);
                    UIKit.Shape(body, C, UIKit.Hex("#FFCC80"), 0.5f, 0.3f, 30f, 26f);
                    break;
                case CustType.Inspector:
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.82f, 40f, 44f);
                    UIKit.Shape(body, R, UIKit.Hex("#263238"), 0.5f, 0.66f, 14f, 70f);
                    UIKit.Shape(body, C, UIKit.Gold, 0.75f, 0.7f, 20f, 20f);
                    break;
                case CustType.Haggler:
                    UIKit.Shape(body, R, UIKit.Hex("#5D4037"), 0.3f, 0.5f, 40f, 170f);
                    UIKit.Shape(body, R, UIKit.Hex("#5D4037"), 0.7f, 0.5f, 40f, 170f);
                    UIKit.Shape(body, C, UIKit.Gold, 0.5f, 0.82f, 50f, 36f);
                    UIKit.Shape(body, C, UIKit.Hex("#FF8F00"), 0.5f, 0.86f, 40f, 30f);
                    break;
                case CustType.Vegan:
                    UIKit.Shape(body, C, UIKit.Hex("#1B5E20"), 0.5f, 0.55f, 50f, 50f);
                    UIKit.Shape(body, R, UIKit.Hex("#A5D6A7"), 0.5f, 0.57f, 16f, 34f, 30f);
                    break;
                case CustType.Sheikh:
                    // морской пиджак: белая рубашка, золотые пуговицы
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.78f, 40f, 60f);
                    for (int k = 0; k < 3; k++) UIKit.Shape(body, C, UIKit.Gold, 0.36f, 0.62f - k * 0.14f, 12f, 12f);
                    for (int k = 0; k < 3; k++) UIKit.Shape(body, C, UIKit.Gold, 0.64f, 0.62f - k * 0.14f, 12f, 12f);
                    break;
                case CustType.Deputy:
                case CustType.Minister:
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.8f, 44f, 56f);
                    UIKit.Shape(body, R, UIKit.Hex(type == CustType.Deputy ? "#C62828" : "#FFC928"), 0.5f, 0.66f, 16f, 76f);
                    // платочек в нагрудном кармане
                    UIKit.Shape(body, R, UIKit.Hex(type == CustType.Deputy ? "#FFD54F" : "#E53935"), 0.26f, 0.74f, 18f, 12f, 12f);
                    if (type == CustType.Minister)
                    {
                        UIKit.Shape(body, C, UIKit.Gold, 0.76f, 0.7f, 24f, 24f);
                        UIKit.Shape(body, R, UIKit.Hex("#C62828"), 0.76f, 0.82f, 14f, 18f);
                    }
                    break;
                case CustType.Millionaire:
                    UIKit.Shape(body, R, Color.white, 0.5f, 0.78f, 40f, 60f);
                    UIKit.Shape(body, R, UIKit.Hex("#111111"), 0.43f, 0.88f, 20f, 14f, 20f);
                    UIKit.Shape(body, R, UIKit.Hex("#111111"), 0.57f, 0.88f, 20f, 14f, -20f);
                    UIKit.Shape(body, C, UIKit.Gold, 0.5f, 0.88f, 10f, 10f);
                    UIKit.Shape(body, R, UIKit.Gold, 0.72f, 0.55f, 26f, 6f, 20f);
                    break;
                case CustType.Socialite:
                    for (int i = 0; i < 7; i++) UIKit.Shape(body, C, Color.white, 0.25f + i * 0.083f, 0.86f - Mathf.Abs(i - 3) * 0.035f, 12f, 12f);
                    UIKit.Shape(body, R, UIKit.Hex("#F48FB1"), 0.5f, 0.4f, 150f, 14f);
                    break;
            }
        }

        /// <summary>Головной убор / причёска. true — нарисовано, обычные волосы не нужны.</summary>
        bool NewHead(Transform h, string hair)
        {
            var R = UIKit.RoundedSmall;
            var Rr = UIKit.Rounded;
            var C = UIKit.Circle;
            var hc = UIKit.Hex(hair);
            switch (type)
            {
                case CustType.Police:
                    UIKit.Shape(h, Rr, UIKit.Hex("#1A237E"), 0.5f, 0.92f, 138f, 50f);
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.5f, 0.8f, 142f, 14f);
                    UIKit.Shape(h, C, UIKit.Gold, 0.5f, 0.93f, 20f, 20f);
                    return true;
                case CustType.Fireman:
                    UIKit.Shape(h, C, UIKit.Hex("#D32F2F"), 0.5f, 0.9f, 144f, 84f);
                    UIKit.Shape(h, R, UIKit.Hex("#B71C1C"), 0.5f, 0.8f, 170f, 18f);
                    UIKit.Shape(h, R, UIKit.Hex("#B71C1C"), 0.5f, 1.0f, 18f, 40f);
                    UIKit.Shape(h, C, UIKit.Gold, 0.5f, 0.9f, 26f, 26f);
                    return true;
                case CustType.Rocker:
                    for (int i = 0; i < 5; i++) UIKit.Shape(h, R, hc, 0.5f, 0.86f + i * 0.045f, 26f - i * 3f, 26f, 0f);
                    UIKit.Shape(h, R, hc, 0.5f, 1.08f, 18f, 30f);
                    return true;
                case CustType.Gamer:
                    UIKit.Shape(h, Rr, hc, 0.5f, 0.88f, 124f, 48f);
                    UIKit.Shape(h, R, UIKit.Hex("#212121"), 0.5f, 0.98f, 146f, 16f);
                    UIKit.Shape(h, C, UIKit.Hex("#212121"), 0.02f, 0.55f, 34f, 46f);
                    UIKit.Shape(h, C, UIKit.Hex("#212121"), 0.98f, 0.55f, 34f, 46f);
                    UIKit.Shape(h, C, UIKit.Hex("#00E5FF"), 0.02f, 0.55f, 16f, 24f);
                    UIKit.Shape(h, C, UIKit.Hex("#00E5FF"), 0.98f, 0.55f, 16f, 24f);
                    return true;
                case CustType.Hipster:
                    UIKit.Shape(h, C, UIKit.Hex("#FFB300"), 0.5f, 0.92f, 128f, 74f);
                    UIKit.Shape(h, R, UIKit.Hex("#FF8F00"), 0.5f, 0.82f, 136f, 22f);
                    UIKit.Shape(h, C, UIKit.Hex("#FFB300"), 0.5f, 1.14f, 24f, 24f);
                    return true;
                case CustType.Taxi:
                case CustType.Haggler:
                    UIKit.Shape(h, Rr, UIKit.Hex(type == CustType.Taxi ? "#424242" : "#6D4C41"), 0.47f, 0.9f, 134f, 42f);
                    UIKit.Shape(h, R, UIKit.Hex(type == CustType.Taxi ? "#303030" : "#4E342E"), 0.72f, 0.83f, 76f, 14f);
                    return true;
                case CustType.Grandpa:
                    UIKit.Shape(h, Rr, hc, 0.08f, 0.62f, 24f, 56f);
                    UIKit.Shape(h, Rr, hc, 0.92f, 0.62f, 24f, 56f);
                    UIKit.Shape(h, C, UIKit.Alpha(Color.white, 0.25f), 0.4f, 0.86f, 34f, 18f);
                    return true;
                case CustType.Fisher:
                    UIKit.Shape(h, C, UIKit.Hex("#8D9440"), 0.5f, 0.92f, 128f, 62f);
                    UIKit.Shape(h, R, UIKit.Hex("#7C8236"), 0.5f, 0.82f, 172f, 18f);
                    UIKit.Shape(h, C, UIKit.Hex("#E53935"), 0.72f, 0.96f, 14f, 14f);
                    return true;
                case CustType.Surfer:
                    UIKit.Shape(h, Rr, hc, 0.5f, 0.88f, 130f, 50f);
                    UIKit.Shape(h, Rr, hc, 0.28f, 0.78f, 60f, 32f, 20f);
                    return true;
                case CustType.Pilot:
                    UIKit.Shape(h, Rr, UIKit.Hex("#0D47A1"), 0.5f, 0.94f, 142f, 46f);
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.5f, 0.82f, 146f, 14f);
                    UIKit.Shape(h, R, UIKit.Gold, 0.5f, 0.9f, 70f, 8f);
                    UIKit.Shape(h, C, UIKit.Gold, 0.5f, 0.97f, 18f, 18f);
                    return true;
                case CustType.Stewardess:
                    UIKit.Shape(h, C, hc, 0.5f, 1.04f, 52f, 52f);
                    UIKit.Shape(h, Rr, hc, 0.5f, 0.88f, 126f, 48f);
                    UIKit.Shape(h, R, UIKit.Hex("#C62828"), 0.64f, 0.98f, 58f, 24f, -10f);
                    return true;
                case CustType.Clown:
                    UIKit.Shape(h, C, UIKit.Hex("#FF5722"), 0.06f, 0.72f, 60f, 60f);
                    UIKit.Shape(h, C, UIKit.Hex("#FF9800"), 0.94f, 0.72f, 60f, 60f);
                    UIKit.Shape(h, C, UIKit.Hex("#8E24AA"), 0.5f, 1.02f, 42f, 42f);
                    UIKit.Shape(h, C, UIKit.Hex("#FFEB3B"), 0.5f, 1.16f, 14f, 14f);
                    return true;
                case CustType.Musician:
                    UIKit.Shape(h, Rr, UIKit.Hex("#3E2723"), 0.5f, 0.95f, 100f, 46f);
                    UIKit.Shape(h, R, UIKit.Hex("#FFB300"), 0.5f, 0.87f, 100f, 8f);
                    UIKit.Shape(h, R, UIKit.Hex("#3E2723"), 0.5f, 0.83f, 164f, 14f);
                    return true;
                case CustType.Sheikh:
                    // капитанская фуражка
                    UIKit.Shape(h, Rr, UIKit.Hex("#FAFAFA"), 0.5f, 0.95f, 150f, 44f);
                    UIKit.Shape(h, R, UIKit.Hex("#0D2A4F"), 0.5f, 0.85f, 150f, 16f);
                    UIKit.Shape(h, R, UIKit.Hex("#212121"), 0.5f, 0.8f, 110f, 12f);
                    UIKit.Shape(h, C, UIKit.Gold, 0.5f, 0.92f, 26f, 22f);
                    return true;
                case CustType.Millionaire:
                    // цилиндр с золотой лентой
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.5f, 0.86f, 170f, 14f);
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.5f, 1.08f, 100f, 90f);
                    UIKit.Shape(h, R, UIKit.Gold, 0.5f, 0.92f, 100f, 12f);
                    return true;
                case CustType.Socialite:
                    UIKit.Shape(h, Rr, hc, 0.5f, 0.9f, 136f, 56f);
                    UIKit.Shape(h, Rr, hc, 0.3f, 0.8f, 66f, 34f, 20f);
                    UIKit.Shape(h, C, UIKit.Hex("#F48FB1"), 0.78f, 0.95f, 30f, 30f);
                    return true;
                case CustType.Vegan:
                    UIKit.Shape(h, Rr, hc, 0.5f, 0.88f, 124f, 48f);
                    UIKit.Shape(h, C, UIKit.Hex("#F48FB1"), 0.2f, 0.9f, 26f, 26f);
                    UIKit.Shape(h, C, UIKit.Hex("#FFEB3B"), 0.2f, 0.9f, 10f, 10f);
                    return true;
            }
            return false;
        }

        /// <summary>Лицо: очки, усы, борода, нос, грим.</summary>
        void NewFace(Transform h, string hair)
        {
            var R = UIKit.RoundedSmall;
            var C = UIKit.Circle;
            switch (type)
            {
                case CustType.Rocker:
                case CustType.Sheikh:
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.36f, 0.52f, 40f, 24f);
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.64f, 0.52f, 40f, 24f);
                    UIKit.Shape(h, R, UIKit.Hex("#111111"), 0.5f, 0.55f, 30f, 6f);
                    break;
                case CustType.Hipster:
                    UIKit.Shape(h, C, UIKit.Hex(hair), 0.5f, 0.18f, 104f, 62f).transform.SetSiblingIndex(_mouth.transform.GetSiblingIndex());
                    Glasses(h, "#212121");
                    break;
                case CustType.Deputy:
                    Glasses(h, "#212121");
                    break;
                case CustType.Minister:
                    UIKit.Shape(h, R, UIKit.Hex("#9E9E9E"), 0.5f, 0.36f, 56f, 10f);
                    break;
                case CustType.Millionaire:
                    var mono = UIKit.Shape(h, C, UIKit.Gold, 0.64f, 0.52f, 40f, 40f);
                    mono.transform.SetSiblingIndex(_eyeR.GetSiblingIndex());
                    UIKit.Shape(mono.transform, C, UIKit.Alpha(Color.white, 0.55f), 0.5f, 0.5f, 32f, 32f);
                    UIKit.Shape(h, R, UIKit.Hex("#3E2723"), 0.4f, 0.36f, 34f, 10f, 10f);
                    UIKit.Shape(h, R, UIKit.Hex("#3E2723"), 0.6f, 0.36f, 34f, 10f, -10f);
                    break;
                case CustType.Socialite:
                    UIKit.Shape(h, R, UIKit.Hex("#4A148C"), 0.36f, 0.52f, 42f, 26f);
                    UIKit.Shape(h, R, UIKit.Hex("#4A148C"), 0.64f, 0.52f, 42f, 26f);
                    UIKit.Shape(h, R, UIKit.Hex("#4A148C"), 0.5f, 0.55f, 30f, 6f);
                    UIKit.Shape(h, R, UIKit.Hex("#D81B60"), 0.5f, 0.28f, 30f, 10f);
                    break;
                case CustType.Inspector:
                    Glasses(h, "#37474F");
                    UIKit.Shape(h, R, UIKit.Hex(hair), 0.5f, 0.36f, 50f, 8f);
                    break;
                case CustType.Grandpa:
                    UIKit.Shape(h, R, UIKit.Hex("#EEEEEE"), 0.4f, 0.35f, 36f, 14f, 12f);
                    UIKit.Shape(h, R, UIKit.Hex("#EEEEEE"), 0.6f, 0.35f, 36f, 14f, -12f);
                    break;
                case CustType.Haggler:
                    UIKit.Shape(h, R, UIKit.Hex(hair), 0.4f, 0.36f, 36f, 14f, 15f);
                    UIKit.Shape(h, R, UIKit.Hex(hair), 0.6f, 0.36f, 36f, 14f, -15f);
                    break;
                case CustType.Clown:
                    UIKit.Shape(h, C, UIKit.Hex("#F44336"), 0.5f, 0.42f, 30f, 30f);
                    UIKit.Shape(h, C, UIKit.Alpha(Color.white, 0.6f), 0.45f, 0.46f, 8f, 8f);
                    break;
                case CustType.Fan:
                    break;
                case CustType.Doctor:
                    UIKit.Shape(h, C, UIKit.Hex("#CFD8DC"), 0.5f, 0.97f, 34f, 34f);
                    UIKit.Shape(h, C, Color.white, 0.5f, 0.97f, 18f, 18f);
                    break;
            }
        }

        void Glasses(Transform h, string col)
        {
            var C = UIKit.Circle;
            var ringL = UIKit.Shape(h, C, UIKit.Hex(col), 0.36f, 0.52f, 38f, 38f);
            ringL.transform.SetSiblingIndex(_eyeL.GetSiblingIndex());
            UIKit.Shape(ringL.transform, C, UIKit.Alpha(Color.white, 0.55f), 0.5f, 0.5f, 30f, 30f);
            var ringR = UIKit.Shape(h, C, UIKit.Hex(col), 0.64f, 0.52f, 38f, 38f);
            ringR.transform.SetSiblingIndex(_eyeR.GetSiblingIndex());
            UIKit.Shape(ringR.transform, C, UIKit.Alpha(Color.white, 0.55f), 0.5f, 0.5f, 30f, 30f);
        }

        /// <summary>Предметы в руках и на шее.</summary>
        void NewProps()
        {
            var R = UIKit.RoundedSmall;
            var C = UIKit.Circle;
            switch (type)
            {
                case CustType.Grandpa:
                    UIKit.Shape(_char, R, UIKit.Hex("#6D4C41"), 0.9f, 0.22f, 12f, 180f);
                    UIKit.Shape(_char, R, UIKit.Hex("#6D4C41"), 0.84f, 0.46f, 40f, 12f);
                    break;
                case CustType.Surfer:
                    var board = UIKit.Shape(_char, UIKit.Rounded, UIKit.Hex("#FF7043"), 0.07f, 0.4f, 44f, 250f, 6f);
                    UIKit.Shape(board.transform, R, Color.white, 0.5f, 0.5f, 10f, 230f);
                    break;
                case CustType.Fan:
                    // полосатый шарф болельщика: ровные полосы вокруг шеи и свисающий конец с бахромой
                    UIKit.Shape(_char, R, UIKit.Hex("#E53935"), 0.5f, 0.375f, 122f, 22f);
                    for (int k = 0; k < 5; k++)
                        UIKit.Shape(_char, R, Color.white, 0.32f + k * 0.09f, 0.375f, 10f, 22f);
                    var tail = UIKit.Rect(_char, "ScarfTail");
                    UIKit.PlaceFixed(tail, 0.63f, 0.3f, 20f, 50f);
                    tail.localRotation = Quaternion.Euler(0, 0, -8f);
                    UIKit.Shape(tail, R, UIKit.Hex("#E53935"), 0.5f, 0.5f, 20f, 50f);
                    for (int k = 0; k < 2; k++) UIKit.Shape(tail, R, Color.white, 0.5f, 0.3f + k * 0.35f, 20f, 8f);
                    for (int k = 0; k < 3; k++) UIKit.Shape(tail, R, Color.white, 0.2f + k * 0.3f, -0.08f, 3f, 10f);
                    UIKit.Shape(_char, R, UIKit.Hex("#FFEB3B"), 0.9f, 0.5f, 40f, 96f);
                    UIKit.Shape(_char, R, UIKit.Hex("#FFEB3B"), 0.92f, 0.66f, 18f, 44f);
                    break;
                case CustType.Musician:
                    UIKit.Shape(_char, C, UIKit.Hex("#D84315"), 0.3f, 0.18f, 96f, 84f);
                    UIKit.Shape(_char, C, UIKit.Hex("#3E2723"), 0.3f, 0.19f, 30f, 30f);
                    UIKit.Shape(_char, R, UIKit.Hex("#5D4037"), 0.58f, 0.33f, 16f, 130f, -55f);
                    break;
                case CustType.Glutton:
                    UIKit.Shape(_char, R, UIKit.Hex("#B0BEC5"), 0.93f, 0.3f, 10f, 80f);
                    UIKit.Shape(_char, R, UIKit.Hex("#B0BEC5"), 0.93f, 0.42f, 28f, 20f);
                    break;
                case CustType.Inspector:
                    var pad = UIKit.Shape(_char, R, UIKit.Hex("#8D6E63"), 0.88f, 0.3f, 58f, 76f, 6f);
                    UIKit.Shape(pad.transform, R, Color.white, 0.5f, 0.45f, 46f, 56f);
                    for (int l = 0; l < 3; l++) UIKit.Shape(pad.transform, R, UIKit.Alpha(UIKit.Dark, 0.4f), 0.5f, 0.6f - l * 0.15f, 34f, 3f);
                    UIKit.Shape(pad.transform, R, UIKit.Hex("#B0BEC5"), 0.5f, 0.92f, 26f, 10f);
                    break;
                case CustType.Vegan:
                    UIKit.Shape(_char, R, UIKit.Hex("#FF7043"), 0.9f, 0.33f, 20f, 76f, -15f);
                    UIKit.Shape(_char, R, UIKit.Hex("#43A047"), 0.92f, 0.47f, 26f, 16f, -15f);
                    break;
                case CustType.Taxi:
                    var sign = UIKit.Shape(_char, R, UIKit.Hex("#FFD600"), 0.88f, 0.34f, 56f, 30f);
                    UIKit.FitLabel(sign.transform, "TAXI", 16, UIKit.Dark, 0.05f, 0.05f, 0.95f, 0.95f);
                    break;
                case CustType.Police:
                    UIKit.Shape(_char, R, UIKit.Hex("#FAFAFA"), 0.88f, 0.34f, 12f, 50f, -30f);
                    break;
                case CustType.Deputy:
                case CustType.Minister:
                    var brief = UIKit.Shape(_char, R, UIKit.Hex(type == CustType.Deputy ? "#4E342E" : "#212121"), 0.88f, 0.16f, 60f, 44f);
                    UIKit.Shape(brief.transform, R, UIKit.Gold, 0.5f, 0.55f, 14f, 6f);
                    UIKit.Shape(_char, R, UIKit.Hex("#3E2723"), 0.88f, 0.26f, 26f, 8f);
                    break;
                case CustType.Millionaire:
                    UIKit.Shape(_char, R, UIKit.Hex("#212121"), 0.9f, 0.25f, 10f, 170f);
                    UIKit.Shape(_char, C, UIKit.Gold, 0.9f, 0.53f, 20f, 20f);
                    break;
                case CustType.Socialite:
                    var bag = UIKit.Shape(_char, R, UIKit.Hex("#F8BBD0"), 0.88f, 0.2f, 50f, 40f);
                    UIKit.Shape(bag.transform, R, UIKit.Gold, 0.5f, 0.6f, 20f, 6f);
                    UIKit.Shape(_char, R, UIKit.Hex("#F8BBD0"), 0.88f, 0.3f, 30f, 6f);
                    break;
            }
        }

        // =====================================================================
        public void BuildOrderView()
        {
            UIKit.Clear(_content);
            if (IsBulk)
            {
                // оптовый заказ горячего лаваша
                for (int k = 0; k < 3; k++) Icons.Lavash(_content, 0.32f, 0.42f + k * 0.08f, 120f);
                var steam = UIKit.Shape(_content, UIKit.Circle, UIKit.Alpha(Color.white, 0.6f), 0.32f, 0.78f, 24f, 30f);
                steam.gameObject.AddComponent<Bobber>().amp = 5f;
                var qb = UIKit.Shape(_content, UIKit.Circle, UIKit.Red, 0.74f, 0.55f, 96f, 96f);
                UIKit.FitLabel(qb.transform, "×" + lavashQty, 40, Color.white, 0.05f, 0.15f, 0.95f, 0.85f);
                UIKit.FitLabel(_content, L.T("горячий лаваш", "hot lavash"), 22, UIKit.Dark, 0.02f, 0.02f, 0.98f, 0.2f);
                UIKit.Clear(_strip);
                UIKit.Label(_strip, L.T("Опт: жми на меня, когда лаваш готов", "Bulk: tap me when lavash is ready"), 18, UIKit.Hex("#E65100"), TextAnchor.MiddleLeft, 0.03f, 0f, 1f, 1f, true);
                return;
            }
            // слева — что кладём В ШАУРМУ, справа отдельной колонкой — что отдаём ОТДЕЛЬНО (напиток или коробка фри)
            var items = new List<int>();
            for (int i = 0; i < order.Length; i++) if (order[i] > 0) items.Add(i);
            bool side = drink != Drink.None;
            float leftW = side ? 0.66f : 1f;

            var left = UIKit.Rect(_content, "InShawarma");
            UIKit.Place(left, 0f, 0f, leftW, 1f);
            if (side)
            {
                var cap = UIKit.FitLabel(left, L.T("в шаурму", "in the wrap"), 16, UIKit.Alpha(UIKit.Dark, 0.55f), 0.02f, 0.86f, 0.98f, 1f);
                cap.fontStyle = FontStyle.Italic;
            }
            int n = items.Count;
            int cols = n <= 2 ? Mathf.Max(n, 1) : (side ? 2 : 3);
            if (side && n > 4) cols = 3;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)cols));
            float top = side ? 0.86f : 1f;
            float bw = 250f * leftW, bh = 264f * top;
            float cellW = 1f / cols, cellH = top / rows;
            float iconSize = Mathf.Min(bw * cellW, bh / rows * 0.75f) * 0.72f;
            iconSize = Mathf.Min(iconSize, 90f);
            for (int k = 0; k < n; k++)
            {
                int col = k % cols, row = k / cols;
                var cell = UIKit.Rect(left, "Cell");
                UIKit.Place(cell, col * cellW, top - (row + 1) * cellH, (col + 1) * cellW, top - row * cellH);
                var cg = cell.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = !needShawarma ? 0.35f : 1f;
                var ing = (Ing)items[k];
                Icons.Ingredient(cell, ing, 0.5f, 0.6f, iconSize);
                UIKit.FitLabel(cell, GameData.Get(ing).Short, 18, UIKit.Dark, 0f, 0.02f, 1f, 0.28f);
                int cnt = order[(int)ing];
                if (cnt > 1)
                {
                    var badge = UIKit.Shape(cell, UIKit.Circle, UIKit.Red, 0.82f, 0.82f, 36f, 36f);
                    UIKit.FitLabel(badge.transform, "×" + cnt, 22, Color.white, 0.05f, 0.1f, 0.95f, 0.9f);
                    Juice.Punch(badge.transform, 0.3f, 0.4f);
                }
                if (!needShawarma) Check(cell, 0.5f, 0.55f, iconSize * 0.7f);
            }

            if (side)
            {
                var right = UIKit.Panel(_content, "Side", drink == Drink.Fries ? UIKit.Hex("#FFF3E0") : UIKit.Hex("#E3F2FD"), 0.68f, 0.04f, 0.99f, 0.98f);
                UIKit.AddOutline(right, UIKit.Alpha(UIKit.Dark, 0.25f), 2f);
                var r = right.transform;
                var cap2 = UIKit.FitLabel(r, L.T("отдельно", "separate"), 16, UIKit.Alpha(UIKit.Dark, 0.6f), 0.04f, 0.84f, 0.96f, 0.99f);
                cap2.fontStyle = FontStyle.Italic;
                var cg2 = right.gameObject.AddComponent<CanvasGroup>();
                cg2.alpha = !needDrink ? 0.35f : 1f;
                Icons.DrinkIcon(r, drink, 0.5f, 0.52f, 66f);
                UIKit.FitLabel(r, drink == Drink.Fries ? L.T("Фри", "Fries") : GameData.Get(drink).Name, 18, UIKit.Dark, 0.02f, 0.02f, 0.98f, 0.22f);
                if (!needDrink) Check(r, 0.5f, 0.52f, 46f);
            }
            BuildStrip();
        }

        /// <summary>На 1-й локации упаковки нет: не спрашиваем «здесь или с собой».</summary>
        public static bool PackingOn = true;
        /// <summary>Идёт обучение — терпение не тратится.</summary>
        public static bool Frozen;

        void BuildStrip()
        {
            UIKit.Clear(_strip);
            if (!needShawarma) return;
            if (!PackingOn && !IsCourier && !boss && type != CustType.Glutton)
            {
                UIKit.Label(_strip, L.T("Шаурма", "Shawarma") + (qty > 1 ? " ×" + qty : ""), 20, UIKit.Wood, TextAnchor.MiddleLeft, 0.05f, 0f, 1f, 1f, true);
                if (type == CustType.Critic) Icons.Star(_strip, 0.93f, 0.5f, 36f, true);
                return;
            }
            if (!modeKnown)
            {
                Icons.Question(_strip, 0.09f, 0.5f, 40f);
                UIKit.Label(_strip, L.T("Здесь или с собой? Спроси!", "Here or to go? Ask!"), 20, UIKit.Purple, TextAnchor.MiddleLeft, 0.2f, 0f, 1f, 1f, true);
            }
            else if (mode == ServeMode.Here)
            {
                Icons.Plate(_strip, 0.09f, 0.5f, 44f);
                UIKit.Label(_strip, L.T("Здесь — в обёртке", "For here — wrapped"), 20, UIKit.Blue, TextAnchor.MiddleLeft, 0.2f, 0f, 1f, 1f, true);
            }
            else
            {
                Icons.Bag(_strip, 0.09f, 0.5f, 44f);
                UIKit.Label(_strip, L.T("С собой — в пакет", "To go — in a bag"), 20, UIKit.Wood, TextAnchor.MiddleLeft, 0.2f, 0f, 1f, 1f, true);
            }
            if (type == CustType.Critic) Icons.Star(_strip, 0.93f, 0.5f, 36f, true);
            if (type == CustType.Glutton && !IsCourier && !boss)
            {
                UIKit.Clear(_strip);
                if (!modeKnown) Icons.Question(_strip, 0.09f, 0.5f, 40f);
                else if (mode == ServeMode.Here) Icons.Plate(_strip, 0.09f, 0.5f, 44f);
                else Icons.Bag(_strip, 0.09f, 0.5f, 44f);
                string m = !modeKnown ? L.T("Спроси! ", "Ask! ") : (mode == ServeMode.Here ? L.T("Здесь. ", "Here. ") : L.T("С собой. ", "To go. "));
                UIKit.Label(_strip, m + L.T("Две одинаковые!", "Two identical!"), 18, UIKit.Purple, TextAnchor.MiddleLeft, 0.2f, 0f, 0.8f, 1f, true);
                var gb = UIKit.Shape(_strip, UIKit.Circle, UIKit.Red, 0.9f, 0.5f, 44f, 44f);
                UIKit.FitLabel(gb.transform, "×" + qty, 24, Color.white, 0f, 0.1f, 1f, 0.9f);
            }
            if (IsCourier || boss)
            {
                UIKit.Clear(_strip);
                Icons.Bag(_strip, 0.09f, 0.5f, 44f);
                UIKit.Label(_strip, L.T("Оптом с собой: ещё ", "Bulk to go: ") + qty + L.T(" шт.", " left"), 20, UIKit.Wood, TextAnchor.MiddleLeft, 0.2f, 0f, 0.8f, 1f, true);
                var qb = UIKit.Shape(_strip, UIKit.Circle, UIKit.Red, 0.9f, 0.5f, 44f, 44f);
                UIKit.FitLabel(qb.transform, "×" + qty, 24, Color.white, 0f, 0.1f, 1f, 0.9f);
            }
        }

        /// <summary>Клиента спросили: здесь или с собой.</summary>
        public void Reveal()
        {
            modeKnown = true;
            BuildStrip();
            Say(Meta.ModeLine(mode), 2f);
            Juice.Punch(_bubble, 0.12f, 0.25f);
        }

        public void Say(string text, float dur)
        {
            _say.text = text;
            _sayT = dur;
            _sayBox.gameObject.SetActive(true);
            Juice.Punch(_sayBox, 0.2f, 0.25f);
        }

        static void Check(Transform parent, float ax, float ay, float size)
        {
            var c = UIKit.Shape(parent, UIKit.Circle, UIKit.Green, ax, ay, size, size);
            UIKit.Shape(c.transform, UIKit.RoundedSmall, Color.white, 0.4f, 0.42f, size * 0.12f, size * 0.36f, 45f);
            UIKit.Shape(c.transform, UIKit.RoundedSmall, Color.white, 0.58f, 0.5f, size * 0.12f, size * 0.6f, -40f);
        }

        // =====================================================================
        public void Tick(float dt)
        {
            if (_appear < 1f)
            {
                _appear = Mathf.Min(1f, _appear + dt * 3f);
                float e = 1f - (1f - _appear) * (1f - _appear);
                root.anchoredPosition = new Vector2(Mathf.Lerp(-220f, 0f, e), 0f);
                _cg.alpha = e;
            }

            // моргание
            _blinkT -= dt;
            if (_blinkT <= 0f)
            {
                _blinkHold = 0.12f;
                _blinkT = Random.Range(2.5f, 5f);
            }
            float eyeY = 1f;
            if (_blinkHold > 0f) { _blinkHold -= dt; eyeY = 0.15f; }
            _eyeL.localScale = new Vector3(1f, eyeY, 1f);
            _eyeR.localScale = new Vector3(1f, eyeY, 1f);

            if (_aura != null)
            {
                float p = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
                _aura.rectTransform.localScale = new Vector3(p, p, 1f);
            }

            if (_sayT > 0f)
            {
                _sayT -= dt;
                if (_sayT <= 0f) _sayBox.gameObject.SetActive(false);
            }

            if (leaving)
            {
                leaveT += dt;
                float dir = _happyLeave ? 1f : -1f;
                root.anchoredPosition += new Vector2(dir * 380f * dt * Mathf.Clamp01(leaveT * 2f - 0.6f), 0f);
                _cg.alpha = Mathf.Clamp01(1.6f - leaveT * 1.3f);
                return;
            }

            if (!Frozen) patience -= dt;
            float f = PatienceFrac;
            UIKit.SetFill(_barFill, f);
            _barImg.color = f > 0.5f ? UIKit.Green : (f > 0.25f ? UIKit.Orange : UIKit.Red);
            SetMood(f);
        }

        void SetMood(float f)
        {
            var mrt = _mouth.rectTransform;
            if (f > 0.6f)
            {
                mrt.sizeDelta = new Vector2(46f, 16f);
                _browL.rectTransform.localRotation = Quaternion.Euler(0, 0, 8f);
                _browR.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);
                _head.color = _skin;
            }
            else if (f > 0.3f)
            {
                mrt.sizeDelta = new Vector2(34f, 8f);
                _browL.rectTransform.localRotation = Quaternion.identity;
                _browR.rectTransform.localRotation = Quaternion.identity;
                _head.color = _skin;
            }
            else
            {
                mrt.sizeDelta = new Vector2(26f, 8f);
                _browL.rectTransform.localRotation = Quaternion.Euler(0, 0, -22f);
                _browR.rectTransform.localRotation = Quaternion.Euler(0, 0, 22f);
                _head.color = Color.Lerp(_skin, UIKit.Hex("#E57373"), (0.3f - f) / 0.3f * 0.6f);
            }
        }

        // =====================================================================
        public bool WantsDrink(Drink d) { return needDrink && d == drink; }

        public bool WantsShawarma(int[] item)
        {
            return needShawarma && GameData.Same(order, item);
        }

        /// <summary>Отдали оптовику n горячих лавашей.</summary>
        public void GiveLavash(int n)
        {
            lavashQty = Mathf.Max(0, lavashQty - n);
            BuildOrderView();
            Juice.Punch(_bubble, 0.15f, 0.25f);
        }

        public void Deliver(bool isDrink)
        {
            if (isDrink) needDrink = false;
            else
            {
                qty--;
                needShawarma = qty > 0;
            }
            BuildOrderView();
            Juice.Punch(_bubble, 0.12f, 0.25f);
        }

        public void ChangeOrder(int[] newOrder)
        {
            order = newOrder;
            changedMind = true;
            BuildOrderView();
            Juice.Punch(_bubble, 0.2f, 0.35f);
        }

        public void LosePatience(float fraction)
        {
            patience -= maxPatience * fraction;
            Juice.Shake(_char, 10f, 0.3f);
        }

        public void Leave(bool happy, string text)
        {
            leaving = true;
            leaveT = 0f;
            _happyLeave = happy;
            _bubble.gameObject.SetActive(false);
            _barFill.parent.gameObject.SetActive(false);
            _reaction.text = text;
            _reaction.color = happy ? UIKit.Green : UIKit.Red;
            Juice.Punch(_reaction.transform, 0.3f, 0.3f);
            SetMood(happy ? 1f : 0f);
            if (!happy) Juice.Shake(_char, 12f, 0.35f);
        }

        public void Destroy()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }
    }
}
