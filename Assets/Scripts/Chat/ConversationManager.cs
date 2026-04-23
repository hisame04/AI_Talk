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

    [SerializeField] private AICharactorData aiCharactorData;

    [SerializeField] private TalkUIController talkUIController;

    [SerializeField] private TMP_InputField _textInterface;

    [SerializeField] private bool isSmoothVoice;
    [SerializeField] private bool isVoiceInput;
    [SerializeField] private int charactorId;
    private CharactorData currentCharactorData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        charactorId = 0; //初期キャラクターIDを0に設定
        SetCharactorId(charactorId); //初期キャラクターの設定
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
            await OnReceiveReplyAsync(recordingText);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }


    public async UniTask OnReceiveReplyAsync(string userText)
    {
        try
        {
            string aiReply = await aiChatController.SendMessageToAI(userText,this.GetCancellationTokenOnDestroy());
            await ReplyToSpeechAsync(aiReply);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }

    public async UniTask ReplyToSpeechAsync(string aiReply)
    {
        if (isSmoothVoice)
        {
            await mikuTtsClient.SpeakAsync("1a_miku_default_rvc_(aple)", aiReply, this.GetCancellationTokenOnDestroy());// 初音ミクモデルで音声を再生
        }
        else
        {
            await openJTalkClient.SpeakAsync(aiReply, this.GetCancellationTokenOnDestroy());
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

    public void SetCharactorId(int id)
    {
        charactorId = id;
        currentCharactorData = Array.Find(aiCharactorData.charactors, c => c.id == id);
        aiChatController.SetCharactorData(currentCharactorData);
    }
}
