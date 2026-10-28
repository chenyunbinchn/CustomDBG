using System.Collections;
using enums;
using gameStates;
using gameStates.transient;
using random;

namespace action
{
    // The deterministic state required by every battle action. Keeping the battle RNG in the
    // shared context prevents individual actions from creating unrelated random streams.
    public sealed class BattleActionContext
    {
        public StateManager StateManager { get; }
        public BattleState BattleState { get; }
        public BattlePlayerState PlayerState { get; }
        public RandomManager RandomManager { get; }
        public GameActionManager ActionManager { get; }
        public ulong RootCommandId { get; }

        public BattleActionContext(StateManager stateManager, BattlePlayerState playerState, ulong rootCommandId)
        {
            StateManager = stateManager ?? throw new System.ArgumentNullException(nameof(stateManager));
            BattleState = stateManager.BattleState;
            PlayerState = playerState;
            RandomManager = stateManager.GameState.RandomManager;
            ActionManager = stateManager.GameState.GameActionManager;
            RootCommandId = rootCommandId;
        }

        // Lightweight constructor for isolated action tests that do not fire hooks or enqueue reactions.
        public BattleActionContext(BattleState battleState, BattlePlayerState playerState, RandomManager randomManager)
        {
            BattleState = battleState ?? throw new System.ArgumentNullException(nameof(battleState));
            PlayerState = playerState;
            RandomManager = randomManager ?? throw new System.ArgumentNullException(nameof(randomManager));
        }
    }

    public abstract class GameAction
    {
        public abstract ActionId Id { get; }
        public abstract EnumActionStatus ActionStatus { get; set; }
        public virtual string ResultSummary => null;
        public abstract IEnumerator Execute(BattleActionContext context); // Todo: Check if action should not have execute function, ActionExecutor handle every execute?
    }
}
