using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvionController : MonoBehaviour
{
    [Header("Configuration de l'avion")]
    public float vitesseAvant = 20f;
    public float vitesseMaximale = 100f;
    public float accelerationAvant = 10f;
    public float sensibiliteTourne = 1.5f;        // Réduit pour un contrôle plus précis
    public float sensibiliteMonte = 1.5f;         // Réduit pour un contrôle plus précis
    public float stabilisationVitesse = 2f;       // Augmenté pour une réponse plus rapide
    public float stabilisationAutomatique = 1.2f; // Augmenté pour un meilleur redressement

    [Header("Limites")]
    public float altitudeMaximale = 300f;         // Altitude maximale autorisée
    public float forceRetourAltitude = 2f;        // Force de retour quand l'altitude est dépassée

    [Header("Orientation")]
    public bool forceAxisX = false;  // Désactivé par défaut pour un vol libre
    public float alignXForce = 3f;   // Réduit pour moins forcer l'alignement
    public float correctionDeriveX = 0.2f; // Correction de la dérive vers la droite
    public float correctionDeriveY = 0.1f; // Correction de la dérive verticale
    public float correctionDeriveZ = 0.3f; // Correction de la dérive sur l'axe Z

    [Header("Composants")]
    public Transform propulseur;
    public float vitesseRotationPropulseur = 1000f;
    public AudioSource sonMoteur;
    public float pitchMinMoteur = 0.6f;
    public float pitchMaxMoteur = 1.2f;

    [Header("Caméra")]
    public Transform cameraAvion;
    public float distanceCameraY = 5f;
    public float distanceCameraZ = -10f;
    public float hauteurCamera = 2f;
    public float vitesseLissageCam = 5f;

    [Header("Contrôles")]
    [Tooltip("Utiliser des contrôles alternatifs (flèches au lieu de ZQSD)")]
    public bool utiliserControlesAlternatifs = true;
    
    // Contrôles ZQSD
    public KeyCode toucheAvancer = KeyCode.Z;        // Z pour avancer
    public KeyCode toucheReculer = KeyCode.S;        // S pour reculer
    public KeyCode toucheGauche = KeyCode.Q;         // Q pour tourner à gauche
    public KeyCode toucheDroite = KeyCode.D;         // D pour tourner à droite
    
    // Contrôles alternatifs (flèches)
    public KeyCode toucheAvancerAlt = KeyCode.UpArrow;     // Flèche haut pour avancer
    public KeyCode toucheReculerAlt = KeyCode.DownArrow;   // Flèche bas pour reculer
    public KeyCode toucheGaucheAlt = KeyCode.LeftArrow;    // Flèche gauche
    public KeyCode toucheDroiteAlt = KeyCode.RightArrow;   // Flèche droite
    
    // Contrôles communs
    public KeyCode toucheMonter = KeyCode.Space;     // Espace pour monter
    public KeyCode toucheDescendre = KeyCode.LeftControl; // Ctrl pour descendre
    public KeyCode toucheAccelerer = KeyCode.LeftShift; // Maj pour accélérer
    public KeyCode toucheRalentir = KeyCode.R;       // R pour ralentir
    
    [Header("Autopilote")]
    public bool autopilotAltitude = true;            // Active le maintien automatique d'altitude
    public float forcePiloteAutomatique = 0.3f;      // Force du pilote automatique

    // Variables
    public float vitesseActuelle;
    private float inputPitchActuel;
    private float inputRollActuel;
    private float inputYawActuel;
    private float inputElevationDirecte = 0f;       // Nouvelle variable pour l'élévation directe
    private Rigidbody rb;
    private TerrainGenerator terrainGenerator;
    private float altitudeInitiale;
    private bool controlsEnabled = true;
    private Vector3 directionInitiale = Vector3.right; // Direction initiale sur l'axe X
    private Vector3 correctionDeriveVector;
    private Vector3 correctionAxeZVector;
    
    // Debug
    [Header("Débogage")]
    public bool afficherDebug = true;
    private float tempsDepuisDebug = 0f;
    private string derniereEntree = "Aucune";
    public bool afficherVecteurs = true;

    void Start()
    {
        // Vérifier si le Rigidbody existe, sinon l'ajouter
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("AvionController: Rigidbody manquant, ajouté automatiquement.");
        }
        
        // Configuration du Rigidbody
        rb.mass = 1000;
        rb.linearDamping = 0.1f;  // Résistance à l'air
        rb.angularDamping = 0.8f; // Résistance à la rotation
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Initialiser la vitesse
        vitesseActuelle = vitesseAvant * 1.2f;

        // Trouver le générateur de terrain
        terrainGenerator = FindObjectOfType<TerrainGenerator>();
        
        // Initialiser le son du moteur s'il existe
        if (sonMoteur)
        {
            sonMoteur.loop = true;
            sonMoteur.Play();
        }
        
        // Mémoriser l'altitude initiale pour l'autopilote
        altitudeInitiale = transform.position.y;
        
        // Orientation initiale vers l'axe X positif
        if (forceAxisX)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.right);
        }
        
        // Initialiser les vecteurs de correction de dérive
        correctionDeriveVector = new Vector3(-correctionDeriveX, -correctionDeriveY, 0);
        correctionAxeZVector = new Vector3(0, 0, -correctionDeriveZ);
        
        // Afficher un message pour confirmer l'initialisation
        Debug.Log("AvionController initialisé. Utilisez " + 
                 (utiliserControlesAlternatifs ? "les flèches" : "ZQSD") + 
                 " pour contrôler l'avion.");
    }

    void Update()
    {
        // Toggle du pilote automatique
        if (Input.GetKeyDown(KeyCode.P))
        {
            controlsEnabled = !controlsEnabled;
            if (!controlsEnabled)
                Debug.Log("Pilote automatique activé");
            else
                Debug.Log("Contrôle manuel activé");
        }

        // Gestion des entrées
        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;
        inputElevationDirecte = 0f; // Réinitialiser l'élévation directe
        
        if (controlsEnabled)
        {
            bool toucheAppuyee = false;
            
            // Déterminer quelles touches utiliser selon le mode de contrôle
            KeyCode avancer = utiliserControlesAlternatifs ? toucheAvancerAlt : toucheAvancer;
            KeyCode reculer = utiliserControlesAlternatifs ? toucheReculerAlt : toucheReculer;
            KeyCode gauche = utiliserControlesAlternatifs ? toucheGaucheAlt : toucheGauche;
            KeyCode droite = utiliserControlesAlternatifs ? toucheDroiteAlt : toucheDroite;
            
            // Avancer/Reculer (Monter/Descendre)
            if (Input.GetKey(avancer)) { pitch -= 1f; toucheAppuyee = true; derniereEntree = "Avancer"; }
            if (Input.GetKey(reculer)) { pitch += 1f; toucheAppuyee = true; derniereEntree = "Reculer"; }
            
            // Gauche/Droite (Roulis et direction)
            if (Input.GetKey(gauche)) { roll += 1f; yaw -= 1f; toucheAppuyee = true; derniereEntree = "Gauche"; }
            if (Input.GetKey(droite)) { roll -= 1f; yaw += 1f; toucheAppuyee = true; derniereEntree = "Droite"; }
            
            // Monter/Descendre directement (utiliser l'élévation directe au lieu de pitch)
            if (Input.GetKey(toucheMonter)) 
            { 
                inputElevationDirecte = 1f; 
                toucheAppuyee = true; 
                derniereEntree = "Monter"; 
            }
            if (Input.GetKey(toucheDescendre)) 
            { 
                inputElevationDirecte = -1f; 
                toucheAppuyee = true; 
                derniereEntree = "Descendre"; 
            }
            
            // Accélération/Décélération
            if (Input.GetKey(toucheAccelerer))
            {
                vitesseActuelle += accelerationAvant * 1.5f * Time.deltaTime;
                toucheAppuyee = true;
                derniereEntree = "Accélérer";
            }
            else if (Input.GetKey(toucheRalentir))
            {
                vitesseActuelle -= accelerationAvant * 1.5f * Time.deltaTime;
                toucheAppuyee = true;
                derniereEntree = "Ralentir";
            }
            
            // Appliquer la correction de dérive constante
            roll += correctionDeriveX;
            pitch += correctionDeriveY;
            
            // Afficher des informations de débogage
            if (afficherDebug)
            {
                tempsDepuisDebug += Time.deltaTime;
                if (tempsDepuisDebug > 1f && toucheAppuyee)
                {
                    Debug.Log("Contrôle: " + derniereEntree + 
                              " | Vitesse: " + vitesseActuelle.ToString("F1") + 
                              " | Position: " + transform.position.ToString("F1") +
                              " | Élévation: " + inputElevationDirecte);
                    tempsDepuisDebug = 0f;
                }
            }
        }
        
        // Vérifier l'altitude maximale
        if (transform.position.y > altitudeMaximale)
        {
            // Appliquer une force vers le bas proportionnelle au dépassement
            float depassement = transform.position.y - altitudeMaximale;
            pitch += depassement * forceRetourAltitude * 0.01f;
            
            if (afficherDebug && tempsDepuisDebug > 1f)
            {
                Debug.Log("Altitude maximale dépassée! Force de retour appliquée.");
                tempsDepuisDebug = 0f;
            }
        }
        
        // Autopilote pour maintenir l'altitude si activé et pas de commande d'élévation manuelle
        if (autopilotAltitude && inputElevationDirecte == 0f)
        {
            float altitudeDifference = altitudeInitiale - transform.position.y;
            pitch -= altitudeDifference * forcePiloteAutomatique; // Correction proportionnelle
        }

        // Maintenir une vitesse minimale pour éviter les pertes de contrôle
        if (vitesseActuelle < vitesseAvant * 0.5f)
        {
            vitesseActuelle = vitesseAvant * 0.5f;
        }
        
        // Limiteur de vitesse
        vitesseActuelle = Mathf.Clamp(vitesseActuelle, vitesseAvant * 0.5f, vitesseMaximale);

        // Lissage des contrôles
        inputPitchActuel = Mathf.Lerp(inputPitchActuel, pitch * sensibiliteMonte, Time.deltaTime * stabilisationVitesse);
        inputRollActuel = Mathf.Lerp(inputRollActuel, roll * sensibiliteTourne, Time.deltaTime * stabilisationVitesse);
        inputYawActuel = Mathf.Lerp(inputYawActuel, yaw * sensibiliteTourne, Time.deltaTime * stabilisationVitesse);

        // Rotation de l'hélice
        if (propulseur)
        {
            propulseur.Rotate(Vector3.forward, vitesseRotationPropulseur * (vitesseActuelle / vitesseMaximale) * Time.deltaTime);
        }

        // Son du moteur
        if (sonMoteur)
        {
            sonMoteur.pitch = Mathf.Lerp(pitchMinMoteur, pitchMaxMoteur, vitesseActuelle / vitesseMaximale);
            sonMoteur.volume = 0.5f + 0.5f * (vitesseActuelle / vitesseMaximale);
        }

        // Mise à jour du générateur de terrain si disponible
        if (terrainGenerator != null)
        {
            terrainGenerator.ActualiserPosition(transform.position);
        }
        
        // Afficher les vecteurs de correction en mode debug
        if (afficherVecteurs)
        {
            Debug.DrawRay(transform.position, correctionDeriveVector * 10f, Color.red);
            Debug.DrawRay(transform.position, correctionAxeZVector * 10f, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.green);
        }
    }

    void FixedUpdate()
    {
        // Appliquer les forces physiques
        Vector3 direction = transform.forward;
        
        // Forcer l'alignement avec l'axe X si l'option est activée
        if (forceAxisX && !Input.GetKey(utiliserControlesAlternatifs ? toucheGaucheAlt : toucheGauche) 
                       && !Input.GetKey(utiliserControlesAlternatifs ? toucheDroiteAlt : toucheDroite))
        {
            // Calculer un vecteur de direction qui tend vers l'axe X tout en gardant la composante Y
            float currentY = direction.y;
            direction = Vector3.Lerp(direction, new Vector3(1, currentY, 0).normalized, Time.fixedDeltaTime * alignXForce);
            direction.Normalize();
            
            // Appliquer cette rotation à l'avion
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * alignXForce);
        }
        
        // Appliquer la vitesse - Utiliser AddForce au lieu de définir directement la vélocité
        rb.AddForce(direction * vitesseActuelle * 100f * Time.fixedDeltaTime, ForceMode.Acceleration);
        
        // Appliquer une force opposée à la dérive sur l'axe Z
        rb.AddForce(-transform.right * correctionDeriveZ * 50f * Time.fixedDeltaTime, ForceMode.Acceleration);
        
        // Appliquer l'élévation directe (monter/descendre) indépendamment de la rotation
        if (inputElevationDirecte != 0)
        {
            // Force verticale pure, indépendante de la direction de l'avion
            rb.AddForce(Vector3.up * inputElevationDirecte * 2000f * Time.fixedDeltaTime, ForceMode.Force);
        }
        
        // Limiter la vitesse maximale
        if (rb.linearVelocity.magnitude > vitesseMaximale)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * vitesseMaximale;
        }

        // Appliquer les rotations avec correction de dérive
        Vector3 rotation = new Vector3(inputPitchActuel, inputYawActuel, -inputRollActuel);
        rb.AddRelativeTorque(rotation * 10000f * Time.fixedDeltaTime, ForceMode.Force);
        
        // Appliquer une force constante pour contrer la dérive naturelle
        rb.AddRelativeTorque(correctionDeriveVector * 5000f * Time.fixedDeltaTime, ForceMode.Force);
        
        // Appliquer une correction spécifique pour la dérive sur l'axe Z
        rb.AddRelativeTorque(correctionAxeZVector * 3000f * Time.fixedDeltaTime, ForceMode.Force);

        // Stabilisation naturelle renforcée pour un pilotage plus facile
        rb.AddRelativeTorque(
            -rb.angularVelocity.x * stabilisationVitesse * stabilisationAutomatique,
            -rb.angularVelocity.y * stabilisationVitesse * stabilisationAutomatique,
            -rb.angularVelocity.z * stabilisationVitesse * stabilisationAutomatique,
            ForceMode.Acceleration
        );
        
        // Redressement automatique quand on ne touche à rien
        KeyCode gauche = utiliserControlesAlternatifs ? toucheGaucheAlt : toucheGauche;
        KeyCode droite = utiliserControlesAlternatifs ? toucheDroiteAlt : toucheDroite;
        KeyCode avancer = utiliserControlesAlternatifs ? toucheAvancerAlt : toucheAvancer;
        KeyCode reculer = utiliserControlesAlternatifs ? toucheReculerAlt : toucheReculer;
        
        if (!Input.GetKey(gauche) && !Input.GetKey(droite) && 
            !Input.GetKey(avancer) && !Input.GetKey(reculer))
        {
            // Rotation vers le haut plus Quaternion.identity pour redresser l'avion
            Quaternion targetRotation;
            
            if (forceAxisX)
            {
                // Si on force l'axe X, on redresse sur les axes Y et Z
                targetRotation = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                // Sinon on redresse juste l'axe Z (garder l'avion droit)
                targetRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            }
            
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * stabilisationAutomatique);
        }
    }

    void LateUpdate()
    {
        // Gérer la caméra si elle est assignée
        if (cameraAvion)
        {
            Vector3 targetPosition = transform.position 
                                   + (transform.up * hauteurCamera) 
                                   + (transform.right * distanceCameraY) 
                                   + (transform.forward * distanceCameraZ);
            
            cameraAvion.position = Vector3.Lerp(cameraAvion.position, targetPosition, Time.deltaTime * vitesseLissageCam);
            cameraAvion.LookAt(transform.position + transform.forward * 10f);
        }
    }
    
    void OnGUI()
    {
        if (afficherDebug)
        {
            // Afficher les informations de contrôle à l'écran
            GUI.Label(new Rect(10, 10, 300, 20), "Contrôles: " + (utiliserControlesAlternatifs ? "Flèches" : "ZQSD"));
            GUI.Label(new Rect(10, 30, 300, 20), "Vitesse: " + vitesseActuelle.ToString("F1"));
            GUI.Label(new Rect(10, 50, 300, 20), "Dernière touche: " + derniereEntree);
            GUI.Label(new Rect(10, 70, 300, 20), "Altitude: " + transform.position.y.ToString("F1") + " / " + altitudeMaximale);
            GUI.Label(new Rect(10, 90, 300, 20), "Position Z: " + transform.position.z.ToString("F1"));
            GUI.Label(new Rect(10, 110, 300, 20), "Correction Z: " + correctionDeriveZ.ToString("F2"));
        }
    }
} 