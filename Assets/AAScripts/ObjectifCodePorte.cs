using UnityEngine;

/// <summary>
/// Exemple de script pour gérer un objectif de type "trouver le code d'une porte"
/// Ce script peut être attaché à un objet qui contient un indice pour le code
/// </summary>
public class ObjectifCodePorte : MonoBehaviour
{
    [Tooltip("Référence au système d'objectifs")]
    public SystemeObjectifs systemeObjectifs;
    
    [Tooltip("ID de la porte concernée")]
    public string doorId = "porte_principale";
    
    [Tooltip("Le code de la porte (ou une partie)")]
    public string codeIndice = "1939";
    
    [Tooltip("Description de l'indice à afficher")]
    public string descriptionIndice = "J'ai trouvé une partie du code: ";
    
    [Tooltip("Distance maximale d'interaction")]
    public float distanceInteraction = 2f;
    
    [Tooltip("Couches pour le raycast")]
    public LayerMask interactionLayers = -1;
    
    [Tooltip("Son joué quand l'indice est trouvé")]
    public AudioClip sonIndice;
    
    private bool indiceDecouvert = false;
    private AudioSource audioSource;
    
    void Start()
    {
        // Récupérer ou ajouter un AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && sonIndice != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }
        
        // Vérifier que l'objet a un collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("L'objet " + gameObject.name + " n'a pas de collider. L'interaction ne fonctionnera pas correctement.");
        }
    }
    
    void Update()
    {
        // Ne rien faire si l'indice a déjà été découvert
        if (indiceDecouvert)
            return;
            
        // Détecter l'interaction (touche E par défaut)
        if (Input.GetKeyDown(KeyCode.E))
        {
            VerifierInteraction();
        }
    }
    
    void VerifierInteraction()
    {
        // Vérifier si le joueur regarde cet objet
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
            
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distanceInteraction, interactionLayers))
        {
            // Vérifier si c'est cet objet ou un de ses enfants
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                DecouvrirIndice();
            }
        }
    }
    
    void DecouvrirIndice()
    {
        if (indiceDecouvert)
            return;
            
        indiceDecouvert = true;
        
        // Jouer le son de découverte
        if (audioSource != null && sonIndice != null)
        {
            audioSource.PlayOneShot(sonIndice);
        }
        
        // Afficher un message à l'écran
        Debug.Log(descriptionIndice + codeIndice);
        
        // Si le système d'objectifs est défini, lui notifier la découverte
        if (systemeObjectifs != null)
        {
            // Vous pourriez créer un objectif spécifique pour cet indice
            string idObjectifIndice = "indice_code_" + doorId + "_" + codeIndice;
            systemeObjectifs.AjouterObjectif(idObjectifIndice, descriptionIndice + codeIndice, true);
            systemeObjectifs.CompleterObjectif(idObjectifIndice);
            
            // Vous pourriez aussi simplifier en mettant à jour un objectif existant
            // Par exemple, en incrémentant un compteur d'indices trouvés
        }
    }
    
    // Afficher un message quand le joueur est proche
    void OnGUI()
    {
        if (indiceDecouvert)
            return;
            
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
            
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distanceInteraction, interactionLayers))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                // Créer un style pour le texte
                GUIStyle style = new GUIStyle();
                style.fontSize = 16;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                
                // Afficher le message d'interaction
                GUI.Label(new Rect(Screen.width/2 - 100, Screen.height/2 + 50, 200, 30), 
                    "Appuyez sur E pour examiner", style);
            }
        }
    }
} 