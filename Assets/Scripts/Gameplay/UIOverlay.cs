using System.Collections;
using TMPro;
using UnityEngine;

public class UIOverlay : MonoBehaviour
{
    public TextMeshProUGUI UIText;
    public Banner banner;
    private Player player;

    public void StartUI(Player localPlayer)
    {
        player = localPlayer;
        UpdateUI();
    }

    private void Update()
    {
        if (player == null) return;
        UpdateUI();
    }

    private void UpdateUI()
    {
        UIText.text =
            $"Health: {player.health:0}%\n";
    }

    public void ShowDeathMessage(float respawnTime)
    {
        banner.ShowTemporaryBanner($"Has muerto, reapareciendo en {respawnTime} segundos...", respawnTime);
    }
}
