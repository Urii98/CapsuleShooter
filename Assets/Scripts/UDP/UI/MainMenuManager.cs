using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel; // El panel del men� principal
    public GameObject joinGamePanel; // El panel para unirse a una partida
    public GameObject lobbyPanel; // El panel del lobby
    public TextMeshProUGUI lobbyStatusText; // El texto de estado en el lobby
    public Button startGameButton; // El bot�n para iniciar el juego

    public Server server;
    public Client client;

    public GameObject GameManager; // Referencia al GameObject del juego
    public GameObject UIManager;

    private void Start()
    {
        joinGamePanel.SetActive(false); // Ocultar el panel de unirse al inicio
        lobbyPanel.SetActive(false); // Ocultar el lobby al inicio
        startGameButton.interactable = false; // Deshabilitar el bot�n de inicio al inicio

        GameManager.SetActive(false); // Asegurarse de que el GameManager est� desactivado al inicio
    }

    public void OnCreateGameClicked()
    {
        // Iniciar el servidor
        server.StartServer();

        // El servidor tambi�n act�a como cliente y se conecta a s� mismo
        client.ConnectToServer("127.0.0.1", server.port);

        // Desactivar el men� principal y activar el lobby
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        // Actualizar el estado del lobby
        lobbyStatusText.text = "Esperando a otros jugadores...";
    }

    public void OnJoinGameClicked()
    {
        // Mostrar el panel para introducir IP y puerto
        joinGamePanel.SetActive(true);
    }

    public void OnConnectButtonClicked()
    {
        // Obtener referencias a los InputFields
        TMP_InputField ipInput = joinGamePanel.transform.Find("IPInputField").GetComponent<TMP_InputField>();
        TMP_InputField portInput = joinGamePanel.transform.Find("PortInputField").GetComponent<TMP_InputField>();

        string ip = ipInput.text;
        int port = int.Parse(portInput.text);

        // Conectarse al servidor
        bool connected = client.ConnectToServer(ip, port);

        if (connected)
        {
            // Ocultar el panel de conexi�n y mostrar el lobby
            joinGamePanel.SetActive(false);
            lobbyPanel.SetActive(true);

            // Actualizar el estado del lobby
            lobbyStatusText.text = "Esperando a que el anfitri�n inicie el juego...";
        }
      
    }

    public void OnStartGameClicked()
    {
        // Indicar al servidor que inicie el juego
        server.StartGame();
    }

    // M�todo para habilitar el bot�n de inicio cuando haya m�s de un jugador
    public void EnableStartButton()
    {
        startGameButton.interactable = true;
        lobbyStatusText.text = "Jugador conectado. Puedes iniciar el juego.";
    }

    // M�todo que los clientes llamar�n para iniciar el juego
    public void StartGame()
    {
        // Desactivar el Lobby y activar el GameManager
        UIManager.SetActive(false);
        GameManager.SetActive(true);
    }
}