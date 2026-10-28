using System.Collections.Generic;
using recording;
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
        private readonly BehaviorRecorder _recorder;
        private ulong _nextCommandId = 1;

        public BattleCommandManager(BehaviorRecorder recorder)
        {
            _recorder = recorder;
        }

        public void Enqueue(BattleCommand command)
        {
            // Note: Command identity belongs to the authoritative queue, not to the observer.
            //       It is allocated even when every recording sink has failed.
            BattleCommand queuedCommand = command.WithCommandId(_nextCommandId++);
            Pending.Add(queuedCommand);
            _recorder.CommandSubmitted(queuedCommand);
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
