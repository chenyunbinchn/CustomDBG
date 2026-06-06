using UnityEngine;

namespace cards.definition
{
    [CreateAssetMenu(menuName = "Cards/Card Definition Library")]
    public class CardDefinitionLibrary : ScriptableObject
    {
        [SerializeField] public CardDefinitionAuthoring[] definitionSOs;
    }
}