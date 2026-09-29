using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ValourGames.Shawarma
{
    /// <summary>Кнопка чуть сжимается при нажатии.</summary>
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float _target = 1f;

        public void OnPointerDown(PointerEventData e) { _target = 0.93f; }
        public void OnPointerUp(PointerEventData e) { _target = 1f; }
        public void OnPointerExit(PointerEventData e) { _target = 1f; }

        void Update()
        {
            float s = transform.localScale.x;
            if (Mathf.Abs(s - _target) < 0.001f) return;
            s = Mathf.MoveTowards(s, _target, Time.unscaledDeltaTime * 2.5f);
            transform.localScale = new Vector3(s, s, 1f);
        }

        void OnDisable() { _target = 1f; transform.localScale = Vector3.one; }
    }

    /// <summary>
    /// Зона для свайпа/удержания. Нужна картинка (Image) на этом же объекте, чтобы ловить касания.
    /// onDrag получает пройденное расстояние в единицах канваса и позицию пальца.
    /// </summary>
    public class DragZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<float, Vector2> onDrag;
        public Action onDown;
        public Action onUp;
        public bool Held { get; private set; }

        Canvas _canvas;

        void Awake() { _canvas = GetComponentInParent<Canvas>(); }

        float Scale { get { return _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f; } }

        public void OnPointerDown(PointerEventData e)
        {
            Held = true;
            if (onDown != null) onDown();
        }

        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { if (!e.dragging) Release(); }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (onDrag != null) onDrag(e.delta.magnitude / Scale, e.position);
        }

        public void OnEndDrag(PointerEventData e) { Release(); }

        void Release()
        {
            if (!Held) return;
            Held = false;
            if (onUp != null) onUp();
        }

        void OnDisable() { Held = false; }
    }

    /// <summary>
    /// Перетаскиваемый предмет: пока тянешь — за пальцем летит «призрак» (makeGhost),
    /// при отпускании вызывается onDrop(позиция). Короткое касание без перетаскивания — onTap.
    /// canDrag — можно ли сейчас начать тянуть. Нужна картинка (Image) на этом же объекте.
    /// </summary>
    public class DragSource : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Func<RectTransform> makeGhost;
        public Func<bool> canDrag;
        public Action<Vector2> onDrop;
        public Action onTap;

        public bool Dragging { get; private set; }
        public Vector2 Pos { get; private set; }
        public RectTransform Ghost { get; private set; }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || Dragging) return;
            if (onTap != null) onTap();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (canDrag != null && !canDrag()) { Dragging = false; return; }
            Dragging = true;
            Pos = e.position;
            Ghost = makeGhost != null ? makeGhost() : null;
            if (Ghost != null)
            {
                var cg = Ghost.GetComponent<CanvasGroup>();
                if (cg == null) cg = Ghost.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                Ghost.SetAsLastSibling();
                Ghost.position = new Vector3(e.position.x, e.position.y, 0f);
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Dragging) return;
            Pos = e.position;
            if (Ghost != null) Ghost.position = new Vector3(e.position.x, e.position.y, 0f);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!Dragging) return;
            Dragging = false;
            Pos = e.position;
            if (Ghost != null) Destroy(Ghost.gameObject);
            Ghost = null;
            if (onDrop != null) onDrop(e.position);
        }

        void OnDisable()
        {
            Dragging = false;
            if (Ghost != null) Destroy(Ghost.gameObject);
            Ghost = null;
        }

        /// <summary>Палец над этим прямоугольником?</summary>
        public static bool Over(RectTransform rt, Vector2 screenPos)
        {
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
        }
    }
}
