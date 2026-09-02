using System;
using System.Collections;
using System.Collections.Generic;
using tools.assert;
using ui.loading;
using UnityEngine;

namespace ui.core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance => _instance;

        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private Canvas pageLayer;
        [SerializeField] private Canvas hudLayer;
        [SerializeField] private Canvas modalLayer;
        [SerializeField] private Canvas overlayLayer;

        private readonly List<UIView> _huds = new List<UIView>();
        private readonly List<UIView> _modalStack = new List<UIView>();
        private readonly List<UIView> _overlays = new List<UIView>();

        private AddressablesUIViewLoader _loader;
        private UIView _currentPage;
        private UIView _operationResult;
        private bool _isBusy;

        public UIView CurrentPage => _currentPage;
        public int ModalCount => _modalStack.Count;
        public bool IsBusy => _isBusy;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                MyAssert.Assert(false, "Only one UIManager may exist at a time.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            bool valid = pageLayer != null && hudLayer != null && modalLayer != null && overlayLayer != null;
            MyAssert.Assert(valid, "UIManager requires Page, HUD, Modal and Overlay Canvas layers.");
            if (!valid)
            {
                _instance = null;
                enabled = false;
                return;
            }

            ConfigureLayer(pageLayer, 0);
            ConfigureLayer(hudLayer, 1000);
            ConfigureLayer(modalLayer, 2000);
            ConfigureLayer(overlayLayer, 3000);
            _loader = new AddressablesUIViewLoader();

            if (persistAcrossScenes)
            {
                MyAssert.Assert(transform.parent == null, "Persistent UIManager must be a root GameObject.");
                DontDestroyOnLoad(gameObject);
            }
        }

        public bool OpenPage(string address, IUIViewModel viewModel, Action<UIView> onCompleted = null)
        {
            return StartOperation(OpenPageRoutine(address, viewModel), onCompleted);
        }

        public bool ShowHud(string address, IUIViewModel viewModel, Action<UIView> onCompleted = null)
        {
            MyAssert.Assert(_currentPage != null, "HUD requires an active Page.");
            if (_currentPage == null)
            {
                onCompleted?.Invoke(null);
                return false;
            }

            return StartOperation(ShowHudRoutine(address, viewModel), onCompleted);
        }

        public bool PushModal(string address, IUIViewModel viewModel, Action<UIView> onCompleted = null)
        {
            MyAssert.Assert(_currentPage != null, "Modal requires an active Page.");
            if (_currentPage == null)
            {
                onCompleted?.Invoke(null);
                return false;
            }

            return StartOperation(PushModalRoutine(address, viewModel), onCompleted);
        }

        public bool ShowOverlay(string address, IUIViewModel viewModel, Action<UIView> onCompleted = null)
        {
            return StartOperation(ShowOverlayRoutine(address, viewModel), onCompleted);
        }

        public bool Refresh(UIView view, IUIViewModel viewModel)
        {
            MyAssert.Assert(view != null, "Cannot refresh a null UIView.");
            MyAssert.Assert(viewModel != null, "Cannot refresh UIView with a null ViewModel.");
            MyAssert.Assert(!_isBusy, "Cannot refresh a root view during another root UI operation.");
            if (view == null || viewModel == null || _isBusy)
            {
                return false;
            }

            view.Bind(viewModel);
            return true;
        }

        public bool ClosePage(Action onCompleted = null)
        {
            MyAssert.Assert(_currentPage != null, "No active Page to close.");
            if (_currentPage == null)
            {
                onCompleted?.Invoke();
                return false;
            }

            return StartOperation(ClosePageRoutine(), result => onCompleted?.Invoke());
        }

        public bool HideHud(UIView hud, Action onCompleted = null)
        {
            MyAssert.Assert(_huds.Contains(hud), "UIView is not an active HUD.");
            if (!_huds.Contains(hud))
            {
                onCompleted?.Invoke();
                return false;
            }

            return StartOperation(HideHudRoutine(hud), result => onCompleted?.Invoke());
        }

        public bool PopModal(Action onCompleted = null)
        {
            MyAssert.Assert(_modalStack.Count > 0, "No active Modal to pop.");
            if (_modalStack.Count == 0)
            {
                onCompleted?.Invoke();
                return false;
            }

            return StartOperation(PopModalRoutine(), result => onCompleted?.Invoke());
        }

        public bool HideOverlay(UIView overlay, Action onCompleted = null)
        {
            MyAssert.Assert(_overlays.Contains(overlay), "UIView is not an active Overlay.");
            if (!_overlays.Contains(overlay))
            {
                onCompleted?.Invoke();
                return false;
            }

            return StartOperation(HideOverlayRoutine(overlay), result => onCompleted?.Invoke());
        }

        private bool StartOperation(IEnumerator operation, Action<UIView> onCompleted)
        {
            MyAssert.Assert(!_isBusy, "Another root UI operation is already running.");
            if (_isBusy)
            {
                onCompleted?.Invoke(null);
                return false;
            }

            _isBusy = true;
            _operationResult = null;
            StartCoroutine(RunOperation(operation, onCompleted));
            return true;
        }

        private IEnumerator RunOperation(IEnumerator operation, Action<UIView> onCompleted)
        {
            yield return operation;
            UIView result = _operationResult;
            _operationResult = null;
            _isBusy = false;
            onCompleted?.Invoke(result);
        }

        private IEnumerator OpenPageRoutine(string address, IUIViewModel viewModel)
        {
            UIView nextPage = null;
            yield return _loader.Load(address, pageLayer.transform, view => nextPage = view);
            if (nextPage == null)
            {
                yield break;
            }

            yield return CloseAll(_modalStack);
            yield return CloseAll(_huds);
            if (_currentPage != null)
            {
                yield return CloseRoot(_currentPage);
            }

            _currentPage = nextPage;
            yield return OpenRoot(nextPage, viewModel);
            _operationResult = nextPage;
        }

        private IEnumerator ShowHudRoutine(string address, IUIViewModel viewModel)
        {
            UIView hud = null;
            yield return _loader.Load(address, hudLayer.transform, view => hud = view);
            if (hud == null)
            {
                yield break;
            }

            _huds.Add(hud);
            yield return OpenRoot(hud, viewModel);
            _operationResult = hud;
        }

        private IEnumerator PushModalRoutine(string address, IUIViewModel viewModel)
        {
            UIView modal = null;
            yield return _loader.Load(address, modalLayer.transform, view => modal = view);
            if (modal == null)
            {
                yield break;
            }

            if (_modalStack.Count > 0)
            {
                _modalStack[_modalStack.Count - 1].SetInput(false);
            }

            _modalStack.Add(modal);
            modal.transform.SetAsLastSibling();
            yield return OpenRoot(modal, viewModel);
            _operationResult = modal;
        }

        private IEnumerator ShowOverlayRoutine(string address, IUIViewModel viewModel)
        {
            UIView overlay = null;
            yield return _loader.Load(address, overlayLayer.transform, view => overlay = view);
            if (overlay == null)
            {
                yield break;
            }

            _overlays.Add(overlay);
            overlay.transform.SetAsLastSibling();
            yield return OpenRoot(overlay, viewModel);
            _operationResult = overlay;
        }

        private IEnumerator ClosePageRoutine()
        {
            yield return CloseAll(_modalStack);
            yield return CloseAll(_huds);
            yield return CloseRoot(_currentPage);
            _currentPage = null;
        }

        private IEnumerator HideHudRoutine(UIView hud)
        {
            _huds.Remove(hud);
            yield return CloseRoot(hud);
        }

        private IEnumerator PopModalRoutine()
        {
            int topIndex = _modalStack.Count - 1;
            UIView modal = _modalStack[topIndex];
            _modalStack.RemoveAt(topIndex);
            yield return CloseRoot(modal);

            if (_modalStack.Count > 0)
            {
                _modalStack[_modalStack.Count - 1].SetInput(true);
            }
        }

        private IEnumerator HideOverlayRoutine(UIView overlay)
        {
            _overlays.Remove(overlay);
            yield return CloseRoot(overlay);
        }

        private IEnumerator OpenRoot(UIView view, IUIViewModel viewModel)
        {
            view.gameObject.SetActive(true);
            view.SetInput(false);
            view.Bind(viewModel);
            yield return view.PlayOpenAnimation();
            view.SetInput(true);
        }

        private IEnumerator CloseRoot(UIView view)
        {
            view.SetInput(false);
            yield return view.PlayCloseAnimation();
            view.Unbind();
            _loader.Release(view);
        }

        private IEnumerator CloseAll(List<UIView> views)
        {
            for (int i = views.Count - 1; i >= 0; i--)
            {
                yield return CloseRoot(views[i]);
            }
            views.Clear();
        }

        private void ConfigureLayer(Canvas layer, int sortingOrder)
        {
            layer.overrideSorting = true;
            layer.sortingOrder = sortingOrder;
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;
            StopAllCoroutines();
            UnbindActiveViews();
            _loader?.Dispose();
            _loader = null;
        }

        private void UnbindActiveViews()
        {
            _currentPage?.Unbind();
            for (int i = 0; i < _huds.Count; i++)
            {
                _huds[i].Unbind();
            }
            for (int i = 0; i < _modalStack.Count; i++)
            {
                _modalStack[i].Unbind();
            }
            for (int i = 0; i < _overlays.Count; i++)
            {
                _overlays[i].Unbind();
            }
        }
    }
}
