using System.Collections;
using System;
using combat;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class CostEnergyAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public int Value;
        public ActionEntityId Player;
        public int EnergyAfter { get; private set; }
        public override string ResultSummary => $"player={Player.Id} spent={Value} energyAfter={EnergyAfter}";

        public CostEnergyAction(int value, ActionEntityId player, ActionId id, EnumActionStatus actionStatus)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
            Player = player;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePlayerState player = EntityApi.Resolve(Player, context.BattleState) as BattlePlayerState
                ?? throw new InvalidOperationException($"Energy payer {Player.Type}#{Player.Id} is not a player.");
            if (player.PlayerEnergy < Value)
            {
                throw new InvalidOperationException(
                    $"Player#{Player.Id} has {player.PlayerEnergy} energy but action costs {Value}.");
            }
            player.PlayerEnergy -= Value;
            EnergyAfter = player.PlayerEnergy;
            Debug.Log($"[CostEnergyAction] player#{Player.Id} -{Value} -> PlayerEnergy = {player.PlayerEnergy}");
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterEnergySpent, Player, Player, Value), context);
            yield return null;
        }
    }
}
