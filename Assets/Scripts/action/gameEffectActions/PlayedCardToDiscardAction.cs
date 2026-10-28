using System.Collections;
using System;
using cards.instance;
using combat;
using enums;
using gameStates.transient;
using hook;
using systems;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class HandCardToPlayAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public ActionEntityId Player { get; }
        public CardInstanceId Card { get; }
        public override string ResultSummary => $"player={Player.Id} card={Card.Value} destination=Play";

        public HandCardToPlayAction(ActionEntityId player, CardInstanceId card, ActionId id,
            EnumActionStatus actionStatus)
        {
            Player = player;
            Card = card;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePlayerState player = EntityApi.Resolve(Player, context.BattleState) as BattlePlayerState
                ?? throw new InvalidOperationException($"Card owner {Player.Type}#{Player.Id} is not a player.");
            if (!BattlePileApi.MoveCard(player.HandCards, player.PlayPile, Card))
            {
                throw new InvalidOperationException($"Card #{Card.Value} is not in player#{Player.Id}'s hand.");
            }
            Debug.Log($"[HandCardToPlayAction] {player.PileManager.DescribeCard(Card)} -> Play({player.PlayPile.Count})");
            yield return null;
        }
    }

    // Note: Move the played card from hand to discard. Deliberately distinct from
    //       DiscardHandCardAction (the "discard a card" effect): playing a card must NOT count as
    //       discarding — they will trigger different hooks. See 《260717-rule-multiplayer-battle-model》 §4-3.
    public sealed class PlayedCardToDiscardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public ActionEntityId Player;
        public CardInstanceId Card;
        public ActionEntityId PlayedTarget;
        public string Destination { get; private set; }
        public override string ResultSummary => $"player={Player.Id} card={Card.Value} destination={Destination}";

        public PlayedCardToDiscardAction(ActionEntityId player, CardInstanceId card, ActionEntityId playedTarget,
            ActionId id, EnumActionStatus actionStatus)
        {
            Player = player;
            Card = card;
            PlayedTarget = playedTarget;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            BattlePlayerState player = EntityApi.Resolve(Player, context.BattleState) as BattlePlayerState
                ?? throw new InvalidOperationException($"Card owner {Player.Type}#{Player.Id} is not a player.");
            bool discarded = BattlePileApi.MoveCard(player.PlayPile, player.DiscardPile, Card);
            if (discarded)
            {
                Destination = "Discard";
                Debug.Log($"[PlayedCardToDiscardAction] {player.PileManager.DescribeCard(Card)} -> Discard({player.DiscardPile.Count})");
            }
            else if (player.ExhaustedPile.Contains(Card))
            {
                Destination = "Exhaust";
                Debug.Log($"[PlayedCardToDiscardAction] {player.PileManager.DescribeCard(Card)} already exhausted; skip discard");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Played card #{Card.Value} is in neither PlayPile nor ExhaustedPile.");
            }

            if (context.StateManager != null)
            {
                HookSystem.Fire(
                    new Hook(EnumHookType.AfterCardPlayed, Player, PlayedTarget, 0),
                    context);
            }
            yield return null;
        }
    }
}
