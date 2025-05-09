using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Système de Chunks")]
    [Range(64f, 256f)] public float tailleChunk = 128f;
    [Range(3, 8)] public int chunksFrontaux = 4;
    [Range(1, 5)] public int chunksLateraux = 2;
    [Range(200f, 1000f)] public float distanceVisibilite = 500f;
    
    [Header("Préfabs de montagnes")]
    public List<GameObject> prefabsMontagnes;
    
    [Header("Configuration de génération")]
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

    [Header("Plan d'eau")]
    public bool activerPlanEau = true;
    public Material materielEau;
    public float niveauEau = -5f;
    public float tailleEau = 10000f;
    [Tooltip("Si activé, le plan d'eau reste centré sur le joueur, sinon il reste à position fixe")]
    public bool eauSuitJoueur = false;
    [Tooltip("Créer des bordures autour du monde")]
    public bool creerBordureMonde = true;
    [Tooltip("Distance des bordures depuis le centre")]
    public float distanceBordure = 5000f;
    [Tooltip("Hauteur des bordures")]
    public float hauteurBordure = 500f;
    private GameObject planEau;
    private GameObject[] bordures;

    [Header("Rotation")]
    [Tooltip("Appliquer une rotation de -90 degrés sur l'axe X")]
    public bool appliquerRotationX = true;
    public Vector3 rotationSpecifique = new Vector3(-90f, 0f, 0f);

    [Header("Performance")]
    public float intervalleVerification = 0.5f;
    [Range(10f, 100f)] public float distanceMinimaleJoueur = 30f;
    public bool utiliserObjectPooling = true;
    public int taillePoolParPrefab = 20;
    public int chunksEnPooling = 30;
    
    // Paramètres LOD
    [Header("LOD")]
    [Range(0, 1000)] public float distanceLOD0 = 200f; // Haute qualité
    [Range(0, 1500)] public float distanceLOD1 = 400f; // Qualité moyenne
    [Range(0, 2000)] public float distanceLOD2 = 800f; // Basse qualité

    [Header("Optimisations Avancées")]
    [Tooltip("Priorité au chargement des chunks dans la direction du vol")]
    public bool prechargerDirectionVol = true;
    [Tooltip("Distance supplémentaire de préchargement (en chunks)")]
    [Range(1, 5)] public int distancePrecharge = 2;
    [Tooltip("Activer le culling d'occlusion pour les objets distants")]
    public bool activerOcclusionCulling = true;
    [Tooltip("Distance à partir de laquelle les objets peuvent être cachés (occlusion)")]
    public float distanceOcclusion = 1000f;
    [Tooltip("Activer le cache de chunks déjà visités")]
    public bool activerCacheChunks = true;
    [Tooltip("Taille maximale du cache (nombre de chunks)")]
    [Range(10, 100)] public int tailleCacheChunks = 30;
    [Tooltip("Utiliser des shaders plus légers pour les objets lointains")]
    public bool utiliserShadersLegers = true;
    [Tooltip("Matériau à utiliser pour les objets lointains")]
    public Material materiauLODBas;
    
    // Structure pour stocker les information d'un chunk
    public class Chunk
    {
        public Vector2Int coordonnees;
        public GameObject conteneur;
        public List<GameObject> objets = new List<GameObject>();
        public int niveauLOD = 0;
        public bool estActif = false;
        public float distanceAuJoueur = 0f;
        public bool estGenere = false;
        public bool estOcclus = false;
        
        public Chunk(Vector2Int coords)
        {
            coordonnees = coords;
        }
    }
    
    // Structures de données
    private Dictionary<Vector2Int, Chunk> chunksActifs = new Dictionary<Vector2Int, Chunk>();
    private Queue<Chunk> chunksPool = new Queue<Chunk>();
    private Dictionary<int, Queue<GameObject>> objectPools = new Dictionary<int, Queue<GameObject>>();
    
    // Cache de chunks
    private Dictionary<Vector2Int, Chunk> chunksCache = new Dictionary<Vector2Int, Chunk>();
    private Queue<Vector2Int> chunksOrdreCache = new Queue<Vector2Int>();
    
    // Optimisation de la performance
    private Transform joueurTransform;
    private Vector3 dernierePositionJoueur;
    private Vector2Int dernierChunkJoueur;
    private float tempsDepuisDerniereVerification = 0f;
    private Vector3 directionAvion = Vector3.forward;
    private float vitesseAvion = 0f;
    private List<Material> materiauxOriginaux = new List<Material>();

    // Cache de données
    private Dictionary<int, Vector3> echellesOriginales = new Dictionary<int, Vector3>();
    
    // Pour le multithreading
    private bool generationEnCours = false;
    private List<Vector2Int> chunksPrioritaires = new List<Vector2Int>();

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
            
            // Stocker également les matériaux originaux pour le système de LOD de shader
            if (utiliserShadersLegers)
            {
                Renderer renderer = prefabsMontagnes[i].GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    materiauxOriginaux.Add(renderer.sharedMaterial);
                }
                else
                {
                    materiauxOriginaux.Add(null);
                }
            }
        }
        
        // Créer un matériau LOD bas si non fourni et si l'option est activée
        if (utiliserShadersLegers && materiauLODBas == null)
        {
            materiauLODBas = CreerMaterialLODBas();
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
        
        // Créer le conteneur pour tous les chunks
        GameObject chunkParent = new GameObject("Chunks");
        chunkParent.transform.parent = transform;
        
        // Initialiser les pools d'objets
        if (utiliserObjectPooling)
        {
            InitialiserObjectPools();
            InitialiserChunkPool(chunkParent.transform);
        }
        
        // Initialiser la position du joueur
        dernierePositionJoueur = joueurTransform.position;
        dernierChunkJoueur = PositionVersChunk(dernierePositionJoueur);
        
        // Créer le plan d'eau
        if (activerPlanEau)
        {
            CreerPlanEau();
        }
        
        // Générer le terrain initial autour du joueur
        ActualiserChunks();
    }
    
    void Update()
    {
        if (joueurTransform == null) return;
        
        tempsDepuisDerniereVerification += Time.deltaTime;
        
        // Vérifier uniquement à intervalles réguliers pour optimiser les performances
        if (tempsDepuisDerniereVerification >= intervalleVerification && !generationEnCours)
        {
            tempsDepuisDerniereVerification = 0f;
            
            // Estimer la direction et la vitesse de l'avion
            Vector3 nouvellePosition = joueurTransform.position;
            Vector3 deplacement = nouvellePosition - dernierePositionJoueur;
            
            if (deplacement.magnitude > 0.1f)
            {
                directionAvion = deplacement.normalized;
                vitesseAvion = deplacement.magnitude / intervalleVerification;
            }
            
            // Si le plan d'eau est actif et doit suivre le joueur, le repositionner
            if (activerPlanEau && planEau != null && eauSuitJoueur)
            {
                Vector3 positionEau = new Vector3(nouvellePosition.x, niveauEau, nouvellePosition.z);
                planEau.transform.position = positionEau;
            }
            
            // Convertir la position en coordonnées de chunk
            Vector2Int chunkActuel = PositionVersChunk(nouvellePosition);
            
            // Vérifier si le joueur a changé de chunk
            if (chunkActuel != dernierChunkJoueur)
            {
                dernierChunkJoueur = chunkActuel;
                
                // Si le préchargement est activé, calculer les chunks dans la direction du vol
                if (prechargerDirectionVol)
                {
                    CalculerChunksPrioritaires();
                }
                
                ActualiserChunks();
            }
            else
            {
                // Même si on reste dans le même chunk, mettre à jour les LOD
                MettreAJourLOD(nouvellePosition);
                
                // Et l'occlusion si activée
                if (activerOcclusionCulling)
                {
                    MettreAJourOcclusion(nouvellePosition);
                }
            }
            
            dernierePositionJoueur = nouvellePosition;
        }
    }
    
    private void CreerPlanEau()
    {
        // Créer un plan pour l'eau
        planEau = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planEau.name = "PlanEau";
        planEau.transform.parent = transform;
        
        // Positionner le plan d'eau
        planEau.transform.position = new Vector3(0, niveauEau, 0);
        
        // Appliquer une mise à l'échelle pour couvrir une grande zone
        float echelle = tailleEau / 10f; // Le plan par défaut fait 10x10 unités
        planEau.transform.localScale = new Vector3(echelle, 1f, echelle);
        
        // Obtenir le renderer
        MeshRenderer renderer = planEau.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
        
        // Appliquer le matériel d'eau ou en créer un temporaire
        if (materielEau != null)
        {
            renderer.material = materielEau;
            Debug.Log("Matériau d'eau assigné au plan d'eau.");
        }
        else
        {
            Debug.LogWarning("Aucun matériel d'eau n'a été assigné. Création d'un matériau temporaire.");
            
            // Essayer de générer un matériau d'eau automatiquement
            CreateWaterMaterial waterMaterialGenerator = GetComponent<CreateWaterMaterial>();
            if (waterMaterialGenerator == null)
            {
                waterMaterialGenerator = gameObject.AddComponent<CreateWaterMaterial>();
            }
            
            waterMaterialGenerator.GenerateWaterMaterial();
            if (waterMaterialGenerator.waterMaterial != null)
            {
                renderer.material = waterMaterialGenerator.waterMaterial;
                materielEau = waterMaterialGenerator.waterMaterial;
            }
            else
            {
                // Fallback : créer un matériau standard bleu transparent
                Debug.LogWarning("Impossible de créer un matériau avec CreateWaterMaterial. Utilisation d'un matériau standard bleu.");
                Material fallbackMaterial = new Material(Shader.Find("Standard"));
                
                if (fallbackMaterial != null)
                {
                    // Configurer comme transparent
                    fallbackMaterial.SetFloat("_Mode", 3); // Mode transparent
                    fallbackMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    fallbackMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    fallbackMaterial.SetInt("_ZWrite", 0);
                    fallbackMaterial.DisableKeyword("_ALPHATEST_ON");
                    fallbackMaterial.EnableKeyword("_ALPHABLEND_ON");
                    fallbackMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    fallbackMaterial.renderQueue = 3000;
                    
                    // Couleur bleue semi-transparente
                    fallbackMaterial.SetColor("_Color", new Color(0.2f, 0.5f, 0.7f, 0.7f));
                    fallbackMaterial.SetFloat("_Glossiness", 0.8f);
                    fallbackMaterial.SetFloat("_Metallic", 0.5f);
                    
                    renderer.material = fallbackMaterial;
                    materielEau = fallbackMaterial;
                }
                else
                {
                    Debug.LogError("Impossible de créer un matériau standard. Vérifiez votre installation d'Unity.");
                }
            }
        }
        
        // Créer les bordures du monde si activé
        if (creerBordureMonde)
        {
            CreerBorduresMonde();
        }
    }
    
    private void CreerBorduresMonde()
    {
        // Créer 4 murs pour délimiter le monde
        bordures = new GameObject[4];
        
        // Créer un matériau semi-transparent pour les bordures
        Material matBordure = new Material(Shader.Find("Standard"));
        if (matBordure != null)
        {
            matBordure.SetFloat("_Mode", 3); // Mode transparent
            matBordure.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            matBordure.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            matBordure.SetInt("_ZWrite", 0);
            matBordure.DisableKeyword("_ALPHATEST_ON");
            matBordure.EnableKeyword("_ALPHABLEND_ON");
            matBordure.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            matBordure.renderQueue = 3000;
            
            // Couleur bleu avec transparence
            matBordure.SetColor("_Color", new Color(0.3f, 0.5f, 0.8f, 0.2f));
            matBordure.SetFloat("_Glossiness", 0.8f);
        }
        
        // Créer les 4 bordures (Nord, Sud, Est, Ouest)
        string[] directions = { "Nord", "Sud", "Est", "Ouest" };
        Vector3[] positions = {
            new Vector3(0, niveauEau + hauteurBordure/2, distanceBordure),
            new Vector3(0, niveauEau + hauteurBordure/2, -distanceBordure),
            new Vector3(distanceBordure, niveauEau + hauteurBordure/2, 0),
            new Vector3(-distanceBordure, niveauEau + hauteurBordure/2, 0)
        };
        Vector3[] scales = {
            new Vector3(distanceBordure * 2, hauteurBordure, 0.1f),
            new Vector3(distanceBordure * 2, hauteurBordure, 0.1f),
            new Vector3(0.1f, hauteurBordure, distanceBordure * 2),
            new Vector3(0.1f, hauteurBordure, distanceBordure * 2)
        };
        
        // Créer chaque bordure
        for (int i = 0; i < 4; i++)
        {
            bordures[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bordures[i].name = "Bordure" + directions[i];
            bordures[i].transform.parent = transform;
            bordures[i].transform.position = positions[i];
            bordures[i].transform.localScale = scales[i];
            
            // Appliquer le matériau
            if (matBordure != null)
            {
                MeshRenderer renderer = bordures[i].GetComponent<MeshRenderer>();
                renderer.material = matBordure;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            
            // Ajouter un collider pour empêcher l'avion de sortir du monde
            bordures[i].GetComponent<Collider>().isTrigger = false;
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
    
    private void InitialiserChunkPool(Transform parent)
    {
        // Créer des chunks vides pour le pool
        for (int i = 0; i < chunksEnPooling; i++)
        {
            Chunk chunk = new Chunk(new Vector2Int(0, 0));
            chunk.conteneur = new GameObject($"Chunk_Pool_{i}");
            chunk.conteneur.transform.parent = parent;
            chunk.conteneur.SetActive(false);
            chunksPool.Enqueue(chunk);
        }
    }
    
    private Chunk ObtenirChunkDePool(Vector2Int coords)
    {
        Chunk chunk;
        
        if (chunksPool.Count > 0)
        {
            chunk = chunksPool.Dequeue();
            chunk.coordonnees = coords;
            chunk.niveauLOD = 0;
            chunk.estActif = true;
            chunk.conteneur.name = $"Chunk_{coords.x}_{coords.y}";
            chunk.conteneur.SetActive(true);
        }
        else
        {
            // Créer un nouveau chunk si le pool est vide
            chunk = new Chunk(coords);
            chunk.conteneur = new GameObject($"Chunk_{coords.x}_{coords.y}");
            chunk.conteneur.transform.parent = transform.Find("Chunks");
            chunk.estActif = true;
        }
        
        return chunk;
    }
    
    private void RetournerChunkAuPool(Chunk chunk)
    {
        // Vider les objets du chunk
        foreach (GameObject obj in chunk.objets)
        {
            if (obj != null)
            {
                // Trouver l'index du préfab pour cet objet
                for (int i = 0; i < prefabsMontagnes.Count; i++)
                {
                    if (obj.name.StartsWith(prefabsMontagnes[i].name))
                    {
                        RetournerObjetAuPool(obj, i);
                        break;
                    }
                }
            }
        }
        
        chunk.objets.Clear();
        chunk.estActif = false;
        chunk.conteneur.SetActive(false);
        chunksPool.Enqueue(chunk);
    }
    
    private GameObject ObtenirObjetDePool(int indexPrefab)
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
    
    private void RetournerObjetAuPool(GameObject objet, int indexPrefab)
    {
        if (!utiliserObjectPooling || !objectPools.ContainsKey(indexPrefab)) 
        {
            Destroy(objet);
            return;
        }
        
        objet.SetActive(false);
        objectPools[indexPrefab].Enqueue(objet);
    }
    
    private Vector2Int PositionVersChunk(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / tailleChunk),
            Mathf.FloorToInt(position.z / tailleChunk)
        );
    }
    
    private Vector3 ChunkVersPosition(Vector2Int chunk)
    {
        return new Vector3(
            chunk.x * tailleChunk + tailleChunk / 2,
            0,
            chunk.y * tailleChunk + tailleChunk / 2
        );
    }
    
    public void ActualiserChunks()
    {
        // Déterminer quels chunks doivent être affichés
        HashSet<Vector2Int> nouveauxChunks = CalculerChunksVisibles();
        
        // Ajouter les chunks prioritaires en premier s'ils ne sont pas déjà inclus
        if (prechargerDirectionVol)
        {
            foreach (Vector2Int coords in chunksPrioritaires)
            {
                if (!nouveauxChunks.Contains(coords) && !chunksActifs.ContainsKey(coords))
                {
                    nouveauxChunks.Add(coords);
                }
            }
        }
        
        // Créer une liste des chunks à supprimer
        List<Vector2Int> chunksASupprimer = new List<Vector2Int>();
        foreach (var kvp in chunksActifs)
        {
            if (!nouveauxChunks.Contains(kvp.Key))
            {
                chunksASupprimer.Add(kvp.Key);
            }
        }
        
        // Supprimer les chunks qui ne sont plus visibles
        foreach (Vector2Int coords in chunksASupprimer)
        {
            if (chunksActifs.TryGetValue(coords, out Chunk chunk))
            {
                // Ajouter au cache au lieu de retourner immédiatement au pool
                if (activerCacheChunks)
                {
                    AjouterChunkAuCache(chunk);
                }
                else
                {
                    RetournerChunkAuPool(chunk);
                }
                chunksActifs.Remove(coords);
            }
        }
        
        // Créer les nouveaux chunks ou mettre à jour les existants
        foreach (Vector2Int coords in nouveauxChunks)
        {
            if (!chunksActifs.ContainsKey(coords))
            {
                // D'abord essayer de récupérer du cache
                Chunk chunk = null;
                if (activerCacheChunks)
                {
                    chunk = ObtenirChunkDuCache(coords);
                }
                
                // Si le chunk a été trouvé dans le cache
                if (chunk != null)
                {
                    chunk.conteneur.SetActive(true);
                    chunksActifs[coords] = chunk;
                    
                    // Mettre à jour le LOD
                    float distanceChunk = Vector3.Distance(
                        new Vector3(joueurTransform.position.x, 0, joueurTransform.position.z),
                        new Vector3(ChunkVersPosition(coords).x, 0, ChunkVersPosition(coords).z)
                    );
                    AppliquerLOD(chunk, distanceChunk);
                }
                else
                {
                    // Générer un nouveau chunk si pas dans le cache
                    StartCoroutine(GenererChunkAsync(coords));
                }
            }
        }
        
        // Mettre à jour les niveaux de LOD
        MettreAJourLOD(joueurTransform.position);
        
        // Mettre à jour l'occlusion
        if (activerOcclusionCulling)
        {
            MettreAJourOcclusion(joueurTransform.position);
        }
    }
    
    private HashSet<Vector2Int> CalculerChunksVisibles()
    {
        HashSet<Vector2Int> chunks = new HashSet<Vector2Int>();
        Vector2Int chunkJoueur = dernierChunkJoueur;
        
        // Utiliser la direction de l'avion pour prioriser les chunks devant lui
        Vector2 directionXZ = new Vector2(directionAvion.x, directionAvion.z).normalized;
        
        // Ajuster pour charger plus devant et moins derrière
        int chunksFront = chunksFrontaux;
        int chunksBack = Mathf.Max(1, chunksFrontaux / 2);
        
        // Déterminer les limites de chargement des chunks
        for (int x = -chunksLateraux; x <= chunksLateraux; x++)
        {
            for (int z = -chunksBack; z <= chunksFront; z++)
            {
                // Ajuster la position en fonction de la direction de l'avion
                float angleRad = Mathf.Atan2(directionXZ.y, directionXZ.x);
                float cosAngle = Mathf.Cos(angleRad);
                float sinAngle = Mathf.Sin(angleRad);
                
                int rotatedX = Mathf.RoundToInt(x * cosAngle - z * sinAngle);
                int rotatedZ = Mathf.RoundToInt(x * sinAngle + z * cosAngle);
                
                Vector2Int chunkCoords = new Vector2Int(
                    chunkJoueur.x + rotatedX,
                    chunkJoueur.y + rotatedZ
                );
                
                // Vérifier si le chunk est dans la distance de visibilité
                Vector3 chunkCenter = ChunkVersPosition(chunkCoords);
                float distance = Vector3.Distance(
                    new Vector3(joueurTransform.position.x, 0, joueurTransform.position.z),
                    new Vector3(chunkCenter.x, 0, chunkCenter.z)
                );
                
                if (distance <= distanceVisibilite)
                {
                    chunks.Add(chunkCoords);
                }
            }
        }
        
        return chunks;
    }
    
    private IEnumerator GenererChunkAsync(Vector2Int coords)
    {
        generationEnCours = true;
        
        // Récupérer ou créer un chunk du pool
        Chunk chunk = ObtenirChunkDePool(coords);
        chunksActifs[coords] = chunk;
        
        // Positionner le conteneur du chunk
        Vector3 position = ChunkVersPosition(coords);
        chunk.conteneur.transform.position = new Vector3(position.x, 0, position.z);
        
        // Effectuer la génération du chunk en tâche parallèle
        Task genTask = Task.Run(() => {
            // Cette partie s'exécute dans un thread séparé
            // Ici, on pourrait calculer une heightmap ou d'autres données complexes
            // Pour cet exemple, on se contente de préparer les positions des objets
            
            // Nous ne pouvons pas interagir directement avec Unity depuis ce thread
            // Les données sont préparées ici, puis appliquées dans le thread principal
        });
        
        // Attendre que la tâche soit terminée
        while (!genTask.IsCompleted)
        {
            yield return null;
        }
        
        // Une fois la génération terminée, placer les objets dans le chunk
        int nombreObjets = Random.Range(densiteMontagnes - 2, densiteMontagnes + 2);
        
        for (int i = 0; i < nombreObjets; i++)
        {
            // Position aléatoire dans le chunk
            Vector3 posObjet = new Vector3(
                position.x - tailleChunk/2 + Random.Range(0, tailleChunk),
                0,
                position.z - tailleChunk/2 + Random.Range(0, tailleChunk)
            );
            
            // Vérifier si l'objet n'est pas trop proche du joueur
            if (Vector3.Distance(posObjet, joueurTransform.position) < distanceMinimaleJoueur)
            {
                continue;
            }
            
            // Créer l'objet
            int indexPrefab = Random.Range(0, prefabsMontagnes.Count);
            GameObject objet = ObtenirObjetDePool(indexPrefab);
            
            // Configurer l'objet
            objet.transform.position = posObjet;
            
            // Rotation aléatoire
            Quaternion rotationY = Quaternion.Euler(
                appliquerRotationX ? rotationSpecifique.x : 0, 
                Random.Range(0, 360), 
                0
            );
            objet.transform.rotation = rotationY;
            
            // Mise à l'échelle
            if (conserverTailleOriginale)
            {
                Vector3 echelleOriginale = echellesOriginales[indexPrefab];
                float variation = 1.0f + Random.Range(-variationTaille, variationTaille);
                float variationLargeur = Random.Range(0.8f, 1.2f);
                float variationEpaisseur = Random.Range(0.8f, 1.2f);
                
                Vector3 nouvelleEchelle = new Vector3(
                    echelleOriginale.x * variation * variationLargeur, 
                    echelleOriginale.y * variation, 
                    echelleOriginale.z * variation * variationEpaisseur
                );
                
                objet.transform.localScale = nouvelleEchelle;
            }
            else
            {
                float largeur = Random.Range(largeurMinimale, largeurMaximale);
                float epaisseur = Random.Range(epaisseurMinimale, epaisseurMaximale);
                objet.transform.localScale = new Vector3(largeur, hauteurFixe, epaisseur);
            }
            
            // Ajouter l'objet au chunk
            objet.transform.parent = chunk.conteneur.transform;
            chunk.objets.Add(objet);
        }
        
        // Appliquer le LOD approprié
        float distanceChunk = Vector3.Distance(
            new Vector3(joueurTransform.position.x, 0, joueurTransform.position.z),
            new Vector3(position.x, 0, position.z)
        );
        AppliquerLOD(chunk, distanceChunk);
        
        generationEnCours = false;
        yield return null;
    }
    
    private void MettreAJourLOD(Vector3 positionJoueur)
    {
        Vector3 positionJoueurXZ = new Vector3(positionJoueur.x, 0, positionJoueur.z);
        
        foreach (var kvp in chunksActifs)
        {
            Chunk chunk = kvp.Value;
            Vector3 positionChunk = ChunkVersPosition(chunk.coordonnees);
            Vector3 positionChunkXZ = new Vector3(positionChunk.x, 0, positionChunk.z);
            
            float distance = Vector3.Distance(positionJoueurXZ, positionChunkXZ);
            chunk.distanceAuJoueur = distance;
            
            AppliquerLOD(chunk, distance);
        }
    }
    
    private void AppliquerLOD(Chunk chunk, float distance)
    {
        int nouveauLOD;
        
        if (distance <= distanceLOD0) {
            nouveauLOD = 0; // Haute qualité
        } else if (distance <= distanceLOD1) {
            nouveauLOD = 1; // Qualité moyenne
        } else if (distance <= distanceLOD2) {
            nouveauLOD = 2; // Basse qualité
        } else {
            nouveauLOD = 3; // Invisible
        }
        
        // Ne mettre à jour que si le niveau a changé
        if (chunk.niveauLOD != nouveauLOD)
        {
            chunk.niveauLOD = nouveauLOD;
            
            // Rendre le chunk complètement invisible si niveau LOD = 3
            if (nouveauLOD == 3)
            {
                chunk.conteneur.SetActive(false);
                return;
            }
            else
            {
                chunk.conteneur.SetActive(true);
            }
            
            // Appliquer le LOD à chaque objet du chunk
            foreach (GameObject obj in chunk.objets)
            {
                if (obj == null) continue;
                
                LODGroup lodGroup = obj.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    lodGroup.ForceLOD(nouveauLOD);
                }
                else
                {
                    // Si l'objet n'a pas de LODGroup, ajuster sa visibilité en fonction du niveau
                    switch (nouveauLOD)
                    {
                        case 0:
                            // Tous les détails visibles et matériau original
                            obj.SetActive(true);
                            if (utiliserShadersLegers)
                            {
                                RestaurerMateriauxOriginaux(obj);
                            }
                            break;
                        case 1:
                            // Qualité moyenne - activer uniquement les renderers principaux
                            obj.SetActive(true);
                            break;
                        case 2:
                            // Basse qualité - version simplifiée avec shader léger
                            obj.SetActive(true);
                            if (utiliserShadersLegers && materiauLODBas != null)
                            {
                                AppliquerShaderLeger(obj);
                            }
                            break;
                    }
                }
            }
        }
    }
    
    // Nouvelles méthodes pour gérer les shaders
    private void AppliquerShaderLeger(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Stocker temporairement le matériau original si nécessaire
            if (!renderer.gameObject.TryGetComponent<MateriauOriginal>(out var comp))
            {
                comp = renderer.gameObject.AddComponent<MateriauOriginal>();
                comp.materialOriginal = renderer.sharedMaterial;
            }
            
            // Appliquer le shader léger
            renderer.material = materiauLODBas;
        }
    }
    
    private void RestaurerMateriauxOriginaux(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Restaurer le matériau original s'il existe
            MateriauOriginal comp = renderer.gameObject.GetComponent<MateriauOriginal>();
            if (comp != null && comp.materialOriginal != null)
            {
                renderer.material = comp.materialOriginal;
            }
        }
    }
    
    // Classe utilitaire pour stocker le matériau original
    private class MateriauOriginal : MonoBehaviour
    {
        public Material materialOriginal;
    }
    
    // Méthode pour créer un matériau simplifié pour les objets lointains
    private Material CreerMaterialLODBas()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        if (mat != null)
        {
            mat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1.0f));
            mat.SetFloat("_Smoothness", 0.0f);
            return mat;
        }
        
        // Fallback si le shader URP n'est pas disponible
        return new Material(Shader.Find("Standard"));
    }
    
    // Calcule quels chunks devraient être préchargés en priorité
    private void CalculerChunksPrioritaires()
    {
        chunksPrioritaires.Clear();
        
        // Calculer la direction en 2D (XZ)
        Vector2 directionXZ = new Vector2(directionAvion.x, directionAvion.z).normalized;
        
        // Prédire les prochains chunks basés sur la direction et la vitesse
        float distancePrediction = distancePrecharge * tailleChunk;
        Vector3 positionPredite = joueurTransform.position + directionAvion * distancePrediction;
        Vector2Int chunkPredit = PositionVersChunk(positionPredite);
        
        // Ajouter les chunks entre la position actuelle et la position prédite
        for (int i = 1; i <= distancePrecharge; i++)
        {
            Vector3 pos = joueurTransform.position + directionAvion * (i * tailleChunk);
            Vector2Int chunk = PositionVersChunk(pos);
            
            if (!chunksPrioritaires.Contains(chunk) && !chunksActifs.ContainsKey(chunk))
            {
                chunksPrioritaires.Add(chunk);
            }
        }
    }
    
    // Nouvelle méthode pour gérer le cache de chunks
    private void AjouterChunkAuCache(Chunk chunk)
    {
        if (!activerCacheChunks) return;
        
        Vector2Int coords = chunk.coordonnees;
        
        // Si le chunk est déjà dans le cache, seulement mettre à jour sa position dans la queue
        if (chunksCache.ContainsKey(coords))
        {
            // Retirer de la queue et réajouter à la fin
            List<Vector2Int> tempListe = new List<Vector2Int>(chunksOrdreCache);
            tempListe.Remove(coords);
            chunksOrdreCache = new Queue<Vector2Int>(tempListe);
            chunksOrdreCache.Enqueue(coords);
            return;
        }
        
        // Si le cache est plein, retirer le chunk le plus ancien
        if (chunksCache.Count >= tailleCacheChunks && chunksOrdreCache.Count > 0)
        {
            Vector2Int ancienCoords = chunksOrdreCache.Dequeue();
            if (chunksCache.TryGetValue(ancienCoords, out Chunk ancienChunk))
            {
                chunksCache.Remove(ancienCoords);
                RetournerChunkAuPool(ancienChunk);
            }
        }
        
        // Ajouter le nouveau chunk au cache
        chunksCache[coords] = chunk;
        chunksOrdreCache.Enqueue(coords);
    }
    
    // Essaye de récupérer un chunk depuis le cache
    private Chunk ObtenirChunkDuCache(Vector2Int coords)
    {
        if (!activerCacheChunks || !chunksCache.TryGetValue(coords, out Chunk chunk))
        {
            return null;
        }
        
        // Retirer le chunk du cache
        chunksCache.Remove(coords);
        
        // Mettre à jour la queue
        List<Vector2Int> tempListe = new List<Vector2Int>(chunksOrdreCache);
        tempListe.Remove(coords);
        chunksOrdreCache = new Queue<Vector2Int>(tempListe);
        
        return chunk;
    }
    
    // Méthode pour gérer l'occlusion
    private void MettreAJourOcclusion(Vector3 positionJoueur)
    {
        if (!activerOcclusionCulling) return;
        
        foreach (var kvp in chunksActifs)
        {
            Chunk chunk = kvp.Value;
            Vector3 positionChunk = ChunkVersPosition(chunk.coordonnees);
            
            // Vérifier si le chunk est assez éloigné pour appliquer l'occlusion
            if (chunk.distanceAuJoueur > distanceOcclusion)
            {
                // Vérifier s'il y a un autre chunk entre le joueur et ce chunk
                bool estOcclus = VerifierOcclusion(positionJoueur, positionChunk);
                
                if (estOcclus != chunk.estOcclus)
                {
                    chunk.estOcclus = estOcclus;
                    chunk.conteneur.SetActive(!estOcclus);
                }
            }
            else if (chunk.estOcclus)
            {
                // Si le chunk est plus proche maintenant, le réactiver
                chunk.estOcclus = false;
                chunk.conteneur.SetActive(true);
            }
        }
    }
    
    // Vérifie si un chunk est occlus par d'autres chunks
    private bool VerifierOcclusion(Vector3 positionJoueur, Vector3 positionChunk)
    {
        Vector3 direction = positionChunk - positionJoueur;
        float distance = direction.magnitude;
        direction.Normalize();
        
        // Lancer un rayon pour voir s'il y a des obstacles
        Ray ray = new Ray(positionJoueur, direction);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distance - 10f)) // -10f pour éviter que le chunk lui-même ne bloque le rayon
        {
            // Vérifier que ce n'est pas le chunk lui-même qui est touché
            Vector2Int hitChunkCoords = PositionVersChunk(hit.point);
            if (chunksActifs.TryGetValue(hitChunkCoords, out Chunk hitChunk))
            {
                if (hitChunk.conteneur != hit.transform.gameObject)
                {
                    return true; // Il y a un obstacle qui bloque la vue
                }
            }
            else
            {
                return true; // Un objet non-chunk bloque la vue
            }
        }
        
        return false; // Pas d'obstacle, chunk visible
    }
} 