using action;
using action.gameEffectActions;
using cards.instance;
using combat;
using enums;
using gameStates.transient;
using tools.assert;
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
        public static void Pump(BattleCommandManager commandManager, BattleState battleState,
            GameActionManager actionManager, ActionExecutor actionExecutor)
        {
            if (actionExecutor.IsRunning || actionManager.ActionQueue.Count > 0)
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
                    ExecutePlayCard(command, battleState, actionManager, actionExecutor);
                    break;
                default:
                    MyAssert.Assert(false, $"Unhandled EnumCommandType: {command.Type}");
                    break;
            }
        }

        // Note: Shared by UI pre-check (grey out unplayable cards) and the authoritative check in
        //       ExecutePlayCard — one rule set, no drift. Read-only.
        public static EnumPlayCardResult CanPlayCard(BattleCommand command, BattleState battleState)
        {
            MyAssert.Assert(command.Player.Type == EnumEntityType.Player, $"PlayCard command from non-player entity {command.Player.Type}!");

            if (!battleState.IsPlayerTurn)
            {
                return EnumPlayCardResult.NotPlayerTurn;
            }

            BattlePlayerState player = (BattlePlayerState)EntityApi.Resolve(command.Player, battleState);
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

            return EnumPlayCardResult.Ok;
        }

        private static void ExecutePlayCard(BattleCommand command, BattleState battleState,
            GameActionManager actionManager, ActionExecutor actionExecutor)
        {
            EnumPlayCardResult result = CanPlayCard(command, battleState);
            if (result != EnumPlayCardResult.Ok)
            {
                Debug.Log($"[Command] PlayCard rejected ({result}): player#{command.Player.Id}, card#{command.Card.Value}");
                return;
            }

            BattlePlayerState player = (BattlePlayerState)EntityApi.Resolve(command.Player, battleState);
            CardInstance cardInstance = player.PileManager.Dictionary[command.Card];

            // Note: Energy cost is a card field (plan A). Synthesize the CostEnergyAction from it and queue
            //       it FIRST (pay to play, then effects resolve). The card's Effects never hold a CostEnergy
            //       effect. See 《260717-rule-multiplayer-battle-model》 §4-2 (cost is action-ified, not a
            //       direct mutation in this command function).
            actionManager.Add(new CostEnergyAction(cardInstance.EnergyCost, actionManager.NextId(), EnumActionStatus.WaitingForExecution));
            for (int i = 0; i < cardInstance.Effects.Length; i++)
            {
                EffectApi.Translate(cardInstance.Effects[i], command.Player, command.Target, actionManager);
            }
            actionManager.Add(new PlayedCardToDiscardAction(command.Card, actionManager.NextId(), EnumActionStatus.WaitingForExecution));
            // Todo: hook — Fire(AfterCardPlayed, source: command.Player) here once hook dispatch is rebuilt.

            Debug.Log($"[Command] PlayCard: player#{command.Player.Id} plays {player.PileManager.DescribeCard(command.Card)} -> {actionManager.ActionQueue.Count} actions queued");
            actionExecutor.Kick(actionManager, battleState, player);
        }
    }
}
