using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TalkUIController : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button sendButton;
    public AIChatController aIChatController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sendButton.onClick.AddListener(OnSend);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnSend()
    {
        if(string.IsNullOrEmpty(inputField.text)) return;
        sendButton.interactable = false;

        //APIに入力内容を送信
        aIChatController.SendMessageToMiku(inputField.text);

        inputField.text = "";
        Invoke("ReEnableButton", 2.0f);
    }

    void ReEnableButton()
    {
        sendButton.interactable = true;
    }
}
