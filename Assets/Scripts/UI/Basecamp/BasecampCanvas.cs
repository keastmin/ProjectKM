using Player;
using System;
using System.Collections;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public class BasecampCanvas : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private PlayerCore _player;
    [SerializeField] private GameStarter _gameStarter;
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;
    [SerializeField] private PlayerUpgradeUI _playerUpgradeUI;
    [SerializeField] private Image _nodeMapChangeImage;
    [SerializeField] private Button _nodeMapExitButton;

    [Header("World")]
    [SerializeField] private WeaponModeViewer _weaponModeViewerWorld;
    [SerializeField] private PlayerUpgrade _playerUpgradeWorld;

    [Header("Node World")]
    [SerializeField] private NodeWorld _nodeWorld;

    [Header("Player")]
    [SerializeField] private PlayerCinemachineController _playerCineCamController;

    private BasecampUI _currentFocusUI;

    private void Awake()
    {
        _currentFocusUI = null;
        _gameStarter.OnPlayerSpawnedAction += PlayerReferenceInject;
        _gameStarter.OnPlayerCinemachineControllerSpawnedAction += PlayerCinemachineControllerReferenceInject;
        _weaponModeViewerUI.gameObject.SetActive(false);
        _playerUpgradeUI.gameObject.SetActive(false);
        _nodeMapExitButton.gameObject.SetActive(false);

        Color nodeMapChangeImageColor = _nodeMapChangeImage.color;
        nodeMapChangeImageColor.a = 0f;
        _nodeMapChangeImage.color = nodeMapChangeImageColor;
    }

    private void OnEnable()
    {
        _weaponModeViewerWorld.OnInteractWeaponModeViewerAction += ActiveWeaponModeViewerUIHandle;
        _playerUpgradeWorld.OnInteractPlayerUpgradeAction += ActivePlayerUpgradeUIHandle;

        _weaponModeViewerUI.OnEscapeThisUIAction += DisactiveUI;
        _playerUpgradeUI.OnEscapeThisUIAction += DisactiveUI;

        BindGameStateChangeAction();
    }

    private void OnDisable()
    {
        _weaponModeViewerWorld.OnInteractWeaponModeViewerAction -= ActiveWeaponModeViewerUIHandle;
        _playerUpgradeWorld.OnInteractPlayerUpgradeAction -= ActivePlayerUpgradeUIHandle;

        _weaponModeViewerUI.OnEscapeThisUIAction -= DisactiveUI;
        _playerUpgradeUI.OnEscapeThisUIAction -= DisactiveUI;

        UnbindGameStateChangeAction();
    }

    private void Update()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }
        if(GameManager.Instance.CurrentState == GameState.Game)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _currentFocusUI.InputEscapeKey();
        }
    }

    private void PlayerReferenceInject(PlayerCore player)
    {
        _player = player;
        _weaponModeViewerUI.GetPlayerReference(player);
        _playerUpgradeUI.GetPlayerReference(player);
    }

    private void PlayerCinemachineControllerReferenceInject(PlayerCinemachineController cineController)
    {
        _playerCineCamController = cineController;
    }

    private void ActiveWeaponModeViewerUIHandle()
    {
        ActiveUI(_weaponModeViewerUI);
    }

    private void ActivePlayerUpgradeUIHandle()
    {
        ActiveUI(_playerUpgradeUI);
    }

    private void ActiveUI(BasecampUI ui)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.CurrentState = GameState.UI;
        ui.gameObject.SetActive(true);
        _currentFocusUI = ui;
    }

    private void DisactiveUI(BasecampUI ui)
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.CurrentState = GameState.Game;
        ui.gameObject.SetActive(false);
        _currentFocusUI = null;
    }

    private void BindGameStateChangeAction()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.OnChangeGameState -= NodeMapChangeSequence;
        GameManager.Instance.OnChangeGameState += NodeMapChangeSequence;
    }

    private void UnbindGameStateChangeAction()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.OnChangeGameState -= NodeMapChangeSequence;
    }

    private void NodeMapChangeSequence(GameState prev, GameState curr)
    {
        if (_playerCineCamController == null || _nodeWorld == null)
            return;

        if (prev != GameState.NodeMap && curr != GameState.NodeMap)
            return;

        if (prev == GameState.NodeMap && curr == GameState.NodeMap)
            return;

        if (curr == GameState.NodeMap)
        {
            StartCoroutine(InNodeMap(0.5f, 0.2f, 0.5f));
        }
        else
        {
            StartCoroutine(OutNodeMap(0.5f, 0.2f, 0.5f));
        }
    }

    private IEnumerator InNodeMap(float fadeInDuration, float fadeContinueDuration, float fadeOutDuration)
    {
        float startPlayerCinemachineFOV = _playerCineCamController.FOV;
        float startNodeMapCinemachineFOV = _nodeWorld.GetFOV() / 2f;
        float endPlayerCinemachineFOV = startPlayerCinemachineFOV / 2f;
        float endNodeMapCinemachineFOV = _nodeWorld.GetFOV();
        float fromAlpha = 0f;
        float toAlpha = 1f;
        _nodeWorld.SetFOV(startNodeMapCinemachineFOV);

        float elapsed = 0f;

        while(elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeInDuration;
            float currentFOV = Mathf.Lerp(startPlayerCinemachineFOV, endPlayerCinemachineFOV, t);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            Color nodeMapChangeImageColor = _nodeMapChangeImage.color;
            nodeMapChangeImageColor.a = alpha;

            _nodeMapChangeImage.color = nodeMapChangeImageColor;
            _playerCineCamController.SetFOV(currentFOV);

            yield return null;
        }

        _playerCineCamController.SetFOV(startPlayerCinemachineFOV);
        _playerCineCamController.SetActiveCinemachine(false);
        yield return new WaitForSecondsRealtime(fadeContinueDuration);
        _nodeWorld.SetCamActive(true);
        _nodeMapExitButton.gameObject.SetActive(true);

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeOutDuration;
            float currentFOV = Mathf.Lerp(startNodeMapCinemachineFOV, endNodeMapCinemachineFOV, t);
            float alpha = Mathf.Lerp(toAlpha, fromAlpha, t);

            Color nodeMapChangeImageColor = _nodeMapChangeImage.color;
            nodeMapChangeImageColor.a = alpha;

            _nodeMapChangeImage.color = nodeMapChangeImageColor;
            _nodeWorld.SetFOV(currentFOV);

            yield return null;
        }
    }

    private IEnumerator OutNodeMap(float fadeInDuration, float fadeContinueDuration, float fadeOutDuration)
    {
        float startPlayerCinemachineFOV = _playerCineCamController.FOV / 2f;
        float startNodeMapCinemachineFOV = _nodeWorld.GetFOV();
        float endPlayerCinemachineFOV = _playerCineCamController.FOV;
        float endNodeMapCinemachineFOV = startNodeMapCinemachineFOV / 2f;
        float fromAlpha = 0f;
        float toAlpha = 1f;
        _playerCineCamController.SetFOV(startNodeMapCinemachineFOV);

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeInDuration;
            float currentFOV = Mathf.Lerp(startNodeMapCinemachineFOV, endNodeMapCinemachineFOV, t);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            Color nodeMapChangeImageColor = _nodeMapChangeImage.color;
            nodeMapChangeImageColor.a = alpha;

            _nodeMapChangeImage.color = nodeMapChangeImageColor;
            _nodeWorld.SetFOV(currentFOV);

            yield return null;
        }

        _nodeWorld.SetFOV(startNodeMapCinemachineFOV);
        _nodeWorld.SetCamActive(false);
        yield return new WaitForSecondsRealtime(fadeContinueDuration);
        _playerCineCamController.SetActiveCinemachine(true);
        _nodeMapExitButton.gameObject.SetActive(false);

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeOutDuration;
            float currentFOV = Mathf.Lerp(startPlayerCinemachineFOV, endPlayerCinemachineFOV, t);
            float alpha = Mathf.Lerp(toAlpha, fromAlpha, t);

            Color nodeMapChangeImageColor = _nodeMapChangeImage.color;
            nodeMapChangeImageColor.a = alpha;

            _nodeMapChangeImage.color = nodeMapChangeImageColor;
            _playerCineCamController.SetFOV(currentFOV);

            yield return null;
        }
    }

    public void OnClickNodeMapExitButton()
    {
        GameManager.Instance.CurrentState = GameState.Game;
    }
}
