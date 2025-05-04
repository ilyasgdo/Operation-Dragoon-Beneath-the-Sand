using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.IO;
using System.Text;

public class FPSDisplay : MonoBehaviour
{
    float deltaTime = 0.0f;
    private float ramUsage = 0.0f;
    private float vramUsage = 0.0f;
    private string gpuInfo = "N/A";
    private string cpuInfo = "N/A";
    
    // Pourcentages d'utilisation (estimations)
    private float cpuUsagePercent = 0f;
    private float gpuUsagePercent = 0f;
    private float vramUsagePercent = 0f;
    
    // Paramètres pour les logs
    [Header("Paramètres de logging")]
    public bool enableLogging = true;
    public float logInterval = 5f; // Intervalle de log en secondes
    
    // Variables pour les moyennes
    private List<float> fpsValues = new List<float>();
    private List<float> ramValues = new List<float>();
    private List<float> vramValues = new List<float>();
    private List<float> cpuValues = new List<float>();
    private List<float> gpuValues = new List<float>();
    
    // Chemin du fichier de log
    private string logFilePath;
    
    void Start()
    {
        StartCoroutine(UpdateSystemStats());
        
        if (enableLogging)
        {
            logFilePath = Path.Combine(Application.persistentDataPath, "performance_log.csv");
            // Créer ou réécrire l'en-tête du fichier
            if (!File.Exists(logFilePath))
            {
                string header = "Timestamp,FPS moyen,RAM moyenne (MB),VRAM moyenne (MB),CPU (%),GPU (%)\n";
                File.WriteAllText(logFilePath, header);
            }
            
            StartCoroutine(LogPerformanceData());
        }
        
        // Récupération des informations système
        gpuInfo = SystemInfo.graphicsDeviceName;
        cpuInfo = SystemInfo.processorType;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // Collecter des données pour les moyennes
        float fps = 1.0f / deltaTime;
        fpsValues.Add(fps);
        ramValues.Add(ramUsage);
        vramValues.Add(vramUsage);
        cpuValues.Add(cpuUsagePercent);
        gpuValues.Add(gpuUsagePercent);
        
        // Limiter la taille des listes
        if (fpsValues.Count > 100)
        {
            fpsValues.RemoveAt(0);
            ramValues.RemoveAt(0);
            vramValues.RemoveAt(0);
            cpuValues.RemoveAt(0);
            gpuValues.RemoveAt(0);
        }
    }
    
    IEnumerator UpdateSystemStats()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);
        int frameCount = 0;
        float prevCpuTime = 0f;
        
        while (true)
        {
            // Mise à jour RAM (mémoire gérée)
            ramUsage = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f); // En MB
            
            // Mise à jour VRAM (approximation via texture memory)
            vramUsage = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f); // En MB
            
            // Estimation du pourcentage de VRAM utilisé
            float totalVRAM = SystemInfo.graphicsMemorySize;
            vramUsagePercent = (totalVRAM > 0) ? (vramUsage / totalVRAM) * 100f : 0f;
            
            // Estimation du pourcentage CPU (basée sur le temps passé dans Update/FixedUpdate)
            float currCpuTime = Time.realtimeSinceStartup;
            float cpuFrameTime = (currCpuTime - prevCpuTime);
            cpuUsagePercent = (cpuFrameTime > 0) ? (Time.deltaTime / cpuFrameTime) * 100f : 0f;
            cpuUsagePercent = Mathf.Clamp(cpuUsagePercent, 0f, 100f);
            prevCpuTime = currCpuTime;
            
            // Estimation du pourcentage GPU (basée sur FPS vs capacité théorique)
            float targetFPS = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
            float currentFPS = 1.0f / deltaTime;
            gpuUsagePercent = (targetFPS > 0) ? Mathf.Clamp(100f - ((currentFPS / targetFPS) * 100f), 0f, 100f) : 0f;
            
            frameCount++;
            yield return waitTime;
        }
    }
    
    IEnumerator LogPerformanceData()
    {
        WaitForSeconds waitTime = new WaitForSeconds(logInterval);
        
        while (enableLogging)
        {
            // Calculer les moyennes
            float avgFps = CalculateAverage(fpsValues);
            float avgRam = CalculateAverage(ramValues);
            float avgVram = CalculateAverage(vramValues);
            float avgCpu = CalculateAverage(cpuValues);
            float avgGpu = CalculateAverage(gpuValues);
            
            // Créer l'entrée de log
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = string.Format("{0},{1:0.0},{2:0.0},{3:0.0},{4:0.0},{5:0.0}\n", 
                                            timestamp, avgFps, avgRam, avgVram, avgCpu, avgGpu);
            
            // Écrire dans le fichier
            try
            {
                File.AppendAllText(logFilePath, logEntry);
                Debug.Log("Données de performance enregistrées: " + logEntry);
            }
            catch (Exception e)
            {
                Debug.LogError("Erreur lors de l'écriture du fichier de log: " + e.Message);
            }
            
            yield return waitTime;
        }
    }
    
    float CalculateAverage(List<float> values)
    {
        if (values.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float value in values)
        {
            sum += value;
        }
        return sum / values.Count;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.white;
        
        // FPS
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string fpsText = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        
        // Ressources
        string ramText = string.Format("RAM: {0:0.0} MB", ramUsage);
        string vramText = string.Format("VRAM: {0:0.0} MB ({1:0.0}%)", vramUsage, vramUsagePercent);
        string cpuText = string.Format("CPU: {0} - Utilisation: {1:0.0}%", cpuInfo, cpuUsagePercent);
        string gpuText = string.Format("GPU: {0} - Utilisation: {1:0.0}%", gpuInfo, gpuUsagePercent);
        
        // Informations additionnelles
        string sysInfo = string.Format("Résolution: {0}x{1} ({2}Hz)", Screen.width, Screen.height, Screen.currentResolution.refreshRate);
        string logInfo = enableLogging ? "Logging activé: " + logFilePath : "Logging désactivé";
        
        // Affichage des informations
        Rect fpsRect = new Rect(10, 10, w, h * 2 / 100);
        Rect ramRect = new Rect(10, 10 + h * 2 / 40, w, h * 2 / 100);
        Rect vramRect = new Rect(10, 10 + h * 4 / 40, w, h * 2 / 100);
        Rect cpuRect = new Rect(10, 10 + h * 6 / 40, w, h * 2 / 100);
        Rect gpuRect = new Rect(10, 10 + h * 8 / 40, w, h * 2 / 100);
        Rect sysRect = new Rect(10, 10 + h * 10 / 40, w, h * 2 / 100);
        Rect logRect = new Rect(10, 10 + h * 12 / 40, w, h * 2 / 100);
        
        GUI.Label(fpsRect, fpsText, style);
        GUI.Label(ramRect, ramText, style);
        GUI.Label(vramRect, vramText, style);
        GUI.Label(cpuRect, cpuText, style);
        GUI.Label(gpuRect, gpuText, style);
        GUI.Label(sysRect, sysInfo, style);
        GUI.Label(logRect, logInfo, style);
    }
    
    // Méthode pour accéder aux moyennes depuis d'autres scripts
    public Dictionary<string, float> GetPerformanceAverages()
    {
        Dictionary<string, float> averages = new Dictionary<string, float>
        {
            { "fps", CalculateAverage(fpsValues) },
            { "ram", CalculateAverage(ramValues) },
            { "vram", CalculateAverage(vramValues) },
            { "cpu", CalculateAverage(cpuValues) },
            { "gpu", CalculateAverage(gpuValues) }
        };
        
        return averages;
    }
}
