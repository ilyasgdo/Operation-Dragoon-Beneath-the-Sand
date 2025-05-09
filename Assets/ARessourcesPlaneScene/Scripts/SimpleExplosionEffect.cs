using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleExplosionEffect : MonoBehaviour
{
    [Header("Configuration Particules")]
    public ParticleSystem particulesExplosion;
    public ParticleSystem particulesFeu;
    public ParticleSystem particulesFumee;
    
    [Header("Configuration Lumière")]
    public Light lumiereExplosion;
    public float intensiteMaxLumiere = 8f;
    public float dureeFlash = 0.2f;
    public float dureeLumiereRésiduelle = 2f;
    
    [Header("Sons")]
    public AudioClip[] sonsExplosion;
    public float volumeMaximal = 1.0f;
    
    private AudioSource sourceAudio;
    
    void Start()
    {
        // Récupérer ou créer l'AudioSource
        sourceAudio = GetComponent<AudioSource>();
        if (sourceAudio == null)
        {
            sourceAudio = gameObject.AddComponent<AudioSource>();
        }
        
        // Configurer l'AudioSource
        sourceAudio.spatialBlend = 1.0f; // Son 3D
        sourceAudio.rolloffMode = AudioRolloffMode.Linear;
        sourceAudio.minDistance = 5f;
        sourceAudio.maxDistance = 100f;
        
        // Démarrer l'explosion
        DemarrerExplosion();
    }
    
    void DemarrerExplosion()
    {
        // Activer les systèmes de particules
        if (particulesExplosion != null)
        {
            particulesExplosion.Play();
        }
        
        if (particulesExplosion != null)
        {
            particulesFeu.Play();
        }
        
        if (particulesFumee != null)
        {
            particulesFumee.Play();
        }
        
        // Jouer un son d'explosion aléatoire
        if (sonsExplosion != null && sonsExplosion.Length > 0 && sourceAudio != null)
        {
            int indexSon = Random.Range(0, sonsExplosion.Length);
            sourceAudio.PlayOneShot(sonsExplosion[indexSon], volumeMaximal);
        }
        
        // Gérer l'effet de lumière
        if (lumiereExplosion != null)
        {
            StartCoroutine(EffetLumiereExplosion());
        }
    }
    
    IEnumerator EffetLumiereExplosion()
    {
        // Configurer la lumière
        lumiereExplosion.intensity = intensiteMaxLumiere;
        lumiereExplosion.enabled = true;
        
        // Attendre la durée du flash
        yield return new WaitForSeconds(dureeFlash);
        
        // Effet de fondu de la lumière
        float tempsEcoule = 0f;
        float intensiteInitiale = intensiteMaxLumiere;
        
        while (tempsEcoule < dureeLumiereRésiduelle)
        {
            tempsEcoule += Time.deltaTime;
            float ratio = 1.0f - (tempsEcoule / dureeLumiereRésiduelle);
            lumiereExplosion.intensity = intensiteInitiale * ratio;
            yield return null;
        }
        
        // Désactiver la lumière
        lumiereExplosion.enabled = false;
    }
    
    // Permet de créer facilement un effet d'explosion à une position donnée
    public static GameObject CreerExplosion(GameObject prefabExplosion, Vector3 position, float taille = 1.0f)
    {
        if (prefabExplosion == null) return null;
        
        GameObject nouvelleExplosion = Instantiate(prefabExplosion, position, Quaternion.identity);
        nouvelleExplosion.transform.localScale = Vector3.one * taille;
        
        return nouvelleExplosion;
    }
} 