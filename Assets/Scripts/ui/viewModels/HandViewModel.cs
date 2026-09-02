using System.Collections.Generic;

namespace ui.viewModels
{
    public sealed class HandViewModel
    {
        public IReadOnlyList<CardViewModel> Cards { get; }

        public HandViewModel(IReadOnlyList<CardViewModel> cards)
        {
            Cards = cards;
        }
    }
}
