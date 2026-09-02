using action;
using combat;
using enums;
using gameStates;
using systems;
using tools.assert;

namespace hook
{
    // Note: THE hook dispatch. Stateless: it owns no listener list, it asks HookManager to gather them
    //       fresh at the moment of firing. Broadcast is indiscriminate — every listener on the field is
    //       walked and each decides for itself (Hook type + Filter) whether this event concerns it.
    //       A matching listener does NOT execute anything: its Effect is translated into an ordinary
    //       GameAction and queued, then drained by the existing serial resolution. That is what keeps
    //       reactions from recursing. See 《260718-rule-hook-system》 §4/§6.
    public static class HookSystem
    {
        public static void Fire(in HookEvent hookEvent, HookManager hookManager, StateManager stateManager,
            GameActionManager actionManager)
        {
            hookManager.IterateHookListeners(stateManager);

            for (int i = 0; i < hookManager.AllHookListeners.Count; i++)
            {
                HookListener listener = hookManager.AllHookListeners[i];
                if (listener.Hook != hookEvent.Hook)
                {
                    continue;
                }
                if (!PassesFilter(listener, hookEvent))
                {
                    continue;
                }

                // Note: the listener's Owner is the effect's source, so Self/User resolve to whoever the
                //       listener acts for — not to whatever host object physically carries it.
                EffectApi.Translate(listener.Effect, listener.Owner, hookEvent.Target, actionManager);
            }
        }

        // Note: The ONE place filtering is decided. Listeners carry the condition as data (EnumHookFilter)
        //       and never a method, so battle state stays serializable.
        private static bool PassesFilter(HookListener listener, in HookEvent hookEvent)
        {
            switch (listener.Filter)
            {
                case EnumHookFilter.Always:
                    return true;
                case EnumHookFilter.SelfIsSource:
                    return listener.Owner == hookEvent.Source;
                case EnumHookFilter.SelfIsTarget:
                    return listener.Owner == hookEvent.Target;
                default:
                    MyAssert.Assert(false, $"Unhandled EnumHookFilter: {listener.Filter}");
                    return false;
            }
        }
    }
}
