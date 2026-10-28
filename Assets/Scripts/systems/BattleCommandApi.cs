using action;
using action.gameEffectActions;
using cards.instance;
using combat;
using enums;
using gameStates;
using gameStates.transient;
using recording;
using UnityEngine;

namespace systems
{
    // Note: THE single entry point for battle input. Local UI and (future) remote network messages
    //       both Submit the same BattleCommand; nothing mutates battle state directly from input.
    //       Pump runs at most ONE command per call and only at quiescence (executor idle) — one
    //       command's actions fully resolve before the next command starts.
    //       See 《260717-rule-multiplayer-battle-model》 §2/§3.
    public static class BattleCommandApi
    {
        public static void Submit(BattleCommand command, BattleCommandManager commandManager)
        {
            commandManager.Enqueue(command);
        }

        // Note: Call once per frame. Authoritative validation happens HERE (dequeue time), not at
        //       submit — an earlier command may have changed what is legal. An invalid command is
        //       dropped; every end drops it identically (same state + same rules = same verdict).
        //       Takes StateManager because firing hooks needs to reach every listener host.
        public static void Pump(StateManager stateManager)
        {
            GameActionManager actionManager = stateManager.GameState.GameActionManager;
            ActionExecutor actionExecutor = stateManager.GameState.ActionExecutor;
            BattleCommandManager commandManager = stateManager.GameState.BattleCommandManager;

            if (actionExecutor.IsRunning)
            {
                if (actionExecutor.CurrentAction is HandCardChooseAction choiceAction &&
                    commandManager.TryDequeueChoice(out BattleCommand choiceCommand))
                {
                    ExecuteChoice(choiceCommand, choiceAction, stateManager);
                }
                return;
            }
            if (actionManager.ActionQueue.Count > 0)
            {
                return;
            }
            if (!commandManager.HasPending())
            {
                return;
            }

            BattleCommand command = commandManager.Dequeue();
            switch (command.Type)
            {
                case EnumCommandType.PlayCard:
                    ExecutePlayCard(command, stateManager);
                    break;
                case EnumCommandType.ChooseCard:
                case EnumCommandType.CancelChoice:
                    stateManager.GameState.BehaviorRecorder.CommandRejected(
                        command, EnumPlayCardResult.NoPendingChoice);
                    break;
                default:
                    stateManager.GameState.BehaviorRecorder.CommandRejected(
                        command, EnumPlayCardResult.InvalidError);
                    Debug.LogWarning($"[Command] Rejected unsupported command type: {command.Type}");
                    break;
            }
        }

        // Note: Shared by UI pre-check (grey out unplayable cards) and the authoritative check in
        //       ExecutePlayCard — one rule set, no drift. Read-only.
        public static EnumPlayCardResult CanPlayCard(BattleCommand command, BattleState battleState)
        {
            if (battleState.IsFaulted)
            {
                return EnumPlayCardResult.BattleFaulted;
            }
            if (battleState.IsBattleOver)
            {
                return EnumPlayCardResult.BattleEnded;
            }
            if (command.Player.Type != EnumEntityType.Player ||
                !EntityApi.TryResolve(command.Player, battleState, out ICombatActor actor) ||
                !(actor is BattlePlayerState player))
            {
                return EnumPlayCardResult.InvalidPlayer;
            }
            if (player.Hp <= 0)
            {
                return EnumPlayCardResult.TargetDead;
            }

            if (!battleState.IsPlayerTurn)
            {
                return EnumPlayCardResult.NotPlayerTurn;
            }

            bool found = player.PileManager.Dictionary.TryGetValue(command.Card, out CardInstance cardInstance);
            if (!found)
            {
                return EnumPlayCardResult.CardNotFound;
            }
            if (!player.HandCards.Contains(command.Card))
            {
                return EnumPlayCardResult.CardNotInHand;
            }

            if (cardInstance.EnergyCost > player.PlayerEnergy)
            {
                return EnumPlayCardResult.NotEnoughEnergy;
            }

            for (int i = 0; i < cardInstance.Effects.Length; i++)
            {
                if (!EffectApi.IsTargetValid(
                        cardInstance.Effects[i], command.Player, command.Target, command.Card, battleState))
                {
                    return EnumPlayCardResult.InvalidTarget;
                }
            }

            return EnumPlayCardResult.Ok;
        }

        private static void ExecutePlayCard(BattleCommand command, StateManager stateManager)
        {
            BattleState battleState = stateManager.BattleState;
            GameActionManager actionManager = stateManager.GameState.GameActionManager;

            EnumPlayCardResult result = CanPlayCard(command, battleState);
            if (result != EnumPlayCardResult.Ok)
            {
                stateManager.GameState.BehaviorRecorder.CommandRejected(command, result);
                Debug.Log($"[Command] PlayCard rejected ({result}): player#{command.Player.Id}, card#{command.Card.Value}");
                return;
            }

            BattlePlayerState player = (BattlePlayerState)EntityApi.Resolve(command.Player, battleState);
            CardInstance cardInstance = player.PileManager.Dictionary[command.Card];
            BehaviorRecorder recorder = stateManager.GameState.BehaviorRecorder;
            recorder.CommandAccepted(command);
            BattleActionContext actionContext = new BattleActionContext(stateManager, player, command.CommandId);

            try
            {
            // Note: Energy cost is a card field (plan A). Synthesize the CostEnergyAction from it and queue
            //       it FIRST (pay to play, then effects resolve). The card's Effects never hold a CostEnergy
            //       effect. See 《260717-rule-multiplayer-battle-model》 §4-2 (cost is action-ified, not a
            //       direct mutation in this command function).
            actionManager.Add(
                new HandCardToPlayAction(command.Player, command.Card, actionManager.NextId(),
                    EnumActionStatus.WaitingForExecution),
                command.CommandId);
            actionManager.Add(
                new CostEnergyAction(cardInstance.EnergyCost, command.Player, actionManager.NextId(),
                    EnumActionStatus.WaitingForExecution),
                command.CommandId);
            for (int i = 0; i < cardInstance.Effects.Length; i++)
            {
                EffectApi.Translate(cardInstance.Effects[i], command.Player, command.Target, command.Card,
                    actionContext);
            }
            actionManager.Add(
                new PlayedCardToDiscardAction(command.Player, command.Card, command.Target,
                    actionManager.NextId(), EnumActionStatus.WaitingForExecution),
                command.CommandId);

            Debug.Log($"[Command] PlayCard: player#{command.Player.Id} plays {player.PileManager.DescribeCard(command.Card)} -> {actionManager.ActionQueue.Count} actions queued");
            stateManager.GameState.ActionExecutor.Kick(actionManager, actionContext, command.CommandId);
            }
            catch (System.Exception exception)
            {
                actionManager.ActionQueue.Clear();
                battleState.MarkFault($"Failed to expand PlayCard command {command.CommandId}: {exception.Message}");
                recorder.CommandInterrupted(command.CommandId);
                Debug.LogException(exception);
            }
        }

        private static void ExecuteChoice(BattleCommand command, HandCardChooseAction choiceAction,
            StateManager stateManager)
        {
            bool accepted = command.ChoiceId == choiceAction.ChoiceId &&
                            (command.Type == EnumCommandType.ChooseCard
                                ? choiceAction.TryChoose(command.Player, command.Card)
                                : choiceAction.TryCancel(command.Player));
            if (!accepted)
            {
                stateManager.GameState.BehaviorRecorder.CommandRejected(
                    command, EnumPlayCardResult.InvalidChoice);
                return;
            }

            stateManager.GameState.BehaviorRecorder.CommandAccepted(command);
            stateManager.GameState.BehaviorRecorder.CommandCompleted(command.CommandId);
        }
    }
}
