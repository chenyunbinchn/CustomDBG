using System;
using System.IO;
using System.Linq;
using action.gameEffectActions;
using cards.instance;
using combat;
using enums;
using hook;
using NUnit.Framework;
using recording;
using recording.enums;
using UnityEngine;
using UnityEngine.TestTools;

namespace recording.tests
{
    public class BehaviorRecorderTests
    {
        [Test]
        public void FailedSink_DoesNotPreventMemorySink()
        {
            InMemoryBehaviorRecordSink memorySink = new InMemoryBehaviorRecordSink();
            BehaviorRecorder recorder = new BehaviorRecorder(memorySink, new ThrowingSink());
            LogAssert.Expect(LogType.Error, "[BehaviorRecorder] Recording disabled for one sink: sink failed");

            Assert.DoesNotThrow(() => recorder.BattleStarted(1));

            Assert.That(recorder.IsLogComplete, Is.False);
            Assert.That(memorySink.Records.Count, Is.EqualTo(1));
            Assert.That(memorySink.Records[0], Is.TypeOf<BattleRecord>());
            Assert.That(((BattleRecord)memorySink.Records[0]).Kind,
                Is.EqualTo(EnumBattleRecordKind.BattleStarted));
            recorder.Dispose();
        }

        [Test]
        public void CommandManager_AssignsIdsEvenWhenRecorderSinkFails()
        {
            BehaviorRecorder recorder = new BehaviorRecorder(new ThrowingSink());
            BattleCommandManager manager = new BattleCommandManager(recorder);
            BattleCommand command = new BattleCommand(
                EnumCommandType.PlayCard,
                new ActionEntityId(EnumEntityType.Player, 0),
                new CardInstanceId(1),
                new ActionEntityId(EnumEntityType.Enemy, 0));
            LogAssert.Expect(LogType.Error, "[BehaviorRecorder] Recording disabled for one sink: sink failed");

            Assert.DoesNotThrow(() =>
            {
                manager.Enqueue(command);
                manager.Enqueue(command);
            });

            Assert.That(manager.Pending.Select(item => item.CommandId), Is.EqualTo(new ulong[] { 1, 2 }));
            recorder.Dispose();
        }

        [Test]
        public void Recorder_GeneratesOneExpandableRootChain()
        {
            InMemoryBehaviorRecordSink memorySink = new InMemoryBehaviorRecordSink();
            BehaviorRecorder recorder = new BehaviorRecorder(memorySink);
            BattleCommandManager manager = new BattleCommandManager(recorder);
            BattleCommand command = NewCommand();
            manager.Enqueue(command);
            BattleCommand queuedCommand = manager.Pending[0];
            recorder.CommandAccepted(queuedCommand);

            DamageAction action = new DamageAction(
                3,
                queuedCommand.Target,
                new action.ActionId(4),
                EnumActionStatus.WaitingForExecution);
            recorder.ActionQueued(action, queuedCommand.CommandId);
            Hook firedHook = new Hook(
                EnumHookType.AfterCardPlayed,
                queuedCommand.Player,
                queuedCommand.Target,
                0);
            recorder.HookFired(firedHook, queuedCommand.CommandId);
            recorder.HookMatched(firedHook, new HookListener
            {
                Owner = queuedCommand.Player,
                Filter = EnumHookFilter.SelfIsSource,
            }, queuedCommand.CommandId);
            recorder.ActionStarted(action, queuedCommand.CommandId);
            recorder.ActionCompleted(action, queuedCommand.CommandId);
            recorder.CommandCompleted(queuedCommand.CommandId);
            manager.Enqueue(command);

            RecordFilter filter = new RecordFilter { ExpandRootCommandChain = true };
            filter.HookKinds.Add(EnumHookRecordKind.HookMatched);
            var result = RecordQuery.Apply(memorySink.Records, filter);

            Assert.That(result.Count, Is.EqualTo(8));
            Assert.That(result.All(record => record.RootCommandId == queuedCommand.CommandId), Is.True);
            Assert.That(result.Select(record => record.Sequence), Is.Ordered.Ascending);
            Assert.That(result.Count(record => record is CommandRecord), Is.EqualTo(3));
            Assert.That(result.Count(record => record is ActionRecord), Is.EqualTo(3));
            Assert.That(result.Count(record => record is HookRecord), Is.EqualTo(2));
            recorder.Dispose();
        }

        [Test]
        public void Recorder_DisposeMarksAcceptedCommandInterrupted()
        {
            InMemoryBehaviorRecordSink memorySink = new InMemoryBehaviorRecordSink();
            BehaviorRecorder recorder = new BehaviorRecorder(memorySink);
            BattleCommandManager manager = new BattleCommandManager(recorder);
            manager.Enqueue(NewCommand());
            BattleCommand queuedCommand = manager.Pending[0];
            recorder.CommandAccepted(queuedCommand);

            recorder.Dispose();

            CommandRecord last = memorySink.Records.Last() as CommandRecord;
            Assert.That(last, Is.Not.Null);
            Assert.That(last.Kind, Is.EqualTo(EnumCommandRecordKind.CommandInterrupted));
            Assert.That(last.RootCommandId, Is.EqualTo(queuedCommand.CommandId));
        }

        [Test]
        public void Recorder_RejectedCommandFlushesTerminalRecord()
        {
            TrackingSink sink = new TrackingSink();
            BehaviorRecorder recorder = new BehaviorRecorder(sink);
            BattleCommandManager manager = new BattleCommandManager(recorder);
            manager.Enqueue(NewCommand());

            recorder.CommandRejected(manager.Pending[0], EnumPlayCardResult.NotEnoughEnergy);

            Assert.That(sink.AppendCount, Is.EqualTo(2));
            Assert.That(sink.FlushCount, Is.EqualTo(1));
            recorder.Dispose();
        }

        [Test]
        public void Record_NormalizesZeroRootToRootless()
        {
            BehaviorRecord record = Record(1, EnumHookRecordKind.HookFired, 0);

            Assert.That(record.RootCommandId, Is.Null);
        }

        [Test]
        public void DomainRecords_ExposeOnlyTheirOwnDomainFields()
        {
            Assert.That(typeof(BehaviorRecord).IsAbstract, Is.True);
            Assert.That(typeof(ActionRecord).GetProperty(nameof(CommandRecord.CommandResult)), Is.Null);
            Assert.That(typeof(CommandRecord).GetProperty(nameof(ActionRecord.ActionId)), Is.Null);
            Assert.That(typeof(HookRecord).GetProperty(nameof(ActionRecord.ActionType)), Is.Null);
        }

        [Test]
        public void DomainRecords_UseIndependentKindEnums()
        {
            Assert.That(typeof(BattleRecord).GetProperty(nameof(BattleRecord.Kind)).PropertyType,
                Is.EqualTo(typeof(EnumBattleRecordKind)));
            Assert.That(typeof(CommandRecord).GetProperty(nameof(CommandRecord.Kind)).PropertyType,
                Is.EqualTo(typeof(EnumCommandRecordKind)));
            Assert.That(typeof(ActionRecord).GetProperty(nameof(ActionRecord.Kind)).PropertyType,
                Is.EqualTo(typeof(EnumActionRecordKind)));
            Assert.That(typeof(HookRecord).GetProperty(nameof(HookRecord.Kind)).PropertyType,
                Is.EqualTo(typeof(EnumHookRecordKind)));
        }

        [Test]
        public void MemorySink_RetainsNewestRecordsWithinCapacity()
        {
            InMemoryBehaviorRecordSink sink = new InMemoryBehaviorRecordSink(2);
            sink.Append(Record(1, EnumBattleRecordKind.BattleStarted));
            sink.Append(Record(2, EnumCommandRecordKind.CommandSubmitted, 1));
            sink.Append(Record(3, EnumCommandRecordKind.CommandCompleted, 1));

            Assert.That(sink.Records.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 2, 3 }));
            Assert.That(sink.Version, Is.EqualTo(3));
        }

        [Test]
        public void JsonLinesSink_WritesUtf8WithoutBomAndReadableJsonLines()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"customdbg-recording-{Guid.NewGuid():N}.jsonl");
            try
            {
                using (JsonLinesBehaviorRecordSink sink = new JsonLinesBehaviorRecordSink(filePath))
                {
                    sink.Append(Record(1, EnumCommandRecordKind.CommandSubmitted, 1));
                    sink.Append(Record(2, EnumCommandRecordKind.CommandRejected, 1));
                    sink.Flush();
                }

                byte[] bytes = File.ReadAllBytes(filePath);
                string[] lines = File.ReadAllLines(filePath);
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
                Assert.That(lines.Length, Is.EqualTo(2));
                Assert.That(lines.All(line => line.StartsWith("{") && line.EndsWith("}")), Is.True);
                Assert.That(lines[1], Does.Contain("\"kind\":\"CommandRejected\""));
                Assert.That(lines[1], Does.Not.Contain("actionType"));
                Assert.That(lines[1], Does.Not.Contain("$type"));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public void Query_NullFilterReturnsSortedCopyWithoutMutatingInput()
        {
            BehaviorRecord[] input =
            {
                Record(5, EnumActionRecordKind.ActionCompleted, 1),
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1),
                Record(3, EnumActionRecordKind.ActionStarted, 1),
            };

            var result = RecordQuery.Apply(input);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 1, 3, 5 }));
            Assert.That(input.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 5, 1, 3 }));
        }

        [Test]
        public void Query_UsesOrWithinAFieldAndAndAcrossFields()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumActionRecordKind.ActionQueued, 1, turn: 2),
                Record(2, EnumActionRecordKind.ActionStarted, 1, turn: 2),
                Record(3, EnumActionRecordKind.ActionCompleted, 1, turn: 2),
                Record(4, EnumHookRecordKind.HookFired, 1, turn: 2),
                Record(5, EnumActionRecordKind.ActionQueued, 2, turn: 3),
            };
            RecordFilter filter = new RecordFilter { Turn = 2 };
            filter.Categories.Add(BehaviorRecordCategory.Action);
            filter.ActionKinds.Add(EnumActionRecordKind.ActionQueued);
            filter.ActionKinds.Add(EnumActionRecordKind.ActionStarted);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 1, 2 }));
        }

        [Test]
        public void Query_UsesOrAcrossDomainKindSets()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1),
                Record(2, EnumActionRecordKind.ActionStarted, 1),
                Record(3, EnumHookRecordKind.HookMatched, 1),
            };
            RecordFilter filter = new RecordFilter();
            filter.ActionKinds.Add(EnumActionRecordKind.ActionStarted);
            filter.HookKinds.Add(EnumHookRecordKind.HookMatched);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 2, 3 }));
        }

        [Test]
        public void Query_AppliesOutcomeSessionRootBattleAndSeverity()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandRejected, 9, "session-a", turn: 3, battleId: 2),
                Record(2, EnumCommandRecordKind.CommandRejected, 8, "session-a", turn: 3, battleId: 2),
                Record(3, EnumCommandRecordKind.CommandRejected, 9, "session-b", turn: 3, battleId: 2),
                Record(4, EnumCommandRecordKind.CommandSubmitted, 9, "session-a", turn: 3, battleId: 2),
                Record(5, EnumCommandRecordKind.CommandRejected, 9, "session-a", turn: 3, battleId: 1),
            };
            RecordFilter filter = new RecordFilter
            {
                BattleId = 2,
                MinimumSeverity = EnumBehaviorRecordSeverity.Warning,
            };
            filter.Outcomes.Add(EnumBehaviorRecordOutcome.Rejected);
            filter.SessionIds.Add("session-a");
            filter.RootCommandIds.Add(9);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 1 }));
        }

        [Test]
        public void Query_OnlyProblemsReturnsRejectedAndInterruptedRecords()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1),
                Record(2, EnumCommandRecordKind.CommandRejected, 1),
                Record(3, EnumActionRecordKind.ActionCompleted, 1),
                Record(4, EnumCommandRecordKind.CommandInterrupted, 2),
            };

            var result = RecordQuery.Apply(input, new RecordFilter { OnlyProblems = true });

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 2, 4 }));
        }

        [Test]
        public void Query_GroupsSessionsThenSortsEachBySequence()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumActionRecordKind.ActionCompleted, 1, "session-b"),
                Record(2, EnumActionRecordKind.ActionCompleted, 1, "session-a"),
                Record(1, EnumActionRecordKind.ActionStarted, 1, "session-a"),
            };

            var result = RecordQuery.Apply(input);

            Assert.That(
                result.Select(record => $"{record.SessionId}:{record.Sequence}"),
                Is.EqualTo(new[] { "session-a:1", "session-a:2", "session-b:1" }));
        }

        [Test]
        public void Query_WithoutExpansionReturnsOnlySeedMatches()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandSubmitted, 9),
                Record(2, EnumHookRecordKind.HookMatched, 9),
                Record(3, EnumActionRecordKind.ActionCompleted, 9),
            };
            RecordFilter filter = new RecordFilter();
            filter.HookKinds.Add(EnumHookRecordKind.HookMatched);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 2 }));
        }

        [Test]
        public void Query_ExpansionAddsOnlyTheMatchedCommandChain()
        {
            BehaviorRecord[] input =
            {
                Record(5, EnumActionRecordKind.ActionCompleted, 9),
                Record(1, EnumCommandRecordKind.CommandSubmitted, 9),
                Record(4, EnumHookRecordKind.HookMatched, 9),
                Record(2, EnumCommandRecordKind.CommandSubmitted, 10),
            };
            RecordFilter filter = new RecordFilter { ExpandRootCommandChain = true };
            filter.HookKinds.Add(EnumHookRecordKind.HookMatched);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 1, 4, 5 }));
        }

        [Test]
        public void Query_ExpansionDoesNotGroupRootlessRecords()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumBattleRecordKind.BattleStarted),
                Record(2, EnumHookRecordKind.HookFired),
                Record(3, EnumActionRecordKind.ActionCompleted, 7),
            };
            RecordFilter filter = new RecordFilter { ExpandRootCommandChain = true };
            filter.HookKinds.Add(EnumHookRecordKind.HookFired);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 2 }));
        }

        [Test]
        public void Query_ExpansionDoesNotCrossSessionBoundary()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1, "session-a"),
                Record(2, EnumHookRecordKind.HookMatched, 1, "session-a"),
                Record(3, EnumActionRecordKind.ActionCompleted, 1, "session-b"),
            };
            RecordFilter filter = new RecordFilter { ExpandRootCommandChain = true };
            filter.HookKinds.Add(EnumHookRecordKind.HookMatched);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Select(record => record.Sequence), Is.EqualTo(new ulong[] { 1, 2 }));
        }

        [Test]
        public void Query_RootFilterWithSessionSelectsOnlyThatSession()
        {
            BehaviorRecord[] input =
            {
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1, "session-a"),
                Record(2, EnumCommandRecordKind.CommandCompleted, 1, "session-a"),
                Record(1, EnumCommandRecordKind.CommandSubmitted, 1, "session-b"),
            };
            RecordFilter filter = new RecordFilter();
            filter.SessionIds.Add("session-a");
            filter.RootCommandIds.Add(1);

            var result = RecordQuery.Apply(input, filter);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(record => record.SessionId == "session-a"), Is.True);
        }

        private static BehaviorRecord Record(
            ulong sequence,
            EnumBattleRecordKind kind,
            ulong? rootCommandId = null,
            string sessionId = "session-a",
            int turn = 1,
            int battleId = 1)
        {
            if (kind != EnumBattleRecordKind.BattleStarted)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return new BattleRecord(Metadata(sequence, rootCommandId, sessionId, turn, battleId));
        }

        private static BehaviorRecord Record(
            ulong sequence,
            EnumCommandRecordKind kind,
            ulong? rootCommandId = null,
            string sessionId = "session-a",
            int turn = 1,
            int battleId = 1)
        {
            return new CommandRecord(kind, Metadata(sequence, rootCommandId, sessionId, turn, battleId));
        }

        private static BehaviorRecord Record(
            ulong sequence,
            EnumActionRecordKind kind,
            ulong? rootCommandId = null,
            string sessionId = "session-a",
            int turn = 1,
            int battleId = 1)
        {
            return new ActionRecord(
                kind,
                Metadata(sequence, rootCommandId, sessionId, turn, battleId),
                new action.ActionId(1),
                "TestAction");
        }

        private static BehaviorRecord Record(
            ulong sequence,
            EnumHookRecordKind kind,
            ulong? rootCommandId = null,
            string sessionId = "session-a",
            int turn = 1,
            int battleId = 1)
        {
            return new HookRecord(
                kind,
                Metadata(sequence, rootCommandId, sessionId, turn, battleId),
                EnumHookType.AfterCardPlayed,
                new ActionEntityId(EnumEntityType.Player, 0),
                new ActionEntityId(EnumEntityType.Enemy, 0),
                0);
        }

        private static BehaviorRecordMetadata Metadata(
            ulong sequence,
            ulong? rootCommandId = null,
            string sessionId = "session-a",
            int turn = 1,
            int battleId = 1)
        {
            return new BehaviorRecordMetadata(
                sessionId,
                sequence,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                battleId,
                turn,
                rootCommandId);
        }

        private static BattleCommand NewCommand()
        {
            return new BattleCommand(
                EnumCommandType.PlayCard,
                new ActionEntityId(EnumEntityType.Player, 0),
                new CardInstanceId(1),
                new ActionEntityId(EnumEntityType.Enemy, 0));
        }

        private sealed class ThrowingSink : IBehaviorRecordSink
        {
            public void Append(BehaviorRecord record)
            {
                throw new InvalidOperationException("sink failed");
            }

            public void Flush()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class TrackingSink : IBehaviorRecordSink
        {
            public int AppendCount { get; private set; }
            public int FlushCount { get; private set; }

            public void Append(BehaviorRecord record)
            {
                AppendCount++;
            }

            public void Flush()
            {
                FlushCount++;
            }

            public void Dispose()
            {
            }
        }
    }
}
