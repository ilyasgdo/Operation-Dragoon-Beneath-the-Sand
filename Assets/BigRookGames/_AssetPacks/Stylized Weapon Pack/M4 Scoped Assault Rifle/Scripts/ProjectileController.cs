using UnityEngine;

namespace BigRookGames.Weapons
{
    public class ProjectileController : MonoBehaviour
    {
        // --- Config ---
        [Tooltip("Vitesse du projectile en unités par seconde")]
        public float projectileSpeed = 100.0f;
        
        [Tooltip("Durée de vie du projectile en secondes avant destruction automatique")]
        public float lifetime = 5.0f;
        
        [Tooltip("Dégâts causés par le projectile")]
        public float damage = 10.0f;
        
        // --- Effets ---
        [Tooltip("Effet à instancier lors de l'impact")]
        public GameObject impactEffect;
        
        [Tooltip("Traînée du projectile pour améliorer la visibilité")]
        public bool useTrail = true;
        public Color trailColor = Color.yellow;
        public float trailTime = 0.5f;
        
        // --- Références privées ---
        private Rigidbody rb;
        private TrailRenderer trail;
        
        private void Awake()
        {
            // Obtenir ou ajouter un Rigidbody
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // Configurer le Rigidbody
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Ajouter une traînée pour améliorer la visibilité si elle n'existe pas déjà
            if (useTrail)
            {
                trail = GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = gameObject.AddComponent<TrailRenderer>();
                    trail.time = trailTime;
                    trail.startWidth = 0.1f;
                    trail.endWidth = 0.0f;
                    trail.startColor = trailColor;
                    trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0);
                    trail.material = new Material(Shader.Find("Sprites/Default"));
                }
            }
            
            // S'assurer que le projectile a un collider
            if (GetComponent<Collider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
                collider.isTrigger = false;
            }
            
            // Détruire le projectile après sa durée de vie
            Destroy(gameObject, lifetime);
        }
        
        private void Start()
        {
            // Appliquer la vitesse initiale dans la direction avant du projectile
            // Utiliser la rotation actuelle du projectile pour déterminer la direction
            rb.velocity = transform.forward * projectileSpeed;
            
            // Ajouter un effet visuel pour rendre le projectile plus visible
            if (GetComponent<Renderer>() == null)
            {
                // Ajouter un MeshRenderer si aucun n'existe
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
                MeshFilter filter = sphere.GetComponent<MeshFilter>();
                
                // Copier le mesh et le material sur notre projectile
                gameObject.AddComponent<MeshFilter>().mesh = filter.mesh;
                MeshRenderer projectileRenderer = gameObject.AddComponent<MeshRenderer>();
                projectileRenderer.material = renderer.material;
                projectileRenderer.material.color = trailColor;
                
                // Ajuster la taille du projectile
                transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // Détruire l'objet temporaire
                Destroy(sphere);
            }
            
            // Ajouter un effet de lumière pour améliorer la visibilité
            Light light = GetComponent<Light>();
            if (light == null)
            {
                light = gameObject.AddComponent<Light>();
                light.color = trailColor;
                light.intensity = 2.0f;
                light.range = 2.0f;
            }
            
            // Afficher un message de débogage pour confirmer que le projectile a été créé
            Debug.Log("Projectile créé avec une vitesse de " + projectileSpeed);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Calculer le point d'impact approximatif et la normale
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            
            HandleImpact(other.gameObject, hitPoint, hitNormal);
        }
        
        private void HandleImpact(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Instancier l'effet d'impact si défini
            if (impactEffect != null)
            {
                Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            }
            else
            {
                // Créer un effet d'impact par défaut si aucun n'est défini
                GameObject impactSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                impactSphere.transform.position = hitPoint;
                impactSphere.transform.rotation = Quaternion.LookRotation(hitNormal);
                impactSphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // Ajouter un matériau coloré
                Renderer renderer = impactSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
                
                // Détruire l'effet après un court délai
                Destroy(impactSphere, 1.0f);
            }
            
            // Appliquer des dégâts si la cible a un composant approprié
            // Exemple pour un système de santé générique
            var healthComponent = hitObject.GetComponent<MonoBehaviour>();
            if (healthComponent != null)
            {
                // Essayer d'appeler une méthode TakeDamage si elle existe
                var takeDamageMethod = healthComponent.GetType().GetMethod("TakeDamage");
                if (takeDamageMethod != null)
                {
                    takeDamageMethod.Invoke(healthComponent, new object[] { damage });
                    Debug.Log("Dégâts appliqués à " + hitObject.name + ": " + damage);
                }
            }
            
            // Afficher un message de débogage pour confirmer l'impact
            Debug.Log("Projectile a touché: " + hitObject.name);
            
            // Détruire le projectile après l'impact
            Destroy(gameObject);
        }
        
        private void Update()
        {
            // Visualiser la trajectoire du projectile en mode débogage
            Debug.DrawRay(transform.position, rb.velocity.normalized * 2.0f, Color.red);
            
            // Vérifier si le projectile se déplace correctement
            if (rb.velocity.magnitude < projectileSpeed * 0.5f)
            {
                // Si la vitesse est trop basse, réappliquer la vitesse initiale
                rb.velocity = transform.forward * projectileSpeed;
                Debug.LogWarning("Vitesse du projectile corrigée");
            }
        }
    }
}