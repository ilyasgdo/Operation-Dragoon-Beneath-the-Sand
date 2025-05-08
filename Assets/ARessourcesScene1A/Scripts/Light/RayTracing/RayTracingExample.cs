using UnityEngine;

[ExecuteInEditMode]
public class RayTracingExample : MonoBehaviour
{
    [Header("Références")]
    public Material rayTracingMaterial;

    [Header("Textures")]
    public Texture2D groundTexture;
    public Texture2D sphere1Texture;
    public Texture2D sphere2Texture;
    public bool useTextures = true;

    [Header("Paramètres de Ray Tracing")]
    [Range(8, 256)]
    public int maxSteps = 64;
    [Range(50, 200)]
    public float maxDistance = 100f;
    [Range(0.001f, 0.1f)]
    public float hitDistance = 0.01f;
    [Range(0f, 1f)]
    public float reflectionStrength = 0.5f;

    [Header("Sphères")]
    [Range(0.1f, 3f)]
    public float sphereRadius = 1f;
    public Vector3 sphere1Position = new Vector3(0f, 1f, 0f);
    public Vector3 sphere2Position = new Vector3(2f, 1f, 2f);
    public Color sphere1Color = Color.red;
    public Color sphere2Color = Color.blue;

    [Header("Éclairage")]
    public Color lightColor = Color.white;
    public Color ambientColor = new Color(0.2f, 0.2f, 0.3f);
    [Range(0f, 1f)]
    public float shadowIntensity = 0.75f;
    [Range(1f, 64f)]
    public float shadowSoftness = 16f;
    [Range(1f, 128f)]
    public float specularPower = 32f;
    [Range(0f, 1f)]
    public float specularIntensity = 0.5f;

    [Header("Effets Atmosphériques")]
    [Range(0f, 0.05f)]
    public float fogDensity = 0.01f;
    public Color fogColor = new Color(0.75f, 0.85f, 1f);

    [Header("Animation")]
    public bool animateSpheres = false;
    public float animationSpeed = 1f;
    public bool animateLight = false;
    public float lightAnimationSpeed = 0.5f;

    private float animTime = 0f;

    private void Awake()
    {
        // Assurez-vous que le material existe
        if (rayTracingMaterial == null)
        {
            Debug.LogError("RayTracingMaterial non assigné dans l'inspecteur!");
        }
    }

    private void Update()
    {
        if (rayTracingMaterial == null) return;

        // Mise à jour des paramètres de base
        rayTracingMaterial.SetInt("_MaxSteps", maxSteps);
        rayTracingMaterial.SetFloat("_MaxDistance", maxDistance);
        rayTracingMaterial.SetFloat("_HitDistance", hitDistance);
        rayTracingMaterial.SetFloat("_ReflectionStrength", reflectionStrength);
        rayTracingMaterial.SetFloat("_SphereRadius", sphereRadius);

        // Associer les textures au matériau
        if (useTextures)
        {
            // Activer l'utilisation des textures dans le shader
            rayTracingMaterial.EnableKeyword("_USE_TEXTURES");
            
            // Assigner les textures
            if (groundTexture != null)
                rayTracingMaterial.SetTexture("_GroundTexture", groundTexture);
            
            if (sphere1Texture != null)
                rayTracingMaterial.SetTexture("_Sphere1Texture", sphere1Texture);
            
            if (sphere2Texture != null)
                rayTracingMaterial.SetTexture("_Sphere2Texture", sphere2Texture);
        }
        else
        {
            // Désactiver l'utilisation des textures
            rayTracingMaterial.DisableKeyword("_USE_TEXTURES");
        }

        // Mise à jour des paramètres d'éclairage
        rayTracingMaterial.SetColor("_LightColor", lightColor);
        rayTracingMaterial.SetColor("_AmbientColor", ambientColor);
        rayTracingMaterial.SetFloat("_ShadowIntensity", shadowIntensity);
        rayTracingMaterial.SetFloat("_ShadowSoftness", shadowSoftness);
        rayTracingMaterial.SetFloat("_SpecularPower", specularPower);
        rayTracingMaterial.SetFloat("_SpecularIntensity", specularIntensity);

        // Mise à jour des effets atmosphériques
        rayTracingMaterial.SetFloat("_FogDensity", fogDensity);
        rayTracingMaterial.SetColor("_FogColor", fogColor);

        // Animation
        animTime += Time.deltaTime * animationSpeed;
        
        // Animation des sphères
        if (animateSpheres)
        {
            // Déplacement circulaire des sphères
            float radius1 = 2f;
            float radius2 = 3f;
            
            sphere1Position = new Vector3(
                Mathf.Sin(animTime) * radius1,
                1f,
                Mathf.Cos(animTime) * radius1
            );
            
            sphere2Position = new Vector3(
                Mathf.Sin(animTime * 0.7f) * radius2,
                1f,
                Mathf.Cos(animTime * 0.7f) * radius2
            );
        }

        // Animation de la lumière
        if (animateLight)
        {
            // Direction de lumière qui tourne lentement
            float lightX = Mathf.Sin(animTime * lightAnimationSpeed);
            float lightZ = Mathf.Cos(animTime * lightAnimationSpeed);
            Vector3 lightDir = new Vector3(lightX, 1f, lightZ).normalized;
            
            rayTracingMaterial.SetVector("_LightDirection", lightDir);
        }
        else
        {
            // Lumière directionnelle par défaut
            rayTracingMaterial.SetVector("_LightDirection", new Vector3(1f, 1f, 1f).normalized);
        }

        // Mise à jour des positions et couleurs des sphères
        rayTracingMaterial.SetVector("_SpherePos1", new Vector4(sphere1Position.x, sphere1Position.y, sphere1Position.z, 0f));
        rayTracingMaterial.SetVector("_SpherePos2", new Vector4(sphere2Position.x, sphere2Position.y, sphere2Position.z, 0f));
        rayTracingMaterial.SetColor("_SphereColor1", sphere1Color);
        rayTracingMaterial.SetColor("_SphereColor2", sphere2Color);
    }
} 