using UnityEngine;

namespace cards.definition
{
    // Todo: Add "SO" postfix
    [CreateAssetMenu(menuName = "Cards/Card Definition Library")]
    public class CardDefinitionLibrary : ScriptableObject
    {
        [SerializeField] public CardDefinitionAuthoring[] definitionSOs;
    }
}