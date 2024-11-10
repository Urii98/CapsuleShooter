using System.Collections;
using UnityEngine;

public class Player : DamageableObject
{
    public float speed = 5;
    public WeaponController weaponController;

    private string playerId;
    private int kills = 0;
    bool isBlocked = false;

    public int Kills => kills;

    [Header("Death VFX")]
    public GameObject deathVFXPrefab; // Prefab del efecto visual de muerte

    public override void Start()
    {
        base.Start();
        weaponController = GetComponent<WeaponController>();
        weaponController.EquipWeapon();

        // Suscribirse al evento de muerte
        OnObjectDied += TriggerDeathVFX;
    }

    private void OnDestroy()
    {
        // Asegurarse de desuscribirse para evitar referencias nulas
        OnObjectDied -= TriggerDeathVFX;
    }

    private void Update()
    {
        if (GameManager.Instance.isMultiplayer && GameManager.Instance.localPlayerId != playerId)
        {
            return;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        if (!isBlocked)
        {
            Vector3 direction = new Vector3(x, 0, z).normalized;
            transform.position += direction * speed * Time.deltaTime;

            RotateTowardsMouse();

            if (Input.GetMouseButton(0))
            {
                weaponController.weapon.Shoot();
            }
        }
    }

    private void RotateTowardsMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Vector3 targetPosition = hitInfo.point;
            targetPosition.y = transform.position.y;
            transform.LookAt(targetPosition);
        }
    }

    private void TriggerDeathVFX()
    {
        if (deathVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfxInstance, 3f);
        }
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(health + amount, totalHealth); 
        Debug.Log($"{playerId} healed by {amount}. Current health: {health}");
    }


    public void AddKill()
    {
        kills++;
    }

    public void SetPlayerId(string id)
    {
        playerId = id;
    }

    public void ResetPlayer()
    {
        health = totalHealth;
        Transform spawnPoint = GameManager.Instance.level.GetSpawnPoint(playerId);
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
    }

    public void BlockMovement()
    {
        isBlocked = true;
    }
}
