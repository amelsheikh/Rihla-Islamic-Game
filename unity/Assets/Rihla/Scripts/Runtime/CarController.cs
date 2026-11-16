using UnityEngine;
using UnityEngine.InputSystem;

namespace Rihla
{
    /// <summary>
    /// A placeholder car on four WheelColliders: arrow keys or W A S D, Space for the handbrake,
    /// R to put it back on the road, P for autopilot (Jiddo drives). Start the player with
    /// -autopilot on the command line to have it drive the whole road by itself.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
        public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;
        public RoadPath road;
        public float motorTorque = 900f, brakeTorque = 3000f, maxSteer = 32f, topSpeedKmh = 110f;
        public bool autopilot;
        public float autopilotKmh = 60f;
        [HideInInspector] public bool driverPresent = true;

        Rigidbody body;
        int roadHint = -1;
        float steer;

        public float SpeedKmh => body ? body.linearVelocity.magnitude * 3.6f : 0f;
        public int RoadIndex => roadHint;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = new Vector3(0f, 0.35f, 0.1f);
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-autopilot") autopilot = true;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.pKey.wasPressedThisFrame) autopilot = !autopilot;
            if (kb.rKey.wasPressedThisFrame && driverPresent) ResetToRoad();
        }

        void FixedUpdate()
        {
            if (road != null) roadHint = road.Nearest(transform.position, roadHint);
            float throttle = 0f, steerInput = 0f; bool handbrake = false;
            var kb = Keyboard.current; var pad = Gamepad.current;

            if (!driverPresent) { throttle = 0f; handbrake = true; }
            else if (autopilot && road != null) Autopilot(out throttle, out steerInput);
            else
            {
                if (kb != null)
                {
                    if (kb.upArrowKey.isPressed || kb.wKey.isPressed) throttle += 1f;
                    if (kb.downArrowKey.isPressed || kb.sKey.isPressed) throttle -= 1f;
                    if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) steerInput -= 1f;
                    if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) steerInput += 1f;
                    handbrake = kb.spaceKey.isPressed;
                }
                if (pad != null)
                {
                    throttle += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                    steerInput += pad.leftStick.x.ReadValue();
                    handbrake |= pad.buttonSouth.isPressed;
                }
            }

            // Less steering at speed; smooth the input so keys do not jerk the wheel.
            float speed = SpeedKmh, steerLimit = Mathf.Lerp(maxSteer, maxSteer * 0.3f, speed / topSpeedKmh);
            steer = Mathf.MoveTowards(steer, Mathf.Clamp(steerInput, -1f, 1f) * steerLimit, 120f * Time.fixedDeltaTime);
            frontLeft.steerAngle = frontRight.steerAngle = steer;

            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            bool braking = (throttle < 0f && forwardSpeed > 1f) || (throttle > 0f && forwardSpeed < -1f);
            float drive = braking ? 0f : throttle * motorTorque * (speed < topSpeedKmh ? 1f : 0f);
            float brake = braking ? Mathf.Abs(throttle) * brakeTorque : (Mathf.Approximately(throttle, 0f) ? 40f : 0f);
            rearLeft.motorTorque = rearRight.motorTorque = drive * 0.5f;
            frontLeft.motorTorque = frontRight.motorTorque = drive * 0.5f;
            frontLeft.brakeTorque = frontRight.brakeTorque = brake;
            rearLeft.brakeTorque = rearRight.brakeTorque = handbrake ? brakeTorque * 2f : brake;

            Sync(frontLeft, frontLeftMesh); Sync(frontRight, frontRightMesh);
            Sync(rearLeft, rearLeftMesh); Sync(rearRight, rearRightMesh);
        }

        void Autopilot(out float throttle, out float steerInput)
        {
            float speed = SpeedKmh;
            Vector3 target = road.Ahead(roadHint, Mathf.Clamp(speed * 0.35f, 8f, 25f));
            Vector3 local = transform.InverseTransformPoint(target);
            steerInput = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg / maxSteer, -1f, 1f);
            // Slow for bends: look further ahead and compare headings.
            Vector3 a = road.Ahead(roadHint, 10f) - road.Ahead(roadHint, 0f), b = road.Ahead(roadHint, 45f) - road.Ahead(roadHint, 30f);
            float bend = Vector3.Angle(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
            float want = Mathf.Lerp(autopilotKmh, 25f, bend / 60f);
            throttle = Mathf.Clamp((want - speed) * 0.15f, -1f, 1f);
            if (roadHint >= road.points.Length - 2) throttle = speed > 1f ? -1f : 0f;
        }

        public void ResetToRoad()
        {
            int i = road ? road.Nearest(transform.position) : 0;
            if (road == null) return;
            i = Mathf.Min(i, road.points.Length - 2);
            Vector3 p = road.points[i], dir = road.points[i + 1] - p; dir.y = 0f;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(p + Vector3.up * 0.6f, Quaternion.LookRotation(dir.normalized));
            roadHint = i;
        }

        static void Sync(WheelCollider c, Transform mesh)
        {
            if (!c || !mesh) return;
            c.GetWorldPose(out var pos, out var rot);
            mesh.SetPositionAndRotation(pos, rot * Quaternion.Euler(0f, 0f, 90f));
        }
    }
}
