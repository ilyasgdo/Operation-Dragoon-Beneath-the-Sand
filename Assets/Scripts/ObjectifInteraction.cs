using UnityEngine;
using UnityEngine.Events;

public class ObjectifInteraction : MonoBehaviour
{
    [Tooltip("ID de l'objectif à compléter")]
    public string objectifId;
    
    [Header("Type d'interaction")]
    [Tooltip("Référence à un TableauInteractif si cette interaction est liée à la lecture d'un tableau")]
    public TableauInteractif tableauInteractif;
    
    [Tooltip("Référence à une FeuilleInteractive si cette interaction est liée à la lecture d'une feuille")]
    public FeuilleInteractive feuilleInteractive;
    
    [Tooltip("Référence à un DoorController si cette interaction est liée à l'ouverture d'une porte")]
    public DoorController doorController;
    
    [Header("Actions supplémentaires")]
    [Tooltip("Événement personnalisé à déclencher en plus de compléter l'objectif")]
    public UnityEvent actionsSurCompletion;
    
    // Référence au gestionnaire d'objectifs
    private ObjectifManager objectifManager;
    
    // État de l'objectif
    private bool objectifComplete = false;
    
    private void Start()
    {
        // Trouver le gestionnaire d'objectifs dans la scène
        objectifManager = FindObjectOfType<ObjectifManager>();
        
        if (objectifManager == null)
        {
            Debug.LogWarning("Aucun ObjectifManager trouvé dans la scène. Le script " + 
                            "ObjectifInteraction ne pourra pas compléter d'objectifs.");
            return;
        }
        
        // Configurer les écouteurs d'événements selon le type d'interaction
        if (tableauInteractif != null)
        {
            // Utiliser un MonoBehaviour pour s'abonner à l'événement Update
            StartCoroutine(DetecterInteractionTableau());
        }
        
        if (feuilleInteractive != null)
        {
            // Utiliser un MonoBehaviour pour s'abonner à l'événement Update
            StartCoroutine(DetecterInteractionFeuille());
        }
        
        if (doorController != null)
        {
            // Utiliser un MonoBehaviour pour s'abonner à l'événement Update
            StartCoroutine(DetecterInteractionPorte());
        }
    }
    
    // Coroutine pour détecter l'interaction avec un tableau
    private System.Collections.IEnumerator DetecterInteractionTableau()
    {
        bool interactionDetectee = false;
        
        while (!objectifComplete)
        {
            // Détecter si le joueur consulte ou a consulté le tableau
            if (tableauInteractif != null)
            {
                // Si le joueur est en interaction avec le tableau
                if (!interactionDetectee && tableauInteractif.GetComponent<AudioSource>().isPlaying)
                {
                    interactionDetectee = true;
                }
                
                // Si l'interaction est terminée, compléter l'objectif
                if (interactionDetectee && !tableauInteractif.GetComponent<AudioSource>().isPlaying)
                {
                    CompleterObjectif();
                    yield break;
                }
            }
            
            yield return null;
        }
    }
    
    // Coroutine pour détecter l'interaction avec une feuille
    private System.Collections.IEnumerator DetecterInteractionFeuille()
    {
        bool interactionDetectee = false;
        
        while (!objectifComplete)
        {
            // Vérifier si le joueur consulte la feuille
            if (feuilleInteractive != null)
            {
                // Comme FeuilleInteractive n'expose pas directement son état, on doit trouver un moyen indirect
                // Vérifier si la feuille a été déplacée de sa position d'origine
                Vector3 positionOriginale = (Vector3)feuilleInteractive.GetType().GetField("positionOriginale", 
                                                System.Reflection.BindingFlags.NonPublic | 
                                                System.Reflection.BindingFlags.Instance).GetValue(feuilleInteractive);
                
                if (!interactionDetectee && Vector3.Distance(feuilleInteractive.transform.position, positionOriginale) > 0.1f)
                {
                    interactionDetectee = true;
                }
                
                // Si l'interaction est terminée (la feuille est revenue à sa position)
                if (interactionDetectee && Vector3.Distance(feuilleInteractive.transform.position, positionOriginale) < 0.1f)
                {
                    CompleterObjectif();
                    yield break;
                }
            }
            
            yield return null;
        }
    }
    
    // Coroutine pour détecter l'interaction avec une porte
    private System.Collections.IEnumerator DetecterInteractionPorte()
    {
        // Au lieu d'accéder directement à isOpen qui est privé, on va surveiller 
        // les changements de rotation de la porte
        Quaternion rotationInitiale = doorController.transform.rotation;
        bool porteInteraction = false;
        
        while (!objectifComplete)
        {
            // Vérifier si la rotation de la porte a changé, ce qui indique qu'elle a été ouverte/fermée
            if (doorController != null)
            {
                // On vérifie si la rotation a significativement changé
                if (!porteInteraction && Quaternion.Angle(rotationInitiale, doorController.transform.rotation) > 5.0f)
                {
                    porteInteraction = true;
                    CompleterObjectif();
                    yield break;
                }
            }
            
            yield return null;
        }
    }
    
    // Compléter l'objectif et déclencher les actions associées
    private void CompleterObjectif()
    {
        if (objectifManager != null && !string.IsNullOrEmpty(objectifId) && !objectifComplete)
        {
            objectifManager.CompleterObjectif(objectifId);
            objectifComplete = true;
            
            // Déclencher les actions supplémentaires
            actionsSurCompletion?.Invoke();
            
            Debug.Log("Objectif " + objectifId + " complété par l'interaction " + gameObject.name);
        }
    }
} 