using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ce script doit être ajouté à la scène pour définir les paramètres de post-traitement spécifiques
/// et permettre le déclenchement des changements d'ambiance en fonction des événements de jeu.
/// </summary>
public class ScenePostProcessController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Profil de post-processing par défaut pour cette scène")]
    public VolumeProfile profilParDefaut;
    
    [Header("Ambiances")]
    [Tooltip("Profil pour les zones d'eau")]
    public VolumeProfile profilZoneEau;
    [Tooltip("Profil pour les zones de combat")]
    public VolumeProfile profilCombat;
    [Tooltip("Profil pour les zones brumeuses")]
    public VolumeProfile profilBrume;
    [Tooltip("Profil pour les effets de basse altitude")]
    public VolumeProfile profilBasseAltitude;
    
    [Header("Transitions")]
    [Tooltip("Durée de transition entre ambiances (secondes)")]
    public float dureeTransition = 1.5f;
    [Tooltip("Hauteur minimum pour considérer basse altitude")]
    public float hauteurBasseAltitude = 30f;
    
    // Référence au gestionnaire global de post-processing
    private PostProcessingManager postProcessManager;
    
    // État actuel
    private string ambianceActuelle = "Defaut";
    private float dernierChangementAmbiance = 0f;
    private Transform joueur;
    
    private void Start()
    {
        // Rechercher le gestionnaire de post-processing
        postProcessManager = FindObjectOfType<PostProcessingManager>();
        
        if (postProcessManager != null)
        {
            // Appliquer directement le profil par défaut de cette scène
            if (profilParDefaut != null)
            {
                postProcessManager.ChangerProfilImmediat(profilParDefaut);
                Debug.Log("Profil de post-processing de scène appliqué: " + gameObject.scene.name);
            }
        }
        else
        {
            Debug.LogWarning("Aucun PostProcessingManager trouvé! Créez un objet avec ce composant dans votre scène principale.");
        }
        
        // Trouver l'avion/joueur
        joueur = FindObjectOfType<AvionController>()?.transform;
        if (joueur == null)
        {
            joueur = Camera.main?.transform;
        }
    }
    
    private void Update()
    {
        // Ne rien faire si le gestionnaire n'existe pas ou si le joueur n'est pas trouvé
        if (postProcessManager == null || joueur == null) return;
        
        // Limiter la fréquence des vérifications d'ambiance
        if (Time.time - dernierChangementAmbiance < 0.5f) return;
        
        // Vérifier l'ambiance en fonction de l'environnement
        DetecterAmbianceBaseeEnvironnement();
    }
    
    /// <summary>
    /// Vérifie l'environnement pour changer automatiquement l'ambiance
    /// </summary>
    private void DetecterAmbianceBaseeEnvironnement()
    {
        // Vérifier si l'avion est à basse altitude
        if (joueur.position.y < hauteurBasseAltitude && profilBasseAltitude != null)
        {
            if (ambianceActuelle != "BasseAltitude")
            {
                ChangerAmbiance("BasseAltitude", profilBasseAltitude);
            }
            return;
        }
        
        // Détection de zones d'eau (raycast vers le bas)
        RaycastHit hit;
        if (Physics.Raycast(joueur.position, Vector3.down, out hit, 500f))
        {
            // Vérifier si c'est une zone d'eau par le tag ou la couche
            if (hit.collider.CompareTag("Eau") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Eau"))
            {
                if (ambianceActuelle != "Eau" && profilZoneEau != null)
                {
                    ChangerAmbiance("Eau", profilZoneEau);
                    return;
                }
            }
            // Vérifier si c'est une zone brumeuse
            else if (hit.collider.CompareTag("ZoneBrume") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Brume"))
            {
                if (ambianceActuelle != "Brume" && profilBrume != null)
                {
                    ChangerAmbiance("Brume", profilBrume);
                    return;
                }
            }
        }
        
        // Par défaut, revenir à l'ambiance standard si on n'est dans aucune zone spéciale
        if (ambianceActuelle != "Defaut")
        {
            ChangerAmbiance("Defaut", profilParDefaut);
        }
    }
    
    /// <summary>
    /// Change l'ambiance visuelle du jeu
    /// </summary>
    private void ChangerAmbiance(string nomAmbiance, VolumeProfile profil)
    {
        if (profil != null && postProcessManager != null)
        {
            ambianceActuelle = nomAmbiance;
            dernierChangementAmbiance = Time.time;
            
            postProcessManager.ChangerProfil(profil, dureeTransition);
            Debug.Log($"Changement d'ambiance: {nomAmbiance}");
        }
    }
    
    /// <summary>
    /// Déclencher manuellement un changement d'ambiance (peut être appelé par d'autres scripts)
    /// </summary>
    public void DeclencharAmbiance(string typeAmbiance)
    {
        switch (typeAmbiance.ToLower())
        {
            case "combat":
                if (profilCombat != null)
                    ChangerAmbiance("Combat", profilCombat);
                break;
            case "eau":
                if (profilZoneEau != null)
                    ChangerAmbiance("Eau", profilZoneEau);
                break;
            case "brume":
                if (profilBrume != null)
                    ChangerAmbiance("Brume", profilBrume);
                break;
            case "bassealtitude":
                if (profilBasseAltitude != null)
                    ChangerAmbiance("BasseAltitude", profilBasseAltitude);
                break;
            case "defaut":
            case "normal":
                if (profilParDefaut != null)
                    ChangerAmbiance("Defaut", profilParDefaut);
                break;
            default:
                Debug.LogWarning($"Type d'ambiance inconnu: {typeAmbiance}");
                break;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualiser la hauteur de basse altitude
        Gizmos.color = Color.yellow;
        Vector3 position = transform.position;
        position.y = hauteurBasseAltitude;
        Gizmos.DrawLine(position - Vector3.right * 100f, position + Vector3.right * 100f);
        Gizmos.DrawLine(position - Vector3.forward * 100f, position + Vector3.forward * 100f);
    }
} 