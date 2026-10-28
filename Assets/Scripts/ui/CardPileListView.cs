using System;
using System.Collections.Generic;
using tools.assert;
using ui.intents;
using ui.viewModels;
using UnityEngine;
using UnityEngine.UI;

namespace ui
{
    // CardPileListView owns the read-only CardView collection inside a scrolling grid.
    public sealed class CardPileListView : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private int columnCount = 4;
        [SerializeField] private Vector2 cellSize = new Vector2(400f, 600f);
        [SerializeField] private Vector2 spacing = new Vector2(20f, 20f);

        private readonly List<CardView> _cardViews = new List<CardView>();

        public event Action<ViewCardDetailIntent> DetailRequested;

        private void Awake()
        {
            MyAssert.Assert(scrollRect != null, "CardPileListView requires a ScrollRect.");
            if (scrollRect != null && scrollRect.content != null)
            {
                // ScrollRect.content is the canonical list root and corrects legacy prefab wiring.
                contentRoot = scrollRect.content;
            }

            MyAssert.Assert(contentRoot != null, "CardPileListView requires ScrollRect.content.");
            MyAssert.Assert(cardPrefab != null, "CardPileListView requires a CardView prefab.");
            MyAssert.Assert(cardPrefab == null || cardPrefab.transform is RectTransform,
                "CardPileListView requires a CardView prefab with a RectTransform root.");

            ConfigureLayout();
        }

        public void Render(IReadOnlyList<CardViewModel> cards)
        {
            MyAssert.Assert(cards != null, "CardPileListView requires a card collection.");
            if (cards == null)
            {
                return;
            }

            Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                CardView cardView = Instantiate(cardPrefab, contentRoot, false);
                cardView.Render(cards[i]);
                cardView.Clicked += HandleCardClicked;
                _cardViews.Add(cardView);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;

            // TODO: Reconcile CardViews by CardInstanceId instead of rebuilding the whole list.
            // TODO: Add a CardView pool after profiling large or frequently opened piles.
            // TODO: Add list virtualization if real pile sizes make pooled off-screen views worthwhile.
        }

        public void Clear()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView cardView = _cardViews[i];
                if (cardView == null)
                {
                    continue;
                }

                cardView.Clicked -= HandleCardClicked;
                cardView.gameObject.SetActive(false);
                Destroy(cardView.gameObject);
            }
            _cardViews.Clear();
        }

        private void ConfigureLayout()
        {
            if (scrollRect == null || contentRoot == null)
            {
                return;
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
            }
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columnCount);

            ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void HandleCardClicked(CardView cardView)
        {
            DetailRequested?.Invoke(new ViewCardDetailIntent(cardView.CardId));
        }
    }
}
