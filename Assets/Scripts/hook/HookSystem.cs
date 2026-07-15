using action;
using combat;
using enums;
using gameStates.transient;
using systems;

namespace hook
{
    // Note: Fire an event. Walk every hook listener currently on the battlefield in a FIXED order
    //       (player first, then enemies — deterministic for replay); for each listener whose Hook
    //       matches, translate its Effect into an action queued for the executor. The listener's
    //       host actor is the effect source (so Self resolves to the host).
    //       Single-player: walks the current player + enemies directly, no StateManager needed.
    //       Todo: multi-player — walk all players via HookApi.IterateHookListeners(StateManager).
    //       Todo: self-filter (a listener should only react to events its own host caused).
    public static class HookSystem
    {
        public static void Fire(EnumHookType hook, BattleState battleState, BattlePlayerState currentPlayer, GameActionManager actionManager)
        {
            FireOn(currentPlayer, hook, actionManager);
            for (int i = 0; i < battleState.EnemyList.Count; i++)
            {
                FireOn(battleState.EnemyList[i], hook, actionManager);
            }
        }

        private static void FireOn(IHookListener host, EnumHookType hook, GameActionManager actionManager)
        {
            for (int i = 0; i < host.HookListeners.Count; i++)
            {
                HookListener listener = host.HookListeners[i];
                if (listener.Hook != hook)
                {
                    continue;
                }
                ActionEntityId source = ((ICombatActor)listener.Host).Id;
                EffectApi.Translate(listener.Effect, source, default, actionManager);
            }
        }
    }
}
