using hook;

namespace combat
{
    // Note: A combat entity (Player or Enemy). Extends IHookListener, so an actor both carries hook
    //       listeners (HookListeners) and exposes battle state (Hp/Block) + its own stable EntityId.
    //       EntityApi.Resolve returns this so actions can operate uniformly, without switching on type.
    public interface ICombatActor : IHookListener
    {
        int Hp { get; set; }
        int Block { get; set; }
        ActionEntityId Id { get; }
    }
}
