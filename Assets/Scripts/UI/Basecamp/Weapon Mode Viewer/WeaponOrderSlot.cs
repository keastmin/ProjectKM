using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponOrderSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TextMeshProUGUI _weaponNameText;

    public float Height => _rectTransform.rect.height;

    private Vector3 _originPos;
    private int _originSiblingIndex;

    public void InitializeSlot(WeaponSlot slot, float localYPos)
    {
        _rectTransform.anchoredPosition = new Vector2(0, localYPos);
        _weaponNameText.text = slot.Instance.WeaponName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        _originPos = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Input.mousePosition;
        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;
        transform.position = mousePos +  new Vector3(-width / 2f, height / 2f, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetSiblingIndex(_originSiblingIndex);
        transform.position = _originPos;
    }

    public void SetLocalPosition()
    {

    }
}
