using System.Collections.Generic;
using tools.assert;
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

        private readonly List<CardView> _cardViews = new List<CardView>();

        private void Awake()
        {
            MyAssert.Assert(scrollRect != null, "CardPileListView requires a ScrollRect.");
            MyAssert.Assert(contentRoot != null, "CardPileListView requires a contentRoot.");
            MyAssert.Assert(cardPrefab != null, "CardPileListView requires a CardView prefab.");
            MyAssert.Assert(scrollRect == null || scrollRect.content == contentRoot,
                "CardPileListView requires ScrollRect.content to reference contentRoot.");
            MyAssert.Assert(contentRoot == null || contentRoot.GetComponent<GridLayoutGroup>() != null,
                "CardPileListView contentRoot requires GridLayoutGroup.");
            MyAssert.Assert(contentRoot == null || contentRoot.GetComponent<ContentSizeFitter>() != null,
                "CardPileListView contentRoot requires ContentSizeFitter.");
            MyAssert.Assert(cardPrefab == null || cardPrefab.transform is RectTransform,
                "CardPileListView requires a CardView prefab with a RectTransform root.");
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

                cardView.gameObject.SetActive(false);
                Destroy(cardView.gameObject);
            }
            _cardViews.Clear();
        }
    }
}
