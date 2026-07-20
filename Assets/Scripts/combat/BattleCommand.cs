using cards.instance;
using enums;

namespace combat
{
    // Note: One battle input as pure data — THE unit of networking (a command is the wire message),
    //       of replay (a command is one log line) and of sequencing (the host orders commands, every
    //       end executes them in the same order). Local and remote input build the same struct and go
    //       through the same submit point — no host-privileged path. Fields are value types only.
    //       See 《260717-rule-multiplayer-battle-model》 §2.
    public readonly struct BattleCommand
    {
        public readonly EnumCommandType Type;
        public readonly ActionEntityId Player;   // who issued the command
        public readonly CardInstanceId Card;     // PlayCard: which card (never a hand index)
        public readonly ActionEntityId Target;   // PlayCard: picked target (default = untargeted)

        public BattleCommand(EnumCommandType type, ActionEntityId player, CardInstanceId card, ActionEntityId target)
        {
            Type = type;
            Player = player;
            Card = card;
            Target = target;
        }
    }
}
