using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PersonnageInteractif : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec le personnage")]
    public float distanceInteraction = 2.0f;
    
    [Tooltip("Le tag du joueur pour détecter la proximité")]
    public string playerTag = "Player";
    
    [Header("Audio")]
    [Tooltip("La source audio qui jouera le son du souvenir")]
    public AudioSource audioSource;
    
    [Tooltip("Le clip audio à jouer avant la transition")]
    public AudioClip souvenirAudioClip;
    
    [Tooltip("Volume du son")]
    [Range(0f, 1f)]
    public float volumeSon = 1.0f;
    
    [Header("Transition")]
    [Tooltip("Durée de la transition en fondu au noir")]
    public float dureeFondu = 1.5f;
    
    [Tooltip("Délai avant de charger la nouvelle scène (après la fin du son)")]
    public float delaiAvantChargement = 0.5f;
    
    [Tooltip("Nom de la scène du souvenir à charger")]
    public string nomSceneSouvenir = "Introduction";
    
    [Header("Interface Utilisateur")]
    [Tooltip("Texte à afficher pour indiquer comment interagir")]
    public string texteInteraction = "Appuyez sur F pour interagir";
    
    [Tooltip("Taille du texte d'instruction")]
    public int tailleTexte = 20;
    
    // Variables privées
    private bool estJoueurProche = false;
    private bool estEnInteraction = false;
    private bool transitionEnCours = false;
    private GameObject objetJoueur;
    private CharacterController controleurJoueur;
    private PlayerInput inputJoueur;
    private Rigidbody rigidbodyJoueur;
    private MonoBehaviour[] scriptsDeplacementJoueur;
    private GUIStyle styleTexte;
    
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
                audioSource.spatialBlend = 1f; // Son 3D
                audioSource.volume = volumeSon;
            }
        }
        
        // S'assurer que le volume est correctement réglé
        if (audioSource != null)
        {
            audioSource.volume = volumeSon;
        }
        
        // Initialiser le style de texte
        styleTexte = new GUIStyle();
        styleTexte.fontSize = tailleTexte;
        styleTexte.normal.textColor = Color.white;
        styleTexte.alignment = TextAnchor.MiddleCenter;
        styleTexte.fontStyle = FontStyle.Bold;
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Ne rien faire si une transition est déjà en cours
        if (transitionEnCours) return;
        
        // Vérifier si le joueur est à proximité du personnage
        VerifierProximiteJoueur();
        
        // Vérifier l'interaction avec le personnage
        if (estJoueurProche && !estEnInteraction && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CommencerInteraction();
        }
    }
    
    // Vérifier si le joueur est à proximité du personnage
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
            }
        }
    }
    
    // Commencer l'interaction avec le personnage
    void CommencerInteraction()
    {
        estEnInteraction = true;
        
        // Désactiver tous les contrôles du joueur
        DesactiverDeplacementJoueur();
        
        // Jouer le son du souvenir
        if (souvenirAudioClip != null)
        {
            audioSource.clip = souvenirAudioClip;
            audioSource.volume = volumeSon; // S'assurer que le volume est bien réglé
            audioSource.Play();
            
            // Attendre la fin du son avant de commencer la transition
            StartCoroutine(AttendreFinSonEtTransition());
        }
        else
        {
            // Si pas de son, commencer directement la transition
            StartCoroutine(FaireFonduEtChargerScene());
        }
    }
    
    // Attendre que le son se termine avant de faire la transition
    IEnumerator AttendreFinSonEtTransition()
    {
        // Attendre que le son se termine
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        
        // Attendre un court délai supplémentaire
        yield return new WaitForSeconds(delaiAvantChargement);
        
        // Commencer la transition vers la scène du souvenir
        StartCoroutine(FaireFonduEtChargerScene());
    }
    
    // Faire un fondu au noir et charger la nouvelle scène
    IEnumerator FaireFonduEtChargerScene()
    {
        transitionEnCours = true;
        
        // Utiliser le gestionnaire de transition pour le fondu
        if (TransitionSceneManager.Instance != null)
        {
            // Lancer le fondu en sortie via le gestionnaire
            yield return StartCoroutine(TransitionSceneManager.Instance.FonduSortie(dureeFondu));
        }
        else
        {
            // Attendre la durée du fondu si le gestionnaire n'est pas disponible
            yield return new WaitForSeconds(dureeFondu);
        }
        
        // Vérifier si la scène est valide
        if (Application.CanStreamedLevelBeLoaded(nomSceneSouvenir))
        {
            // Charger la scène du souvenir
            SceneManager.LoadScene(nomSceneSouvenir);
        }
        else
        {
            Debug.LogError("La scène '" + nomSceneSouvenir + "' n'a pas pu être chargée. Assurez-vous qu'elle est ajoutée dans File->Build Settings->Scenes in Build.");
            
            // Réactiver les contrôles du joueur en cas d'échec
            ReactiverDeplacementJoueur();
            estEnInteraction = false;
            transitionEnCours = false;
            
            // Si le gestionnaire de transition est disponible, faire un fondu d'entrée pour revenir
            if (TransitionSceneManager.Instance != null)
            {
                StartCoroutine(TransitionSceneManager.Instance.FonduEntree());
            }
        }
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
                    // Ne pas désactiver ce script (PersonnageInteractif)
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
                    // Ne pas activer ce script (PersonnageInteractif)
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
    }
    
    // Afficher le texte d'instruction à l'écran
    void OnGUI()
    {
        // Afficher le texte d'instruction uniquement quand le joueur est proche et qu'aucune interaction n'est en cours
        if (estJoueurProche && !estEnInteraction && !transitionEnCours)
        {
            // Créer un fond semi-transparent pour le texte
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
            
            // Calculer la position du texte (centré en bas de l'écran)
            float largeurTexte = 300;
            float hauteurTexte = 30;
            Rect positionTexte = new Rect(
                (Screen.width - largeurTexte) / 2,
                Screen.height - hauteurTexte - 50,
                largeurTexte,
                hauteurTexte
            );
            
            // Dessiner le texte avec une ombre pour meilleure lisibilité
            GUI.Box(positionTexte, "");
            GUI.Label(positionTexte, texteInteraction, styleTexte);
        }
    }
} 