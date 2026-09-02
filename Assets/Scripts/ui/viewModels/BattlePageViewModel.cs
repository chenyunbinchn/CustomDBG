using ui.core;

namespace ui.viewModels
{
    // The minimum battle page only establishes page ownership and has no display data yet.
    public sealed class BattlePageViewModel : IUIViewModel
    {
        public static BattlePageViewModel Empty { get; } = new BattlePageViewModel();

        private BattlePageViewModel()
        {
        }
    }
}
