using System.Collections.Generic;
using ui.core;

namespace ui.viewModels
{
    public sealed class HandViewModel : IUIViewModel
    {
        public IReadOnlyList<CardViewModel> Cards { get; }

        public HandViewModel(IReadOnlyList<CardViewModel> cards)
        {
            Cards = cards;
        }
    }
}
