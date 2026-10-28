using System;
using action;
using recording.enums;

namespace recording
{
    [Serializable]
    public sealed class ActionRecord : BehaviorRecord
    {
        public EnumActionRecordKind Kind { get; }
        public ActionId ActionId { get; }
        public string ActionType { get; }

        public override BehaviorRecordCategory Category => BehaviorRecordCategory.Action;
        public override EnumBehaviorRecordOutcome Outcome =>
            Kind == EnumActionRecordKind.ActionCompleted
                ? EnumBehaviorRecordOutcome.Succeeded
                : EnumBehaviorRecordOutcome.Observed;
        public override EnumBehaviorRecordSeverity Severity => EnumBehaviorRecordSeverity.Info;

        public ActionRecord(
            EnumActionRecordKind kind,
            BehaviorRecordMetadata metadata,
            ActionId actionId,
            string actionType)
            : base(metadata)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                throw new ArgumentException("An Action type is required.", nameof(actionType));
            }

            Kind = kind;
            ActionId = actionId;
            ActionType = actionType;
        }
    }
}
