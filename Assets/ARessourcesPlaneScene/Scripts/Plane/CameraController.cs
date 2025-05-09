using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Cible")]
    public Transform cibleAvion;
    
    // Déclarer l'enum sans Header
    public enum ModeCam { Suivre, Cockpit, Vue3eme, Libre }
    
    [Header("Modes de caméra")]
    public ModeCam modeCamera = ModeCam.Suivre;
    
    [Header("Configuration de suivi")]
    public float distanceArriere = 10f;
    public float hauteur = 3f;
    public float vitesseLissage = 5f;
    
    [Header("Configuration cockpit")]
    public Vector3 offsetCockpit = new Vector3(0f, 2f, 0.5f);
    public float sensibiliteRotation = 2f;
    
    [Header("Configuration 3ème personne")]
    public float distance3eme = 15f;
    public float hauteur3eme = 5f;
    public float angle3eme = 30f;
    
    [Header("Configuration libre")]
    public float vitesseLibre = 50f;
    public float sensibiliteLibre = 3f;
    
    private Vector3 velocite = Vector3.zero;
    private Vector3 positionCible;
    private Quaternion rotationCible;
    private Vector3 positionLibre;
    private Vector3 rotationLibre;
    
    void Start()
    {
        if (cibleAvion == null)
        {
            // Essayer de trouver automatiquement l'avion
            AvionController avion = FindObjectOfType<AvionController>();
            if (avion != null)
            {
                cibleAvion = avion.transform;
            }
            else
            {
                Debug.LogWarning("CameraController: Aucune cible d'avion n'a été assignée!");
            }
        }
        
        // Initialiser la position pour le mode libre
        positionLibre = transform.position;
        rotationLibre = transform.eulerAngles;
    }
    
    void LateUpdate()
    {
        if (cibleAvion == null) return;
        
        // Changer de mode caméra avec les touches du pavé numérique
        if (Input.GetKeyDown(KeyCode.Alpha1)) modeCamera = ModeCam.Suivre;
        if (Input.GetKeyDown(KeyCode.Alpha2)) modeCamera = ModeCam.Cockpit;
        if (Input.GetKeyDown(KeyCode.Alpha3)) modeCamera = ModeCam.Vue3eme;
        if (Input.GetKeyDown(KeyCode.Alpha4)) modeCamera = ModeCam.Libre;
        
        switch (modeCamera)
        {
            case ModeCam.Suivre:
                ModeUivreUpdate();
                break;
                
            case ModeCam.Cockpit:
                ModeCockpitUpdate();
                break;
                
            case ModeCam.Vue3eme:
                Mode3emeUpdate();
                break;
                
            case ModeCam.Libre:
                ModeLibreUpdate();
                break;
        }
    }
    
    void ModeUivreUpdate()
    {
        // Calculer la position et rotation cibles
        positionCible = cibleAvion.position 
                      - cibleAvion.forward * distanceArriere 
                      + Vector3.up * hauteur;
                      
        rotationCible = Quaternion.LookRotation(cibleAvion.position - positionCible + cibleAvion.forward * 10f);
        
        // Appliquer un lissage pour éviter les mouvements brusques
        transform.position = Vector3.Slerp(transform.position, positionCible, Time.deltaTime * vitesseLissage);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationCible, Time.deltaTime * vitesseLissage);
    }
    
    void ModeCockpitUpdate()
    {
        // Position dans le cockpit
        transform.position = cibleAvion.TransformPoint(offsetCockpit);
        
        // Rotation de base alignée avec l'avion
        Quaternion baseRotation = cibleAvion.rotation;
        
        // Ajout de la rotation de la tête du joueur (contrôlée par la souris si le bouton droit est enfoncé)
        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse Y") * -sensibiliteRotation;
            float rotY = Input.GetAxis("Mouse X") * sensibiliteRotation;
            
            // Limiter la rotation vers le haut/bas
            rotX = Mathf.Clamp(rotX, -45f, 45f);
            
            baseRotation *= Quaternion.Euler(rotX, rotY, 0);
        }
        
        transform.rotation = baseRotation;
    }
    
    void Mode3emeUpdate()
    {
        // Calculer la position en fonction de l'angle et la distance
        float angle = Mathf.Deg2Rad * angle3eme;
        
        positionCible = cibleAvion.position 
                      - cibleAvion.forward * distance3eme * Mathf.Cos(angle) 
                      + Vector3.up * hauteur3eme;
                      
        rotationCible = Quaternion.LookRotation(cibleAvion.position - positionCible + cibleAvion.forward * 5f);
        
        // Appliquer un lissage
        transform.position = Vector3.Slerp(transform.position, positionCible, Time.deltaTime * vitesseLissage);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationCible, Time.deltaTime * vitesseLissage);
    }
    
    void ModeLibreUpdate()
    {
        // Mode spectateur libre contrôlé par WASD et souris
        if (Input.GetMouseButton(1))
        {
            // Rotation avec la souris
            float mouseX = Input.GetAxis("Mouse X") * sensibiliteLibre;
            float mouseY = Input.GetAxis("Mouse Y") * -sensibiliteLibre;
            
            rotationLibre.y += mouseX;
            rotationLibre.x = Mathf.Clamp(rotationLibre.x + mouseY, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(rotationLibre);
        }
        
        // Déplacement avec WASD
        Vector3 deplacement = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) deplacement += transform.forward;
        if (Input.GetKey(KeyCode.S)) deplacement -= transform.forward;
        if (Input.GetKey(KeyCode.A)) deplacement -= transform.right;
        if (Input.GetKey(KeyCode.D)) deplacement += transform.right;
        if (Input.GetKey(KeyCode.E)) deplacement += transform.up;
        if (Input.GetKey(KeyCode.Q)) deplacement -= transform.up;
        
        // Accélérer avec Shift
        float vitesseActuelle = vitesseLibre;
        if (Input.GetKey(KeyCode.LeftShift)) vitesseActuelle *= 3f;
        
        // Appliquer le déplacement
        positionLibre += deplacement.normalized * vitesseActuelle * Time.deltaTime;
        transform.position = positionLibre;
    }
} 