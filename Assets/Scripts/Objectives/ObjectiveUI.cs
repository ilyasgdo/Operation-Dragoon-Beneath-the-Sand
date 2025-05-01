using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private GameObject objectiveItemPrefab;
    [SerializeField] private Text objectiveCountText;
    
    [Header("Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    private ObjectiveManager objectiveManager;
    private Coroutine hideNotificationCoroutine;
    
    private void Start()
    {
        objectiveManager = ObjectiveManager.Instance;
        
        if (objectiveManager == null)
        {
            Debug.LogError("ObjectiveManager non trouvé! Assurez-vous qu'il existe dans la scène.");
        }
        
        // Cacher les panneaux au démarrage
        if (objectivePanel != null)
            objectivePanel.SetActive(false);
            
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    private void Update()
    {
        // Afficher/cacher le panneau d'objectifs avec la touche Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleObjectivePanel();
        }
    }
    
    public void ToggleObjectivePanel()
    {
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(!objectivePanel.activeSelf);
            
            // Mettre à jour l'interface si le panneau est ouvert
            if (objectivePanel.activeSelf)
            {
                UpdateObjectiveList();
            }
        }
    }
    
    public void UpdateObjectiveList()
    {
        if (objectiveManager == null || objectivesContainer == null || objectiveItemPrefab == null)
            return;
            
        // Effacer les éléments existants
        foreach (Transform child in objectivesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Obtenir les objectifs actifs depuis l'ObjectiveManager
        List<Objective> activeObjectives = objectiveManager.GetActiveObjectives();
        
        // Créer un élément UI pour chaque objectif actif
        foreach (Objective objective in activeObjectives)
        {
            GameObject objectiveItem = Instantiate(objectiveItemPrefab, objectivesContainer);
            
            // Configurer l'élément d'objectif
            Text[] texts = objectiveItem.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = objective.title;
                texts[1].text = objective.description;
            }
            
            // Configurer la case à cocher (si présente)
            Toggle toggle = objectiveItem.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = objective.isCompleted;
                toggle.interactable = false; // L'utilisateur ne peut pas cocher directement
            }
        }
        
        // Mettre à jour le texte du compteur d'objectifs
        if (objectiveCountText != null)
        {
            int completedCount = objectiveManager.GetCompletedObjectives().Count;
            int totalCount = activeObjectives.Count + completedCount;
            objectiveCountText.text = $"Objectifs: {activeObjectives.Count} actifs, {completedCount} complétés";
        }
    }
    
    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            // Définir le message
            notificationText.text = message;
            
            // Afficher le panneau
            notificationPanel.SetActive(true);
            
            // Arrêter la coroutine précédente si elle existe
            if (hideNotificationCoroutine != null)
            {
                StopCoroutine(hideNotificationCoroutine);
            }
            
            // Démarrer une nouvelle coroutine pour cacher la notification
            hideNotificationCoroutine = StartCoroutine(HideNotificationAfterDelay());
        }
    }
    
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        notificationPanel.SetActive(false);
        hideNotificationCoroutine = null;
    }
}