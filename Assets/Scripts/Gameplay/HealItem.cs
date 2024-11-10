using UnityEngine;

public class HealItem : MonoBehaviour
{
    public float healAmount = 20f; 

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
