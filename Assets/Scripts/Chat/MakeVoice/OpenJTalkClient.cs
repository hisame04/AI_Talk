using System;
using System.Collections;
using System.IO;
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

    [SerializeField] AudioSource audioSource;

    string dicPath;
    string voicePath;

    void Start()
    {
        dicPath = System.IO.Path.Combine(Application.streamingAssetsPath, "mecab-naist-jdic");
        voicePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Miku_Voice.htsvoice");

        OpenJTalk_initialize(voicePath, dicPath);
    }

    public void Speak(string text)
    {
        StartCoroutine(SpeakCoroutine(text));
    }

    IEnumerator SpeakCoroutine(string text)
    {
        string wavPath = Path.Combine(Application.persistentDataPath, "jtalk.wav");

        bool ok = OpenJTalk_speak_to_wav(text, wavPath);
        if (!ok)
        {
            Debug.LogError("WAV書き出しに失敗しました");
            yield break;
        }

        string url = "file://" + wavPath;
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("WAV読み込み失敗: " + req.error);
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(req);
        audioSource.clip = clip;
        audioSource.Play();
    }

    void OnDestroy()
    {
        OpenJTalk_clear();
    }
}
