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
    public sealed class DiscardHandCardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public ActionEntityId Player { get; }
        public CardInstanceId[] Cards { get; }
        public int DiscardedCount { get; private set; }
        public override string ResultSummary => $"player={Player.Id} discarded={DiscardedCount}";

        public DiscardHandCardAction(ActionEntityId player, CardInstanceId[] cards, ActionId id,
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
                    throw new InvalidOperationException($"Duplicate discard card #{card.Value}.");
                if (!player.HandCards.Contains(card))
                    throw new InvalidOperationException($"Card #{card.Value} is not in HandCards.");
                if (player.DiscardPile.Contains(card))
                    throw new InvalidOperationException($"Card #{card.Value} already exists in DiscardPile.");
            }

            for (int i = 0; i < Cards.Length; i++)
            {
                CardInstanceId card = Cards[i];
                BattlePileApi.MoveCard(player.HandCards, player.DiscardPile, card);
                DiscardedCount++;
                Debug.Log($"[DiscardHandCardAction] {player.PileManager.DescribeCard(card)} -> Discard({player.DiscardPile.Count})");
            }
            if (context.StateManager != null)
                hook.HookSystem.Fire(new hook.Hook(EnumHookType.AfterCardsDiscarded, Player, Player,
                    DiscardedCount), context);
            yield return null;
        }
    }
}
