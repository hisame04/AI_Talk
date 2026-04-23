using System;
using System.Collections;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
 using UnityEngine.Networking;
using UnityEngine.Rendering;

public class OpenJTalkClient : MonoBehaviour
{
    const string LibName = "openjtalk"; // openjtalk.bundle 内の実体名
    [DllImport(LibName)] static extern void OpenJTalk_initialize(string voicePath, string dicPath);
    [DllImport(LibName)] static extern bool OpenJTalk_speak_to_wav(string text, string wavPath);
    [DllImport(LibName)] static extern void OpenJTalk_clear();

    string dicPath;
    string voicePath;

    void Start()
    {
        dicPath = System.IO.Path.Combine(Application.streamingAssetsPath, "mecab-naist-jdic");
        voicePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Miku_Voice.htsvoice");

        OpenJTalk_initialize(voicePath, dicPath);
    }

    public async UniTask<AudioClip> GetAudioAsync(string text, CancellationToken cancellationToken = default)
    {
        string wavPath = await RequestAudioURLAsync(text, cancellationToken);
        var clip = await LoadAudioClipAsync(wavPath, cancellationToken);
        return clip;
    }

    // TTSリクエストから音声ファイルURLを取得するメソッド
    private async UniTask<string> RequestAudioURLAsync(string text, CancellationToken cancellationToken = default)
    {
        string wavPath = Path.Combine(Application.persistentDataPath, "jtalk.wav");
        bool ok = OpenJTalk_speak_to_wav(text, wavPath);
        if (!ok)
        {
            Debug.LogError("WAV書き出しに失敗しました");
            return null;
        }
        return wavPath;
    }

    // 音声URLからAudioClipを取得するメソッド
    private async UniTask<AudioClip> LoadAudioClipAsync(string wavPath, CancellationToken cancellationToken = default)
    {
        string url = "file://" + wavPath;
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        await req.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("WAV読み込み失敗: " + req.error);
            return null;
        }

        return DownloadHandlerAudioClip.GetContent(req);
    }

    void OnDestroy()
    {
        OpenJTalk_clear();
    }
}
