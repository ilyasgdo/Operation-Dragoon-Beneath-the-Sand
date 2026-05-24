using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
    public AudioClip doorLockedSound;
    public AudioClip doorUnlockSound;
    public AudioClip codeErrorSound;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    
    [Header("Système de Verrouillage")]
    public bool requiresCode = true;
    public string correctCode = "19391945";
    public bool isLocked = true;
    private string currentInputCode = "";
    public int maxCodeLength = 8;
    public float resetCodeTime = 5f;
    private float lastInputTime;
    
    [Header("Système d'Objectifs")]
    public string doorId = "porte_principale";
    public SystemeObjectifs systemeObjectifs;
    public bool estObjectif = false;
    private bool objectifComplete = false;
    
    [Header("VR Physics & Feedback")]
    public bool usePhysicsInVR = true;
    public float hapticIntensity = 0.2f;
    public float hapticDuration = 0.1f;
    public float soundVelocityThreshold = 0.1f;
    
    private Rigidbody rb;
    private HingeJoint hinge;
    private XRBaseInteractable xrInteractable;
    private float lastHingeAngle;
    private bool isBeingGrabbed = false;

    private bool isOpen = false;
    private bool isRotating = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    
    private GUIStyle codeStyle;
    private GUIStyle statusStyle;
    public LayerMask raycastLayers = -1;

    void Start()
    {
        if (doorPivot == null) doorPivot = transform;
        
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
        if (hinge != null) lastHingeAngle = hinge.angle;
        
        // Desktop GUI Styles
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
            systemeObjectifs.AjouterObjectif("trouver_code_" + doorId, "Trouver le code de la porte: " + doorId);
        }
    }

    void SetupVRInteraction()
    {
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
            rb.mass = 10f; 
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null) meshCollider.convex = true;

            hinge = GetComponent<HingeJoint>();
            if (hinge == null) hinge = gameObject.AddComponent<HingeJoint>();
            
            // Smart Anchor detection
            Vector3 anchorPos = Vector3.zero;
            if (doorPivot != null && doorPivot != transform)
            {
                anchorPos = transform.InverseTransformPoint(doorPivot.position);
            }
            else
            {
                var meshFilter = GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    var bounds = meshFilter.sharedMesh.bounds;
                    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (Mathf.Abs(bounds.size.y - maxDim) < 0.01f) {
                        hinge.axis = Vector3.up;
                        anchorPos = new Vector3(bounds.min.x, 0, 0);
                    } else if (Mathf.Abs(bounds.size.z - maxDim) < 0.01f) {
                        hinge.axis = Vector3.forward;
                        anchorPos = new Vector3(bounds.min.x, 0, 0);
                    } else {
                        hinge.axis = Vector3.right;
                        anchorPos = new Vector3(0, 0, bounds.min.z);
                    }
                }
            }

            hinge.anchor = anchorPos;
            if (hinge.axis == Vector3.zero) hinge.axis = rotationAxis; 
            hinge.useLimits = true;
            JointLimits limits = hinge.limits;
            limits.min = -openAngle; 
            limits.max = openAngle;
            hinge.limits = limits;
        }
        else
        {
            if (oldGrab != null && Application.isPlaying) Destroy(oldGrab);
            var simple = oldSimple != null ? oldSimple : gameObject.AddComponent<XRSimpleInteractable>();
            xrInteractable = simple;
        }

        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
            xrInteractable.selectExited.AddListener(OnXRSelectExit);
            xrInteractable.hoverEntered.AddListener(OnXRHover);
        }
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        isBeingGrabbed = true;
        TriggerHaptic(args.interactorObject, hapticIntensity, hapticDuration);
        HandleVRInteraction();
    }

    private void OnXRSelectExit(SelectExitEventArgs args)
    {
        isBeingGrabbed = false;
    }

    private void OnXRHover(HoverEnterEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor direct) direct.SendHapticImpulse(0.05f, 0.05f);
        if (args.interactorObject is XRDirectInteractor) HandleVRInteraction();
    }

    private void TriggerHaptic(IXRInteractor interactor, float intensity, float duration)
    {
        if (interactor is XRBaseInputInteractor input) input.SendHapticImpulse(intensity, duration);
    }

    private void HandleVRInteraction()
    {
        if (requiresCode && isLocked)
        {
            if (audioSource != null && doorLockedSound != null) audioSource.PlayOneShot(doorLockedSound);
        }
        else if (!usePhysicsInVR)
        {
            ToggleDoor();
        }
    }
    
    void FixedUpdate() {
        if (usePhysicsInVR && rb != null) {
            if (!isLocked && rb.isKinematic) rb.isKinematic = false;
            if (isLocked && !rb.isKinematic) rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) TryToggleDoor();
        CheckNumericInput();
        
        if (currentInputCode.Length > 0 && Time.time - lastInputTime > resetCodeTime) ResetCode();
        
        if (isRotating && !usePhysicsInVR) AnimateDoor();

        if (usePhysicsInVR && hinge != null && audioSource != null)
        {
            float currentAngle = hinge.angle;
            float velocity = Mathf.Abs(currentAngle - lastHingeAngle) / Time.deltaTime;
            
            if (velocity > soundVelocityThreshold)
            {
                if (!audioSource.isPlaying && doorOpenSound != null)
                {
                    audioSource.clip = doorOpenSound;
                    audioSource.Play();
                }

                if (isBeingGrabbed && xrInteractable != null && xrInteractable.interactorsSelecting.Count > 0)
                {
                    TriggerHaptic(xrInteractable.interactorsSelecting[0], 0.05f, 0.01f);
                }
            }
            lastHingeAngle = currentAngle;
        }
    }

    public void AddDigit(int digit) => AddDigitToCode(digit.ToString());

    void OnGUI()
    {
        if (Application.isBatchMode || !requiresCode || !isLocked) return;
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                    (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
                {
                    string displayCode = currentInputCode.PadRight(maxCodeLength, '_');
                    GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 50), "CODE: " + displayCode, codeStyle);
                }
            }
        }
    }
    
    void TryToggleDoor()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || 
                (doorPivot != transform && (hit.transform == doorPivot || hit.transform.IsChildOf(doorPivot))))
            {
                if (requiresCode && isLocked)
                {
                    if (audioSource != null && doorLockedSound != null) audioSource.PlayOneShot(doorLockedSound);
                }
                else ToggleDoor();
            }
        }
    }
    
    void CheckNumericInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) AddDigitToCode(i.ToString());
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ValidateCode();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) ResetCode();
    }
    
    void AddDigitToCode(string digit)
    {
        if (currentInputCode.Length < maxCodeLength)
        {
            currentInputCode += digit;
            lastInputTime = Time.time;
            if (currentInputCode.Length == maxCodeLength) ValidateCode();
        }
    }
    
    void ValidateCode()
    {
        if (currentInputCode == correctCode)
        {
            isLocked = false;
            if (rb != null) rb.isKinematic = false;
            if (audioSource != null && doorUnlockSound != null) audioSource.PlayOneShot(doorUnlockSound);
            if (estObjectif && !objectifComplete && systemeObjectifs != null)
            {
                systemeObjectifs.CompleterObjectif("trouver_code_" + doorId);
                objectifComplete = true;
            }
            if (!usePhysicsInVR) ToggleDoor();
        }
        else if (audioSource != null && codeErrorSound != null) audioSource.PlayOneShot(codeErrorSound);
        currentInputCode = "";
    }
    
    void ResetCode() => currentInputCode = "";
    public void SetDoorCode(string newCode) => correctCode = newCode;
    
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