using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Configuration de la porte")]
    public float openAngle = 90f;
    public float rotationSpeed = 2f;
    public Vector3 rotationAxis = Vector3.up;
    public float raycastDistance = 3f;
    public Transform doorPivot; // Point de pivot de la porte
    
    [Header("Configuration Audio")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public AudioClip doorLockedSound; // Son quand la porte est verrouillée
    public AudioClip doorUnlockSound; // Son quand la porte est déverrouillée
    public AudioClip codeErrorSound; // Son quand le code est incorrect
    [Range(0f, 1f)]
    public float volume = 1.0f;
    
    [Header("Système de Verrouillage")]
    public bool requiresCode = true; // Si la porte nécessite un code pour s'ouvrir
    public string correctCode = "19391945"; // Le code correct par défaut
    public bool isLocked = true; // Si la porte est verrouillée
    private string currentInputCode = ""; // Le code actuellement saisi
    public int maxCodeLength = 8; // Longueur maximale du code
    public float resetCodeTime = 5f; // Temps avant réinitialisation du code saisi
    private float lastInputTime; // Dernière fois qu'un chiffre a été saisi
    
    [Header("Débogage")]
    public bool showDebugRay = true;
    public LayerMask raycastLayers = -1; // Tous les layers par défaut
    
    private bool isOpen = false;
    private bool isRotating = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    
    // Style pour l'interface utilisateur
    private GUIStyle codeStyle;
    private GUIStyle statusStyle;
    
    void Start()
    {
        // Si aucun pivot n'est spécifié, utiliser cet objet
        if (doorPivot == null)
        {
            doorPivot = transform;
        }
        
        initialRotation = doorPivot.rotation;
        targetRotation = Quaternion.Euler(rotationAxis * openAngle) * initialRotation;
        
        Debug.Log("Porte initialisée. Rotation initiale: " + initialRotation.eulerAngles);
        Debug.Log("Rotation cible: " + targetRotation.eulerAngles);
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f;
                audioSource.volume = volume;
            }
        }
        
        // Vérifier si la porte a un collider
        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider == null)
        {
            Debug.LogWarning("Attention: La porte n'a pas de collider! Ajoutez un Box Collider.");
        }
        
        // Initialiser les styles pour l'interface utilisateur
        codeStyle = new GUIStyle();
        codeStyle.fontSize = 24;
        codeStyle.normal.textColor = Color.white;
        codeStyle.alignment = TextAnchor.MiddleCenter;
        
        statusStyle = new GUIStyle();
        statusStyle.fontSize = 18;
        statusStyle.normal.textColor = Color.yellow;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        
        // Initialiser le temps de dernière saisie
        lastInputTime = Time.time;
    }
    
    void Update()
    {
        // Vérifier l'interaction avec la touche F
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Touche F appuyée");
            TryToggleDoor();
        }
        
        // Vérifier les entrées numériques pour le code
        CheckNumericInput();
        
        // Vérifier si le temps de réinitialisation du code est écoulé
        if (currentInputCode.Length > 0 && Time.time - lastInputTime > resetCodeTime)
        {
            ResetCode();
        }
        
        // Animer la rotation de la porte
        if (isRotating)
        {
            AnimateDoor();
        }
        
        // Afficher le rayon de débogage
        if (showDebugRay && Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red);
        }
    }
    
    // Afficher l'interface utilisateur pour le code
    void OnGUI()
    {
        // Ne rien afficher si la porte n'est pas verrouillée ou ne nécessite pas de code
        if (!requiresCode || !isLocked)
            return;
            
        // Vérifier si le joueur est assez proche de la porte
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, raycastLayers))
            {
                // Vérifier si c'est cette porte ou un de ses enfants
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                    (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
                {
                    // Afficher le code en cours de saisie
                    string displayCode = currentInputCode;
                    while (displayCode.Length < maxCodeLength)
                        displayCode += "_";
                        
                    GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 50), 
                        "CODE: " + displayCode, codeStyle);
                        
                    // Afficher les instructions
                    GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2, 300, 30), 
                        "Entrez le code avec les touches numériques", statusStyle);
                }
            }
        }
    }
    
    void TryToggleDoor()
    {
        // Obtenir la caméra principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Pas de caméra principale trouvée!");
            return;
        }
        
        // Créer un rayon depuis le centre de la caméra
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        // Vérifier si le rayon touche quelque chose
        if (Physics.Raycast(ray, out hit, raycastDistance, raycastLayers))
        {
            Debug.Log("Rayon a touché: " + hit.transform.name + " à distance: " + hit.distance);
            
            // Vérifier si c'est cette porte ou un de ses enfants
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
            {
                Debug.Log("Porte détectée!");
                
                // Vérifier si la porte nécessite un code et si elle est verrouillée
                if (requiresCode && isLocked)
                {
                    Debug.Log("La porte est verrouillée. Code actuel: " + currentInputCode);
                    // Jouer le son de porte verrouillée
                    if (audioSource != null && doorLockedSound != null)
                    {
                        audioSource.clip = doorLockedSound;
                        audioSource.Play();
                    }
                }
                else
                {
                    // Si la porte n'est pas verrouillée ou ne nécessite pas de code, l'ouvrir
                    ToggleDoor();
                }
            }
        }
        else
        {
            Debug.Log("Rayon n'a rien touché dans la distance " + raycastDistance);
        }
    }
    
    // Vérifie les entrées numériques pour le code
    void CheckNumericInput()
    {
        // Vérifier les touches numériques (0-9)
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                AddDigitToCode(i.ToString());
            }
        }
        
        // Vérifier la touche Entrée pour valider le code
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ValidateCode();
        }
        
        // Vérifier la touche Échap ou Retour pour effacer le code
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetCode();
        }
    }
    
    // Ajoute un chiffre au code actuel
    void AddDigitToCode(string digit)
    {
        if (currentInputCode.Length < maxCodeLength)
        {
            currentInputCode += digit;
            lastInputTime = Time.time;
            Debug.Log("Code actuel: " + currentInputCode);
            
            // Si le code a atteint la longueur maximale, le valider automatiquement
            if (currentInputCode.Length == maxCodeLength)
            {
                ValidateCode();
            }
        }
    }
    
    // Valide le code entré
    void ValidateCode()
    {
        if (currentInputCode == correctCode)
        {
            Debug.Log("Code correct! Déverrouillage de la porte.");
            isLocked = false;
            
            // Jouer le son de déverrouillage
            if (audioSource != null && doorUnlockSound != null)
            {
                audioSource.clip = doorUnlockSound;
                audioSource.Play();
            }
            
            // Ouvrir la porte automatiquement après déverrouillage
            ToggleDoor();
        }
        else
        {
            Debug.Log("Code incorrect! La porte reste verrouillée.");
            
            // Jouer le son d'erreur de code
            if (audioSource != null && codeErrorSound != null)
            {
                audioSource.clip = codeErrorSound;
                audioSource.Play();
            }
        }
        
        // Réinitialiser le code après validation
        ResetCode();
    }
    
    // Réinitialise le code actuel
    void ResetCode()
    {
        currentInputCode = "";
        Debug.Log("Code réinitialisé.");
    }
    
    // Définit un nouveau code pour la porte
    public void SetDoorCode(string newCode)
    {
        correctCode = newCode;
        Debug.Log("Nouveau code défini: " + newCode);
    }
    
    void ToggleDoor()
    {
        isOpen = !isOpen;
        isRotating = true;
        
        Debug.Log("Porte en train de " + (isOpen ? "s'ouvrir" : "se fermer"));
        
        // Jouer le son approprié
        if (audioSource != null)
        {
            if (isOpen && doorOpenSound != null)
            {
                audioSource.clip = doorOpenSound;
                audioSource.Play();
            }
            else if (!isOpen && doorCloseSound != null)
            {
                audioSource.clip = doorCloseSound;
                audioSource.Play();
            }
        }
    }
    
    void AnimateDoor()
    {
        // Déterminer la rotation cible
        Quaternion targetRot = isOpen ? targetRotation : initialRotation;
        
        // Effectuer la rotation progressive
        doorPivot.rotation = Quaternion.Slerp(doorPivot.rotation, targetRot, Time.deltaTime * rotationSpeed);
        
        // Vérifier si la rotation est terminée
        if (Quaternion.Angle(doorPivot.rotation, targetRot) < 0.1f)
        {
            doorPivot.rotation = targetRot;
            isRotating = false;
            Debug.Log("Animation de porte terminée");
        }
    }
}