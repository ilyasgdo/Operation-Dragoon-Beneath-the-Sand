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
    
    [Tooltip("Espacement entre les éléments d'objectif")]
    public float espacementElements = 360f; // Triplé (120f * 3)
    
    // Dictionnaire stockant tous les objectifs (ID -> ElementObjectif)
    private Dictionary<string, ElementObjectif> objectifs = new Dictionary<string, ElementObjectif>();
    
    // Dictionnaire stockant l'état des objectifs (ID -> bool)
    private Dictionary<string, bool> etatObjectifs = new Dictionary<string, bool>();
    
    // Ensemble pour suivre les tableaux déjà visités
    private HashSet<string> tableauxVisites = new HashSet<string>();
    
    // Nombre total de tableaux à visiter
    private int nombreTotalTableaux = 0;
    
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
        
        // Configurer le conteneur d'objectifs s'il existe
        if (conteneurObjectifs != null)
        {
            // Vérifier si le conteneur a déjà un VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = conteneurObjectifs.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                // Ajouter un VerticalLayoutGroup pour organiser les éléments verticalement
                layoutGroup = conteneurObjectifs.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.spacing = espacementElements; // Utiliser l'espacement configurable
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                layoutGroup.childControlHeight = true;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.padding = new RectOffset(120, 120, 120, 120); // Padding triplé (40 * 3)
                Debug.Log("VerticalLayoutGroup ajouté au conteneur d'objectifs");
            }
            else
            {
                // Mettre à jour l'espacement si le VerticalLayoutGroup existe déjà
                layoutGroup.spacing = espacementElements;
                layoutGroup.padding = new RectOffset(120, 120, 120, 120); // Padding triplé (40 * 3)
            }
            
            // S'assurer que le conteneur utilise un ContentSizeFitter pour s'adapter à son contenu
            ContentSizeFitter sizeFitter = conteneurObjectifs.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = conteneurObjectifs.gameObject.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Debug.Log("ContentSizeFitter ajouté au conteneur d'objectifs");
            }
        }
        
        // Initialiser les ensembles pour les tableaux
        tableauxVisites = new HashSet<string>();
    }
    
    private void Start()
    {
        // Délai pour permettre à tous les objets de s'initialiser
        StartCoroutine(InitialiserObjectifsAvecDelai());
    }
    
    private IEnumerator InitialiserObjectifsAvecDelai()
    {
        // Attendre une frame pour s'assurer que tous les objets sont initialisés
        yield return null;
        
        // Initialiser les objectifs principaux
        
        // Ajouter l'objectif d'écouter le message du Général de Gaulle
        AjouterObjectifEcouterMessageDeGaulle();
        
        // Compter le nombre de tableaux dans la scène
        TableauInteractif[] tableaux = FindObjectsOfType<TableauInteractif>();
        nombreTotalTableaux = tableaux.Length;
        
        Debug.Log("Nombre total de tableaux trouvés: " + nombreTotalTableaux);
        
        // Donner un ID unique à chaque tableau qui n'en a pas et s'assurer qu'il a une référence au système d'objectifs
        foreach (TableauInteractif tableau in tableaux)
        {
            if (string.IsNullOrEmpty(tableau.tableauId))
            {
                tableau.tableauId = "tableau_" + tableau.GetInstanceID();
                Debug.Log("ID généré pour tableau: " + tableau.tableauId);
            }
            
            // Assigner ce SystemeObjectifs au tableau s'il n'en a pas
            if (tableau.systemeObjectifs == null)
            {
                tableau.systemeObjectifs = this;
                Debug.Log("SystemeObjectifs assigné au tableau: " + tableau.tableauId);
            }
        }
        
        // Ajouter l'objectif si des tableaux sont présents
        if (nombreTotalTableaux > 0)
        {
            AjouterObjectifInteragirTableaux(nombreTotalTableaux);
        }
        
        Debug.Log("Objectifs initialisés: Message de De Gaulle et " + nombreTotalTableaux + " tableaux à trouver.");
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
            // Instancier le nouvel élément d'objectif
            GameObject nouvelElementGO = Instantiate(prefabElementObjectif, conteneurObjectifs);
            ElementObjectif nouvelElement = nouvelElementGO.GetComponent<ElementObjectif>();
            
            if (nouvelElement != null)
            {
                nouvelElement.Initialiser(id, description, estOptionnel, false);
                objectifs.Add(id, nouvelElement);
                
                // Forcer la mise à jour du layout
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(conteneurObjectifs.GetComponent<RectTransform>());
                
                // Attendre une frame pour s'assurer que le layout est correct
                StartCoroutine(ForceRefreshLayout());
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
    
    // Coroutine pour s'assurer que le layout est correctement rafraîchi
    private IEnumerator ForceRefreshLayout()
    {
        yield return null; // Attendre une frame
        
        if (conteneurObjectifs != null)
        {
            // Forcer le rafraîchissement du layout une deuxième fois
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(conteneurObjectifs.GetComponent<RectTransform>());
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
        // Créer une copie des clés pour éviter l'erreur "Collection was modified"
        List<string> listeIds = new List<string>(etatObjectifs.Keys);
        
        // Parcourir la liste des IDs et réinitialiser chaque objectif
        foreach (string id in listeIds)
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
        
        // Réinitialiser les tableaux visités
        tableauxVisites.Clear();
    }
    
    /// <summary>
    /// Créer un objectif pour écouter le message du Général de Gaulle sur la radio
    /// </summary>
    public void AjouterObjectifEcouterMessageDeGaulle()
    {
        string idObjectif = "ecouter_message_de_gaulle";
        AjouterObjectif(idObjectif, "Écouter le message du Général de Gaulle sur la radio");
    }
    
    /// <summary>
    /// Marquer l'objectif d'écouter le message du Général de Gaulle comme complété
    /// Cette méthode devrait être appelée par la classe RadioInteractive quand le joueur trouve la bonne fréquence
    /// </summary>
    public void CompleterObjectifMessageDeGaulle()
    {
        CompleterObjectif("ecouter_message_de_gaulle");
    }
    
    /// <summary>
    /// Créer un objectif pour interagir avec tous les tableaux
    /// </summary>
    /// <param name="nombreTableaux">Le nombre total de tableaux à trouver</param>
    public void AjouterObjectifInteragirTableaux(int nombreTableaux)
    {
        string idObjectif = "interagir_tableaux";
        AjouterObjectif(idObjectif, "Interagir avec tous les tableaux (0/" + nombreTableaux + ")");
        
        // Initialiser le compteur
        nombreTotalTableaux = nombreTableaux;
        tableauxVisites.Clear();
        
        Debug.Log("Objectif tableaux initialisé: 0/" + nombreTableaux);
    }
    
    /// <summary>
    /// Enregistrer un tableau visité et mettre à jour l'objectif
    /// </summary>
    /// <param name="tableauId">Identifiant unique du tableau</param>
    public void EnregistrerTableauVisite(string tableauId)
    {
        if (string.IsNullOrEmpty(tableauId))
        {
            Debug.LogError("ID de tableau invalide!");
            return;
        }
        
        // Vérifier si ce tableau a déjà été visité (pour éviter les doublons)
        if (tableauxVisites.Contains(tableauId))
        {
            Debug.Log("Tableau déjà visité: " + tableauId);
            return;
        }
            
        // Ajouter ce tableau à l'ensemble des tableaux visités
        tableauxVisites.Add(tableauId);
        
        int nombreTableauxVisites = tableauxVisites.Count;
        
        Debug.Log("Tableau visité: " + tableauId + " - Total: " + nombreTableauxVisites + "/" + nombreTotalTableaux);
        
        // Mettre à jour la description de l'objectif
        if (objectifs.ContainsKey("interagir_tableaux"))
        {
            // Mettre à jour le texte de l'objectif avec la progression actuelle
            ElementObjectif element = objectifs["interagir_tableaux"];
            if (element != null)
            {
                // Utiliser la méthode pour mettre à jour le texte
                element.MettreAJourTexte("Interagir avec tous les tableaux (" + nombreTableauxVisites + "/" + nombreTotalTableaux + ")");
                
                // Si tous les tableaux ont été visités, marquer l'objectif comme complété
                if (nombreTableauxVisites >= nombreTotalTableaux)
                {
                    CompleterObjectif("interagir_tableaux");
                }
            }
        }
    }
    
    // Pour le débogage, ajouter une méthode pour signaler quels tableaux ont été visités
    public void AfficherTableauxVisites()
    {
        string tableauxStr = "Tableaux visités (" + tableauxVisites.Count + "/" + nombreTotalTableaux + "): ";
        foreach (string tableauId in tableauxVisites)
        {
            tableauxStr += tableauId + ", ";
        }
        Debug.Log(tableauxStr);
    }
}