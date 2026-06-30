using System.Collections.Generic;

namespace hook
{
    public interface IHookListener
    {
        public List<HookInstance> HookInstances { get; }
    }
}