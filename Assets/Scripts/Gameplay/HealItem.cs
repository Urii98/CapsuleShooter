using UnityEngine;

public class HealItem : MonoBehaviour
{
    public float healAmount = 20f;

    GameManager gameManager;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.Heal(healAmount);
            gameManager.SendEvent(Events.HEAL); 

            Destroy(gameObject);
        }
    }
}
