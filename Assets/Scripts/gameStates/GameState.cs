using cards.definition;
using enums;

namespace gameStates
{
    public class GameState
    {
        public EnumDifficulty Difficulty;
        public PlayerState PlayerState = new PlayerState();
        public BattleCardState BattleCardState = new BattleCardState();
        public CardDefinitionManager DefinitionManager = new CardDefinitionManager();
    }
}
