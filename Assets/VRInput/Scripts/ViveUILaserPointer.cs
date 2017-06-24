using UnityEngine;
using Valve.VR;

namespace VRInput
{
    public class ViveUILaserPointer : RayPointer
    {
        #region Handle

        public EVRButtonId Button = EVRButtonId.k_EButton_SteamVR_Trigger;
        [SerializeField]
        private SteamVR_TrackedObject trackedObject;

        public override bool ButtonDown()
        {
            var device = GetDevice();
            return device != null && device.GetPressDown(Button);
        }

        public override bool ButtonUp()
        {
            var device = GetDevice();
            return device != null && device.GetPressUp(Button);
        }

        public override void OnEnterControl(GameObject control)
        {
            var device = GetDevice();
            if (device != null)
                device.TriggerHapticPulse(1000);
        }

        public override void OnExitControl(GameObject control)
        {
            var device = GetDevice();
            if (device != null)
                device.TriggerHapticPulse(600);
        }

        private SteamVR_Controller.Device GetDevice()
        {
            return trackedObject ? SteamVR_Controller.Input((int)trackedObject.index) : null;
        }

        #endregion

        public float laserThickness = 0.002f;
        public float laserHitScale = 0.02f;
        public Color color;

        private GameObject hitPoint;
        private GameObject pointer;

        protected override void Start()
        {
            base.Start();

            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
            pointer.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);

            hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitPoint.transform.SetParent(transform, false);
            hitPoint.transform.localScale = new Vector3(laserHitScale, laserHitScale, laserHitScale);
            hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);

            hitPoint.SetActive(false);

            // remove the colliders on our primitives
            DestroyImmediate(hitPoint.GetComponent<SphereCollider>());
            DestroyImmediate(pointer.GetComponent<BoxCollider>());

            var newMaterial = new Material(Shader.Find("VRInput/LaserPointer"));

            newMaterial.SetColor("_Color", color);
            pointer.GetComponent<MeshRenderer>().material = newMaterial;
            hitPoint.GetComponent<MeshRenderer>().material = newMaterial;
        }

        protected override void Draw(bool isHit, float distance)
        {
            var ray = LastRay;

            if (ray.direction == Vector3.zero)
            {
                pointer.SetActive(false);
            }
            else
            {
                pointer.SetActive(true);
                pointer.transform.localScale = new Vector3(laserThickness, laserThickness, distance);
                pointer.transform.position = ray.origin;
                pointer.transform.transform.rotation = Quaternion.LookRotation(ray.direction);
                pointer.transform.Translate(new Vector3(0.0f, 0.0f, distance * 0.5f));
            }

            if (ray.direction == Vector3.zero)
            {
                hitPoint.SetActive(false);
            }
            else
            {
                if (isHit)
                {
                    hitPoint.SetActive(true);
                    hitPoint.transform.position = ray.origin;
                    hitPoint.transform.transform.rotation = Quaternion.LookRotation(ray.direction);
                    hitPoint.transform.Translate(new Vector3(0.0f, 0.0f, distance));
                }
                else
                {
                    hitPoint.SetActive(false);
                }
            }
        }
    }
}
