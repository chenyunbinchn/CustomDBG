using System;
using cards.authoring;
using enums;
using UnityEngine;

namespace cards.definition
{
    [Serializable]
    public class CardDefinitionAuthoring
    {
        // Todo: Display name?
        [SerializeField] public string idName;
        [SerializeField] public EnumCardType cardType;
        [SerializeField] public int energyCost;
        [SerializeField] public CardEffectAuthoring[] effectAuthoringArray;
        [SerializeField] public string description;
        [SerializeField] public Sprite image;
    }
}