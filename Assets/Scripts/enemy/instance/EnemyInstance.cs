using System.Collections.Generic;
using combat;
using enums;
using hook;

namespace enemy.instance
{
    public class EnemyInstance : ICombatActor
    {
        public int Hp { get; set; }
        public int Block { get; set; }
        public List<HookListener> HookListeners { get; } = new List<HookListener>();

        public ActionEntityId Id { get; set; }
    }
}
