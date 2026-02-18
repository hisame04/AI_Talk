using System.Collections; // コルーチンの型(IEnumerator)を使うための名前空間を読み込む
using System.Text; // 文字列をUTF-8バイトに変換するための名前空間を読み込む
using UnityEngine; // Unityの基本APIを使うための名前空間を読み込む
using UnityEngine.Networking; // UnityWebRequestなどネットワークAPIを使うための名前空間を読み込む
public class MikuTtsClient : MonoBehaviour
{ 
    [SerializeField] private string proxyUrl = "http://127.0.0.1:8000/tts"; // インスペクターから設定できるプロキシURLを保持する
    [SerializeField] private AudioSource audioSource; // 再生に使うAudioSource参照を保持する
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
    public void Speak(string text)
    {
        Debug.Log("mikuTTS Speak");
        StartCoroutine(SpeakCoroutine(text));
    }
    private IEnumerator SpeakCoroutine(string text)
    {
        Debug.Log($"Proxy URL: {proxyUrl}"); // 利用するプロキシURLをログに出す
        var req = new TtsRequest // リクエストオブジェクトを生成する
        {
            args = new Args // argsオブジェクトを生成する
            {
                model_name = "1a_miku_default_rvc_(aple)", // モデル名を設定する
                speed = 0, // 速度を設定する
                tts_text = text, // 入力テキストを設定する
                tts_voice = "ja-JP-NanamiNeural-Female", // 音声種類を設定する
                f0_up_key = 6, // ピッチシフトを設定する
                f0_method = "pm", // ピッチ推定方式を設定する
                index_rate = 0f, // インデックス利用率を設定する
                protect = 0.33f // 保護係数を設定する
            }
        };

        // HTTPリクエストを送る準備をする
        string json = JsonUtility.ToJson(req); // リクエストをJSON文字列に変換する
        using (var www = new UnityWebRequest(proxyUrl, "POST")) // POSTリクエストを作成する
        {
            activeRequest = www;
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)); // JSONをUTF-8バイトにしてアップロードボディに設定する
            www.downloadHandler = new DownloadHandlerBuffer(); // レスポンスをメモリに受け取るハンドラを設定する
            www.SetRequestHeader("Content-Type", "application/json"); // JSON送信であることをヘッダに設定する
            yield return www.SendWebRequest(); // リクエスト送信と完了待ちを行う
            if (this == null) yield break;
            if (www.result != UnityWebRequest.Result.Success) // 通信結果が成功かどうかを確認する
            {
                Debug.LogError(www.error); // エラー内容をログに出す
                yield break; // コルーチンを終了する
            }
            var audioUrl = JsonUtility.FromJson<AudioUrlResponse>(www.downloadHandler.text).audio_url; // レスポンスJSONから音声URLを取り出す
            using (var audioReq = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.WAV)) // 音声クリップ取得用リクエストを作成する
            {
                yield return audioReq.SendWebRequest(); // 音声クリップの取得を実行して完了を待つ
                if (audioReq.result != UnityWebRequest.Result.Success) // 音声取得が成功か確認する
                {
                    Debug.LogError(audioReq.error);
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(audioReq); // 取得したAudioClipを取り出す
                Debug.Log(www.downloadHandler.text);
                Debug.Log($"audio clip: {clip}");
                audioSource.clip = clip; // AudioSourceにクリップを設定する
                audioSource.Play(); // クリップの再生を開始する
                activeRequest = null;
            }
        }
        
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
