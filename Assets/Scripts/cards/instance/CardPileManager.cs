using System.Collections.Generic;
using cards.definition;
using enums;
using UnityEngine;

namespace cards.instance
{
    public class CardPileManager
    {
        public Dictionary<CardInstanceId, CardInstance> Dictionary = new Dictionary<CardInstanceId, CardInstance>();
        private uint _maxIndex = 0; 

        public void CopyFromDeck(List<CardInstance> deck)
        {
            foreach (CardInstance card in deck)
            {
                Dictionary.TryAdd(card.Id, card);
                if (card.Id.Value > _maxIndex)
                {
                    _maxIndex = card.Id.Value;
                }
            }
            Debug.Log($"[Pile] CopyFromDeck -> registry({Dictionary.Count}): {DescribePile(new List<CardInstanceId>(Dictionary.Keys))}");
        }

        public CardInstanceId AddCard(CardDefinition definition)
        {
            uint newIdValue = ++_maxIndex;
            CardInstance newCard = new CardInstance(definition, new CardInstanceId(newIdValue), EnumEnchantmentType.None);
            return newCard.Id;
        }
        
        public void Reset()
        {
            Dictionary.Clear();
        }

        // Note: Debug-friendly name for a card instance, e.g. "攻击#3" (definition name + instance id).
        public string DescribeCard(CardInstanceId id)
        {
            bool found = Dictionary.TryGetValue(id, out CardInstance card);
            return found ? $"{card.DefinitionId.Name}#{id.Value}" : $"?#{id.Value}";
        }

        // Note: Debug-friendly listing of a pile, e.g. "[攻击#3, 防御#1]".
        public string DescribePile(List<CardInstanceId> pile)
        {
            string[] parts = new string[pile.Count];
            for (int i = 0; i < pile.Count; i++)
            {
                parts[i] = DescribeCard(pile[i]);
            }
            return "[" + string.Join(", ", parts) + "]";
        }
    }
}