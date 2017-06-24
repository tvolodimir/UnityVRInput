using UnityEngine;
using UnityEngine.EventSystems;

namespace VRInput
{
    public class GravityGun : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private float attraction;
        [SerializeField] private float dampening;

        private Rigidbody current;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData is LaserPointerEventData)
            {
                var e = eventData as LaserPointerEventData;
                current = e.Current.GetComponent<Rigidbody>();
                if (current)
                {
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            current = null;
        }

        private void FixedUpdate()
        {
            if (current)
            {
                var dest = transform.TransformPoint(offset);
                var force = attraction * (dest - current.transform.position);
                current.AddForce(-current.velocity * dampening);
                current.AddForce(force, ForceMode.Acceleration);
            }
        }
    }
}
