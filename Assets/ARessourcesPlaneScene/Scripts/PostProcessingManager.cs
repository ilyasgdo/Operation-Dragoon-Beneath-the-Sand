using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestionnaire de post-processing permettant de charger automatiquement le bon profil
/// pour chaque scène et gérer les transitions entre profils.
/// </summary>
public class PostProcessingManager : MonoBehaviour
{
    [Header("Volume de post-processing")]
    [Tooltip("Volume global à contrôler")]
    public Volume volumePostProcess;
    
    [Header("Profils par scène")]
    [Tooltip("Liste associant les noms de scènes à leurs profils de post-processing")]
    public SceneProfile[] profilsParScene;
    
    [Header("Profils d'ambiance")]
    [Tooltip("Profils supplémentaires pour différentes ambiances")]
    public AmbianceProfile[] profilsAmbiance;
    
    [Header("Paramètres de transition")]
    [Tooltip("Durée de la transition entre profils")]
    public float dureeTransitionDefaut = 2.0f;
    
    // Profil de post-processing actuel
    private VolumeProfile profilActuel;
    // Coroutine de transition en cours
    private Coroutine transitionCoroutine;
    
    [System.Serializable]
    public class SceneProfile
    {
        public string nomScene;
        public VolumeProfile profil;
    }
    
    [System.Serializable]
    public class AmbianceProfile
    {
        public string nomAmbiance;
        public VolumeProfile profil;
        [Tooltip("Description de cette ambiance")]
        [TextArea(2, 5)]
        public string description;
    }

    private void Awake()
    {
        // Ne pas détruire ce gestionnaire lors du changement de scène
        DontDestroyOnLoad(gameObject);
        
        // Si aucun volume n'est assigné, essayer d'en trouver un dans la scène
        if (volumePostProcess == null)
        {
            volumePostProcess = FindObjectOfType<Volume>();
            if (volumePostProcess == null)
            {
                Debug.LogWarning("PostProcessingManager: Aucun Volume trouvé. Création d'un nouveau volume global.");
                GameObject volumeObj = new GameObject("Global Post Process Volume");
                volumePostProcess = volumeObj.AddComponent<Volume>();
                volumePostProcess.isGlobal = true;
            }
        }
        
        // Sauvegarder le profil initial
        if (volumePostProcess.profile != null)
        {
            profilActuel = volumePostProcess.profile;
        }
    }

    private void OnEnable()
    {
        // S'abonner à l'événement de changement de scène
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Se désabonner de l'événement de changement de scène
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Appelé automatiquement lors du chargement d'une nouvelle scène
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Rechercher le profil correspondant à cette scène
        VolumeProfile profilScene = TrouverProfilPourScene(scene.name);
        
        if (profilScene != null)
        {
            // Appliquer le profil avec une transition
            ChangerProfil(profilScene, dureeTransitionDefaut);
        }
        else
        {
            Debug.LogWarning($"PostProcessingManager: Aucun profil trouvé pour la scène {scene.name}");
        }
    }
    
    /// <summary>
    /// Trouve le profil de post-processing associé à une scène
    /// </summary>
    private VolumeProfile TrouverProfilPourScene(string nomScene)
    {
        foreach (SceneProfile sceneProfile in profilsParScene)
        {
            if (sceneProfile.nomScene == nomScene && sceneProfile.profil != null)
            {
                return sceneProfile.profil;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Change immédiatement le profil de post-processing
    /// </summary>
    public void ChangerProfilImmediat(VolumeProfile nouveauProfil)
    {
        if (nouveauProfil != null)
        {
            volumePostProcess.profile = nouveauProfil;
            profilActuel = nouveauProfil;
        }
    }
    
    /// <summary>
    /// Change le profil avec une transition douce
    /// </summary>
    public void ChangerProfil(VolumeProfile nouveauProfil, float dureeTransition = 2.0f)
    {
        // Arrêter toute transition en cours
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        // Démarrer la nouvelle transition
        if (nouveauProfil != null)
        {
            transitionCoroutine = StartCoroutine(TransitionProfil(nouveauProfil, dureeTransition));
        }
    }
    
    /// <summary>
    /// Change le profil d'ambiance par son nom
    /// </summary>
    public void ChangerAmbiance(string nomAmbiance, float dureeTransition = 2.0f)
    {
        foreach (AmbianceProfile ambiance in profilsAmbiance)
        {
            if (ambiance.nomAmbiance == nomAmbiance && ambiance.profil != null)
            {
                ChangerProfil(ambiance.profil, dureeTransition);
                Debug.Log($"Changement d'ambiance: {nomAmbiance} - {ambiance.description}");
                return;
            }
        }
        
        Debug.LogWarning($"PostProcessingManager: Ambiance '{nomAmbiance}' non trouvée");
    }
    
    /// <summary>
    /// Coroutine qui gère la transition entre deux profils
    /// </summary>
    private IEnumerator TransitionProfil(VolumeProfile profilCible, float duree)
    {
        // Approche simplifiée : ne pas essayer d'interpoler les valeurs
        // mais plutôt utiliser un poids pour le volume
        
        // Créer un volume temporaire pour la transition
        GameObject tempVolumeObj = new GameObject("Transition Volume");
        tempVolumeObj.transform.SetParent(transform);
        Volume volumeTransition = tempVolumeObj.AddComponent<Volume>();
        volumeTransition.isGlobal = true;
        volumeTransition.priority = volumePostProcess.priority + 1; // Plus haute priorité
        volumeTransition.profile = profilCible;
        volumeTransition.weight = 0; // Commencer à 0
        
        // Temps écoulé
        float tempsEcoule = 0f;
        
        // Effectuer la transition progressive en ajustant le poids
        while (tempsEcoule < duree)
        {
            // Calculer le poids actuel
            float t = tempsEcoule / duree;
            volumeTransition.weight = Mathf.Lerp(0f, 1f, t);
            
            tempsEcoule += Time.deltaTime;
            yield return null;
        }
        
        // À la fin de la transition, appliquer directement le profil cible
        volumePostProcess.profile = profilCible;
        profilActuel = profilCible;
        
        // Supprimer le volume de transition
        Destroy(tempVolumeObj);
        
        transitionCoroutine = null;
    }
    
    /// <summary>
    /// Version alternative utilisant la duplication des profils et la méthode de fondu
    /// </summary>
    private IEnumerator TransitionProfilAlternative(VolumeProfile profilCible, float duree)
    {
        if (profilActuel == null || profilCible == null)
        {
            volumePostProcess.profile = profilCible;
            profilActuel = profilCible;
            transitionCoroutine = null;
            yield break;
        }
        
        // Créer deux volumes pour gérer la transition
        GameObject volumeSrcObj = new GameObject("Volume Source");
        Volume volumeSrc = volumeSrcObj.AddComponent<Volume>();
        volumeSrc.isGlobal = true;
        volumeSrc.profile = profilActuel;
        volumeSrc.priority = volumePostProcess.priority;
        volumeSrc.weight = 1f;
        
        GameObject volumeDestObj = new GameObject("Volume Destination");
        Volume volumeDest = volumeDestObj.AddComponent<Volume>();
        volumeDest.isGlobal = true;
        volumeDest.profile = profilCible;
        volumeDest.priority = volumePostProcess.priority + 1;
        volumeDest.weight = 0f;
        
        // Désactiver le volume original pendant la transition
        float originalWeight = volumePostProcess.weight;
        volumePostProcess.weight = 0f;
        
        // Temps écoulé
        float tempsEcoule = 0f;
        
        // Effectuer la transition progressive
        while (tempsEcoule < duree)
        {
            // Calculer le pourcentage de progression
            float t = tempsEcoule / duree;
            
            // Ajuster les poids des volumes
            volumeSrc.weight = 1f - t;
            volumeDest.weight = t;
            
            tempsEcoule += Time.deltaTime;
            yield return null;
        }
        
        // À la fin, appliquer le profil cible et restaurer le volume original
        volumePostProcess.profile = profilCible;
        volumePostProcess.weight = originalWeight;
        profilActuel = profilCible;
        
        // Nettoyer
        Destroy(volumeSrcObj);
        Destroy(volumeDestObj);
        
        transitionCoroutine = null;
    }
} 