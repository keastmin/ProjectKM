using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private RectTransform _loadingUIProgressRectTransform;
    [SerializeField] private Image _loadingBackgroundImage;
    [SerializeField] private Image _blackScreen;
    [SerializeField] private float _progressRotateSpeed = 180f;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _blackScreenDuration = 0.5f;
    [SerializeField] private float _visibleDuration = 0.5f;

    private bool _isLoading = false;

    public float BlackScreenDuration => _blackScreenDuration;
    public float VisibleDuration => _visibleDuration;
    public bool IsLoading
    {
        get
        {
            return _isLoading;
        }
        set
        {
            _isLoading = value;
        }
    }
    public CanvasGroup LoadingUICanvasGroup => _canvasGroup;
    public Image BlackScreen => _blackScreen;

    private void Awake()
    {
        IsLoading = false;
        LoadingUICanvasGroup.alpha = 0f;
        Color blackScreenColor = BlackScreen.color;
        blackScreenColor.a = 1f;
        BlackScreen.color = blackScreenColor;
    }

    private void Update()
    {
        if (_isLoading)
        {
            _loadingUIProgressRectTransform.Rotate(0f, 0f, -_progressRotateSpeed * Time.unscaledDeltaTime);
        }
    }

    public void SetActiveLoadingUI(bool active)
    {
        _loadingBackgroundImage.gameObject.SetActive(active);
        _loadingUIProgressRectTransform.gameObject.SetActive(active);
    }
}