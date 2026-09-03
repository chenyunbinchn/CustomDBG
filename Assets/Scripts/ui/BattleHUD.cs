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
    // BattleHUD combines hand, energy, and pile entry points without mutating battle state.
    public sealed class BattleHUD : UIView
    {
        [SerializeField] private HandView handView;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text drawPileCountText;
        [SerializeField] private TMP_Text discardPileCountText;
        [SerializeField] private Button drawPileButton;
        [SerializeField] private Button discardPileButton;

        public event Action<PlayCardIntent> PlayCardRequested;
        public event Action<ViewCardPileIntent> ViewPileRequested;
        public event Action<ViewCardDetailIntent> CardDetailRequested;
        public event Action Closed;

        private void Awake()
        {
            MyAssert.Assert(handView != null, "BattleHUD requires a HandView.");
            MyAssert.Assert(energyText != null, "BattleHUD requires energyText.");
            MyAssert.Assert(drawPileCountText != null, "BattleHUD requires drawPileCountText.");
            MyAssert.Assert(discardPileCountText != null, "BattleHUD requires discardPileCountText.");
            MyAssert.Assert(drawPileButton != null, "BattleHUD requires drawPileButton.");
            MyAssert.Assert(discardPileButton != null, "BattleHUD requires discardPileButton.");

            handView.PlayRequested += HandlePlayRequested;
            handView.DetailRequested += HandleCardDetailRequested;
            drawPileButton.onClick.AddListener(HandleDrawPileClicked);
            discardPileButton.onClick.AddListener(HandleDiscardPileClicked);
        }

        protected override void Render(IUIViewModel viewModel)
        {
            BattleHudViewModel model = viewModel as BattleHudViewModel;
            MyAssert.Assert(model != null, "BattleHUD requires BattleHudViewModel.");
            if (model == null)
            {
                return;
            }

            energyText.text = model.EnergyText;
            drawPileCountText.text = model.DrawPileCountText;
            discardPileCountText.text = model.DiscardPileCountText;
            handView.Render(model.Hand);
        }

        protected override void OnUnbind()
        {
            handView.Clear();
            Closed?.Invoke();
        }

        private void HandlePlayRequested(PlayCardIntent intent)
        {
            PlayCardRequested?.Invoke(intent);
        }

        private void HandleCardDetailRequested(ViewCardDetailIntent intent)
        {
            CardDetailRequested?.Invoke(intent);
        }

        private void HandleDrawPileClicked()
        {
            ViewPileRequested?.Invoke(new ViewCardPileIntent(EnumCardPileKind.Draw));
        }

        private void HandleDiscardPileClicked()
        {
            ViewPileRequested?.Invoke(new ViewCardPileIntent(EnumCardPileKind.Discard));
        }

        private void OnDestroy()
        {
            if (handView != null)
            {
                handView.PlayRequested -= HandlePlayRequested;
                handView.DetailRequested -= HandleCardDetailRequested;
            }
            if (drawPileButton != null)
            {
                drawPileButton.onClick.RemoveListener(HandleDrawPileClicked);
            }
            if (discardPileButton != null)
            {
                discardPileButton.onClick.RemoveListener(HandleDiscardPileClicked);
            }
        }
    }
}
