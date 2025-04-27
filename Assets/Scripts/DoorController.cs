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
    [Range(0f, 1f)]
    public float volume = 1.0f;
    
    [Header("Débogage")]
    public bool showDebugRay = true;
    public LayerMask raycastLayers = -1; // Tous les layers par défaut
    
    private bool isOpen = false;
    private bool isRotating = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    
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
    }
    
    void Update()
    {
        // Vérifier l'interaction avec la touche F
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Touche F appuyée");
            TryToggleDoor();
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
                Debug.Log("Porte détectée! Ouverture/fermeture...");
                ToggleDoor();
            }
        }
        else
        {
            Debug.Log("Rayon n'a rien touché dans la distance " + raycastDistance);
        }
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