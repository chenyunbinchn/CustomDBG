using System.Collections;
using cards.instance;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action.gameEffectActions
{
    // Note: Move the played card from hand to discard. Deliberately distinct from
    //       DiscardHandCardAction (the "discard a card" effect): playing a card must NOT count as
    //       discarding — they will trigger different hooks. See 《260717-rule-multiplayer-battle-model》 §4-3.
    public sealed class PlayedCardToDiscardAction : GameAction
    {
        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public CardInstanceId Card;

        public PlayedCardToDiscardAction(CardInstanceId card, ActionId id, EnumActionStatus actionStatus)
        {
            Card = card;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override IEnumerator Execute(BattleState battleState, BattlePlayerState playerState)
        {
            bool removed = playerState.HandCards.Remove(Card);
            if (removed)
            {
                playerState.DiscardPile.Add(Card);
                Debug.Log($"[PlayedCardToDiscardAction] {playerState.PileManager.DescribeCard(Card)} -> Discard({playerState.DiscardPile.Count})");
            }
            else
            {
                // Note: an earlier action of the SAME command may have moved the card already
                //       (e.g. an exhaust/discard effect on the played card itself) — tolerated.
                Debug.Log($"[PlayedCardToDiscardAction] {playerState.PileManager.DescribeCard(Card)} no longer in hand, skip");
            }
            yield return null;
        }
    }
}
