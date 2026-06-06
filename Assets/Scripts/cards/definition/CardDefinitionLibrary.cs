using cards.definition;
using UnityEngine;

namespace cards.artwork
{
    [CreateAssetMenu(menuName = "Cards/Card Definition Library")]
    public class CardDefinitionLibrary : ScriptableObject
    {
        [SerializeField] public CardDefinitionSO[] definitionSOs;
    }
}