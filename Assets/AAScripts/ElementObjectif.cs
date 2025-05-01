using UnityEngine;
using UnityEngine.UI;

public class ElementObjectif : MonoBehaviour
{
    [Tooltip("Référence au texte de description de l'objectif")]
    public Text texteDescription;
    
    [Tooltip("Référence à la case à cocher de l'objectif")]
    public Toggle caseObjectif;
    
    [Tooltip("Couleur du texte pour les objectifs non-complétés")]
    public Color couleurNonComplete = Color.white;
    
    [Tooltip("Couleur du texte pour les objectifs complétés")]
    public Color couleurCompletee = Color.green;
    
    // ID de l'objectif associé à cet élément
    private string objectifId;
    
    // Initialiser l'élément avec les données de l'objectif
    public void Initialiser(string id, string description, bool estOptionnel, bool estComplete)
    {
        objectifId = id;
        
        if (texteDescription != null)
        {
            string prefixe = estOptionnel ? "[Optionnel] " : "";
            texteDescription.text = prefixe + description;
            texteDescription.color = estComplete ? couleurCompletee : couleurNonComplete;
        }
        
        if (caseObjectif != null)
        {
            caseObjectif.isOn = estComplete;
            caseObjectif.interactable = false; // Le joueur ne peut pas modifier directement
        }
    }
    
    // Marquer l'objectif comme complété
    public void MarquerComplete(bool estComplete)
    {
        if (caseObjectif != null)
        {
            caseObjectif.isOn = estComplete;
        }
        
        if (texteDescription != null)
        {
            texteDescription.color = estComplete ? couleurCompletee : couleurNonComplete;
        }
    }
    
    // Récupérer l'ID de l'objectif associé
    public string ObtenirId()
    {
        return objectifId;
    }
} 