using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Newtonsoft.Json;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AIChatController : MonoBehaviour
{
    [SerializeField] private OpenAIClient openAIClient;
    private string chat_apiUrl = "https://api.openai.com/v1/chat/completions";
    private List<Message> messageHistory = new List<Message>();
    private bool smoothVoice;
    public OpenJTalkClient openJTalkClient;
    public MikuTtsClient mikuTtsClient;

    void Start()
    {
        //キャラクターを初音ミクに設定
        string charactorSetting = "あなたは初音ミクです。短く可愛らしく返事をしてください。英単語はアルファベットはカタカナに置き換えてから返答してください。絵文字や顔文字の使用は禁止です。";
        AssistantSetCharactor(charactorSetting);
    }

    private void AssistantSetCharactor(string charactorSetting)
    {
        var systemMessage = new Message { role = "system", content = charactorSetting };
        messageHistory.Add(systemMessage);
    }

    // 会話を始める関数（UIのボタンなどから呼ぶ）
    public async UniTask<string> SendMessageToMiku(string userMessage, CancellationToken cancellationToken = default)
    {
        //入力内容の記憶
        var userMsg = new Message { role = "user", content = userMessage };
        messageHistory.Add(userMsg);

        // 送信するデータを作成
        var messageData = new
        {
            model = "gpt-4o-mini", // コスパが良いモデル
            messages = messageHistory
        };
        string json = JsonConvert.SerializeObject(messageData);
        string responseText = await openAIClient.PostJsonAsync(chat_apiUrl, json, cancellationToken);
        
        var response = JsonConvert.DeserializeObject<OpenAIResponse>(responseText);
        string mikuReply = response.choices[0].message.content;


        //返答の記憶
        var assistantMsg = new Message { role = "assistant", content = mikuReply };
        messageHistory.Add(assistantMsg);

        return mikuReply;
    }

    // レスポンス受け取り用のクラス定義
    public class OpenAIResponse
    {
        public Choice[] choices;
    }
    public class Choice
    {
        public Message message;
    }
    public class Message
    {
        public string role;
        public string content;
    }

}
