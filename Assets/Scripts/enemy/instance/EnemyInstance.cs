using System.Collections.Generic;
using hook;

namespace enemy.instance
{
    public class EnemyInstance : IHookListener
    {
        public int Hp;
        public List<HookListener> HookListeners { get; } = new List<HookListener>();
    }
}