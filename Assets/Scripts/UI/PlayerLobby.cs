using UnityEngine;

public class PlayerLobby : MonoBehaviour
{
    private Animator animator;
    private float nextWaveTime; 
    private float minTime = 5f; 
    private float maxTime = 20f;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsJumping", false);
        }

        ScheduleNextWave();
    }

    private void Update()
    {
        if (Time.time >= nextWaveTime)
        {
            TriggerWaveAnimation();
        }
    }

    private void TriggerWaveAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("WaveTrigger");
            ScheduleNextWave();
        }
    }

    private void ScheduleNextWave()
    {
        nextWaveTime = Time.time + Random.Range(minTime, maxTime);
    }
}
