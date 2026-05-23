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

        public void TriggerHaptic(SelectEnterEventArgs args) {
            Trigger(args.interactorObject);
        }

        public void Trigger(IXRInteractor interactor) {
            if (interactor is XRBaseInputInteractor baseInputInteractor) {
                baseInputInteractor.SendHapticImpulse(intensity, duration);
            }
        }
    }

    // --- FLICKERING LIGHT (BUGGED LIGHTING) ---
    public class FlickeringLight : MonoBehaviour {
        public Light targetLight;
        public float minIntensity = 0.2f;
        public float maxIntensity = 1.0f;
        public float flickerSpeed = 0.1f;
        public float glitchChance = 0.05f;

        private float baseIntensity;

        void Start() {
            if (targetLight == null) targetLight = GetComponent<Light>();
            if (targetLight != null) baseIntensity = targetLight.intensity;
        }

        void Update() {
            if (targetLight == null) return;
            
            if (Random.value < glitchChance) {
                targetLight.intensity = Random.Range(minIntensity, maxIntensity);
            } else {
                targetLight.intensity = Mathf.Lerp(targetLight.intensity, baseIntensity, Time.deltaTime * 5);
            }
        }
    }

    // --- SIMPLE WEAPON (SHOOTING) ---
    public class VRWeapon : MonoBehaviour {
        public Transform muzzleTransform;
        public ParticleSystem muzzleFlash;
        public AudioSource audioSource;
        public AudioClip shootSound;
        public float shootForce = 100f;
        public float range = 50f;
        public LayerMask impactLayers = -1;

        private XRGrabInteractable grabInteractable;

        void Awake() {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null) {
                grabInteractable.activated.AddListener(OnTriggerPressed);
            }
        }

        void OnTriggerPressed(ActivateEventArgs args) {
            Shoot(args.interactorObject);
        }

        public void Shoot(IXRInteractor interactor) {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

            // Haptic feedback
            if (interactor is XRBaseInputInteractor controller) {
                controller.SendHapticImpulse(0.7f, 0.15f);
            }

            // Raycast for impact
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
        private AudioSource audioSource;

        void Start() {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void OnCollected() {
            if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, transform.position);
            Debug.Log("Collected item: " + itemName);
            gameObject.SetActive(false); // Hide it
        }
    }
}
