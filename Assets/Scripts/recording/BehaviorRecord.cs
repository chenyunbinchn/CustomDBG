using System;

namespace recording
{
    // Common timeline metadata shared by every record domain. Domain-specific data belongs to
    // sealed subclasses so invalid cross-domain field combinations cannot be represented.
    [Serializable]
    public abstract class BehaviorRecord
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; }
        public string SessionId { get; }
        public ulong Sequence { get; }
        public DateTime UtcTime { get; }
        public int BattleId { get; }
        public int Turn { get; }
        public ulong? RootCommandId { get; }

        public abstract BehaviorRecordCategory Category { get; }
        public abstract EnumBehaviorRecordOutcome Outcome { get; }
        public abstract EnumBehaviorRecordSeverity Severity { get; }

        protected BehaviorRecord(BehaviorRecordMetadata metadata)
        {
            SchemaVersion = metadata.SchemaVersion;
            SessionId = metadata.SessionId;
            Sequence = metadata.Sequence;
            UtcTime = metadata.UtcTime;
            BattleId = metadata.BattleId;
            Turn = metadata.Turn;
            RootCommandId = metadata.RootCommandId == 0 ? null : metadata.RootCommandId;
        }
    }

    // Construction input for the common record envelope. It is copied into BehaviorRecord and is
    // deliberately not exposed as a nested JSON property, preserving the existing flat JSONL shape.
    public readonly struct BehaviorRecordMetadata
    {
        public int SchemaVersion { get; }
        public string SessionId { get; }
        public ulong Sequence { get; }
        public DateTime UtcTime { get; }
        public int BattleId { get; }
        public int Turn { get; }
        public ulong? RootCommandId { get; }

        public BehaviorRecordMetadata(
            string sessionId,
            ulong sequence,
            DateTime utcTime,
            int battleId,
            int turn,
            ulong? rootCommandId = null,
            int schemaVersion = BehaviorRecord.CurrentSchemaVersion)
        {
            SchemaVersion = schemaVersion;
            SessionId = sessionId;
            Sequence = sequence;
            UtcTime = utcTime;
            BattleId = battleId;
            Turn = turn;
            RootCommandId = rootCommandId;
        }
    }
}
