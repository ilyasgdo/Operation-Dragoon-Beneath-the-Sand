using UnityEngine;
using UnityEngine.InputSystem;

public class TableauInteractif : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec le tableau")]
    public float distanceInteraction = 2.0f;
    
    [Tooltip("Le tag du joueur pour détecter la proximité")]
    public string playerTag = "Player";
    
    [Header("Audio")]
    [Tooltip("La source audio qui jouera la narration")]
    public AudioSource audioSource;
    
    [Tooltip("Le clip audio de narration à jouer")]
    public AudioClip narrationAudio;
    
    [Tooltip("Volume de la narration")]
    [Range(0f, 1f)]
    public float volumeNarration = 1.0f;
    
    [Header("Animation de Caméra")]
    [Tooltip("La caméra du joueur qui sera animée")]
    public Camera playerCamera;
    
    [Tooltip("Position cible pour le zoom de la caméra")]
    public Transform zoomTarget;
    
    [Tooltip("Vitesse de transition du zoom")]
    public float zoomSpeed = 2.0f;
    
    [Tooltip("Champ de vision normal de la caméra")]
    public float normalFOV = 60f;
    
    [Tooltip("Champ de vision en mode zoom")]
    public float zoomFOV = 30f;
    
    // Variables privées
    private bool isPlayerNearby = false;
    private bool isInteracting = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private GameObject playerObject;
    private CharacterController playerController;
    private PlayerInput playerInput;
    private Rigidbody playerRigidbody;
    private MonoBehaviour[] playerMovementScripts; // Pour stocker tous les scripts de mouvement potentiels
    
    // Awake est appelé lorsque le script est initialisé
    void Awake()
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
                audioSource.spatialBlend = 0f; // Son 2D pour la narration
                audioSource.volume = volumeNarration;
            }
        }
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est à proximité du tableau
        CheckPlayerProximity();
        
        // Vérifier l'interaction avec le tableau
        if (isPlayerNearby && !isInteracting && Keyboard.current.fKey.wasPressedThisFrame)
        {
            StartInteraction();
        }
        
        // Gérer l'animation de zoom si l'interaction est en cours
        if (isInteracting)
        {
            HandleZoomAnimation();
            
            // Vérifier si l'audio est terminé pour arrêter l'interaction
            if (!audioSource.isPlaying && audioSource.time > 0)
            {
                StopInteraction();
            }
            
            // Permettre au joueur de quitter l'interaction en appuyant sur Échap
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopInteraction();
            }
        }
    }
    
    // Vérifier si le joueur est à proximité du tableau
    void CheckPlayerProximity()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            
            // Mettre à jour l'état de proximité du joueur
            isPlayerNearby = distance <= distanceInteraction;
            
            // Stocker une référence au joueur si on est à proximité
            if (isPlayerNearby && playerObject == null)
            {
                playerObject = player;
                playerController = player.GetComponent<CharacterController>();
                playerInput = player.GetComponent<PlayerInput>();
                playerRigidbody = player.GetComponent<Rigidbody>();
                
                // Récupérer tous les scripts potentiels de mouvement
                playerMovementScripts = player.GetComponents<MonoBehaviour>();
                
                // Si la caméra n'est pas assignée, essayer de la trouver
                if (playerCamera == null)
                {
                    playerCamera = player.GetComponentInChildren<Camera>();
                }
            }
        }
    }
    
    // Démarrer l'interaction avec le tableau
    void StartInteraction()
    {
        if (playerCamera == null || narrationAudio == null) return;
        
        isInteracting = true;
        
        // Sauvegarder la position et rotation originales de la caméra
        originalCameraPosition = playerCamera.transform.position;
        originalCameraRotation = playerCamera.transform.rotation;
        
        // Désactiver tous les contrôles du joueur
        DisablePlayerMovement();
        
        // Jouer le son de narration
        audioSource.clip = narrationAudio;
        audioSource.Play();
    }
    
    // Gérer l'animation de zoom de la caméra
    void HandleZoomAnimation()
    {
        if (playerCamera == null || zoomTarget == null) return;
        
        // Animer la position de la caméra vers la cible
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            zoomTarget.position,
            Time.deltaTime * zoomSpeed
        );
        
        // Animer la rotation de la caméra pour regarder le tableau
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            Quaternion.LookRotation(transform.position - playerCamera.transform.position),
            Time.deltaTime * zoomSpeed
        );
        
        // Animer le champ de vision pour zoomer
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            zoomFOV,
            Time.deltaTime * zoomSpeed
        );
    }
    
    // Arrêter l'interaction avec le tableau
    void StopInteraction()
    {
        isInteracting = false;
        
        // Restaurer la position et rotation originales de la caméra
        if (playerCamera != null)
        {
            playerCamera.transform.position = originalCameraPosition;
            playerCamera.transform.rotation = originalCameraRotation;
            playerCamera.fieldOfView = normalFOV;
        }
        
        // Réactiver les contrôles du joueur
        EnablePlayerMovement();
    }
    
    // Désactiver tous les composants de mouvement du joueur
    void DisablePlayerMovement()
    {
        // Désactiver le CharacterController
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Désactiver le PlayerInput
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
        
        // Désactiver ou geler le Rigidbody si présent
        if (playerRigidbody != null)
        {
            if (playerRigidbody.isKinematic == false)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
                playerRigidbody.isKinematic = true;
            }
        }
        
        // Désactiver tous les scripts potentiels de mouvement
        if (playerMovementScripts != null)
        {
            foreach (MonoBehaviour script in playerMovementScripts)
            {
                // Vérifier si le script est probablement lié au mouvement (par son nom)
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("move") || scriptName.Contains("controller") || 
                    scriptName.Contains("motor") || scriptName.Contains("character") ||
                    scriptName.Contains("player") || scriptName.Contains("input"))
                {
                    // Ne pas désactiver ce script (TableauInteractif)
                    if (script != this)
                    {
                        script.enabled = false;
                    }
                }
            }
        }
    }
    
    // Réactiver tous les composants de mouvement du joueur
    void EnablePlayerMovement()
    {
        // Réactiver le CharacterController
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Réactiver le PlayerInput
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
        
        // Réactiver le Rigidbody si présent
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }
        
        // Réactiver tous les scripts potentiels de mouvement
        if (playerMovementScripts != null)
        {
            foreach (MonoBehaviour script in playerMovementScripts)
            {
                // Vérifier si le script est probablement lié au mouvement (par son nom)
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("move") || scriptName.Contains("controller") || 
                    scriptName.Contains("motor") || scriptName.Contains("character") ||
                    scriptName.Contains("player") || scriptName.Contains("input"))
                {
                    // Ne pas activer ce script (TableauInteractif)
                    if (script != this)
                    {
                        script.enabled = true;
                    }
                }
            }
        }
    }
    
    // Dessiner des gizmos pour visualiser la zone d'interaction dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
        
        if (zoomTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, zoomTarget.position);
            Gizmos.DrawSphere(zoomTarget.position, 0.1f);
        }
    }
}