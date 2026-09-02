using mono;
using tools.assert;
using ui.presenters;
using UnityEngine;

namespace ui.controllers
{
    // Scene-level bridge that owns BattlePresenter and advances its deferred refresh.
    public sealed class BattleUIController : MonoBehaviour
    {
        [SerializeField] private UnityBoostrap unityBootstrap;
        [SerializeField] private int playerIndex;
        [SerializeField] private string battleHudAddress = "UI/BattleHUD";
        [SerializeField] private string cardPileDialogAddress = "UI/CardPileDialog";

        private BattlePresenter _presenter;

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
            _presenter = new BattlePresenter(unityBootstrap.StateManager, playerIndex, cardPileDialogAddress);
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
