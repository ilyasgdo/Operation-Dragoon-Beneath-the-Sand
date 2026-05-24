using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VRMovementSetup : MonoBehaviour
{
    public InputActionAsset actionAsset;
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;

    private ActionBasedContinuousMoveProvider moveProvider;
    private InputAction runAction;

    void Start()
    {
        if (actionAsset == null)
        {
#if UNITY_EDITOR
            actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/XRI Default Input Actions.inputactions");
#endif
        }

        if (actionAsset == null) return;

        var locomotionSystem = GetComponentInChildren<LocomotionSystem>();
        if (locomotionSystem == null) locomotionSystem = GetComponent<LocomotionSystem>();
        if (locomotionSystem == null) return;

        // Setup Snap Turn
        var turnProvider = locomotionSystem.GetComponent<ActionBasedSnapTurnProvider>();
        if (turnProvider != null)
        {
            turnProvider.system = locomotionSystem;
            var turnAction = actionAsset.FindAction("XRI Left Locomotion/Turn");
            if (turnAction != null)
            {
                turnProvider.leftHandSnapTurnAction = new InputActionProperty(turnAction);
                turnProvider.rightHandSnapTurnAction = new InputActionProperty(); 
            }
        }

        // Setup Continuous Move
        moveProvider = locomotionSystem.GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.system = locomotionSystem;
            var moveAct = actionAsset.FindAction("XRI Right Locomotion/Move");
            if (moveAct != null)
            {
                moveProvider.leftHandMoveAction = new InputActionProperty();
                moveProvider.rightHandMoveAction = new InputActionProperty(moveAct);
                moveProvider.moveSpeed = walkSpeed;
            }
        }

        // Setup Run Action (Left Stick Click usually)
        runAction = actionAsset.FindAction("XRI Right Locomotion/Move"); // Reuse move but we check stick click
        // Often XRI has a specific action for sprint, but let's try to find it or use a default binding
        var sprint = actionAsset.FindAction("XRI Right/Thumbstick Click"); 
        if (sprint == null) sprint = actionAsset.FindAction("Sprint");
        runAction = sprint;

        // Setup Controllers
        var controllers = GetComponentsInChildren<ActionBasedController>();
        foreach (var controller in controllers)
        {
            string hand = controller.name.Contains("Left") ? "Left" : "Right";
            var posAction = actionAsset.FindAction("XRI " + hand + "/Position");
            var rotAction = actionAsset.FindAction("XRI " + hand + "/Rotation");
            if (posAction != null) controller.positionAction = new InputActionProperty(posAction);
            if (rotAction != null) controller.rotationAction = new InputActionProperty(rotAction);
            
            var selectAction = actionAsset.FindAction("XRI " + hand + " Interaction/Select");
            var activateAction = actionAsset.FindAction("XRI " + hand + " Interaction/Activate");
            if (selectAction != null) controller.selectAction = new InputActionProperty(selectAction);
            if (activateAction != null) controller.activateAction = new InputActionProperty(activateAction);

            var visual = controller.transform.Find("HandVisual");
            if (visual != null) visual.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (moveProvider != null && runAction != null)
        {
            bool isRunning = runAction.ReadValue<float>() > 0.1f;
            moveProvider.moveSpeed = isRunning ? runSpeed : walkSpeed;
        }
    }
}
