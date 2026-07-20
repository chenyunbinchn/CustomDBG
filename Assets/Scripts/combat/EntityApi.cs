using enums;
using gameStates.transient;
using tools.assert;

namespace combat
{
    // Note: Turn a stable EntityId into the live actor. THE single place that switches on entity
    //       type — actions never switch themselves; they Resolve then operate on ICombatActor.
    //       Player resolves by indexing BattleState.Players with ActionEntityId.Id
    //       (assigned by StateManager.Init).
    public static class EntityApi
    {
        public static ICombatActor Resolve(ActionEntityId id, BattleState battleState)
        {
            switch (id.Type)
            {
                case EnumEntityType.Player:
                    MyAssert.Assert(battleState.Players != null && id.Id < (uint)battleState.Players.Length,
                        $"Player index {id.Id} out of range!");
                    return battleState.Players[(int)id.Id];
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
