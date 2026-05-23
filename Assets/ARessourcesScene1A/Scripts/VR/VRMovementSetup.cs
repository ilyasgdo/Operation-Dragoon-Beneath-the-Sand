using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.InputSystem;
using UnityEditor;

public class VRMovementSetup : MonoBehaviour
{
    public InputActionAsset actionAsset;

    void Start()
    {
        if (actionAsset == null)
        {
            Debug.LogWarning("VRMovementSetup: actionAsset is null. Trying to find default XRI actions.");
#if UNITY_EDITOR
            actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/XRI Default Input Actions.inputactions");
#endif
        }

        if (actionAsset == null)
        {
            Debug.LogError("VRMovementSetup: No action asset found. Movement and controls will not be configured.");
            return;
        }

        var locomotionSystem = GetComponentInChildren<LocomotionSystem>();
        if (locomotionSystem == null) locomotionSystem = GetComponent<LocomotionSystem>();
        if (locomotionSystem == null) return;

        // User requested Left Joystick for Camera orientation (Snap Turn)
        var turnProvider = locomotionSystem.GetComponent<ActionBasedSnapTurnProvider>();
        if (turnProvider != null)
        {
            turnProvider.system = locomotionSystem;
            var turnAction = actionAsset.FindAction("XRI Left Locomotion/Turn");
            if (turnAction != null)
            {
                turnProvider.leftHandSnapTurnAction = new InputActionProperty(turnAction);
                turnProvider.rightHandSnapTurnAction = new InputActionProperty(); // Disable right hand
                Debug.Log("VR: Turn Provider (Left Hand) configured");
            }
            else
            {
                Debug.LogWarning("VR: Turn action not found in action asset.");
            }
        }

        // Setup Continuous Move (moved to Right Hand to avoid conflict)
        var moveProvider = locomotionSystem.GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.system = locomotionSystem;
            var moveAction = actionAsset.FindAction("XRI Right Locomotion/Move");
            if (moveAction != null)
            {
                moveProvider.leftHandMoveAction = new InputActionProperty(); // Disable left hand
                moveProvider.rightHandMoveAction = new InputActionProperty(moveAction);
                moveProvider.moveSpeed = 2.0f;
                Debug.Log("VR: Move Provider (Right Hand) configured");
            }
            else
            {
                Debug.LogWarning("VR: Move action not found in action asset.");
            }
        }

        // Setup Controllers with proper tracking
        var controllers = GetComponentsInChildren<ActionBasedController>();
        foreach (var controller in controllers)
        {
            string hand = controller.name.Contains("Left") ? "Left" : "Right";
            
            var posAction = actionAsset.FindAction("XRI " + hand + "/Position");
            var rotAction = actionAsset.FindAction("XRI " + hand + "/Rotation");
            var trackingStateAction = actionAsset.FindAction("XRI " + hand + "/Tracking State");
            
            if (posAction != null) controller.positionAction = new InputActionProperty(posAction);
            if (rotAction != null) controller.rotationAction = new InputActionProperty(rotAction);
            if (trackingStateAction != null) controller.trackingStateAction = new InputActionProperty(trackingStateAction);
            
            // Interaction actions
            var selectAction = actionAsset.FindAction("XRI " + hand + " Interaction/Select");
            var activateAction = actionAsset.FindAction("XRI " + hand + " Interaction/Activate");
            
            if (selectAction != null) controller.selectAction = new InputActionProperty(selectAction);
            if (activateAction != null) controller.activateAction = new InputActionProperty(activateAction);
            
            // Ensure Visuals are enabled
            var visual = controller.transform.Find("HandVisual");
            if (visual != null) 
            {
                visual.gameObject.SetActive(true);
                Debug.Log("VR: Enabled HandVisual for " + controller.name);
            }
        }
    }
}
