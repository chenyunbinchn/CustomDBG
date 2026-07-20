using System;
using cards.definition;
using cards.instance;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ui
{
    public class CardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, 
        IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image artworkImage;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;
        
        private Canvas _canvas;

        public event Action<CardView> OnDragEvent;
        public event Action<CardView> EndDragEvent;
        
        private bool _isDragging;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }
        
        public void Bind(CardInstance instance, CardDefinition definition)
        {

        }

        public void SetHandPosition(Vector2 position, float rotation)
        {
            root.DOAnchorPos(position, 0.25f);
            root.DORotate(new Vector3(0f, 0f, rotation), 0.25f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOAnchorPosY(30f, 0.12f);
            visualRoot.DOScale(1.12f, 0.12f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOAnchorPosY(0f, 0.12f);
            visualRoot.DOScale(1f, 0.12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;

            root.DOKill();
            visualRoot.DOKill();

            visualRoot.DOScale(1.08f, 0.1f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnDragEvent?.Invoke(this);
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDragEvent?.Invoke(this);
            _isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            visualRoot.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }
    }
}