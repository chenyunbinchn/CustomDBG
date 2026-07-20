using System.Collections.Generic;
using cards.definition;
using enums;
using gameEffects;
using hook;

namespace cards.instance
{
    public class CardInstance : IHookListener
    {
        public CardDefinitionId DefinitionId;
        public CardInstanceId Id;
        public EnumCardType Type;
        public EnumEnchantmentType Enchantment;
        // Note: Copied from the definition, but mutable per-instance so a future "reduce this card's cost"
        //       effect can lower it without touching the immutable definition.
        public int EnergyCost;
        public Effect[] Effects;
        
        // Note: Hooks the card itself carries. Card instances are rebuilt for every battle
        //       (CardPileManager.CopyFromDeck), so these die with the battle — host decides lifetime.
        public List<HookListener> HookListeners { get; } = new List<HookListener>();

        public CardInstance(CardDefinition definition, CardInstanceId id, EnumEnchantmentType enchantment)
        {
            DefinitionId = definition.Id;
            Enchantment = enchantment;
            Id = id;
            EnergyCost = definition.EnergyCost;
            Effects = definition.Effects;
            Type = definition.Type;
        }

        // Note: Battle copy — CardPileManager.CopyFromDeck builds a fresh instance per battle from the
        //       persistent deck card, so in-battle mutation (cost reduction, attached hooks) never
        //       touches the deck. Keeps the same CardInstanceId so deck and battle card stay traceable.
        //       Effects points at the immutable definition's array, so sharing the reference is safe.
        public CardInstance(CardInstance source)
        {
            DefinitionId = source.DefinitionId;
            Enchantment = source.Enchantment;
            Id = source.Id;
            EnergyCost = source.EnergyCost;
            Effects = source.Effects;
            Type = source.Type;
        }

    }
}