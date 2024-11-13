using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class Player : DamageableObject
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float fuerzaSalto = 5f;
    [SerializeField] private float sensibilidadRaton = 2f;

    [Header("Environment")]
    [SerializeField] private bool enSuelo = false;
    [SerializeField] private float radioSuelo = 0.2f;
    [SerializeField] private LayerMask capaSuelo;
    public float distanciaRaycast = 1.1f; 

    [Header("Camera Setup")]
    // Variables de cámara
    private Camera camara;
    [SerializeField] private float distanciaCamara = 5f;
    [SerializeField] private Vector3 offsetCamara = new Vector3(0, 2, -5);


    public WeaponController weaponController;
    private Rigidbody rb;
    private float rotacionY = 0f;
    public string playerId;
    private int kills = 0;
    bool isBlocked = false;

    public int Kills => kills;

    [Header("Death VFX")]
    public GameObject deathVFXPrefab;

    GameManager gameManager; 

    public override void Start()
    {
        base.Start();
        PlayerSetup();
        gameManager = FindObjectOfType<GameManager>();
    }

    public void PlayerSetup()
    {
        health = totalHealth;

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        weaponController = GetComponent<WeaponController>();
        weaponController.EquipWeapon();

        OnObjectDied += TriggerDeathVFX;
    }

    void SetupCamara()
    {

        camara = Camera.main;
        camara.transform.parent = this.transform;
        camara.transform.localPosition = offsetCamara;
        camara.transform.localRotation = Quaternion.identity;

    }

    private void OnDestroy()
    {
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

        if (!isBlocked)
        {
            // Rotación con el ratón
            float movimientoRatonX = Input.GetAxis("Mouse X") * sensibilidadRaton;
            rotacionY += movimientoRatonX;
            transform.rotation = Quaternion.Euler(0, rotacionY, 0);

            // Salto
            if (Input.GetKeyDown(KeyCode.Space) && enSuelo)
            {
                rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
                enSuelo = false;
            }

            if (Input.GetMouseButton(0))
            {
                gameManager.SendEvent(Events.SHOOT);
                weaponController.weapon.Shoot();
            }
        }
    }

    void FixedUpdate()
    {
        
        float movimientoX = Input.GetAxis("Horizontal");
        float movimientoZ = Input.GetAxis("Vertical");  

        Vector3 movimiento = transform.right * movimientoX + transform.forward * movimientoZ;
        Vector3 velocidadDeseada = movimiento * velocidad;
        Vector3 velocidadActual = rb.velocity;
        Vector3 fuerza = velocidadDeseada - new Vector3(velocidadActual.x, 0, velocidadActual.z);
        rb.AddForce(fuerza, ForceMode.VelocityChange);

        RaycastHit hit;

        Vector3 origenRaycast = transform.position + Vector3.up * 0.1f; 
        if (Physics.Raycast(origenRaycast, Vector3.down, out hit, distanciaRaycast, capaSuelo))
        {
            enSuelo = true;
        }
        else
        {
            enSuelo = false;
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

    public void SetPlayerAsLocal()
    {
        SetupCamara();
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
