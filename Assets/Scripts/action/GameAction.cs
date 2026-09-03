using System.Collections;
using enums;
using gameStates.transient;
using random;

namespace action
{
    // The deterministic state required by every battle action. Keeping the battle RNG in the
    // shared context prevents individual actions from creating unrelated random streams.
    public sealed class BattleActionContext
    {
        public BattleState BattleState { get; }
        public BattlePlayerState PlayerState { get; }
        public RandomManager RandomManager { get; }

        public BattleActionContext(BattleState battleState, BattlePlayerState playerState, RandomManager randomManager)
        {
            BattleState = battleState;
            PlayerState = playerState;
            RandomManager = randomManager;
        }
    }

    public abstract class GameAction
    {
        public abstract ActionId Id { get; }
        public abstract EnumActionStatus ActionStatus { get; set; }
        public abstract IEnumerator Execute(BattleActionContext context); // Todo: Check if action should not have execute function, ActionExecutor handle every execute?
    }
}
