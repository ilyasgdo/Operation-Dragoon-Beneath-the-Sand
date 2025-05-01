using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ObjectifManager : MonoBehaviour
{
    [System.Serializable]
    public class Objectif
    {
        public string id;
        public string description;
        public bool estOptionnel;
        public bool estComplété;
        public string objetSuivantId; // ID de l'objectif qui sera débloqué quand celui-ci est terminé
    }

    [Header("Configuration")]
    [Tooltip("Liste des objectifs du jeu")]
    public List<Objectif> objectifs = new List<Objectif>();

    [Header("Interface Utilisateur")]
    [Tooltip("Panneau UI contenant la liste des objectifs")]
    public GameObject panneauObjectifs;
    
    [Tooltip("Prefab pour chaque élément d'objectif dans la liste")]
    public GameObject prefabElementObjectif;
    
    [Tooltip("Transform parent pour les éléments d'objectif")]
    public Transform conteneurObjectifs;
    
    [Tooltip("Touche pour afficher/masquer les objectifs")]
    public KeyCode toucheObjectifs = KeyCode.Tab;

    // Cache pour l'accès rapide aux objectifs par ID
    private Dictionary<string, Objectif> objectifsParId = new Dictionary<string, Objectif>();
    private Dictionary<string, GameObject> elementsUIParId = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Initialiser le dictionnaire pour un accès rapide
        foreach (Objectif obj in objectifs)
        {
            objectifsParId[obj.id] = obj;
        }

        // Masquer le panneau des objectifs au démarrage
        if (panneauObjectifs != null)
        {
            panneauObjectifs.SetActive(false);
        }
    }

    private void Start()
    {
        // Créer les éléments UI pour chaque objectif
        CreerElementsObjectifs();
    }

    private void Update()
    {
        // Gérer l'affichage/masquage du panneau des objectifs
        if (Input.GetKeyDown(toucheObjectifs))
        {
            if (panneauObjectifs != null)
            {
                panneauObjectifs.SetActive(!panneauObjectifs.activeSelf);
            }
        }
    }

    private void CreerElementsObjectifs()
    {
        // Vérifier que tout est configuré correctement
        if (conteneurObjectifs == null || prefabElementObjectif == null)
        {
            Debug.LogWarning("Configuration incomplète pour les éléments UI des objectifs");
            return;
        }

        // Supprimer les anciens éléments
        foreach (Transform child in conteneurObjectifs)
        {
            Destroy(child.gameObject);
        }
        elementsUIParId.Clear();

        // Créer un élément UI pour chaque objectif
        foreach (Objectif obj in objectifs)
        {
            GameObject elementUI = Instantiate(prefabElementObjectif, conteneurObjectifs);
            Text texteObjectif = elementUI.GetComponentInChildren<Text>();
            Toggle checkboxObjectif = elementUI.GetComponentInChildren<Toggle>();

            if (texteObjectif != null)
            {
                string prefixe = obj.estOptionnel ? "[Optionnel] " : "";
                texteObjectif.text = prefixe + obj.description;
            }

            if (checkboxObjectif != null)
            {
                checkboxObjectif.isOn = obj.estComplété;
                checkboxObjectif.interactable = false; // Le joueur ne peut pas cocher/décocher directement
            }

            // Cacher les objectifs qui ne sont pas actifs au début du jeu
            elementUI.SetActive(EstObjectifActif(obj.id));

            // Enregistrer l'élément UI pour un accès ultérieur
            elementsUIParId[obj.id] = elementUI;
        }
    }

    // Méthode pour compléter un objectif et mettre à jour l'interface
    public void CompleterObjectif(string objectifId)
    {
        if (objectifsParId.TryGetValue(objectifId, out Objectif obj))
        {
            obj.estComplété = true;
            
            // Mettre à jour l'interface utilisateur
            if (elementsUIParId.TryGetValue(objectifId, out GameObject elementUI))
            {
                Toggle checkboxObjectif = elementUI.GetComponentInChildren<Toggle>();
                if (checkboxObjectif != null)
                {
                    checkboxObjectif.isOn = true;
                }
            }
            
            // Débloquer l'objectif suivant si nécessaire
            if (!string.IsNullOrEmpty(obj.objetSuivantId))
            {
                DebloquerObjectif(obj.objetSuivantId);
            }
            
            // Jouer un son ou une animation pour féliciter le joueur
            // TODO: Ajouter un son de réussite ici
            
            Debug.Log("Objectif complété: " + obj.description);
        }
    }
    
    // Méthode pour débloquer un objectif
    public void DebloquerObjectif(string objectifId)
    {
        if (objectifsParId.TryGetValue(objectifId, out Objectif obj))
        {
            // Activer l'élément UI correspondant
            if (elementsUIParId.TryGetValue(objectifId, out GameObject elementUI))
            {
                elementUI.SetActive(true);
            }
            
            Debug.Log("Objectif débloqué: " + obj.description);
        }
    }
    
    // Vérifier si un objectif est visible/actif
    public bool EstObjectifActif(string objectifId)
    {
        // Par défaut, seuls les objectifs qui n'ont pas d'objectif précédent sont actifs au début
        if (objectifsParId.TryGetValue(objectifId, out Objectif obj))
        {
            // Vérifie si cet objectif est mentionné comme "suivant" dans un autre objectif
            foreach (Objectif autreObj in objectifs)
            {
                if (autreObj.objetSuivantId == objectifId && !autreObj.estComplété)
                {
                    return false; // L'objectif précédent n'est pas complété
                }
            }
            
            return true; // Pas d'objectif précédent ou tous les objectifs précédents sont complétés
        }
        
        return false; // Objectif non trouvé
    }
    
    // Vérifier si tous les objectifs obligatoires sont complétés
    public bool TousObjectifsObligatoiresComplétés()
    {
        foreach (Objectif obj in objectifs)
        {
            if (!obj.estOptionnel && !obj.estComplété)
            {
                return false; // Au moins un objectif obligatoire n'est pas complété
            }
        }
        
        return true; // Tous les objectifs obligatoires sont complétés
    }
} 