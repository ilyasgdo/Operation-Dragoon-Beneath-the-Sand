using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crée et configure automatiquement un matériau d'eau basé sur le shader SimpleWaterShader.
/// Ce script peut être attaché au générateur de terrain ou exécuté dans l'éditeur.
/// </summary>
[ExecuteInEditMode]
public class CreateWaterMaterial : MonoBehaviour
{
    public Material waterMaterial;
    public bool generateMaterialAtStart = true;
    public bool forceRegenerateNow = false;
    
    [Header("Paramètres de l'eau")]
    public Color waterColor = new Color(0.2f, 0.5f, 0.7f, 0.8f);
    public float waveSpeed = 1.0f;
    public float waveScale = 1.0f;
    public float distortionScale = 0.1f;
    public float reflectionStrength = 0.5f;
    public float glossiness = 0.8f;
    
    [Header("Textures")]
    public Texture2D mainTexture;
    public Texture2D normalMapTexture;
    
    void Start()
    {
        if (generateMaterialAtStart && waterMaterial == null)
        {
            GenerateWaterMaterial();
        }
    }
    
    void Update()
    {
        if (forceRegenerateNow)
        {
            GenerateWaterMaterial();
            forceRegenerateNow = false;
        }
    }
    
    public void GenerateWaterMaterial()
    {
        // Essayer d'abord d'utiliser notre shader URP
        Shader waterShader = Shader.Find("Custom/SimpleWaterShaderURP");
        
        // Si le shader URP n'est pas trouvé, rechercher l'autre shader
        if (waterShader == null)
        {
            waterShader = Shader.Find("Custom/SimpleWaterShader");
        }
        
        // Si aucun des shaders personnalisés n'est trouvé, utiliser un shader URP standard
        if (waterShader == null)
        {
            Debug.LogWarning("Shaders d'eau personnalisés introuvables. Utilisation d'un shader URP standard.");
            waterShader = Shader.Find("Universal Render Pipeline/Lit");
            
            if (waterShader == null)
            {
                // Si nous n'avons même pas le shader URP standard, essayer le shader standard
                Debug.LogWarning("Shader URP/Lit introuvable. Tentative d'utilisation du shader Standard.");
                waterShader = Shader.Find("Standard");
                
                if (waterShader == null)
                {
                    Debug.LogError("Impossible de trouver un shader utilisable. Vérifiez votre installation Unity.");
                    return;
                }
            }
        }
        
        waterMaterial = new Material(waterShader);
        
        // Si on utilise un shader URP standard
        if (waterShader.name == "Universal Render Pipeline/Lit")
        {
            // Configurer le shader URP/Lit pour ressembler à de l'eau
            waterMaterial.SetFloat("_Surface", 1); // Mode transparent (0=opaque, 1=transparent)
            waterMaterial.SetFloat("_Blend", 0); // Alpha blend (0=SrcAlpha OneMinusSrcAlpha)
            waterMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            
            // Appliquer les propriétés adaptées au shader URP
            waterMaterial.SetColor("_BaseColor", new Color(waterColor.r, waterColor.g, waterColor.b, 0.7f));
            waterMaterial.SetFloat("_Smoothness", glossiness);
            waterMaterial.SetFloat("_Metallic", 0.5f);
            
            Debug.Log("Matériau d'eau créé avec le shader URP/Lit");
        }
        // Si on utilise le shader standard
        else if (waterShader.name == "Standard")
        {
            // Configurer le shader standard pour ressembler à de l'eau
            waterMaterial.SetFloat("_Mode", 3); // Mode transparent
            waterMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMaterial.SetInt("_ZWrite", 0);
            waterMaterial.DisableKeyword("_ALPHATEST_ON");
            waterMaterial.EnableKeyword("_ALPHABLEND_ON");
            waterMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMaterial.renderQueue = 3000;
            
            // Appliquer les propriétés adaptées au shader standard
            waterMaterial.SetColor("_Color", new Color(waterColor.r, waterColor.g, waterColor.b, 0.7f));
            waterMaterial.SetFloat("_Glossiness", glossiness);
            waterMaterial.SetFloat("_Metallic", 0.5f);
            
            Debug.Log("Matériau d'eau créé avec le shader Standard");
        }
        else
        {
            // Configurer les propriétés pour notre shader personnalisé
            waterMaterial.SetColor("_Color", waterColor);
            waterMaterial.SetFloat("_WaveSpeed", waveSpeed);
            waterMaterial.SetFloat("_WaveScale", waveScale);
            waterMaterial.SetFloat("_DistortionScale", distortionScale);
            waterMaterial.SetFloat("_ReflectionStrength", reflectionStrength);
            
            // Pour le shader URP personnalisé, utiliser _Smoothness au lieu de _Glossiness
            if (waterShader.name == "Custom/SimpleWaterShaderURP")
            {
                waterMaterial.SetFloat("_Smoothness", glossiness);
                Debug.Log("Matériau d'eau créé avec le shader personnalisé SimpleWaterShaderURP");
            }
            else
            {
                waterMaterial.SetFloat("_Glossiness", glossiness);
                Debug.Log("Matériau d'eau créé avec le shader personnalisé SimpleWaterShader");
            }
            
            // Assigner les textures si fournies
            if (mainTexture != null)
            {
                waterMaterial.SetTexture("_MainTex", mainTexture);
            }
            
            if (normalMapTexture != null)
            {
                waterMaterial.SetTexture("_NormalMap", normalMapTexture);
            }
            else
            {
                // Créer une normal map par défaut si aucune n'est fournie
                CreateDefaultNormalMap();
            }
        }
        
        // Attacher au générateur de terrain si présent
        TerrainGenerator terrainGen = GetComponent<TerrainGenerator>();
        if (terrainGen != null)
        {
            terrainGen.materielEau = waterMaterial;
            Debug.Log("Matériau d'eau assigné au générateur de terrain.");
        }
        else
        {
            Debug.Log("Matériau d'eau créé. Vous pouvez l'assigner manuellement au générateur de terrain.");
        }
    }
    
    private void CreateDefaultNormalMap()
    {
        // Créer une normal map par défaut (bruit de Perlin)
        int resolution = 256;
        normalMapTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        normalMapTexture.name = "DefaultWaterNormal";
        
        Color[] pixels = new Color[resolution * resolution];
        
        // Générer un bruit de Perlin
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Coordonnées normalisées
                float u = (float)x / resolution;
                float v = (float)y / resolution;
                
                // Plusieurs couches de bruit
                float noise1 = Mathf.PerlinNoise(u * 5f, v * 5f) * 0.5f;
                float noise2 = Mathf.PerlinNoise(u * 10f, v * 10f) * 0.25f;
                float noise3 = Mathf.PerlinNoise(u * 20f, v * 20f) * 0.125f;
                
                float combinedNoise = noise1 + noise2 + noise3;
                
                // Convertir en normal map (R, G = directions XY, B = 1, A = 1)
                float gradX = (Mathf.PerlinNoise(u + 0.01f, v) - Mathf.PerlinNoise(u, v)) * 10f;
                float gradY = (Mathf.PerlinNoise(u, v + 0.01f) - Mathf.PerlinNoise(u, v)) * 10f;
                
                // Normaliser et convertir de [-1,1] à [0,1] pour le stockage
                Vector3 normal = new Vector3(gradX, gradY, 1f).normalized;
                
                pixels[y * resolution + x] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z,
                    1f
                );
            }
        }
        
        normalMapTexture.SetPixels(pixels);
        normalMapTexture.Apply();
        
        // Assigner la texture générée au matériau
        waterMaterial.SetTexture("_NormalMap", normalMapTexture);
    }
} 