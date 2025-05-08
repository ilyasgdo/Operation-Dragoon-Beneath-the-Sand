using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Préfabs de montagnes")]
    public List<GameObject> prefabsMontagnes;
    
    [Header("Configuration de génération")]
    [Range(200f, 800f)] public float rayonVisibilite = 500f;
    [Range(50f, 200f)] public float distanceEntreMontagnes = 100f;
    [Range(3, 15)] public int densiteMontagnes = 8;
    
    [Header("Variation des montagnes")]
    [Tooltip("Conserver la taille originale du préfab")]
    public bool conserverTailleOriginale = true;
    [Range(0.3f, 3.0f)] public float largeurMinimale = 0.8f;
    [Range(0.5f, 5.0f)] public float largeurMaximale = 1.5f;
    [Range(0.3f, 3.0f)] public float epaisseurMinimale = 0.8f;
    [Range(0.5f, 5.0f)] public float epaisseurMaximale = 1.5f;
    [Range(0.5f, 2.0f)] public float hauteurFixe = 1.0f;
    [Range(0.0f, 0.5f)] public float variationTaille = 0.2f;

    [Header("Rotation")]
    [Tooltip("Appliquer une rotation de -90 degrés sur l'axe X")]
    public bool appliquerRotationX = true;
    public Vector3 rotationSpecifique = new Vector3(-90f, 0f, 0f);

    [Header("Performance")]
    public float intervalleVerification = 0.5f;
    [Range(10f, 100f)] public float distanceMinimaleJoueur = 30f;
    [Range(1, 5)] public int niveauLOD = 3;
    public bool utiliserObjectPooling = true;
    public int taillePoolParPrefab = 20;
    
    // Structures de données
    private Dictionary<Vector2Int, List<GameObject>> montagnePlacees = new Dictionary<Vector2Int, List<GameObject>>();
    private List<Vector2Int> cellulesCouvertes = new List<Vector2Int>();
    private Vector3 dernierePositionJoueur;
    private float tempsDepuisDerniereVerification = 0f;
    private Dictionary<int, Queue<GameObject>> objectPools = new Dictionary<int, Queue<GameObject>>();
    
    // Optimisation de la performance
    private Transform joueurTransform;
    
    // Stockage des échelles originales des préfabs
    private Dictionary<int, Vector3> echellesOriginales = new Dictionary<int, Vector3>();
    
    void Start()
    {
        if (prefabsMontagnes == null || prefabsMontagnes.Count == 0)
        {
            Debug.LogError("Aucun préfab de montagne n'a été assigné au générateur de terrain!");
            return;
        }
        
        // Stocker les échelles originales des préfabs
        for (int i = 0; i < prefabsMontagnes.Count; i++)
        {
            echellesOriginales[i] = prefabsMontagnes[i].transform.localScale;
        }
        
        // Trouver le joueur (généralement la caméra ou l'avion)
        joueurTransform = Camera.main?.transform;
        if (joueurTransform == null)
        {
            // Essayer de trouver l'avion
            var avion = FindObjectOfType<AvionController>();
            if (avion != null)
            {
                joueurTransform = avion.transform;
            }
            else
            {
                Debug.LogWarning("Impossible de trouver le joueur automatiquement. Le générateur utilisera sa propre position.");
                joueurTransform = transform;
            }
        }
        
        // Initialiser les pools d'objets si activé
        if (utiliserObjectPooling)
        {
            InitialiserObjectPools();
        }
        
        // Générer le terrain initial autour du joueur
        ActualiserPosition(joueurTransform.position);
    }
    
    void Update()
    {
        if (joueurTransform == null) return;
        
        tempsDepuisDerniereVerification += Time.deltaTime;
        
        // Limiter la fréquence des vérifications pour optimiser les performances
        if (tempsDepuisDerniereVerification >= intervalleVerification)
        {
            tempsDepuisDerniereVerification = 0f;
            
            // Calculer la distance parcourue depuis la dernière vérification
            float distanceParcourue = Vector3.Distance(dernierePositionJoueur, joueurTransform.position);
            
            // Actualiser uniquement si le joueur s'est déplacé significativement
            if (distanceParcourue > distanceEntreMontagnes * 0.2f)
            {
                ActualiserPosition(joueurTransform.position);
            }
        }
    }
    
    private void InitialiserObjectPools()
    {
        // Créer un conteneur pour les objets en pool
        GameObject poolContainer = new GameObject("MontagnePools");
        poolContainer.transform.parent = transform;
        
        // Créer un pool pour chaque préfab
        for (int i = 0; i < prefabsMontagnes.Count; i++)
        {
            Queue<GameObject> pool = new Queue<GameObject>();
            
            // Pré-instancier les objets
            for (int j = 0; j < taillePoolParPrefab; j++)
            {
                GameObject obj = Instantiate(prefabsMontagnes[i], Vector3.zero, Quaternion.identity);
                
                // Appliquer la rotation spécifique si demandé
                if (appliquerRotationX)
                {
                    obj.transform.rotation = Quaternion.Euler(rotationSpecifique);
                }
                
                obj.SetActive(false);
                obj.transform.parent = poolContainer.transform;
                pool.Enqueue(obj);
            }
            
            objectPools[i] = pool;
        }
    }
    
    private GameObject ObtenirMontagneDePool(int indexPrefab)
    {
        if (!utiliserObjectPooling || !objectPools.ContainsKey(indexPrefab))
        {
            GameObject obj = Instantiate(prefabsMontagnes[indexPrefab]);
            
            // Appliquer la rotation spécifique si demandé
            if (appliquerRotationX)
            {
                obj.transform.rotation = Quaternion.Euler(rotationSpecifique);
            }
            
            return obj;
        }
        
        // S'il reste des objets dans le pool, les utiliser
        Queue<GameObject> pool = objectPools[indexPrefab];
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        // Sinon, créer un nouvel objet
        GameObject newObj = Instantiate(prefabsMontagnes[indexPrefab]);
        
        // Appliquer la rotation spécifique si demandé
        if (appliquerRotationX)
        {
            newObj.transform.rotation = Quaternion.Euler(rotationSpecifique);
        }
        
        return newObj;
    }
    
    private void RetournerMontagneAuPool(GameObject montagne, int indexPrefab)
    {
        if (!utiliserObjectPooling || !objectPools.ContainsKey(indexPrefab)) 
        {
            Destroy(montagne);
            return;
        }
        
        montagne.SetActive(false);
        objectPools[indexPrefab].Enqueue(montagne);
    }
    
    public void ActualiserPosition(Vector3 positionJoueur)
    {
        dernierePositionJoueur = positionJoueur;
        
        // Calculer les cellules qui devraient être couvertes
        List<Vector2Int> nouvellesCellules = CalculerCellulesVisibles(positionJoueur);
        
        // Ajouter les nouvelles cellules
        foreach (Vector2Int cellule in nouvellesCellules)
        {
            if (!montagnePlacees.ContainsKey(cellule))
            {
                GenererMontagneDansCellule(cellule, positionJoueur);
            }
        }
        
        // Supprimer les cellules qui ne sont plus visibles
        List<Vector2Int> cellulesASupprimer = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, List<GameObject>> paire in montagnePlacees)
        {
            if (!nouvellesCellules.Contains(paire.Key))
            {
                cellulesASupprimer.Add(paire.Key);
            }
        }
        
        foreach (Vector2Int cellule in cellulesASupprimer)
        {
            if (montagnePlacees.TryGetValue(cellule, out List<GameObject> montagnes))
            {
                foreach (GameObject montagne in montagnes)
                {
                    if (utiliserObjectPooling)
                    {
                        // Trouver l'index du préfab pour ce montagne
                        for (int i = 0; i < prefabsMontagnes.Count; i++)
                        {
                            // Comparer par nom pour simplicité
                            if (montagne.name.StartsWith(prefabsMontagnes[i].name))
                            {
                                RetournerMontagneAuPool(montagne, i);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Destroy(montagne);
                    }
                }
            }
            montagnePlacees.Remove(cellule);
        }
        
        cellulesCouvertes = nouvellesCellules;
    }
    
    private List<Vector2Int> CalculerCellulesVisibles(Vector3 position)
    {
        List<Vector2Int> cellules = new List<Vector2Int>();
        int rayonCellule = Mathf.CeilToInt(rayonVisibilite / distanceEntreMontagnes);
        
        // Convertir la position 3D en cellule 2D (grille XZ)
        Vector2Int celluleCentrale = new Vector2Int(
            Mathf.FloorToInt(position.x / distanceEntreMontagnes),
            Mathf.FloorToInt(position.z / distanceEntreMontagnes)
        );
        
        // Ajouter toutes les cellules dans le rayon
        for (int x = -rayonCellule; x <= rayonCellule; x++)
        {
            for (int z = -rayonCellule; z <= rayonCellule; z++)
            {
                Vector2Int cellule = new Vector2Int(celluleCentrale.x + x, celluleCentrale.y + z);
                
                // Vérifier si la cellule est dans le rayon de visibilité
                Vector3 positionCellule = new Vector3(
                    cellule.x * distanceEntreMontagnes + distanceEntreMontagnes / 2,
                    0,
                    cellule.y * distanceEntreMontagnes + distanceEntreMontagnes / 2
                );
                
                float distance = Vector3.Distance(positionCellule, new Vector3(position.x, 0, position.z));
                if (distance <= rayonVisibilite)
                {
                    cellules.Add(cellule);
                }
            }
        }
        
        return cellules;
    }
    
    private void GenererMontagneDansCellule(Vector2Int cellule, Vector3 positionJoueur)
    {
        if (prefabsMontagnes.Count == 0) return;
        
        // Position de base de la cellule
        Vector3 positionCellule = new Vector3(
            cellule.x * distanceEntreMontagnes,
            0,
            cellule.y * distanceEntreMontagnes
        );
        
        List<GameObject> montagnesDansCellule = new List<GameObject>();
        List<Vector3> positionsUtilisees = new List<Vector3>();
        
        // Placement aléatoire dans la cellule
        for (int i = 0; i < densiteMontagnes; i++)
        {
            // Position aléatoire dans la cellule avec répartition plus uniforme
            Vector3 position;
            bool positionValide = false;
            int tentatives = 0;
            const int maxTentatives = 10;
            
            do {
                // Diviser la cellule en secteurs pour une meilleure répartition
                float subdivX = distanceEntreMontagnes / Mathf.Sqrt(densiteMontagnes);
                float subdivZ = distanceEntreMontagnes / Mathf.Sqrt(densiteMontagnes);
                
                // Trouver le secteur et ajouter une variation à l'intérieur
                float secteurX = (i % Mathf.Sqrt(densiteMontagnes)) * subdivX;
                float secteurZ = (i / Mathf.Sqrt(densiteMontagnes)) * subdivZ;
                
                position = new Vector3(
                    positionCellule.x + secteurX + Random.Range(0, subdivX),
                    0,
                    positionCellule.z + secteurZ + Random.Range(0, subdivZ)
                );
                
                // Vérifier si la position est suffisamment distante des autres montagnes
                positionValide = true;
                foreach (Vector3 posExistante in positionsUtilisees)
                {
                    if (Vector3.Distance(posExistante, position) < subdivX * 0.5f)
                    {
                        positionValide = false;
                        break;
                    }
                }
                
                tentatives++;
            } while (!positionValide && tentatives < maxTentatives);
            
            if (!positionValide) continue;
            positionsUtilisees.Add(position);
            
            // Distance par rapport au joueur
            float distanceAuJoueur = Vector3.Distance(position, positionJoueur);
            
            // Ne pas placer de montagne trop près du joueur
            if (distanceAuJoueur < distanceMinimaleJoueur) continue;
            
            // Sélectionner un préfab aléatoire
            int indexPrefab = Random.Range(0, prefabsMontagnes.Count);
            
            // Obtenir une montagne (du pool ou nouvelle instance)
            GameObject montagne;
            if (utiliserObjectPooling)
            {
                montagne = ObtenirMontagneDePool(indexPrefab);
            }
            else
            {
                montagne = Instantiate(prefabsMontagnes[indexPrefab]);
                
                // Appliquer la rotation spécifique si demandé
                if (appliquerRotationX)
                {
                    montagne.transform.rotation = Quaternion.Euler(rotationSpecifique);
                }
            }
            
            // Positionner la montagne
            montagne.transform.position = position;
            
            // Appliquer une rotation aléatoire autour de l'axe Y tout en conservant la rotation X
            Quaternion rotationY = Quaternion.Euler(
                appliquerRotationX ? rotationSpecifique.x : 0, 
                Random.Range(0, 360), 
                0
            );
            montagne.transform.rotation = rotationY;
            
            // Appliquer la mise à l'échelle
            if (conserverTailleOriginale)
            {
                // Récupérer l'échelle originale et appliquer une petite variation
                Vector3 echelleOriginale = echellesOriginales[indexPrefab];
                float variation = 1.0f + Random.Range(-variationTaille, variationTaille);
                
                // Conserver la hauteur d'origine mais varier la largeur et l'épaisseur
                float variationLargeur = Random.Range(0.8f, 1.2f);
                float variationEpaisseur = Random.Range(0.8f, 1.2f);
                
                Vector3 nouvelleEchelle = new Vector3(
                    echelleOriginale.x * variation * variationLargeur, 
                    echelleOriginale.y * variation, 
                    echelleOriginale.z * variation * variationEpaisseur
                );
                
                montagne.transform.localScale = nouvelleEchelle;
            }
            else
            {
                // Varier la largeur et l'épaisseur, mais garder la hauteur fixe
                float largeur = Random.Range(largeurMinimale, largeurMaximale);
                float epaisseur = Random.Range(epaisseurMinimale, epaisseurMaximale);
                montagne.transform.localScale = new Vector3(largeur, hauteurFixe, epaisseur);
            }
            
            // Configurer le LOD si disponible
            ConfigurerLOD(montagne, distanceAuJoueur);
            
            // Ajouter à la liste des montagnes dans cette cellule
            montagnesDansCellule.Add(montagne);
            
            // Transformer en enfant du générateur pour organisation
            montagne.transform.parent = transform;
        }
        
        // Stocker les montagnes générées pour cette cellule
        montagnePlacees[cellule] = montagnesDansCellule;
    }
    
    private void ConfigurerLOD(GameObject montagne, float distance)
    {
        // Configurer le LOD en fonction de la distance
        LODGroup lodGroup = montagne.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            // Ajuster les seuils de LOD en fonction de la distance
            float facteurDistance = Mathf.Clamp01(distance / rayonVisibilite);
            
            // Réduire la qualité pour les objets lointains
            if (facteurDistance > 0.7f)
            {
                lodGroup.ForceLOD(niveauLOD - 1);
            }
            else if (facteurDistance > 0.4f)
            {
                lodGroup.ForceLOD(Mathf.Max(1, niveauLOD - 2));
            }
            else
            {
                lodGroup.ForceLOD(0); // LOD de plus haute qualité
            }
        }
    }
} 