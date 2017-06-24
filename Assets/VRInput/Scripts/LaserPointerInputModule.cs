using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace VRInput
{
    public class LaserPointerInputModule : BaseInputModule
    {
        public static LaserPointerInputModule Instance { get; private set; }

        public LayerMask layerMask;

        // storage class for controller specific data
        private class ControllerData
        {
            public LaserPointerEventData PointerEvent;
            public GameObject CurrentPoint;
            public GameObject CurrentPressed;
            public GameObject CurrentDragging;
        }

        private Camera uiCamera;
        private PhysicsRaycaster raycaster;

        private readonly Dictionary<RayPointer, ControllerData> controllerData =
            new Dictionary<RayPointer, ControllerData>();

        protected override void Awake()
        {
            base.Awake();

            //Cursor.lockState = CursorLockMode.Locked;

            if (Instance != null)
            {
                Debug.LogWarning("Trying to instantiate multiple LaserPointerInputModule.");
                DestroyImmediate(gameObject);
            }

            Instance = this;
        }

        protected override void Start()
        {
            base.Start();

            // Create a new camera that will be used for raycasts
            //var obj = GameObject.Find("UI Camera");
            //if (obj)
            //{
            //uiCamera = obj.GetComponent<Camera>();
            //raycaster = obj.GetComponent<PhysicsRaycaster>();
            //}
            //else
            //{
            uiCamera = new GameObject("UI Camera").AddComponent<Camera>();
            // Added PhysicsRaycaster so that pointer events are sent to 3d objects
            raycaster = uiCamera.gameObject.AddComponent<PhysicsRaycaster>();
            //}
            uiCamera.clearFlags = CameraClearFlags.Nothing;
            uiCamera.enabled = false;
            uiCamera.fieldOfView = 5;
            uiCamera.nearClipPlane = 0.01f;

            // Find canvases in the scene and assign our custom
            // UICamera to them
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.worldCamera = uiCamera;
            }
        }

        public void AddController(RayPointer controller)
        {
            controllerData.Add(controller, new ControllerData());
        }

        public void RemoveController(RayPointer controller)
        {
            controllerData.Remove(controller);
        }

        protected bool UpdateCameraPosition(RayPointer controller, PointerEventData pointerEventData)
        {
            Ray ray;

            if (controller.GetRay(out ray,
                (fromPoint, direction) => RaycastAll(pointerEventData, fromPoint, direction)))
            {

                uiCamera.transform.position = ray.origin;
                uiCamera.transform.rotation = Quaternion.LookRotation(ray.direction);
                return true;
            }
            return false;
        }

        public void ClearSelection()
        {
            if (eventSystem.currentSelectedGameObject)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        // select a game object
        private void Select(GameObject go)
        {
            ClearSelection();

            if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
            {
                eventSystem.SetSelectedGameObject(go);
            }
        }

        public override void Process()
        {
            raycaster.eventMask = layerMask;

            foreach (var pair in controllerData)
            {
                var controller = pair.Key;
                var data = pair.Value;

                if (!UpdateCameraPosition(controller, data.PointerEvent)) continue;

                if (data.PointerEvent == null)
                    data.PointerEvent = new LaserPointerEventData(eventSystem);
                else
                    data.PointerEvent.Reset();

                data.PointerEvent.Controller = controller;
                data.PointerEvent.delta = Vector2.zero;
                data.PointerEvent.position = new Vector2(uiCamera.pixelWidth * 0.5f, uiCamera.pixelHeight * 0.5f);
                //data.pointerEvent.scrollDelta = Vector2.zero;

                // trigger a raycast
                eventSystem.RaycastAll(data.PointerEvent, m_RaycastResultCache);
                data.PointerEvent.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
                m_RaycastResultCache.Clear();

                // make sure our controller knows about the raycast result
                // we add 0.01 because that is the near plane distance of our camera and we want the correct distance
                if (data.PointerEvent.pointerCurrentRaycast.distance > 0.0f)
                    controller.LimitLaserDistance(data.PointerEvent.pointerCurrentRaycast.distance + 0.01f);

                // stop if no UI element was hit
                //if(pointerEvent.pointerCurrentRaycast.gameObject == null)
                //return;

                // Send control enter and exit events to our controller
                var hitControl = data.PointerEvent.pointerCurrentRaycast.gameObject;
                if (data.CurrentPoint != hitControl)
                {
                    if (data.CurrentPoint != null)
                        controller.OnExitControl(data.CurrentPoint);

                    if (hitControl != null)
                        controller.OnEnterControl(hitControl);
                }

                data.CurrentPoint = hitControl;

                // Handle enter and exit events on the GUI controlls that are hit
                HandlePointerExitAndEnter(data.PointerEvent, data.CurrentPoint);

                if (controller.ButtonDown())
                {
                    ClearSelection();

                    data.PointerEvent.pressPosition = data.PointerEvent.position;
                    data.PointerEvent.pointerPressRaycast = data.PointerEvent.pointerCurrentRaycast;
                    data.PointerEvent.pointerPress = null;

                    // update current pressed if the cursor is over an element
                    if (data.CurrentPoint != null)
                    {
                        data.CurrentPressed = data.CurrentPoint;
                        data.PointerEvent.Current = data.CurrentPressed;
                        var newPressed = ExecuteEvents.ExecuteHierarchy(data.CurrentPressed, data.PointerEvent,
                            ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.PointerEvent, ExecuteEvents.pointerDownHandler);
                        if (newPressed == null)
                        {
                            // some UI elements might only have click handler and not pointer down handler
                            newPressed = ExecuteEvents.ExecuteHierarchy(data.CurrentPressed, data.PointerEvent,
                                ExecuteEvents.pointerClickHandler);
                            ExecuteEvents.Execute(controller.gameObject, data.PointerEvent,
                                ExecuteEvents.pointerClickHandler);
                            if (newPressed != null)
                            {
                                data.CurrentPressed = newPressed;
                            }
                        }
                        else
                        {
                            data.CurrentPressed = newPressed;
                            // we want to do click on button down at same time, unlike regular mouse processing
                            // which does click when mouse goes up over same object it went down on
                            // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                            ExecuteEvents.Execute(newPressed, data.PointerEvent, ExecuteEvents.pointerClickHandler);
                            ExecuteEvents.Execute(controller.gameObject, data.PointerEvent,
                                ExecuteEvents.pointerClickHandler);
                        }

                        if (newPressed != null)
                        {
                            data.PointerEvent.pointerPress = newPressed;
                            data.CurrentPressed = newPressed;
                            Select(data.CurrentPressed);
                        }

                        ExecuteEvents.Execute(data.CurrentPressed, data.PointerEvent, ExecuteEvents.beginDragHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.PointerEvent, ExecuteEvents.beginDragHandler);

                        data.PointerEvent.pointerDrag = data.CurrentPressed;
                        data.CurrentDragging = data.CurrentPressed;
                    }
                } // button down end

                if (controller.ButtonUp())
                {
                    if (data.CurrentDragging != null)
                    {
                        data.PointerEvent.Current = data.CurrentDragging;
                        ExecuteEvents.Execute(data.CurrentDragging, data.PointerEvent, ExecuteEvents.endDragHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.PointerEvent, ExecuteEvents.endDragHandler);
                        if (data.CurrentPoint != null)
                        {
                            ExecuteEvents.ExecuteHierarchy(data.CurrentPoint, data.PointerEvent,
                                ExecuteEvents.dropHandler);
                        }
                        data.PointerEvent.pointerDrag = null;
                        data.CurrentDragging = null;
                    }
                    if (data.CurrentPressed)
                    {
                        data.PointerEvent.Current = data.CurrentPressed;
                        ExecuteEvents.Execute(data.CurrentPressed, data.PointerEvent, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.PointerEvent, ExecuteEvents.pointerUpHandler);
                        data.PointerEvent.rawPointerPress = null;
                        data.PointerEvent.pointerPress = null;
                        data.CurrentPressed = null;
                    }
                }

                // drag handling
                if (data.CurrentDragging != null)
                {
                    data.PointerEvent.Current = data.CurrentPressed;
                    ExecuteEvents.Execute(data.CurrentDragging, data.PointerEvent, ExecuteEvents.dragHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.PointerEvent, ExecuteEvents.dragHandler);
                }

                // update selected element for keyboard focus
                if (eventSystem.currentSelectedGameObject != null)
                {
                    data.PointerEvent.Current = eventSystem.currentSelectedGameObject;
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(),
                        ExecuteEvents.updateSelectedHandler);
                    //ExecuteEvents.Execute(controller.gameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
                }
            }
        }

        public RaycastResult RaycastAll(PointerEventData eventData, Vector3 fromPoint, Vector3 direction)
        {
            uiCamera.transform.position = fromPoint;
            uiCamera.transform.rotation = Quaternion.LookRotation(direction);

            if (eventData == null)
                eventData = new PointerEventData(eventSystem);
            else
                eventData.Reset();

            eventData.delta = Vector2.zero;
            eventData.position = new Vector2(uiCamera.pixelWidth * 0.5f, uiCamera.pixelHeight * 0.5f);

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            var res = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();

            if (res.distance > 0.0f)
            {
                res.distance = res.distance + 0.01f;
            }
            return res;
        }
    }
}
