using System;
using System.Collections.Generic;
using recording.enums;

namespace recording
{
    public sealed class RecordFilter
    {
        public HashSet<BehaviorRecordCategory> Categories { get; } = new HashSet<BehaviorRecordCategory>();
        public HashSet<EnumBattleRecordKind> BattleKinds { get; } = new HashSet<EnumBattleRecordKind>();
        public HashSet<EnumCommandRecordKind> CommandKinds { get; } = new HashSet<EnumCommandRecordKind>();
        public HashSet<EnumActionRecordKind> ActionKinds { get; } = new HashSet<EnumActionRecordKind>();
        public HashSet<EnumHookRecordKind> HookKinds { get; } = new HashSet<EnumHookRecordKind>();
        public HashSet<EnumBehaviorRecordOutcome> Outcomes { get; } = new HashSet<EnumBehaviorRecordOutcome>();
        public HashSet<string> SessionIds { get; } = new HashSet<string>();
        public HashSet<ulong> RootCommandIds { get; } = new HashSet<ulong>();

        public int? BattleId { get; set; }
        public int? Turn { get; set; }
        public EnumBehaviorRecordSeverity? MinimumSeverity { get; set; }
        public bool ExpandRootCommandChain { get; set; }
    }

    public static class RecordQuery
    {
        public static List<BehaviorRecord> Apply(
            IReadOnlyList<BehaviorRecord> records,
            RecordFilter filter = null)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            bool[] selected = new bool[records.Count];
            HashSet<(string SessionId, ulong RootCommandId)> roots =
                new HashSet<(string SessionId, ulong RootCommandId)>();

            for (int i = 0; i < records.Count; i++)
            {
                BehaviorRecord record = records[i];
                if (record == null || !Matches(record, filter))
                {
                    continue;
                }

                selected[i] = true;
                if (filter?.ExpandRootCommandChain == true && record.RootCommandId.HasValue)
                {
                    roots.Add((record.SessionId, record.RootCommandId.Value));
                }
            }

            if (roots.Count > 0)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    BehaviorRecord record = records[i];
                    if (record?.RootCommandId != null &&
                        roots.Contains((record.SessionId, record.RootCommandId.Value)))
                    {
                        selected[i] = true;
                    }
                }
            }

            List<BehaviorRecord> result = new List<BehaviorRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                if (selected[i])
                {
                    result.Add(records[i]);
                }
            }

            result.Sort(CompareBySessionAndSequence);
            return result;
        }

        private static bool Matches(BehaviorRecord record, RecordFilter filter)
        {
            if (filter == null)
            {
                return true;
            }
            if (filter.Categories.Count > 0 && !filter.Categories.Contains(record.Category))
            {
                return false;
            }
            if (!MatchesKind(record, filter))
            {
                return false;
            }
            if (filter.Outcomes.Count > 0 && !filter.Outcomes.Contains(record.Outcome))
            {
                return false;
            }
            if (filter.SessionIds.Count > 0 && !filter.SessionIds.Contains(record.SessionId))
            {
                return false;
            }
            if (filter.RootCommandIds.Count > 0 &&
                (!record.RootCommandId.HasValue || !filter.RootCommandIds.Contains(record.RootCommandId.Value)))
            {
                return false;
            }
            if (filter.BattleId.HasValue && record.BattleId != filter.BattleId.Value)
            {
                return false;
            }
            if (filter.Turn.HasValue && record.Turn != filter.Turn.Value)
            {
                return false;
            }
            if (filter.MinimumSeverity.HasValue &&
                (int)record.Severity < (int)filter.MinimumSeverity.Value)
            {
                return false;
            }

            return true;
        }

        private static bool MatchesKind(BehaviorRecord record, RecordFilter filter)
        {
            bool hasKindFilter =
                filter.BattleKinds.Count > 0 ||
                filter.CommandKinds.Count > 0 ||
                filter.ActionKinds.Count > 0 ||
                filter.HookKinds.Count > 0;
            if (!hasKindFilter)
            {
                return true;
            }

            if (record is BattleRecord battleRecord)
            {
                return filter.BattleKinds.Contains(battleRecord.Kind);
            }
            if (record is CommandRecord commandRecord)
            {
                return filter.CommandKinds.Contains(commandRecord.Kind);
            }
            if (record is ActionRecord actionRecord)
            {
                return filter.ActionKinds.Contains(actionRecord.Kind);
            }
            if (record is HookRecord hookRecord)
            {
                return filter.HookKinds.Contains(hookRecord.Kind);
            }

            return false;
        }

        private static int CompareBySessionAndSequence(BehaviorRecord left, BehaviorRecord right)
        {
            int sessionComparison = string.CompareOrdinal(left.SessionId, right.SessionId);
            return sessionComparison != 0
                ? sessionComparison
                : left.Sequence.CompareTo(right.Sequence);
        }
    }
}
