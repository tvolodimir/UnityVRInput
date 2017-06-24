using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

namespace VRInput
{
    public class ParabolaPointer : RayPointer
    {
        #region Handle

        public EVRButtonId Button = EVRButtonId.k_EButton_SteamVR_Trigger;
        [SerializeField] private SteamVR_TrackedObject trackedObject;

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
            return trackedObject ? SteamVR_Controller.Input((int) trackedObject.index) : null;
        }

        #endregion

        public float laserThickness = 0.002f;
        public float laserHitScale = 0.02f;
        public Color color;
        public float initialSpeed;

        private int len;
        private readonly Vector3[] positions = new Vector3[256];
        private float dist = 1000;
        private LineRenderer lineRenderer;
        private GameObject hitPoint;
        private float time;

        protected override void Start()
        {
            base.Start();

            var newMaterial = new Material(Shader.Find("VRInput/LaserPointer"));
            newMaterial.SetColor("_Color", color);

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = newMaterial;
            lineRenderer.widthMultiplier = laserThickness;

            hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitPoint.transform.SetParent(transform, false);
            hitPoint.transform.localScale = new Vector3(laserHitScale, laserHitScale, laserHitScale);
            hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);
            hitPoint.SetActive(false);
            DestroyImmediate(hitPoint.GetComponent<SphereCollider>());
        }

        protected override void Draw(bool isHit, float distance)
        {
            var ray = LastRay;

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

        public override bool GetRay(out Ray finalRay, Func<Vector3, Vector3, RaycastResult> raycast)
        {
            var g = Physics.gravity;
            var startSpeed = transform.forward.normalized * initialSpeed;
            var startPosition = transform.position;
            var result = ParabolaHelpers.FindRay(g, startPosition, startSpeed, dist, raycast, out LastRay, out time);
            finalRay = LastRay;
            BuildLines(time, g);
            return result;
        }

        private void BuildLines(float totalTime, Vector3 gravity)
        {
            if (totalTime <= 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            var dt = 0.05f;

            len = (int) (totalTime / dt) + 1;
            if (len > positions.Length)
                len = positions.Length;

            var startSpeed = transform.forward.normalized * initialSpeed;
            var startPosition = transform.position;

            for (var i = 0; i < len; i++)
            {
                var t = (totalTime * i) / (len - 1);
                var point = ParabolaHelpers.GetArcPositionAtTime(t, startSpeed, startPosition, gravity);
                positions[i] = transform.worldToLocalMatrix.MultiplyPoint(point);
            }

            lineRenderer.positionCount = len;
            lineRenderer.SetPositions(positions);
        }
    }

    public static class ParabolaHelpers
    {
        public static Vector3 GetArcPositionAtTime(float time, Vector3 startSpeed, Vector3 startPosition,
            Vector3 gravity)
        {
            return startPosition + startSpeed * time + 0.5f * time * time * gravity;
        }

        public static float GetArcTimeAtPosition(Vector3 startSpeed, Vector3 startPosition, Vector3 gravity, Vector3 xt,
            float neart)
        {
            var gn = gravity.normalized;
            var a = 0.5f * Vector3.Dot(gn, gravity);
            var b = Vector3.Dot(gn, startSpeed);
            var c = Vector3.Dot(gn, startPosition) - Vector3.Dot(gn, xt);
            var d2 = b * b - 4 * a * c;
            if (d2 < 0) return 0;
            var d = Mathf.Sqrt(b * b - 4 * a * c);
            var t1 = (-b + d) / (2 * a);
            var t2 = (-b - d) / (2 * a);

            return Mathf.Abs(t1 - neart) > Mathf.Abs(t2 - neart) ? t2 : t1;
        }

        public static bool FindRay(
            Vector3 gravity,
            Vector3 startPosition,
            Vector3 startSpeed,
            float maxDistance,
            Func<Vector3, Vector3, RaycastResult> raycast,
            out Ray ray,
            out float time)
        {
            var g = gravity;
            var dt = 0.05f;
            var backMultiplier = 0.1f;
            var forwardMultiplier = 0.1f;

            ray = default(Ray);
            time = 0;

            float t = 0;
            float totalDistance = 0;
            var hitSomething = false;

            var p0 = startPosition;

            while (totalDistance < maxDistance)
            {
                t += dt;
                var p1 = GetArcPositionAtTime(t, startSpeed, startPosition, g);

                var deltaDistance = Vector3.Distance(p0, p1);

                totalDistance += deltaDistance;

                var deltaDirection = (p1 - p0).normalized;

                var p0Back = p0 - deltaDirection * backMultiplier;

                var hitResult = raycast(p0Back, deltaDirection);

                if (hitResult.distance > 0 &&
                    hitResult.distance <= (1 + backMultiplier + forwardMultiplier) * deltaDistance)
                {
                    var endPoint = p0Back + deltaDirection * hitResult.distance;
                    time = GetArcTimeAtPosition(startSpeed, startPosition, g, endPoint, t);
                    ray = new Ray(p0Back, deltaDirection);
                    hitSomething = true;
                    break;
                }

                p0 = p1;
            }
            if (!hitSomething)
            {
                time = t;
            }
            return hitSomething;
        }
    }
}
