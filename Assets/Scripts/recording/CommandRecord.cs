using System;
using cards.instance;
using combat;
using enums;
using recording.enums;

namespace recording
{
    [Serializable]
    public sealed class CommandRecord : BehaviorRecord
    {
        public EnumCommandRecordKind Kind { get; }
        public EnumCommandType? CommandType { get; }
        public EnumPlayCardResult? CommandResult { get; }
        public ActionEntityId? Source { get; }
        public ActionEntityId? Target { get; }
        public CardInstanceId? Card { get; }

        public override BehaviorRecordCategory Category => BehaviorRecordCategory.Command;

        public override EnumBehaviorRecordOutcome Outcome
        {
            get
            {
                switch (Kind)
                {
                    case EnumCommandRecordKind.CommandRejected:
                        return EnumBehaviorRecordOutcome.Rejected;
                    case EnumCommandRecordKind.CommandInterrupted:
                        return EnumBehaviorRecordOutcome.Interrupted;
                    case EnumCommandRecordKind.CommandAccepted:
                    case EnumCommandRecordKind.CommandCompleted:
                        return EnumBehaviorRecordOutcome.Succeeded;
                    default:
                        return EnumBehaviorRecordOutcome.Observed;
                }
            }
        }

        public override EnumBehaviorRecordSeverity Severity =>
            Kind == EnumCommandRecordKind.CommandRejected ||
            Kind == EnumCommandRecordKind.CommandInterrupted
                ? EnumBehaviorRecordSeverity.Warning
                : EnumBehaviorRecordSeverity.Info;

        public CommandRecord(
            EnumCommandRecordKind kind,
            BehaviorRecordMetadata metadata,
            EnumCommandType? commandType = null,
            EnumPlayCardResult? commandResult = null,
            ActionEntityId? source = null,
            ActionEntityId? target = null,
            CardInstanceId? card = null)
            : base(metadata)
        {
            Kind = kind;
            CommandType = commandType;
            CommandResult = commandResult;
            Source = source;
            Target = target;
            Card = card;
        }
    }
}
