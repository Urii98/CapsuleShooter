using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel; 
    public GameObject joinGamePanel; 
    public GameObject lobbyPanel; 
    public GameObject lobbyDecoration; 
    public TextMeshProUGUI lobbyStatusText; 
    public Button startGameButton; 

    public Server server;
    public Client client;
    public GameManager gameManager; 
    public GameObject UIManager;

    public GameObject GameManagerObject; 

    [Header("Audio")]
    public AudioSource buttonClickSound;

    private void Start()
    {
        joinGamePanel.SetActive(false); 
        lobbyPanel.SetActive(false);
        startGameButton.interactable = false; 

        GameManagerObject.SetActive(false); 
    }

    public void OnCreateGameClicked()
    {
        buttonClickSound.Play();

        client = FindAnyObjectByType<Client>();
        server = FindAnyObjectByType<Server>();

        // Assign localPlayerId
        string localPlayerId = "Player1";
        client.localPlayerId = localPlayerId;
        gameManager.localPlayerId = localPlayerId;

        // Set isMultiplayer to true
        gameManager.isMultiplayer = true;

       
        server.StartServer();
        client.ConnectToServer("127.0.0.1", server.port);

        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        lobbyStatusText.text = "Esperando a otros jugadores...";
    }

    public void OnJoinGameClicked()
    {
        buttonClickSound.Play();

        mainMenuPanel.SetActive(false);
        joinGamePanel.SetActive(true);

 
        TMP_InputField ipInput = joinGamePanel.transform.Find("IPInputField").GetComponent<TMP_InputField>();
        TMP_InputField portInput = joinGamePanel.transform.Find("PortInputField").GetComponent<TMP_InputField>();
        ipInput.text = "127.0.0.1";
        portInput.text = "9000";
    }

    public void OnConnectButtonClicked()
    {
        buttonClickSound.Play();

        client = FindAnyObjectByType<Client>();

        
        TMP_InputField ipInput = joinGamePanel.transform.Find("IPInputField").GetComponent<TMP_InputField>();
        TMP_InputField portInput = joinGamePanel.transform.Find("PortInputField").GetComponent<TMP_InputField>();

        string ip = ipInput.text;
        string portText = portInput.text;
        int port = 0;
        if(portText.Length > 0)
        {
            port = int.Parse(portInput.text);
        }

        if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(portText))
        {
            return;
        }

        
        string localPlayerId = "Player2";
        client.localPlayerId = localPlayerId;
        gameManager.localPlayerId = localPlayerId;

        
        gameManager.isMultiplayer = true;

        
        client.ConnectToServer(ip, port);
        
        if(server != null)
        {
            server.enabled = false;
        }

        
        joinGamePanel.SetActive(false);
        lobbyDecoration.SetActive(false); 
        lobbyPanel.SetActive(true);

      
        lobbyStatusText.text = "Esperando a que el anfitrión inicie el juego...";
    }

    public void OnStartGameClicked()
    {
        buttonClickSound.Play();

        
        server.StartGame();
    }

    
    public void EnableStartButton()
    {
        startGameButton.interactable = true;
        lobbyStatusText.text = "Jugador conectado. Puedes iniciar el juego.";
    }

    
    public void StartGame()
    {
        
        UIManager.SetActive(false);
        lobbyDecoration.SetActive(false);
        GameManagerObject.SetActive(true);

        client.gameManager = GameManager.Instance;
    }
}
