// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Video;

#if UNITY_EDITOR
    using UnityEditor;
    using System.Net;
#endif

public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    #region Camera Movement Variables

    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    // Internal Variables
    private bool isWalking = false;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;

    // Internal Variables
    private bool isGrounded = false;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion

    #region Effets Réalistes FPS

    [Header("Corps et Mouvements Réalistes")]
    // Respiration en idle
    public bool enableBreathing = true;
    [Range(0.01f, 0.1f)]
    public float breathingIntensity = 0.03f;
    public float breathingSpeed = 1.2f;
    private float breathingTimer = 0f;

    // Inclinaison dans les virages
    public bool enableLeanInTurns = true;
    [Range(0.1f, 5f)]
    public float leanAngle = 2f;
    [Range(0.1f, 5f)]
    public float leanSpeed = 1f;
    private float currentLeanAngle = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;

    // Vision trouble après course
    public bool enableBlurredVision = true;
    [Range(0.1f, 10f)]
    public float blurRecoverySpeed = 1f;
    [Range(0.01f, 1f)]
    public float maxBlurAmount = 0.5f;
    private float currentBlurAmount = 0f;
    private PostProcessVolume postProcessVolume;
    private Vignette vignetteEffect;
    private float lastSprintTime = 0f;

    #endregion

    #region PTSD Effects
    
    [Header("Troubles Post-Traumatiques")]
    public bool enablePTSDEffects = true;
    public float minTimeBetweenFlashbacks = 30f;
    public float maxTimeBetweenFlashbacks = 120f;
    public float flashbackDuration = 5f;
    [Range(0.05f, 0.5f)]
    public float timeSlowdownFactor = 0.2f;
    [Range(0.05f, 0.5f)]
    public float movementSlowdownFactor = 0.15f;
    [Range(0.01f, 0.2f)]
    public float cameraShakeIntensity = 0.12f;
    [Range(0f, 1f)]
    public float vignettingIntensity = 0.7f;
    public bool muteSoundsOnFlashback = true;
    
    // Vidéos et son de flashback
    public VideoClip[] flashbackVideos;
    public AudioClip ptsdSoundEffect;
    [Range(0f, 1f)]
    public float ptsdSoundVolume = 0.8f;
    public Color flashbackColor = new Color(1, 0, 0, 0.3f);
    
    // Variables internes pour le système simplifié
    private AudioSource ptsdAudioSource;
    private Canvas ptsdCanvas;
    private Image ptsdOverlay;
    private Image vignetteImage;
    private Image distortionImage;
    private RawImage videoImage;
    private VideoPlayer videoPlayer;
    private float originalFixedDeltaTime;
    private float originalWalkSpeed;
    private float originalSprintSpeed;
    private float originalFOV;
    private float nextFlashbackTime;
    private bool isHavingFlashback = false;
    private float flashbackEndTime;
    
    // Variables pour le silencieux de son
    private Dictionary<AudioSource, float> originalAudioVolumes = new Dictionary<AudioSource, float>();
    private AudioSource[] activeAudioSources;
    
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        crosshairObject = GetComponentInChildren<Image>();

        // Set internal variables
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
        
        // Configuration des effets PTSD (version simplifiée)
        if (enablePTSDEffects)
        {
            SetupPTSDSystem();
            originalFixedDeltaTime = Time.fixedDeltaTime;
            ScheduleNextFlashback();
        }
        
        // Configuration des effets réalistes
        SetupRealisticEffects();
    }

    // Configurateur automatique du système PTSD
    private void SetupPTSDSystem()
    {
        // Créer une source audio
        ptsdAudioSource = gameObject.AddComponent<AudioSource>();
        ptsdAudioSource.spatialBlend = 0f; // Son 2D
        ptsdAudioSource.playOnAwake = false;
        ptsdAudioSource.volume = ptsdSoundVolume;
        ptsdAudioSource.loop = false;
        
        // Créer un canvas pour les effets de flashback
        GameObject canvasObj = new GameObject("PTSD_Canvas");
        canvasObj.transform.SetParent(transform, false);
        ptsdCanvas = canvasObj.AddComponent<Canvas>();
        ptsdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ptsdCanvas.sortingOrder = 999; // Toujours au premier plan
        
        // Ajouter un canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Créer une image overlay pour l'effet rouge
        GameObject overlayObj = new GameObject("PTSD_Overlay");
        overlayObj.transform.SetParent(ptsdCanvas.transform, false);
        ptsdOverlay = overlayObj.AddComponent<Image>();
        ptsdOverlay.color = new Color(1, 0, 0, 0);
        
        // Faire en sorte que l'overlay couvre tout l'écran
        RectTransform rectTransform = ptsdOverlay.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        
        // Créer un effet de vignettage (bords noirs)
        GameObject vignetteObj = new GameObject("PTSD_Vignette");
        vignetteObj.transform.SetParent(ptsdCanvas.transform, false);
        vignetteImage = vignetteObj.AddComponent<Image>();
        vignetteImage.sprite = CreateVignetteSprite();
        vignetteImage.color = new Color(0, 0, 0, 0);
        
        // Configurer le vignettage
        RectTransform vignetteRect = vignetteImage.rectTransform;
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.sizeDelta = Vector2.zero;
        
        // Créer un effet de distorsion visuelle
        GameObject distortionObj = new GameObject("PTSD_Distortion");
        distortionObj.transform.SetParent(ptsdCanvas.transform, false);
        distortionImage = distortionObj.AddComponent<Image>();
        distortionImage.sprite = CreateNoiseSprite();
        distortionImage.color = new Color(1, 1, 1, 0);
        
        // Configurer la distorsion
        RectTransform distortionRect = distortionImage.rectTransform;
        distortionRect.anchorMin = Vector2.zero;
        distortionRect.anchorMax = Vector2.one;
        distortionRect.sizeDelta = Vector2.zero;
        
        // Créer un RawImage pour la vidéo
        GameObject videoObj = new GameObject("FlashbackVideo");
        videoObj.transform.SetParent(ptsdCanvas.transform, false);
        videoImage = videoObj.AddComponent<RawImage>();
        
        // Configurer le RawImage pour qu'il soit centré et de taille appropriée
        RectTransform videoRect = videoImage.rectTransform;
        videoRect.anchorMin = new Vector2(0.5f, 0.5f);
        videoRect.anchorMax = new Vector2(0.5f, 0.5f);
        videoRect.pivot = new Vector2(0.5f, 0.5f);
        videoRect.sizeDelta = new Vector2(1600, 900); // Format 16:9 encore plus grand
        
        // Créer et configurer le VideoPlayer
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Créer une RenderTexture pour la vidéo
        RenderTexture videoTexture = new RenderTexture(1600, 900, 16, RenderTextureFormat.ARGB32);
        videoTexture.Create();
        
        // Assigner la texture au VideoPlayer et au RawImage
        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
        videoImage.gameObject.SetActive(false);
        
        // Désactiver initialement
        ptsdCanvas.gameObject.SetActive(false);
        
        // Créer quelques vidéos par défaut si aucune n'est spécifiée
        SetupDefaultVideos();
    }

    private void SetupDefaultVideos()
    {
        // N'ajoute des vidéos par défaut que si aucune n'est définie
        if (flashbackVideos == null || flashbackVideos.Length == 0)
        {
            // Vérifie s'il y a des vidéos dans le dossier Resources/Videos
            VideoClip[] resourceVideos = Resources.LoadAll<VideoClip>("Videos");
            if (resourceVideos != null && resourceVideos.Length > 0)
            {
                flashbackVideos = resourceVideos;
            }
        }
    }

    void Start()
    {
        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if(crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }

        #region Sprint Bar

        sprintBarCG = GetComponentInChildren<CanvasGroup>();

        if(useSprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if(hideBarWhenFull)
            {
                sprintBarCG.alpha = 0;
            }
        }
        else
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }

        #endregion
    }

    float camRotation;

    private void Update()
    {
        #region Camera

        // Control camera movement
        if(cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            }
            else
            {
                // Inverted Y
                pitch += mouseSensitivity * Input.GetAxis("Mouse Y");
            }

            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        #region Camera Zoom

        if (enableZoom)
        {
            // Changes isZoomed when key is pressed
            // Behavior for toogle zoom
            if(Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
            {
                if (!isZoomed)
                {
                    isZoomed = true;
                }
                else
                {
                    isZoomed = false;
                }
            }

            // Changes isZoomed when key is pressed
            // Behavior for hold to zoom
            if(holdToZoom && !isSprinting)
            {
                if(Input.GetKeyDown(zoomKey))
                {
                    isZoomed = true;
                }
                else if(Input.GetKeyUp(zoomKey))
                {
                    isZoomed = false;
                }
            }

            // Lerps camera.fieldOfView to allow for a smooth transistion
            if(isZoomed)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            }
            else if(!isZoomed && !isSprinting)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
            }
        }

        #endregion
        #endregion

        #region Blurred Vision Effect
        
        // Gestion de l'effet de vision trouble
        if (enableBlurredVision && currentBlurAmount > 0)
        {
            // Récupération du temps écoulé depuis le dernier sprint
            float timeSinceLastSprint = Time.time - lastSprintTime;
            
            // Diminution progressive de l'effet
            if (timeSinceLastSprint > 0.5f) // Petit délai avant que l'effet commence à diminuer
            {
                currentBlurAmount = Mathf.Max(0, currentBlurAmount - Time.deltaTime * blurRecoverySpeed);
            }
            
            // Application de l'effet visuel (vignettage)
            if (vignetteEffect != null)
            {
                vignetteEffect.intensity.Override(currentBlurAmount);
            }
        }
        
        #endregion

        #region Sprint

        if(enableSprint)
        {
            if(isSprinting)
            {
                isZoomed = false;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

                // Drain sprint remaining while sprinting
                if(!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                // Regain sprint while not sprinting
                sprintRemaining = Mathf.Clamp(sprintRemaining += 1 * Time.deltaTime, 0, sprintDuration);
            }

            // Handles sprint cooldown 
            // When sprint remaining == 0 stops sprint ability until hitting cooldown
            if(isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0)
                {
                    isSprintCooldown = false;
                }
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            // Handles sprintBar 
            if(useSprintBar && !unlimitedSprint)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);
            }
        }

        #endregion

        #region Jump

        // Gets input and calls jump method
        if(enableJump && Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }

        #endregion

        #region Crouch

        if (enableCrouch)
        {
            if(Input.GetKeyDown(crouchKey) && !holdToCrouch)
            {
                Crouch();
            }
            
            if(Input.GetKeyDown(crouchKey) && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if(Input.GetKeyUp(crouchKey) && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }

        #endregion

        CheckGround();

        if(enableHeadBob)
        {
            HeadBob();
        }
        
        // Gestion des flashbacks PTSD
        if (enablePTSDEffects && !isHavingFlashback && Time.time >= nextFlashbackTime)
        {
            StartCoroutine(TriggerPTSDFlashback());
        }
    }

    void FixedUpdate()
    {
        #region Movement

        if (playerCanMove)
        {
            // Calculate how fast we should be moving
            Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            // Checks if player is walking and isGrounded
            // Will allow head bob
            if (targetVelocity.x != 0 || targetVelocity.z != 0 && isGrounded)
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }

            // All movement calculations shile sprint is active
            if (enableSprint && Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown)
            {
                targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;

                // Apply a force that attempts to reach our target velocity
                Vector3 velocity = rb.linearVelocity;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0;

                // Player is only moving when valocity change != 0
                // Makes sure fov change only happens during movement
                if (velocityChange.x != 0 || velocityChange.z != 0)
                {
                    isSprinting = true;

                    if (isCrouched)
                    {
                        Crouch();
                    }

                    if (hideBarWhenFull && !unlimitedSprint)
                    {
                        sprintBarCG.alpha += 5 * Time.deltaTime;
                    }
                }

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            // All movement calculations while walking
            else
            {
                isSprinting = false;

                if (hideBarWhenFull && sprintRemaining == sprintDuration)
                {
                    sprintBarCG.alpha -= 3 * Time.deltaTime;
                }

                targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;

                // Apply a force that attempts to reach our target velocity
                Vector3 velocity = rb.linearVelocity;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0;

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        #endregion
    }

    // Sets isGrounded based on a raycast sent straigth down from the player object
    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if(isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Crouch()
    {
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if(isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;

            isCrouched = false;
        }
        // Crouches player down to set height
        // Reduces walkSpeed
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;

            isCrouched = true;
        }
    }

    private void SetupRealisticEffects()
    {
        // Recherche du volume post-processing pour les effets visuels
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        
        if (postProcessVolume != null)
        {
            // Récupération ou création des effets de post-processing
            postProcessVolume.profile.TryGetSettings(out vignetteEffect);
            
            if (vignetteEffect == null && enableBlurredVision)
            {
                // Création du vignettage pour l'effet de vision trouble
                vignetteEffect = ScriptableObject.CreateInstance<Vignette>();
                vignetteEffect.enabled.Override(true);
                vignetteEffect.intensity.Override(0f);
                vignetteEffect.smoothness.Override(0.2f);
                vignetteEffect.color.Override(new Color(0, 0, 0, 1));
                postProcessVolume.profile.AddSettings(vignetteEffect);
            }
        }
        else
        {
            Debug.LogWarning("Aucun PostProcessVolume trouvé. Les effets visuels ne fonctionneront pas.");
        }
    }

    private void HeadBob()
    {
        Vector3 finalPosition = jointOriginalPos;
        
        if(isWalking)
        {
            // Calculates HeadBob speed during sprint
            if(isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement (plus subtle, especially on X and Z)
            finalPosition += new Vector3(
                Mathf.Sin(timer) * bobAmount.x * 0.7f,  // Reduced X movement
                Mathf.Sin(timer) * bobAmount.y,
                Mathf.Sin(timer) * bobAmount.z * 0.7f   // Reduced Z movement
            );
            
            // Si l'effet de vision trouble est activé, on le déclenche après un sprint
            if (enableBlurredVision && isSprinting)
            {
                lastSprintTime = Time.time;
                currentBlurAmount = maxBlurAmount;
            }
        }
        else
        {
            // Respiration en idle quand on ne bouge pas
            if (enableBreathing && isGrounded)
            {
                breathingTimer += Time.deltaTime * breathingSpeed;
                
                // Mouvement vertical subtil pour la respiration
                finalPosition += new Vector3(
                    0f, 
                    Mathf.Sin(breathingTimer) * breathingIntensity,
                    0f
                );
            }
            
            // Resets walking timer
            timer = 0;
        }
        
        // Applique l'inclinaison dans les virages
        ApplyLeanInTurns();
        
        // Interpolation douce vers la position finale
        joint.localPosition = Vector3.Lerp(joint.localPosition, finalPosition, Time.deltaTime * bobSpeed);
    }

    private void ApplyLeanInTurns()
    {
        if (enableLeanInTurns && isWalking)
        {
            // Récupère la direction horizontale
            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
            
            // Calcule la rotation de la caméra basée sur le changement de direction
            if (moveDirection.magnitude > 0.1f)
            {
                // Détection d'un virage (changement de direction)
                float turnAmount = Vector3.SignedAngle(
                    lastMoveDirection, 
                    moveDirection, 
                    Vector3.up
                );
                
                // Limite la valeur max de l'inclinaison
                turnAmount = Mathf.Clamp(turnAmount * 0.1f, -leanAngle, leanAngle);
                
                // Interpolation douce vers l'angle cible
                currentLeanAngle = Mathf.Lerp(currentLeanAngle, turnAmount, Time.deltaTime * leanSpeed);
                
                // Mémorise la direction pour le prochain frame
                lastMoveDirection = moveDirection;
            }
            else
            {
                // Retour progressif à zéro quand pas de mouvement
                currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0, Time.deltaTime * leanSpeed * 2);
            }
            
            // Applique l'inclinaison à la caméra (rotation en Z)
            playerCamera.transform.localRotation = Quaternion.Euler(
                playerCamera.transform.localEulerAngles.x,
                playerCamera.transform.localEulerAngles.y,
                currentLeanAngle
            );
        }
    }

    private void ScheduleNextFlashback()
    {
        float delay = Random.Range(minTimeBetweenFlashbacks, maxTimeBetweenFlashbacks);
        nextFlashbackTime = Time.time + delay;
    }

    private IEnumerator TriggerPTSDFlashback()
    {
        isHavingFlashback = true;
        flashbackEndTime = Time.time + flashbackDuration;
        
        // Sauvegarder les vitesses originales et le FOV
        originalWalkSpeed = walkSpeed;
        originalSprintSpeed = sprintSpeed;
        originalFOV = playerCamera.fieldOfView;
        
        // Ralentir le personnage drastiquement
        walkSpeed *= movementSlowdownFactor;
        sprintSpeed *= movementSlowdownFactor;
        
        // Couper toutes les musiques et sons (sauf celui du PTSD)
        if (muteSoundsOnFlashback)
        {
            MuteAllAudio();
        }
        
        // Activer l'overlay
        ptsdCanvas.gameObject.SetActive(true);
        
        // Ralentir le temps encore plus
        Time.timeScale = timeSlowdownFactor;
        Time.fixedDeltaTime = originalFixedDeltaTime * timeSlowdownFactor;
        
        // Jouer une vidéo de flashback si disponible
        bool hasVideo = false;
        if (flashbackVideos != null && flashbackVideos.Length > 0)
        {
            int videoIndex = Random.Range(0, flashbackVideos.Length);
            if (flashbackVideos[videoIndex] != null)
            {
                videoPlayer.clip = flashbackVideos[videoIndex];
                // Désactiver le son de la vidéo, nous utilisons un son spécifique à la place
                videoPlayer.SetDirectAudioMute(0, true);
                videoPlayer.Play();
                videoImage.gameObject.SetActive(true);
                hasVideo = true;
            }
        }
        
        // Jouer le son du PTSD (au lieu du son généré ou du son de la vidéo)
        if (ptsdSoundEffect != null)
        {
            // Utiliser le son spécifié dans l'inspecteur
            ptsdAudioSource.clip = ptsdSoundEffect;
            ptsdAudioSource.volume = ptsdSoundVolume;
            ptsdAudioSource.pitch = Random.Range(0.9f, 1.1f); // Légère variation du pitch pour plus de variété
            ptsdAudioSource.Play();
        }
        else
        {
            // Générer un son d'horreur basique si aucun son n'est spécifié
            GenerateDefaultPtsdSound();
        }
        
        // Effets visuels pendant toute la durée du flashback
        float elapsed = 0f;
        
        // Récupérer la position originale de la caméra
        Vector3 originalCameraPosition = playerCamera.transform.localPosition;
        Quaternion originalCameraRotation = playerCamera.transform.localRotation;
        
        while (elapsed < flashbackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = elapsed / flashbackDuration;
            float phase = normalizedTime * Mathf.PI * 2;
            
            // Effet de pulsation pour l'overlay rouge
            float pulseIntensity = 0.3f + 0.2f * Mathf.Sin(normalizedTime * 20f);
            ptsdOverlay.color = new Color(flashbackColor.r, flashbackColor.g, flashbackColor.b, hasVideo ? pulseIntensity * 0.3f : pulseIntensity);
            
            // Effet de vignettage pulsant
            vignetteImage.color = new Color(0, 0, 0, vignettingIntensity * (0.6f + 0.4f * Mathf.Sin(phase * 3)));
            
            // Effet de distorsion aléatoire
            if (Random.value < 0.1f)
            {
                distortionImage.color = new Color(1, 1, 1, Random.Range(0.05f, 0.2f));
                distortionImage.sprite = CreateNoiseSprite(); // Générer un nouveau bruit aléatoire
            }
            else
            {
                distortionImage.color = new Color(1, 1, 1, Mathf.Max(0, distortionImage.color.a - 0.01f));
            }
            
            // Effet de champ de vision dynamique (tunnel vision / zoom)
            float fovModulation = 10f * Mathf.Sin(phase * 2);
            playerCamera.fieldOfView = originalFOV + fovModulation;
            
            // Effet de distorsion de la caméra (tremblement extrêmement intense)
            float shakeAmount = cameraShakeIntensity * (1f + Mathf.Sin(normalizedTime * 40f));
            float traumaFactor = Mathf.Pow(1f - normalizedTime * 0.5f, 2); // Plus intense au début
            
            // Tremblement plus erratique et complexe
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount) * traumaFactor,
                Random.Range(-shakeAmount, shakeAmount) * traumaFactor,
                Random.Range(-shakeAmount * 0.5f, shakeAmount * 0.5f) * traumaFactor
            );
            
            // Ajout d'un mouvement sinusoïdal pour plus de réalisme
            shakeOffset += new Vector3(
                Mathf.Sin(elapsed * 13.5f) * shakeAmount * 0.3f,
                Mathf.Sin(elapsed * 17.7f) * shakeAmount * 0.3f,
                0
            );
            
            playerCamera.transform.localPosition = originalCameraPosition + shakeOffset;
            
            // Rotation erratique plus intense
            Quaternion shakeRotation = Quaternion.Euler(
                playerCamera.transform.localEulerAngles.x + Random.Range(-shakeAmount * 25f, shakeAmount * 25f) * traumaFactor,
                playerCamera.transform.localEulerAngles.y + Random.Range(-shakeAmount * 15f, shakeAmount * 15f) * traumaFactor,
                Random.Range(-shakeAmount * 30f, shakeAmount * 30f) * traumaFactor
            );
            
            playerCamera.transform.localRotation = shakeRotation;
            
            // Ajouter des flashs aléatoires plus intenses
            if (Random.value < 0.08f)
            {
                StartCoroutine(FlashScreen(Random.Range(0.05f, 0.1f)));
            }
            
            yield return null;
        }
        
        // Restaurer la caméra à sa position et rotation d'origine
        playerCamera.transform.localPosition = originalCameraPosition;
        playerCamera.transform.localRotation = originalCameraRotation;
        playerCamera.fieldOfView = originalFOV;
        
        // Fin du flashback
        isHavingFlashback = false;
        ptsdCanvas.gameObject.SetActive(false);
        videoImage.gameObject.SetActive(false);
        videoPlayer.Stop();
        
        // Restaurer les sons qui étaient en cours
        if (muteSoundsOnFlashback)
        {
            RestoreAudio();
        }
        
        // Restaurer le temps normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        
        // Restaurer les vitesses originales
        walkSpeed = originalWalkSpeed;
        sprintSpeed = originalSprintSpeed;
        
        // Planifier le prochain flashback
        ScheduleNextFlashback();
    }

    private IEnumerator FlashScreen(float duration = 0.05f)
    {
        Color originalOverlayColor = ptsdOverlay.color;
        Color originalVignetteColor = vignetteImage.color;
        
        // Flash blanc intense
        ptsdOverlay.color = new Color(1, 1, 1, 0.7f);
        vignetteImage.color = new Color(0, 0, 0, 0);
        
        yield return new WaitForSecondsRealtime(duration);
        
        // Restaurer les couleurs originales
        ptsdOverlay.color = originalOverlayColor;
        vignetteImage.color = originalVignetteColor;
    }

    private void OnDestroy()
    {
        // S'assurer que le temps est restauré si l'objet est détruit pendant un flashback
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }

    // Crée un sprite pour l'effet de vignettage
    private Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calculer la distance du centre (normalisée)
                float dx = (x / (float)size) - 0.5f;
                float dy = (y / (float)size) - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // * 2 pour que la distance soit 1 aux coins
                
                // Calculer l'opacité (0 au centre, 1 aux bords)
                float alpha = Mathf.Clamp01(distance * distance * 1.5f);
                texture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        
        texture.Apply();
        
        // Créer un sprite à partir de la texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Crée un sprite pour l'effet de bruit/distorsion
    private Sprite CreateNoiseSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Générer un bruit aléatoire
                float noise = UnityEngine.Random.Range(0f, 1f);
                texture.SetPixel(x, y, new Color(noise, noise, noise, 0.2f));
            }
        }
        
        texture.Apply();
        
        // Créer un sprite à partir de la texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Méthode pour couper toutes les sources audio dans la scène sauf celle du PTSD
    private void MuteAllAudio()
    {
        // Récupérer toutes les sources audio actives dans la scène
        activeAudioSources = FindObjectsOfType<AudioSource>();
        originalAudioVolumes.Clear();
        
        // Sauvegarder le volume original de chaque source audio et les mettre en sourdine
        foreach (AudioSource source in activeAudioSources)
        {
            // Ne pas couper notre propre source audio pour le PTSD
            if (source != ptsdAudioSource)
            {
                // Sauvegarder le volume original
                originalAudioVolumes[source] = source.volume;
                
                // Mettre en sourdine
                source.volume = 0f;
            }
        }
    }

    // Méthode pour restaurer les volumes audio originaux
    private void RestoreAudio()
    {
        foreach (var kvp in originalAudioVolumes)
        {
            if (kvp.Key != null) // Vérifier que l'AudioSource existe toujours
            {
                // Restaurer le volume original
                kvp.Key.volume = kvp.Value;
            }
        }
        
        // Vider le dictionnaire
        originalAudioVolumes.Clear();
    }

    // Méthode pour générer un son par défaut si aucun son PTSD n'est défini
    private void GenerateDefaultPtsdSound()
    {
        // Utilisons l'AudioClip.Create pour créer un son d'horreur basique
        float frequency = 440f; // Fréquence en Hz (La)
        int sampleRate = 44100; // Taux d'échantillonnage standard
        float duration = flashbackDuration;
        
        AudioClip noiseClip = AudioClip.Create("PTSDNoise", (int)(sampleRate * duration), 1, sampleRate, false);
        float[] samples = new float[(int)(sampleRate * duration)];
        
        // Générer un son inquiétant plus complexe
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / sampleRate;
            // Son de base avec variations de fréquence et amplitude
            float baseFreq = frequency * (1f + 0.1f * Mathf.Sin(t * 0.5f));
            float noise = Random.Range(-0.5f, 0.5f);
            float pulse = Mathf.Sin(t * baseFreq) * (0.5f + 0.5f * Mathf.Sin(t * 2f));
            float whisper = Mathf.Sin(t * baseFreq * 2) * Mathf.Sin(t * 8.7f) * 0.2f;
            samples[i] = (pulse * 0.5f + noise * 0.4f + whisper) * Mathf.Min(1, t * 2) * Mathf.Min(1, (duration - t) * 2);
        }
        
        noiseClip.SetData(samples, 0);
        ptsdAudioSource.clip = noiseClip;
        ptsdAudioSource.volume = ptsdSoundVolume;
        ptsdAudioSource.Play();
    }
}



// Custom Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
    public class FirstPersonControllerEditor : Editor
    {
    FirstPersonController fpc;
    SerializedObject SerFPC;

    private void OnEnable()
    {
        fpc = (FirstPersonController)target;
        SerFPC = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        SerFPC.Update();


        #region Camera Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera", "Camera attached to the controller."), fpc.playerCamera, typeof(Camera), true);
        fpc.fov = EditorGUILayout.Slider(new GUIContent("Field of View", "The camera's view angle. Changes the player camera directly."), fpc.fov, fpc.zoomFOV, 179f);
        fpc.cameraCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Rotation", "Determines if the camera is allowed to move."), fpc.cameraCanMove);

        GUI.enabled = fpc.cameraCanMove;
        fpc.invertCamera = EditorGUILayout.ToggleLeft(new GUIContent("Invert Camera Rotation", "Inverts the up and down movement of the camera."), fpc.invertCamera);
        fpc.mouseSensitivity = EditorGUILayout.Slider(new GUIContent("Look Sensitivity", "Determines how sensitive the mouse movement is."), fpc.mouseSensitivity, .1f, 10f);
        fpc.maxLookAngle = EditorGUILayout.Slider(new GUIContent("Max Look Angle", "Determines the max and min angle the player camera is able to look."), fpc.maxLookAngle, 40, 90);
        GUI.enabled = true;

        fpc.lockCursor = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide Cursor", "Turns off the cursor visibility and locks it to the middle of the screen."), fpc.lockCursor);

        fpc.crosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Crosshair", "Determines if the basic crosshair will be turned on, and sets is to the center of the screen."), fpc.crosshair);

        // Only displays crosshair options if crosshair is enabled
        if(fpc.crosshair) 
        { 
            EditorGUI.indentLevel++; 
            EditorGUILayout.BeginHorizontal(); 
            EditorGUILayout.PrefixLabel(new GUIContent("Crosshair Image", "Sprite to use as the crosshair.")); 
            fpc.crosshairImage = (Sprite)EditorGUILayout.ObjectField(fpc.crosshairImage, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.crosshairColor = EditorGUILayout.ColorField(new GUIContent("Crosshair Color", "Determines the color of the crosshair."), fpc.crosshairColor);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--; 
        }

        EditorGUILayout.Space();

        #region Camera Zoom Setup

        GUILayout.Label("Zoom", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableZoom = EditorGUILayout.ToggleLeft(new GUIContent("Enable Zoom", "Determines if the player is able to zoom in while playing."), fpc.enableZoom);

        GUI.enabled = fpc.enableZoom;
        fpc.holdToZoom = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Zoom", "Requires the player to hold the zoom key instead if pressing to zoom and unzoom."), fpc.holdToZoom);
        fpc.zoomKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Zoom Key", "Determines what key is used to zoom."), fpc.zoomKey);
        fpc.zoomFOV = EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Determines the field of view the camera zooms to."), fpc.zoomFOV, .1f, fpc.fov);
        fpc.zoomStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while zooming in."), fpc.zoomStepTime, .1f, 10f);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Movement Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Player Movement", "Determines if the player is allowed to move."), fpc.playerCanMove);

        GUI.enabled = fpc.playerCanMove;
        fpc.walkSpeed = EditorGUILayout.Slider(new GUIContent("Walk Speed", "Determines how fast the player will move while walking."), fpc.walkSpeed, .1f, fpc.sprintSpeed);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #region Sprint

        GUILayout.Label("Sprint", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableSprint = EditorGUILayout.ToggleLeft(new GUIContent("Enable Sprint", "Determines if the player is allowed to sprint."), fpc.enableSprint);

        GUI.enabled = fpc.enableSprint;
        fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(new GUIContent("Unlimited Sprint", "Determines if 'Sprint Duration' is enabled. Turning this on will allow for unlimited sprint."), fpc.unlimitedSprint);
        fpc.sprintKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "Determines what key is used to sprint."), fpc.sprintKey);
        fpc.sprintSpeed = EditorGUILayout.Slider(new GUIContent("Sprint Speed", "Determines how fast the player will move while sprinting."), fpc.sprintSpeed, fpc.walkSpeed, 20f);

        //GUI.enabled = !fpc.unlimitedSprint;
        fpc.sprintDuration = EditorGUILayout.Slider(new GUIContent("Sprint Duration", "Determines how long the player can sprint while unlimited sprint is disabled."), fpc.sprintDuration, 1f, 20f);
        fpc.sprintCooldown = EditorGUILayout.Slider(new GUIContent("Sprint Cooldown", "Determines how long the recovery time is when the player runs out of sprint."), fpc.sprintCooldown, .1f, fpc.sprintDuration);
        //GUI.enabled = true;

        fpc.sprintFOV = EditorGUILayout.Slider(new GUIContent("Sprint FOV", "Determines the field of view the camera changes to while sprinting."), fpc.sprintFOV, fpc.fov, 179f);
        fpc.sprintFOVStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while sprinting."), fpc.sprintFOVStepTime, .1f, 20f);

        fpc.useSprintBar = EditorGUILayout.ToggleLeft(new GUIContent("Use Sprint Bar", "Determines if the default sprint bar will appear on screen."), fpc.useSprintBar);

        // Only displays sprint bar options if sprint bar is enabled
        if(fpc.useSprintBar)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(new GUIContent("Hide Full Bar", "Hides the sprint bar when sprint duration is full, and fades the bar in when sprinting. Disabling this will leave the bar on screen at all times when the sprint bar is enabled."), fpc.hideBarWhenFull);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar BG", "Object to be used as sprint bar background."));
            fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField(fpc.sprintBarBG, typeof(Image), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar", "Object to be used as sprint bar foreground."));
            fpc.sprintBar = (Image)EditorGUILayout.ObjectField(fpc.sprintBar, typeof(Image), true);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarWidthPercent = EditorGUILayout.Slider(new GUIContent("Bar Width", "Determines the width of the sprint bar."), fpc.sprintBarWidthPercent, .1f, .5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarHeightPercent = EditorGUILayout.Slider(new GUIContent("Bar Height", "Determines the height of the sprint bar."), fpc.sprintBarHeightPercent, .001f, .025f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Jump

        GUILayout.Label("Jump", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableJump = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump", "Determines if the player is allowed to jump."), fpc.enableJump);

        GUI.enabled = fpc.enableJump;
        fpc.jumpKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "Determines what key is used to jump."), fpc.jumpKey);
        fpc.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "Determines how high the player will jump."), fpc.jumpPower, .1f, 20f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Crouch

        GUILayout.Label("Crouch", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Crouch", "Determines if the player is allowed to crouch."), fpc.enableCrouch);

        GUI.enabled = fpc.enableCrouch;
        fpc.holdToCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Hold To Crouch", "Requires the player to hold the crouch key instead if pressing to crouch and uncrouch."), fpc.holdToCrouch);
        fpc.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "Determines what key is used to crouch."), fpc.crouchKey);
        fpc.crouchHeight = EditorGUILayout.Slider(new GUIContent("Crouch Height", "Determines the y scale of the player object when crouched."), fpc.crouchHeight, .1f, 1);
        fpc.speedReduction = EditorGUILayout.Slider(new GUIContent("Speed Reduction", "Determines the percent 'Walk Speed' is reduced by. 1 being no reduction, and .5 being half."), fpc.speedReduction, .1f, 1);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Head Bob

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Head Bob Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bob", "Determines if the camera will bob while the player is walking."), fpc.enableHeadBob);
        

        GUI.enabled = fpc.enableHeadBob;
        fpc.joint = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Joint", "Joint object position is moved while head bob is active."), fpc.joint, typeof(Transform), true);
        fpc.bobSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Determines how often a bob rotation is completed."), fpc.bobSpeed, 1, 20);
        fpc.bobAmount = EditorGUILayout.Vector3Field(new GUIContent("Bob Amount", "Determines the amount the joint moves in both directions on every axes."), fpc.bobAmount);
        GUI.enabled = true;

        #endregion

        #region PTSD Setup

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Troubles Post-Traumatiques Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enablePTSDEffects = EditorGUILayout.ToggleLeft(new GUIContent("Enable PTSD Effects", "Determines if the player is allowed to experience PTSD effects."), fpc.enablePTSDEffects);
        
        GUI.enabled = fpc.enablePTSDEffects;
        fpc.minTimeBetweenFlashbacks = EditorGUILayout.Slider(new GUIContent("Min Time Between Flashbacks", "Determines the minimum time between flashbacks."), fpc.minTimeBetweenFlashbacks, 1f, 120f);
        fpc.maxTimeBetweenFlashbacks = EditorGUILayout.Slider(new GUIContent("Max Time Between Flashbacks", "Determines the maximum time between flashbacks."), fpc.maxTimeBetweenFlashbacks, fpc.minTimeBetweenFlashbacks, 300f);
        fpc.flashbackDuration = EditorGUILayout.Slider(new GUIContent("Flashback Duration", "Determines the duration of each flashback."), fpc.flashbackDuration, 1f, 20f);
        fpc.timeSlowdownFactor = EditorGUILayout.Slider(new GUIContent("Time Slowdown Factor", "Determines the factor by which time is slowed during a flashback."), fpc.timeSlowdownFactor, 0.05f, 0.5f);
        fpc.movementSlowdownFactor = EditorGUILayout.Slider(new GUIContent("Movement Slowdown Factor", "Determines the factor by which player movement is slowed during a flashback."), fpc.movementSlowdownFactor, 0.05f, 0.5f);
        fpc.cameraShakeIntensity = EditorGUILayout.Slider(new GUIContent("Camera Shake Intensity", "Determines the intensity of camera shake during a flashback."), fpc.cameraShakeIntensity, 0.01f, 0.2f);
        fpc.vignettingIntensity = EditorGUILayout.Slider(new GUIContent("Vignetting Intensity", "Controls the intensity of the vignette effect (dark borders)."), fpc.vignettingIntensity, 0f, 1f);
        fpc.muteSoundsOnFlashback = EditorGUILayout.ToggleLeft(new GUIContent("Mute All Sounds", "Coupe toutes les musiques et sons pendant les flashbacks."), fpc.muteSoundsOnFlashback);

        EditorGUILayout.Space();
        SerializedProperty videosArrayProp = SerFPC.FindProperty("flashbackVideos");
        EditorGUILayout.PropertyField(videosArrayProp, new GUIContent("Vidéos de Flashback", "Vidéos qui seront jouées aléatoirement pendant les flashbacks."), true);

        fpc.ptsdSoundEffect = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Son de Flashback", "Son unique joué pendant tous les flashbacks PTSD."), fpc.ptsdSoundEffect, typeof(AudioClip), false);
        fpc.ptsdSoundVolume = EditorGUILayout.Slider(new GUIContent("Volume du Son", "Volume du son de flashback."), fpc.ptsdSoundVolume, 0f, 1f);

        fpc.flashbackColor = EditorGUILayout.ColorField(new GUIContent("Couleur de Flashback", "Couleur de l'overlay pendant les flashbacks."), fpc.flashbackColor);

        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Corps et Mouvements Réalistes

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Corps et Mouvements Réalistes", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        // Respiration
        fpc.enableBreathing = EditorGUILayout.ToggleLeft(new GUIContent("Respiration en Idle", "Active l'effet de respiration quand le joueur ne bouge pas."), fpc.enableBreathing);
        GUI.enabled = fpc.enableBreathing;
        fpc.breathingIntensity = EditorGUILayout.Slider(new GUIContent("Intensité Respiration", "Intensité du mouvement de respiration."), fpc.breathingIntensity, 0.01f, 0.1f);
        fpc.breathingSpeed = EditorGUILayout.Slider(new GUIContent("Vitesse Respiration", "Vitesse du cycle de respiration."), fpc.breathingSpeed, 0.5f, 2f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Inclinaison caméra
        fpc.enableLeanInTurns = EditorGUILayout.ToggleLeft(new GUIContent("Inclinaison dans les Virages", "La caméra s'incline légèrement lors des virages."), fpc.enableLeanInTurns);
        GUI.enabled = fpc.enableLeanInTurns;
        fpc.leanAngle = EditorGUILayout.Slider(new GUIContent("Angle d'Inclinaison", "Angle maximum d'inclinaison dans les virages."), fpc.leanAngle, 0.1f, 5f);
        fpc.leanSpeed = EditorGUILayout.Slider(new GUIContent("Vitesse d'Inclinaison", "Vitesse de transition de l'inclinaison."), fpc.leanSpeed, 0.1f, 5f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Vision trouble
        fpc.enableBlurredVision = EditorGUILayout.ToggleLeft(new GUIContent("Vision Trouble après Course", "Effet de vision trouble après un sprint ou pendant un stress."), fpc.enableBlurredVision);
        GUI.enabled = fpc.enableBlurredVision;
        fpc.maxBlurAmount = EditorGUILayout.Slider(new GUIContent("Intensité Maximale", "Intensité maximale de l'effet de vision trouble."), fpc.maxBlurAmount, 0.01f, 1f);
        fpc.blurRecoverySpeed = EditorGUILayout.Slider(new GUIContent("Vitesse de Récupération", "Vitesse à laquelle la vision redevient normale."), fpc.blurRecoverySpeed, 0.1f, 10f);
        GUI.enabled = true;

        #endregion

        //Sets any changes from the prefab
        if(GUI.changed)
        {
            EditorUtility.SetDirty(fpc);
            Undo.RecordObject(fpc, "FPC Change");
            SerFPC.ApplyModifiedProperties();
        }
    }

}

#endif