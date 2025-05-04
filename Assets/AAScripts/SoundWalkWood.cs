using UnityEngine;

public class SoundWalkWood : MonoBehaviour
{
    [Header("Configuration Audio")]
    [Tooltip("La source audio qui jouera le son des pas")]
    public AudioSource audioSource;
    
    [Tooltip("Le son qui sera joué quand le joueur marche sur cet asset")]
    public AudioClip footstepSound;
    
    [Tooltip("Le tag du joueur pour détecter la collision")]
    public string playerTag = "Player";
    
    [Tooltip("Volume du son des pas")]
    [Range(1f, 2f)]
    public float volume = 1.0f;
    
    [Tooltip("Seuil de vitesse en dessous duquel le joueur est considéré comme immobile")]
    public float movementThreshold = 0.1f;
    
    private bool playerIsOnSurface = false;
    private GameObject player;
    
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
            player = other.gameObject;
            
            // Jouer le son si un son est assigné
            if (footstepSound != null && !audioSource.isPlaying)
            {
                audioSource.clip = footstepSound;
                audioSource.Play();
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
            
            // Arrêter immédiatement le son quand le joueur quitte la surface
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est sur la surface et s'il est immobile
        if (playerIsOnSurface && player != null)
        {
            // Obtenir la vitesse du joueur (en utilisant le Rigidbody si disponible)
            float playerSpeed = 0f;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            CharacterController cc = player.GetComponent<CharacterController>();
            
            if (rb != null)
            {
                playerSpeed = rb.linearVelocity.magnitude;
            }
            else if (cc != null)
            {
                // Pour les contrôleurs de personnage, on peut vérifier la vitesse de déplacement
                playerSpeed = cc.velocity.magnitude;
            }
            
            // Si le joueur est immobile et que le son est en cours de lecture, arrêter le son
            if (playerSpeed < movementThreshold && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            // Si le joueur se déplace à nouveau et que le son n'est pas en cours de lecture, reprendre le son
            else if (playerSpeed >= movementThreshold && !audioSource.isPlaying && footstepSound != null)
            {
                audioSource.clip = footstepSound;
                audioSource.Play();
            }
        }
    }
    
    // Cette méthode peut être appelée par le contrôleur du joueur pour jouer le son des pas
    public void PlayFootstepSound()
    {
        if (playerIsOnSurface && footstepSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(footstepSound, volume);
        }
    }
}
