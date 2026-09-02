using System;
using System.Collections.Generic;
using cards.definition;
using cards.instance;
using combat;
using enums;
using gameStates;
using gameStates.transient;
using systems;
using tools.assert;
using ui.core;
using ui.intents;
using ui.viewModels;
using UnityEngine;

namespace ui.presenters
{
    // Converts battle snapshots to ViewModels and UI intents to BattleCommands.
    public sealed class BattlePresenter : IDisposable
    {
        private readonly StateManager _stateManager;
        private readonly int _playerIndex;
        private readonly string _cardPileDialogAddress;

        private BattleHUD _hud;
        private bool _waitingForCommand;
        private bool _refreshRequested;

        public BattlePresenter(StateManager stateManager, int playerIndex, string cardPileDialogAddress)
        {
            MyAssert.Assert(stateManager != null, "BattlePresenter requires StateManager.");
            MyAssert.Assert(playerIndex >= 0 && playerIndex < stateManager.BattlePlayerStates.Length,
                $"BattlePresenter player index is out of range: {playerIndex}");
            MyAssert.Assert(!string.IsNullOrWhiteSpace(cardPileDialogAddress),
                "BattlePresenter requires a card pile dialog address.");

            _stateManager = stateManager;
            _playerIndex = playerIndex;
            _cardPileDialogAddress = cardPileDialogAddress;
        }

        public bool ShowHud(string battleHudAddress)
        {
            UIManager manager = UIManager.Instance;
            MyAssert.Assert(manager != null, "BattlePresenter requires an active UIManager.");
            if (manager == null)
            {
                return false;
            }

            return manager.ShowHud(battleHudAddress, CreateHudViewModel(), HandleHudOpened);
        }

        public void Tick()
        {
            if (_hud == null)
            {
                return;
            }

            if (_waitingForCommand && IsBattleStable())
            {
                _waitingForCommand = false;
                _refreshRequested = true;
            }

            UIManager manager = UIManager.Instance;
            if (_refreshRequested && manager != null && !manager.IsBusy)
            {
                _refreshRequested = false;
                manager.Refresh(_hud, CreateHudViewModel());
            }
        }

        public void Refresh()
        {
            _refreshRequested = true;
        }

        public BattleHudViewModel CreateHudViewModel()
        {
            BattlePlayerState player = GetPlayer();
            CardViewModel[] handCards = BuildCards(player.HandCards, true, false);
            return new BattleHudViewModel(
                new HandViewModel(handCards),
                player.PlayerEnergy.ToString(),
                player.DrawPile.Count.ToString(),
                player.DiscardPile.Count.ToString());
        }

        public void Dispose()
        {
            DetachHud();
        }

        private void HandleHudOpened(UIView view)
        {
            BattleHUD hud = view as BattleHUD;
            MyAssert.Assert(hud != null, "Battle HUD prefab root requires BattleHUD.");
            if (hud == null)
            {
                return;
            }

            AttachHud(hud);
        }

        private void AttachHud(BattleHUD hud)
        {
            DetachHud();
            _hud = hud;
            _hud.PlayCardRequested += HandlePlayCardRequested;
            _hud.ViewPileRequested += HandleViewPileRequested;
            _hud.Closed += HandleHudClosed;
        }

        private void DetachHud()
        {
            if (_hud == null)
            {
                return;
            }

            _hud.PlayCardRequested -= HandlePlayCardRequested;
            _hud.ViewPileRequested -= HandleViewPileRequested;
            _hud.Closed -= HandleHudClosed;
            _hud = null;
        }

        private void HandleHudClosed()
        {
            DetachHud();
        }

        private void HandlePlayCardRequested(PlayCardIntent intent)
        {
            if (_waitingForCommand)
            {
                return;
            }

            BattleCommand command = CreatePlayCardCommand(intent.Card);
            EnumPlayCardResult result = BattleCommandApi.CanPlayCard(command, _stateManager.BattleState);
            if (result != EnumPlayCardResult.Ok)
            {
                _refreshRequested = true;
                return;
            }

            BattleCommandApi.Submit(command, _stateManager.GameState.BattleCommandManager);
            _waitingForCommand = true;
        }

        private void HandleViewPileRequested(ViewCardPileIntent intent)
        {
            CardPileViewModel viewModel = CreatePileViewModel(intent.Pile);
            UIManager manager = UIManager.Instance;
            if (manager != null)
            {
                manager.PushModal(_cardPileDialogAddress, viewModel);
            }
        }

        private CardPileViewModel CreatePileViewModel(EnumCardPileKind pileKind)
        {
            BattlePlayerState player = GetPlayer();
            List<CardInstanceId> source;
            string title;
            switch (pileKind)
            {
                case EnumCardPileKind.Draw:
                    source = player.DrawPile;
                    title = "Draw Pile";
                    break;
                case EnumCardPileKind.Discard:
                    source = player.DiscardPile;
                    title = "Discard Pile";
                    break;
                default:
                    MyAssert.Assert(false, $"Unhandled card pile kind: {pileKind}");
                    source = player.DrawPile;
                    title = "Cards";
                    break;
            }

            // Both piles use their tail as the visible top, so the dialog shows cards in reverse order.
            return new CardPileViewModel(title, BuildCards(source, false, true));
        }

        private CardViewModel[] BuildCards(List<CardInstanceId> cardIds, bool checkPlayable, bool reverse)
        {
            CardViewModel[] result = new CardViewModel[cardIds.Count];
            for (int i = 0; i < cardIds.Count; i++)
            {
                int sourceIndex = reverse ? cardIds.Count - 1 - i : i;
                result[i] = CreateCardViewModel(cardIds[sourceIndex], checkPlayable);
            }
            return result;
        }

        private CardViewModel CreateCardViewModel(CardInstanceId cardId, bool checkPlayable)
        {
            BattlePlayerState player = GetPlayer();
            bool found = player.PileManager.Dictionary.TryGetValue(cardId, out CardInstance instance);
            MyAssert.Assert(found, $"Card instance not found while building CardViewModel: {cardId.Value}");
            CardDefinitionManager definitionManager = _stateManager.GameDefinitionState.CardDefinitionManager;
            CardDefinition definition = definitionManager.Get(instance.DefinitionId);
            definitionManager.TryGetImage(instance.DefinitionId, out Sprite artwork);

            bool isPlayable = false;
            if (checkPlayable && !_waitingForCommand)
            {
                BattleCommand command = CreatePlayCardCommand(cardId);
                isPlayable = BattleCommandApi.CanPlayCard(command, _stateManager.BattleState) ==
                             EnumPlayCardResult.Ok;
            }

            return new CardViewModel(cardId, definition.Id.Name, instance.EnergyCost.ToString(),
                definition.Description, artwork, isPlayable);
        }

        private BattleCommand CreatePlayCardCommand(CardInstanceId cardId)
        {
            ActionEntityId target = default;
            if (_stateManager.BattleState.EnemyList.Count > 0)
            {
                target = _stateManager.BattleState.EnemyList[0].Id;
            }

            // TODO: Replace the default first enemy with target selection from the battle UI.
            return new BattleCommand(EnumCommandType.PlayCard, GetPlayer().Id, cardId, target);
        }

        private BattlePlayerState GetPlayer()
        {
            return _stateManager.BattlePlayerStates[_playerIndex];
        }

        private bool IsBattleStable()
        {
            return !_stateManager.GameState.BattleCommandManager.HasPending() &&
                   !_stateManager.GameState.ActionExecutor.IsRunning &&
                   _stateManager.GameState.GameActionManager.ActionQueue.Count == 0;
        }
    }
}
