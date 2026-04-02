using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using Christina.UI;

public class AIChatController : MonoBehaviour
{
    private string apiKey;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private List<Message> messageHistory = new List<Message>();
    private bool smoothVoice;
    public OpenJTalkClient openJTalkClient;
    public MikuTtsClient mikuTtsClient;

    void Awake()
    {
        apiKey = LocalEnv.Get("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OPENAI_API_KEY が見つかりません。.env.local を設定してください。");
        }
    }

    void Start()
    {
        var systemMessage = new Message { role = "system", content = "あなたは初音ミクです。短く可愛らしく返事をしてください。英単語はアルファベットはカタカナに置き換えてから返答してください。絵文字や顔文字の使用は禁止です。" };
        messageHistory.Add(systemMessage);
    }

    // 会話を始める関数（UIのボタンなどから呼ぶ）
    public void SendMessageToMiku(string userMessage)
    {
        StartCoroutine(PostRequest(userMessage));
    }

    IEnumerator PostRequest(string text)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OPENAI_API_KEY が未設定のため、APIリクエストを送信できません。");
            yield break;
        }

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

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))// APIルートにPOSTを投げる
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                string mikuReply = response.choices[0].message.content;

                Debug.Log("ミク: " + mikuReply);
                //音声の再生
                if (smoothVoice)
                {
                    mikuTtsClient.Speak(mikuReply);
                }
                else
                {
                    openJTalkClient.Speak(mikuReply);
                }
                
                // ここでテキストUIに表示したり、次の音声合成に渡したりする

                //返答の記憶
                var assistantMsg = new Message { role = "assistant", content = mikuReply };
                messageHistory.Add(assistantMsg);
            }
            else
            {
                Debug.LogError("通信エラー: " + request.error);

                if (request.downloadHandler != null)
                {
                    Debug.LogError("詳細な原因: " + request.downloadHandler.text);
                }
            }
        }
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

    //使用する読み上げモードをトグルスイッチ経由で更新するメソッド
    public void SetSmoothVoiceOn()
    {
        smoothVoice = true;
    }
    public void SetSmoothVoiceOff()
    {
        smoothVoice = false;
    }

}
