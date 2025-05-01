using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script pour l'élément d'objectif individuel dans l'interface utilisateur.
/// Ce script est attaché au préfab d'élément d'objectif.
/// </summary>
public class ObjectiveItem : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Toggle completionToggle;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.4f, 0.2f, 0.8f);
    [SerializeField] private Color optionalColor = new Color(0.4f, 0.4f, 0.2f, 0.8f);
    
    private Objective objective;
    
    public void SetObjective(Objective objective)
    {
        this.objective = objective;
        
        if (titleText != null)
            titleText.text = objective.title;
            
        if (descriptionText != null)
            descriptionText.text = objective.description;
            
        if (completionToggle != null)
        {
            completionToggle.isOn = objective.isCompleted;
            completionToggle.interactable = false; // L'utilisateur ne peut pas modifier directement
        }
        
        UpdateAppearance();
    }
    
    private void UpdateAppearance()
    {
        if (background != null && objective != null)
        {
            if (objective.isCompleted)
            {
                background.color = completedColor;
            }
            else if (objective.isOptional)
            {
                background.color = optionalColor;
            }
            else
            {
                background.color = normalColor;
            }
        }
    }
}