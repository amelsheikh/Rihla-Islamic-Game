using UnityEngine;
using UnityEngine.InputSystem;

namespace Rihla
{
    /// <summary>The child on foot: W A S D or arrows to walk and turn, Shift to run, right mouse button to look.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class Walker : MonoBehaviour
    {
        public float walkSpeed = 1.6f, runSpeed = 4f, turnSpeed = 120f;
        CharacterController cc;
        float fall;
        public float Speed { get; private set; }
        /// <summary>For tests (-autopilot in Ariha): walk to this point by itself.</summary>
        [System.NonSerialized] public Vector3? autoTarget;
        /// <summary>Inside a sacred place the child walks slowly and cannot run (brief section 5: "a slow walk").</summary>
        [System.NonSerialized] public bool slow;

        void Awake() => cc = GetComponent<CharacterController>();

        void Update()
        {
            var kb = Keyboard.current; var pad = Gamepad.current;
            float fwd = 0f, turn = 0f; bool run = false;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) fwd += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) fwd -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) turn -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turn += 1f;
                run = kb.leftShiftKey.isPressed;
            }
            if (pad != null) { fwd += pad.leftStick.y.ReadValue(); turn += pad.leftStick.x.ReadValue(); run |= pad.leftStickButton.isPressed; }
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed) turn += mouse.delta.x.ReadValue() * 0.15f;

            if (autoTarget.HasValue)
            {
                Vector3 d = autoTarget.Value - transform.position; d.y = 0f;
                if (d.magnitude > 0.6f) { fwd = 1f; turn = Mathf.Clamp(Vector3.SignedAngle(transform.forward, d, Vector3.up) / 30f, -3f, 3f); run = d.magnitude > 8f; }
            }
            transform.Rotate(0f, Mathf.Clamp(turn, -3f, 3f) * turnSpeed * Time.deltaTime, 0f);
            if (slow) run = false;
            Speed = Mathf.Clamp(fwd, -0.5f, 1f) * (run ? runSpeed : walkSpeed) * (slow ? 0.8f : 1f);
            fall = cc.isGrounded ? -1f : fall - 9.81f * Time.deltaTime;
            cc.Move((transform.forward * Speed + Vector3.up * fall) * Time.deltaTime);
        }
    }
}
