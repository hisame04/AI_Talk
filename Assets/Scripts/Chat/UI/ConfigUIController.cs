using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Christina.UI;

public class ConfigUIController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField]private TMP_InputField api_key_InputField;
    [SerializeField] private Transform smoothVoiceToggleItem;
    [SerializeField] private TMP_Dropdown mikuTtsConnectionDropdown;
    [Header("Secript")]
    [SerializeField]private OpenAIClient openAIClient;
    [SerializeField]private ConversationManager conversationManager;
    [SerializeField] private MikuTtsClient mikuTtsClient;
    private ToggleSwitch smoothVoiceToggle;
    private TMP_Text smoothVoiceLabel;
    private Button smoothVoiceRetryButton;
    private bool localOptionAvailable;
    private int connectionUiVersion;

    void Awake()
    {
        if (mikuTtsConnectionDropdown != null)
        {
            mikuTtsConnectionDropdown.onValueChanged.AddListener(OnMikuTtsConnectionChanged);
        }
        if (smoothVoiceToggleItem == null) return;
        smoothVoiceToggle = smoothVoiceToggleItem.GetComponentInChildren<ToggleSwitch>(true);
        smoothVoiceLabel = smoothVoiceToggleItem.GetComponentInChildren<TMP_Text>(true);
        if (smoothVoiceLabel != null)
        {
            smoothVoiceRetryButton = smoothVoiceLabel.GetComponent<Button>();
            if (smoothVoiceRetryButton == null)
            {
                smoothVoiceRetryButton = smoothVoiceLabel.gameObject.AddComponent<Button>();
            }
            smoothVoiceRetryButton.targetGraphic = smoothVoiceLabel as Graphic;
            smoothVoiceRetryButton.transition = Selectable.Transition.None;
            smoothVoiceRetryButton.onClick.AddListener(RetrySmoothVoicePreparation);
        }
    }

    void OnEnable()
    {
        RefreshMikuTtsConnectionOptionsAsync().Forget();
        if (conversationManager == null) return;
        conversationManager.OnMikuTtsPreparationStateChanged += UpdateSmoothVoiceStatus;
        if (smoothVoiceToggle != null)
        {
            smoothVoiceToggle.OnUnavailableClick += RetrySmoothVoicePreparation;
        }
        UpdateSmoothVoiceStatus(conversationManager.CurrentMikuTtsPreparationState);
    }

    void OnDisable()
    {
        connectionUiVersion++;
        if (conversationManager != null)
        {
            conversationManager.OnMikuTtsPreparationStateChanged -= UpdateSmoothVoiceStatus;
        }
        if (smoothVoiceToggle != null)
        {
            smoothVoiceToggle.OnUnavailableClick -= RetrySmoothVoicePreparation;
        }
    }

    void OnDestroy()
    {
        if (mikuTtsConnectionDropdown != null)
        {
            mikuTtsConnectionDropdown.onValueChanged.RemoveListener(OnMikuTtsConnectionChanged);
        }
    }

    private async UniTask RefreshMikuTtsConnectionOptionsAsync()
    {
        if (mikuTtsConnectionDropdown == null || mikuTtsClient == null) return;

        int version = ++connectionUiVersion;
        mikuTtsConnectionDropdown.interactable =
            mikuTtsClient.CurrentConnectionMode == MikuTtsClient.ConnectionMode.Remote;
        localOptionAvailable = false;
        SetMikuTtsConnectionOptions(false);

        // OnEnable が他のコンポーネントの Awake より先に走る場合に備える。
        await UniTask.Yield();
        await mikuTtsClient.WaitUntilConnectionReadyAsync();
        if (this == null || version != connectionUiVersion || !isActiveAndEnabled) return;

        bool localAvailable = await mikuTtsClient.IsLocalAvailableAsync();
        if (this == null || version != connectionUiVersion || !isActiveAndEnabled) return;

        if (!localAvailable && mikuTtsClient.CurrentConnectionMode == MikuTtsClient.ConnectionMode.Local)
        {
            await mikuTtsClient.TrySetConnectionModeAsync(MikuTtsClient.ConnectionMode.Remote);
            if (this == null || version != connectionUiVersion || !isActiveAndEnabled) return;
        }

        localOptionAvailable = localAvailable;
        SetMikuTtsConnectionOptions(localAvailable);
        mikuTtsConnectionDropdown.interactable = true;
    }

    private void SetMikuTtsConnectionOptions(bool includeLocal)
    {
        var options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Remote")
        };
        if (includeLocal)
        {
            options.Add(new TMP_Dropdown.OptionData("Local"));
        }

        mikuTtsConnectionDropdown.ClearOptions();
        mikuTtsConnectionDropdown.AddOptions(options);
        int selectedIndex = includeLocal && mikuTtsClient.CurrentConnectionMode == MikuTtsClient.ConnectionMode.Local ? 1 : 0;
        mikuTtsConnectionDropdown.SetValueWithoutNotify(selectedIndex);
        mikuTtsConnectionDropdown.RefreshShownValue();
    }

    private void OnMikuTtsConnectionChanged(int selectedIndex)
    {
        ChangeMikuTtsConnectionAsync(selectedIndex).Forget();
    }

    private async UniTask ChangeMikuTtsConnectionAsync(int selectedIndex)
    {
        if (mikuTtsConnectionDropdown == null || mikuTtsClient == null) return;

        int version = ++connectionUiVersion;
        mikuTtsConnectionDropdown.interactable = false;
        var mode = selectedIndex == 1 && localOptionAvailable
            ? MikuTtsClient.ConnectionMode.Local
            : MikuTtsClient.ConnectionMode.Remote;

        bool applied = await mikuTtsClient.TrySetConnectionModeAsync(mode);
        if (this == null || version != connectionUiVersion || !isActiveAndEnabled) return;

        if (!applied)
        {
            await mikuTtsClient.TrySetConnectionModeAsync(MikuTtsClient.ConnectionMode.Remote);
            if (this == null || version != connectionUiVersion || !isActiveAndEnabled) return;
            localOptionAvailable = false;
            Debug.LogWarning("ローカルMikuTTSに接続できないため、リモート経路を使用します。");
        }

        SetMikuTtsConnectionOptions(localOptionAvailable);
        mikuTtsConnectionDropdown.interactable = true;
    }

    private void UpdateSmoothVoiceStatus(ConversationManager.MikuTtsPreparationState state)
    {
        if (smoothVoiceToggle != null)
        {
            smoothVoiceToggle.SetInteractable(state == ConversationManager.MikuTtsPreparationState.Ready);
        }
        if (smoothVoiceRetryButton != null)
        {
            smoothVoiceRetryButton.enabled = state == ConversationManager.MikuTtsPreparationState.Failed;
        }
        if (smoothVoiceLabel == null) return;

        smoothVoiceLabel.fontSize = 20;
        switch (state)
        {
            case ConversationManager.MikuTtsPreparationState.Preparing:
                smoothVoiceLabel.text = "Smooth\nLoading";
                break;
            case ConversationManager.MikuTtsPreparationState.Ready:
                smoothVoiceLabel.text = "Smooth\nReady";
                break;
            case ConversationManager.MikuTtsPreparationState.Failed:
                smoothVoiceLabel.text = "Smooth\nRetry";
                break;
        }
    }

    private void RetrySmoothVoicePreparation()
    {
        conversationManager.RetryInitialAudioPreparation();
    }

    /* APIキーを設定するボタンから呼び出すメソッド */
    public void OnApplyAPIKey()
    {
        string key = api_key_InputField.text;
        openAIClient.SetAPIKey(key);
    }

    /* Smoothトグルから呼び出すメソッド*/
    public void SetSmoothToggleOn()
    {
        conversationManager.SetSmoothVoiceOn();
    }
    public void SetSmoothToggleOff()
    {
        conversationManager.SetSmoothVoiceOff();
    }

    /* VoidInputトグルから呼び出すメソッド*/
    public void SetVoiceInputToggleOn()
    {
        conversationManager.SetVoiceInputOn();
    }
    public void SetVoiceInputToggleOff()
    {
        conversationManager.SetVoiceInputOff();
    }
}
