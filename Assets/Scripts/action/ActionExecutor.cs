using System.Collections;
using System;
using enums;
using recording;
using UnityEngine;

namespace action
{
    // Todo: Make my own IEnumerator? Instead of Unity?? 
    public class ActionExecutor
    {
        public const int MaxActionsPerCommand = 1024;

        private readonly MonoBehaviour _host;
        private readonly BehaviorRecorder _recorder;
        private Coroutine _runningCoroutine;

        public bool IsRunning { get; private set; }
        public GameAction CurrentAction { get; private set; }
        public ulong CurrentRootCommandId { get; private set; }

        public ActionExecutor(MonoBehaviour host, BehaviorRecorder recorder)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        public void Kick(GameActionManager actionManager, BattleActionContext context, ulong rootCommandId)
        {
            if (actionManager == null) throw new ArgumentNullException(nameof(actionManager));
            if (context == null) throw new ArgumentNullException(nameof(context));
            // Note: Commands only execute at quiescence (BattleCommandApi.Pump), so a Kick can never
            //       overlap a running batch — an overlap means the serial-resolution rule
            //       (《260717-rule-multiplayer-battle-model》 §3) was broken upstream.
            if (IsRunning)
            {
                throw new InvalidOperationException("ActionExecutor was kicked while already running.");
            }
            _runningCoroutine = _host.StartCoroutine(Run(actionManager, context, rootCommandId));
        }

        private IEnumerator Run(GameActionManager actionManager, BattleActionContext context, ulong rootCommandId)
        {
            IsRunning = true;
            CurrentRootCommandId = rootCommandId;
            int executedActionCount = 0;
            bool commandFailed = false;

            try
            {
                while (actionManager.ActionQueue.Count > 0)
                {
                    if (executedActionCount >= MaxActionsPerCommand)
                    {
                        commandFailed = true;
                        context.BattleState.MarkFault(
                            $"Command {rootCommandId} expanded past {MaxActionsPerCommand} actions.");
                        Debug.LogError($"[ActionExecutor] Action expansion limit reached for command {rootCommandId}.");
                        break;
                    }

                    CurrentAction = actionManager.Pop();
                    CurrentAction.ActionStatus = EnumActionStatus.Executing;
                    _recorder.ActionStarted(CurrentAction, rootCommandId);
                    executedActionCount++;

                    IEnumerator actionRoutine = null;
                    Exception actionFailure = null;
                    try
                    {
                        actionRoutine = CurrentAction.Execute(context);
                    }
                    catch (Exception exception)
                    {
                        actionFailure = exception;
                    }

                    while (actionFailure == null && actionRoutine != null)
                    {
                        bool hasNext = false;
                        object yielded = null;
                        try
                        {
                            hasNext = actionRoutine.MoveNext();
                            if (hasNext)
                            {
                                yielded = actionRoutine.Current;
                            }
                        }
                        catch (Exception exception)
                        {
                            actionFailure = exception;
                        }

                        if (!hasNext)
                        {
                            break;
                        }
                        yield return yielded;
                    }

                    if (actionFailure == null && CurrentAction.ActionStatus != EnumActionStatus.Canceled)
                    {
                        try
                        {
                            context.BattleState.EvaluateOutcome();
                        }
                        catch (Exception exception)
                        {
                            actionFailure = exception;
                        }
                    }

                    if (actionFailure != null)
                    {
                        CurrentAction.ActionStatus = EnumActionStatus.Faulted;
                        _recorder.ActionFailed(CurrentAction, rootCommandId, actionFailure);
                        context.BattleState.MarkFault(
                            $"{CurrentAction.GetType().Name}#{CurrentAction.Id.Value} failed: {actionFailure.Message}");
                        Debug.LogException(actionFailure);
                        commandFailed = true;
                        break;
                    }

                    if (CurrentAction.ActionStatus == EnumActionStatus.Canceled)
                    {
                        _recorder.ActionCanceled(CurrentAction, rootCommandId);
                    }
                    else
                    {
                        CurrentAction.ActionStatus = EnumActionStatus.Finished;
                        _recorder.ActionCompleted(CurrentAction, rootCommandId);
                    }

                    CurrentAction = null;

                    if (context.BattleState.IsFaulted)
                    {
                        actionManager.ActionQueue.Clear();
                        break;
                    }
                }
            }
            finally
            {
                // Never leave the executor permanently busy, including if code outside the action
                // iterator throws unexpectedly.
                if (CurrentAction != null &&
                    CurrentAction.ActionStatus != EnumActionStatus.Finished &&
                    CurrentAction.ActionStatus != EnumActionStatus.Canceled &&
                    CurrentAction.ActionStatus != EnumActionStatus.Faulted)
                {
                    commandFailed = true;
                    CurrentAction.ActionStatus = EnumActionStatus.Faulted;
                    InvalidOperationException exception = new InvalidOperationException(
                        $"{CurrentAction.GetType().Name}#{CurrentAction.Id.Value} execution aborted unexpectedly.");
                    _recorder.ActionFailed(CurrentAction, rootCommandId, exception);
                    context.BattleState.MarkFault(exception.Message);
                }

                CurrentAction = null;
                IsRunning = false;
                CurrentRootCommandId = 0;
                _runningCoroutine = null;
                if (context.StateManager != null &&
                    (context.BattleState.IsBattleOver || context.BattleState.IsFaulted))
                {
                    context.StateManager.GameState.IsInBattle = false;
                }
                if (commandFailed || context.BattleState.IsFaulted)
                {
                    _recorder.CommandInterrupted(rootCommandId);
                }
                else
                {
                    _recorder.CommandCompleted(rootCommandId);
                }
            }
        }

        public void Reset()
        {
            if (_runningCoroutine != null)
            {
                _host.StopCoroutine(_runningCoroutine);
            }
            if (IsRunning && CurrentRootCommandId != 0)
            {
                _recorder.CommandInterrupted(CurrentRootCommandId);
            }
            _runningCoroutine = null;
            CurrentAction = null;
            CurrentRootCommandId = 0;
            IsRunning = false;
        }
    }
}
