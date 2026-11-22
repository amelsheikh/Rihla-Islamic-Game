using UnityEngine;
using UnityEngine.InputSystem;

namespace Rihla
{
    /// <summary>E gets out of the car when it has stopped, and back in when the child is beside it.</summary>
    public class PlayerSwitch : MonoBehaviour
    {
        public CarController car;
        public Walker walker;
        public FollowCamera cam;
        public bool OnFoot => walker && walker.gameObject.activeSelf;
        public bool NearCar => OnFoot && Vector3.Distance(walker.transform.position, car.transform.position) < 4f;

        void Start() => Drive();

        void Update()
        {
            var kb = Keyboard.current; var pad = Gamepad.current;
            bool pressed = (kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            if (!pressed) return;
            if (!OnFoot && car.SpeedKmh < 3f) GetOut();
            else if (NearCar) Drive();
        }

        public void GetOut()
        {
            walker.transform.SetPositionAndRotation(car.transform.position - car.transform.right * 2.2f + Vector3.up * 0.5f,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(car.transform.forward, Vector3.up)));
            walker.gameObject.SetActive(true);
            car.driverPresent = false;
            cam.target = walker.transform; cam.distance = 4f; cam.height = 1.8f; cam.lookHeight = 1.3f;
        }

        public void Drive()
        {
            walker.gameObject.SetActive(false);
            car.driverPresent = true;
            cam.target = car.transform; cam.distance = 8f; cam.height = 3f; cam.lookHeight = 1.2f;
        }
    }
}
