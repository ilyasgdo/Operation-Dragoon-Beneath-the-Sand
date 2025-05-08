using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject performancePanel;
    
    [Header("Paramètres de performance")]
    public Toggle fpsDisplayToggle;
    public Dropdown qualityPresetDropdown;
    public Dropdown resolutionDropdown;
    public Toggle vsyncToggle;
    public Slider renderDistanceSlider;
    public Toggle shadowsToggle;
    
    [Header("Préréglages PC")]
    public Button lowEndPCButton;
    public Button midRangePCButton;
    public Button highEndPCButton;
    
    // Préréglages pour différentes configurations PC
    private Dictionary<string, PerformanceSettings> pcPresets = new Dictionary<string, PerformanceSettings>();
    
    // Référence vers le script FPS
    private FPSDisplay fpsDisplay;
    
    // Structure pour les paramètres de performance
    [System.Serializable]
    public class PerformanceSettings
    {
        public bool showFPS;
        public int qualityLevel;
        public int resolutionIndex;
        public bool enableVSync;
        public float renderDistance;
        public bool enableShadows;
        public bool enableLogging;
    }

    void Start()
    {
        // Initialiser les panneaux
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (performancePanel) performancePanel.SetActive(false);
        
        // Trouver la référence vers le script FPS
        fpsDisplay = FindObjectOfType<FPSDisplay>();
        if (fpsDisplay == null)
        {
            Debug.LogWarning("FPSDisplay non trouvé dans la scène.");
        }
        
        // Remplir les options de qualité
        if (qualityPresetDropdown != null)
        {
            qualityPresetDropdown.ClearOptions();
            List<string> options = new List<string>();
            
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                options.Add(QualitySettings.names[i]);
            }
            
            qualityPresetDropdown.AddOptions(options);
            qualityPresetDropdown.value = QualitySettings.GetQualityLevel();
        }
        
        // Remplir les options de résolution
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            Resolution[] resolutions = Screen.resolutions;
            
            foreach (Resolution resolution in resolutions)
            {
                options.Add(resolution.width + " x " + resolution.height + " @ " + resolution.refreshRate + "Hz");
            }
            
            resolutionDropdown.AddOptions(options);
        }
        
        // Initialiser les préréglages PC
        InitializePresets();
        
        // Charger les paramètres sauvegardés
        LoadSettings();
        
        // Configurer les listeners pour les boutons
        SetupButtonListeners();
    }
    
    void InitializePresets()
    {
        // Configuration PC bas de gamme
        PerformanceSettings lowEndSettings = new PerformanceSettings
        {
            showFPS = true,
            qualityLevel = 0, // Qualité minimale
            resolutionIndex = 0, // Résolution la plus basse
            enableVSync = false,
            renderDistance = 0.5f, // Distance de rendu réduite
            enableShadows = false,
            enableLogging = false
        };
        
        // Configuration PC milieu de gamme
        PerformanceSettings midRangeSettings = new PerformanceSettings
        {
            showFPS = true,
            qualityLevel = 2, // Qualité moyenne
            resolutionIndex = Screen.resolutions.Length / 2, // Résolution moyenne
            enableVSync = true,
            renderDistance = 0.75f, // Distance de rendu moyenne
            enableShadows = true,
            enableLogging = false
        };
        
        // Configuration PC haut de gamme
        PerformanceSettings highEndSettings = new PerformanceSettings
        {
            showFPS = false,
            qualityLevel = QualitySettings.names.Length - 1, // Qualité maximale
            resolutionIndex = Screen.resolutions.Length - 1, // Résolution la plus haute
            enableVSync = true,
            renderDistance = 1.0f, // Distance de rendu maximale
            enableShadows = true,
            enableLogging = true
        };
        
        // Ajouter les préréglages au dictionnaire
        pcPresets.Add("LowEnd", lowEndSettings);
        pcPresets.Add("MidRange", midRangeSettings);
        pcPresets.Add("HighEnd", highEndSettings);
    }
    
    void SetupButtonListeners()
    {
        // Boutons de préréglages PC
        if (lowEndPCButton) lowEndPCButton.onClick.AddListener(() => ApplyPreset("LowEnd"));
        if (midRangePCButton) midRangePCButton.onClick.AddListener(() => ApplyPreset("MidRange"));
        if (highEndPCButton) highEndPCButton.onClick.AddListener(() => ApplyPreset("HighEnd"));
        
        // Toggles et dropdowns
        if (fpsDisplayToggle) fpsDisplayToggle.onValueChanged.AddListener(ToggleFPSDisplay);
        if (qualityPresetDropdown) qualityPresetDropdown.onValueChanged.AddListener(SetQualityLevel);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (vsyncToggle) vsyncToggle.onValueChanged.AddListener(ToggleVSync);
        if (shadowsToggle) shadowsToggle.onValueChanged.AddListener(ToggleShadows);
    }
    
    public void ApplyPreset(string presetName)
    {
        if (!pcPresets.ContainsKey(presetName))
        {
            Debug.LogError("Préréglage non trouvé: " + presetName);
            return;
        }
        
        PerformanceSettings settings = pcPresets[presetName];
        
        // Appliquer les paramètres
        if (fpsDisplayToggle) fpsDisplayToggle.isOn = settings.showFPS;
        if (qualityPresetDropdown) qualityPresetDropdown.value = settings.qualityLevel;
        if (resolutionDropdown) resolutionDropdown.value = settings.resolutionIndex;
        if (vsyncToggle) vsyncToggle.isOn = settings.enableVSync;
        if (renderDistanceSlider) renderDistanceSlider.value = settings.renderDistance;
        if (shadowsToggle) shadowsToggle.isOn = settings.enableShadows;
        
        // Appliquer directement certains paramètres
        ToggleFPSDisplay(settings.showFPS);
        SetQualityLevel(settings.qualityLevel);
        SetResolution(settings.resolutionIndex);
        ToggleVSync(settings.enableVSync);
        ToggleShadows(settings.enableShadows);
        
        // Activer le logging sur FPSDisplay
        if (fpsDisplay)
        {
            fpsDisplay.enableLogging = settings.enableLogging;
        }
        
        // Sauvegarder les paramètres
        SaveSettings();
    }
    
    void ToggleFPSDisplay(bool enable)
    {
        if (fpsDisplay)
        {
            fpsDisplay.gameObject.SetActive(enable);
        }
    }
    
    void SetQualityLevel(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
    }
    
    void SetResolution(int index)
    {
        if (index >= 0 && index < Screen.resolutions.Length)
        {
            Resolution resolution = Screen.resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen, resolution.refreshRate);
        }
    }
    
    void ToggleVSync(bool enable)
    {
        QualitySettings.vSyncCount = enable ? 1 : 0;
    }
    
    void ToggleShadows(bool enable)
    {
        QualitySettings.shadows = enable ? ShadowQuality.All : ShadowQuality.Disable;
    }
    
    public void ShowSettingsPanel()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        if (performancePanel) performancePanel.SetActive(false);
    }
    
    public void ShowPerformancePanel()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (performancePanel) performancePanel.SetActive(true);
    }
    
    public void BackToMainMenu()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (performancePanel) performancePanel.SetActive(false);
    }
    
    public void StartGame()
    {
        // Sauvegarder avant de démarrer
        SaveSettings();
        
        // Charger la scène du jeu (remplacer "GameScene" par le nom de votre scène de jeu)
        SceneManager.LoadScene("GameScene");
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    void SaveSettings()
    {
        // Sauvegarder l'affichage FPS
        PlayerPrefs.SetInt("ShowFPS", fpsDisplayToggle && fpsDisplayToggle.isOn ? 1 : 0);
        
        // Sauvegarder le niveau de qualité
        PlayerPrefs.SetInt("QualityLevel", qualityPresetDropdown ? qualityPresetDropdown.value : 0);
        
        // Sauvegarder l'index de résolution
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown ? resolutionDropdown.value : 0);
        
        // Sauvegarder VSync
        PlayerPrefs.SetInt("VSync", vsyncToggle && vsyncToggle.isOn ? 1 : 0);
        
        // Sauvegarder la distance de rendu
        PlayerPrefs.SetFloat("RenderDistance", renderDistanceSlider ? renderDistanceSlider.value : 1.0f);
        
        // Sauvegarder les ombres
        PlayerPrefs.SetInt("Shadows", shadowsToggle && shadowsToggle.isOn ? 1 : 0);
        
        // Sauvegarder le logging
        PlayerPrefs.SetInt("EnableLogging", fpsDisplay && fpsDisplay.enableLogging ? 1 : 0);
        
        PlayerPrefs.Save();
    }
    
    void LoadSettings()
    {
        // Charger l'affichage FPS
        if (fpsDisplayToggle) fpsDisplayToggle.isOn = PlayerPrefs.GetInt("ShowFPS", 1) == 1;
        
        // Charger le niveau de qualité
        if (qualityPresetDropdown) qualityPresetDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        
        // Charger la résolution
        if (resolutionDropdown) resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", 0);
        
        // Charger VSync
        if (vsyncToggle) vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) == 1;
        
        // Charger la distance de rendu
        if (renderDistanceSlider) renderDistanceSlider.value = PlayerPrefs.GetFloat("RenderDistance", 1.0f);
        
        // Charger les ombres
        if (shadowsToggle) shadowsToggle.isOn = PlayerPrefs.GetInt("Shadows", 1) == 1;
        
        // Appliquer les paramètres
        if (fpsDisplay)
        {
            bool showFPS = PlayerPrefs.GetInt("ShowFPS", 1) == 1;
            fpsDisplay.gameObject.SetActive(showFPS);
            fpsDisplay.enableLogging = PlayerPrefs.GetInt("EnableLogging", 0) == 1;
        }
        
        SetQualityLevel(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()));
        SetResolution(PlayerPrefs.GetInt("ResolutionIndex", 0));
        ToggleVSync(PlayerPrefs.GetInt("VSync", 1) == 1);
        ToggleShadows(PlayerPrefs.GetInt("Shadows", 1) == 1);
    }
} 