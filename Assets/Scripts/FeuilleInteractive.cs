using UnityEngine;
using UnityEngine.InputSystem;

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
    
    [Header("Animation de la Feuille")]
    [Tooltip("La caméra du joueur")]
    public Camera playerCamera;
    
    [Tooltip("Position de la feuille devant la caméra")]
    public Vector3 positionDevantCamera = new Vector3(0, 0, 0.5f);
    
    [Tooltip("Rotation de la feuille devant la caméra")]
    public Vector3 rotationDevantCamera = new Vector3(0, 90, 0);
    
    [Tooltip("Échelle de la feuille quand elle est devant la caméra")]
    public Vector3 echelleDevantCamera = new Vector3(1, 1, 1);
    
    [Tooltip("Vitesse de transition de l'animation")]
    public float vitesseAnimation = 5.0f;
    
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
            
            // Si aucune source audio n'existe, on en crée une
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // Son 3D pour l'effet de papier
                audioSource.volume = volumeSon;
            }
        }
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifier si le joueur est à proximité de la feuille
        VerifierProximiteJoueur();
        
        // Vérifier l'interaction avec la feuille
        if (estJoueurProche && !estEnConsultation && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CommencerConsultation();
        }
        
        // Gérer l'animation de la feuille si la consultation est en cours
        if (estEnConsultation)
        {
            AnimerFeuille();
            
            // Permettre au joueur de quitter la consultation en appuyant sur Échap
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TerminerConsultation();
            }
        }
    }
    
    // Vérifier si le joueur est à proximité de la feuille
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
                controleurJoueur = joueur.GetComponent<CharacterController>();
                inputJoueur = joueur.GetComponent<PlayerInput>();
                rigidbodyJoueur = joueur.GetComponent<Rigidbody>();
                
                // Récupérer tous les scripts potentiels de mouvement
                scriptsDeplacementJoueur = joueur.GetComponents<MonoBehaviour>();
                
                // Si la caméra n'est pas assignée, essayer de la trouver
                if (playerCamera == null)
                {
                    playerCamera = joueur.GetComponentInChildren<Camera>();
                }
            }
        }
    }
    
    // Commencer la consultation de la feuille
    void CommencerConsultation()
    {
        if (playerCamera == null) return;
        
        estEnConsultation = true;
        
        // Désactiver tous les contrôles du joueur
        DesactiverDeplacementJoueur();
        
        // Jouer le son de prise de papier
        if (paperPickupSound != null)
        {
            audioSource.clip = paperPickupSound;
            audioSource.Play();
        }
    }
    
    // Animer la feuille pour qu'elle se place devant la caméra
    void AnimerFeuille()
    {
        if (playerCamera == null) return;
        
        // Calculer la position cible devant la caméra
        Vector3 positionCible = playerCamera.transform.position + playerCamera.transform.forward * positionDevantCamera.z
                                + playerCamera.transform.up * positionDevantCamera.y
                                + playerCamera.transform.right * positionDevantCamera.x;
        
        // Calculer la rotation cible (face à la caméra avec rotation de 90 degrés)
        // On utilise LookRotation pour s'assurer que la feuille fait face à la caméra
        // puis on applique la rotation supplémentaire définie dans rotationDevantCamera
        Vector3 directionCamera = playerCamera.transform.position - positionCible;
        Quaternion rotationFaceCamera = Quaternion.LookRotation(-directionCamera);
        Quaternion rotationCible = rotationFaceCamera * Quaternion.Euler(rotationDevantCamera);
        
        // Animer la position de la feuille
        transform.position = Vector3.Lerp(transform.position, positionCible, Time.deltaTime * vitesseAnimation);
        
        // Animer la rotation de la feuille
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationCible, Time.deltaTime * vitesseAnimation);
        
        // Animer l'échelle de la feuille
        transform.localScale = Vector3.Lerp(transform.localScale, echelleDevantCamera, Time.deltaTime * vitesseAnimation);
    }
    
    // Terminer la consultation de la feuille
    void TerminerConsultation()
    {
        estEnConsultation = false;
        
        // Animer le retour de la feuille à sa position originale
        StartCoroutine(RetournerFeuillePosition());
        
        // Réactiver les contrôles du joueur
        ReactiverDeplacementJoueur();
        
        // Jouer le son de remise de papier
        if (paperPutdownSound != null)
        {
            audioSource.clip = paperPutdownSound;
            audioSource.Play();
        }
    }
    
    // Coroutine pour animer le retour de la feuille à sa position originale
    System.Collections.IEnumerator RetournerFeuillePosition()
    {
        float tempsEcoule = 0;
        Vector3 positionDepart = transform.position;
        Quaternion rotationDepart = transform.rotation;
        Vector3 echelleDepart = transform.localScale;
        
        while (tempsEcoule < 1.0f)
        {
            tempsEcoule += Time.deltaTime * vitesseAnimation;
            float t = Mathf.Clamp01(tempsEcoule);
            
            // Interpolation de la position, rotation et échelle
            transform.position = Vector3.Lerp(positionDepart, positionOriginale, t);
            transform.rotation = Quaternion.Slerp(rotationDepart, rotationOriginale, t);
            transform.localScale = Vector3.Lerp(echelleDepart, echelleOriginale, t);
            
            yield return null;
        }
        
        // S'assurer que la feuille est exactement à sa position originale
        transform.position = positionOriginale;
        transform.rotation = rotationOriginale;
        transform.localScale = echelleOriginale;
    }
    
    // Désactiver tous les composants de mouvement du joueur
    void DesactiverDeplacementJoueur()
    {
        // Désactiver le CharacterController
        if (controleurJoueur != null)
        {
            controleurJoueur.enabled = false;
        }
        
        // Désactiver le PlayerInput
        if (inputJoueur != null)
        {
            inputJoueur.enabled = false;
        }
        
        // Désactiver ou geler le Rigidbody si présent
        if (rigidbodyJoueur != null)
        {
            if (rigidbodyJoueur.isKinematic == false)
            {
                rigidbodyJoueur.linearVelocity = Vector3.zero;
                rigidbodyJoueur.angularVelocity = Vector3.zero;
                rigidbodyJoueur.isKinematic = true;
            }
        }
        
        // Désactiver tous les scripts potentiels de mouvement
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                // Vérifier si le script est probablement lié au mouvement (par son nom)
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    // Ne pas désactiver ce script (FeuilleInteractive)
                    if (script != this)
                    {
                        script.enabled = false;
                    }
                }
            }
        }
    }
    
    // Réactiver tous les composants de mouvement du joueur
    void ReactiverDeplacementJoueur()
    {
        // Réactiver le CharacterController
        if (controleurJoueur != null)
        {
            controleurJoueur.enabled = true;
        }
        
        // Réactiver le PlayerInput
        if (inputJoueur != null)
        {
            inputJoueur.enabled = true;
        }
        
        // Réactiver le Rigidbody si présent
        if (rigidbodyJoueur != null)
        {
            rigidbodyJoueur.isKinematic = false;
        }
        
        // Réactiver tous les scripts potentiels de mouvement
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                // Vérifier si le script est probablement lié au mouvement (par son nom)
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    // Ne pas activer ce script (FeuilleInteractive)
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
        
        // Visualiser la position devant la caméra si elle est disponible
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