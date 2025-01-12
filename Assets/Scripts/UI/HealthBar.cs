using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBarImage;
    [SerializeField] private float reduceSpeed = 2f;
    private float target = 1; 
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        target = currentHealth / maxHealth;
    }

    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        healthBarImage.fillAmount = Mathf.MoveTowards(healthBarImage.fillAmount, target, reduceSpeed * Time.deltaTime);
    }
}
