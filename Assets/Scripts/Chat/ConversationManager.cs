using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ConversationManager : MonoBehaviour
{
    public enum MikuTtsPreparationState { Preparing, Ready, Failed }

    private const string MikuModelName = "1a_miku_default_rvc_(aple)";

    [SerializeField] private AIChatController aiChatController;
    [SerializeField] private WhisperSpeechToText whisperSpeechToText;
    [SerializeField] private OpenJTalkClient openJTalkClient;
    [SerializeField] private MikuTtsClient mikuTtsClient;
    [SerializeField] private AudioSource audioSource; // 再生に使うAudioSource参照を保持する

    [SerializeField] private AICharactorData aiCharactorData;

    [SerializeField] private TalkUIController talkUIController;

    [SerializeField] private TMP_InputField _textInterface;

    [SerializeField] private bool isSmoothVoice;
    [SerializeField] private bool isVoiceInput;
    [SerializeField] private int charactorId;
    [SerializeField] private string initialMikuText = "スムーズボイスモードだよ。";
    [SerializeField] private string initialJTalkText = "ノーマルボイスモードだよ。";
    private CharactorData currentCharactorData;
    private AudioClip initialMikuAudio;
    private AudioClip initialJTalkAudio;
    private bool pendingJTalkGreeting;
    private bool preparingJTalkAudio;
    private MikuTtsPreparationState mikuTtsPreparationState = MikuTtsPreparationState.Preparing;

    public MikuTtsPreparationState CurrentMikuTtsPreparationState => mikuTtsPreparationState;
    public event Action<MikuTtsPreparationState> OnMikuTtsPreparationStateChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        charactorId = 0; //初期キャラクターIDを0に設定
        SetCharactorId(charactorId); //初期キャラクターの設定
    }

    void Start()
    {
        PrepareInitialMikuAudioAsync().Forget();
        PrepareInitialJTalkAudioAsync().Forget();
    }

    public void RetryInitialAudioPreparation()
    {
        if (mikuTtsPreparationState == MikuTtsPreparationState.Failed)
        {
            PrepareInitialMikuAudioAsync().Forget();
        }
    }

    private async UniTask PrepareInitialMikuAudioAsync()
    {
        SetMikuTtsPreparationState(MikuTtsPreparationState.Preparing);
        try
        {
            initialMikuAudio = await mikuTtsClient.CallTTS(MikuModelName, initialMikuText);
            if (this == null) return;

            SetMikuTtsPreparationState(initialMikuAudio != null
                ? MikuTtsPreparationState.Ready
                : MikuTtsPreparationState.Failed);
            if (initialMikuAudio != null && isSmoothVoice)
            {
                PlayAudio(initialMikuAudio);
            }
        }
        catch (Exception ex)
        {
            if (this == null) return;
            Debug.LogError($"MikuTTSの初期音声生成に失敗: {ex.Message}");
            SetMikuTtsPreparationState(MikuTtsPreparationState.Failed);
        }
    }

    private async UniTask PrepareInitialJTalkAudioAsync()
    {
        if (preparingJTalkAudio) return;
        preparingJTalkAudio = true;
        try
        {
            initialJTalkAudio = await openJTalkClient.GetAudioAsync(initialJTalkText,
                this.GetCancellationTokenOnDestroy());
            if (this == null) return;

            if (initialJTalkAudio == null)
            {
                Debug.LogError("OpenJTalkの初期音声生成に失敗しました。");
                return;
            }

            if (pendingJTalkGreeting && !isSmoothVoice)
            {
                pendingJTalkGreeting = false;
                PlayAudio(initialJTalkAudio);
            }
        }
        catch (OperationCanceledException)
        {
            // オブジェクト破棄時は生成を終了する
        }
        catch (Exception ex)
        {
            if (this != null)
            {
                Debug.LogError($"OpenJTalkの初期音声生成に失敗: {ex.Message}");
            }
        }
        finally
        {
            preparingJTalkAudio = false;
        }
    }

    private void SetMikuTtsPreparationState(MikuTtsPreparationState state)
    {
        mikuTtsPreparationState = state;
        OnMikuTtsPreparationStateChanged?.Invoke(state);
    }

    
    public void OnStartRecording()
    {
        StopInitialGreeting();
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
            StopInitialGreeting();

            //AIから返答を取得
            string aiReply = await aiChatController.SendMessageToAI(userText,this.GetCancellationTokenOnDestroy());
            // Debug.Log("Final Reply: " + aiReply);

            //タグとテキストのセットのリストに変換する処理をここに入れる
            List<SentenceData> replySentences = ParseReply(aiReply);

            //順番に音声生成
            foreach (var sentence in replySentences)
            {
                GenerateAudioAsync(sentence).Forget();
            }
            //【NEXT】音声のタグに対応するアニメーションを再生する処理をここに入れる
            foreach (var sentence in replySentences)
            {
                await UniTask.WaitUntil(() => sentence.Audio != null, cancellationToken: this.GetCancellationTokenOnDestroy());

                PlayAudio(sentence.Audio);
                Debug.Log($"Audioを再生：{sentence.Text} [{sentence.EmotionTag}]");
                // 【NEXT】タグに対応するアニメーションを再生する処理をここに入れる

                await UniTask.WaitUntil(() => !audioSource.isPlaying, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }

    private async UniTask GenerateAudioAsync(SentenceData sentence)
    {
        try
        {
            sentence.Audio = await ReplyToAudioAsync(sentence.Text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"音声生成エラー ({sentence.Text}): {ex.Message}");
        }
    }

    // テキストを指定のTTSで音声ファイルに変換するメソッド
    private async UniTask<AudioClip> ReplyToAudioAsync(string aiReply)
    {
        AudioClip clip;
        if (isSmoothVoice)
        {
            // ローカル実行で音声ファイルを取得
            // clip = await mikuTtsClient.GetAudioAsync("1a_miku_default_rvc_(aple)", aiReply, this.GetCancellationTokenOnDestroy());// 初音ミクモデルで音声を再生
            // API経由で音声ファイルを取得
            clip = await mikuTtsClient.CallTTS(MikuModelName, aiReply);
        }
        else
        {
            clip = await openJTalkClient.GetAudioAsync(aiReply, this.GetCancellationTokenOnDestroy());
        }

        return clip;
    }

    //音声を再生するメソッド
    private void PlayAudio(AudioClip clip)
    {        
        if (clip != null)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.clip = clip; // クリップの再生を開始する
            audioSource.Play();
        }   
        else
        {
            Debug.LogError("AudioClip is null. Cannot play audio.");
        }       
    }

    // AIの返答をテキストとタグのセットのリストに変換するメソッド
    private List<SentenceData> ParseReply(string aiReply)
    {
        List<SentenceData> sentences = new List<SentenceData>();
        //Group 1: テキスト、Group 2: タグ
        string pattern = @"([^\[]+)\[([A-Za-z]+)\]";
        MatchCollection matches = Regex.Matches(aiReply, pattern);
        foreach (Match match in matches)
        {
            string sentenceText = match.Groups[1].Value.Trim();
            string emotionTag = match.Groups[2].Value.Trim();
            sentences.Add(new SentenceData(sentenceText, emotionTag));
        }
        return sentences;
    }

    //使用する読み上げモードをトグルスイッチ経由で更新するメソッド
    public void SetSmoothVoiceOn()
    {
        if (mikuTtsPreparationState != MikuTtsPreparationState.Ready)
        {
            Debug.LogWarning("MikuTTSはまだ準備できていません。");
            return;
        }
        isSmoothVoice = true;
        pendingJTalkGreeting = false;
        Debug.Log("MikuTTSの初期音声を再生します。");
        PlayAudio(initialMikuAudio);
    }
    public void SetSmoothVoiceOff()
    {
        isSmoothVoice = false;
        if (initialJTalkAudio != null)
        {
            Debug.Log("OpenJTalkの初期音声を再生します。");
            PlayAudio(initialJTalkAudio);
        }
        else
        {
            pendingJTalkGreeting = true;
            Debug.Log("OpenJTalkの初期音声を準備中です。");
            if (!preparingJTalkAudio)
            {
                PrepareInitialJTalkAudioAsync().Forget();
            }
        }
    }

    private void StopInitialGreeting()
    {
        pendingJTalkGreeting = false;
        if (audioSource.isPlaying &&
            (audioSource.clip == initialMikuAudio || audioSource.clip == initialJTalkAudio))
        {
            audioSource.Stop();
        }
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

public class SentenceData
{
    public string Text;
    public string EmotionTag;
    public AudioClip Audio;
    
    // 音声の準備が完了したかどうか
    public bool IsAudioReady => Audio != null;

    public SentenceData(string text, string emotionTag)
    {
        this.Text = text;
        this.EmotionTag = emotionTag;
    }
}
