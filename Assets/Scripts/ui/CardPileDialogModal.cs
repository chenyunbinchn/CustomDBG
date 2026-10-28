using System;
using TMPro;
using tools.assert;
using ui.core;
using ui.intents;
using ui.viewModels;
using UnityEngine;
using UnityEngine.UI;

namespace ui
{
    // CardPileDialogModal shows a read-only snapshot of one pile.
    public sealed class CardPileDialogModal : UIView
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private CardPileListView cardPileListView;
        [SerializeField] private Button closeButton;

        public event Action<ViewCardDetailIntent> CardDetailRequested;
        public event Action Closed;

        private void Awake()
        {
            MyAssert.Assert(titleText != null, "CardPileDialogModal requires titleText.");
            MyAssert.Assert(cardPileListView != null, "CardPileDialogModal requires a CardPileListView.");
            MyAssert.Assert(closeButton != null, "CardPileDialogModal requires a closeButton.");
            cardPileListView.DetailRequested += HandleCardDetailRequested;
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        protected override void Render(IUIViewModel viewModel)
        {
            CardPileViewModel model = viewModel as CardPileViewModel;
            MyAssert.Assert(model != null, "CardPileDialogModal requires CardPileViewModel.");
            if (model == null)
            {
                return;
            }

            titleText.text = model.Title;
            cardPileListView.Render(model.Cards);
        }

        protected override void OnUnbind()
        {
            cardPileListView.Clear();
            Closed?.Invoke();
        }

        private void HandleCloseClicked()
        {
            UIManager.Instance.PopModal();
        }

        private void HandleCardDetailRequested(ViewCardDetailIntent intent)
        {
            CardDetailRequested?.Invoke(intent);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
            if (cardPileListView != null)
            {
                cardPileListView.DetailRequested -= HandleCardDetailRequested;
            }
        }
    }
}
