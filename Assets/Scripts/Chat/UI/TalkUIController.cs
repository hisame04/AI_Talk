using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;  

public class TalkUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [ SerializeField] private Button startRecordingButton;
    [ SerializeField] private Button stopRecordingButton;
    [SerializeField] private ConversationManager conversationManager;
    [SerializeField] private GameObject configPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sendButton.onClick.AddListener(OnInputTextSend);
        startRecordingButton.onClick.AddListener(conversationManager.OnStartRecording);
        stopRecordingButton.onClick.AddListener(OnStopRecording);
        configPanel.SetActive(false); //初期状態では設定パネルを非表示にする
        OnVoiceInputUnable(); //初期状態ではテキスト入力モードを有効にする
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            configPanel.SetActive(!configPanel.activeSelf);
        }
    }

    //録音停止字に呼ぶメソッド
    async void OnStopRecording()
    {
        try
        {
            await conversationManager.OnStopRecording();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }
    
    // テキスト送信ボタンを押した時に呼ぶメソッド
    async void OnInputTextSend()
    {
        try
        {
            await OnSendAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }
    }

    // テキスト送信ボタンが押されたときの処理
    async UniTask OnSendAsync()
    {
        if(string.IsNullOrEmpty(inputField.text)) return;
        sendButton.interactable = false;
        string text = inputField.text;
        inputField.text = "";

        try
        {
            //APIに入力内容を送信
            await conversationManager.OnReceiveMikuReplyAsync(text);
        }
        finally
        {
            sendButton.interactable = true;
        }
    }

    public void OnVoiceInputAble()
    {
        startRecordingButton.gameObject.SetActive(true); //録音開始ボタンを表示
        stopRecordingButton.gameObject.SetActive(true); //録音停止ボタンを表示
        sendButton.gameObject.SetActive(false); //テキスト送信ボタンを非表示
        inputField.gameObject.SetActive(false); //テキスト入力フィールドを非表示
    }

    public void OnVoiceInputUnable()
    {
        startRecordingButton.gameObject.SetActive(false); //録音開始ボタンを非表示
        stopRecordingButton.gameObject.SetActive(false); //録音停止ボタンを非表示
        sendButton.gameObject.SetActive(true); //テキスト送信ボタンを表示
        inputField.gameObject.SetActive(true); //テキスト入力フィールドを表示
    }
}
