using UnityEngine;

namespace cards.definition
{
    [CreateAssetMenu(menuName = "Cards/Card Definition Library")]
    public class CardDefinitionLibrarySO : ScriptableObject
    {
        [SerializeField] public CardDefinitionAuthoring[] definitionSOs;
    }
}