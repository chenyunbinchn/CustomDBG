using System;

namespace cards.definition
{
    // Note: Root object of StreamingAssets/cards.json — the JSON that replaces the old
    //       CardDefinitionLibrarySO ScriptableObject. Deserialized by CardDefinitionManager.
    //       Todo: add a content version / hash field for multiplayer handshake (all clients must
    //             load the same card data — see 《260717-report-multiplayer-gap-scan》 5-3).
    [Serializable]
    public class CardLibraryData
    {
        public CardDefinitionAuthoring[] cards;
    }
}
