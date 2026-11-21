using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Christina.UI;

public class ConfigUIController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField]private TMP_InputField api_key_InputField;
    [SerializeField] private Transform smoothVoiceToggleItem;
    [Header("Secript")]
    [SerializeField]private OpenAIClient openAIClient;
    [SerializeField]private ConversationManager conversationManager;
    private ToggleSwitch smoothVoiceToggle;
    private TMP_Text smoothVoiceLabel;
    private Button smoothVoiceRetryButton;

    void Awake()
    {
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
        if (conversationManager != null)
        {
            conversationManager.OnMikuTtsPreparationStateChanged -= UpdateSmoothVoiceStatus;
        }
        if (smoothVoiceToggle != null)
        {
            smoothVoiceToggle.OnUnavailableClick -= RetrySmoothVoicePreparation;
        }
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
