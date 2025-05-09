using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère le niveau de détail (LOD) d'un objet terrain en fonction de sa distance au joueur.
/// Supporte plusieurs niveaux de LOD, des shaders dynamiques et l'occlusion.
/// </summary>
public class TerrainObjectLOD : MonoBehaviour
{
    [Header("Configuration LOD")]
    [Tooltip("Distance pour le LOD 0 (haute qualité)")]
    public float distanceLOD0 = 200f;
    
    [Tooltip("Distance pour le LOD 1 (qualité moyenne)")]
    public float distanceLOD1 = 400f;
    
    [Tooltip("Distance pour le LOD 2 (basse qualité)")]
    public float distanceLOD2 = 800f;
    
    [Tooltip("Désactiver l'objet au-delà de cette distance")]
    public float distanceDesactivation = 1000f;
    
    [Header("Optimisations")]
    [Tooltip("Activer la réduction du nombre de triangles pour les LOD élevés")]
    public bool reduireTriangles = true;
    
    [Tooltip("Activer l'utilisation de shaders simplifiés pour les LOD élevés")]
    public bool utiliserShadersSimplifie = true;
    
    [Tooltip("Utiliser le culling d'occlusion (cache des objets non visibles)")]
    public bool activerOcclusion = true;
    
    [Tooltip("Intervalle de vérification de l'occlusion (en secondes)")]
    public float intervalleVerificationOcclusion = 0.5f;
    
    [Header("Renderers")]
    [Tooltip("Renderers à activer uniquement pour LOD 0 (haute qualité)")]
    public List<Renderer> renderersHauteQualite;
    
    [Tooltip("Renderers à activer pour LOD 0 et 1")]
    public List<Renderer> renderersQualiteMoyenne;
    
    [Tooltip("Renderers toujours actifs (tous niveaux de LOD)")]
    public List<Renderer> renderersBasseQualite;
    
    [Header("Matériaux")]
    [Tooltip("Matériau pour LOD 0 (détaillé)")]
    public Material materielHauteQualite;
    
    [Tooltip("Matériau pour LOD 1 (moyen)")]
    public Material materielQualiteMoyenne;
    
    [Tooltip("Matériau pour LOD 2 (simplifié)")]
    public Material materielBasseQualite;
    
    // Variables internes
    private Transform joueur;
    private int niveauLODActuel = 0;
    private Dictionary<Renderer, Material> materiauxOriginaux = new Dictionary<Renderer, Material>();
    private bool estOcclus = false;
    private float tempsDepuisVerifOcclusion = 0f;
    
    void Start()
    {
        // Trouver le joueur
        joueur = Camera.main?.transform;
        if (joueur == null)
        {
            // Essayer de trouver l'avion
            var avion = FindObjectOfType<AvionController>();
            if (avion != null)
                joueur = avion.transform;
        }
        
        if (joueur == null)
        {
            Debug.LogWarning("TerrainObjectLOD: Impossible de trouver le joueur!");
            enabled = false;
            return;
        }
        
        // Sauvegarder les matériaux originaux pour tous les renderers
        SauvegarderMateriauxOriginaux();
        
        // Appliquer le LOD initial
        ActualiserLOD();
    }
    
    void Update()
    {
        // Calculer la distance au joueur
        float distance = Vector3.Distance(transform.position, joueur.position);
        
        // Vérifier et appliquer le LOD approprié
        ActualiserLODParDistance(distance);
        
        // Vérifier l'occlusion à intervalles réguliers
        if (activerOcclusion)
        {
            tempsDepuisVerifOcclusion += Time.deltaTime;
            if (tempsDepuisVerifOcclusion >= intervalleVerificationOcclusion)
            {
                tempsDepuisVerifOcclusion = 0f;
                VerifierOcclusion(distance);
            }
        }
    }
    
    void SauvegarderMateriauxOriginaux()
    {
        // Sauvegarder les matériaux originaux pour restauration future
        List<Renderer> tousRenderers = new List<Renderer>();
        tousRenderers.AddRange(renderersHauteQualite);
        tousRenderers.AddRange(renderersQualiteMoyenne);
        tousRenderers.AddRange(renderersBasseQualite);
        
        foreach (Renderer renderer in tousRenderers)
        {
            if (renderer != null && !materiauxOriginaux.ContainsKey(renderer))
            {
                materiauxOriginaux[renderer] = renderer.sharedMaterial;
            }
        }
        
        // Si aucun matériau n'est spécifié, utiliser les originaux
        if (materielHauteQualite == null && renderersHauteQualite.Count > 0 && renderersHauteQualite[0] != null)
        {
            materielHauteQualite = renderersHauteQualite[0].sharedMaterial;
        }
        
        if (materielQualiteMoyenne == null)
        {
            materielQualiteMoyenne = materielHauteQualite;
        }
        
        if (materielBasseQualite == null && materielQualiteMoyenne != null)
        {
            // Créer une version simplifiée du matériau moyen
            materielBasseQualite = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            if (materielBasseQualite != null && materielQualiteMoyenne != null)
            {
                materielBasseQualite.SetColor("_BaseColor", materielQualiteMoyenne.GetColor("_BaseColor"));
                materielBasseQualite.SetFloat("_Smoothness", 0f);
            }
        }
    }
    
    void ActualiserLOD()
    {
        // Appliquer le niveau de LOD actuel
        switch (niveauLODActuel)
        {
            case 0: // Haute qualité
                ActiverHauteQualite();
                break;
            case 1: // Qualité moyenne
                ActiverQualiteMoyenne();
                break;
            case 2: // Basse qualité
                ActiverBasseQualite();
                break;
            case 3: // Invisible
                gameObject.SetActive(false);
                break;
        }
    }
    
    void ActualiserLODParDistance(float distance)
    {
        int nouveauLOD;
        
        // Déterminer le niveau de LOD basé sur la distance
        if (distance <= distanceLOD0)
            nouveauLOD = 0;
        else if (distance <= distanceLOD1)
            nouveauLOD = 1;
        else if (distance <= distanceLOD2)
            nouveauLOD = 2;
        else if (distance <= distanceDesactivation)
            nouveauLOD = 2; // Toujours LOD 2 jusqu'à désactivation
        else
            nouveauLOD = 3; // Désactivé
        
        // Mettre à jour uniquement si le niveau a changé
        if (nouveauLOD != niveauLODActuel)
        {
            niveauLODActuel = nouveauLOD;
            ActualiserLOD();
        }
    }
    
    void ActiverHauteQualite()
    {
        // Activer tous les renderers
        foreach (Renderer renderer in renderersHauteQualite)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        foreach (Renderer renderer in renderersQualiteMoyenne)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        foreach (Renderer renderer in renderersBasseQualite)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        // Restaurer les matériaux originaux de haute qualité
        if (utiliserShadersSimplifie)
        {
            AppliquerMateriaux(materielHauteQualite, true);
        }
    }
    
    void ActiverQualiteMoyenne()
    {
        // Désactiver les renderers de haute qualité
        foreach (Renderer renderer in renderersHauteQualite)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        // Activer les renderers de qualité moyenne et basse
        foreach (Renderer renderer in renderersQualiteMoyenne)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        foreach (Renderer renderer in renderersBasseQualite)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        // Appliquer le matériau de qualité moyenne si nécessaire
        if (utiliserShadersSimplifie && materielQualiteMoyenne != null)
        {
            AppliquerMateriaux(materielQualiteMoyenne, false);
        }
    }
    
    void ActiverBasseQualite()
    {
        // Désactiver les renderers de haute et moyenne qualité
        foreach (Renderer renderer in renderersHauteQualite)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        foreach (Renderer renderer in renderersQualiteMoyenne)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        // N'activer que les renderers de basse qualité
        foreach (Renderer renderer in renderersBasseQualite)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        // Appliquer le matériau de basse qualité pour les objets restants
        if (utiliserShadersSimplifie && materielBasseQualite != null)
        {
            AppliquerMateriaux(materielBasseQualite, false);
        }
    }
    
    void AppliquerMateriaux(Material materiel, bool restaurerOriginaux)
    {
        if (restaurerOriginaux)
        {
            // Restaurer tous les matériaux originaux
            foreach (var kvp in materiauxOriginaux)
            {
                if (kvp.Key != null && kvp.Value != null)
                    kvp.Key.sharedMaterial = kvp.Value;
            }
        }
        else if (materiel != null)
        {
            // Appliquer le matériau spécifié aux renderers actifs
            foreach (Renderer renderer in renderersBasseQualite)
            {
                if (renderer != null && renderer.enabled)
                    renderer.sharedMaterial = materiel;
            }
        }
    }
    
    void VerifierOcclusion(float distance)
    {
        // Ne vérifier l'occlusion que pour les objets à une certaine distance
        if (distance < distanceLOD1)
        {
            if (estOcclus)
            {
                estOcclus = false;
                gameObject.SetActive(true);
                ActualiserLOD(); // Réappliquer le LOD approprié
            }
            return;
        }
        
        // Vérifier si l'objet est visible par le joueur
        Vector3 directionVersJoueur = joueur.position - transform.position;
        Ray ray = new Ray(transform.position, directionVersJoueur.normalized);
        
        // Lancer un rayon dans la direction du joueur
        if (Physics.Raycast(ray, out RaycastHit hit, directionVersJoueur.magnitude))
        {
            // Si le rayon touche quelque chose avant d'atteindre le joueur, cet objet est caché
            if (hit.transform != joueur)
            {
                if (!estOcclus)
                {
                    estOcclus = true;
                    gameObject.SetActive(false);
                }
            }
            else if (estOcclus)
            {
                estOcclus = false;
                gameObject.SetActive(true);
                ActualiserLOD();
            }
        }
        else if (estOcclus)
        {
            // Aucun obstacle, l'objet est visible
            estOcclus = false;
            gameObject.SetActive(true);
            ActualiserLOD();
        }
    }
    
    // Méthode publique pour forcer un niveau de LOD spécifique
    public void ForcerLOD(int niveau)
    {
        if (niveau >= 0 && niveau <= 3 && niveau != niveauLODActuel)
        {
            niveauLODActuel = niveau;
            ActualiserLOD();
        }
    }
} 