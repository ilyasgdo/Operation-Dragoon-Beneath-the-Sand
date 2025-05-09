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

    [Header("Aérodynamique Avancée")]
    [Tooltip("Force de portance de l'avion")]
    public float coefficientPortance = 0.002f;
    [Tooltip("Vitesse à laquelle la portance devient maximale")]
    public float vitessePortanceOptimale = 80f;
    [Tooltip("Effet aérodynamique (tendance de l'avion à s'aligner avec sa direction)")]
    public float effetAerodynamique = 0.02f;
    [Tooltip("Augmentation de la traînée avec la vitesse")]
    public float facteurAugmentationTrainee = 0.001f;
    [Tooltip("Force du virage en inclinaison")]
    public float effetVirageIncline = 0.5f;
    
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
    public KeyCode toucheFreinAir = KeyCode.B;
    
    [Header("Autopilote")]
    public bool autopilotAltitude = true;            // Active le maintien automatique d'altitude
    public float forcePiloteAutomatique = 0.3f;      // Force du pilote automatique

    // Variables publiques accessibles
    public float VitesseActuelle { get { return vitesseActuelle; } }
    public float Altitude { get { return transform.position.y; } }
    public float AngleInclinaison { get; private set; }
    public float AngleTangage { get; private set; }
    public bool FreinAir { get; private set; }
    public float PuissanceMoteur { get; private set; }

    // Variables
    private float vitesseActuelle;
    private float accelerateur = 0.5f;
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
    private float traineeDOrigine;
    private float traineeAngulaireDOrigine;
    private float facteurAero;
    private bool immobilise = false;
    private float montantVirageIncline;
    
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
        
        // Stocker les valeurs d'origine pour la traînée
        traineeDOrigine = rb.linearDamping;
        traineeAngulaireDOrigine = rb.angularDamping;
        
        // Configuration du Rigidbody
        rb.mass = 1000;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.8f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Initialiser la vitesse
        vitesseActuelle = vitesseAvant * 1.2f;
        accelerateur = 0.6f;

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
        GererEntreesUtilisateur();
        
        // Mise à jour du propulseur
        if (propulseur)
        {
            propulseur.Rotate(Vector3.forward, vitesseRotationPropulseur * accelerateur * Time.deltaTime);
        }

        // Son du moteur
        if (sonMoteur)
        {
            sonMoteur.pitch = Mathf.Lerp(pitchMinMoteur, pitchMaxMoteur, accelerateur);
            sonMoteur.volume = 0.5f + 0.5f * accelerateur;
        }

        // Mise à jour du générateur de terrain si disponible
        if (terrainGenerator != null)
        {
            terrainGenerator.ActualiserChunks();
        }
        
        // Afficher les vecteurs de correction en mode debug
        if (afficherVecteurs)
        {
            Debug.DrawRay(transform.position, correctionDeriveVector * 10f, Color.red);
            Debug.DrawRay(transform.position, correctionAxeZVector * 10f, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.green);
        }
    }
    
    void GererEntreesUtilisateur()
    {
        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;
        inputElevationDirecte = 0f;
        FreinAir = false;
        
        if (controlsEnabled && !immobilise)
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
            
            // Monter/Descendre directement
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
            
            // Frein à air
            if (Input.GetKey(toucheFreinAir))
            {
                FreinAir = true;
                toucheAppuyee = true;
                derniereEntree = "Frein Air";
            }
            
            // Accélération/Décélération
            if (Input.GetKey(toucheAccelerer))
            {
                accelerateur += Time.deltaTime * 0.3f;
                toucheAppuyee = true;
                derniereEntree = "Accélérer";
            }
            else if (Input.GetKey(toucheRalentir))
            {
                accelerateur -= Time.deltaTime * 0.3f;
                toucheAppuyee = true;
                derniereEntree = "Ralentir";
            }
            
            // Limiter l'accélérateur entre 0.1 et 1.0
            accelerateur = Mathf.Clamp01(accelerateur);
            
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
            pitch -= altitudeDifference * forcePiloteAutomatique;
        }

        // Lissage des contrôles
        inputPitchActuel = Mathf.Lerp(inputPitchActuel, pitch * sensibiliteMonte, Time.deltaTime * stabilisationVitesse);
        inputRollActuel = Mathf.Lerp(inputRollActuel, roll * sensibiliteTourne, Time.deltaTime * stabilisationVitesse);
        inputYawActuel = Mathf.Lerp(inputYawActuel, yaw * sensibiliteTourne, Time.deltaTime * stabilisationVitesse);
    }

    void FixedUpdate()
    {
        // Calculer les angles de roulis et de tangage
        CalculerAnglesRoulisTangage();
        
        // Stabilisation automatique
        StabilisationAuto();
        
        // Calculer la vitesse avant
        CalculerVitesseAvant();
        
        // Calculer la traînée
        CalculerTrainee();
        
        // Effet aérodynamique
        CalculerEffetAerodynamique();
        
        // Calculer et appliquer les forces linéaires
        CalculerForcesLineaires();
        
        // Calculer et appliquer les forces de torsion
        CalculerForcesTorsion();
        
        // Gérer les caméras
        GererCameras();
    }
    
    void CalculerAnglesRoulisTangage()
    {
        // Calculer le vecteur avant à plat (sans composante Y)
        Vector3 avantPlat = transform.forward;
        avantPlat.y = 0;
        
        if (avantPlat.sqrMagnitude > 0)
        {
            avantPlat.Normalize();
            
            // Calculer l'angle de tangage actuel
            Vector3 avantPlatLocal = transform.InverseTransformDirection(avantPlat);
            AngleTangage = Mathf.Atan2(avantPlatLocal.y, avantPlatLocal.z);
            
            // Calculer l'angle d'inclinaison (roll) actuel
            Vector3 droitePlat = Vector3.Cross(Vector3.up, avantPlat);
            Vector3 droitePlatLocal = transform.InverseTransformDirection(droitePlat);
            AngleInclinaison = Mathf.Atan2(droitePlatLocal.y, droitePlatLocal.x);
        }
    }
    
    void StabilisationAuto()
    {
        // Le montant de virage incliné est le sinus de l'angle d'inclinaison
        montantVirageIncline = Mathf.Sin(AngleInclinaison);
        
        // Auto-stabilisation du roulis si pas d'entrée de roulis
        if (inputRollActuel == 0f)
        {
            inputRollActuel = -AngleInclinaison * stabilisationAutomatique * 0.2f;
        }
        
        // Auto-correction du tangage si pas d'entrée de tangage
        if (inputPitchActuel == 0f)
        {
            inputPitchActuel = -AngleTangage * stabilisationAutomatique * 0.2f;
            inputPitchActuel -= Mathf.Abs(montantVirageIncline * montantVirageIncline * 0.5f);
        }
    }
    
    void CalculerVitesseAvant()
    {
        // La vitesse avant est la vitesse dans la direction avant de l'avion
        Vector3 velociteLocale = transform.InverseTransformDirection(rb.linearVelocity);
        vitesseActuelle = Mathf.Max(0, velociteLocale.z);
        
        // Calculer la puissance du moteur
        PuissanceMoteur = accelerateur * vitesseMaximale;
    }
    
    void CalculerTrainee()
    {
        // Augmenter la traînée en fonction de la vitesse
        float traineeExtra = rb.linearVelocity.magnitude * facteurAugmentationTrainee;
        
        // Les freins à air augmentent considérablement la traînée
        rb.linearDamping = FreinAir ? (traineeDOrigine + traineeExtra) * 3f : traineeDOrigine + traineeExtra;
        
        // La vitesse avant affecte la traînée angulaire - à haute vitesse, il est plus difficile pour l'avion de tourner
        rb.angularDamping = traineeAngulaireDOrigine * vitesseActuelle;
    }
    
    void CalculerEffetAerodynamique()
    {
        // Calculs "aérodynamiques". C'est une approximation simple de l'effet selon lequel un avion
        // essaiera naturellement de s'aligner avec sa direction à grande vitesse.
        if (rb.linearVelocity.magnitude > 0)
        {
            // Comparer la direction dans laquelle nous pointons avec la direction de mouvement
            facteurAero = Vector3.Dot(transform.forward, rb.linearVelocity.normalized);
            facteurAero *= facteurAero; // Multiplié par lui-même pour une courbe de diminution souhaitable
            
            // Calculer une nouvelle vélocité en inclinant la direction actuelle vers
            // la direction de l'avion, selon facteurAero
            Vector3 nouvelleVelocite = Vector3.Lerp(rb.linearVelocity, transform.forward * vitesseActuelle,
                                          facteurAero * vitesseActuelle * effetAerodynamique * Time.deltaTime);
            rb.linearVelocity = nouvelleVelocite;

            // Tourner légèrement l'avion vers la direction du mouvement
            rb.rotation = Quaternion.Slerp(rb.rotation,
                                         Quaternion.LookRotation(rb.linearVelocity, transform.up),
                                         effetAerodynamique * Time.deltaTime);
        }
    }
    
    void CalculerForcesLineaires()
    {
        // Accumuler les forces dans cette variable
        Vector3 forces = Vector3.zero;
        
        // Ajouter la puissance du moteur dans la direction avant
        forces += PuissanceMoteur * transform.forward;
        
        // La direction de la force de portance est perpendiculaire à la vitesse de l'avion
        Vector3 directionPortance = Vector3.Cross(rb.linearVelocity, transform.right).normalized;
        
        // La portance diminue à mesure que l'avion augmente sa vitesse (en réalité, cela se produit lorsque le pilote rétracte les volets)
        float facteurPortanceZero = Mathf.InverseLerp(vitessePortanceOptimale, 0, vitesseActuelle);
        
        // Calculer et ajouter la force de portance
        float puissancePortance = vitesseActuelle * vitesseActuelle * coefficientPortance * facteurPortanceZero * facteurAero;
        forces += puissancePortance * directionPortance;
        
        // Appliquer les forces calculées au Rigidbody
        rb.AddForce(forces);
    }
    
    void CalculerForcesTorsion()
    {
        // Accumuler les forces de torsion dans cette variable
        Vector3 torsion = Vector3.zero;
        
        // Ajouter la torsion pour le tangage basée sur l'entrée de tangage
        torsion += inputPitchActuel * sensibiliteMonte * transform.right;
        
        // Ajouter la torsion pour le lacet basée sur l'entrée de lacet
        torsion += inputYawActuel * sensibiliteTourne * 0.2f * transform.up;
        
        // Ajouter la torsion pour le roulis basée sur l'entrée de roulis
        torsion += -inputRollActuel * sensibiliteTourne * transform.forward;
        
        // Ajouter la torsion pour le virage incliné
        torsion += montantVirageIncline * effetVirageIncline * transform.up;
        
        // La torsion totale est multipliée par la vitesse avant
        rb.AddTorque(torsion * vitesseActuelle * facteurAero);
        
        // Appliquer les corrections de dérive
        rb.AddRelativeTorque(correctionDeriveVector * 5000f * Time.deltaTime, ForceMode.Force);
        rb.AddRelativeTorque(correctionAxeZVector * 3000f * Time.deltaTime, ForceMode.Force);
        
        // Élévation directe (monter/descendre) indépendante de la rotation
        if (inputElevationDirecte != 0)
        {
            rb.AddForce(Vector3.up * inputElevationDirecte * 2000f * Time.deltaTime, ForceMode.Force);
        }
        
        // Stabilisation naturelle
        rb.AddRelativeTorque(
            -rb.angularVelocity.x * stabilisationVitesse * stabilisationAutomatique,
            -rb.angularVelocity.y * stabilisationVitesse * stabilisationAutomatique,
            -rb.angularVelocity.z * stabilisationVitesse * stabilisationAutomatique,
            ForceMode.Acceleration
        );
    }
    
    void GererCameras()
    {
        // Gérer la caméra si elle est assignée
        if (cameraAvion)
        {
            Vector3 positionCible = transform.position 
                                + (transform.up * hauteurCamera) 
                                + (transform.right * distanceCameraY) 
                                + (transform.forward * distanceCameraZ);
            
            cameraAvion.position = Vector3.Lerp(cameraAvion.position, positionCible, Time.deltaTime * vitesseLissageCam);
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
            GUI.Label(new Rect(10, 50, 300, 20), "Accélérateur: " + (accelerateur * 100).ToString("F0") + "%");
            GUI.Label(new Rect(10, 70, 300, 20), "Portance: " + (vitesseActuelle * vitesseActuelle * coefficientPortance * facteurAero).ToString("F1"));
            GUI.Label(new Rect(10, 90, 300, 20), "Altitude: " + transform.position.y.ToString("F1") + " / " + altitudeMaximale);
            GUI.Label(new Rect(10, 110, 300, 20), "Angle d'inclinaison: " + (AngleInclinaison * Mathf.Rad2Deg).ToString("F1") + "°");
            GUI.Label(new Rect(10, 130, 300, 20), "Angle de tangage: " + (AngleTangage * Mathf.Rad2Deg).ToString("F1") + "°");
        }
    }
    
    // Méthodes publiques
    
    // Immobiliser l'avion (par exemple, suite à une collision)
    public void Immobiliser()
    {
        immobilise = true;
        accelerateur = 0;
    }
    
    // Réinitialiser l'avion
    public void Reinitialiser()
    {
        immobilise = false;
        accelerateur = 0.5f;
    }
    
    // Obtenir l'altitude par rapport au terrain
    public float ObtenirAltitudeTerrain()
    {
        Ray ray = new Ray(transform.position - Vector3.up * 10, -Vector3.up);
        RaycastHit hit;
        return Physics.Raycast(ray, out hit) ? hit.distance + 10 : transform.position.y;
    }
} 