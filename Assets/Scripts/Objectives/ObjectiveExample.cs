using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exemple de script montrant comment utiliser le système d'objectifs dans votre jeu.
/// Attachez ce script à un GameObject dans votre scène pour tester le système.
/// </summary>
public class ObjectiveExample : MonoBehaviour
{
    private ObjectiveManager objectiveManager;
    
    // Exemples de noms d'objectifs pour les tests
    private string[] objectifExemples = {
        "Trouver le document secret",
        "Parler au commandant",
        "Récupérer l'équipement",
        "Explorer la zone de combat",
        "Éliminer l'ennemi"
    };
    
    void Start()
    {
        // Obtenir la référence à l'ObjectiveManager
        objectiveManager = ObjectiveManager.Instance;
        
        if (objectiveManager == null)
        {
            Debug.LogError("ObjectiveManager non trouvé! Assurez-vous qu'il existe dans la scène.");
            return;
        }
        
        // Ajouter un objectif initial
        objectiveManager.AddObjective(
            "Commencer la mission", 
            "Explorez la zone et trouvez des indices sur l'opération Dragoon.",
            false,  // Non optionnel
            10      // Points de récompense
        );
    }
    
    void Update()
    {
        // Exemple: Appuyez sur la touche 'O' pour ajouter un nouvel objectif aléatoire
        if (Input.GetKeyDown(KeyCode.O))
        {
            AddRandomObjective();
        }
        
        // Exemple: Appuyez sur la touche 'P' pour compléter un objectif aléatoire actif
        if (Input.GetKeyDown(KeyCode.P))
        {
            CompleteRandomObjective();
        }
    }
    
    // Ajoute un objectif aléatoire à partir de la liste d'exemples
    void AddRandomObjective()
    {
        if (objectiveManager == null) return;
        
        int index = Random.Range(0, objectifExemples.Length);
        string title = objectifExemples[index];
        
        // Vérifier si cet objectif existe déjà
        if (!objectiveManager.IsObjectiveActive(title) && !objectiveManager.IsObjectiveCompleted(title))
        {
            objectiveManager.AddObjective(
                title,
                "Description détaillée de l'objectif: " + title,
                Random.value > 0.7f,  // 30% de chance d'être optionnel
                Random.Range(5, 25)    // Points de récompense entre 5 et 25
            );
        }
    }
    
    // Complète un objectif aléatoire parmi les objectifs actifs
    void CompleteRandomObjective()
    {
        if (objectiveManager == null) return;
        
        List<Objective> activeObjectives = objectiveManager.GetActiveObjectives();
        
        if (activeObjectives.Count > 0)
        {
            int index = Random.Range(0, activeObjectives.Count);
            objectiveManager.CompleteObjective(activeObjectives[index].title);
        }
    }
}