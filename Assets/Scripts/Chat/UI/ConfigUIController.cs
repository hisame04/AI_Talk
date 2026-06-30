using UnityEngine;
using TMPro;

public class ConfigUIController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField]private TMP_InputField api_key_InputField;
    [Header("Secript")]
    [SerializeField]private OpenAIClient openAIClient;
    [SerializeField]private ConversationManager conversationManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
