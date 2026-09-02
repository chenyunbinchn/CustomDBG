using tools.assert;
using ui.core;
using ui.viewModels;

namespace ui
{
    // BattlePage is the full-screen lifecycle owner for battle HUDs and modals.
    public sealed class BattlePage : UIView
    {
        protected override void Render(IUIViewModel viewModel)
        {
            BattlePageViewModel model = viewModel as BattlePageViewModel;
            MyAssert.Assert(model != null, "BattlePage requires BattlePageViewModel.");

            // The minimum battle page has no visual fields to render yet.
            // TODO: Render a battle background when it becomes page-owned UI data.
        }
    }
}
