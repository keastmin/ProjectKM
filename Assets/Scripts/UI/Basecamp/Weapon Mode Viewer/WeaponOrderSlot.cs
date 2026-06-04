using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponOrderSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TextMeshProUGUI _weaponNameText;
    [SerializeField] private CanvasGroup _canvasGroup;

    private readonly List<RaycastResult> _results = new();

    public float Height => _rectTransform.rect.height;

    private Vector3 _originPos;
    private int _originSiblingIndex;
    private bool _isGrabbing = false;
    private bool _isInitialize = false;

    public Vector3 OriginPos => _originPos;

    public event Action<WeaponOrderSlot, WeaponOrderSlot> OnDetectOtherSlot;

    private void Update()
    {
        if (_isInitialize && !_isGrabbing)
        {
            Move();
        }
    }

    public void InitializeSlot(WeaponSlot slot, float localYPos)
    {
        _rectTransform.anchoredPosition = new Vector2(0, localYPos);
        _originPos = transform.position;
        _weaponNameText.text = slot.Instance.WeaponName;
        _isInitialize = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        _originPos = transform.position;
        _isGrabbing = true;
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Input.mousePosition;
        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;
        transform.position = mousePos + new Vector3(-width / 2f, height / 2f, 0f);

        if (_isGrabbing) 
        {
            WeaponOrderSlot otherSlot = RaycastUI<WeaponOrderSlot>(Input.mousePosition);

            if(otherSlot != null)
            {
                OnDetectOtherSlot?.Invoke(this, otherSlot);
            }
        } 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetSiblingIndex(_originSiblingIndex);
        transform.position = _originPos;
        _isGrabbing = false;
        _canvasGroup.blocksRaycasts = true;
    }

    public void SetOriginPosition(Vector3 pos)
    {
        _originPos = pos;
    }

    private T RaycastUI<T>(Vector2 screenPosition) where T : Component
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        _results.Clear();
        EventSystem.current.RaycastAll(pointerData, _results);

        foreach(RaycastResult result in _results)
        {
            T component = result.gameObject.GetComponentInParent<T>();

            if (component != null)
                return component;
        }

        return null;
    }

    private void Move()
    {
        transform.position = _originPos;
    }
}