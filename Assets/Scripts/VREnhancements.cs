using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

namespace VREnhancements {

    // --- HAPTIC FEEDBACK ---
    public class VRHapticFeedback : MonoBehaviour {
        [Range(0, 1)] public float intensity = 0.5f;
        public float duration = 0.1f;

        private XRBaseInteractable interactable;

        void Awake() {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null) {
                interactable.selectEntered.AddListener(OnSelect);
                interactable.activated.AddListener(OnActivate);
            }
        }

        private void OnSelect(SelectEnterEventArgs args) => Trigger(args.interactorObject);
        private void OnActivate(ActivateEventArgs args) => Trigger(args.interactorObject, 0.8f, 0.2f);

        public void TriggerHaptic(SelectEnterEventArgs args) => Trigger(args.interactorObject);

        public void Trigger(IXRInteractor interactor, float customIntensity = -1, float customDuration = -1) {
            if (interactor is XRBaseInputInteractor baseInputInteractor) {
                float i = customIntensity > 0 ? customIntensity : intensity;
                float d = customDuration > 0 ? customDuration : duration;
                baseInputInteractor.SendHapticImpulse(i, d);
            }
        }
    }

    // --- FLICKERING LIGHT (BUGGED LIGHTING) ---
    public class FlickeringLight : MonoBehaviour {
        public Light targetLight;
        public float minIntensity = 0.2f;
        public float maxIntensity = 1.0f;
        public float glitchChance = 0.05f;
        public AudioClip humSound;
        private AudioSource audioSource;

        private float baseIntensity;

        void Start() {
            if (targetLight == null) targetLight = GetComponent<Light>();
            if (targetLight != null) baseIntensity = targetLight.intensity;
            
            if (humSound != null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = humSound;
                audioSource.loop = true;
                audioSource.spatialBlend = 1.0f;
                audioSource.volume = 0.2f;
                audioSource.Play();
            }
        }

        void Update() {
            if (targetLight == null) return;
            
            if (Random.value < glitchChance) {
                float val = Random.Range(minIntensity, maxIntensity);
                targetLight.intensity = val;
                if (audioSource != null) audioSource.volume = val * 0.3f;
            } else {
                targetLight.intensity = Mathf.Lerp(targetLight.intensity, baseIntensity, Time.deltaTime * 5);
            }
        }
    }

    // --- FUNCTIONAL FLASHLIGHT ---
    public class VRFlashlight : MonoBehaviour {
        public Light flashlightLight;
        public AudioSource audioSource;
        public AudioClip clickSound;
        private bool isOn = true;

        void Awake() {
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.activated.AddListener(ToggleFlashlight);
            if (flashlightLight == null) flashlightLight = GetComponentInChildren<Light>();
        }

        void ToggleFlashlight(ActivateEventArgs args) {
            isOn = !isOn;
            if (flashlightLight != null) flashlightLight.enabled = isOn;
            if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
            
            if (args.interactorObject is XRBaseInputInteractor interactor) {
                interactor.SendHapticImpulse(0.5f, 0.05f);
            }
        }
    }

    // --- SIMPLE WEAPON (SHOOTING) ---
    public class VRWeapon : MonoBehaviour {
        public Transform muzzleTransform;
        public ParticleSystem muzzleFlash;
        public AudioSource audioSource;
        public AudioClip shootSound;
        public float shootForce = 500f;
        public float range = 100f;
        public LayerMask impactLayers = -1;

        void Awake() {
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.activated.AddListener(OnTriggerPressed);
        }

        void OnTriggerPressed(ActivateEventArgs args) {
            Shoot(args.interactorObject);
        }

        public void Shoot(IXRInteractor interactor) {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

            if (interactor is XRBaseInputInteractor controller) {
                controller.SendHapticImpulse(0.8f, 0.2f);
            }

            if (muzzleTransform != null) {
                if (Physics.Raycast(muzzleTransform.position, muzzleTransform.forward, out RaycastHit hit, range, impactLayers)) {
                    Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
                    if (hitRb != null) {
                        hitRb.AddForceAtPosition(muzzleTransform.forward * shootForce, hit.point);
                    }
                }
            }
        }
    }

    // --- COLLECTION SYSTEM ---
    public class CollectionItem : MonoBehaviour {
        public string itemName = "Medal";
        public AudioClip collectSound;

        void Awake() {
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.selectEntered.AddListener(OnGrabbed);
        }

        private void OnGrabbed(SelectEnterEventArgs args) {
            // In a real game, add to UI or counter
            if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, transform.position);
            Debug.Log("Item Collected: " + itemName);
            // We don't destroy it immediately so the user can see it in hand, 
            // but we could disable its 'collectible' state.
        }
    }
}
