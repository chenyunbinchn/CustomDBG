using System.Collections.Generic;
using gameEffects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using tools.assert;
using UnityEngine;

namespace cards.definition
{
    public class CardDefinitionManager
    {
        private readonly Dictionary<CardDefinitionId, CardDefinition> _cards = new Dictionary<CardDefinitionId, CardDefinition>();
        // Note: Importing UnityEngine, but looks OK to me since we are now using Unity
        private Dictionary<CardDefinitionId, Sprite> _cardImages = new Dictionary<CardDefinitionId, Sprite>();

        // Note: Init from JSON text (StreamingAssets/cards.json) — replaces the old ScriptableObject
        //       library. Enums are strings via Newtonsoft's StringEnumConverter: a typo'd enum throws
        //       at load (crash on invalid data, per project philosophy).
        public void Init(string cardsJson) // Note: Maybe someday we can support mods, so can pass in multiple libraries.
        {
            _cards.Clear();
            _cardImages.Clear();
            CardLibraryData data = JsonConvert.DeserializeObject<CardLibraryData>(cardsJson, new StringEnumConverter());
            MyAssert.Assert(data != null && data.cards != null, "cards.json failed to parse, or has no 'cards' array!");
            LoadFromLibrary(data);
        }

        public Sprite GetImage(CardDefinitionId id)
        {
            if (!_cardImages.TryGetValue(id, out Sprite image))
            {
                MyAssert.Assert(false, "Card image not found! DefinitionId: " + id);
            }

            return image;
        }

        public bool TryGet(CardDefinitionId id, out CardDefinition result)
        {
            return _cards.TryGetValue(id, out result);
        }

        public CardDefinition Get(CardDefinitionId id)
        {
            if (!_cards.TryGetValue(id, out CardDefinition result))
            {
                MyAssert.Assert(false, "CardDefinition not found! DefinitionId: " + id);
            }

            return result;
        }

        private void LoadFromLibrary(CardLibraryData data)
        {
            foreach (CardDefinitionAuthoring card in data.cards)
            {
                MyAssert.Assert(card.effectAuthoringArray != null && card.effectAuthoringArray.Length > 0,
                    "card.effectAuthoringArray empty, can not load! Check cards.json, idName: " + card.idName);

                Effect[] effects = new Effect[card.effectAuthoringArray.Length];
                for (int i = 0; i < card.effectAuthoringArray.Length; i++)
                {
                    effects[i] = card.effectAuthoringArray[i].CreateRuntimeEffect();
                }

                CardDefinitionId newId = new CardDefinitionId(card.idName);
                CardDefinition definition = new CardDefinition(newId, card.cardType, card.energyCost, effects, card.description);
                _cards.Add(newId, definition);

                // Note: Sprite by name (plan S1 — Resources.Load). Empty imageName = no art yet, skip.
                //       Todo (plan S3): move to Addressables (already in project) when doing asset
                //       hot-reload — imageName becomes an addressable address, loaded async.
                if (!string.IsNullOrEmpty(card.imageName))
                {
                    Sprite sprite = Resources.Load<Sprite>(card.imageName);
                    MyAssert.Assert(sprite != null, "Card sprite not found in Resources: " + card.imageName + " (idName: " + card.idName + ")");
                    _cardImages.Add(newId, sprite);
                }
            }
        }
    }
}
