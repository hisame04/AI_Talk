using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAIClient : MonoBehaviour
{
    private string apiKey;

    void Awake()
    {
        apiKey = LocalEnv.Get("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OPENAI_API_KEY が見つかりません。.env.local を設定してください。");
        }
    }

    public bool HasApiKey => !string.IsNullOrEmpty(apiKey);

    // APIエンドポイントにJSONを送信する
    // チャットの文字の送信などに使用
    public IEnumerator PostJson(string url, string json, Action<string> onSuccess, Action<string> onError = null)
    {
        if (!HasApiKey)
        {
            onError?.Invoke("OPENAI_API_KEY が未設定です。");
            yield break;
        }

        // 送信用リクエストの作成
        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        // 送信するJSON本文を設定
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // レスポンス本文を受信できるようにする
        request.downloadHandler = new DownloadHandlerBuffer();
        // リクエスト本文のヘッダーの設定
        request.SetRequestHeader("Content-Type", "application/json");
        // OpenAIの認証ヘッダーの設定
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //リクエストの送信処理
        yield return request.SendWebRequest();

        //　HTTP/通信失敗時にエラーをまとめる
        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMessage = request.error;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMessage += "\n" + request.downloadHandler.text;
            }
            //指定されたエラーコールバックがあれば呼び出す
            onError?.Invoke(errorMessage);
            yield break;
        }

        //指定された成功時コールバックがあれば呼び出す
        onSuccess?.Invoke(request.downloadHandler.text);
    }

    // APIエンドポイントに複数データを含んだリストを送信する
    // 音声の送信などに使用
    public IEnumerator PostMultipart(string url, List<IMultipartFormSection> formData, Action<string> onSuccess, Action<string> onError = null)
    {
        if (!HasApiKey)
        {
            onError?.Invoke("OPENAI_API_KEY が未設定です。");
            yield break;
        }

        // 送信用リクエストを作成
        using var request = UnityWebRequest.Post(url, formData);
        // OpenAIの認証ヘッダーの設定
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //リクエストの送信処理
        yield return request.SendWebRequest();

        //　HTTP/通信失敗時にエラーをまとめる
        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMessage = request.error;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMessage += "\n" + request.downloadHandler.text;
            }
            //指定されたエラーコールバックがあれば呼び出す
            onError?.Invoke(errorMessage);
            yield break;
        }

        //指定された成功時コールバックがあれば呼び出す
        onSuccess?.Invoke(request.downloadHandler.text);
    }
}