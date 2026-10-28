using System.Collections;
using System;
using System.Collections.Generic;
using cards.instance;
using combat;
using enums;
using gameStates.transient;
using systems;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class ExhaustCardAction : GameAction
    {
        public ActionEntityId Player;
        public CardInstanceId[] Cards;
        public int ExhaustedCount { get; private set; }
        public override string ResultSummary => $"player={Player.Id} exhausted={ExhaustedCount}";
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }

        public ExhaustCardAction(ActionEntityId player, CardInstanceId[] cards, ActionId id,
            EnumActionStatus actionStatus)
        {
            if (cards == null || cards.Length == 0)
                throw new ArgumentException("At least one card is required.", nameof(cards));
            Player = player;
            Cards = (CardInstanceId[])cards.Clone();
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePlayerState player = EntityApi.Resolve(Player, context.BattleState) as BattlePlayerState
                ?? throw new InvalidOperationException($"Card owner {Player.Type}#{Player.Id} is not a player.");
            HashSet<CardInstanceId> seen = new HashSet<CardInstanceId>();
            for (int i = 0; i < Cards.Length; i++)
            {
                CardInstanceId card = Cards[i];
                if (!seen.Add(card))
                    throw new InvalidOperationException($"Duplicate exhaust card #{card.Value}.");
                bool inPlay = player.PlayPile.Contains(card);
                bool inHand = player.HandCards.Contains(card);
                if (inPlay == inHand)
                    throw new InvalidOperationException(
                        $"Card #{card.Value} must exist in exactly one of PlayPile or HandCards.");
                if (player.ExhaustedPile.Contains(card))
                    throw new InvalidOperationException($"Card #{card.Value} already exists in ExhaustedPile.");
            }

            for (int i = 0; i < Cards.Length; i++)
            {
                CardInstanceId card = Cards[i];
                bool moved = BattlePileApi.MoveCard(player.PlayPile, player.ExhaustedPile, card) ||
                             BattlePileApi.MoveCard(player.HandCards, player.ExhaustedPile, card);
                if (!moved)
                    throw new InvalidOperationException($"Card #{card.Value} disappeared during ExhaustCardAction.");
                ExhaustedCount++;
                Debug.Log($"[ExhaustCardAction] {player.PileManager.DescribeCard(card)} -> Exhaust({player.ExhaustedPile.Count})");
            }
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterCardsExhausted, Player, Player,
                    ExhaustedCount), context);
            yield return null;
        }
    }
}
