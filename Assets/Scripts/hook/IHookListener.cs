using System.Collections.Generic;

namespace hook
{
    public interface IHookListener
    {
        public List<HookListener> HookListeners { get; }
    }
}