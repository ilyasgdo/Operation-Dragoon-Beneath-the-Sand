using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script d'intégration pour connecter le système d'objectifs au contrôleur de personnage.
/// Attachez ce script au même GameObject que le FirstPersonController.
/// </summary>
public class ObjectiveIntegration : MonoBehaviour
{
    [Header("Zones d'Objectifs")]
    [SerializeField] private List<ObjectiveZone> objectiveZones = new List<ObjectiveZone>();
    
    private FirstPersonController playerController;
    private ObjectiveManager objectiveManager;
    
    [System.Serializable]
    public class ObjectiveZone
    {
        public string zoneName;
        public Collider triggerZone;
        public string objectiveTitle;
        public string objectiveDescription;
        public bool isOptional = false;
        public int rewardPoints = 10;
        public bool isCompleted = false;
    }
    
    private void Start()
    {
        // Obtenir les références nécessaires
        playerController = GetComponent<FirstPersonController>();
        objectiveManager = ObjectiveManager.Instance;
        
        if (objectiveManager == null)
        {
            Debug.LogWarning("ObjectiveManager non trouvé! Assurez-vous qu'il existe dans la scène.");
        }
        
        // Ajouter un objectif initial si nécessaire
        if (objectiveManager != null && objectiveZones.Count > 0)
        {
            ObjectiveZone initialZone = objectiveZones[0];
            objectiveManager.AddObjective(
                initialZone.objectiveTitle,
                initialZone.objectiveDescription,
                initialZone.isOptional,
                initialZone.rewardPoints
            );
        }
    }
    
    private void Update()
    {
        if (objectiveManager == null) return;
        
        // Vérifier si le joueur est dans une zone d'objectif
        foreach (ObjectiveZone zone in objectiveZones)
        {
            if (zone.isCompleted || zone.triggerZone == null) continue;
            
            // Vérifier si l'objectif est actif
            if (!objectiveManager.IsObjectiveActive(zone.objectiveTitle) && 
                !objectiveManager.IsObjectiveCompleted(zone.objectiveTitle))
            {
                // Ajouter l'objectif s'il n'est pas déjà actif ou complété
                objectiveManager.AddObjective(
                    zone.objectiveTitle,
                    zone.objectiveDescription,
                    zone.isOptional,
                    zone.rewardPoints
                );
            }
            
            // Vérifier si le joueur est dans la zone de déclenchement
            if (zone.triggerZone.bounds.Contains(transform.position))
            {
                // Compléter l'objectif
                objectiveManager.CompleteObjective(zone.objectiveTitle);
                zone.isCompleted = true;
            }
        }
    }
    
    // Méthode pour ajouter un objectif manuellement (peut être appelée par d'autres scripts)
    public void AddObjective(string title, string description, bool isOptional = false, int rewardPoints = 10)
    {
        if (objectiveManager != null)
        {
            objectiveManager.AddObjective(title, description, isOptional, rewardPoints);
        }
    }
    
    // Méthode pour compléter un objectif manuellement (peut être appelée par d'autres scripts)
    public void CompleteObjective(string title)
    {
        if (objectiveManager != null)
        {
            objectiveManager.CompleteObjective(title);
            
            // Mettre à jour l'état de la zone d'objectif correspondante
            ObjectiveZone zone = objectiveZones.Find(z => z.objectiveTitle == title);
            if (zone != null)
            {
                zone.isCompleted = true;
            }
        }
    }
}