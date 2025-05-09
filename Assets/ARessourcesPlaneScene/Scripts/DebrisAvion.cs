using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DebrisAvion : MonoBehaviour
{
    [Header("Paramètres physiques")]
    public float resistanceAir = 0.2f;         // Résistance de l'air pour ralentir le débris
    public float forceFumee = 0.5f;            // Force de la trainée de fumée (0-1)
    public bool activerRotationAleatoire = true;
    public Vector2 vitesseRotationMinMax = new Vector2(1f, 5f);

    [Header("Apparence")]
    public bool ajouterFlammes = false;        // Activer les flammes sur ce débris
    public bool ajouterFumee = true;           // Activer la fumée sur ce débris
    public Color couleurFlamme = new Color(1f, 0.5f, 0.1f, 0.8f);
    public Color couleurFumee = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    [Header("Effets")]
    public GameObject prefabFlamme;            // Préfab optionnel pour les flammes
    public GameObject prefabFumee;             // Préfab optionnel pour la fumée
    public ParticleSystem particulesFlammes;   // Alternative au préfab pour les flammes
    public ParticleSystem particulesFumee;     // Alternative au préfab pour la fumée

    // Variables privées
    private Rigidbody rb;
    private Vector3 axeRotation;
    private float vitesseRotation;
    private List<GameObject> effetsCrees = new List<GameObject>();
    
    void Start()
    {
        // Récupérer le Rigidbody
        rb = GetComponent<Rigidbody>();
        
        // Configurer le Rigidbody pour un comportement réaliste
        if (rb != null)
        {
            rb.linearDamping = resistanceAir;
            rb.angularDamping = resistanceAir * 0.5f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Définir une rotation aléatoire
        if (activerRotationAleatoire)
        {
            axeRotation = Random.onUnitSphere;
            vitesseRotation = Random.Range(vitesseRotationMinMax.x, vitesseRotationMinMax.y);
        }
        
        // Créer les effets
        if (ajouterFlammes)
        {
            CreerEffetFlammes();
        }
        
        if (ajouterFumee)
        {
            CreerEffetFumee();
        }
    }
    
    void Update()
    {
        // Appliquer la rotation continue si activée
        if (activerRotationAleatoire && rb != null)
        {
            transform.Rotate(axeRotation, vitesseRotation * Time.deltaTime * 60f);
        }
    }
    
    void CreerEffetFlammes()
    {
        // Utiliser un système de particules existant si disponible
        if (particulesFlammes != null)
        {
            particulesFlammes.Play();
            return;
        }
        
        // Sinon, utiliser le préfab
        if (prefabFlamme != null)
        {
            GameObject flamme = Instantiate(prefabFlamme, transform.position, Quaternion.identity, transform);
            effetsCrees.Add(flamme);
            
            // Configurer les particules pour qu'elles semblent plus réalistes
            ParticleSystem ps = flamme.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = couleurFlamme;
                main.startLifetime = Random.Range(0.5f, 1.5f);
                main.startSize = Random.Range(0.5f, 1.5f);
            }
        }
        else
        {
            // Créer un système de particules simple si aucun n'est fourni
            GameObject flamme = new GameObject("FlammeDebris");
            flamme.transform.SetParent(transform);
            flamme.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = flamme.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = couleurFlamme;
            main.startLifetime = Random.Range(0.5f, 1.5f);
            main.startSize = Random.Range(0.5f, 1.5f);
            main.startSpeed = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            effetsCrees.Add(flamme);
        }
    }
    
    void CreerEffetFumee()
    {
        // Utiliser un système de particules existant si disponible
        if (particulesFumee != null)
        {
            particulesFumee.Play();
            return;
        }
        
        // Sinon, utiliser le préfab
        if (prefabFumee != null)
        {
            GameObject fumee = Instantiate(prefabFumee, transform.position, Quaternion.identity, transform);
            effetsCrees.Add(fumee);
            
            // Configurer les particules pour qu'elles semblent plus réalistes
            ParticleSystem ps = fumee.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = couleurFumee;
                main.startLifetime = Random.Range(1.5f, 3f);
                main.startSize = Random.Range(1f, 2f) * forceFumee;
            }
        }
        else
        {
            // Créer un système de particules simple si aucun n'est fourni
            GameObject fumee = new GameObject("FumeeDebris");
            fumee.transform.SetParent(transform);
            fumee.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = fumee.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = couleurFumee;
            main.startLifetime = Random.Range(1.5f, 3f);
            main.startSize = Random.Range(1f, 2f) * forceFumee;
            main.startSpeed = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            effetsCrees.Add(fumee);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Réduire la vitesse à chaque impact pour un effet plus réaliste
        if (rb != null)
        {
            rb.linearVelocity *= 0.8f;
            rb.angularVelocity *= 0.9f;
        }
        
        // Jouer un son d'impact si nécessaire
        // (on pourrait ajouter un AudioSource et des sons d'impact ici)
    }
    
    void OnDestroy()
    {
        // Détruire tous les effets créés par ce script
        foreach (GameObject effet in effetsCrees)
        {
            if (effet != null)
            {
                Destroy(effet);
            }
        }
    }
} 