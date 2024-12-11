using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using Fusion;

public class ARHostButtonController : NetworkBehaviour
{
    private ARRaycastManager arRaycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private float smoothingSpeed = 10f;

    void Start()
    {
        if (arRaycastManager == null)
            arRaycastManager = FindObjectOfType<ARRaycastManager>();

        //gameObject.SetActive(true);
    }

    void Update()
    {
        // Reinstate the authority check:
        // Only the host moves this object
        if (!Object.HasStateAuthority)
        {
            //Debug.Log("No State Authority");
            return;
        }

        bool inputDetected = false;
        

        // Check for touch input (like single player)
        if (Touchscreen.current != null)
        {
            //Debug.Log("Has State Authorithy");
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed)
            {
                Vector2 touchPosition = touch.position.ReadValue();
                if (!IsPointerOverUIObject(touchPosition))
                {
                    // We have valid input
                    UpdatePlacementPose(touchPosition);
                    inputDetected = true;
                }
            }
        }

        // Check for mouse input (Editor)
        if (!inputDetected && Mouse.current != null)
        {
            var mouse = Mouse.current;
            if (mouse.leftButton.isPressed)
            {
                Vector2 mousePosition = mouse.position.ReadValue();
                Debug.Log("Has State Authorithy");
                if (!IsPointerOverUIObject(mousePosition))
                {
                    // We have valid input
                    Debug.Log("leftButton is pressed. HIT!!");
                    UpdatePlacementPose(mousePosition);
                    inputDetected = true;
                }
            }
        }

        // If inputDetected remains false, no call to UpdatePlacementPose is made,
        // so no "hit" logs will appear if you're not actually pressing or touching.
    }

    private void UpdatePlacementPose(Vector2 screenPosition)
    {
        if (arRaycastManager != null && arRaycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes))
        {
            // We got a hit on a plane
            Pose hitPose = hits[0].pose;
            //Debug.Log("PLANES!!");

            // Smooth movement just like single player
            transform.position = Vector3.Lerp(transform.position, hitPose.position, Time.deltaTime * smoothingSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, hitPose.rotation, Time.deltaTime * smoothingSpeed);
            Debug.Log($"Position: {transform.position}, Rotation:{transform.rotation}");
        }
        else
        {
            //
        }
    }

    private bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        Debug.Log($"Event data: {results.Count}");
        return results.Count > 0;
    }


    /*
    public Vector3 GetConfirmedPosition()
    {
        return transform.position;
    }

    public Quaternion GetConfirmedRotation()
    {
        return transform.rotation;
    }
    */
}
