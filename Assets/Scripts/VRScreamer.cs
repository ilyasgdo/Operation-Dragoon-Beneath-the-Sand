using UnityEngine;
using System.Collections;

public class VRScreamer : MonoBehaviour
{
    [Header("Ghost Configuration")]
    public GameObject ghostPrefab;
    public Transform spawnPoint;
    public float displayDuration = 3.0f;
    public float cooldown = 10.0f;

    [Header("Dialogue / Audio")]
    public AudioClip dialogueClip;
    public string subtitleText = "Hey... you shouldn't be here...";

    private bool isTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            StartCoroutine(SpawnGhost());
        }
    }

    IEnumerator SpawnGhost()
    {
        isTriggered = true;
        GameObject ghost = null;
        
        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab, spawnPoint.position, spawnPoint.rotation);
            // Optional: add transparency or ghost material here
        }

        if (dialogueClip != null)
        {
            audioSource.PlayOneShot(dialogueClip);
        }

        Debug.Log("Ghost Spawning: " + subtitleText);

        yield return new WaitForSeconds(displayDuration);

        if (ghost != null)
        {
            Destroy(ghost);
        }

        yield return new WaitForSeconds(cooldown);
        isTriggered = false;
    }
}
