using System.Collections;
using System;
using System.Collections.Generic;
using cards.instance;
using combat;
using enums;
using UnityEngine;

namespace action.gameEffectActions
{
    public sealed class HandCardChooseAction : GameAction
    {
        private readonly HashSet<CardInstanceId> _candidateSet;
        private bool _cancelRequested;

        public HandCardChooseAction(ulong choiceId, ActionEntityId choosingPlayer,
            CardInstanceId[] candidates, bool allowCancel, ActionId id, EnumActionStatus actionStatus)
        {
            if (choiceId == 0) throw new ArgumentOutOfRangeException(nameof(choiceId));
            if (candidates == null || candidates.Length == 0)
                throw new ArgumentException("At least one candidate card is required.", nameof(candidates));
            ChoiceId = choiceId;
            ChoosingPlayer = choosingPlayer;
            Candidates = (CardInstanceId[])candidates.Clone();
            _candidateSet = new HashSet<CardInstanceId>(Candidates);
            if (_candidateSet.Count != Candidates.Length)
                throw new ArgumentException("Candidate cards must be unique.", nameof(candidates));
            AllowCancel = allowCancel;
            Id = id;
            ActionStatus = actionStatus;
        }

        public override ActionId Id { get; }
        public override EnumActionStatus ActionStatus { get; set; }
        public ulong ChoiceId { get; }
        public ActionEntityId ChoosingPlayer { get; }
        public CardInstanceId[] Candidates { get; }
        public bool AllowCancel { get; }
        public CardInstanceId? SelectedCard { get; private set; }
        public override string ResultSummary => SelectedCard.HasValue
            ? $"choice={ChoiceId} player={ChoosingPlayer.Id} card={SelectedCard.Value.Value}"
            : $"choice={ChoiceId} player={ChoosingPlayer.Id} canceled={_cancelRequested}";

        public bool TryChoose(ActionEntityId player, CardInstanceId card)
        {
            if (ActionStatus != EnumActionStatus.GatheringPlayerChoice || player != ChoosingPlayer ||
                !_candidateSet.Contains(card))
            {
                return false;
            }
            SelectedCard = card;
            ActionStatus = EnumActionStatus.ReadyToResumeExecuting;
            return true;
        }

        public bool TryCancel(ActionEntityId player)
        {
            if (!AllowCancel || ActionStatus != EnumActionStatus.GatheringPlayerChoice ||
                player != ChoosingPlayer)
            {
                return false;
            }
            _cancelRequested = true;
            ActionStatus = EnumActionStatus.Canceled;
            return true;
        }

        public override IEnumerator Execute(BattleActionContext context)
        {
            ActionStatus = EnumActionStatus.GatheringPlayerChoice;
            yield return new WaitUntil(() => SelectedCard.HasValue || _cancelRequested);
        }
    }
}
