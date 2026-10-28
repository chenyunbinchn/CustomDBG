using System;
using System.Collections.Generic;
using enums;
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
            if (data?.cards == null)
                throw new ArgumentException("cards.json failed to parse, or has no 'cards' array!", nameof(cardsJson));
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

        public bool TryGetImage(CardDefinitionId id, out Sprite image)
        {
            return _cardImages.TryGetValue(id, out image);
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
                if (card == null)
                    throw new ArgumentException("cards.json contains a null card definition.");
                if (string.IsNullOrWhiteSpace(card.idName))
                    throw new ArgumentException("cards.json contains a card with no idName.");
                if (card.energyCost < 0)
                    throw new ArgumentException($"card '{card.idName}' has a negative energy cost.");
                if (card.effectAuthoringArray == null || card.effectAuthoringArray.Length == 0)
                    throw new ArgumentException(
                        "card.effectAuthoringArray empty, can not load! Check cards.json, idName: " + card.idName);

                Effect[] effects = new Effect[card.effectAuthoringArray.Length];
                for (int i = 0; i < card.effectAuthoringArray.Length; i++)
                {
                    ValidateEffect(card.effectAuthoringArray[i], card.idName, i);
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

        private static void ValidateEffect(gameEffects.authoring.EffectAuthoring effect, string cardName, int index)
        {
            string location = $"card '{cardName}' effect[{index}]";
            if (!Enum.IsDefined(typeof(EnumEffectType), effect.effectType))
                throw new ArgumentException($"{location} uses unknown EffectType value {(int)effect.effectType}.");
            if (!Enum.IsDefined(typeof(EnumTargetType), effect.targetType))
                throw new ArgumentException($"{location} uses unknown TargetType value {(int)effect.targetType}.");
            if (!Enum.IsDefined(typeof(EnumStatusType), effect.statusType))
                throw new ArgumentException($"{location} uses unknown StatusType value {(int)effect.statusType}.");
            if (effect.effectType == EnumEffectType.None || effect.effectType == EnumEffectType.CostEnergy)
                throw new ArgumentException($"{location} uses unsupported EffectType {effect.effectType}.");
            if (effect.value < 0)
                throw new ArgumentException($"{location} has a negative value.");
            if (effect.targetType == EnumTargetType.None)
                throw new ArgumentException($"{location} has no target type.");
            if (effect.effectType == EnumEffectType.ApplyStatus &&
                (effect.statusType == EnumStatusType.None || effect.value == 0))
                throw new ArgumentException($"{location} must specify a positive status amount and status type.");
            if (effect.effectType != EnumEffectType.ApplyStatus && effect.statusType != EnumStatusType.None)
                throw new ArgumentException($"{location} specifies a status for a non-status effect.");
            if (effect.effectType == EnumEffectType.Exhaust && effect.targetType != EnumTargetType.Self)
                throw new ArgumentException($"{location}: Exhaust currently supports Self only.");
            if ((effect.effectType == EnumEffectType.DrawCards || effect.effectType == EnumEffectType.GainEnergy) &&
                effect.targetType != EnumTargetType.Self && effect.targetType != EnumTargetType.User &&
                effect.targetType != EnumTargetType.SelectedAlly && effect.targetType != EnumTargetType.AllAllies)
                throw new ArgumentException($"{location}: {effect.effectType} requires a player target.");
        }
    }
}
