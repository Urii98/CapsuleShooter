using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Logger : MonoBehaviour
{
    public static Logger Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect; 
    [SerializeField] private int maxLogMessages = 100; 

    private Queue<string> logMessages = new Queue<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Log(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";

        logMessages.Enqueue(formattedMessage);

        if (logMessages.Count > maxLogMessages)
        {
            logMessages.Dequeue();
        }

        UpdateLogText();
        Debug.Log(message);

    }

    private void UpdateLogText()
    {
        logText.text = string.Join("\n", logMessages);

        if(scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;

        }
    }

}
