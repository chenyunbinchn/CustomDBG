using System.Collections.Generic;
using cards.effects;
using tools;
using tools.assert;
using UnityEngine;

namespace cards.definition
{
    public class CardDefinitionManager
    {
        private readonly Dictionary<CardDefinitionId, CardDefinition> _cards = new Dictionary<CardDefinitionId, CardDefinition>();
        // Note: Importing UnityEngine, but looks OK to me since we are now using Unity
        private Dictionary<CardDefinitionId, Sprite> _cardImages = new Dictionary<CardDefinitionId, Sprite>(); 

        public void Init(CardDefinitionLibrary library) // Note: Maybe someday we can support mods, so can pass in multiple libraries.
        {
            _cards.Clear();
            _cardImages.Clear();
            LoadFromLibrary(library);
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

        private void LoadFromLibrary(CardDefinitionLibrary library)
        {
            foreach (CardDefinitionAuthoring so in library.definitionSOs)
            {
                MyAssert.Assert(so.effectAuthoringArray.Length > 0, 
                    "so.effectAuthoringArray.Length <= 0, can not load! Check CardDefinitionLibrary!");
                
                CardEffect[] effects = new CardEffect[so.effectAuthoringArray.Length];
                for (int i = 0; i < so.effectAuthoringArray.Length; i++)
                {
                    effects[i] = so.effectAuthoringArray[i].CreateRuntimeEffect();
                }

                CardDefinitionId newId = new CardDefinitionId(so.idName);
                CardDefinition definition = new CardDefinition(newId, so.cardType, so.energyCost, effects, so.description);
                
                _cards.Add(newId, definition);
                _cardImages.Add(newId, so.image);
            }
        }
    }
}