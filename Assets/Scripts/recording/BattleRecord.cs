using System;
using recording.enums;

namespace recording
{
    [Serializable]
    public sealed class BattleRecord : BehaviorRecord
    {
        public EnumBattleRecordKind Kind => EnumBattleRecordKind.BattleStarted;

        public override BehaviorRecordCategory Category => BehaviorRecordCategory.Battle;
        public override EnumBehaviorRecordOutcome Outcome => EnumBehaviorRecordOutcome.Succeeded;
        public override EnumBehaviorRecordSeverity Severity => EnumBehaviorRecordSeverity.Info;

        public BattleRecord(BehaviorRecordMetadata metadata)
            : base(metadata)
        {
        }
    }
}
