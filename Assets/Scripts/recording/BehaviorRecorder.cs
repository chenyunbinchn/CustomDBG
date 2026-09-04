using System;
using System.Collections.Generic;
using action;
using combat;
using enums;
using hook;
using recording.enums;
using UnityEngine;

namespace recording
{
    // Note: Public methods describe facts, not storage details. Every sink boundary is isolated so
    //       recording can never change command validation, queue order, or Action execution.
    public sealed class BehaviorRecorder : IDisposable
    {
        private readonly List<IBehaviorRecordSink> _activeSinks = new List<IBehaviorRecordSink>();
        private readonly HashSet<ulong> _openCommandIds = new HashSet<ulong>();
        private readonly string _sessionId;
        private ulong _sequence;
        private int _battleId;
        private int _turn;
        private bool _disposed;

        public string SessionId => _sessionId;
        public bool HasActiveSink => _activeSinks.Count > 0;
        public bool IsLogComplete { get; private set; } = true;
        public string LastWriteError { get; private set; }

        public BehaviorRecorder(params IBehaviorRecordSink[] sinks)
        {
            _sessionId = Guid.NewGuid().ToString("N");
            if (sinks != null)
            {
                for (int i = 0; i < sinks.Length; i++)
                {
                    if (sinks[i] != null)
                    {
                        _activeSinks.Add(sinks[i]);
                    }
                }
            }

            if (_activeSinks.Count == 0)
            {
                IsLogComplete = false;
                LastWriteError = "No behavior record sink is available.";
            }
        }

        public void BattleStarted(int turn)
        {
            if (_disposed)
            {
                return;
            }

            _battleId++;
            _turn = turn;
            Append(new BattleRecord(CreateMetadata()));
        }

        public void CommandSubmitted(in BattleCommand command)
        {
            RecordCommand(EnumCommandRecordKind.CommandSubmitted, command, null);
        }

        public void CommandAccepted(in BattleCommand command)
        {
            if (_disposed)
            {
                return;
            }

            if (command.CommandId != 0)
            {
                _openCommandIds.Add(command.CommandId);
            }
            RecordCommand(EnumCommandRecordKind.CommandAccepted, command, EnumPlayCardResult.Ok);
        }

        public void CommandRejected(in BattleCommand command, EnumPlayCardResult result)
        {
            if (command.CommandId != 0)
            {
                _openCommandIds.Remove(command.CommandId);
            }
            RecordCommand(EnumCommandRecordKind.CommandRejected, command, result);
            Flush();
        }

        public void ActionQueued(GameAction action, ulong rootCommandId)
        {
            RecordAction(EnumActionRecordKind.ActionQueued, action, rootCommandId);
        }

        public void ActionStarted(GameAction action, ulong rootCommandId)
        {
            RecordAction(EnumActionRecordKind.ActionStarted, action, rootCommandId);
        }

        public void ActionCompleted(GameAction action, ulong rootCommandId)
        {
            RecordAction(EnumActionRecordKind.ActionCompleted, action, rootCommandId);
        }

        public void HookFired(in Hook firedHook, ulong rootCommandId)
        {
            RecordHook(EnumHookRecordKind.HookFired, firedHook, null, rootCommandId);
        }

        public void HookMatched(in Hook firedHook, HookListener listener, ulong rootCommandId)
        {
            RecordHook(EnumHookRecordKind.HookMatched, firedHook, listener, rootCommandId);
        }

        public void CommandCompleted(ulong rootCommandId)
        {
            if (_disposed)
            {
                return;
            }

            _openCommandIds.Remove(rootCommandId);
            Append(new CommandRecord(
                EnumCommandRecordKind.CommandCompleted,
                CreateMetadata(rootCommandId)));
            Flush();
        }

        public void Flush()
        {
            for (int i = _activeSinks.Count - 1; i >= 0; i--)
            {
                try
                {
                    _activeSinks[i].Flush();
                }
                catch (Exception exception)
                {
                    DisableSink(i, exception);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_openCommandIds.Count > 0)
            {
                List<ulong> interruptedCommandIds = new List<ulong>(_openCommandIds);
                interruptedCommandIds.Sort();
                for (int i = 0; i < interruptedCommandIds.Count; i++)
                {
                    Append(new CommandRecord(
                        EnumCommandRecordKind.CommandInterrupted,
                        CreateMetadata(interruptedCommandIds[i])));
                }
                _openCommandIds.Clear();
            }

            Flush();
            for (int i = _activeSinks.Count - 1; i >= 0; i--)
            {
                try
                {
                    _activeSinks[i].Dispose();
                }
                catch (Exception exception)
                {
                    MarkIncomplete(exception);
                }
            }

            _activeSinks.Clear();
            _disposed = true;
        }

        private void RecordCommand(
            EnumCommandRecordKind kind,
            in BattleCommand command,
            EnumPlayCardResult? result)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Append(new CommandRecord(
                    kind,
                    CreateMetadata(command.CommandId),
                    command.Type,
                    result,
                    command.Player,
                    command.Target,
                    command.Card));
            }
            catch (Exception exception)
            {
                MarkIncomplete(exception);
            }
        }

        private void RecordAction(EnumActionRecordKind kind, GameAction action, ulong rootCommandId)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Append(new ActionRecord(
                    kind,
                    CreateMetadata(rootCommandId),
                    action.Id,
                    action.GetType().Name));
            }
            catch (Exception exception)
            {
                MarkIncomplete(exception);
            }
        }

        private void RecordHook(
            EnumHookRecordKind kind,
            in Hook firedHook,
            HookListener listener,
            ulong rootCommandId)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Append(new HookRecord(
                    kind,
                    CreateMetadata(rootCommandId),
                    firedHook.HookType,
                    firedHook.Source,
                    firedHook.Target,
                    firedHook.Value,
                    listener?.Owner,
                    listener?.Filter));
            }
            catch (Exception exception)
            {
                MarkIncomplete(exception);
            }
        }

        private BehaviorRecordMetadata CreateMetadata(ulong? rootCommandId = null)
        {
            return new BehaviorRecordMetadata(
                _sessionId,
                ++_sequence,
                DateTime.UtcNow,
                _battleId,
                _turn,
                rootCommandId);
        }

        private void Append(BehaviorRecord record)
        {
            if (_disposed)
            {
                return;
            }

            for (int i = _activeSinks.Count - 1; i >= 0; i--)
            {
                try
                {
                    _activeSinks[i].Append(record);
                }
                catch (Exception exception)
                {
                    DisableSink(i, exception);
                }
            }
        }

        private void DisableSink(int index, Exception exception)
        {
            IBehaviorRecordSink failedSink = _activeSinks[index];
            _activeSinks.RemoveAt(index);
            try
            {
                failedSink.Dispose();
            }
            catch (Exception disposeException)
            {
                MarkIncomplete(disposeException);
            }

            MarkIncomplete(exception);
        }

        private void MarkIncomplete(Exception exception)
        {
            IsLogComplete = false;
            LastWriteError = exception?.Message ?? "Unknown recorder error.";
            try
            {
                Debug.LogError($"[BehaviorRecorder] Recording disabled for one sink: {LastWriteError}");
            }
            catch
            {
                // Recording failures must never escape into the game simulation.
            }
        }
    }
}
