using UnityEngine;

namespace Rihla
{
    /// <summary>A chase camera that follows whichever of the car or the walker is active.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8f, height = 3f, lookHeight = 1.2f, stiffness = 4f;

        public void Snap()
        {
            if (!target) return;
            transform.position = Desired();
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        Vector3 Desired()
        {
            Vector3 back = -target.forward; back.y = 0f;
            if (back.sqrMagnitude < 1e-4f) back = Vector3.back;
            Vector3 p = target.position + back.normalized * distance + Vector3.up * height;
            // Keep the camera above the ground.
            if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out var hit, 100f) && p.y < hit.point.y + 1f) p.y = hit.point.y + 1f;
            return p;
        }

        void LateUpdate()
        {
            if (!target) return;
            transform.position = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-stiffness * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
