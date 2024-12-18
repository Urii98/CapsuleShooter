using UnityEngine;

public class HealItem : MonoBehaviour
{
    public float healAmount = 20f;
    public int healId;
    [HideInInspector] public GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.Heal(healAmount);
            gameManager.SendEvent(Events.HEAL);

            HealData hd = new HealData { id = healId, position = Vector3.zero };
            string msg = "HealPicked:" + JsonUtility.ToJson(hd);
            gameManager.client.SendToServer(msg);

            BoxCollider bc = this.GetComponent<BoxCollider>();
            bc.enabled = false;
   
        }
    }
}
