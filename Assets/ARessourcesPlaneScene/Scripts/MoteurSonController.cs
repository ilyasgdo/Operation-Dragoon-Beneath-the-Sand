using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoteurSonController : MonoBehaviour
{
    [Header("Sources Audio")]
    public AudioSource sonMoteurIdle;
    public AudioSource sonMoteurAcceleration;
    public AudioSource sonMoteurCroisiere;
    
    [Header("Configuration")]
    public float vitesseMinCroisiere = 40f;
    public float vitesseTransition = 20f;
    public float delaiTransition = 0.5f;
    
    private AvionController controleurAvion;
    private float volumeIdle = 1f;
    private float volumeAcceleration = 0f;
    private float volumeCroisiere = 0f;
    
    void Start()
    {
        controleurAvion = GetComponentInParent<AvionController>();
        
        if (controleurAvion == null)
        {
            Debug.LogError("MoteurSonController: Aucun AvionController n'a été trouvé sur le parent!");
            enabled = false;
            return;
        }
        
        // Initialiser les sources audio
        if (sonMoteurIdle)
        {
            sonMoteurIdle.loop = true;
            sonMoteurIdle.volume = volumeIdle;
            sonMoteurIdle.Play();
        }
        
        if (sonMoteurAcceleration)
        {
            sonMoteurAcceleration.loop = true;
            sonMoteurAcceleration.volume = volumeAcceleration;
            sonMoteurAcceleration.Play();
        }
        
        if (sonMoteurCroisiere)
        {
            sonMoteurCroisiere.loop = true;
            sonMoteurCroisiere.volume = volumeCroisiere;
            sonMoteurCroisiere.Play();
        }
    }
    
    void Update()
    {
        if (controleurAvion == null) return;
        
        // Obtenir la vitesse actuelle
        float vitesseActuelle = controleurAvion.vitesseActuelle;
        
        // Calculer les volumes cibles
        float cibleIdle = 0f;
        float cibleAcceleration = 0f;
        float cibleCroisiere = 0f;
        
        if (vitesseActuelle < vitesseTransition)
        {
            // Au ralenti ou basse vitesse
            cibleIdle = 1.0f - (vitesseActuelle / vitesseTransition);
            cibleAcceleration = vitesseActuelle / vitesseTransition;
        }
        else if (vitesseActuelle < vitesseMinCroisiere)
        {
            // Accélération
            cibleAcceleration = 1.0f - ((vitesseActuelle - vitesseTransition) / (vitesseMinCroisiere - vitesseTransition));
            cibleCroisiere = (vitesseActuelle - vitesseTransition) / (vitesseMinCroisiere - vitesseTransition);
        }
        else
        {
            // Vitesse de croisière
            cibleCroisiere = 1.0f;
        }
        
        // Transition en douceur des volumes
        volumeIdle = Mathf.Lerp(volumeIdle, cibleIdle, Time.deltaTime / delaiTransition);
        volumeAcceleration = Mathf.Lerp(volumeAcceleration, cibleAcceleration, Time.deltaTime / delaiTransition);
        volumeCroisiere = Mathf.Lerp(volumeCroisiere, cibleCroisiere, Time.deltaTime / delaiTransition);
        
        // Appliquer les volumes aux sources audio
        if (sonMoteurIdle) sonMoteurIdle.volume = volumeIdle;
        if (sonMoteurAcceleration) sonMoteurAcceleration.volume = volumeAcceleration;
        if (sonMoteurCroisiere) sonMoteurCroisiere.volume = volumeCroisiere;
        
        // Ajuster le pitch en fonction de la vitesse
        float pitchFactor = 0.8f + (vitesseActuelle / controleurAvion.vitesseMaximale) * 0.4f;
        
        if (sonMoteurIdle) sonMoteurIdle.pitch = pitchFactor * 0.8f;
        if (sonMoteurAcceleration) sonMoteurAcceleration.pitch = pitchFactor;
        if (sonMoteurCroisiere) sonMoteurCroisiere.pitch = pitchFactor;
    }
} 