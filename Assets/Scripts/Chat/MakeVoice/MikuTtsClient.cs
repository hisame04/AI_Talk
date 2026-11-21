using System;
using System.Linq;
using System.Collections; // コルーチンの型(IEnumerator)を使うための名前空間を読み込む
using System.Text; // 文字列をUTF-8バイトに変換するための名前空間を読み込む
using UnityEngine; // Unityの基本APIを使うための名前空間を読み込む
using UnityEngine.Networking; // UnityWebRequestなどネットワークAPIを使うための名前空間を読み込む
using Cysharp.Threading.Tasks;
using UniVRM10;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Net;

public class MikuTtsClient : MonoBehaviour
{ 
    public enum ConnectionMode
    {
        Remote,
        Local
    }

    [SerializeField] private ConnectionMode connectionMode = ConnectionMode.Remote;
    [SerializeField] private string remoteSpaceRoot = "https://john6666-mikutts.hf.space"; // リモートスペースのルートURL 
    [SerializeField] private string remoteApiUrl = "https://john6666-mikutts.hf.space/gradio_api/call/tts"; // リモートスペースのTTS API URL
    [SerializeField] private string localSpaceRoot = "http://127.0.0.1:7860"; // ローカルスペースのルートURL 
    [SerializeField] private string localApiUrl = "http://127.0.0.1:7860/gradio_api/call/tts";// ローカルスペースのTTS API URL
    private string spaceRoot;
    private string apiUrl;

    private void Awake()
    {
        ApplyConnectionMode();
    }

    private void ApplyConnectionMode()
    {
        if (connectionMode == ConnectionMode.Local)
        {
            spaceRoot = localSpaceRoot;
            apiUrl = localApiUrl;
        }
        else
        {
            spaceRoot = remoteSpaceRoot;
            apiUrl = remoteApiUrl;
        }
    }

    //*
    // API経由で取得する方法
    // *//
    /* TTSのAPIを叩き音声ファイルを返すメソッド */
    public async UniTask<AudioClip> CallTTS(string modelName, string text)
    {
        var payload = new
        {
            data = new object[]
            {
                modelName,
                0,
                0,
                0,
                text,
                "ja-JP-NanamiNeural-Female",
                6,
                "rmvpe",
                1,
                0.33
            }
        };
        string json = JsonConvert.SerializeObject(payload);

        string eventId = await PostTTS(json);
        if(string.IsNullOrEmpty(eventId)) return null;

        string resultText = await GetTTS(eventId);
        if(string.IsNullOrEmpty(resultText)) return null;

        string audioUrl = ExtractResultAudioUrl(resultText);
        if (string.IsNullOrEmpty(audioUrl))
        {
            Debug.Log("音声URLが見つかりませんでした");
            return null;
        }

        AudioClip clip = await DownloadAudioClip(audioUrl);
        if(clip == null) return null;
        
        return clip;
    }

    /* apiにPOSTするためのメソッド */
    async UniTask<string> PostTTS(string json)
    {
        using var req = new UnityWebRequest(apiUrl, "POST");
        
        byte[] body = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest().ToUniTask();

        if(req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            return null;
        }
        JObject postResult = JObject.Parse(req.downloadHandler.text);

        return postResult["event_id"].ToString();// 音声ファイルを取得するためのIDを返す
        
    }

    /* apiにGETするためのメソッド */
    async UniTask<string> GetTTS(string eventId)
    {
        string getUrl = apiUrl + "/" + eventId;

        using var req =UnityWebRequest.Get(getUrl);
        await req.SendWebRequest().ToUniTask();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(req.error);
            return null;
        }

        return req.downloadHandler.text;
    }

    /* 音声ファイルの取得先を特定するメソッド */
    string ExtractResultAudioUrl(string sseText)
    {
        var dataLines = sseText
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("data:"))
            .Select(line => line.Substring("data:".Length).Trim());

        foreach (string json in dataLines.Reverse())
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                continue;
            }

            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonReaderException)
            {
                continue;
            }

            if (token.Type != JTokenType.Array)
            {
                continue;
            }

            JArray data = (JArray)token;
            if (data.Count <= 2 || data[2] == null)
            {
                continue;
            }

            string url = data[2]["url"]?.ToString();
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            if (url.StartsWith("/"))
            {
                url = spaceRoot + url;
            }

            return url;
        }

        Debug.LogError("SSEレスポンスから有効な音声URLを取得できませんでした。\n" + sseText);
        return null;
    }

    /* 音声ファイルを取得するメソッド */
    async UniTask<AudioClip> DownloadAudioClip(string audioUrl)
    {
        using var req = UnityWebRequestMultimedia.GetAudioClip(audioUrl,AudioType.WAV);
        
        await req.SendWebRequest().ToUniTask();

        if(req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            return null;
        }

        return DownloadHandlerAudioClip.GetContent(req);
    }
}
