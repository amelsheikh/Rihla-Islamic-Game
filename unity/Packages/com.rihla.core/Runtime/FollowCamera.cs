using UnityEngine;
using UnityEngine.InputSystem;

namespace Rihla
{
    /// <summary>
    /// A chase camera that follows whichever of the car or the walker is active. It keeps out of walls (so it
    /// works in narrow streets) and above the ground. In first person it sits at the walker's eyes; the right
    /// mouse button (or the right stick) looks up and down.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8f, height = 3f, lookHeight = 1.2f, stiffness = 4f;
        public bool firstPerson;
        public float eyeHeight = 1.3f;
        float pitch;

        public void Snap()
        {
            if (!target) return;
            if (firstPerson) { FirstPerson(); return; }
            transform.position = Desired();
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        Vector3 Desired()
        {
            Vector3 back = -target.forward; back.y = 0f;
            if (back.sqrMagnitude < 1e-4f) back = Vector3.back;
            Vector3 pivot = target.position + Vector3.up * lookHeight;
            Vector3 p = target.position + back.normalized * distance + Vector3.up * height;
            // Keep out of walls between the target and the camera.
            Vector3 d = p - pivot; float len = d.magnitude;
            if (len > 0.01f && Physics.SphereCast(pivot, 0.3f, d / len, out var wall, len, ~0, QueryTriggerInteraction.Ignore)
                && !wall.transform.IsChildOf(target))
                p = pivot + d / len * Mathf.Max(0.4f, wall.distance - 0.1f);
            // Keep above the ground just under the camera (not the roofs high above it).
            if (Physics.Raycast(new Vector3(p.x, pivot.y + 2f, p.z), Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target) && p.y < hit.point.y + 0.5f)
                p.y = hit.point.y + 0.5f;
            return p;
        }

        bool bodyHidden;

        /// <summary>In first person the child's own body is hidden, so the camera is not inside it.</summary>
        void ShowBody(bool show)
        {
            if (!target || bodyHidden == !show) return;
            bodyHidden = !show;
            foreach (var r in target.GetComponentsInChildren<Renderer>()) r.enabled = show;
        }

        void FirstPerson()
        {
            ShowBody(false);
            transform.position = target.position + Vector3.up * eyeHeight;
            transform.rotation = Quaternion.Euler(pitch, target.eulerAngles.y, 0f);
        }

        void LateUpdate()
        {
            if (!target) return;
            if (firstPerson)
            {
                var mouse = Mouse.current; var pad = Gamepad.current;
                if (mouse != null && mouse.rightButton.isPressed) pitch -= mouse.delta.y.ReadValue() * 0.15f;
                if (pad != null) pitch -= pad.rightStick.y.ReadValue() * 90f * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, -60f, 70f);
                FirstPerson();
                return;
            }
            ShowBody(true);
            transform.position = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-stiffness * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
