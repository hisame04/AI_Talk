using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public class WhisperSpeechToText : MonoBehaviour
{

	[SerializeField]
	private string openAIApiKey;
	[SerializeField] private InputField _textInterface;

	public int frequency = 16000; // 周波数
	public int maxRecordingTime; // 録音最大時間

	private AudioClip clip;
	private float recordingTime;

	void Update()
	{
	    // レコーディング中であれば
	    if (IsRecording()) 
	    {
		recordingTime += Time.deltaTime;
		// レコーディング時間が超えていないことを確認する
		if (Mathf.FloorToInt(recordingTime) >= maxRecordingTime)
		{
		    StopRecording();
		}
	    }
	}

	public void StartRecording()
	{
	    recordingTime = 0;
	    // すでにレコーディング中であればレコーディングを止める
	    if (IsRecording())
	    {
		Microphone.End(null);
	    }

	    // マイクの録音を開始する
	    Debug.Log("RecordingStart");
	    clip = Microphone.Start(null, true, maxRecordingTime, frequency);
	    // 録音が正しく開始されたかを確認
	    if (clip == null)
	    {
		Debug.LogError("Microphone recording failed.");
	    }
	}

	public bool IsRecording()
	{
	    return Microphone.IsRecording(null);
	}

	public void StopRecording() 
	{
	    Debug.Log("RecordingStop.");
	    // マイクのレコーディングを止める
	    Microphone.End(null);

	    // AudioClipをWAV形式のバイナリデータに変換する
	    var audioData = WavUtility.FromAudioClip(clip);

	    // Send HTTP request to Whisper API
	    StartCoroutine(SendRequest(audioData));
	}

	IEnumerator SendRequest(byte[] audioData) 
	{
	    string url = "https://api.openai.com/v1/audio/transcriptions";
	    string accessToken = openAIApiKey;

	    // フォームデータを作成する
	    var formData = new List<IMultipartFormSection>();
	    formData.Add(new MultipartFormDataSection("model", "whisper-1"));
	    formData.Add(new MultipartFormDataSection("language", "ja"));
	    formData.Add(new MultipartFormFileSection("file", audioData, "audio.wav", "multipart/form-data"));

	    // UnityWebRequestを作成する
	    using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
	    {
		// リクエストヘッダーを設定
		request.SetRequestHeader("Authorization", "Bearer " + accessToken);

		// リクエストを送信し、応答を待機
		yield return request.SendWebRequest();

		// エラー処理
		if (request.result != UnityWebRequest.Result.Success) 
		{
		    Debug.LogError(request.error);
		    yield break;
		}

		// JSONデータのレスポンスをパースする
		string jsonResponse = request.downloadHandler.text;
		string recognizedText = "";
		try 
		{
		    recognizedText = JsonUtility.FromJson<WhisperResponseModel>(jsonResponse).text;
		} 
		catch (System.Exception e) 
		{
		    Debug.LogError(e.Message);
		}

		// 書き起こしされたテキストを出力する
		Debug.Log("Input Text: " + recognizedText);
		_textInterface.text = recognizedText;

	    }
	}
}

public static class WavUtility 
{
	public static byte[] FromAudioClip(AudioClip clip)
	{
	    using var stream = new MemoryStream();
	    using var writer = new BinaryWriter(stream);
	    // Write WAV header
	    writer.Write(0x46464952); // "RIFF"
	    writer.Write(0); // ChunkSize
	    writer.Write(0x45564157); // "WAVE"
	    writer.Write(0x20746d66); // "fmt "
	    writer.Write(16); // Subchunk1Size
	    writer.Write((ushort)1); // AudioFormat
	    writer.Write((ushort)clip.channels); // NumChannels
	    writer.Write(clip.frequency); // SampleRate
	    writer.Write(clip.frequency * clip.channels * 2); // ByteRate
	    writer.Write((ushort)(clip.channels * 2)); // BlockAlign
	    writer.Write((ushort)16); // BitsPerSample
	    writer.Write(0x61746164); // "data"
	    writer.Write(0); // Subchunk2Size

	    // Write audio data
	    float[] samples = new float[clip.samples];
	    clip.GetData(samples, 0);
	    short[] intData = new short[samples.Length];
	    for (int i = 0; i < samples.Length; i++) 
	    {
		intData[i] = (short)(samples[i] * 32767f);
	    }
	    byte[] data = new byte[intData.Length * 2];
	    Buffer.BlockCopy(intData, 0, data, 0, data.Length);
	    writer.Write(data);

	    // Update ChunkSize and Subchunk2Size fields
	    writer.Seek(4, SeekOrigin.Begin);
	    writer.Write((int)(stream.Length - 8));
	    writer.Seek(40, SeekOrigin.Begin);
	    writer.Write((int)(stream.Length - 44));

	    // Close streams and return WAV data
	    writer.Close();
	    stream.Close();
	    return stream.ToArray();
	}
}

public class WhisperResponseModel
{
	public string text;
}