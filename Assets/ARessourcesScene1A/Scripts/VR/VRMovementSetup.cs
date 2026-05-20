using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.InputSystem;

public class VRMovementSetup : MonoBehaviour
{
    public InputActionAsset actionAsset;

    void Start()
    {
        if (actionAsset == null) return;

        var locomotionSystem = GetComponentInChildren<LocomotionSystem>();
        if (locomotionSystem == null) return;

        // Setup Continuous Move
        var moveProvider = locomotionSystem.GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.system = locomotionSystem;
            moveProvider.leftHandMoveAction = new InputActionProperty(actionAsset.FindAction("Player/Move"));
            moveProvider.moveSpeed = 2.0f; // Adjusted for VR
            Debug.Log("VR: Move Provider configured");
        }

        // Setup Snap Turn
        var turnProvider = locomotionSystem.GetComponent<ActionBasedSnapTurnProvider>();
        if (turnProvider != null)
        {
            turnProvider.system = locomotionSystem;
            turnProvider.rightHandSnapTurnAction = new InputActionProperty(actionAsset.FindAction("Player/Look"));
            Debug.Log("VR: Turn Provider configured");
        }

        // Setup Controllers
        var controllers = GetComponentsInChildren<ActionBasedController>();
        foreach (var controller in controllers)
        {
            if (controller.name.Contains("Left"))
            {
                controller.positionAction = new InputActionProperty(actionAsset.FindAction("UI/TrackedDevicePosition"));
                controller.rotationAction = new InputActionProperty(actionAsset.FindAction("UI/TrackedDeviceOrientation"));
                // Add more if needed
            }
            else if (controller.name.Contains("Right"))
            {
                controller.positionAction = new InputActionProperty(actionAsset.FindAction("UI/TrackedDevicePosition"));
                controller.rotationAction = new InputActionProperty(actionAsset.FindAction("UI/TrackedDeviceOrientation"));
            }
        }
    }
}
