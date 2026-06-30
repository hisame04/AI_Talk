using System;
using System.Linq;
using System.Collections; // コルーチンの型(IEnumerator)を使うための名前空間を読み込む
using System.Text; // 文字列をUTF-8バイトに変換するための名前空間を読み込む
using UnityEngine; // Unityの基本APIを使うための名前空間を読み込む
using UnityEngine.Networking; // UnityWebRequestなどネットワークAPIを使うための名前空間を読み込む
using Cysharp.Threading.Tasks;
using System.Threading;
using UniVRM10;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Net;

public class MikuTtsClient : MonoBehaviour
{ 
    [SerializeField] private string spaceRoot = "https://john6666-mikutts.hf.space";
    [SerializeField] private string apiUrl = "https://john6666-mikutts.hf.space/gradio_api/call/tts";
    [SerializeField] private string proxyUrl = "http://127.0.0.1:8000/tts"; // インスペクターから設定できるプロキシURLを保持する // ゲーミングPCはhttp://10.0.0.19:8000/tts
    private UnityWebRequest activeRequest;// 現在進行中のリクエストを保持しておく変数

    //*
    // ローカル実行で取得する方法
    // *//
    [System.Serializable]
    private class TtsRequest { public Args args; } // リクエストボディの外側構造を表すクラスを定義する
    [System.Serializable]
    private class Args // リクエストのargs部分を表すクラスを定義する
    { 
        public string model_name; // 使用するモデル名を格納する
        public int speed; // 速度パラメータを格納する
        public string tts_text; // 読み上げるテキストを格納する
        public string tts_voice; // TTS音声の種類を格納する
        public int f0_up_key; // ピッチシフト量を格納する
        public string f0_method; // ピッチ推定方式を格納する
        public float index_rate; // インデックス利用率を格納する
        public float protect; // 保護係数を格納する
    }
    [System.Serializable]
    private class AudioUrlResponse { public string audio_url; } // 返却JSONのaudio_urlだけを受けるクラスを定義する
    public async UniTask<AudioClip> GetAudioAsync(string modelName, string text, CancellationToken cancellationToken = default)
    {
        Debug.Log("mikuTTS Speak");
        string wavPath = await RequestAudioURLAsync(modelName, text, cancellationToken);
        var clip = await LoadAudioClipAsync(wavPath, cancellationToken);
        return clip;
    }

    // TTSリクエストから音声ファイルURLを取得するメソッド
    private async UniTask<string> RequestAudioURLAsync(string modelName, string text, CancellationToken cancellationToken = default)
    {
        var requestBody = CreateTtsRequest(modelName, text);
        // HTTPリクエストを送る準備をする
        string json = JsonUtility.ToJson(requestBody); // リクエストをJSON文字列に変換する
        using var request = new UnityWebRequest(proxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)); // JSONをUTF-8バイトにしてアップロードボディに設定する
        request.downloadHandler = new DownloadHandlerBuffer(); // レスポンスをメモリに受け取るハンドラを設定する
        request.SetRequestHeader("Content-Type", "application/json"); // JSON送信であることをヘッダに設定する

        await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken); // リクエスト送信と完了待ちを行う

        if(request.result != UnityWebRequest.Result.Success) // 通信結果が成功かどうかを確認する
        {
            throw new Exception(request.error); // エラー内容を例外として投げる
        }

        var responce = JsonUtility.FromJson<AudioUrlResponse>(request.downloadHandler.text); // 返却JSONから音声URLを取り出す
        if(responce==null || string.IsNullOrEmpty(responce.audio_url))
        {
            throw new Exception("Invalid response: " + request.downloadHandler.text); // audio_urlがない場合は例外を投げる
        }
        return responce.audio_url; // 音声URLを返す
    }


    // 音声URLからAudioClipを取得するメソッド
    private async UniTask<AudioClip> LoadAudioClipAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        using var audioReq = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.WAV); // 音声クリップ取得用リクエストを作成する
        await audioReq.SendWebRequest().ToUniTask(cancellationToken:cancellationToken); // 音声クリップの取得を実行して完了を待つ
        if (audioReq.result != UnityWebRequest.Result.Success) // 音声取得が成功か確認する
        {
            throw new Exception(audioReq.error); // エラー内容を例外として投げる        
        }
        return DownloadHandlerAudioClip.GetContent(audioReq); // 取得したAudioClipを返す
    }

    private TtsRequest CreateTtsRequest(string modelName,string text)
    {
      return new TtsRequest
      {
          args = new Args
          {
              model_name = modelName,
              speed = 0,
              tts_text = text,
              tts_voice = "ja-JP-NanamiNeural-Female",
              f0_up_key = 6,
              f0_method = "pm",
              index_rate = 0f,
              protect = 0.33f
          }
      };
    }

    private void OnDestroy()
    {
        // 通信中であれば強制的に中断させる
        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }
    }


    //*
    // API経由で取得する方法
    // *//
    /* TTSのAPIを叩き音声ファイルを返すメソッド */
    public async UniTask<AudioClip> CallTTS(string modelName, string text)
    {
        string url = apiUrl;

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
