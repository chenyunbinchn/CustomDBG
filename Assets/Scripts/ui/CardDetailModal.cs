using TMPro;
using tools.assert;
using ui.core;
using ui.viewModels;
using UnityEngine;
using UnityEngine.UI;

namespace ui
{
    // CardDetailModal renders one read-only card above the current UI.
    public sealed class CardDetailModal : UIView
    {
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            MyAssert.Assert(artworkImage != null, "CardDetailModal requires artworkImage.");
            MyAssert.Assert(nameText != null, "CardDetailModal requires nameText.");
            MyAssert.Assert(costText != null, "CardDetailModal requires costText.");
            MyAssert.Assert(descriptionText != null, "CardDetailModal requires descriptionText.");
            MyAssert.Assert(closeButton != null, "CardDetailModal requires closeButton.");

            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        protected override void Render(IUIViewModel viewModel)
        {
            CardViewModel model = viewModel as CardViewModel;
            MyAssert.Assert(model != null, "CardDetailModal requires CardViewModel.");
            if (model == null)
            {
                return;
            }

            nameText.text = model.Name;
            costText.text = model.CostText;
            descriptionText.text = model.Description;
            artworkImage.sprite = model.Artwork;
            artworkImage.enabled = model.Artwork != null;
        }

        private void HandleCloseClicked()
        {
            UIManager manager = UIManager.Instance;
            if (manager != null && !manager.IsBusy)
            {
                manager.PopModal();
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }
    }
}
