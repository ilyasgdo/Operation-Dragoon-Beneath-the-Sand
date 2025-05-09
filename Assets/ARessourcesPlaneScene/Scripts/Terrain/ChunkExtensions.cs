using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Extensions pour faciliter la gestion et la génération des chunks de terrain
/// </summary>
public static class ChunkExtensions
{
    /// <summary>
    /// Génère les données de heightmap pour un chunk en tâche asynchrone
    /// </summary>
    public static async Task<float[,]> GenererHeightmapAsync(Vector2Int coordonnees, int resolution, float echelle, float amplitude)
    {
        return await Task.Run(() => {
            float[,] heightmap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    float xCoord = (coordonnees.x * resolution + x) / echelle;
                    float zCoord = (coordonnees.y * resolution + z) / echelle;
                    
                    // Génération de terrain procédural avec du Perlin Noise
                    float hauteur = Mathf.PerlinNoise(xCoord, zCoord);
                    
                    // Ajouter plusieurs octaves pour plus de détails
                    hauteur += Mathf.PerlinNoise(xCoord * 2, zCoord * 2) * 0.5f;
                    hauteur += Mathf.PerlinNoise(xCoord * 4, zCoord * 4) * 0.25f;
                    
                    // Normaliser et appliquer l'amplitude
                    hauteur = hauteur / 1.75f * amplitude;
                    
                    heightmap[x, z] = hauteur;
                }
            }
            
            return heightmap;
        });
    }
    
    /// <summary>
    /// Crée un mesh à partir d'une heightmap
    /// </summary>
    public static Mesh CreerMeshDepuisHeightmap(float[,] heightmap, float tailleChunk, int lod = 0)
    {
        int resolution = heightmap.GetLength(0);
        
        // Ajuster la résolution en fonction du LOD
        int skip = lod + 1; // LOD 0 = pas de saut, LOD 1 = saut de 2, etc.
        int verticesParCote = (resolution - 1) / skip + 1;
        int nbVertices = verticesParCote * verticesParCote;
        
        // Créer les tableaux pour les données du mesh
        Vector3[] vertices = new Vector3[nbVertices];
        Vector2[] uvs = new Vector2[nbVertices];
        int[] triangles = new int[(verticesParCote - 1) * (verticesParCote - 1) * 6];
        
        // Remplir les vertices et UVs
        int index = 0;
        for (int z = 0; z < resolution; z += skip)
        {
            for (int x = 0; x < resolution; x += skip)
            {
                float y = heightmap[x, z];
                float percentX = (float)x / (resolution - 1);
                float percentZ = (float)z / (resolution - 1);
                
                // Conversion en coordonnées monde
                vertices[index] = new Vector3(
                    percentX * tailleChunk - tailleChunk / 2,
                    y,
                    percentZ * tailleChunk - tailleChunk / 2
                );
                
                uvs[index] = new Vector2(percentX, percentZ);
                index++;
            }
        }
        
        // Remplir les triangles
        int triangleIndex = 0;
        int indiceVertex = 0;
        
        for (int z = 0; z < verticesParCote - 1; z++)
        {
            for (int x = 0; x < verticesParCote - 1; x++)
            {
                int topLeft = indiceVertex;
                int topRight = indiceVertex + 1;
                int bottomLeft = indiceVertex + verticesParCote;
                int bottomRight = indiceVertex + verticesParCote + 1;
                
                // Premier triangle (en haut à gauche, en haut à droite, en bas à gauche)
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomLeft;
                
                // Deuxième triangle (en haut à droite, en bas à droite, en bas à gauche)
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = bottomLeft;
                
                indiceVertex++;
            }
            indiceVertex++;
        }
        
        // Créer et configurer le mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Recalculer les normales et tangentes
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        
        return mesh;
    }
    
    /// <summary>
    /// Crée un shader simplifié pour les objets lointains (LOD élevé)
    /// </summary>
    public static Material CreerShaderLODBas(Color couleurBase)
    {
        // Essayer d'abord avec URP
        Material mat = null;
        
        // Shader URP simplifié
        Shader shaderSimple = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shaderSimple != null)
        {
            mat = new Material(shaderSimple);
            mat.SetColor("_BaseColor", couleurBase);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
        }
        else 
        {
            // Fallback sur le shader standard
            mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", couleurBase);
            mat.SetFloat("_Glossiness", 0f);
            mat.SetFloat("_Metallic", 0f);
        }
        
        return mat;
    }
    
    /// <summary>
    /// Applique un niveau de LOD à tous les renderers d'un GameObject
    /// </summary>
    public static void AppliquerLODRenderers(GameObject obj, int niveauLOD, Material materiauLODBas = null)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Stocker le matériau original si ce n'est pas déjà fait
            if (niveauLOD > 1 && materiauLODBas != null)
            {
                MateriauTemporaire matTemp = renderer.gameObject.GetComponent<MateriauTemporaire>();
                
                if (matTemp == null)
                {
                    matTemp = renderer.gameObject.AddComponent<MateriauTemporaire>();
                    matTemp.materiauOriginal = renderer.sharedMaterial;
                }
                
                // Appliquer le matériau simplifié pour les LOD élevés
                if (niveauLOD == 2)
                {
                    renderer.material = materiauLODBas;
                }
                else
                {
                    // Restaurer le matériau original
                    renderer.material = matTemp.materiauOriginal;
                }
            }
        }
    }
    
    /// <summary>
    /// Vérifie si un chunk est visible par le joueur (occlusion culling)
    /// </summary>
    public static bool EstChunkVisible(Vector3 positionJoueur, Vector3 positionChunk, float hauteurChunk, LayerMask layerTerrain)
    {
        // Pour l'occlusion, vérifier plusieurs points du chunk
        Vector3[] pointsAVerifier = new Vector3[]
        {
            positionChunk,                              // Centre
            positionChunk + Vector3.up * hauteurChunk,  // Point haut
            new Vector3(positionChunk.x + 25f, positionChunk.y, positionChunk.z + 25f),  // Coin +x +z
            new Vector3(positionChunk.x - 25f, positionChunk.y, positionChunk.z + 25f),  // Coin -x +z
            new Vector3(positionChunk.x + 25f, positionChunk.y, positionChunk.z - 25f),  // Coin +x -z
            new Vector3(positionChunk.x - 25f, positionChunk.y, positionChunk.z - 25f)   // Coin -x -z
        };
        
        // Si au moins un point est visible, le chunk est considéré comme visible
        foreach (Vector3 point in pointsAVerifier)
        {
            Vector3 direction = point - positionJoueur;
            float distance = direction.magnitude;
            
            // Ne pas tester les points trop proches
            if (distance < 10f) return true;
            
            Ray ray = new Ray(positionJoueur, direction.normalized);
            RaycastHit hit;
            
            // Si aucun obstacle, ou si le premier obstacle est notre point, c'est visible
            if (!Physics.Raycast(ray, out hit, distance, layerTerrain) || 
                Vector3.Distance(hit.point, point) < 5f)
            {
                return true;
            }
        }
        
        // Aucun point n'est visible
        return false;
    }
    
    /// <summary>
    /// Prédit les prochains chunks à charger en fonction de la direction et vitesse
    /// </summary>
    public static List<Vector2Int> PredireChunksFuturs(Vector3 position, Vector3 direction, float vitesse, 
                                                    float tempsPrecharge, float tailleChunk)
    {
        List<Vector2Int> chunks = new List<Vector2Int>();
        
        // Convertir la position actuelle en coordonnées de chunk
        Vector2Int chunkActuel = new Vector2Int(
            Mathf.FloorToInt(position.x / tailleChunk),
            Mathf.FloorToInt(position.z / tailleChunk)
        );
        
        // Ajouter le chunk actuel
        chunks.Add(chunkActuel);
        
        // Calculer la distance à précharger en fonction de la vitesse
        float distancePrecharge = vitesse * tempsPrecharge;
        int nbChunksPrecharge = Mathf.CeilToInt(distancePrecharge / tailleChunk);
        
        // Limiter le nombre de chunks à précharger
        nbChunksPrecharge = Mathf.Min(nbChunksPrecharge, 5);
        
        // Calculer les chunks futurs
        for (int i = 1; i <= nbChunksPrecharge; i++)
        {
            // Position future
            Vector3 posFuture = position + direction.normalized * (i * tailleChunk);
            
            // Convertir en coordonnées de chunk
            Vector2Int chunkFutur = new Vector2Int(
                Mathf.FloorToInt(posFuture.x / tailleChunk),
                Mathf.FloorToInt(posFuture.z / tailleChunk)
            );
            
            // Ajouter si pas déjà présent
            if (!chunks.Contains(chunkFutur))
            {
                chunks.Add(chunkFutur);
            }
            
            // Ajouter également les chunks voisins pour un mouvement plus fluide
            if (i <= 2) // Ajouter des voisins seulement pour les chunks proches
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && z == 0) continue; // Sauter le chunk central
                        
                        Vector2Int voisin = new Vector2Int(chunkFutur.x + x, chunkFutur.y + z);
                        if (!chunks.Contains(voisin))
                        {
                            chunks.Add(voisin);
                        }
                    }
                }
            }
        }
        
        return chunks;
    }
}

/// <summary>
/// Composant pour stocker temporairement le matériau original d'un renderer
/// </summary>
public class MateriauTemporaire : MonoBehaviour
{
    public Material materiauOriginal;
} 