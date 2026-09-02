using System.Collections.Generic;
using TMPro;
using tools.assert;
using ui.core;
using ui.viewModels;
using UnityEngine;
using UnityEngine.UI;

namespace ui
{
    // CardPileDialog shows a read-only snapshot of one pile.
    public sealed class CardPileDialog : UIView
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private int columns = 5;
        [SerializeField] private Vector2 spacing = new Vector2(150f, 210f);

        private readonly List<CardView> _cardViews = new List<CardView>();

        private void Awake()
        {
            MyAssert.Assert(titleText != null, "CardPileDialog requires titleText.");
            MyAssert.Assert(cardRoot != null, "CardPileDialog requires cardRoot.");
            MyAssert.Assert(cardPrefab != null, "CardPileDialog requires a CardView prefab.");
            MyAssert.Assert(closeButton != null, "CardPileDialog requires a closeButton.");
            MyAssert.Assert(columns > 0, "CardPileDialog columns must be greater than zero.");
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        protected override void Render(IUIViewModel viewModel)
        {
            CardPileViewModel model = viewModel as CardPileViewModel;
            MyAssert.Assert(model != null, "CardPileDialog requires CardPileViewModel.");
            if (model == null)
            {
                return;
            }

            ClearCards();
            titleText.text = model.Title;
            for (int i = 0; i < model.Cards.Count; i++)
            {
                CardView cardView = Instantiate(cardPrefab, cardRoot, false);
                cardView.Render(model.Cards[i]);
                int row = i / columns;
                int column = i % columns;
                float centeredColumn = column - (columns - 1) * 0.5f;
                cardView.SetImmediatePosition(new Vector2(centeredColumn * spacing.x, -row * spacing.y), 0f);
                _cardViews.Add(cardView);
            }

            // TODO: Add a shared CardView pool if pile dialogs become large or open frequently.
            // TODO: Add scrolling after the first real pile exceeds the visible grid capacity.
        }

        protected override void OnUnbind()
        {
            ClearCards();
        }

        private void ClearCards()
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

        private void HandleCloseClicked()
        {
            UIManager.Instance.PopModal();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }
    }
}
