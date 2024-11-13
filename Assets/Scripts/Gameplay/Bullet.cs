using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10;
    public float timeToDestroy = 3.0f;
    public float damage;

    private string shooterId;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.velocity = transform.forward * speed;
    }   
    public void Initialize(string shooter)
    {
        shooterId = shooter;
    }

    private void OnCollisionEnter(Collision collision)
    {
        DamageableObject target = collision.collider.GetComponent<DamageableObject>();
        if (target != null)
        {
            target.TakeDamage(damage);
            Debug.Log($"Impacto reportado: {shooterId}");
        }

        Destroy(gameObject);
    }
}
