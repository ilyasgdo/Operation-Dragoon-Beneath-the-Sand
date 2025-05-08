using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaneInteractionScript : MonoBehaviour
{
    [Header("Hélice")]
    [Tooltip("L'objet hélice qui doit tourner")]
    public Transform helice;
    
    [Tooltip("Vitesse de rotation de l'hélice en degrés par seconde")]
    public float rotationSpeed = 800f;
    
    [Tooltip("Axe de rotation de l'hélice")]
    public Vector3 rotationAxis = Vector3.forward;
    
    [Header("Déclenchement")]
    [Tooltip("Distance à laquelle l'hélice commence à tourner")]
    public float activationDistance = 5f;
    
    [Tooltip("Distance à laquelle le son commence à jouer")]
    public float soundActivationDistance = 5f;
    
    [Tooltip("Temps de transition pour atteindre la vitesse maximale")]
    public float spinUpTime = 2f;
    
    [Header("Son")]
    [Tooltip("Clip audio à jouer")]
    public AudioClip engineSound;
    
    [Tooltip("Volume du son")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    
    [Tooltip("Pitch minimum du son (au démarrage)")]
    [Range(0.1f, 3f)]
    public float minPitch = 0.5f;
    
    [Tooltip("Pitch maximum du son (à vitesse maximale)")]
    [Range(0.1f, 3f)]
    public float maxPitch = 1.2f;
    
    [Header("Débug")]
    [Tooltip("Afficher les rayons de détection dans l'éditeur")]
    public bool showDebugRanges = true;
    
    // Variables privées
    private AudioSource audioSource;
    private Transform playerTransform;
    private float currentRotationSpeed = 0f;
    private bool isActive = false;
    private float activationTime = 0f;
    
    private void Awake()
    {
        // Initialiser l'AudioSource
        SetupAudioSource();
        
        // Chercher le joueur (camera principale par défaut)
        playerTransform = Camera.main.transform;
        
        // S'assurer que l'hélice est assignée
        if (helice == null)
        {
            // Chercher un enfant avec "helice" ou "propeller" dans son nom
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("helice") || 
                    child.name.ToLower().Contains("hélice") || 
                    child.name.ToLower().Contains("propeller"))
                {
                    helice = child;
                    Debug.Log("Hélice trouvée automatiquement: " + helice.name);
                    break;
                }
            }
            
            // Si toujours pas trouvé, prendre le premier enfant
            if (helice == null && transform.childCount > 0)
            {
                helice = transform.GetChild(0);
                Debug.LogWarning("Aucune hélice trouvée, premier enfant utilisé: " + helice.name);
            }
        }
    }
    
    private void SetupAudioSource()
    {
        // Obtenir ou créer un AudioSource
        audioSource = GetComponent<AudioSource>();
        
        // Configurer l'AudioSource
        audioSource.clip = engineSound;
        audioSource.volume = volume;
        audioSource.pitch = minPitch;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // Son 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = activationDistance * 2;
        audioSource.playOnAwake = false;
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // Calculer la distance au joueur
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Mise à jour de l'activation
        bool shouldBeActive = distanceToPlayer <= activationDistance;
        
        // Si l'état change
        if (shouldBeActive != isActive)
        {
            isActive = shouldBeActive;
            activationTime = Time.time;
            
            // Gérer le son
            if (isActive && distanceToPlayer <= soundActivationDistance)
            {
                if (!audioSource.isPlaying && engineSound != null)
                {
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource.isPlaying)
                {
                    // Fade out progressif peut être ajouté ici
                    audioSource.Stop();
                }
            }
        }
        
        // Mise à jour de la vitesse de rotation
        if (isActive)
        {
            float timeSinceActivation = Time.time - activationTime;
            float t = Mathf.Clamp01(timeSinceActivation / spinUpTime);
            currentRotationSpeed = Mathf.Lerp(0, rotationSpeed, t);
            
            // Mise à jour du pitch du son
            if (audioSource.isPlaying)
            {
                audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
            }
        }
        else
        {
            float timeSinceDeactivation = Time.time - activationTime;
            float t = Mathf.Clamp01(timeSinceDeactivation / spinUpTime);
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0, t);
            
            // Mise à jour du pitch du son (si toujours en train de jouer)
            if (audioSource.isPlaying)
            {
                audioSource.pitch = Mathf.Lerp(audioSource.pitch, minPitch, t);
                
                // Arrêter le son quand la vitesse devient très faible
                if (currentRotationSpeed < rotationSpeed * 0.1f)
                {
                    audioSource.Stop();
                }
            }
        }
        
        // Faire tourner l'hélice
        if (helice != null)
        {
            helice.Rotate(rotationAxis, currentRotationSpeed * Time.deltaTime);
        }
    }
    
    private void OnValidate()
    {
        // Mettre à jour l'AudioSource si les paramètres changent dans l'inspecteur
        if (audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.clip = engineSound;
            audioSource.maxDistance = activationDistance * 2;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugRanges) return;
        
        // Dessiner la sphère d'activation de l'hélice
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
        
        // Dessiner la sphère d'activation du son
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, soundActivationDistance);
    }
} 