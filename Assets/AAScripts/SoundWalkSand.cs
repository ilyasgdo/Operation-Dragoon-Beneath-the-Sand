using UnityEngine;

public class SoundWalkSand : MonoBehaviour
{
    [Header("Configuration Audio")]
    [Tooltip("La source audio qui jouera le son des pas")]
    public AudioSource audioSource;
    
    [Tooltip("Le son qui sera joué quand le joueur marche sur le sable")]
    public AudioClip footstepSound;
    
    [Tooltip("Le tag du joueur pour détecter la collision")]
    public string playerTag = "Player";
    
    [Tooltip("Volume du son des pas")]
    [Range(0f, 1f)]
    public float volume = 1.0f;
    
    [Tooltip("Délai minimum entre les sons (pour éviter la répétition trop rapide)")]
    public float minTimeBetweenSounds = 0.3f;
    
    private bool playerIsOnSurface = false;
    private float lastSoundTime = 0f;
    
    // Start est appelé avant la première exécution de Update après la création du MonoBehaviour
    void Start()
    {
        // Si aucune source audio n'est assignée, on essaie d'en obtenir une sur cet objet
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            
            // Si aucune source audio n'existe, on en crée une
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // Son 3D
                audioSource.volume = volume;
            }
        }
    }
    
    // OnTriggerEnter est appelé quand un autre collider entre dans le trigger
    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur qui est entré dans le trigger
        if (other.CompareTag(playerTag))
        {
            playerIsOnSurface = true;
            
            // Jouer le son si un son est assigné et si le délai minimum est respecté
            if (footstepSound != null && Time.time - lastSoundTime > minTimeBetweenSounds)
            {
                audioSource.clip = footstepSound;
                audioSource.Play();
                lastSoundTime = Time.time;
            }
        }
    }
    
    // OnTriggerExit est appelé quand un autre collider sort du trigger
    void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur qui est sorti du trigger
        if (other.CompareTag(playerTag))
        {
            playerIsOnSurface = false;
        }
    }
    
    // Cette méthode peut être appelée par le contrôleur du joueur pour jouer le son des pas
    public void PlayFootstepSound()
    {
        if (playerIsOnSurface && footstepSound != null && Time.time - lastSoundTime > minTimeBetweenSounds)
        {
            audioSource.PlayOneShot(footstepSound, volume);
            lastSoundTime = Time.time;
        }
    }
}