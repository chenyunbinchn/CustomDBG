using System.Collections;
using System.Collections.Generic;
using action;
using action.gameEffectActions;
using cards.instance;
using combat;
using enemy.instance;
using enums;
using gameStates.transient;
using NUnit.Framework;
using player;
using random;
using systems;

namespace action.tests
{
    public class GameActionTests
    {
        private BattleState _battle;
        private BattlePlayerState _player0;
        private BattlePlayerState _player1;
        private EnemyInstance _enemy;
        private BattleActionContext _context;

        [SetUp]
        public void SetUp()
        {
            _player0 = NewPlayer(0, 50);
            _player1 = NewPlayer(1, 50);
            _enemy = new EnemyInstance
            {
                Id = new ActionEntityId(EnumEntityType.Enemy, 0),
                Hp = 40,
            };
            _battle = new BattleState
            {
                Players = new[] { _player0, _player1 },
            };
            _battle.Reset(new List<EnemyInstance> { _enemy });

            SeedManager seeds = new SeedManager();
            seeds.Init(42u);
            RandomManager randomManager = new RandomManager();
            randomManager.Init(seeds);
            _context = new BattleActionContext(_battle, _player0, randomManager);
        }

        [Test]
        public void Damage_ConsumesBlockBeforeHp_AndRemovesFatalEnemy()
        {
            _enemy.Block = 4;
            DamageAction first = new DamageAction(10, _player0.Id, _enemy.Id, null,
                new ActionId(1), EnumActionStatus.WaitingForExecution);

            Execute(first);

            Assert.That(first.Result.Blocked, Is.EqualTo(4));
            Assert.That(first.Result.HpLoss, Is.EqualTo(6));
            Assert.That(_enemy.Block, Is.Zero);
            Assert.That(_enemy.Hp, Is.EqualTo(34));

            DamageAction fatal = new DamageAction(100, _player0.Id, _enemy.Id, null,
                new ActionId(2), EnumActionStatus.WaitingForExecution);
            Execute(fatal);

            Assert.That(fatal.Result.WasFatal, Is.True);
            Assert.That(_enemy.Hp, Is.Zero);
            Assert.That(_battle.EnemyList, Is.Empty);
        }

        [Test]
        public void Damage_UsesPowerWeakAndVulnerableInStableOrder()
        {
            _player0.StatusStacks[EnumStatusType.Power] = 2;
            _player0.StatusStacks[EnumStatusType.Weak] = 1;
            _enemy.StatusStacks[EnumStatusType.Vulnerable] = 1;
            DamageAction action = new DamageAction(6, _player0.Id, _enemy.Id, null,
                new ActionId(1), EnumActionStatus.WaitingForExecution);

            Execute(action);

            Assert.That(action.Result.FinalAmount, Is.EqualTo(9)); // (6 + 2) * 3/4 * 3/2
            Assert.That(_enemy.Hp, Is.EqualTo(31));
        }

        [Test]
        public void EnergyActions_UseExplicitPlayerTarget()
        {
            _player0.PlayerEnergy = 3;
            _player1.PlayerEnergy = 1;

            Execute(new CostEnergyAction(2, _player0.Id, new ActionId(1),
                EnumActionStatus.WaitingForExecution));
            Execute(new GainEnergyAction(4, _player0.Id, _player1.Id, new ActionId(2),
                EnumActionStatus.WaitingForExecution));

            Assert.That(_player0.PlayerEnergy, Is.EqualTo(1));
            Assert.That(_player1.PlayerEnergy, Is.EqualTo(5));
        }

        [Test]
        public void GainBlock_RepairsNegativeBlockAndUsesExplicitTarget()
        {
            _player0.Block = 9;
            _player1.Block = -3;
            GainBlockAction action = new GainBlockAction(5, _player0.Id, _player1.Id, null,
                new ActionId(1), EnumActionStatus.WaitingForExecution);

            Execute(action);

            Assert.That(_player0.Block, Is.EqualTo(9));
            Assert.That(_player1.Block, Is.EqualTo(5));
            Assert.That(action.ActualGained, Is.EqualTo(5));
        }

        [Test]
        public void Draw_ReshufflesAndStopsAtHandLimit()
        {
            for (uint i = 1; i <= 9; i++) _player0.HandCards.Add(new CardInstanceId(i));
            _player0.DiscardPile.Add(new CardInstanceId(10));
            _player0.DiscardPile.Add(new CardInstanceId(11));
            DrawCardAction action = new DrawCardAction(2, _player0.Id, _player0.Id,
                new ActionId(1), EnumActionStatus.WaitingForExecution);

            Execute(action);

            Assert.That(action.Result.DrawnCards.Count, Is.EqualTo(1));
            Assert.That(action.Result.ReshuffleCount, Is.EqualTo(1));
            Assert.That(action.Result.StoppedReason, Is.EqualTo(EnumDrawStoppedReason.HandFull));
            Assert.That(_player0.HandCards.Count, Is.EqualTo(BattlePileApi.MaxHandSize));
            Assert.That(_player0.DrawPile.Count, Is.EqualTo(1));
        }

        [Test]
        public void PlayedCard_UsesPlayPile_AndExhaustPreventsDiscard()
        {
            CardInstanceId card = new CardInstanceId(1);
            _player0.HandCards.Add(card);

            Execute(new HandCardToPlayAction(_player0.Id, card, new ActionId(1),
                EnumActionStatus.WaitingForExecution));
            Execute(new ExhaustCardAction(_player0.Id, new[] { card }, new ActionId(2),
                EnumActionStatus.WaitingForExecution));
            PlayedCardToDiscardAction finalize = new PlayedCardToDiscardAction(
                _player0.Id, card, _enemy.Id, new ActionId(3), EnumActionStatus.WaitingForExecution);
            Execute(finalize);

            Assert.That(_player0.HandCards, Is.Empty);
            Assert.That(_player0.PlayPile, Is.Empty);
            Assert.That(_player0.DiscardPile, Is.Empty);
            Assert.That(_player0.ExhaustedPile, Is.EqualTo(new[] { card }));
            Assert.That(finalize.Destination, Is.EqualTo("Exhaust"));
        }

        [Test]
        public void PlayedCard_NormalFinalizationMovesPlayToDiscard()
        {
            CardInstanceId card = new CardInstanceId(1);
            _player0.HandCards.Add(card);

            Execute(new HandCardToPlayAction(_player0.Id, card, new ActionId(1),
                EnumActionStatus.WaitingForExecution));
            PlayedCardToDiscardAction finalize = new PlayedCardToDiscardAction(
                _player0.Id, card, _enemy.Id, new ActionId(2), EnumActionStatus.WaitingForExecution);
            Execute(finalize);

            Assert.That(_player0.PlayPile, Is.Empty);
            Assert.That(_player0.DiscardPile, Is.EqualTo(new[] { card }));
            Assert.That(finalize.Destination, Is.EqualTo("Discard"));
        }

        [Test]
        public void Discard_MovesStableCardIds()
        {
            CardInstanceId first = new CardInstanceId(1);
            CardInstanceId second = new CardInstanceId(2);
            _player0.HandCards.Add(first);
            _player0.HandCards.Add(second);
            DiscardHandCardAction action = new DiscardHandCardAction(_player0.Id,
                new[] { second }, new ActionId(1), EnumActionStatus.WaitingForExecution);

            Execute(action);

            Assert.That(_player0.HandCards, Is.EqualTo(new[] { first }));
            Assert.That(_player0.DiscardPile, Is.EqualTo(new[] { second }));
            Assert.That(action.DiscardedCount, Is.EqualTo(1));
        }

        [Test]
        public void MultiCardPileActions_ValidateBeforeMutating()
        {
            CardInstanceId first = new CardInstanceId(1);
            CardInstanceId missing = new CardInstanceId(2);
            _player0.HandCards.Add(first);
            DiscardHandCardAction discard = new DiscardHandCardAction(_player0.Id,
                new[] { first, missing }, new ActionId(1), EnumActionStatus.WaitingForExecution);

            Assert.That(() => Execute(discard), Throws.InvalidOperationException);
            Assert.That(_player0.HandCards, Is.EqualTo(new[] { first }));
            Assert.That(_player0.DiscardPile, Is.Empty);

            ExhaustCardAction exhaust = new ExhaustCardAction(_player0.Id,
                new[] { first, missing }, new ActionId(2), EnumActionStatus.WaitingForExecution);
            Assert.That(() => Execute(exhaust), Throws.InvalidOperationException);
            Assert.That(_player0.HandCards, Is.EqualTo(new[] { first }));
            Assert.That(_player0.ExhaustedPile, Is.Empty);
        }

        [Test]
        public void ApplyStatus_StacksAmountAndKeepsOneListener()
        {
            Execute(new ApplyStatusAction(EnumStatusType.Afterimage, 2, _player0.Id, _player0.Id,
                null, new ActionId(1), EnumActionStatus.WaitingForExecution));
            Execute(new ApplyStatusAction(EnumStatusType.Afterimage, 3, _player0.Id, _player0.Id,
                null, new ActionId(2), EnumActionStatus.WaitingForExecution));

            Assert.That(_player0.StatusStacks[EnumStatusType.Afterimage], Is.EqualTo(5));
            Assert.That(_player0.HookListeners.Count, Is.EqualTo(1));
            Assert.That(_player0.HookListeners[0].Effect.Value, Is.EqualTo(5));
        }

        [Test]
        public void Choice_AcceptsOnlyMatchingPlayerAndCandidateId()
        {
            CardInstanceId candidate = new CardInstanceId(7);
            HandCardChooseAction action = new HandCardChooseAction(99, _player0.Id,
                new[] { candidate }, true, new ActionId(1), EnumActionStatus.WaitingForExecution);
            IEnumerator routine = action.Execute(_context);
            Assert.That(routine.MoveNext(), Is.True);

            Assert.That(action.TryChoose(_player1.Id, candidate), Is.False);
            Assert.That(action.TryChoose(_player0.Id, new CardInstanceId(8)), Is.False);
            Assert.That(action.TryChoose(_player0.Id, candidate), Is.True);
            Assert.That(action.SelectedCard, Is.EqualTo(candidate));
            Assert.That(action.ActionStatus, Is.EqualTo(EnumActionStatus.ReadyToResumeExecuting));
        }

        [Test]
        public void Choice_CancelRequiresPermissionAndMatchingPlayer()
        {
            HandCardChooseAction action = new HandCardChooseAction(99, _player0.Id,
                new[] { new CardInstanceId(7) }, true, new ActionId(1),
                EnumActionStatus.WaitingForExecution);
            IEnumerator routine = action.Execute(_context);
            Assert.That(routine.MoveNext(), Is.True);

            Assert.That(action.TryCancel(_player1.Id), Is.False);
            Assert.That(action.TryCancel(_player0.Id), Is.True);
            Assert.That(action.ActionStatus, Is.EqualTo(EnumActionStatus.Canceled));
            Assert.That(routine.MoveNext(), Is.False);
        }

        [Test]
        public void NumericActions_RejectNegativeValues()
        {
            Assert.That(() => new DamageAction(-1, _player0.Id, _enemy.Id, null,
                new ActionId(1), EnumActionStatus.WaitingForExecution), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new GainBlockAction(-1, _player0.Id, _enemy.Id, null,
                new ActionId(2), EnumActionStatus.WaitingForExecution), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new CostEnergyAction(-1, _player0.Id,
                new ActionId(3), EnumActionStatus.WaitingForExecution), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new GainEnergyAction(-1, _player0.Id, _player1.Id,
                new ActionId(4), EnumActionStatus.WaitingForExecution), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new DrawCardAction(-1, _player0.Id, _player0.Id,
                new ActionId(5), EnumActionStatus.WaitingForExecution), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private static BattlePlayerState NewPlayer(uint id, int hp)
        {
            return new BattlePlayerState(new PlayerInfo(
                hp,
                3,
                0,
                new ActionEntityId(EnumEntityType.Player, id)));
        }

        private void Execute(GameAction action)
        {
            IEnumerator routine = action.Execute(_context);
            while (routine.MoveNext())
            {
            }
        }
    }
}
