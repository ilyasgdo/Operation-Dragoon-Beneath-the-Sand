using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Références de scènes")]
    public GameObject mainMenuPrefab;
    
    [Header("Animation UI")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.3f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Éléments UI")]
    public Image backgroundImage;
    public CanvasGroup mainCanvasGroup;
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    
    // Animation en cours
    private Coroutine currentAnimation;
    private MainMenu mainMenuScript;
    
    void Start()
    {
        // Chercher ou créer le script MainMenu
        mainMenuScript = FindObjectOfType<MainMenu>();
        if (mainMenuScript == null && mainMenuPrefab != null)
        {
            GameObject menuObject = Instantiate(mainMenuPrefab);
            mainMenuScript = menuObject.GetComponent<MainMenu>();
        }
        
        // Initialiser l'interface
        if (mainCanvasGroup)
        {
            mainCanvasGroup.alpha = 0;
            currentAnimation = StartCoroutine(FadeIn(mainCanvasGroup, fadeInTime, fadeInCurve));
        }
        
        // Configurer les boutons
        SetupButtons();
    }
    
    void SetupButtons()
    {
        if (playButton)
        {
            playButton.onClick.AddListener(() => {
                // Animer la sortie et lancer le jeu
                StopCurrentAnimation();
                currentAnimation = StartCoroutine(FadeOutAndStart());
            });
        }
        
        if (settingsButton)
        {
            settingsButton.onClick.AddListener(() => {
                if (mainMenuScript)
                {
                    mainMenuScript.ShowSettingsPanel();
                }
            });
        }
        
        if (quitButton)
        {
            quitButton.onClick.AddListener(() => {
                if (mainMenuScript)
                {
                    mainMenuScript.QuitGame();
                }
            });
        }
    }
    
    IEnumerator FadeIn(CanvasGroup group, float duration, AnimationCurve curve)
    {
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            group.alpha = curve.Evaluate(normalizedTime);
            yield return null;
        }
        
        group.alpha = 1;
    }
    
    IEnumerator FadeOutAndStart()
    {
        if (mainCanvasGroup)
        {
            float elapsed = 0;
            
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / fadeOutTime);
                mainCanvasGroup.alpha = 1 - normalizedTime;
                yield return null;
            }
            
            mainCanvasGroup.alpha = 0;
        }
        
        // Lancer le jeu
        if (mainMenuScript)
        {
            mainMenuScript.StartGame();
        }
    }
    
    void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
    
    // Méthode pour afficher le panneau des performances préréglées
    public void ShowPerformancePresets()
    {
        if (mainMenuScript)
        {
            mainMenuScript.ShowPerformancePanel();
        }
    }
    
    // Méthode pour revenir au menu principal
    public void BackToMainMenu()
    {
        if (mainMenuScript)
        {
            mainMenuScript.BackToMainMenu();
        }
    }
    
    // Méthodes pour appliquer les préréglages PC
    public void ApplyLowEndPreset()
    {
        if (mainMenuScript)
        {
            mainMenuScript.ApplyPreset("LowEnd");
        }
    }
    
    public void ApplyMidRangePreset()
    {
        if (mainMenuScript)
        {
            mainMenuScript.ApplyPreset("MidRange");
        }
    }
    
    public void ApplyHighEndPreset()
    {
        if (mainMenuScript)
        {
            mainMenuScript.ApplyPreset("HighEnd");
        }
    }
} 