using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Système de gestion des objectifs dans le jeu
/// </summary>
public class SystemeObjectifs : MonoBehaviour
{
    [Tooltip("Préfab d'un élément d'objectif à instancier")]
    public GameObject prefabElementObjectif;
    
    [Tooltip("Conteneur parent où ajouter les objectifs dans l'UI")]
    public Transform conteneurObjectifs;
    
    [Tooltip("Texte affiché quand tous les objectifs sont complétés")]
    public Text texteObjectifsCompletes;
    
    [Tooltip("Son joué quand un objectif est complété")]
    public AudioClip sonObjectifComplete;
    
    [Tooltip("Son joué quand tous les objectifs sont complétés")]
    public AudioClip sonTousObjectifsCompletes;
    
    // Dictionnaire stockant tous les objectifs (ID -> ElementObjectif)
    private Dictionary<string, ElementObjectif> objectifs = new Dictionary<string, ElementObjectif>();
    
    // Dictionnaire stockant l'état des objectifs (ID -> bool)
    private Dictionary<string, bool> etatObjectifs = new Dictionary<string, bool>();
    
    // Composant AudioSource pour les sons
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Initialiser l'AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Cacher le message de complétion au démarrage
        if (texteObjectifsCompletes != null)
        {
            texteObjectifsCompletes.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Ajouter un nouvel objectif
    /// </summary>
    public void AjouterObjectif(string id, string description, bool estOptionnel = false)
    {
        // Vérifier si l'objectif existe déjà
        if (etatObjectifs.ContainsKey(id))
        {
            Debug.LogWarning("L'objectif avec l'ID " + id + " existe déjà!");
            return;
        }
        
        // Ajouter l'objectif au dictionnaire d'état
        etatObjectifs.Add(id, false);
        
        // Créer l'élément UI pour l'objectif
        if (prefabElementObjectif != null && conteneurObjectifs != null)
        {
            GameObject nouvelElementGO = Instantiate(prefabElementObjectif, conteneurObjectifs);
            ElementObjectif nouvelElement = nouvelElementGO.GetComponent<ElementObjectif>();
            
            if (nouvelElement != null)
            {
                nouvelElement.Initialiser(id, description, estOptionnel, false);
                objectifs.Add(id, nouvelElement);
            }
            else
            {
                Debug.LogError("Le préfab d'élément d'objectif ne contient pas de composant ElementObjectif!");
            }
        }
        else
        {
            Debug.LogWarning("Préfab d'élément d'objectif ou conteneur non défini!");
        }
    }
    
    /// <summary>
    /// Compléter un objectif
    /// </summary>
    public void CompleterObjectif(string id)
    {
        if (etatObjectifs.ContainsKey(id) && !etatObjectifs[id])
        {
            // Mettre à jour l'état
            etatObjectifs[id] = true;
            
            // Mettre à jour l'UI
            if (objectifs.ContainsKey(id))
            {
                objectifs[id].MarquerComplete(true);
            }
            
            // Jouer le son de complétion
            if (audioSource != null && sonObjectifComplete != null)
            {
                audioSource.PlayOneShot(sonObjectifComplete);
            }
            
            // Vérifier si tous les objectifs sont complétés
            VerifierTousObjectifsCompletes();
        }
    }
    
    /// <summary>
    /// Vérifier si un objectif spécifique est complété
    /// </summary>
    public bool EstObjectifComplete(string id)
    {
        if (etatObjectifs.ContainsKey(id))
        {
            return etatObjectifs[id];
        }
        return false;
    }
    
    /// <summary>
    /// Vérifier si tous les objectifs sont complétés
    /// </summary>
    private void VerifierTousObjectifsCompletes()
    {
        bool tousCompletes = true;
        
        // Parcourir tous les objectifs
        foreach (KeyValuePair<string, bool> objectif in etatObjectifs)
        {
            // Si un seul objectif non-optionnel n'est pas complété, alors ce n'est pas fini
            if (!objectif.Value)
            {
                tousCompletes = false;
                break;
            }
        }
        
        // Si tous les objectifs sont complétés
        if (tousCompletes && etatObjectifs.Count > 0)
        {
            if (texteObjectifsCompletes != null)
            {
                texteObjectifsCompletes.gameObject.SetActive(true);
            }
            
            // Jouer le son quand tous les objectifs sont complétés
            if (audioSource != null && sonTousObjectifsCompletes != null)
            {
                audioSource.PlayOneShot(sonTousObjectifsCompletes);
            }
            
            Debug.Log("Tous les objectifs ont été complétés!");
        }
    }
    
    /// <summary>
    /// Réinitialiser tous les objectifs
    /// </summary>
    public void ReinitialiserObjectifs()
    {
        // Parcourir tous les objectifs et les réinitialiser
        foreach (string id in etatObjectifs.Keys)
        {
            etatObjectifs[id] = false;
            
            if (objectifs.ContainsKey(id))
            {
                objectifs[id].MarquerComplete(false);
            }
        }
        
        // Cacher le message de complétion
        if (texteObjectifsCompletes != null)
        {
            texteObjectifsCompletes.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Exemple d'utilisation - pour créer un objectif de trouver le code d'une porte
    /// </summary>
    public void ExempleObjectifCodePorte(string idPorte, string codePorte)
    {
        string idObjectif = "trouver_code_" + idPorte;
        AjouterObjectif(idObjectif, "Trouver le code de la porte: " + idPorte);
        
        // Vous pourriez ajouter un listener pour vérifier quand le joueur entre le bon code
        // Ceci est juste un exemple, à adapter selon votre système
        // Par exemple, dans le DoorController, vous pourriez appeler cette méthode quand le code est correct
    }
} 