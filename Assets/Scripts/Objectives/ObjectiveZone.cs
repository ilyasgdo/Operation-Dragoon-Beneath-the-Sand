using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script pour définir une zone qui peut déclencher ou compléter un objectif.
/// Attachez ce script à un GameObject avec un Collider configuré comme Trigger.
/// </summary>
public class ObjectiveZone : MonoBehaviour
{
    [Header("Configuration de l'Objectif")]
    [SerializeField] private string objectiveTitle = "Nouvel objectif";
    [SerializeField] [TextArea(2, 4)] private string objectiveDescription = "Description de l'objectif";
    [SerializeField] private bool isOptional = false;
    [SerializeField] private int rewardPoints = 10;
    
    [Header("Comportement")]
    [SerializeField] private bool addObjectiveOnEnter = true;
    [SerializeField] private bool completeObjectiveOnEnter = false;
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Événements")]
    [SerializeField] private UnityEvent onObjectiveAdded;
    [SerializeField] private UnityEvent onObjectiveCompleted;
    
    private ObjectiveManager objectiveManager;
    private bool playerInZone = false;
    private bool objectiveAdded = false;
    private bool objectiveCompleted = false;
    
    private void Start()
    {
        objectiveManager = ObjectiveManager.Instance;
        
        if (objectiveManager == null)
        {
            Debug.LogError("ObjectiveManager non trouvé! Assurez-vous qu'il existe dans la scène.");
        }
        
        // Vérifier que le GameObject a un Collider configuré comme Trigger
        Collider collider = GetComponent<Collider>();
        if (collider == null || !collider.isTrigger)
        {
            Debug.LogWarning("ObjectiveZone nécessite un Collider configuré comme Trigger!", gameObject);
        }
    }
    
    private void Update()
    {
        // Gérer l'interaction si nécessaire
        if (playerInZone && requiresInteraction && !objectiveCompleted)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                if (!objectiveAdded && addObjectiveOnEnter)
                {
                    AddObjective();
                }
                
                if (completeObjectiveOnEnter)
                {
                    CompleteObjective();
                }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;
            
            // Si l'interaction n'est pas requise, ajouter/compléter l'objectif immédiatement
            if (!requiresInteraction)
            {
                if (!objectiveAdded && addObjectiveOnEnter)
                {
                    AddObjective();
                }
                
                if (completeObjectiveOnEnter)
                {
                    CompleteObjective();
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
        }
    }
    
    private void AddObjective()
    {
        if (objectiveManager != null && !objectiveAdded)
        {
            // Vérifier si l'objectif existe déjà
            if (!objectiveManager.IsObjectiveActive(objectiveTitle) && 
                !objectiveManager.IsObjectiveCompleted(objectiveTitle))
            {
                objectiveManager.AddObjective(objectiveTitle, objectiveDescription, isOptional, rewardPoints);
                objectiveAdded = true;
                onObjectiveAdded?.Invoke();
            }
        }
    }
    
    private void CompleteObjective()
    {
        if (objectiveManager != null && !objectiveCompleted)
        {
            // S'assurer que l'objectif est actif avant de le compléter
            if (objectiveManager.IsObjectiveActive(objectiveTitle))
            {
                objectiveManager.CompleteObjective(objectiveTitle);
                objectiveCompleted = true;
                onObjectiveCompleted?.Invoke();
            }
            else if (!objectiveAdded && addObjectiveOnEnter)
            {
                // Si l'objectif n'est pas encore ajouté, l'ajouter d'abord
                AddObjective();
                // Puis le compléter
                objectiveManager.CompleteObjective(objectiveTitle);
                objectiveCompleted = true;
                onObjectiveCompleted?.Invoke();
            }
        }
    }
    
    // Méthodes publiques pour déclencher les actions depuis d'autres scripts
    public void TriggerAddObjective()
    {
        AddObjective();
    }
    
    public void TriggerCompleteObjective()
    {
        CompleteObjective();
    }
}