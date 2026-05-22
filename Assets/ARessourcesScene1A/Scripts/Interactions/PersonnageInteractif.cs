using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PersonnageInteractif : MonoBehaviour
{
    [Header("Configuration")]
    public float distanceInteraction = 2.0f;
    public string playerTag = "Player";
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip souvenirAudioClip;
    [Range(0f, 1f)]
    public float volumeSon = 1.0f;
    
    [Header("Transition")]
    public float dureeFondu = 1.5f;
    public float delaiAvantChargement = 0.5f;
    public string nomSceneSouvenir = "Introduction";
    
    [Header("Interface Utilisateur")]
    public string texteInteraction = "Appuyez sur F pour interagir";
    public int tailleTexte = 20;

    [Header("VR")]
    public XRSimpleInteractable xrInteractable;
    
    private bool estJoueurProche = false;
    private bool estEnInteraction = false;
    private bool transitionEnCours = false;
    private GameObject objetJoueur;
    private CharacterController controleurJoueur;
    private PlayerInput inputJoueur;
    private Rigidbody rigidbodyJoueur;
    private MonoBehaviour[] scriptsDeplacementJoueur;
    private GUIStyle styleTexte;
    
    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.volume = volumeSon;
            }
        }
        
        if (audioSource != null) audioSource.volume = volumeSon;

        if (xrInteractable == null) xrInteractable = GetComponent<XRSimpleInteractable>();
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
        }
        
        styleTexte = new GUIStyle();
        styleTexte.fontSize = tailleTexte;
        styleTexte.normal.textColor = Color.white;
        styleTexte.alignment = TextAnchor.MiddleCenter;
        styleTexte.fontStyle = FontStyle.Bold;
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        if (!estEnInteraction && !transitionEnCours)
        {
            CommencerInteraction();
        }
    }
    
    void Update()
    {
        if (transitionEnCours) return;
        VerifierProximiteJoueur();
        
        if (estJoueurProche && !estEnInteraction && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            CommencerInteraction();
        }
    }
    
    void VerifierProximiteJoueur()
    {
        GameObject joueur = GameObject.FindGameObjectWithTag(playerTag);
        if (joueur != null)
        {
            float distance = Vector3.Distance(transform.position, joueur.transform.position);
            estJoueurProche = distance <= distanceInteraction;
            if (estJoueurProche && objetJoueur == null)
            {
                objetJoueur = joueur;
                controleurJoueur = joueur.GetComponent<CharacterController>();
                inputJoueur = joueur.GetComponent<PlayerInput>();
                rigidbodyJoueur = joueur.GetComponent<Rigidbody>();
                scriptsDeplacementJoueur = joueur.GetComponents<MonoBehaviour>();
            }
        }
    }
    
    public void CommencerInteraction()
    {
        estEnInteraction = true;
        DesactiverDeplacementJoueur();
        
        if (souvenirAudioClip != null)
        {
            audioSource.clip = souvenirAudioClip;
            audioSource.volume = volumeSon;
            audioSource.Play();
            StartCoroutine(AttendreFinSonEtTransition());
        }
        else
        {
            StartCoroutine(FaireFonduEtChargerScene());
        }
    }
    
    IEnumerator AttendreFinSonEtTransition()
    {
        while (audioSource.isPlaying) yield return null;
        yield return new WaitForSeconds(delaiAvantChargement);
        StartCoroutine(FaireFonduEtChargerScene());
    }
    
    IEnumerator FaireFonduEtChargerScene()
    {
        transitionEnCours = true;
        if (TransitionSceneManager.Instance != null)
        {
            yield return StartCoroutine(TransitionSceneManager.Instance.FonduSortie(dureeFondu));
        }
        else
        {
            yield return new WaitForSeconds(dureeFondu);
        }
        
        if (Application.CanStreamedLevelBeLoaded(nomSceneSouvenir))
        {
            SceneManager.LoadScene(nomSceneSouvenir);
        }
        else
        {
            Debug.LogError("La scène '" + nomSceneSouvenir + "' n'a pas pu être chargée.");
            ReactiverDeplacementJoueur();
            estEnInteraction = false;
            transitionEnCours = false;
            if (TransitionSceneManager.Instance != null)
            {
                StartCoroutine(TransitionSceneManager.Instance.FonduEntree());
            }
        }
    }
    
    void DesactiverDeplacementJoueur()
    {
        if (UnityEngine.XR.XRSettings.enabled) return;

        if (controleurJoueur != null) controleurJoueur.enabled = false;
        if (inputJoueur != null) inputJoueur.enabled = false;
        if (rigidbodyJoueur != null && !rigidbodyJoueur.isKinematic)
        {
            rigidbodyJoueur.linearVelocity = Vector3.zero;
            rigidbodyJoueur.angularVelocity = Vector3.zero;
            rigidbodyJoueur.isKinematic = true;
        }
        
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    if (script != this) script.enabled = false;
                }
            }
        }
    }
    
    void ReactiverDeplacementJoueur()
    {
        if (UnityEngine.XR.XRSettings.enabled) return;

        if (controleurJoueur != null) controleurJoueur.enabled = true;
        if (inputJoueur != null) inputJoueur.enabled = true;
        if (rigidbodyJoueur != null) rigidbodyJoueur.isKinematic = false;
        
        if (scriptsDeplacementJoueur != null)
        {
            foreach (MonoBehaviour script in scriptsDeplacementJoueur)
            {
                string nomScript = script.GetType().Name.ToLower();
                if (nomScript.Contains("move") || nomScript.Contains("controller") || 
                    nomScript.Contains("motor") || nomScript.Contains("character") ||
                    nomScript.Contains("player") || nomScript.Contains("input"))
                {
                    if (script != this) script.enabled = true;
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceInteraction);
    }
    
    void OnGUI()
    {
        if (Application.isBatchMode) return;
        if (estJoueurProche && !estEnInteraction && !transitionEnCours)
        {
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
            float largeurTexte = 300;
            float hauteurTexte = 30;
            Rect positionTexte = new Rect((Screen.width - largeurTexte) / 2, Screen.height - hauteurTexte - 50, largeurTexte, hauteurTexte);
            GUI.Box(positionTexte, "");
            GUI.Label(positionTexte, texteInteraction, styleTexte);
        }
    }
}