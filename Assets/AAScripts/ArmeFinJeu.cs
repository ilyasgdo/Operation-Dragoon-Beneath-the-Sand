using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using UnityEngine.UI;

public class ArmeFinJeu : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec l'arme")]
    public float distanceInteraction = 2.0f;
    
    [Tooltip("Le tag du joueur pour détecter la proximité")]
    public string playerTag = "Player";
    
    [Header("Audio")]
    [Tooltip("La source audio qui jouera le son de l'arme")]
    public AudioSource audioSource;
    
    [Tooltip("Le clip audio à jouer lors de l'utilisation de l'arme")]
    public AudioClip sonArme;
    
    [Tooltip("Volume du son")]
    [Range(0f, 1f)]
    public float volumeSon = 1.0f;
    
    [Header("Effets Visuels")]
    [Tooltip("Si vrai, utilise une méthode alternative pour l'effet rouge")]
    public bool utiliserEffetAlternatif = true;
    
    [Tooltip("Volume de post-traitement pour l'effet rouge")]
    public Volume volumePostProcess;
    
    [Tooltip("Intensité maximale de l'effet rouge")]
    [Range(0f, 1f)]
    public float intensiteEffetRouge = 1.0f;
    
    [Tooltip("Intensité maximale du flou")]
    [Range(0f, 1f)]
    public float intensiteFlou = 1.0f;
    
    [Tooltip("Durée de la progression des effets visuels")]
    public float dureeEffetsVisuels = 5.0f;
    
    [Tooltip("Intensité de la couleur rouge (plus élevée = plus intense)")]
    [Range(0.0f, 0.3f)]
    public float intensiteCouleurRouge = 0.0f;
    
    [Header("Transition")]
    [Tooltip("Durée de la transition en fondu au noir")]
    public float dureeFondu = 1.5f;
    
    [Tooltip("Délai avant de charger la scène de départ")]
    public float delaiAvantChargement = 0.5f;
    
    [Tooltip("Nom de la scène de départ à charger")]
    public string nomSceneDepart = "Intro";
    
    [Header("Interface Utilisateur")]
    [Tooltip("Texte à afficher pour indiquer comment interagir")]
    public string texteInteraction = "Appuyez sur F pour utiliser l'arme";
    
    [Tooltip("Taille du texte d'instruction")]
    public int tailleTexte = 20;
    
    [Header("Réinitialisation")]
    [Tooltip("Clé PlayerPrefs pour stocker le fait que le jeu doit être réinitialisé")]
    public string cleReinitialisation = "ResetJeu";
    
    [Tooltip("Si vrai, réinitialise tous les objectifs du jeu")]
    public bool reinitialiserObjectifs = true;
    
    [Tooltip("Si vrai, réinitialise toutes les portes (les ferme)")]
    public bool reinitialiserPortes = true;
    
    [Tooltip("Si vrai, réinitialise les interactions avec les tableaux")]
    public bool reinitialiserTableaux = true;
    
    [Tooltip("Si vrai, reset les positions et rotations des objets déplaçables")]
    public bool reinitialiserObjetsDeplacables = true;
    
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
    
    // Variables pour les effets post-traitement
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private DepthOfField depthOfField;
    
    // Variables pour l'effet alternatif
    private GameObject panneauRouge;
    private RawImage imageRouge;
    private CanvasGroup groupeRouge;
    
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
        
        // Initialiser les effets visuels en fonction du mode choisi
        if (utiliserEffetAlternatif)
        {
            InitialiserEffetAlternatif();
        }
        else
        {
            InitialiserEffetsVisuels();
        }
    }
    
    // Initialiser les effets visuels de post-traitement
    private void InitialiserEffetsVisuels()
    {
        if (volumePostProcess == null)
        {
            // Créer un volume de post-traitement si aucun n'est assigné
            GameObject postProcessObj = new GameObject("VolumeFin");
            postProcessObj.transform.parent = transform;
            volumePostProcess = postProcessObj.AddComponent<Volume>();
            volumePostProcess.isGlobal = true;
            volumePostProcess.priority = 100; // Haute priorité pour surcharger les autres effets
            volumePostProcess.weight = 0; // Désactivé au départ
            volumePostProcess.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            
            Debug.Log("Nouveau volume de post-traitement créé pour l'arme de fin");
        }
        
        // Réinitialiser le profil pour éviter les conflits
        if (volumePostProcess.profile != null && 
            Application.isPlaying && 
            volumePostProcess.profile.Has<ColorAdjustments>())
        {
            // Supprimer les effets existants si on est en mode lecture
            volumePostProcess.profile.Remove<ColorAdjustments>();
            volumePostProcess.profile.Remove<Vignette>();
            volumePostProcess.profile.Remove<DepthOfField>();
            Debug.Log("Effets existants supprimés du profil");
        }
        
        // Ajouter les effets au profil
        if (!volumePostProcess.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volumePostProcess.profile.Add<ColorAdjustments>(false);
            colorAdjustments.active = true;
            Debug.Log("Effet ColorAdjustments ajouté");
        }
        
        if (!volumePostProcess.profile.TryGet(out vignette))
        {
            vignette = volumePostProcess.profile.Add<Vignette>(false);
            vignette.active = true;
            Debug.Log("Effet Vignette ajouté");
        }
        
        if (!volumePostProcess.profile.TryGet(out depthOfField))
        {
            depthOfField = volumePostProcess.profile.Add<DepthOfField>(false);
            depthOfField.active = true;
            Debug.Log("Effet DepthOfField ajouté");
        }
        
        // Configurer les effets avec des valeurs initiales
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.saturation.overrideState = true;
        }
        
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.color.overrideState = true;
            vignette.color.value = Color.red;
        }
        
        if (depthOfField != null)
        {
            depthOfField.active = true;
            depthOfField.focusDistance.overrideState = true;
            depthOfField.aperture.overrideState = true;
        }
        
        // Réinitialiser les effets
        ResetEffetsVisuels();
    }
    
    // Initialiser l'effet visuel alternatif (overlay rouge simple)
    private void InitialiserEffetAlternatif()
    {
        // Créer un canvas en mode Screen Space - Overlay
        if (panneauRouge == null)
        {
            // Créer un objet pour l'effet rouge
            panneauRouge = new GameObject("EffetRougeOverlay");
            panneauRouge.transform.SetParent(transform);
            
            // Ajouter un Canvas
            Canvas canvas = panneauRouge.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998; // S'assurer qu'il est au-dessus des autres éléments
            
            // Ajouter un CanvasScaler
            CanvasScaler scaler = panneauRouge.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Ajouter un CanvasGroup pour contrôler l'alpha
            groupeRouge = panneauRouge.AddComponent<CanvasGroup>();
            groupeRouge.alpha = 0f; // Invisible au départ
            
            // Créer un objet enfant pour l'image rouge
            GameObject imageObj = new GameObject("ImageRouge");
            imageObj.transform.SetParent(panneauRouge.transform, false);
            
            // Configurer le RectTransform pour couvrir tout l'écran
            RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            // Ajouter l'image rouge
            imageRouge = imageObj.AddComponent<RawImage>();
            imageRouge.color = new Color(1f, 0f, 0f, 0.7f); // Rouge semi-transparent
            
            Debug.Log("Effet alternatif rouge initialisé");
            
            // Désactiver l'objet au départ
            panneauRouge.SetActive(false);
        }
    }
    
    // Réinitialiser les effets visuels à zéro
    private void ResetEffetsVisuels()
    {
        if (volumePostProcess != null)
        {
            volumePostProcess.weight = 0;
            
            if (colorAdjustments != null)
            {
                colorAdjustments.colorFilter.value = Color.white;
                colorAdjustments.saturation.value = 0;
            }
            
            if (vignette != null)
            {
                vignette.intensity.value = 0;
                vignette.color.value = Color.red;
            }
            
            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = 10f;
                depthOfField.aperture.value = 5.6f;
            }
        }
    }
    
    // Update est appelé une fois par frame
    void Update()
    {
        // Ne rien faire si une transition est déjà en cours
        if (transitionEnCours) return;
        
        // Vérifier si le joueur est à proximité de l'arme
        VerifierProximiteJoueur();
        
        // Si le joueur n'est pas proche mais estJoueurProche est vrai, réinitialiser les variables
        GameObject joueur = GameObject.FindGameObjectWithTag(playerTag);
        if (joueur == null || Vector3.Distance(transform.position, joueur.transform.position) > distanceInteraction)
        {
            if (estJoueurProche)
            {
                estJoueurProche = false;
                objetJoueur = null;
            }
        }
        
        // Vérifier l'interaction avec l'arme
        if (estJoueurProche && !estEnInteraction && Keyboard.current.fKey.wasPressedThisFrame)
        {
            UtiliserArme();
        }
    }
    
    // Vérifier si le joueur est à proximité de l'arme
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
    
    // Utiliser l'arme pour mettre fin au jeu
    void UtiliserArme()
    {
        estEnInteraction = true;
        transitionEnCours = true;
        
        // Désactiver tous les contrôles du joueur
        DesactiverDeplacementJoueur();
        
        // Jouer le son de l'arme
        if (sonArme != null)
        {
            audioSource.clip = sonArme;
            audioSource.volume = volumeSon;
            audioSource.Play();
        }
        
        // Commencer les effets visuels progressifs et la transition finale
        StartCoroutine(ProgressionEffetsEtTransition());
    }
    
    // Progression des effets visuels et transition vers la scène de départ
    IEnumerator ProgressionEffetsEtTransition()
    {
        if (utiliserEffetAlternatif)
        {
            // Activer l'effet alternatif
            if (panneauRouge != null)
            {
                panneauRouge.SetActive(true);
                
                // Progression des effets sur la durée spécifiée
                float tempsEcoule = 0f;
                
                while (tempsEcoule < dureeEffetsVisuels)
                {
                    tempsEcoule += Time.deltaTime;
                    float t = Mathf.Clamp01(tempsEcoule / dureeEffetsVisuels);
                    
                    // Augmenter progressivement l'opacité de l'effet rouge
                    groupeRouge.alpha = t * intensiteEffetRouge;
                    
                    // Ajuster la couleur pour qu'elle devienne de plus en plus rouge
                    if (imageRouge != null)
                    {
                        float intensiteActuelle = Mathf.Lerp(0.5f, 0.9f, t);
                        imageRouge.color = new Color(1f, 0f, 0f, intensiteActuelle);
                    }
                    
                    Debug.Log($"Effet alternatif : alpha={groupeRouge.alpha}, t={t}");
                    
                    yield return null;
                }
            }
            else
            {
                Debug.LogError("Panneau rouge non initialisé pour l'effet alternatif !");
                yield return new WaitForSeconds(dureeEffetsVisuels);
            }
        }
        else
        {
            // Activer le volume de post-traitement
            if (volumePostProcess != null)
            {
                volumePostProcess.weight = 1f;
                Debug.Log("Volume de post-traitement activé avec poids : " + volumePostProcess.weight);
                
                // Progression des effets sur la durée spécifiée
                float tempsEcoule = 0f;
                
                while (tempsEcoule < dureeEffetsVisuels)
                {
                    tempsEcoule += Time.deltaTime;
                    float t = Mathf.Clamp01(tempsEcoule / dureeEffetsVisuels);
                    
                    // Ajuster les effets en fonction du temps écoulé
                    if (colorAdjustments != null)
                    {
                        // Rendre l'image de plus en plus rouge avec une intensité plus forte
                        Color couleurRouge = new Color(1f, intensiteCouleurRouge, intensiteCouleurRouge, 1f);
                        colorAdjustments.colorFilter.value = Color.Lerp(Color.white, couleurRouge, t);
                        
                        // Désaturer l'image progressivement
                        colorAdjustments.saturation.value = Mathf.Lerp(0f, -100f, t);
                        
                        Debug.Log($"Application de l'effet rouge : t={t}, couleur={colorAdjustments.colorFilter.value}, saturation={colorAdjustments.saturation.value}");
                    }
                    
                    if (vignette != null)
                    {
                        // Augmenter l'intensité de la vignette rouge
                        vignette.intensity.value = Mathf.Lerp(0f, intensiteEffetRouge, t);
                        // Couleur plus intense de la vignette
                        vignette.color.value = new Color(1f, 0f, 0f, 1f);
                        
                        Debug.Log($"Vignette rouge : intensité={vignette.intensity.value}");
                    }
                    
                    if (depthOfField != null)
                    {
                        // Augmenter le flou
                        depthOfField.aperture.value = Mathf.Lerp(5.6f, 0.1f, t);
                        depthOfField.focusDistance.value = Mathf.Lerp(10f, 0.1f, t * intensiteFlou);
                        
                        Debug.Log($"Effet de flou : aperture={depthOfField.aperture.value}, focus={depthOfField.focusDistance.value}");
                    }
                    
                    yield return null;
                }
            }
            else
            {
                Debug.LogError("Volume de post-traitement manquant pour les effets visuels !");
                yield return new WaitForSeconds(dureeEffetsVisuels);
            }
        }
        
        // Faire un fondu au noir et charger la scène de départ
        yield return StartCoroutine(FaireFonduEtChargerScene());
    }
    
    // Faire un fondu au noir et charger la scène de départ
    IEnumerator FaireFonduEtChargerScene()
    {
        // Utiliser le gestionnaire de transition pour le fondu
        if (TransitionSceneManager.Instance != null)
        {
            try
            {
                // Lancer le fondu en sortie via le gestionnaire
                StartCoroutine(TransitionSceneManager.Instance.FonduSortie(dureeFondu));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erreur lors du fondu de sortie: " + e.Message);
            }
        }
        
        // Attendre la durée du fondu même si une erreur s'est produite
        yield return new WaitForSeconds(dureeFondu);
        
        // Si on utilise l'effet alternatif, désactiver le panneau rouge
        if (utiliserEffetAlternatif && panneauRouge != null)
        {
            panneauRouge.SetActive(false);
        }
        
        // Attendre un court délai avant de charger la scène
        yield return new WaitForSeconds(delaiAvantChargement);
        
        // Signaler que le jeu doit être réinitialisé
        PlayerPrefs.SetInt(cleReinitialisation, 1);
        
        try
        {
            // Réinitialiser les systèmes de jeu
            ReinitialiserJeu();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation du jeu: " + e.Message);
            // Continuer malgré l'erreur pour ne pas bloquer le joueur
        }
        
        // Vérifier si la scène est valide
        if (Application.CanStreamedLevelBeLoaded(nomSceneDepart))
        {
            try
            {
                // Charger la scène de départ
                SceneManager.LoadScene(nomSceneDepart);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erreur lors du chargement de la scène: " + e.Message);
                GererErreurChargement();
            }
        }
        else
        {
            Debug.LogError("La scène '" + nomSceneDepart + "' n'a pas pu être chargée. Assurez-vous qu'elle est ajoutée dans File->Build Settings->Scenes in Build.");
            GererErreurChargement();
        }
    }
    
    // Méthode pour gérer une erreur de chargement
    private void GererErreurChargement()
    {
        // Réactiver les contrôles du joueur en cas d'échec
        ReactiverDeplacementJoueur();
        estEnInteraction = false;
        transitionEnCours = false;
        
        // Si on utilise l'effet alternatif, désactiver le panneau rouge
        if (utiliserEffetAlternatif && panneauRouge != null)
        {
            panneauRouge.SetActive(false);
        }
        
        // Si le gestionnaire de transition est disponible, faire un fondu d'entrée pour revenir
        if (TransitionSceneManager.Instance != null)
        {
            try
            {
                StartCoroutine(TransitionSceneManager.Instance.FonduEntree());
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erreur lors du fondu d'entrée après échec: " + e.Message);
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
                    // Ne pas désactiver ce script (ArmeFinJeu)
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
                    // Ne pas activer ce script (ArmeFinJeu)
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
    }
    
    // Afficher le texte d'instruction à l'écran
    void OnGUI()
    {
        // Afficher le texte d'instruction uniquement si le joueur est proche et pas en interaction
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
    
    void Start()
    {
        // Réinitialiser les variables d'état au démarrage
        estJoueurProche = false;
        estEnInteraction = false;
        transitionEnCours = false;
        objetJoueur = null;
        
        // Vérifier si une réinitialisation est demandée
        if (PlayerPrefs.GetInt(cleReinitialisation, 0) == 1)
        {
            // Réinitialiser PlayerPrefs
            PlayerPrefs.SetInt(cleReinitialisation, 0);
            PlayerPrefs.Save();
            
            Debug.Log("Jeu réinitialisé au démarrage de la scène");
        }
    }
    
    // Réinitialiser tous les systèmes du jeu
    void ReinitialiserJeu()
    {
        // Réinitialiser tous les objectifs du jeu
        if (reinitialiserObjectifs)
        {
            ReinitialiserObjectifs();
        }
        
        // Réinitialiser l'état des portes (les fermer)
        if (reinitialiserPortes)
        {
            ReinitialiserPortes();
        }
        
        // Réinitialiser les interactions avec les tableaux
        if (reinitialiserTableaux)
        {
            ReinitialiserTableaux();
        }
        
        // Réinitialiser les objets déplaçables
        if (reinitialiserObjetsDeplacables)
        {
            ReinitialiserObjetsDeplacables();
        }
        
        // Sauvegarder tous les changements dans PlayerPrefs
        PlayerPrefs.Save();
    }
    
    // Réinitialiser tous les objectifs du jeu
    void ReinitialiserObjectifs()
    {
        try
        {
            // Trouver le système d'objectifs et le réinitialiser
            SystemeObjectifs systemeObjectifs = FindObjectOfType<SystemeObjectifs>();
            if (systemeObjectifs != null)
            {
                systemeObjectifs.ReinitialiserObjectifs();
                Debug.Log("Objectifs réinitialisés");
            }
            else
            {
                // Si le système d'objectifs n'est pas trouvé, tenter de le faire via PlayerPrefs
                ReinitialiserObjectifsViaPlayerPrefs();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation des objectifs: " + e.Message);
            // Essayer la méthode alternative
            ReinitialiserObjectifsViaPlayerPrefs();
        }
    }
    
    // Méthode alternative pour réinitialiser les objectifs via PlayerPrefs
    void ReinitialiserObjectifsViaPlayerPrefs()
    {
        try
        {
            // Réinitialiser l'état enregistré des objectifs
            int nbKeys = PlayerPrefs.GetInt("objectifs_keys_count", 0);
            for (int i = 0; i < nbKeys; i++)
            {
                string key = PlayerPrefs.GetString("objectif_key_" + i, "");
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.DeleteKey("objectifs_keys_count");
            
            // Réinitialiser le compteur de tableaux visités
            PlayerPrefs.DeleteKey("tableaux_visites");
            PlayerPrefs.DeleteKey("tableaux_total");
            
            Debug.Log("Données d'objectifs réinitialisées via PlayerPrefs");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation des objectifs via PlayerPrefs: " + e.Message);
        }
    }
    
    // Réinitialiser les portes
    void ReinitialiserPortes()
    {
        try
        {
            // Effacer l'état des portes ouvertes
            PlayerPrefs.DeleteKey("portes_ouvertes_count");
            
            int nbPorteKeys = PlayerPrefs.GetInt("portes_keys_count", 0);
            for (int i = 0; i < nbPorteKeys; i++)
            {
                string key = PlayerPrefs.GetString("porte_key_" + i, "");
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.DeleteKey("portes_keys_count");
            
            Debug.Log("État des portes réinitialisé");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation des portes: " + e.Message);
        }
    }
    
    // Réinitialiser les tableaux
    void ReinitialiserTableaux()
    {
        try
        {
            // Réinitialiser le compteur de tableaux visités
            PlayerPrefs.DeleteKey("tableaux_visites");
            PlayerPrefs.DeleteKey("tableaux_total");
            
            // Effacer les tableaux visités
            int nbTableauxKeys = PlayerPrefs.GetInt("tableaux_keys_count", 0);
            for (int i = 0; i < nbTableauxKeys; i++)
            {
                string key = PlayerPrefs.GetString("tableaux_key_" + i, "");
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.DeleteKey("tableaux_keys_count");
            
            Debug.Log("Interactions tableaux réinitialisées");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation des tableaux: " + e.Message);
        }
    }
    
    // Réinitialiser les objets déplaçables
    void ReinitialiserObjetsDeplacables()
    {
        try
        {
            // Effacer les positions sauvegardées des objets déplaçables
            int nbObjetsKeys = PlayerPrefs.GetInt("objets_deplacables_count", 0);
            for (int i = 0; i < nbObjetsKeys; i++)
            {
                string key = PlayerPrefs.GetString("objet_deplacable_" + i, "");
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey(key + "_posX");
                    PlayerPrefs.DeleteKey(key + "_posY");
                    PlayerPrefs.DeleteKey(key + "_posZ");
                    PlayerPrefs.DeleteKey(key + "_rotX");
                    PlayerPrefs.DeleteKey(key + "_rotY");
                    PlayerPrefs.DeleteKey(key + "_rotZ");
                }
            }
            PlayerPrefs.DeleteKey("objets_deplacables_count");
            
            Debug.Log("Positions des objets déplaçables réinitialisées");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la réinitialisation des objets déplaçables: " + e.Message);
        }
    }

    void OnEnable()
    {
        // S'assurer de réinitialiser les variables d'état à chaque activation
        estJoueurProche = false;
        estEnInteraction = false;
        transitionEnCours = false;
        objetJoueur = null;
        
        // S'abonner à l'événement de chargement de scène
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        // Réinitialiser toutes les variables d'état lorsque le script est désactivé
        estJoueurProche = false;
        estEnInteraction = false;
        transitionEnCours = false;
        objetJoueur = null;
        
        // Se désabonner de l'événement pour éviter les fuites de mémoire
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Méthode appelée à chaque chargement de scène
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Forcer la réinitialisation des variables d'état
        estJoueurProche = false;
        estEnInteraction = false;
        transitionEnCours = false;
        objetJoueur = null;
        
        Debug.Log("ArmeFinJeu réinitialisé lors du chargement de la scène: " + scene.name);
    }

    void OnDestroy()
    {
        // S'assurer que l'effet alternatif est nettoyé
        if (panneauRouge != null)
        {
            Destroy(panneauRouge);
        }
    }
} 