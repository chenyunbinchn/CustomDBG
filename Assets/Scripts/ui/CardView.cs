using System;
using cards.instance;
using DG.Tweening;
using TMPro;
using tools.assert;
using ui.viewModels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ui
{
    // CardView renders one card and reports pointer gestures to HandView.
    public sealed class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;

        private CardViewModel _model;
        private Canvas _canvas;
        private bool _isDragging;

        public event Action<CardView, PointerEventData> DragStarted;
        public event Action<CardView, PointerEventData> DragEnded;

        public CardInstanceId CardId => _model == null ? default : _model.CardId;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (root == null)
            {
                // Legacy prefabs may still keep their layout RectTransform on visualRoot.
                root = visualRoot;
            }
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            MyAssert.Assert(root != null, "CardView requires a root RectTransform.");
            MyAssert.Assert(visualRoot != null, "CardView requires a visualRoot RectTransform.");
            MyAssert.Assert(canvasGroup != null, "CardView requires a CanvasGroup.");
            MyAssert.Assert(_canvas != null, "CardView must be placed under a Canvas.");
            MyAssert.Assert(nameText != null, "CardView requires nameText.");
            MyAssert.Assert(costText != null, "CardView requires costText.");
            MyAssert.Assert(descriptionText != null, "CardView requires descriptionText.");
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void Render(CardViewModel model)
        {
            MyAssert.Assert(model != null, "CardView requires a CardViewModel.");
            if (model == null)
            {
                return;
            }

            _model = model;
            nameText.text = model.Name;
            costText.text = model.CostText;
            descriptionText.text = model.Description;
            if (artworkImage != null)
            {
                artworkImage.sprite = model.Artwork;
                artworkImage.enabled = model.Artwork != null;
            }

            canvasGroup.alpha = model.IsPlayable ? 1f : 0.55f;
            canvasGroup.interactable = model.IsPlayable;
            canvasGroup.blocksRaycasts = true;
            visualRoot.localScale = Vector3.one;
        }

        public void SetHandPosition(Vector2 position, float rotation)
        {
            if (_isDragging)
            {
                return;
            }

            root.DOKill();
            root.DOAnchorPos(position, 0.2f);
            root.DORotate(new Vector3(0f, 0f, rotation), 0.2f);
        }

        public void SetImmediatePosition(Vector2 position, float rotation)
        {
            root.DOKill();
            root.anchoredPosition = position;
            root.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOScale(1.12f, 0.12f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOScale(1f, 0.12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_model == null || !_model.IsPlayable)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOScale(1.08f, 0.1f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return;
            }

            visualRoot.DOKill();
            visualRoot.DOScale(1f, 0.1f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_model == null || !_model.IsPlayable)
            {
                return;
            }

            _isDragging = true;
            root.DOKill();
            visualRoot.DOKill();
            transform.SetAsLastSibling();
            DragStarted?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            root.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            visualRoot.localScale = Vector3.one;
            DragEnded?.Invoke(this, eventData);
        }

        private void OnDisable()
        {
            root?.DOKill();
            visualRoot?.DOKill();
            _isDragging = false;
        }
    }
}
