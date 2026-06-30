using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using TMPro;
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
    public async UniTask<string> PostJsonAsync(string url, string json, CancellationToken cancellationToken = default)
    {
        if (!HasApiKey)
        {
            throw new InvalidOperationException("OPENAI_API_KEY が未設定です。");
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
        await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

        //　HTTP/通信失敗時にエラーをまとめる
        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMessage = request.error;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMessage += "\n" + request.downloadHandler.text;
            }
            throw new Exception(errorMessage);
        }

        //最終的な結果を返す
        return request.downloadHandler.text;   
    }

    // APIエンドポイントに複数データを含んだリストを送信する
    // 音声の送信などに使用
    public async UniTask<string> PostMultipartAsync(string url, List<IMultipartFormSection> formData, CancellationToken cancellationToken = default)
    {
        if (!HasApiKey)
        {
            throw new InvalidOperationException("OPENAI_API_KEY が未設定です。");
        }

        // 送信用リクエストを作成
        using var request = UnityWebRequest.Post(url, formData);
        // OpenAIの認証ヘッダーの設定
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //リクエストの送信処理
        await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

        //　HTTP/通信失敗時にエラーをまとめる
        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMessage = request.error;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMessage += "\n" + request.downloadHandler.text;
            }
            throw new Exception(errorMessage);
        }

        //最終的な結果を返す
        return request.downloadHandler.text;
    }

    public void SetAPIKey(string key)
    {
        apiKey = key;
    }
}