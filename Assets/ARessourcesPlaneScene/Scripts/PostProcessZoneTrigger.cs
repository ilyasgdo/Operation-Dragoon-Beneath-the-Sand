using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ce script peut être attaché à un Collider (trigger) pour définir une zone
/// qui change automatiquement le profil de post-processing lorsque l'avion y entre.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PostProcessZoneTrigger : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Type d'ambiance à appliquer dans cette zone")]
    public ZoneType typeZone = ZoneType.Personnalisee;
    
    [Tooltip("Profil de post-processing personnalisé pour cette zone")]
    public VolumeProfile profilPersonnalise;
    
    [Tooltip("Durée de transition vers ce profil (secondes)")]
    [Range(0.1f, 5.0f)]
    public float dureeTransition = 1.0f;
    
    [Tooltip("Tag des objets pouvant déclencher cette zone (laisser vide pour tous)")]
    public string tagCible = "Player";
    
    [Tooltip("Activer/désactiver cette zone")]
    public bool estActive = true;
    
    // Contrôleurs référencés
    private PostProcessingManager postProcessManager;
    private ScenePostProcessController sceneController;
    
    // Pour suivre les objets qui sont entrés dans la zone
    private List<GameObject> objetsEnZone = new List<GameObject>();
    
    // Types prédéfinis de zones
    public enum ZoneType
    {
        Personnalisee,
        Eau,
        Combat,
        Brume,
        BasseAltitude,
        Normal
    }
    
    private void Awake()
    {
        // S'assurer que le collider est bien en mode Trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"Le collider de {gameObject.name} a été mis en mode Trigger pour PostProcessZoneTrigger");
        }
    }
    
    private void Start()
    {
        // Trouver les références aux contrôleurs
        postProcessManager = FindObjectOfType<PostProcessingManager>();
        sceneController = FindObjectOfType<ScenePostProcessController>();
        
        if (postProcessManager == null && sceneController == null)
        {
            Debug.LogWarning($"PostProcessZoneTrigger sur {gameObject.name}: Aucun contrôleur de post-processing trouvé dans la scène!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!estActive) return;
        
        // Vérifier si l'objet correspond au tag cible (ou si aucun tag n'est spécifié)
        if (string.IsNullOrEmpty(tagCible) || other.CompareTag(tagCible))
        {
            // Ajouter l'objet à la liste des objets dans la zone
            if (!objetsEnZone.Contains(other.gameObject))
            {
                objetsEnZone.Add(other.gameObject);
                
                // Appliquer le profil de post-processing
                AppliquerProfil();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Retirer l'objet de la liste des objets dans la zone
        if (objetsEnZone.Contains(other.gameObject))
        {
            objetsEnZone.Remove(other.gameObject);
            
            // Si la zone est vide, revenir au profil par défaut
            if (objetsEnZone.Count == 0)
            {
                RetablirProfilParDefaut();
            }
        }
    }
    
    /// <summary>
    /// Applique le profil de post-processing de cette zone
    /// </summary>
    private void AppliquerProfil()
    {
        if (sceneController != null)
        {
            // Utiliser le contrôleur de scène pour changer l'ambiance
            switch (typeZone)
            {
                case ZoneType.Eau:
                    sceneController.DeclencharAmbiance("eau");
                    break;
                case ZoneType.Combat:
                    sceneController.DeclencharAmbiance("combat");
                    break;
                case ZoneType.Brume:
                    sceneController.DeclencharAmbiance("brume");
                    break;
                case ZoneType.BasseAltitude:
                    sceneController.DeclencharAmbiance("bassealtitude");
                    break;
                case ZoneType.Normal:
                    sceneController.DeclencharAmbiance("defaut");
                    break;
                case ZoneType.Personnalisee:
                    // Si un profil personnalisé est spécifié et que le gestionnaire est disponible
                    if (profilPersonnalise != null && postProcessManager != null)
                    {
                        postProcessManager.ChangerProfil(profilPersonnalise, dureeTransition);
                    }
                    break;
            }
        }
        else if (postProcessManager != null && profilPersonnalise != null && typeZone == ZoneType.Personnalisee)
        {
            // Utiliser directement le gestionnaire pour le profil personnalisé
            postProcessManager.ChangerProfil(profilPersonnalise, dureeTransition);
        }
    }
    
    /// <summary>
    /// Rétablit le profil par défaut de la scène
    /// </summary>
    private void RetablirProfilParDefaut()
    {
        if (sceneController != null)
        {
            sceneController.DeclencharAmbiance("defaut");
        }
    }
    
    private void OnDrawGizmos()
    {
        // Afficher une visualisation de la zone dans l'éditeur
        Gizmos.color = GetGizmoColor();
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
            }
            // Pour les autres types de colliders, on dessine juste les limites
            else
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
    
    private Color GetGizmoColor()
    {
        // Couleur selon le type de zone
        switch (typeZone)
        {
            case ZoneType.Eau:
                return new Color(0, 0.5f, 1f, 0.3f); // Bleu
            case ZoneType.Combat:
                return new Color(1f, 0.2f, 0.2f, 0.3f); // Rouge
            case ZoneType.Brume:
                return new Color(0.7f, 0.7f, 0.7f, 0.3f); // Gris
            case ZoneType.BasseAltitude:
                return new Color(1f, 0.8f, 0, 0.3f); // Orange
            case ZoneType.Normal:
                return new Color(0.3f, 1f, 0.3f, 0.3f); // Vert
            default:
                return new Color(1f, 0.5f, 1f, 0.3f); // Violet pour personnalisé
        }
    }
} 