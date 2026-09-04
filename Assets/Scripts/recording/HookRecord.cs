using System;
using combat;
using enums;
using recording.enums;

namespace recording
{
    [Serializable]
    public sealed class HookRecord : BehaviorRecord
    {
        public EnumHookRecordKind Kind { get; }
        public EnumHookType HookType { get; }
        public ActionEntityId Source { get; }
        public ActionEntityId Target { get; }
        public int Value { get; }
        public ActionEntityId? ListenerOwner { get; }
        public EnumHookFilter? ListenerFilter { get; }

        public override BehaviorRecordCategory Category => BehaviorRecordCategory.Hook;
        public override EnumBehaviorRecordOutcome Outcome => EnumBehaviorRecordOutcome.Observed;
        public override EnumBehaviorRecordSeverity Severity => EnumBehaviorRecordSeverity.Info;

        public HookRecord(
            EnumHookRecordKind kind,
            BehaviorRecordMetadata metadata,
            EnumHookType hookType,
            ActionEntityId source,
            ActionEntityId target,
            int value,
            ActionEntityId? listenerOwner = null,
            EnumHookFilter? listenerFilter = null)
            : base(metadata)
        {
            Kind = kind;
            HookType = hookType;
            Source = source;
            Target = target;
            Value = value;
            ListenerOwner = listenerOwner;
            ListenerFilter = listenerFilter;
        }
    }
}
