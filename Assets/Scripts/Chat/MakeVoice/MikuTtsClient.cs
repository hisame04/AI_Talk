using System;
using System.Collections; // コルーチンの型(IEnumerator)を使うための名前空間を読み込む
using System.Text; // 文字列をUTF-8バイトに変換するための名前空間を読み込む
using UnityEngine; // Unityの基本APIを使うための名前空間を読み込む
using UnityEngine.Networking; // UnityWebRequestなどネットワークAPIを使うための名前空間を読み込む
using Cysharp.Threading.Tasks;
using System.Threading;
using UniVRM10;
using System.Threading.Tasks;

public class MikuTtsClient : MonoBehaviour
{ 
    [SerializeField] private string proxyUrl = "http://127.0.0.1:8000/tts"; // インスペクターから設定できるプロキシURLを保持する // ゲーミングPCはhttp://10.0.0.19:8000/tts
    private UnityWebRequest activeRequest;// 現在進行中のリクエストを保持しておく変数
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
}
