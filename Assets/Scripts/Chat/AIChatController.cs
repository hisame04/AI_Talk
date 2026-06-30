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
using System.Text.RegularExpressions;
using System.Drawing;
using NUnit.Framework;

public class AIChatController : MonoBehaviour
{
    [SerializeField] private OpenAIClient openAIClient;
    private string chat_apiUrl = "https://api.openai.com/v1/chat/completions";
    private List<Message> messageHistory = new List<Message>();
    private bool smoothVoice;
    public OpenJTalkClient openJTalkClient;
    public MikuTtsClient mikuTtsClient;
    private CharactorData currentCharactorData;
    private string charactorSetting;
    private string systemPrompt;

    void Start()
    {
        //キャラクター設定を追加
        AssistantSetCharactor();
    }

    private void AssistantSetCharactor()
    {
        var systemMessage = new Message { role = "system", content = systemPrompt };
        messageHistory.Add(systemMessage);
    }

    // 会話を始める関数（UIのボタンなどから呼ぶ）
    public async UniTask<string> SendMessageToAI(string userMessage, CancellationToken cancellationToken = default)
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
        string aiReply = response.choices[0].message.content;
        Debug.Log("AI Reply: " + aiReply);//【デバッグ用】返答のログ出力

        //返答の記憶
        var assistantMsg = new Message { role = "assistant", content = aiReply };
        messageHistory.Add(assistantMsg);

        return aiReply;
    }

    // キャラクターのデータを設定する関数
    public void SetCharactorData(CharactorData charactorData)
    {
        currentCharactorData = charactorData;
        charactorSetting = charactorData.charactorPrompt;

        systemPrompt = PromptTemplates.systemPromptTemplate.Replace("{CHARACTER_PROMPT}", charactorSetting);
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
