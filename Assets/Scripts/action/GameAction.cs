using System.Collections;
using enums;
using gameStates.transient;

namespace action
{
    public abstract class GameAction
    {
        public abstract ActionId Id { get; }
        public abstract EnumActionStatus ActionStatus { get; set; }
        public abstract IEnumerator Execute(BattleState battleState, BattlePlayerState playerState); // Todo: Check if action should not have execute function, ActionExecutor handle every execute?
    }
}