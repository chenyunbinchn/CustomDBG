using cards.definition;
using cards.instance;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image artworkImage;

    private CardInstanceId _instanceId;
    private Vector2 _originalAnchoredPosition;
    private bool _isDragging;

    public void Bind(CardInstance instance, CardDefinition definition)
    {
        _instanceId = instance.Id;

        nameText.text = definition.Id.Name;
        // costText.text = definition.Cost.ToString(); Todo: Trying to make cost as CardEffect. So it is easier to modify
        descriptionText.text = definition.Description;
        // artworkImage.sprite = definition.Artwork;
    }

    public void SetHandPosition(Vector2 position, float rotation)
    {
        root.DOAnchorPos(position, 0.25f);
        root.DORotate(new Vector3(0f, 0f, rotation), 0.25f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging)
        {
            return;
        }

        visualRoot.DOKill();
        visualRoot.DOAnchorPosY(30f, 0.12f);
        visualRoot.DOScale(1.12f, 0.12f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging)
        {
            return;
        }

        visualRoot.DOKill();
        visualRoot.DOAnchorPosY(0f, 0.12f);
        visualRoot.DOScale(1f, 0.12f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        _originalAnchoredPosition = root.anchoredPosition;

        root.DOKill();
        visualRoot.DOKill();

        visualRoot.DOScale(1.08f, 0.1f);
        canvasGroup.alpha = 0.9f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        root.anchoredPosition += eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        canvasGroup.alpha = 1f;

        bool canPlay = false; // Later ask CardPlaySystem.

        if (canPlay)
        {
            // cardPlaySystem.TryPlayCard(instanceId);
        }
        else
        {
            root.DOAnchorPos(_originalAnchoredPosition, 0.2f);
            visualRoot.DOScale(1f, 0.12f);
        }
    }
}