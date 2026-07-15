using System.Collections.Generic;
using cards.definition;
using enums;
using gameEffects.authoring;
using UnityEditor;
using UnityEngine;

// Note: Editor-only tool to batch-add cards into the CardDefinitionLibrary asset via code, so we don't
//       hand-edit the .asset YAML. Lives in an "Editor" folder (Unity compiles it only in the editor,
//       never into a build). Folder is capital-E "Editor" per Unity's requirement; namespace kept lowercase
//       to match the rest of the project.
namespace editor
{
    public static class CardLibraryEditorTool
    {
        // Menu: Tools > Cards > Add Test Cards. Adds (or replaces by idName) the test cards below into the
        // first CardDefinitionLibrarySO found in the project, then saves the asset.
        [MenuItem("Tools/Cards/Add Test Cards")]
        public static void AddTestCards()
        {
            CardDefinitionLibrarySO library = FindLibrary();
            if (library == null)
            {
                Debug.LogError("[CardTool] No CardDefinitionLibrarySO found in project.");
                return;
            }

            CardDefinitionAuthoring[] cards =
            {
                new CardDefinitionAuthoring
                {
                    idName = "残影",
                    cardType = EnumCardType.Power,
                    energyCost = 1,
                    description = "每打出一张牌，获得 2 点格挡。",
                    image = null,
                    effectAuthoringArray = new EffectAuthoring[]
                    {
                        new EffectAuthoring
                        {
                            effectType = EnumEffectType.ApplyStatus,
                            targetType = EnumTargetType.Self,
                            statusType = EnumStatusType.Afterimage,
                            value = 2
                        }
                    }
                }
                // Add more test cards here — they will be added or replaced by idName.
            };

            AddCards(library, cards);
        }

        // Reusable batch entry: add each card, replacing an existing one with the same idName.
        public static void AddCards(CardDefinitionLibrarySO library, CardDefinitionAuthoring[] cards)
        {
            List<CardDefinitionAuthoring> list = library.definitionSOs != null
                ? new List<CardDefinitionAuthoring>(library.definitionSOs)
                : new List<CardDefinitionAuthoring>();

            foreach (CardDefinitionAuthoring card in cards)
            {
                int index = list.FindIndex(existing => existing.idName == card.idName);
                if (index >= 0)
                {
                    list[index] = card;
                    Debug.Log($"[CardTool] Replaced card '{card.idName}'.");
                }
                else
                {
                    list.Add(card);
                    Debug.Log($"[CardTool] Added card '{card.idName}'.");
                }
            }

            library.definitionSOs = list.ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CardTool] Saved '{library.name}' — now {library.definitionSOs.Length} cards.");
        }

        private static CardDefinitionLibrarySO FindLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDefinitionLibrarySO");
            if (guids.Length == 0)
            {
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<CardDefinitionLibrarySO>(path);
        }
    }
}
