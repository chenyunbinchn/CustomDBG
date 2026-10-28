using System;
using enums;
using gameStates.transient;

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
            if (TryResolve(id, battleState, out ICombatActor actor))
            {
                return actor;
            }

            if (id.Type == EnumEntityType.Player)
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"Player index {id.Id} is out of range.");
            }
            return null;
        }

        public static bool TryResolve(ActionEntityId id, BattleState battleState, out ICombatActor actor)
        {
            actor = null;
            if (battleState == null)
            {
                return false;
            }

            switch (id.Type)
            {
                case EnumEntityType.Player:
                    if (battleState.Players == null || id.Id >= (uint)battleState.Players.Length)
                    {
                        return false;
                    }
                    actor = battleState.Players[(int)id.Id];
                    return actor != null;
                case EnumEntityType.Enemy:
                    for (int i = 0; i < battleState.EnemyList.Count; i++)
                    {
                        if (battleState.EnemyList[i].Id == id)
                        {
                            actor = battleState.EnemyList[i];
                            return true;
                        }
                    }
                    return false; // enemy no longer in battle — caller skips
                default:
                    return false;
            }
        }
    }
}
