using System;
using System.Collections.Generic;
using tools.assert;
using ui.intents;
using ui.viewModels;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ui
{
    // HandView owns the visual CardView collection and its fan layout.
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private RectTransform playDropArea;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private float preferredSpacing = 140f;
        [SerializeField] private float maximumHandWidth = 900f;
        [SerializeField] private float arcHeight = 80f;
        [SerializeField] private float maximumRotation = 12f;

        private readonly List<CardView> _cardViews = new List<CardView>();
        private CardView _draggingCard;
        private int _dragPlaceholderIndex = -1;

        public event Action<PlayCardIntent> PlayRequested;
        public event Action<ViewCardDetailIntent> DetailRequested;

        private void Awake()
        {
            if (cardRoot == null)
            {
                cardRoot = transform as RectTransform;
            }

            MyAssert.Assert(cardRoot != null, "HandView requires a cardRoot RectTransform.");
            MyAssert.Assert(playDropArea != null, "HandView requires a playDropArea RectTransform.");
            MyAssert.Assert(cardPrefab != null, "HandView requires a CardView prefab.");
        }

        public void Render(HandViewModel model)
        {
            MyAssert.Assert(model != null, "HandView requires a HandViewModel.");
            if (model == null)
            {
                return;
            }

            Clear();
            for (int i = 0; i < model.Cards.Count; i++)
            {
                CardView cardView = Instantiate(cardPrefab, cardRoot, false);
                cardView.Render(model.Cards[i]);
                cardView.DragStarted += HandleDragStarted;
                cardView.DragEnded += HandleDragEnded;
                cardView.Clicked += HandleCardClicked;
                _cardViews.Add(cardView);
            }

            LayoutCards(false);

            // TODO: Reconcile by CardInstanceId instead of rebuilding every CardView on refresh.
            // TODO: Add a CardView pool after profiling confirms Instantiate/Destroy causes frame spikes.
            // TODO: Play draw, discard, and hand-limit animations from explicit presentation events.
        }

        public void Clear()
        {
            _draggingCard = null;
            _dragPlaceholderIndex = -1;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView cardView = _cardViews[i];
                if (cardView == null)
                {
                    continue;
                }

                cardView.DragStarted -= HandleDragStarted;
                cardView.DragEnded -= HandleDragEnded;
                cardView.Clicked -= HandleCardClicked;
                cardView.gameObject.SetActive(false);
                Destroy(cardView.gameObject);
            }
            _cardViews.Clear();
        }

        private void HandleDragStarted(CardView cardView, PointerEventData eventData)
        {
            MyAssert.Assert(_draggingCard == null, "HandView received overlapping card drags.");
            _draggingCard = cardView;
            _dragPlaceholderIndex = _cardViews.IndexOf(cardView);
            cardView.transform.SetAsLastSibling();
        }

        private void HandleCardClicked(CardView cardView)
        {
            DetailRequested?.Invoke(new ViewCardDetailIntent(cardView.CardId));
        }

        private void HandleDragEnded(CardView cardView, PointerEventData eventData)
        {
            MyAssert.Assert(cardView == _draggingCard, "HandView received drag end from a different CardView.");
            bool droppedInPlayArea = RectTransformUtility.RectangleContainsScreenPoint(
                playDropArea, eventData.position, eventData.pressEventCamera);

            _draggingCard = null;
            if (_dragPlaceholderIndex >= 0)
            {
                cardView.transform.SetSiblingIndex(_dragPlaceholderIndex);
            }
            RestoreSiblingOrder();
            LayoutCards(true);

            if (droppedInPlayArea)
            {
                PlayRequested?.Invoke(new PlayCardIntent(cardView.CardId));
            }

            _dragPlaceholderIndex = -1;
            // TODO: Keep a pending-play visual until the command result is available.
            // TODO: Add target selection instead of always using the presenter's default target.
        }

        private void LayoutCards(bool animated)
        {
            int count = _cardViews.Count;
            float totalWidth = Mathf.Min(preferredSpacing * Mathf.Max(0, count - 1), maximumHandWidth);
            for (int i = 0; i < count; i++)
            {
                CardView cardView = _cardViews[i];
                if (cardView == _draggingCard)
                {
                    continue;
                }

                float normalized = count <= 1 ? 0f : (float)i / (count - 1) * 2f - 1f;
                Vector2 position = new Vector2(normalized * totalWidth * 0.5f,
                    -arcHeight * normalized * normalized);
                float rotation = -maximumRotation * normalized;
                if (animated)
                {
                    cardView.SetHandPosition(position, rotation);
                }
                else
                {
                    cardView.SetImmediatePosition(position, rotation);
                }
            }
        }

        private void RestoreSiblingOrder()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].transform.SetSiblingIndex(i);
            }
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
