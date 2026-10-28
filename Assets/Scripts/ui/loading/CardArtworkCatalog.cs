using System;
using System.Collections.Generic;
using cards.definition;
using Newtonsoft.Json;
using tools.assert;
using UnityEngine;

namespace ui.loading
{
    // Client-only artwork. Rule initialization never loads Unity resources.
    public sealed class CardArtworkCatalog
    {
        private readonly Dictionary<CardDefinitionId, Sprite> _images = new Dictionary<CardDefinitionId, Sprite>();

        public void Init(string cardsJson)
        {
            _images.Clear();
            ArtworkLibraryData data = JsonConvert.DeserializeObject<ArtworkLibraryData>(cardsJson);
            if (data?.cards == null)
                throw new ArgumentException("cards.json failed to parse, or has no 'cards' array!", nameof(cardsJson));

            foreach (ArtworkData card in data.cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.idName))
                    throw new ArgumentException("cards.json contains artwork without a card id.", nameof(cardsJson));
                // Cards without artwork keep the existing null-artwork UI behavior.
                if (string.IsNullOrEmpty(card.imageName))
                    continue;

                Sprite sprite = Resources.Load<Sprite>(card.imageName);
                MyAssert.Assert(sprite != null,
                    "Card sprite not found in Resources: " + card.imageName + " (idName: " + card.idName + ")");
                _images.Add(new CardDefinitionId(card.idName), sprite);
            }
        }

        public bool TryGetImage(CardDefinitionId id, out Sprite image)
        {
            return _images.TryGetValue(id, out image);
        }

        // Read only visual fields from the existing shared JSON; ignore rule fields.
        [Serializable]
        private sealed class ArtworkLibraryData
        {
            public ArtworkData[] cards;
        }

        [Serializable]
        private sealed class ArtworkData
        {
            public string idName;
            public string imageName;
        }
    }
}
