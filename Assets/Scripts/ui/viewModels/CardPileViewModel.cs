using System.Collections.Generic;
using ui.core;

namespace ui.viewModels
{
    public sealed class CardPileViewModel : IUIViewModel
    {
        public string Title { get; }
        public IReadOnlyList<CardViewModel> Cards { get; }

        public CardPileViewModel(string title, IReadOnlyList<CardViewModel> cards)
        {
            Title = title;
            Cards = cards;
        }
    }
}
