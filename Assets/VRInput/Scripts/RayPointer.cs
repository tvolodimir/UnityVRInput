using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRInput
{
    public abstract class RayPointer : MonoBehaviour
    {
        private float distanceLimit;
        private bool resultDrawn = true;
        protected Ray LastRay;

        public abstract bool ButtonDown();
        public abstract bool ButtonUp();

        protected virtual void Start()
        {
            if (LaserPointerInputModule.Instance == null)
            {
                new GameObject().AddComponent<LaserPointerInputModule>();
            }

            LaserPointerInputModule.Instance.AddController(this);
        }

        protected virtual void OnDestroy()
        {
            if (LaserPointerInputModule.Instance != null)
                LaserPointerInputModule.Instance.RemoveController(this);
        }

        public virtual void OnEnterControl(GameObject control)
        {
        }

        public virtual void OnExitControl(GameObject control)
        {
        }

        protected virtual void Draw(bool isHit, float distance)
        {
        }

        protected virtual void Update()
        {
            MakeEverFocus(EventSystem.current, Time.frameCount);

            var bHit = false;
            var distance = 100.0f;

            if (!resultDrawn && distanceLimit > 0)
            {
                distance = distanceLimit;
                bHit = true;
            }

            Draw(bHit, distance);

            resultDrawn = true;
        }

        public virtual bool GetRay(out Ray finalRay, Func<Vector3, Vector3, RaycastResult> raycast)
        {
            LastRay = new Ray(transform.position, transform.forward);
            finalRay = LastRay;
            return true;
        }

        // limits the laser distance for the current frame
        public virtual void LimitLaserDistance(float distance)
        {
            distanceLimit = distance;
            resultDrawn = false;
        }

        private static FieldInfo m_PausedFiled;
        private static int lastFrameCount;

        private static void MakeEverFocus(EventSystem eventSystem, int frameCount)
        {
            if (lastFrameCount == frameCount) return;

            if (m_PausedFiled == null)
                m_PausedFiled = eventSystem.GetType()
                    .GetField("m_Paused", BindingFlags.NonPublic
                                          | BindingFlags.Instance);
            if (m_PausedFiled != null)
                m_PausedFiled.SetValue(eventSystem, false);
        }
    }
}
