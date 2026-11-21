using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ConfigUIController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TMP_InputField api_key_InputField;
    [SerializeField] private OptionSelectorItem voiceSelector;
    [SerializeField] private OptionSelectorItem connectionSelector;
    [SerializeField] private OptionSelectorItem inputSelector;

    [Header("Scripts")]
    [SerializeField] private OpenAIClient openAIClient;
    [SerializeField] private ConversationManager conversationManager;
    [SerializeField] private MikuTtsClient mikuTtsClient;

    private int connectionUiVersion;

    private void Awake()
    {
        if (voiceSelector == null) voiceSelector = transform.Find("VoiceSelector")?.GetComponent<OptionSelectorItem>();
        if (connectionSelector == null) connectionSelector = transform.Find("ConnectionSelector")?.GetComponent<OptionSelectorItem>();
        if (inputSelector == null) inputSelector = transform.Find("InputSelector")?.GetComponent<OptionSelectorItem>();
        if (voiceSelector == null || connectionSelector == null || inputSelector == null)
        {
            Debug.LogError("ConfigPanel の OptionSelectorItem 参照が不足しています。", this);
            enabled = false;
            return;
        }

        voiceSelector.Configure("Voice", new[] { "Normal", "Smooth" }, conversationManager.IsSmoothVoice ? 1 : 0);
        connectionSelector.Configure("MikuTTS", new[] { "Remote", "Local" }, 0);
        inputSelector.Configure("Input", new[] { "TextInput", "VoiceInput" }, conversationManager.IsVoiceInput ? 1 : 0);

        voiceSelector.SelectionRequested += OnVoiceRequested;
        voiceSelector.StatusClicked += RetrySmoothVoicePreparation;
        connectionSelector.SelectionRequested += OnConnectionRequested;
        inputSelector.SelectionRequested += OnInputRequested;
    }

    private void OnEnable()
    {
        conversationManager.OnMikuTtsPreparationStateChanged += UpdateSmoothVoiceStatus;
        UpdateSmoothVoiceStatus(conversationManager.CurrentMikuTtsPreparationState);
        voiceSelector.SetSelectedIndex(conversationManager.IsSmoothVoice ? 1 : 0);
        inputSelector.SetSelectedIndex(conversationManager.IsVoiceInput ? 1 : 0);
        RefreshConnectionAsync().Forget();
    }

    private void OnDisable()
    {
        connectionUiVersion++;
        conversationManager.OnMikuTtsPreparationStateChanged -= UpdateSmoothVoiceStatus;
    }

    private void OnDestroy()
    {
        if (voiceSelector != null) voiceSelector.SelectionRequested -= OnVoiceRequested;
        if (voiceSelector != null) voiceSelector.StatusClicked -= RetrySmoothVoicePreparation;
        if (connectionSelector != null) connectionSelector.SelectionRequested -= OnConnectionRequested;
        if (inputSelector != null) inputSelector.SelectionRequested -= OnInputRequested;
    }

    private void UpdateSmoothVoiceStatus(ConversationManager.MikuTtsPreparationState state)
    {
        bool ready = state == ConversationManager.MikuTtsPreparationState.Ready;
        voiceSelector.Configure("Voice", ready ? new[] { "Normal", "Smooth" } : new[] { "Normal" },
            ready && conversationManager.IsSmoothVoice ? 1 : 0);
        voiceSelector.SetNavigationVisible(true);
        voiceSelector.SetInteractable(ready);

        switch (state)
        {
            case ConversationManager.MikuTtsPreparationState.Preparing:
                voiceSelector.SetStatus("Smooth Loading...");
                break;
            case ConversationManager.MikuTtsPreparationState.Failed:
                voiceSelector.SetStatus("Smooth Retry", canClick: true);
                break;
            default:
                voiceSelector.SetStatus(null);
                break;
        }
    }

    private void RetrySmoothVoicePreparation()
    {
        if (conversationManager.CurrentMikuTtsPreparationState == ConversationManager.MikuTtsPreparationState.Failed)
        {
            conversationManager.RetryInitialAudioPreparation();
        }
    }

    private void OnVoiceRequested(int index)
    {
        if (index == 0)
        {
            conversationManager.SetSmoothVoiceOff();
            voiceSelector.SetSelectedIndex(0);
            return;
        }

        if (conversationManager.CurrentMikuTtsPreparationState == ConversationManager.MikuTtsPreparationState.Failed)
        {
            conversationManager.RetryInitialAudioPreparation();
            return;
        }
        if (conversationManager.CurrentMikuTtsPreparationState != ConversationManager.MikuTtsPreparationState.Ready)
        {
            return;
        }

        conversationManager.SetSmoothVoiceOn();
        voiceSelector.SetSelectedIndex(conversationManager.IsSmoothVoice ? 1 : 0);
    }

    private void OnInputRequested(int index)
    {
        if (index == 1)
        {
            conversationManager.SetVoiceInputOn();
        }
        else
        {
            conversationManager.SetVoiceInputOff();
        }
        inputSelector.SetSelectedIndex(conversationManager.IsVoiceInput ? 1 : 0);
    }

    private async UniTask RefreshConnectionAsync()
    {
        int version = ++connectionUiVersion;
        connectionSelector.SetInteractable(false);
        await UniTask.Yield();
        await mikuTtsClient.WaitUntilConnectionReadyAsync();
        if (this == null || !isActiveAndEnabled || version != connectionUiVersion) return;

        bool localAvailable = await mikuTtsClient.IsLocalAvailableAsync();
        if (this == null || !isActiveAndEnabled || version != connectionUiVersion) return;

        if (!localAvailable && mikuTtsClient.CurrentConnectionMode == MikuTtsClient.ConnectionMode.Local)
        {
            await mikuTtsClient.TrySetConnectionModeAsync(MikuTtsClient.ConnectionMode.Remote);
            if (this == null || !isActiveAndEnabled || version != connectionUiVersion) return;
        }

        UpdateConnectionSelector(localAvailable);
    }

    private void OnConnectionRequested(int index)
    {
        ChangeConnectionAsync(index).Forget();
    }

    private async UniTask ChangeConnectionAsync(int index)
    {
        int version = ++connectionUiVersion;
        connectionSelector.SetInteractable(false);
        var mode = index == 1 ? MikuTtsClient.ConnectionMode.Local : MikuTtsClient.ConnectionMode.Remote;
        bool applied = await mikuTtsClient.TrySetConnectionModeAsync(mode);
        if (this == null || !isActiveAndEnabled || version != connectionUiVersion) return;

        if (!applied)
        {
            await mikuTtsClient.TrySetConnectionModeAsync(MikuTtsClient.ConnectionMode.Remote);
            if (this == null || !isActiveAndEnabled || version != connectionUiVersion) return;
            Debug.LogWarning("ローカルMikuTTSに接続できないため、リモート経路を使用します。");
        }

        UpdateConnectionSelector(applied);
    }

    private void UpdateConnectionSelector(bool localAvailable)
    {
        connectionSelector.SetSelectedIndex(mikuTtsClient.CurrentConnectionMode == MikuTtsClient.ConnectionMode.Local ? 1 : 0);
        connectionSelector.SetTitle(localAvailable ? "MikuTTS" : "MikuTTS (offline)");
        connectionSelector.SetInteractable(localAvailable);
    }

    public void OnApplyAPIKey()
    {
        openAIClient.SetAPIKey(api_key_InputField.text);
    }

    // 旧ToggleItemのシーン参照が残る間、UnityEventの参照先を維持する。
    public void SetSmoothToggleOn() => OnVoiceRequested(1);
    public void SetSmoothToggleOff() => OnVoiceRequested(0);
    public void SetVoiceInputToggleOn() => OnInputRequested(1);
    public void SetVoiceInputToggleOff() => OnInputRequested(0);
}
