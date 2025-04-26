using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    [Tooltip("Intervalle entre les sons de pas (en secondes)")]
    public float footstepInterval = 0.5f;
    
    [Tooltip("Le joueur doit-il être en mouvement pour jouer les sons de pas?")]
    public bool requireMovement = true;
    
    private CharacterController characterController;
    private float lastFootstepTime;
    
    void Start()
    {
        // Obtenir le CharacterController du joueur
        characterController = GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogWarning("FootstepManager: Aucun CharacterController trouvé sur le joueur. Les pas ne seront pas liés au mouvement.");
        }
    }
    
    void Update()
    {
        // Vérifier si le joueur est en mouvement (si requis)
        bool isMoving = !requireMovement;
        
        if (requireMovement && characterController != null)
        {
            // Vérifier si le joueur se déplace horizontalement
            Vector2 horizontalVelocity = new Vector2(characterController.velocity.x, characterController.velocity.z);
            isMoving = horizontalVelocity.magnitude > 0.1f && characterController.isGrounded;
        }
        
        // Jouer les sons de pas à intervalle régulier si le joueur est en mouvement
        if (isMoving && Time.time - lastFootstepTime > footstepInterval)
        {
            lastFootstepTime = Time.time;
            PlayFootstepSound();
        }
    }
    
    void PlayFootstepSound()
    {
        // Lancer un rayon vers le bas pour détecter la surface
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.2f))
        {
            // Vérifier si l'objet touché a un composant SoundWalkWood
            SoundWalkWood surfaceSound = hit.collider.GetComponent<SoundWalkWood>();
            
            if (surfaceSound != null)
            {
                // Utiliser la méthode du script SoundWalkWood pour jouer le son
                surfaceSound.PlayFootstepSound();
            }
        }
    }
}