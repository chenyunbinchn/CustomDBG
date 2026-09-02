using ui.core;

namespace ui.viewModels
{
    public sealed class BattleHudViewModel : IUIViewModel
    {
        public HandViewModel Hand { get; }
        public string EnergyText { get; }
        public string DrawPileCountText { get; }
        public string DiscardPileCountText { get; }

        public BattleHudViewModel(HandViewModel hand, string energyText, string drawPileCountText,
            string discardPileCountText)
        {
            Hand = hand;
            EnergyText = energyText;
            DrawPileCountText = drawPileCountText;
            DiscardPileCountText = discardPileCountText;
        }
    }
}
