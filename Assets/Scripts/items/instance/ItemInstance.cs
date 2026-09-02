using System.Collections.Generic;
using hook;

namespace items.instance
{
    // Note: A relic / permanent passive held in PlayerInfo.Items. It carries its own hook listeners,
    //       so they live exactly as long as the item does — losing the item removes its hooks with no
    //       cleanup code and no lifetime field.
    //       Todo: definition id, acquisition flow — structural placeholder for now, the list stays empty
    //             until items are implemented.
    public class ItemInstance : IHookListener
    {
        public List<HookListener> HookListeners { get; } = new List<HookListener>();
    }
}
