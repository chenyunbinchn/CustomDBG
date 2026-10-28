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
        public readonly ulong CommandId;         // assigned by the authoritative command queue
        public readonly EnumCommandType Type;
        public readonly ActionEntityId Player;   // who issued the command
        public readonly CardInstanceId Card;     // PlayCard: which card (never a hand index)
        public readonly ActionEntityId Target;   // PlayCard: picked target (default = untargeted)
        public readonly ulong ChoiceId;          // ChooseCard/CancelChoice: pending choice identity

        public BattleCommand(EnumCommandType type, ActionEntityId player, CardInstanceId card, ActionEntityId target)
        {
            CommandId = 0;
            Type = type;
            Player = player;
            Card = card;
            Target = target;
            ChoiceId = 0;
        }

        public static BattleCommand ChooseCard(ulong choiceId, ActionEntityId player, CardInstanceId card)
        {
            return new BattleCommand(0, EnumCommandType.ChooseCard, player, card, default, choiceId);
        }

        public static BattleCommand CancelChoice(ulong choiceId, ActionEntityId player)
        {
            return new BattleCommand(0, EnumCommandType.CancelChoice, player, default, default, choiceId);
        }

        private BattleCommand(
            ulong commandId,
            EnumCommandType type,
            ActionEntityId player,
            CardInstanceId card,
            ActionEntityId target,
            ulong choiceId)
        {
            CommandId = commandId;
            Type = type;
            Player = player;
            Card = card;
            Target = target;
            ChoiceId = choiceId;
        }

        internal BattleCommand WithCommandId(ulong commandId)
        {
            return new BattleCommand(commandId, Type, Player, Card, Target, ChoiceId);
        }
    }
}
