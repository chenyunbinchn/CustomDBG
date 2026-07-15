using enemy.instance;
using enums;
using gameStates.transient;
using tools.assert;

namespace combat
{
    public static class EntityApi
    {
        // Note: Turn a stable EntityId into the live actor. THE single place that switches on entity
        //       type — actions never switch themselves; they Resolve then operate on ICombatActor.
        //       Single-player: Player resolves to the current player passed in.
        //       Todo: multi-player needs BattlePlayerState[] to resolve Player by index.
        public static ICombatActor Resolve(ActionEntityId id, BattleState battleState, BattlePlayerState currentPlayer)
        {
            switch (id.Type)
            {
                case EnumEntityType.Player:
                    return currentPlayer;
                case EnumEntityType.Enemy:
                    for (int i = 0; i < battleState.EnemyList.Count; i++)
                    {
                        if (battleState.EnemyList[i].Id == id)
                        {
                            return battleState.EnemyList[i];
                        }
                    }
                    return null; // enemy no longer in battle — caller skips
                default:
                    MyAssert.Assert(false, $"Unknown EntityType {id.Type}");
                    return null;
            }
        }
    }
}
