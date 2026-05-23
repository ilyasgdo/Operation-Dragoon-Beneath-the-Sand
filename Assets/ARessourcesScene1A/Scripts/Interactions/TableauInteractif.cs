using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    
    [Tooltip("Distance maximale à laquelle le son est audible")]
    public float distanceAudible = 5.0f;
    
    [Tooltip("Continuer à jouer le son même si le joueur quitte l'interaction")]
    public bool continuerNarrationEnSortant = true;
    
    [Header("Animation de Caméra (Desktop)")]
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
    
    [Header("Interface Utilisateur")]
    [Tooltip("Texte à afficher pour indiquer comment quitter l'interaction")]
    public string texteQuitter = "Appuyez sur ÉCHAP pour quitter";
    
    [Tooltip("Taille du texte d'instruction")]
    public int tailleTexte = 20;

    [Header("VR")]
    public XRSimpleInteractable xrInteractable;
    
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
    private GUIStyle styleTexte;
    
    [Tooltip("Référence au système d'objectifs")]
    public SystemeObjectifs systemeObjectifs;
    
    [Tooltip("ID unique de ce tableau pour le système d'objectifs")]
    public string tableauId;
    
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
                audioSource.spatialBlend = 1f; // Son 3D pour la narration spatiale
                audioSource.volume = volumeNarration;
                audioSource.rolloffMode = AudioRolloffMode.Linear; // Mode d'atténuation linéaire
                audioSource.maxDistance = distanceAudible;
                audioSource.minDistance = 1.0f;
            }
        }
        else
        {
            // Configurer l'AudioSource existante pour le son 3D
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = distanceAudible;
            audioSource.minDistance = 1.0f;
        }
        
        // Initialiser le style de texte
        styleTexte = new GUIStyle();
        styleTexte.fontSize = tailleTexte;
        styleTexte.normal.textColor = Color.white;
        styleTexte.alignment = TextAnchor.MiddleCenter;
        styleTexte.fontStyle = FontStyle.Bold;

        if (xrInteractable == null) xrInteractable = GetComponent<XRSimpleInteractable>();
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
            xrInteractable.hoverEntered.AddListener(OnXRHover);
        }
        }

        private void OnXRHover(HoverEnterEventArgs args)
        {
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor && !isInteracting)
        {
            StartInteraction();
        }
        }

        private void OnXRSelect(SelectEnterEventArgs args)
    {
        if (isInteracting) StopInteraction();
        else StartInteraction();
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est à proximité du tableau
        CheckPlayerProximity();
        
        // Vérifier l'interaction avec le tableau (Desktop)
        if (isPlayerNearby && !isInteracting && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            StartInteraction();
        }
        
        // Gérer l'animation de zoom si l'interaction est en cours
        if (isInteracting)
        {
            // Don't animate camera in VR to avoid nausea
            bool isVR = UnityEngine.XR.XRSettings.enabled;
            if (!isVR)
            {
                HandleZoomAnimation();
            }
            
            // Vérifier si l'audio est terminé pour arrêter l'interaction
            if (!audioSource.isPlaying && audioSource.time > 0)
            {
                StopInteraction();
            }
            
            // Permettre au joueur de quitter l'interaction (Desktop)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopInteraction();
            }
        }
        
        // Gérer le volume de la narration en fonction de la distance si on n'est plus en interaction
        if (!isInteracting && audioSource.isPlaying && !continuerNarrationEnSortant)
        {
            // Si le joueur n'est plus à proximité et que l'option est désactivée, arrêter la narration
            if (!isPlayerNearby)
            {
                audioSource.Stop();
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
    public void StartInteraction()
    {
        if (narrationAudio == null) return;
        
        isInteracting = true;
        
        if (playerCamera != null)
        {
            // Sauvegarder la position et rotation originales de la caméra
            originalCameraPosition = playerCamera.transform.position;
            originalCameraRotation = playerCamera.transform.rotation;
        }
        
        // Désactiver tous les contrôles du joueur
        DisablePlayerMovement();
        
        // Jouer le son de narration
        audioSource.clip = narrationAudio;
        audioSource.Play();
        
        // Enregistrer l'interaction avec ce tableau dans le système d'objectifs
        if (systemeObjectifs != null && !string.IsNullOrEmpty(tableauId))
        {
            systemeObjectifs.EnregistrerTableauVisite(tableauId);
        }
    }
    
    // Gérer l'animation de zoom de la caméra (Desktop only)
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
    public void StopInteraction()
    {
        isInteracting = false;
        
        // Restaurer la position et rotation originales de la caméra
        if (playerCamera != null)
        {
            bool isVR = UnityEngine.XR.XRSettings.enabled;
            if (!isVR)
            {
                playerCamera.transform.position = originalCameraPosition;
                playerCamera.transform.rotation = originalCameraRotation;
                playerCamera.fieldOfView = normalFOV;
            }
        }
        
        // Réactiver les contrôles du joueur
        EnablePlayerMovement();
        
        // Si l'option de continuer la narration est désactivée et que le joueur n'est pas à proximité, arrêter le son
        if (!continuerNarrationEnSortant && !isPlayerNearby && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    // Désactiver tous les composants de mouvement du joueur
    void DisablePlayerMovement()
    {
        bool isVR = UnityEngine.XR.XRSettings.enabled;
        if (isVR) return; // Keep movement in VR or handle separately

        if (playerController != null) playerController.enabled = false;
        if (playerInput != null) playerInput.enabled = false;
        if (playerRigidbody != null && !playerRigidbody.isKinematic)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
        
        if (playerMovementScripts != null)
        {
            foreach (MonoBehaviour script in playerMovementScripts)
            {
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("move") || scriptName.Contains("controller") || 
                    scriptName.Contains("motor") || scriptName.Contains("character") ||
                    scriptName.Contains("player") || scriptName.Contains("input"))
                {
                    if (script != this) script.enabled = false;
                }
            }
        }
    }
    
    // Réactiver tous les composants de mouvement du joueur
    void EnablePlayerMovement()
    {
        bool isVR = UnityEngine.XR.XRSettings.enabled;
        if (isVR) return;

        if (playerController != null) playerController.enabled = true;
        if (playerInput != null) playerInput.enabled = true;
        if (playerRigidbody != null) playerRigidbody.isKinematic = false;
        
        if (playerMovementScripts != null)
        {
            foreach (MonoBehaviour script in playerMovementScripts)
            {
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("move") || scriptName.Contains("controller") || 
                    scriptName.Contains("motor") || scriptName.Contains("character") ||
                    scriptName.Contains("player") || scriptName.Contains("input"))
                {
                    if (script != this) script.enabled = true;
                }
            }
        }
    }
    
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
    
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(tableauId))
        {
            tableauId = "tableau_" + gameObject.GetInstanceID();
        }
        
        if (systemeObjectifs == null)
        {
            systemeObjectifs = FindObjectOfType<SystemeObjectifs>();
        }
    }
    
    void OnGUI()
    {
        if (Application.isBatchMode) return;
        if (isInteracting)
        {
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
            float largeurTexte = 300;
            float hauteurTexte = 30;
            Rect positionTexte = new Rect(
                (Screen.width - largeurTexte) / 2,
                Screen.height - hauteurTexte - 50,
                largeurTexte,
                hauteurTexte
            );
            GUI.Box(positionTexte, "");
            GUI.Label(positionTexte, texteQuitter, styleTexte);
        }
    }
}