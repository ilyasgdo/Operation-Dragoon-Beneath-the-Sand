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

    [Header("Aérodynamique")]
    public float coefficientTrainee = 0.1f;
    public float coefficientPortance = 0.5f;
    public float vitesseMinimale = 10f;
    public float vitesseDecrochage = 15f;
    public float facteurInertie = 0.8f;
    public float tempsReponseAccelerateur = 2f;
    public float tempsReponseFrein = 1.5f;
    public float facteurTurbulence = 0.05f;
    public float frequenceTurbulence = 1f;
    public float amplitudeTurbulence = 0.2f;
    public float facteurGivrage = 0f;
    public float vitesseGivrage = 0.1f;
    public float altitudeGivrage = 100f;
    public float temperatureGivrage = 0f;

    [Header("Roulis et virages")]
    public float angleRoulisMax = 45f;
    public float vitesseRoulis = 2f;
    public float facteurRoulisVitesse = 0.5f;
    public float facteurRoulisAltitude = 0.3f;
    public float stabilisationRoulis = 1f;
    public float facteurDerapage = 0.2f;
    public float facteurGlissement = 0.1f;

    [Header("Dommages et résistance")]
    public float santeMaximale = 100f;
    public float santeActuelle = 100f;
    public float resistanceStructure = 0.8f;
    public float facteurDommageVitesse = 0.01f;
    public float facteurDommageCollision = 10f;
    public float tempsReparation = 5f;
    public bool enReparation = false;

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
    public AudioSource sonTurbulence;
    public AudioSource sonDommage;
    public AudioSource sonGivrage;
    public ParticleSystem particulesGivrage;
    public ParticleSystem particulesDommage;
    public Light lumiereDommage;

    [Header("Caméra")]
    public Transform cameraAvion;
    public float distanceCameraZ = -10f;
    public float hauteurCamera = 2f;
    public float vitesseLissageCam = 5f;
    public float amplitudeSecousse = 0.1f;
    public float frequenceSecousse = 10f;

    // Variables privées
    private Rigidbody rb;
    private float vitesseActuelle;
    private float accelerateur = 0.5f;
    private float accelerateurCible = 0.5f;
    private bool controlsEnabled = true;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float vitesseRotationActuelle = 0f;
    private float vitesseRotationCible = 0f;
    private float vitessePrecedente;
    private Vector3 directionPrecedente;
    private bool enDecrochage = false;
    private float tempsDecrochage = 0f;
    private float tempsTurbulence = 0f;
    private float tempsReparationRestant = 0f;
    private float tempsSecousse = 0f;
    private Vector3 positionCameraInitiale;
    private Quaternion rotationCameraInitiale;
    private float facteurDommage = 1f;
    private float facteurGivrageActuel = 0f;
    private float tempsGivrage = 0f;
    private bool givrageActif = false;
    private float temperatureActuelle = 20f;
    private float humiditeActuelle = 0.5f;
    private float angleRoulisActuel = 0f;
    private float angleRoulisCible = 0f;
    private float facteurDerapageActuel = 0f;
    private float facteurGlissementActuel = 0f;

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
        accelerateurCible = 0.5f;
        vitessePrecedente = vitesseActuelle;
        directionPrecedente = transform.forward;
        santeActuelle = santeMaximale;

        // Orientation initiale vers l'axe X positif
        transform.rotation = Quaternion.LookRotation(Vector3.right);
        
        if (sonMoteur)
        {
            sonMoteur.loop = true;
            sonMoteur.Play();
        }
        
        // Initialiser la caméra
        if (cameraAvion)
        {
            positionCameraInitiale = cameraAvion.localPosition;
            rotationCameraInitiale = cameraAvion.localRotation;
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
        if (controlsEnabled && !enReparation)
        {
            // Accélération/Décélération avec Espace et Ctrl
            if (Input.GetKey(KeyCode.Space))
            {
                accelerateurCible = Mathf.Min(1f, accelerateurCible + Time.deltaTime / tempsReponseAccelerateur);
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                accelerateurCible = Mathf.Max(0f, accelerateurCible - Time.deltaTime / tempsReponseFrein);
            }
            else
            {
                // Retour progressif à la position neutre
                accelerateurCible = Mathf.MoveTowards(accelerateurCible, 0.5f, Time.deltaTime / tempsReponseAccelerateur);
            }

            // Appliquer l'accélération avec inertie
            accelerateur = Mathf.Lerp(accelerateur, accelerateurCible, Time.deltaTime * 2f);

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
                
                // Calculer l'angle de roulis cible en fonction de la direction du virage
                float facteurRoulis = Mathf.Clamp01(vitesseActuelle / vitesseMaximale) * facteurRoulisVitesse;
                facteurRoulis *= (1f - (transform.position.y / 200f) * facteurRoulisAltitude);
                angleRoulisCible = -mouseX * angleRoulisMax * facteurRoulis;
                
                // Calculer le dérapage en fonction de l'angle de roulis
                facteurDerapageActuel = Mathf.Abs(angleRoulisActuel) / angleRoulisMax * facteurDerapage;
                
                // Calculer le glissement en fonction de la vitesse et de l'angle de roulis
                facteurGlissementActuel = (vitesseActuelle / vitesseMaximale) * (Mathf.Abs(angleRoulisActuel) / angleRoulisMax) * facteurGlissement;
            }
            else
            {
                vitesseRotationCible = Mathf.MoveTowards(vitesseRotationCible, 0f, decelerationRotation * Time.deltaTime);
                
                // Retour progressif à l'angle de roulis neutre
                angleRoulisCible = Mathf.MoveTowards(angleRoulisCible, 0f, stabilisationRoulis * Time.deltaTime);
                
                // Réduire progressivement le dérapage et le glissement
                facteurDerapageActuel = Mathf.MoveTowards(facteurDerapageActuel, 0f, stabilisationRoulis * Time.deltaTime);
                facteurGlissementActuel = Mathf.MoveTowards(facteurGlissementActuel, 0f, stabilisationRoulis * Time.deltaTime);
            }

            // Appliquer la rotation avec inertie
            vitesseRotationActuelle = Mathf.Lerp(vitesseRotationActuelle, vitesseRotationCible, Time.deltaTime * 5f);
            rotationY += vitesseRotationActuelle;

            // Limiter la rotation verticale
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -80f, 80f);

            // Appliquer l'angle de roulis avec inertie
            angleRoulisActuel = Mathf.Lerp(angleRoulisActuel, angleRoulisCible, Time.deltaTime * vitesseRoulis);

            // Appliquer la rotation avec l'axe X comme direction principale et inclure le roulis
            Quaternion rotation = Quaternion.Euler(rotationX, rotationY, angleRoulisActuel);
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

        // Gestion de la réparation
        if (enReparation)
        {
            tempsReparationRestant -= Time.deltaTime;
            if (tempsReparationRestant <= 0f)
            {
                enReparation = false;
                santeActuelle = Mathf.Min(santeMaximale, santeActuelle + 20f);
                if (santeActuelle >= santeMaximale)
                {
                    santeActuelle = santeMaximale;
                    facteurDommage = 1f;
                }
            }
        }

        // Mise à jour de la caméra avec secousse
        if (cameraAvion)
        {
            tempsSecousse += Time.deltaTime * frequenceSecousse;
            float secousseX = Mathf.Sin(tempsSecousse) * amplitudeSecousse * (1f - santeActuelle / santeMaximale);
            float secousseY = Mathf.Cos(tempsSecousse * 1.2f) * amplitudeSecousse * (1f - santeActuelle / santeMaximale);
            
            Vector3 positionCible = transform.position + transform.forward * distanceCameraZ + Vector3.up * hauteurCamera;
            positionCible += new Vector3(secousseX, secousseY, 0f);
            
            cameraAvion.position = Vector3.Lerp(cameraAvion.position, positionCible, Time.deltaTime * vitesseLissageCam);
            cameraAvion.LookAt(transform.position + transform.forward * 10f);
        }

        // Mise à jour des effets visuels
        if (lumiereDommage)
        {
            lumiereDommage.intensity = Mathf.Lerp(0f, 2f, 1f - santeActuelle / santeMaximale);
        }

        if (particulesDommage)
        {
            var emission = particulesDommage.emission;
            emission.rateOverTime = 10f * (1f - santeActuelle / santeMaximale);
        }

        if (particulesGivrage && givrageActif)
        {
            var emission = particulesGivrage.emission;
            emission.rateOverTime = 20f * facteurGivrageActuel;
        }
    }

    void FixedUpdate()
    {
        // Calculer la vitesse actuelle avec inertie
        float vitesseCible = vitesseMaximale * accelerateur * facteurDommage * (1f - facteurGivrageActuel * 0.5f);
        vitesseActuelle = Mathf.Lerp(vitesseActuelle, vitesseCible, Time.deltaTime * acceleration);

        // Vérifier le décrochage
        float angleAttaque = Vector3.Angle(transform.forward, rb.linearVelocity.normalized);
        if (vitesseActuelle < vitesseDecrochage && angleAttaque > 30f)
        {
            enDecrochage = true;
            tempsDecrochage += Time.deltaTime;
        }
        else
        {
            enDecrochage = false;
            tempsDecrochage = 0f;
        }

        // Calculer la turbulence
        tempsTurbulence += Time.deltaTime * frequenceTurbulence;
        float turbulenceX = Mathf.Sin(tempsTurbulence) * amplitudeTurbulence;
        float turbulenceY = Mathf.Cos(tempsTurbulence * 1.3f) * amplitudeTurbulence;
        float turbulenceZ = Mathf.Sin(tempsTurbulence * 0.7f) * amplitudeTurbulence;
        Vector3 turbulence = new Vector3(turbulenceX, turbulenceY, turbulenceZ) * facteurTurbulence * (1f - santeActuelle / santeMaximale);

        // Appliquer la force de propulsion dans la direction de l'avion
        Vector3 force = transform.forward * vitesseActuelle;
        
        // Ajouter la traînée aérodynamique
        Vector3 trainee = -rb.linearVelocity.normalized * (rb.linearVelocity.sqrMagnitude * coefficientTrainee);
        
        // Ajouter la portance
        float facteurPortance = Mathf.Clamp01((vitesseActuelle - vitesseMinimale) / (vitesseMaximale - vitesseMinimale));
        Vector3 portance = Vector3.up * facteurPortance * coefficientPortance * vitesseActuelle;
        
        // Appliquer les forces
        rb.AddForce(force + trainee + portance + turbulence, ForceMode.Acceleration);

        // Stabilisation automatique avec influence de la vitesse
        float facteurStabilisation = stabilisation * (1f + (vitesseActuelle / vitesseMaximale));
        Vector3 stabilisationForce = -rb.linearVelocity * facteurStabilisation;
        rb.AddForce(stabilisationForce, ForceMode.Acceleration);

        // Correction de la dérive latérale avec dérapage
        Vector3 vitesseLaterale = Vector3.Project(rb.linearVelocity, transform.right);
        rb.AddForce(-vitesseLaterale * (correctionDerive + facteurDerapageActuel), ForceMode.Acceleration);

        // Appliquer le glissement latéral
        if (Mathf.Abs(angleRoulisActuel) > 1f)
        {
            Vector3 directionGlissement = transform.right * Mathf.Sign(angleRoulisActuel);
            rb.AddForce(directionGlissement * facteurGlissementActuel * vitesseActuelle, ForceMode.Acceleration);
        }

        // Appliquer l'inertie de direction
        if (vitesseActuelle > vitesseMinimale)
        {
            Vector3 directionCible = transform.forward;
            Vector3 directionActuelle = rb.linearVelocity.normalized;
            Vector3 nouvelleDirection = Vector3.Lerp(directionActuelle, directionCible, facteurInertie * Time.deltaTime);
            
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                rb.linearVelocity = nouvelleDirection * rb.linearVelocity.magnitude;
            }
        }

        // Mise à jour des variables pour le prochain frame
        vitessePrecedente = vitesseActuelle;
        directionPrecedente = transform.forward;

        // Gestion du givrage
        UpdateGivrage();
    }

    void UpdateGivrage()
    {
        // Calculer la température en fonction de l'altitude
        temperatureActuelle = 20f - (transform.position.y / 100f) * 0.65f;
        
        // Calculer l'humidité en fonction de l'altitude et de la température
        humiditeActuelle = Mathf.Clamp01(0.5f + (transform.position.y / 200f) - (temperatureActuelle / 40f));
        
        // Vérifier les conditions de givrage
        bool conditionsGivrage = temperatureActuelle < temperatureGivrage && humiditeActuelle > 0.7f && transform.position.y > altitudeGivrage;
        
        if (conditionsGivrage)
        {
            if (!givrageActif)
            {
                givrageActif = true;
                if (sonGivrage && !sonGivrage.isPlaying)
                {
                    sonGivrage.Play();
                }
                if (particulesGivrage && !particulesGivrage.isPlaying)
                {
                    particulesGivrage.Play();
                }
            }
            
            facteurGivrageActuel = Mathf.Min(1f, facteurGivrageActuel + Time.deltaTime * vitesseGivrage);
        }
        else
        {
            if (givrageActif)
            {
                givrageActif = false;
                if (sonGivrage && sonGivrage.isPlaying)
                {
                    sonGivrage.Stop();
                }
                if (particulesGivrage && particulesGivrage.isPlaying)
                {
                    particulesGivrage.Stop();
                }
            }
            
            facteurGivrageActuel = Mathf.Max(0f, facteurGivrageActuel - Time.deltaTime * vitesseGivrage * 2f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Calculer les dommages en fonction de la vitesse d'impact
        float vitesseImpact = collision.relativeVelocity.magnitude;
        float dommages = vitesseImpact * facteurDommageCollision;
        
        // Appliquer les dommages
        santeActuelle = Mathf.Max(0f, santeActuelle - dommages);
        facteurDommage = Mathf.Lerp(0.5f, 1f, santeActuelle / santeMaximale);
        
        // Jouer le son de dommage
        if (sonDommage)
        {
            sonDommage.volume = Mathf.Clamp01(dommages / 10f);
            sonDommage.Play();
        }
        
        // Activer les particules de dommage
        if (particulesDommage)
        {
            particulesDommage.Play();
        }
        
        // Si les dommages sont importants, activer la réparation
        if (dommages > 5f && !enReparation)
        {
            enReparation = true;
            tempsReparationRestant = tempsReparation;
        }
    }

    void OnGUI()
    {
        // Afficher les informations de contrôle
        GUI.Label(new Rect(10, 10, 400, 200), 
            "Contrôles:\n" +
            "- Souris : Orientation de l'avion\n" +
            "- Espace : Accélérer\n" +
            "- Ctrl : Ralentir\n" +
            "- P : Activer/Désactiver les contrôles\n\n" +
            "Vitesse: " + vitesseActuelle.ToString("F1") + "\n" +
            "Accélérateur: " + (accelerateur * 100).ToString("F0") + "%\n" +
            "Altitude: " + transform.position.y.ToString("F1") + "\n" +
            "État: " + (enDecrochage ? "DÉCROCHAGE!" : "Normal") + "\n" +
            "Santé: " + santeActuelle.ToString("F0") + "/" + santeMaximale.ToString("F0") + "\n" +
            "Givrage: " + (facteurGivrageActuel * 100).ToString("F0") + "%\n" +
            "Température: " + temperatureActuelle.ToString("F1") + "°C\n" +
            "Humidité: " + (humiditeActuelle * 100).ToString("F0") + "%\n" +
            (enReparation ? "Réparation en cours: " + tempsReparationRestant.ToString("F1") + "s" : ""));
    }
} 