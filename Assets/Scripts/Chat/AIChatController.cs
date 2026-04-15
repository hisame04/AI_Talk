using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Newtonsoft.Json;

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
        var systemMessage = new Message { role = "system", content = "あなたは初音ミクです。短く可愛らしく返事をしてください。英単語はアルファベットはカタカナに置き換えてから返答してください。絵文字や顔文字の使用は禁止です。" };
        messageHistory.Add(systemMessage);
    }

    // 会話を始める関数（UIのボタンなどから呼ぶ）
    public void SendMessageToMiku(string userMessage, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(PostRequest(userMessage, onSuccess, onError));
    }

    IEnumerator PostRequest(string text, Action<string> onSuccess, Action<string> onError)
    {
        //入力内容の記憶
        var userMsg = new Message { role = "user", content = text };
        messageHistory.Add(userMsg);

        // 送信するデータを作成
        var messageData = new
        {
            model = "gpt-4o-mini", // コスパが良いモデル
            messages = messageHistory
        };

        string json = JsonConvert.SerializeObject(messageData);

        yield return openAIClient.PostJson(
            chat_apiUrl,
            json,
            onSuccess: (responseText) =>
             {
                 var response = JsonConvert.DeserializeObject<OpenAIResponse>(responseText);
                 string mikuReply = response.choices[0].message.content;

                 Debug.Log("ミク: " + mikuReply);
                 onSuccess?.Invoke(mikuReply); // ここでテキストUIに表示したり、次の音声合成に渡したりする
                 
                 // ここでテキストUIに表示したり、次の音声合成に渡したりする

                 //返答の記憶
                 var assistantMsg = new Message { role = "assistant", content = mikuReply };
                 messageHistory.Add(assistantMsg);
             },
             onError: (errorMessage) =>
             {
                Debug.LogError("通信エラー: " + errorMessage);
                onError?.Invoke(errorMessage);
             }
        );
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
