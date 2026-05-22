using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RadioInteractive : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec la radio")]
    public float distanceInteraction = 1.5f;
    
    [Tooltip("Le tag du joueur pour détecter la proximité")]
    public string playerTag = "Player";
    
    [Tooltip("La fréquence correcte pour entendre le discours")]
    [Range(88.0f, 108.0f)]
    public float frequenceCorrecte = 95.5f;
    
    [Tooltip("Marge d'erreur acceptable pour la fréquence (±)")]
    [Range(0.1f, 1.0f)]
    public float margeErreur = 0.3f;
    
    [Tooltip("Vitesse de rotation de la molette")]
    public float vitesseRotation = 10f;
    
    [Header("Audio")]
    [Tooltip("Le clip audio de grésillement")]
    public AudioClip clipGresillement;
    
    [Tooltip("Le clip audio du discours à jouer quand la bonne fréquence est trouvée")]
    public AudioClip clipDiscours;
    
    [Tooltip("Volume du grésillement")]
    [Range(0f, 1f)]
    public float volumeGresillement = 0.7f;
    
    [Tooltip("Volume du discours")]
    [Range(0f, 1f)]
    public float volumeDiscours = 1.0f;
    
    [Header("Visuel")]
    [Tooltip("L'objet représentant la molette de la radio")]
    public Transform molette;
    
    [Tooltip("L'axe de rotation de la molette (x, y ou z)")]
    public Vector3 axeRotation = new Vector3(0, 1, 0);
    
    [Header("Interface")]
    [Tooltip("Afficher la fréquence actuelle à l'écran")]
    public bool afficherFrequence = true;
    
    [Tooltip("Texte d'instruction pour l'interaction")]
    public string texteInstruction = "Appuyez sur F pour interagir avec la radio";
    
    [Tooltip("Texte d'instruction pour ajuster la fréquence")]
    public string texteAjuster = "Utilisez la molette de la souris pour ajuster la fréquence";
    
    [Tooltip("Sensibilité de la molette de la souris")]
    [Range(0.1f, 5f)]
    public float sensibiliteMolette = 1.0f;
    
    [Header("VR")]
    public XRSimpleInteractable xrInteractable;
    public InputActionProperty vrRotateAction;

    // Variables privées
    private bool estJoueurProche = false;
    private bool estEnInteraction = false;
    private float frequenceActuelle = 90.0f;
    private bool discoursTrouve = false;
    private float intensiteGresillement = 1.0f;
    private GUIStyle styleTexte;
    private GameObject objetJoueur;
    private PlayerInput inputJoueur;
    private float rotationInitialeMolette = 0f;
    
    [Tooltip("Référence au système d'objectifs")]
    public SystemeObjectifs systemeObjectifs;
    
    [Tooltip("Indique si ce message est celui du Général de Gaulle")]
    public bool estMessageDeGaulle = false;
    
    // Sources audio générées automatiquement
    private AudioSource audioSourceGresillement;
    private AudioSource audioSourceDiscours;
    
    // Awake est appelé lorsque le script est initialisé
    void Awake()
    {
        // Initialiser le style de texte
        styleTexte = new GUIStyle();
        styleTexte.fontSize = 20;
        styleTexte.normal.textColor = Color.white;
        styleTexte.alignment = TextAnchor.MiddleCenter;
        styleTexte.fontStyle = FontStyle.Bold;
        
        // Sauvegarder la rotation initiale de la molette si elle est assignée
        if (molette != null)
        {
            rotationInitialeMolette = Vector3.Dot(molette.localEulerAngles, axeRotation);
        }
        
        // Créer les sources audio automatiquement
        CreerSourcesAudio();

        if (xrInteractable == null) xrInteractable = GetComponent<XRSimpleInteractable>();
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
        }
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        if (estEnInteraction) TerminerInteraction();
        else CommencerInteraction();
    }
    
    // Créer les sources audio automatiquement
    void CreerSourcesAudio()
    {
        // Créer la source audio pour le grésillement
        audioSourceGresillement = gameObject.AddComponent<AudioSource>();
        audioSourceGresillement.loop = true;
        audioSourceGresillement.spatialBlend = 1f; // Son 3D
        audioSourceGresillement.volume = 0;  // Commencer à volume 0
        audioSourceGresillement.playOnAwake = false;
        
        // Créer la source audio pour le discours
        audioSourceDiscours = gameObject.AddComponent<AudioSource>();
        audioSourceDiscours.loop = false;
        audioSourceDiscours.spatialBlend = 1f; // Son 3D
        audioSourceDiscours.volume = volumeDiscours;
        audioSourceDiscours.playOnAwake = false;
        
        // Assigner les clips audio
        if (clipGresillement != null)
        {
            audioSourceGresillement.clip = clipGresillement;
            audioSourceGresillement.Play();
            audioSourceGresillement.volume = 0.1f; // Volume faible quand inactif
        }
        
        if (clipDiscours != null)
        {
            audioSourceDiscours.clip = clipDiscours;
        }
    }
    
    void Start()
    {
        // Vérifier si les sources audio sont correctement configurées
        if (audioSourceGresillement != null && audioSourceGresillement.clip == null && clipGresillement != null)
        {
            audioSourceGresillement.clip = clipGresillement;
            audioSourceGresillement.Play();
        }
        
        if (audioSourceDiscours != null && audioSourceDiscours.clip == null && clipDiscours != null)
        {
            audioSourceDiscours.clip = clipDiscours;
        }
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est à proximité de la radio
        VerifierProximiteJoueur();
        
        // Vérifier l'interaction avec la radio (Desktop)
        if (estJoueurProche && !estEnInteraction && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CommencerInteraction();
        }
        
        // Gérer l'ajustement de la fréquence
        if (estEnInteraction)
        {
            AjusterFrequence();
            
            // Vérifier si la fréquence est correcte
            VerifierFrequence();
            
            // Ajuster le volume du grésillement en fonction de la proximité avec la fréquence correcte
            AjusterGresillement();
            
            // Permettre au joueur de quitter l'interaction en appuyant sur Échap (Desktop)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TerminerInteraction();
            }
        }
    }
    
    // Vérifier si le joueur est à proximité de la radio
    void VerifierProximiteJoueur()
    {
        GameObject joueur = GameObject.FindGameObjectWithTag(playerTag);
        
        if (joueur != null)
        {
            float distance = Vector3.Distance(transform.position, joueur.transform.position);
            
            // Mettre à jour l'état de proximité du joueur
            estJoueurProche = distance <= distanceInteraction;
            
            // Stocker une référence au joueur si on est à proximité
            if (estJoueurProche && objetJoueur == null)
            {
                objetJoueur = joueur;
                inputJoueur = joueur.GetComponent<PlayerInput>();
            }
        }
    }
    
    // Commencer l'interaction avec la radio
    public void CommencerInteraction()
    {
        estEnInteraction = true;
        
        // Activer le son du grésillement
        if (audioSourceGresillement != null)
        {
            audioSourceGresillement.volume = volumeGresillement;
        }
    }
    
    // Ajuster la fréquence avec la molette de la souris ou VR joystick
    void AjusterFrequence()
    {
        float ajustement = 0;
        
        // VR Input
        if (vrRotateAction.action != null)
        {
            Vector2 joystickValue = vrRotateAction.action.ReadValue<Vector2>();
            ajustement = joystickValue.x * 0.1f * sensibiliteMolette;
        }

        // Desktop Input
        if (Mouse.current != null)
        {
            float scrollDelta = Mouse.current.scroll.y.ReadValue();
            if (scrollDelta != 0)
            {
                ajustement += scrollDelta * 0.01f * sensibiliteMolette;
            }
        }
        
        // Appliquer l'ajustement
        if (ajustement != 0)
        {
            frequenceActuelle = Mathf.Clamp(frequenceActuelle + ajustement, 88.0f, 108.0f);
            
            // Animer la rotation de la molette
            if (molette != null)
            {
                // Calculer la rotation basée sur la fréquence (mapping de 88-108 MHz à 0-360 degrés)
                float angleRotation = (frequenceActuelle - 88.0f) * (360.0f / 20.0f);
                
                // Rotation selon l'axe spécifié
                if (axeRotation.x > 0.5f)
                {
                    // Rotation autour de l'axe X
                    molette.localEulerAngles = new Vector3(angleRotation, 0, 0);
                }
                else if (axeRotation.y > 0.5f)
                {
                    // Rotation autour de l'axe Y
                    molette.localEulerAngles = new Vector3(0, angleRotation, 0);
                }
                else if (axeRotation.z > 0.5f)
                {
                    // Rotation autour de l'axe Z
                    molette.localEulerAngles = new Vector3(0, 0, angleRotation);
                }
                else
                {
                    // Rotation personnalisée (multi-axes)
                    Vector3 rotationNormalisee = axeRotation.normalized;
                    molette.Rotate(rotationNormalisee, angleRotation - rotationInitialeMolette, Space.Self);
                    rotationInitialeMolette = angleRotation;
                }
            }
        }
    }
    
    // Vérifier si la fréquence actuelle est correcte
    void VerifierFrequence()
    {
        bool frequenceEstCorrecte = Mathf.Abs(frequenceActuelle - frequenceCorrecte) <= margeErreur;
        
        // Si on vient de trouver la bonne fréquence
        if (frequenceEstCorrecte && !discoursTrouve)
        {
            discoursTrouve = true;
            
            // Baisser le volume du grésillement
            if (audioSourceGresillement != null)
            {
                audioSourceGresillement.volume = 0.1f;
            }
            
            // Jouer le discours
            if (audioSourceDiscours != null && clipDiscours != null)
            {
                audioSourceDiscours.clip = clipDiscours;
                audioSourceDiscours.volume = volumeDiscours;
                audioSourceDiscours.Play();
                
                // Si c'est le message du Général de Gaulle, compléter l'objectif correspondant
                if (estMessageDeGaulle && systemeObjectifs != null)
                {
                    systemeObjectifs.CompleterObjectifMessageDeGaulle();
                }
            }
        }
        // Si on s'éloigne de la bonne fréquence
        else if (!frequenceEstCorrecte && discoursTrouve)
        {
            discoursTrouve = false;
            
            // Arrêter le discours
            if (audioSourceDiscours != null && audioSourceDiscours.isPlaying)
            {
                audioSourceDiscours.Stop();
            }
        }
    }
    
    // Ajuster l'intensité du grésillement en fonction de la proximité avec la fréquence correcte
    void AjusterGresillement()
    {
        if (audioSourceGresillement != null)
        {
            // Calculer la distance à la fréquence correcte (0 = parfait, 10 = le plus loin possible)
            float distanceFrequence = Mathf.Abs(frequenceActuelle - frequenceCorrecte);
            
            // Si on a trouvé la bonne fréquence, le grésillement est minimal
            if (discoursTrouve)
            {
                intensiteGresillement = 0.1f;
            }
            else
            {
                // Calculer l'intensité du grésillement (1 = fort, 0.1 = faible)
                // Plus on est proche de la fréquence correcte, moins il y a de grésillement
                intensiteGresillement = Mathf.Lerp(0.3f, volumeGresillement, distanceFrequence / 10.0f);
            }
            
            audioSourceGresillement.volume = intensiteGresillement;
        }
    }
    
    // Terminer l'interaction avec la radio
    public void TerminerInteraction()
    {
        estEnInteraction = false;
        
        // Baisser le volume du grésillement mais ne pas l'arrêter complètement
        if (audioSourceGresillement != null)
        {
            audioSourceGresillement.volume = 0.1f;
        }
        
        // Si le discours est en cours, le laisser continuer
    }
    
    // Dessiner des gizmos pour visualiser la zone d'interaction dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
    }
    
    // Afficher l'interface utilisateur
    void OnGUI()
    {
        if (Application.isBatchMode) return;
        // Afficher l'instruction d'interaction si le joueur est proche mais n'interagit pas encore
        if (estJoueurProche && !estEnInteraction)
        {
            AfficherTexteInterface(texteInstruction, Screen.height - 100);
        }
        
        // Afficher la fréquence et les instructions d'ajustement pendant l'interaction
        if (estEnInteraction)
        {
            // Afficher la fréquence actuelle
            if (afficherFrequence)
            {
                string texteFrequence = string.Format("{0:F1} MHz", frequenceActuelle);
                AfficherTexteInterface(texteFrequence, Screen.height / 2 - 50);
            }
            
            // Afficher l'instruction pour ajuster la fréquence
            AfficherTexteInterface(texteAjuster, Screen.height - 100);
        }
    }
    
    // Méthode utilitaire pour afficher du texte à l'écran
    void AfficherTexteInterface(string texte, float hauteur)
    {
        float largeurTexte = 300;
        float hauteurTexte = 30;
        
        Rect positionTexte = new Rect(
            (Screen.width - largeurTexte) / 2,
            hauteur,
            largeurTexte,
            hauteurTexte
        );
        
        // Fond semi-transparent
        GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
        GUI.Box(positionTexte, "");
        
        // Texte
        GUI.Label(positionTexte, texte, styleTexte);
    }
}