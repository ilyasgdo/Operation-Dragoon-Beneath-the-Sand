using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class Objective
{
    public string title;
    public string description;
    public bool isCompleted;
    public bool isOptional;
    public int rewardPoints;

    public Objective(string title, string description, bool isOptional = false, int rewardPoints = 10)
    {
        this.title = title;
        this.description = description;
        this.isCompleted = false;
        this.isOptional = isOptional;
        this.rewardPoints = rewardPoints;
    }
}

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Objectives")]
    [SerializeField] private List<Objective> objectives = new List<Objective>();
    [SerializeField] private List<Objective> activeObjectives = new List<Objective>();
    [SerializeField] private List<Objective> completedObjectives = new List<Objective>();

    [Header("UI References")]
    [SerializeField] private GameObject objectiveUIPanel;
    [SerializeField] private GameObject objectivePrefab;
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private Text objectiveCountText;
    [SerializeField] private float displayDuration = 3f;

    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text notificationText;

    private int totalPoints = 0;
    private Dictionary<string, GameObject> objectiveUIElements = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (objectiveUIPanel != null)
        {
            objectiveUIPanel.SetActive(false);
        }

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }

        UpdateObjectiveUI();
    }

    private void Update()
    {
        // Toggle objectives panel with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleObjectivesPanel();
        }
    }

    public void AddObjective(string title, string description, bool isOptional = false, int rewardPoints = 10)
    {
        Objective newObjective = new Objective(title, description, isOptional, rewardPoints);
        objectives.Add(newObjective);
        activeObjectives.Add(newObjective);
        
        ShowNotification($"Nouvel objectif: {title}");
        UpdateObjectiveUI();
    }

    public void CompleteObjective(string title)
    {
        Objective objective = activeObjectives.Find(o => o.title == title);
        
        if (objective != null)
        {
            objective.isCompleted = true;
            activeObjectives.Remove(objective);
            completedObjectives.Add(objective);
            totalPoints += objective.rewardPoints;
            
            ShowNotification($"Objectif accompli: {title} (+{objective.rewardPoints} points)");
            UpdateObjectiveUI();
        }
    }

    public bool IsObjectiveCompleted(string title)
    {
        Objective objective = completedObjectives.Find(o => o.title == title);
        return objective != null;
    }

    public bool IsObjectiveActive(string title)
    {
        Objective objective = activeObjectives.Find(o => o.title == title);
        return objective != null;
    }

    public int GetTotalPoints()
    {
        return totalPoints;
    }
    
    public List<Objective> GetActiveObjectives()
    {
        return activeObjectives;
    }
    
    public List<Objective> GetCompletedObjectives()
    {
        return completedObjectives;
    }

    public void ToggleObjectivesPanel()
    {
        if (objectiveUIPanel != null)
        {
            objectiveUIPanel.SetActive(!objectiveUIPanel.activeSelf);
        }
    }

    private void UpdateObjectiveUI()
    {
        if (objectivesContainer == null || objectivePrefab == null)
            return;

        // Clear existing UI elements
        foreach (Transform child in objectivesContainer)
        {
            Destroy(child.gameObject);
        }
        objectiveUIElements.Clear();

        // Add active objectives to UI
        foreach (Objective objective in activeObjectives)
        {
            GameObject objectiveUI = Instantiate(objectivePrefab, objectivesContainer);
            Text[] texts = objectiveUI.GetComponentsInChildren<Text>();
            
            if (texts.Length >= 2)
            {
                texts[0].text = objective.title;
                texts[1].text = objective.description;
            }

            Toggle toggle = objectiveUI.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = objective.isCompleted;
                toggle.interactable = false; // User can't toggle objectives directly
            }

            objectiveUIElements.Add(objective.title, objectiveUI);
        }

        // Update objective count text
        if (objectiveCountText != null)
        {
            objectiveCountText.text = $"Objectifs: {activeObjectives.Count} actifs, {completedObjectives.Count} complétés";
        }
    }

    private void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay());
        }
    }

    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        notificationPanel.SetActive(false);
    }
}