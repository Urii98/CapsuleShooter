using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;   
    public GameObject levelContainer;

    [Header("Lobby Elements")]
    public TextMeshProUGUI playerStatusText;   
    public Button startGameButton;  

    [Header("Audio")]
    public AudioSource buttonClickSound;

    private bool isPlayerReady = false; 
    private bool isOpponentReady = false;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        levelContainer.SetActive(false);

        startGameButton.interactable = false;
    }

    public void OnPlayButtonClicked()
    {
        buttonClickSound.Play();
        Invoke("SwitchToLobby", 0.5f);
    }

    void SwitchToLobby()
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        //TODO: Susituir esto por info que venga por parte del servidor
        playerStatusText.text = "Esperando a otro jugador...";
        isPlayerReady = true;

        StartCoroutine(SimulateOpponentJoin());
    }

    public void OnQuitButtonClicked()
    {
        buttonClickSound.Play();
        Invoke("QuitGame", 0.5f);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    //TODO: Esto lo llamamos cuando se conecte otro jugador de verdad
    private IEnumerator SimulateOpponentJoin()
    {
        yield return new WaitForSeconds(3f);

        isOpponentReady = true;
        UpdateLobbyStatus();
    }

    private void UpdateLobbyStatus()
    {
        if (isPlayerReady && isOpponentReady)
        {
            playerStatusText.text = "¡Listos para jugar!";
            startGameButton.interactable = true;
        }
    }

    public void OnStartGameButtonClicked()
    {
        buttonClickSound.Play();
        Invoke("SwitchToGame", 0.5f);
    }

    void SwitchToGame()
    {
        lobbyPanel.SetActive(false);
        levelContainer.SetActive(true);

        Debug.Log("juego empieza");
    }
}
