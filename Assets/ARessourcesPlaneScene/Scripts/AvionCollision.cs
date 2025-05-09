using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AvionCollision : MonoBehaviour
{
    [Header("Paramètres de collision")]
    public float vitesseMinimaleImpact = 20f;   // Vitesse minimale pour considérer un impact comme un crash
    public float delaiAvantReinitialisation = 3f;  // Délai en secondes avant de recharger la scène
    public string nomSceneInitiale = "FirstScene"; // Nom de la scène de départ à charger

    [Header("Effets de crash")]
    public GameObject prefabExplosion;          // Préfab contenant les particules d'explosion
    public AudioClip sonExplosion;              // Son d'explosion
    [Range(0f, 1f)]
    public float volumeSonExplosion = 1.0f;     // Volume du son d'explosion
    public bool desactiverControles = true;     // Désactiver les contrôles après collision
    public bool ralentirTemps = true;           // Ralentir le temps après l'explosion
    public float facteurRalentissement = 0.3f;  // Facteur de ralentissement du temps (0-1)

    [Header("Débris")]
    public GameObject[] prefabsDebris;          // Préfabs de débris à instancier
    public int nombreDebrisMin = 5;             // Nombre minimum de débris
    public int nombreDebrisMax = 10;            // Nombre maximum de débris
    public float forceMinimaleDebris = 5f;      // Force minimale appliquée aux débris
    public float forceMaximaleDebris = 15f;     // Force maximale appliquée aux débris

    // Variables privées
    private bool enCollision = false;
    private Rigidbody rb;
    private AvionController controllerAvion;
    private AudioSource audioSource;
    private List<Renderer> renderersAvion = new List<Renderer>();
    private List<Collider> collidersAvion = new List<Collider>();

    void Start()
    {
        // Récupérer les composants nécessaires
        rb = GetComponent<Rigidbody>();
        controllerAvion = GetComponent<AvionController>();
        
        // Créer un AudioSource pour les sons si nécessaire
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Récupérer tous les renderers et colliders de l'avion et ses enfants
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            renderersAvion.Add(r);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            collidersAvion.Add(c);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Vérifier si nous ne sommes pas déjà en collision
        if (enCollision) return;

        // Vérifier si l'impact est assez fort
        if (rb.linearVelocity.magnitude > vitesseMinimaleImpact)
        {
            // Marquer que nous sommes en collision
            enCollision = true;
            
            // Déclencher l'explosion et la réinitialisation
            StartCoroutine(GererCrash(collision.contacts[0].point));
        }
    }

    IEnumerator GererCrash(Vector3 positionImpact)
    {
        // Désactiver les contrôles de l'avion
        if (desactiverControles && controllerAvion != null)
        {
            controllerAvion.enabled = false;
        }

        // Ralentir le temps
        if (ralentirTemps)
        {
            Time.timeScale = facteurRalentissement;
        }

        // Créer l'explosion
        if (prefabExplosion != null)
        {
            GameObject explosion = Instantiate(prefabExplosion, positionImpact, Quaternion.identity);
            
            // Détruire l'effet d'explosion après quelques secondes
            Destroy(explosion, delaiAvantReinitialisation);
        }

        // Jouer le son d'explosion
        if (sonExplosion != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonExplosion, volumeSonExplosion);
        }

        // Créer des débris
        CreerDebris(positionImpact);

        // Cacher l'avion
        foreach (Renderer r in renderersAvion)
        {
            r.enabled = false;
        }

        // Désactiver les colliders
        foreach (Collider c in collidersAvion)
        {
            c.enabled = false;
        }

        // Désactiver le rigidbody
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Attendre quelques secondes avant de réinitialiser
        yield return new WaitForSecondsRealtime(delaiAvantReinitialisation);

        // Restaurer le temps normal
        Time.timeScale = 1.0f;

        // Recharger la scène
        SceneManager.LoadScene(nomSceneInitiale);
    }

    void CreerDebris(Vector3 position)
    {
        if (prefabsDebris == null || prefabsDebris.Length == 0) return;

        // Déterminer combien de débris créer
        int nombreDebris = Random.Range(nombreDebrisMin, nombreDebrisMax + 1);

        for (int i = 0; i < nombreDebris; i++)
        {
            // Choisir un préfab de débris aléatoire
            int indexPrefab = Random.Range(0, prefabsDebris.Length);
            GameObject prefabDebris = prefabsDebris[indexPrefab];

            if (prefabDebris != null)
            {
                // Instancier le débris
                GameObject debris = Instantiate(prefabDebris, position, Random.rotation);
                
                // Ajouter une force aléatoire
                Rigidbody rbDebris = debris.GetComponent<Rigidbody>();
                if (rbDebris != null)
                {
                    Vector3 direction = Random.onUnitSphere;
                    float force = Random.Range(forceMinimaleDebris, forceMaximaleDebris);
                    rbDebris.AddForce(direction * force, ForceMode.Impulse);
                    
                    // Ajouter une rotation aléatoire
                    rbDebris.AddTorque(Random.onUnitSphere * force * 0.5f, ForceMode.Impulse);
                }
                
                // Détruire le débris après quelques secondes
                Destroy(debris, delaiAvantReinitialisation * 0.95f);
            }
        }
    }
} 