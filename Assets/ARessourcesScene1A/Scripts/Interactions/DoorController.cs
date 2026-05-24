using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    
    [Header("Système d'Objectifs")]
    public string doorId = "porte_principale"; // Identifiant unique de la porte
    public SystemeObjectifs systemeObjectifs; // Référence au système d'objectifs
    public bool estObjectif = false; // Si trouver le code de cette porte est un objectif
    private bool objectifComplete = false; // Si l'objectif a été complété
    
    [Header("Débogage")]
    public bool showDebugRay = true;
    public LayerMask raycastLayers = -1; // Tous les layers par défaut
    
    [Header("VR Physics")]
    public bool usePhysicsInVR = true;
    private Rigidbody rb;
    private HingeJoint hinge;
    private XRBaseInteractable xrInteractable;

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
        
        SetupVRInteraction();

        // Initialiser les styles pour l'interface utilisateur (Desktop only)
        codeStyle = new GUIStyle();
        codeStyle.fontSize = 24;
        codeStyle.normal.textColor = Color.white;
        codeStyle.alignment = TextAnchor.MiddleCenter;
        
        statusStyle = new GUIStyle();
        statusStyle.fontSize = 18;
        statusStyle.normal.textColor = Color.yellow;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        
        lastInputTime = Time.time;
        
        if (estObjectif && systemeObjectifs != null)
        {
            string idObjectif = "trouver_code_" + doorId;
            systemeObjectifs.AjouterObjectif(idObjectif, "Trouver le code de la porte: " + doorId);
        }
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Door " + gameObject.name + " is being DESTROYED!");
        }
    }

    void SetupVRInteraction()
    {
        // Clean up old interactables to avoid conflicts
        var oldSimple = GetComponent<XRSimpleInteractable>();
        var oldGrab = GetComponent<XRGrabInteractable>();

        if (usePhysicsInVR)
        {
            if (oldSimple != null && Application.isPlaying) Destroy(oldSimple);
            
            var grab = oldGrab;
            if (grab == null) grab = gameObject.AddComponent<XRGrabInteractable>();
            
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            xrInteractable = grab;

            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = isLocked;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 1f;
            rb.angularDamping = 5f;

            hinge = GetComponent<HingeJoint>();
            if (hinge == null) hinge = gameObject.AddComponent<HingeJoint>();
            
            Vector3 anchorPos = transform.InverseTransformPoint(doorPivot.position);
            if (anchorPos.magnitude < 0.1f)
            {
                var filter = GetComponentInChildren<MeshFilter>();
                if (filter != null)
                {
                    var bounds = filter.sharedMesh.bounds;
                    anchorPos = new Vector3(bounds.min.x, 0, 0);
                }
            }

            hinge.anchor = anchorPos;
            hinge.axis = Vector3.up; 
            hinge.useLimits = true;
            JointLimits limits = hinge.limits;
            limits.min = 0;
            limits.max = openAngle;
            hinge.limits = limits;
        }
        else
        {
            if (oldGrab != null && Application.isPlaying) Destroy(oldGrab);
            
            var simple = oldSimple;
            if (simple == null) simple = gameObject.AddComponent<XRSimpleInteractable>();
            xrInteractable = simple;
        }

        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
            xrInteractable.hoverEntered.AddListener(OnXRHover);
        }
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        HandleVRInteraction();
    }

    private void OnXRHover(HoverEnterEventArgs args)
    {
        // Only trigger by hand (direct interactor) touching, not ray
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor)
        {
            HandleVRInteraction();
        }
    }

    private void HandleVRInteraction()
    {
        if (requiresCode && isLocked)
        {
            if (audioSource != null && doorLockedSound != null)
            {
                audioSource.PlayOneShot(doorLockedSound);
            }
        }
        else if (!usePhysicsInVR)
        {
            ToggleDoor();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryToggleDoor();
        }
        
        CheckNumericInput();
        
        if (currentInputCode.Length > 0 && Time.time - lastInputTime > resetCodeTime)
        {
            ResetCode();
        }
        
        if (isRotating && !usePhysicsInVR)
        {
            AnimateDoor();
        }
    }

    // New method for VR interactions (e.g. from a keypad)
    public void AddDigit(int digit)
    {
        AddDigitToCode(digit.ToString());
    }
    
    void OnGUI()
    {
        if (Application.isBatchMode) return; // Skip in headless/editor tools
        if (!requiresCode || !isLocked)
            return;
            
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, raycastLayers))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                    (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
                {
                    string displayCode = currentInputCode;
                    while (displayCode.Length < maxCodeLength)
                        displayCode += "_";
                        
                    GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 50), 
                        "CODE: " + displayCode, codeStyle);
                }
            }
        }
    }
    
    void TryToggleDoor()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, raycastDistance, raycastLayers))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
            {
                if (requiresCode && isLocked)
                {
                    if (audioSource != null && doorLockedSound != null)
                    {
                        audioSource.PlayOneShot(doorLockedSound);
                    }
                }
                else
                {
                    ToggleDoor();
                }
            }
        }
    }
    
    void CheckNumericInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                AddDigitToCode(i.ToString());
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ValidateCode();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetCode();
        }
    }
    
    void AddDigitToCode(string digit)
    {
        if (currentInputCode.Length < maxCodeLength)
        {
            currentInputCode += digit;
            lastInputTime = Time.time;
            
            if (currentInputCode.Length == maxCodeLength)
            {
                ValidateCode();
            }
        }
    }
    
    void ValidateCode()
    {
        if (currentInputCode == correctCode)
        {
            isLocked = false;
            if (rb != null) rb.isKinematic = false;

            if (audioSource != null && doorUnlockSound != null)
            {
                audioSource.PlayOneShot(doorUnlockSound);
            }
            
            if (estObjectif && !objectifComplete && systemeObjectifs != null)
            {
                string idObjectif = "trouver_code_" + doorId;
                systemeObjectifs.CompleterObjectif(idObjectif);
                objectifComplete = true;
            }
            
            if (!usePhysicsInVR) ToggleDoor();
        }
        else
        {
            if (audioSource != null && codeErrorSound != null)
            {
                audioSource.PlayOneShot(codeErrorSound);
            }
        }
        currentInputCode = "";
    }
    
    void ResetCode()
    {
        currentInputCode = "";
    }
    
    public void SetDoorCode(string newCode)
    {
        correctCode = newCode;
    }
    
    void ToggleDoor()
    {
        isOpen = !isOpen;
        isRotating = true;
        
        if (audioSource != null)
        {
            if (isOpen && doorOpenSound != null) audioSource.PlayOneShot(doorOpenSound);
            else if (!isOpen && doorCloseSound != null) audioSource.PlayOneShot(doorCloseSound);
        }
    }
    
    void AnimateDoor()
    {
        Quaternion targetRot = isOpen ? targetRotation : initialRotation;
        doorPivot.rotation = Quaternion.Slerp(doorPivot.rotation, targetRot, Time.deltaTime * rotationSpeed);
        
        if (Quaternion.Angle(doorPivot.rotation, targetRot) < 0.1f)
        {
            doorPivot.rotation = targetRot;
            isRotating = false;
        }
    }
}