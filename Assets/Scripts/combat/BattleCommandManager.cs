using System.Collections.Generic;
using tools.assert;

namespace combat
{
    // Note: Pending battle commands, executed strictly one at a time — the next command only runs
    //       once the action queue has fully drained (quiescence). Submitted commands are never
    //       dropped: busy means "wait in line". The serial point is the EXIT of this queue, never
    //       the entrance. See 《260717-rule-multiplayer-battle-model》 §3.
    public class BattleCommandManager
    {
        public List<BattleCommand> Pending = new List<BattleCommand>();

        public void Enqueue(BattleCommand command)
        {
            Pending.Add(command);
        }

        public bool HasPending()
        {
            return Pending.Count > 0;
        }

        public BattleCommand Dequeue()
        {
            MyAssert.Assert(Pending.Count > 0, "No pending battle command!");
            BattleCommand result = Pending[0];
            Pending.RemoveAt(0);
            return result;
        }
    }
}
