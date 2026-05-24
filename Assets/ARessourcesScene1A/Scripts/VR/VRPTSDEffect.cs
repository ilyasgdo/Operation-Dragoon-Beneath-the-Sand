using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using System.Collections;
using System.Collections.Generic;

public class VRPTSDEffect : MonoBehaviour
{
    [Header("PTSD Settings")]
    public bool enablePTSDEffects = true;
    public float minTimeBetweenFlashbacks = 30f;
    public float maxTimeBetweenFlashbacks = 120f;
    public float flashbackDuration = 5f;
    
    [Range(0.05f, 0.5f)]
    public float timeSlowdownFactor = 0.2f;
    [Range(0.05f, 0.5f)]
    public float movementSlowdownFactor = 0.15f;
    
    [Header("VR Specifics")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.5f;
    public float hapticDuration = 0.1f;
    
    [Header("Media")]
    public VideoClip[] flashbackVideos;
    public AudioClip ptsdSoundEffect;
    [Range(0f, 1f)]
    public float ptsdSoundVolume = 0.8f;
    public Color flashbackColor = new Color(1, 0, 0, 0.3f);

    private AudioSource ptsdAudioSource;
    private GameObject hudContainer;
    private Image ptsdOverlay;
    private RawImage videoImage;
    private VideoPlayer videoPlayer;
    private ContinuousMoveProviderBase moveProvider;
    
    private float originalWalkSpeed;
    private float nextFlashbackTime;
    private bool isHavingFlashback = false;
    private float originalFixedDeltaTime;

    void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
        SetupVRHUD();
        ScheduleNextFlashback();
        
        moveProvider = GetComponentInChildren<ContinuousMoveProviderBase>();
        if (moveProvider == null) moveProvider = FindObjectOfType<ContinuousMoveProviderBase>();
    }

    void SetupVRHUD()
    {
        // Create a world-space HUD parented to the camera
        Camera mainCam = GetComponentInChildren<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        hudContainer = new GameObject("VR_PTSD_HUD");
        hudContainer.transform.SetParent(mainCam.transform, false);
        hudContainer.transform.localPosition = new Vector3(0, 0, 0.5f); // 50cm in front of eyes
        hudContainer.transform.localRotation = Quaternion.identity;

        Canvas canvas = hudContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = hudContainer.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2, 2); // 2x2 meters square
        hudContainer.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Scale down to fit view

        // Red Overlay
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(hudContainer.transform, false);
        ptsdOverlay = overlayObj.AddComponent<Image>();
        ptsdOverlay.color = new Color(1, 0, 0, 0);
        ptsdOverlay.rectTransform.sizeDelta = new Vector2(200, 200);

        // Video
        GameObject videoObj = new GameObject("FlashbackVideo");
        videoObj.transform.SetParent(hudContainer.transform, false);
        videoImage = videoObj.AddComponent<RawImage>();
        videoImage.color = new Color(1, 1, 1, 0);
        videoImage.rectTransform.sizeDelta = new Vector2(160, 90);
        
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        RenderTexture rt = new RenderTexture(1280, 720, 0);
        videoPlayer.targetTexture = rt;
        videoImage.texture = rt;

        // Audio
        ptsdAudioSource = gameObject.AddComponent<AudioSource>();
        ptsdAudioSource.spatialBlend = 0f;

        hudContainer.SetActive(false);
    }

    void Update()
    {
        if (enablePTSDEffects && !isHavingFlashback && Time.time >= nextFlashbackTime)
        {
            StartCoroutine(TriggerFlashback());
        }
    }

    private void ScheduleNextFlashback()
    {
        nextFlashbackTime = Time.time + Random.Range(minTimeBetweenFlashbacks, maxTimeBetweenFlashbacks);
    }

    private IEnumerator TriggerFlashback()
    {
        isHavingFlashback = true;
        hudContainer.SetActive(true);
        
        if (moveProvider != null)
        {
            originalWalkSpeed = moveProvider.moveSpeed;
            moveProvider.moveSpeed *= movementSlowdownFactor;
        }

        Time.timeScale = timeSlowdownFactor;
        Time.fixedDeltaTime = originalFixedDeltaTime * timeSlowdownFactor;

        if (flashbackVideos.Length > 0)
        {
            int idx = Random.Range(0, flashbackVideos.Length);
            videoPlayer.clip = flashbackVideos[idx];
            videoPlayer.Play();
            videoImage.color = Color.white;
        }

        if (ptsdSoundEffect != null)
        {
            ptsdAudioSource.PlayOneShot(ptsdSoundEffect, ptsdSoundVolume);
        }

        float elapsed = 0f;
        while (elapsed < flashbackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Pulsing Red
            float alpha = 0.2f + 0.2f * Mathf.Sin(elapsed * 10f);
            ptsdOverlay.color = new Color(flashbackColor.r, flashbackColor.g, flashbackColor.b, alpha);
            
            // VR Haptics
            SendHaptics(hapticIntensity);

            yield return null;
        }

        // Restore
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        if (moveProvider != null) moveProvider.moveSpeed = originalWalkSpeed;
        
        videoPlayer.Stop();
        hudContainer.SetActive(false);
        isHavingFlashback = false;
        ScheduleNextFlashback();
    }

    private void SendHaptics(float intensity)
    {
        // Simplified haptic call - in XRI 3.0 we'd find the interactors/controllers
        XRBaseController[] controllers = FindObjectsOfType<XRBaseController>();
        foreach(var controller in controllers)
        {
            controller.SendHapticImpulse(intensity, hapticDuration);
        }
    }
}
