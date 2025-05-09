using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvionController : MonoBehaviour
{
    [Header("Configuration de l'avion")]
    public float vitesseMaximale = 50f;
    public float acceleration = 20f;
    public float sensibiliteSouris = 2f;
    public float stabilisation = 1f;
    public float correctionDerive = 0.5f;

    [Header("Limitations de virage")]
    [Range(0.1f, 2f)] public float vitesseMaxRotation = 0.8f;  // Vitesse maximale de rotation
    [Range(0.1f, 1f)] public float accelerationRotation = 0.3f;  // Accélération de la rotation
    [Range(0.1f, 1f)] public float decelerationRotation = 0.5f;  // Décélération de la rotation
    [Range(0f, 1f)] public float influenceVitesseRotation = 0.7f;  // Influence de la vitesse sur la rotation
    [Range(0f, 1f)] public float influenceAltitudeRotation = 0.3f;  // Influence de l'altitude sur la rotation

    [Header("Composants")]
    public Transform propulseur;
    public float vitesseRotationPropulseur = 1000f;
    public Vector3 axeRotationPropulseur = Vector3.forward;  // Axe de rotation du propulseur
    public AudioSource sonMoteur;
    public float pitchMinMoteur = 0.6f;
    public float pitchMaxMoteur = 1.2f;

    [Header("Caméra")]
    public Transform cameraAvion;
    public float distanceCameraZ = -10f;
    public float hauteurCamera = 2f;
    public float vitesseLissageCam = 5f;

    // Variables privées
    private Rigidbody rb;
    private float vitesseActuelle;
    private float accelerateur = 0.5f;
    private bool controlsEnabled = true;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float vitesseRotationActuelle = 0f;
    private float vitesseRotationCible = 0f;

    // Propriété publique pour accéder à la vitesse actuelle
    public float VitesseActuelle { get { return vitesseActuelle; } }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = 1000;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        vitesseActuelle = vitesseMaximale * 0.5f;
        accelerateur = 0.5f;

        // Orientation initiale vers l'axe X positif
        transform.rotation = Quaternion.LookRotation(Vector3.right);
        
        if (sonMoteur)
        {
            sonMoteur.loop = true;
            sonMoteur.Play();
        }
        
        // Verrouiller et cacher le curseur
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Toggle du pilote automatique
        if (Input.GetKeyDown(KeyCode.P))
        {
            controlsEnabled = !controlsEnabled;
            if (!controlsEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Gestion des entrées
        if (controlsEnabled)
        {
            // Accélération/Décélération avec Espace et Ctrl
            if (Input.GetKey(KeyCode.Space))
            {
                accelerateur = Mathf.Min(1f, accelerateur + Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                accelerateur = Mathf.Max(0f, accelerateur - Time.deltaTime);
            }

            // Rotation avec la souris
            float mouseX = Input.GetAxis("Mouse X") * sensibiliteSouris;
            float mouseY = Input.GetAxis("Mouse Y") * sensibiliteSouris;

            // Calculer la vitesse de rotation cible en fonction de la vitesse et de l'altitude
            float facteurVitesse = 1f - (vitesseActuelle / vitesseMaximale) * influenceVitesseRotation;
            float facteurAltitude = 1f - (transform.position.y / 100f) * influenceAltitudeRotation;
            float vitesseRotationMax = vitesseMaxRotation * facteurVitesse * facteurAltitude;

            // Appliquer l'accélération et la décélération de la rotation
            if (Mathf.Abs(mouseX) > 0.1f)
            {
                vitesseRotationCible = Mathf.MoveTowards(vitesseRotationCible, mouseX * vitesseRotationMax, accelerationRotation * Time.deltaTime);
            }
            else
            {
                vitesseRotationCible = Mathf.MoveTowards(vitesseRotationCible, 0f, decelerationRotation * Time.deltaTime);
            }

            // Appliquer la rotation avec inertie
            vitesseRotationActuelle = Mathf.Lerp(vitesseRotationActuelle, vitesseRotationCible, Time.deltaTime * 5f);
            rotationY += vitesseRotationActuelle;

            // Limiter la rotation verticale
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -80f, 80f);

            // Appliquer la rotation avec l'axe X comme direction principale
            Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
            transform.rotation = Quaternion.LookRotation(Vector3.right) * rotation;
        }
        
        // Mise à jour du propulseur
        if (propulseur)
        {
            // Rotation continue du propulseur autour de son axe central
            propulseur.Rotate(axeRotationPropulseur, vitesseRotationPropulseur * accelerateur * Time.deltaTime, Space.Self);
        }

        // Son du moteur
        if (sonMoteur)
        {
            sonMoteur.pitch = Mathf.Lerp(pitchMinMoteur, pitchMaxMoteur, accelerateur);
            sonMoteur.volume = 0.5f + 0.5f * accelerateur;
        }
    }

    void FixedUpdate()
    {
        // Calculer la vitesse actuelle
        vitesseActuelle = Mathf.Lerp(vitesseActuelle, vitesseMaximale * accelerateur, Time.deltaTime * acceleration);

        // Appliquer la force de propulsion dans la direction de l'avion
        Vector3 force = transform.forward * vitesseActuelle;
        rb.AddForce(force, ForceMode.Acceleration);

        // Stabilisation automatique avec influence de la vitesse
        float facteurStabilisation = stabilisation * (1f + (vitesseActuelle / vitesseMaximale));
        Vector3 stabilisationForce = -rb.linearVelocity * facteurStabilisation;
        rb.AddForce(stabilisationForce, ForceMode.Acceleration);

        // Correction de la dérive latérale
        Vector3 vitesseLaterale = Vector3.Project(rb.linearVelocity, transform.right);
        rb.AddForce(-vitesseLaterale * correctionDerive, ForceMode.Acceleration);

        // Mise à jour de la caméra
        if (cameraAvion)
        {
            Vector3 positionCible = transform.position + transform.forward * distanceCameraZ + Vector3.up * hauteurCamera;
            cameraAvion.position = Vector3.Lerp(cameraAvion.position, positionCible, Time.deltaTime * vitesseLissageCam);
            cameraAvion.LookAt(transform.position + transform.forward * 10f);
        }
    }
    
    void OnGUI()
    {
        // Afficher les informations de contrôle
        GUI.Label(new Rect(10, 10, 400, 100), 
            "Contrôles:\n" +
            "- Souris : Orientation de l'avion\n" +
            "- Espace : Accélérer\n" +
            "- Ctrl : Ralentir\n" +
            "- P : Activer/Désactiver les contrôles\n\n" +
            "Vitesse: " + vitesseActuelle.ToString("F1") + "\n" +
            "Accélérateur: " + (accelerateur * 100).ToString("F0") + "%\n" +
            "Altitude: " + transform.position.y.ToString("F1"));
    }
} 