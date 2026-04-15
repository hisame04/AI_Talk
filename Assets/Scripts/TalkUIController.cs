using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TalkUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [ SerializeField] private Button startRecordingButton;
    [ SerializeField] private Button stopRecordingButton;
    [SerializeField] private AIChatController aIChatController;
    [SerializeField] private WhisperSpeechToText whisperSpeechToText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sendButton.onClick.AddListener(OnSend);
        startRecordingButton.onClick.AddListener(whisperSpeechToText.StartRecording);
        stopRecordingButton.onClick.AddListener(whisperSpeechToText.StopRecording);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnSend()
    {
        if(string.IsNullOrEmpty(inputField.text)) return;
        sendButton.interactable = false;

        //APIに入力内容を送信
        aIChatController.SendMessageToMiku(inputField.text);

        inputField.text = "";
        Invoke("ReEnableButton", 2.0f);
    }

    void ReEnableButton()
    {
        sendButton.interactable = true;
    }
}
