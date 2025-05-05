using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TransitionSceneManager : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Durée du fondu en entrée de scène")]
    public float dureeFonduEntree = 1.5f;
    
    [Tooltip("Délai avant de commencer le fondu en entrée")]
    public float delaiAvantFonduEntree = 0.5f;
    
    [Tooltip("Courbe d'animation pour le fondu")]
    public AnimationCurve courbeFondu = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Référence vers l'instance unique (pattern Singleton)
    private static TransitionSceneManager instance;
    
    // Panneau de transition
    private CanvasGroup panneauFondu;
    
    // Propriété pour accéder à l'instance unique
    public static TransitionSceneManager Instance
    {
        get 
        {
            // Si l'instance n'existe pas, on la cherche
            if (instance == null)
            {
                instance = FindObjectOfType<TransitionSceneManager>();
                
                // Si elle n'existe toujours pas, on en crée une
                if (instance == null)
                {
                    GameObject go = new GameObject("TransitionSceneManager");
                    instance = go.AddComponent<TransitionSceneManager>();
                }
            }
            
            return instance;
        }
    }
    
    void Awake()
    {
        // S'assurer qu'il n'y a qu'une seule instance de ce gestionnaire
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Conserver cet objet lors des changements de scène
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Créer le panneau de fondu
        CreerPanneauFondu();
        
        // Commencer avec un écran noir
        if (panneauFondu != null)
        {
            panneauFondu.alpha = 1f;
            
            // Lancer le fondu d'entrée
            StartCoroutine(FonduEntree());
        }
    }
    
    // Créer un panneau noir pour le fondu de transition
    void CreerPanneauFondu()
    {
        // Créer un GameObject pour le panneau de fondu
        GameObject panneauFonduObj = new GameObject("PanneauFondu");
        panneauFonduObj.transform.SetParent(transform);
        
        // Ajouter un Canvas
        Canvas canvas = panneauFonduObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // S'assurer qu'il est au-dessus de tout
        
        // Ajouter un CanvasScaler
        CanvasScaler scaler = panneauFonduObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ajouter un CanvasGroup pour contrôler l'alpha
        panneauFondu = panneauFonduObj.AddComponent<CanvasGroup>();
        panneauFondu.alpha = 1f;
        panneauFondu.blocksRaycasts = true;
        
        // Ajouter une image noire qui couvre tout l'écran
        GameObject imageObj = new GameObject("ImageFondu");
        imageObj.transform.SetParent(panneauFonduObj.transform, false);
        
        RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        
        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;
    }
    
    // Faire un fondu en entrée (du noir vers transparent)
    public IEnumerator FonduEntree()
    {
        if (panneauFondu == null) yield break;
        
        // Attendre un délai initial
        yield return new WaitForSeconds(delaiAvantFonduEntree);
        
        // Activer le blocage des rayons pour empêcher les interactions pendant la transition
        panneauFondu.blocksRaycasts = true;
        
        float tempsEcoule = 0;
        
        while (tempsEcoule < dureeFonduEntree)
        {
            tempsEcoule += Time.deltaTime;
            float t = Mathf.Clamp01(tempsEcoule / dureeFonduEntree);
            
            // Diminuer l'opacité du panneau noir (en utilisant la courbe d'animation)
            panneauFondu.alpha = 1f - courbeFondu.Evaluate(t);
            
            yield return null;
        }
        
        // S'assurer que le panneau est complètement transparent
        panneauFondu.alpha = 0f;
        panneauFondu.blocksRaycasts = false;
    }
    
    // Faire un fondu en sortie (transparent vers noir) avec une durée spécifique
    public IEnumerator FonduSortie(float duree)
    {
        if (panneauFondu == null) yield break;
        
        // Activer le blocage des rayons pour empêcher les interactions pendant la transition
        panneauFondu.blocksRaycasts = true;
        
        float tempsEcoule = 0;
        
        while (tempsEcoule < duree)
        {
            tempsEcoule += Time.deltaTime;
            float t = Mathf.Clamp01(tempsEcoule / duree);
            
            // Augmenter l'opacité du panneau noir (en utilisant la courbe d'animation)
            panneauFondu.alpha = courbeFondu.Evaluate(t);
            
            yield return null;
        }
        
        // S'assurer que le panneau est complètement opaque
        panneauFondu.alpha = 1f;
    }
} 