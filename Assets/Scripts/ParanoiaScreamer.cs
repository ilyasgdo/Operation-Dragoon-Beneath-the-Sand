using UnityEngine;
using System.Collections;

public class ParanoiaScreamer : MonoBehaviour
{
    public SpriteRenderer ghostRenderer;
    public AudioSource audioSource;
    public float displayDuration = 1.5f;
    public float distanceInFront = 2.0f;
    public float heightOffset = -0.2f;

    [Header("Random Spawning")]
    public bool useRandomSpawning = true;
    public float minInterval = 45f;
    public float maxInterval = 90f;

    private float nextTriggerTime;

    private void Start()
    {
        if (ghostRenderer != null) ghostRenderer.enabled = false;
        if (useRandomSpawning) ScheduleNext();
    }

    private void Update()
    {
        if (useRandomSpawning && Time.time >= nextTriggerTime)
        {
            TriggerScreamer();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        nextTriggerTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    [ContextMenu("Trigger Screamer")]
    public void TriggerScreamer()
    {
        StopAllCoroutines();
        StartCoroutine(ScreamerRoutine());
    }

    private IEnumerator ScreamerRoutine()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        // Position in front of the player camera
        Vector3 spawnPos = mainCam.transform.position + mainCam.transform.forward * distanceInFront;
        spawnPos.y += heightOffset;
        
        transform.position = spawnPos;
        
        // Face the player
        transform.LookAt(mainCam.transform);
        transform.Rotate(0, 180, 0); 

        // Show and Play
        if (ghostRenderer != null) ghostRenderer.enabled = true;
        if (audioSource != null) audioSource.Play();

        yield return new WaitForSeconds(displayDuration);

        // Hide
        if (ghostRenderer != null) ghostRenderer.enabled = false;
    }
}