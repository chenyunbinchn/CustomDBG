using mono;
using tools.assert;
using ui.core;
using ui.presenters;
using ui.viewModels;
using UnityEngine;

namespace ui.controllers
{
    // Scene-level bridge that owns BattlePresenter and advances its deferred refresh.
    public sealed class BattleUIController : MonoBehaviour
    {
        [SerializeField] private UnityBoostrap unityBootstrap;
        [SerializeField] private int playerIndex;
        [SerializeField] private string battlePageAddress = "UI/BattlePage";
        [SerializeField] private string battleHudAddress = "UI/BattleHUD";
        [SerializeField] private string cardPileDialogModalAddress = "UI/CardPileDialogModal";
        [SerializeField] private string cardDetailModalAddress = "UI/CardDetailModal";

        private BattlePresenter _presenter;

        public bool OpenBattleUi()
        {
            UIManager manager = UIManager.Instance;
            MyAssert.Assert(manager != null, "BattleUIController requires an active UIManager.");
            if (manager == null || manager.IsBusy)
            {
                return false;
            }

            return manager.OpenPage(battlePageAddress, BattlePageViewModel.Empty,
                HandleBattlePageOpened);
        }

        public bool ShowBattleHud()
        {
            EnsurePresenter();
            return _presenter.ShowHud(battleHudAddress);
        }

        public void RefreshBattleHud()
        {
            EnsurePresenter();
            _presenter.Refresh();
        }

        private void HandleBattlePageOpened(UIView view)
        {
            if (view == null)
            {
                return;
            }

            BattlePage battlePage = view as BattlePage;
            MyAssert.Assert(battlePage != null, "Battle page address must load a BattlePage root.");
            if (battlePage != null)
            {
                ShowBattleHud();
            }
        }

        private void Update()
        {
            _presenter?.Tick();
        }

        private void EnsurePresenter()
        {
            if (_presenter != null)
            {
                return;
            }

            MyAssert.Assert(unityBootstrap != null, "BattleUIController requires UnityBoostrap.");
            _presenter = new BattlePresenter(unityBootstrap.StateManager, playerIndex,
                cardPileDialogModalAddress, cardDetailModalAddress);
            // TODO: Replace the scene reference with the final game-flow dependency injection path.
            // TODO: Select the locally controlled player instead of using a serialized player index.
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _presenter = null;
        }
    }
}
