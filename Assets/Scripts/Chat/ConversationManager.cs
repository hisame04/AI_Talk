using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class ConversationManager : MonoBehaviour
{
    [SerializeField] private AIChatController aiChatController;
    [SerializeField] private WhisperSpeechToText whisperSpeechToText;
    [SerializeField] private OpenJTalkClient openJTalkClient;
    [SerializeField] private MikuTtsClient mikuTtsClient;

    [SerializeField] private TalkUIController talkUIController;

    [SerializeField] private TMP_InputField _textInterface;

    [SerializeField] private bool isSmoothVoice;
    [SerializeField] private bool isVoiceInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    
    public void OnStartRecording()
    {
        whisperSpeechToText.StartRecording();
    }

    public void OnStopRecording()
    {
        whisperSpeechToText.StopRecordingTranscribe(
            onSuccess: (recordingText) =>
            {
                _textInterface.text = recordingText;
                OnReceiveMikuReply(recordingText);
            },
            onError: (errorMessage) =>
            {
                Debug.LogError("Error occurred: " + errorMessage);
            }
        );
    }


    public void OnReceiveMikuReply(string userText)
    {
        aiChatController.SendMessageToMiku(
            userText,
            onSuccess: (mikuReply) =>
            {
                Debug.Log("ミク: " + mikuReply);
                MikuReplyToSpeech(mikuReply);
            },
            onError: (errorMessage) =>
            {
                Debug.LogError("通信エラー: " + errorMessage);
            }
        );
    }

    public void MikuReplyToSpeech(string mikuReply)
    {
        if (isSmoothVoice)
        {
            mikuTtsClient.Speak(mikuReply);
        }
        else
        {
            openJTalkClient.Speak(mikuReply);
        }
    }

    //使用する読み上げモードをトグルスイッチ経由で更新するメソッド
    public void SetSmoothVoiceOn()
    {
        isSmoothVoice = true;
    }
    public void SetSmoothVoiceOff()
    {
        isSmoothVoice = false;
    }

    //使用する入力モードをトグルスイッチ経由で更新するメソッド
    public void SetVoiceInputOn()
    {
        isVoiceInput = true; 
        talkUIController.OnVoiceInputAble();  
    }
    public void SetVoiceInputOff()
    {
        isVoiceInput = false;
        talkUIController.OnVoiceInputUnable();
    }
}
