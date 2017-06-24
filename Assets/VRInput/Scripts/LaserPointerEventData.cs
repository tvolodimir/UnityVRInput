using UnityEngine;
using UnityEngine.EventSystems;

namespace VRInput
{
    public class LaserPointerEventData : PointerEventData
    {
        public GameObject Current;
        public RayPointer Controller;

        public LaserPointerEventData(EventSystem e) : base(e)
        {
        }

        public override void Reset()
        {
            Current = null;
            Controller = null;
            base.Reset();
        }
    }
}
