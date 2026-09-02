using combat;
using enums;
using gameEffects;

namespace hook
{
    // Note: Pure data — a listener carries CONDITIONS, never methods (the filter switch lives in
    //       HookSystem). No lifetime field on purpose: the host object decides how long it lives
    //       (item -> until the item is lost, BattlePlayerState / CardInstance / EnemyInstance -> until
    //       the battle is rebuilt). See 《260718-rule-hook-system》.
    public class HookListener
    {
        public Effect Effect;
        public EnumHookType Hook;

        // Note: Which COMBAT ACTOR this listener acts for. Needed because a host can be a CardInstance
        //       or an ItemInstance, which have no ActionEntityId — self-filtering compares this against
        //       HookEvent.Source / Target, and it is also the effect's source when translated.
        public ActionEntityId Owner;

        // Note: When the host should react (Always / SelfIsSource / SelfIsTarget).
        public EnumHookFilter Filter;
    }
}
