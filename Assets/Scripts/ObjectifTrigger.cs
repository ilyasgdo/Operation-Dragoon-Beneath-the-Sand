using UnityEngine;
using UnityEngine.Events;

public class ObjectifTrigger : MonoBehaviour
{
    [Tooltip("ID de l'objectif à compléter")]
    public string objectifId;
    
    [Tooltip("Le tag du joueur pour détecter la collision")]
    public string playerTag = "Player";
    
    [Header("Type de déclenchement")]
    [Tooltip("Se déclenche quand le joueur entre dans la zone")]
    public bool declencherSurEntree = true;
    
    [Tooltip("Se déclenche quand le joueur appuie sur une touche dans la zone")]
    public bool declencherSurTouche = false;
    
    [Tooltip("La touche à appuyer pour déclencher l'objectif")]
    public KeyCode toucheDeclenchement = KeyCode.F;
    
    [Tooltip("Message à afficher quand le joueur peut interagir")]
    public string messageInteraction = "Appuyez sur F pour interagir";
    
    [Header("Actions supplémentaires")]
    [Tooltip("Événement personnalisé à déclencher en plus de compléter l'objectif")]
    public UnityEvent actionsSurCompletion;
    
    // Référence au gestionnaire d'objectifs
    private ObjectifManager objectifManager;
    
    // État d'interaction
    private bool joueurDansZone = false;
    private bool objectifComplete = false;
    
    // Style pour l'affichage du message d'interaction
    private GUIStyle styleMessage;
    
    private void Start()
    {
        // Trouver le gestionnaire d'objectifs dans la scène
        objectifManager = FindObjectOfType<ObjectifManager>();
        
        if (objectifManager == null)
        {
            Debug.LogWarning("Aucun ObjectifManager trouvé dans la scène. Le script " + 
                             "ObjectifTrigger ne pourra pas compléter d'objectifs.");
        }
        
        // Initialiser le style du message
        styleMessage = new GUIStyle();
        styleMessage.fontSize = 20;
        styleMessage.normal.textColor = Color.white;
        styleMessage.alignment = TextAnchor.MiddleCenter;
        styleMessage.fontStyle = FontStyle.Bold;
        
        // Ajouter un Collider si nécessaire
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("Aucun Collider trouvé sur " + gameObject.name + 
                             ". Ajoutez un Collider et activez 'Is Trigger'.");
        }
    }
    
    private void Update()
    {
        // Gérer l'interaction par touche
        if (joueurDansZone && declencherSurTouche && !objectifComplete)
        {
            if (Input.GetKeyDown(toucheDeclenchement))
            {
                CompleterObjectif();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur qui entre dans la zone
        if (other.CompareTag(playerTag))
        {
            joueurDansZone = true;
            
            // Si l'objectif doit être complété à l'entrée dans la zone
            if (declencherSurEntree && !objectifComplete)
            {
                CompleterObjectif();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur qui sort de la zone
        if (other.CompareTag(playerTag))
        {
            joueurDansZone = false;
        }
    }
    
    private void CompleterObjectif()
    {
        if (objectifManager != null && !string.IsNullOrEmpty(objectifId) && !objectifComplete)
        {
            objectifManager.CompleterObjectif(objectifId);
            objectifComplete = true;
            
            // Déclencher les actions supplémentaires
            actionsSurCompletion?.Invoke();
            
            Debug.Log("Objectif " + objectifId + " complété par le trigger " + gameObject.name);
        }
    }
    
    // Afficher le message d'interaction si nécessaire
    private void OnGUI()
    {
        if (joueurDansZone && declencherSurTouche && !objectifComplete)
        {
            GUI.Label(
                new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 50),
                messageInteraction,
                styleMessage
            );
        }
    }
} 