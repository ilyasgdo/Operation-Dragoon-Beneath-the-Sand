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
    
    [Tooltip("Hauteur minimale de l'élément d'objectif")]
    public float hauteurMinimale = 300f; // Triplé (100f * 3)
    
    [Tooltip("Marge verticale supplémentaire entre les objectifs")]
    public float margeVerticale = 5000f; // Triplé (20f * 3)
    
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
            
            // Configurer le texte pour éviter les superpositions
            texteDescription.resizeTextForBestFit = false;
            texteDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            texteDescription.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Ajuster les marges du texte pour qu'il ne déborde pas
            RectTransform textRect = texteDescription.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, hauteurMinimale - 90f); // Triplé (30f * 3)
            }
            
            // Tripler la taille de police
            texteDescription.fontSize *= 3;
        }
        
        if (caseObjectif != null)
        {
            caseObjectif.isOn = estComplete;
            caseObjectif.interactable = false; // Le joueur ne peut pas modifier directement
            
            // Tripler la taille de la case à cocher
            RectTransform toggleRect = caseObjectif.GetComponent<RectTransform>();
            if (toggleRect != null)
            {
                toggleRect.sizeDelta = new Vector2(toggleRect.sizeDelta.x * 3, toggleRect.sizeDelta.y * 3);
            }
        }
        
        // S'assurer que le RectTransform est correctement configuré
        AjusterTailleRectTransform();
    }
    
    // Ajuster la taille du RectTransform en fonction du contenu
    private void AjusterTailleRectTransform()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Hauteur augmentée pour éviter la superposition
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, hauteurMinimale + margeVerticale);
            
            // S'assurer que l'élément s'étire horizontalement
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            
            // Force le rafraîchissement du layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
    
    // Fonction appelée à chaque frame après que tous les autres composants aient été mis à jour
    private void LateUpdate()
    {
        // Vérifier si la taille doit être réajustée
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.sizeDelta.y < hauteurMinimale)
        {
            AjusterTailleRectTransform();
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
    
    // Mettre à jour le texte de l'objectif
    public void MettreAJourTexte(string nouveauTexte)
    {
        if (texteDescription != null)
        {
            texteDescription.text = nouveauTexte;
            
            // Force le rafraîchissement du layout après changement de texte
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }
    }
}