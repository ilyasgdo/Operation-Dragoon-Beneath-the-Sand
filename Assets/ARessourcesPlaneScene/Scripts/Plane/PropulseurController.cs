using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PropulseurController : MonoBehaviour
{
    [Header("Configuration")]
    public float vitesseRotationMax = 3000f;
    public float accelerationRotation = 500f;
    public float decelerationRotation = 200f;
    public Vector3 axeRotation = Vector3.forward;
    
    [Header("Effets")]
    public ParticleSystem effetAir;
    public float intensiteMinEffet = 0.2f;
    public float intensiteMaxEffet = 1.0f;
    
    private float vitesseRotationActuelle = 0f;
    private AvionController controleurAvion;
    
    void Start()
    {
        controleurAvion = GetComponentInParent<AvionController>();
        
        if (controleurAvion == null)
        {
            Debug.LogWarning("PropulseurController: Aucun AvionController n'a été trouvé sur le parent. Vitesse contrôlée manuellement.");
        }
        
        // Désactiver l'effet d'air au démarrage
        if (effetAir != null)
        {
            var emission = effetAir.emission;
            emission.enabled = false;
        }
    }
    
    void Update()
    {
        float vitesseCible = 0f;
        
        if (controleurAvion != null)
        {
            // La vitesse de rotation est proportionnelle à la vitesse de l'avion
            vitesseCible = (controleurAvion.VitesseActuelle / controleurAvion.vitesseMaximale) * vitesseRotationMax;
        }
        else
        {
            // Sans contrôleur d'avion, utiliser une valeur fixe (pour les tests)
            vitesseCible = vitesseRotationMax * 0.7f;
        }
        
        // Accélération ou décélération progressive
        if (vitesseRotationActuelle < vitesseCible)
        {
            vitesseRotationActuelle += accelerationRotation * Time.deltaTime;
            if (vitesseRotationActuelle > vitesseCible)
            {
                vitesseRotationActuelle = vitesseCible;
            }
        }
        else if (vitesseRotationActuelle > vitesseCible)
        {
            vitesseRotationActuelle -= decelerationRotation * Time.deltaTime;
            if (vitesseRotationActuelle < vitesseCible)
            {
                vitesseRotationActuelle = vitesseCible;
            }
        }
        
        // Appliquer la rotation
        transform.Rotate(axeRotation, vitesseRotationActuelle * Time.deltaTime);
        
        // Gérer les effets de particules
        if (effetAir != null)
        {
            var emission = effetAir.emission;
            
            if (vitesseRotationActuelle > 100f)
            {
                if (!emission.enabled)
                {
                    emission.enabled = true;
                    effetAir.Play();
                }
                
                // Ajuster l'intensité des particules
                float intensiteNormalisee = Mathf.Clamp01(vitesseRotationActuelle / vitesseRotationMax);
                float intensiteEffet = Mathf.Lerp(intensiteMinEffet, intensiteMaxEffet, intensiteNormalisee);
                
                var emissionRate = effetAir.emission.rateOverTime;
                emissionRate.constant = 50f * intensiteEffet;
                emission.rateOverTime = emissionRate;
                
                // Ajuster la vitesse des particules
                var mainModule = effetAir.main;
                mainModule.startSpeed = 5f * intensiteEffet;
            }
            else if (emission.enabled)
            {
                emission.enabled = false;
                effetAir.Stop();
            }
        }
    }
} 