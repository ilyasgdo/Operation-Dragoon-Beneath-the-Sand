using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FeuilleInteractive : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec la feuille")]
    public float distanceInteraction = 1.5f;
    
    [Tooltip("Le tag du joueur pour détecter la proximité")]
    public string playerTag = "Player";
    
    [Header("Audio")]
    [Tooltip("La source audio qui jouera le son de papier")]
    public AudioSource audioSource;
    
    [Tooltip("Le clip audio à jouer quand on prend la feuille")]
    public AudioClip paperPickupSound;
    
    [Tooltip("Le clip audio à jouer quand on repose la feuille")]
    public AudioClip paperPutdownSound;
    
    [Tooltip("Volume du son")]
    [Range(0f, 1f)]
    public float volumeSon = 0.7f;
    
    [Header("Position de la Feuille (Desktop)")]
    [Tooltip("La caméra du joueur")]
    public Camera playerCamera;
    
    [Tooltip("Position de la feuille devant la caméra")]
    public Vector3 positionDevantCamera = new Vector3(0, 0, 0.5f);
    
    [Tooltip("Rotation de la feuille devant la caméra")]
    public Vector3 rotationDevantCamera = new Vector3(0, 90, 0);
    
    [Tooltip("Échelle de la feuille quand elle est devant la caméra")]
    public Vector3 echelleDevantCamera = new Vector3(1, 1, 1);
    
    [Header("Interface Utilisateur")]
    [Tooltip("Texte à afficher pour indiquer comment quitter l'interaction")]
    public string texteQuitter = "Appuyez sur ÉCHAP pour quitter";
    
    [Tooltip("Taille du texte d'instruction")]
    public int tailleTexte = 20;

    [Header("VR")]
    public XRGrabInteractable xrGrabInteractable;
    
    // Variables privées
    private bool estJoueurProche = false;
    private bool estEnConsultation = false;
    private Vector3 positionOriginale;
    private Quaternion rotationOriginale;
    private Vector3 echelleOriginale;
    private GameObject objetJoueur;
    private CharacterController controleurJoueur;
    private PlayerInput inputJoueur;
    private Rigidbody rigidbodyJoueur;
    private MonoBehaviour[] scriptsDeplacementJoueur;
    private GUIStyle styleTexte;
    
    // Awake est appelé lorsque le script est initialisé
    void Awake()
    {
        // Sauvegarder la position, rotation et échelle originales de la feuille
        positionOriginale = transform.position;
        rotationOriginale = transform.rotation;
        echelleOriginale = transform.localScale;
        
        // Si aucune source audio n'est assignée, on essaie d'en obtenir une sur cet objet
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.volume = volumeSon;
            }
        }
        
        // Setup VR Grab
        if (xrGrabInteractable == null) xrGrabInteractable = GetComponent<XRGrabInteractable>();
        if (xrGrabInteractable != null)
        {
            xrGrabInteractable.selectEntered.AddListener(OnXRGrab);
            xrGrabInteractable.selectExited.AddListener(OnXRRelease);
        }

        // Initialiser le style de texte
        styleTexte = new GUIStyle();
        styleTexte.fontSize = tailleTexte;
        styleTexte.normal.textColor = Color.white;
        styleTexte.alignment = TextAnchor.MiddleCenter;
        styleTexte.fontStyle = FontStyle.Bold;
    }

    private void OnXRGrab(SelectEnterEventArgs args)
    {
        if (paperPickupSound != null) audioSource.PlayOneShot(paperPickupSound);
        estEnConsultation = true;
    }

    private void OnXRRelease(SelectExitEventArgs args)
    {
        if (paperPutdownSound != null) audioSource.PlayOneShot(paperPutdownSound);
        estEnConsultation = false;
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est à proximité de la feuille (Desktop)
        VerifierProximiteJoueur();
        
        // Vérifier l'interaction (Desktop)
        if (estJoueurProche && !estEnConsultation && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CommencerConsultation();
        }
        
        // Permettre au joueur de quitter (Desktop)
        if (estEnConsultation && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TerminerConsultation();
        }
    }
    
    // Vérifier si le joueur est à proximité de la feuille
    void VerifierProximiteJoueur()
    {
        GameObject joueur = GameObject.FindGameObjectWithTag(playerTag);
        if (joueur != null)
        {
            float distance = Vector3.Distance(transform.position, joueur.transform.position);
            estJoueurProche = distance <= distanceInteraction;
            if (estJoueurProche && objetJoueur == null)
            {
                objetJoueur = joueur;
                controleurJoueur = joueur.GetComponent<CharacterController>();
                inputJoueur = joueur.GetComponent<PlayerInput>();
                rigidbodyJoueur = joueur.GetComponent<Rigidbody>();
                scriptsDeplacementJoueur = joueur.GetComponents<MonoBehaviour>();
                if (playerCamera == null) playerCamera = joueur.GetComponentInChildren<Camera>();
            }
        }
    }
    
    // Commencer la consultation (Desktop)
    public void CommencerConsultation()
    {
        if (playerCamera == null) return;
        estEnConsultation = true;
        DesactiverDeplacementJoueur();
        if (paperPickupSound != null) audioSource.PlayOneShot(paperPickupSound);
    }
    
    // Terminer la consultation (Desktop)
    public void TerminerConsultation()
    {
        estEnConsultation = false;
        ReactiverDeplacementJoueur();
        if (paperPutdownSound != null) audioSource.PlayOneShot(paperPutdownSound);
    }
    
    // Désactiver tous les composants de mouvement du joueur
    void DesactiverDeplacementJoueur()
    {
        if (UnityEngine.XR.XRSettings.enabled) return;

        if (controleurJoueur != null) controleurJoueur.enabled = false;
        if (inputJoueur != null) inputJoueur.enabled = false;
        if (rigidbodyJoueur != null && !rigidbodyJoueur.isKinematic)
        {
            rigidbodyJoueur.linearVelocity = Vector3.zero;
            rigidbodyJoueur.angularVelocity = Vector3.zero;
            rigidbodyJoueur.isKinematic = true;
        }
        
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    if (script != this) script.enabled = false;
                }
            }
        }
    }
    
    // Réactiver tous les composants de mouvement du joueur
    void ReactiverDeplacementJoueur()
    {
        if (UnityEngine.XR.XRSettings.enabled) return;

        if (controleurJoueur != null) controleurJoueur.enabled = true;
        if (inputJoueur != null) inputJoueur.enabled = true;
        if (rigidbodyJoueur != null) rigidbodyJoueur.isKinematic = false;
        
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    if (script != this) script.enabled = true;
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
        
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 positionCible = playerCamera.transform.position + playerCamera.transform.forward * positionDevantCamera.z
                                    + playerCamera.transform.up * positionDevantCamera.y
                                    + playerCamera.transform.right * positionDevantCamera.x;
            Gizmos.DrawSphere(positionCible, 0.05f);
            Gizmos.DrawLine(transform.position, positionCible);
        }
    }
    }