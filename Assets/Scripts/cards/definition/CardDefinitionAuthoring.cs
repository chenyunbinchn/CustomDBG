using System;
using enums;
using gameEffects.authoring;

namespace cards.definition
{
    // Note: Designer-facing card data, deserialized from JSON (StreamingAssets/cards.json) by
    //       CardDefinitionManager. Plain data — no Unity object refs (sprite is a name, resolved at load).
    [Serializable]
    public class CardDefinitionAuthoring
    {
        // Todo: Display name?
        public string idName;
        public EnumCardType cardType;
        public int energyCost;
        public EffectAuthoring[] effectAuthoringArray;
        public string description;
        // Note: Sprite resolved by name at load (Resources.Load, plan S1). Empty = no art yet.
        public string imageName;
    }
}
