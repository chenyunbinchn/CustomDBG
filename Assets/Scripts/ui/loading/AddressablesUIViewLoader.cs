using System;
using System.Collections;
using System.Collections.Generic;
using tools.assert;
using ui.core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ui.loading
{
    // Load ui for UIManager
    public sealed class AddressablesUIViewLoader : IDisposable
    {
        private readonly Dictionary<UIView, AsyncOperationHandle<GameObject>> _handles =
            new Dictionary<UIView, AsyncOperationHandle<GameObject>>();
        private readonly List<AsyncOperationHandle<GameObject>> _pendingHandles =
            new List<AsyncOperationHandle<GameObject>>();
        private bool _isDisposed;

        public IEnumerator Load(string address, Transform parent, Action<UIView> onCompleted)
        {
            MyAssert.Assert(!string.IsNullOrWhiteSpace(address), "UI address must not be empty.");
            MyAssert.Assert(parent != null, $"UI parent is null. Address: {address}");
            MyAssert.Assert(onCompleted != null, $"UI load callback is null. Address: {address}");
            MyAssert.Assert(!_isDisposed, "Cannot load UI after loader disposal.");
            if (_isDisposed)
            {
                onCompleted?.Invoke(null);
                yield break;
            }

            // TODO: Move Addressables handle acquisition into the shared UI preload/cache service.
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            _pendingHandles.Add(handle);
            yield return handle;
            _pendingHandles.Remove(handle);

            if (_isDisposed)
            {
                ReleaseHandle(handle);
                onCompleted?.Invoke(null);
                yield break;
            }

            bool succeeded = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null;
            MyAssert.Assert(succeeded, $"Failed to load UI prefab: {address}");
            if (!succeeded)
            {
                ReleaseHandle(handle);
                onCompleted?.Invoke(null);
                yield break;
            }

            GameObject instance = UnityEngine.Object.Instantiate(handle.Result, parent, false);
            UIView view = instance.GetComponent<UIView>();
            MyAssert.Assert(view != null, $"UI prefab root requires UIView. Address: {address}");
            if (view == null)
            {
                UnityEngine.Object.Destroy(instance);
                ReleaseHandle(handle);
                onCompleted?.Invoke(null);
                yield break;
            }

            _handles.Add(view, handle);
            view.Prepare();
            onCompleted?.Invoke(view);
        }

        public void Release(UIView view)
        {
            MyAssert.Assert(view != null, "Cannot release a null UIView.");
            if (view == null)
            {
                return;
            }

            bool found = _handles.TryGetValue(view, out AsyncOperationHandle<GameObject> handle);
            MyAssert.Assert(found, $"No Addressables handle found for UIView: {view.name}");
            _handles.Remove(view);
            UnityEngine.Object.Destroy(view.gameObject);
            if (found)
            {
                ReleaseHandle(handle);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            List<UIView> views = new List<UIView>(_handles.Keys);
            for (int i = 0; i < views.Count; i++)
            {
                Release(views[i]);
            }

            for (int i = 0; i < _pendingHandles.Count; i++)
            {
                ReleaseHandle(_pendingHandles[i]);
            }
            _pendingHandles.Clear();
        }

        private void ReleaseHandle(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
