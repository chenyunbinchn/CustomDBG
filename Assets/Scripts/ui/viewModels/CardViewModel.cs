using cards.instance;
using ui.core;
using UnityEngine;

namespace ui.viewModels
{
    public sealed class CardViewModel : IUIViewModel
    {
        public CardInstanceId CardId { get; }
        public string Name { get; }
        public string CostText { get; }
        public string Description { get; }
        public Sprite Artwork { get; }
        public bool IsPlayable { get; }

        public CardViewModel(CardInstanceId cardId, string name, string costText, string description,
            Sprite artwork, bool isPlayable)
        {
            CardId = cardId;
            Name = name;
            CostText = costText;
            Description = description;
            Artwork = artwork;
            IsPlayable = isPlayable;
        }
    }
}
