using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    public async UniTask OnStopRecording()
    {
        try
        {
            string recordingText = await whisperSpeechToText.StopRecordingTranscribe();
            _textInterface.text = recordingText;
            await OnReceiveMikuReplyAsync(recordingText);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }


    public async UniTask OnReceiveMikuReplyAsync(string userText)
    {
        try
        {
            string mikuReply = await aiChatController.SendMessageToMiku(userText,this.GetCancellationTokenOnDestroy());
            await MikuReplyToSpeechAsync(mikuReply);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }

    public async UniTask MikuReplyToSpeechAsync(string mikuReply)
    {
        if (isSmoothVoice)
        {
            await mikuTtsClient.SpeakAsync("1a_miku_default_rvc_(aple)", mikuReply, this.GetCancellationTokenOnDestroy());// 初音ミクモデルで音声を再生
        }
        else
        {
            await openJTalkClient.SpeakAsync(mikuReply, this.GetCancellationTokenOnDestroy());
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
