using System.Collections;
using tools.assert;
using UnityEngine;

namespace ui.core
{
    // Page, HUD, Modal, and Overlay roots inherit UIView.
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool blocksRaycasts = true;

        internal void Prepare()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            MyAssert.Assert(canvasGroup != null, $"{GetType().Name} requires a CanvasGroup.");
            SetInput(false);
            gameObject.SetActive(false);
        }

        internal void Bind(IUIViewModel viewModel)
        {
            MyAssert.Assert(viewModel != null, $"{GetType().Name} received a null ViewModel.");
            if (viewModel == null)
            {
                return;
            }

            Render(viewModel);
        }

        internal void Unbind()
        {
            OnUnbind();
        }

        internal IEnumerator PlayOpenAnimation()
        {
            IEnumerator animation = OnPlayOpenAnimation();
            if (animation != null)
            {
                yield return animation;
            }
        }

        internal IEnumerator PlayCloseAnimation()
        {
            IEnumerator animation = OnPlayCloseAnimation();
            if (animation != null)
            {
                yield return animation;
            }
        }

        internal void SetInput(bool enabled)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = enabled && blocksRaycasts;
            canvasGroup.blocksRaycasts = enabled && blocksRaycasts;
        }

        protected abstract void Render(IUIViewModel viewModel);

        protected virtual void OnUnbind()
        {
        }

        protected virtual IEnumerator OnPlayOpenAnimation()
        {
            yield break;
        }

        protected virtual IEnumerator OnPlayCloseAnimation()
        {
            yield break;
        }
    }
}
