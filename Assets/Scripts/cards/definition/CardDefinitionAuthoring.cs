using System;
using enums;
using gameEffects.authoring;

namespace cards.definition
{
    // Note: Designer-facing card data, deserialized from JSON (StreamingAssets/cards.json) by
    //       CardDefinitionManager. Plain data — no Unity object refs.
    [Serializable]
    public class CardDefinitionAuthoring
    {
        // Todo: Display name?
        public string idName;
        public EnumCardType cardType;
        public int energyCost;
        public EffectAuthoring[] effectAuthoringArray;
        public string description;
        // Client-only metadata read by ui.loading.CardArtworkCatalog; ignored by rule loading.
        public string imageName;
    }
}
