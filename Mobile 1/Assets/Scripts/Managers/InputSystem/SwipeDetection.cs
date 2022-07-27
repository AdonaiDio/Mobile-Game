using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeDetection : MonoBehaviour
{
    [SerializeField]
    private float minimumDistance = .2f;
    [SerializeField]
    private float maximumTime = 1f;
    [SerializeField, Range(0, 1f)]
    private float directionThreshold = .9f;
    [SerializeField]
    private GameObject swipeTrail;

    private Vector3 startPosition;
    private float startTime;
    private Vector3 endPosition;
    private float endTime;

    private Coroutine coroutine;

    private InputManager inputManager;

    private Vector3 gizmoPoint;
    private Ray ray;

    private void Awake()
    {
        inputManager = InputManager.current;
    }
    private void OnEnable()
    {
        Events.onStartTouchEvent.AddListener(SwipeStart);
        Events.onEndTouchEvent.AddListener(SwipeEnd);
    }
    private void OnDisable()
    {
        Events.onStartTouchEvent.RemoveListener(SwipeStart);
        Events.onEndTouchEvent.RemoveListener(SwipeEnd);
    }
    private void SwipeStart(Vector3 position, float time)
    {
        
        startPosition = Utils.ScreenToWorld(Camera.main, position);
        //Debug.Log("Swipe start: "+startPosition);
        startTime = time;
        swipeTrail.SetActive(true);
        swipeTrail.transform.position = Utils.ScreenToWorld(Camera.main, position);
        coroutine = StartCoroutine(SwipeTrail());
    }

    private IEnumerator SwipeTrail()
    {
        while (true)
        {
            swipeTrail.transform.position = inputManager.PrimaryPosition();
            yield return null;
        }
    }

    private void SwipeEnd(Vector3 position, float time)
    {
        swipeTrail.SetActive(false);
        StopCoroutine(coroutine);
        endPosition = Utils.ScreenToWorld(Camera.main, position);
        //Debug.Log("Swipe end: " + endPosition);
        endTime = time;
        DetectSwipe();
    }
    private void DetectSwipe()
    {
        if (Vector3.Distance(startPosition, endPosition) >=minimumDistance && (endTime - startTime) <= maximumTime) {
            //Debug.Log("Swipe Detected");
            Debug.DrawLine(startPosition, endPosition, Color.red, 5f);
            Vector3 direction = endPosition - startPosition;
            Vector2 direction2D = new Vector2(direction.x, direction.y).normalized;
            SwipeDirection(direction2D);
            DetectSwipeCollision();
        }
    }

    private void SwipeDirection(Vector2 direction)
    {
        if (Vector2.Dot(Vector2.up, direction) > directionThreshold)
        {
            //Debug.Log("Swipe Up");
            //precisa ser y >= start.y e y <= end.y

        }
        else if (Vector2.Dot(Vector2.down, direction) > directionThreshold)
        {
            //Debug.Log("Swipe Down");
            //precisa ser y <= start.y e y >= end.y
        }
        else if (Vector2.Dot(Vector2.left, direction) > directionThreshold)
        {
            //Debug.Log("Swipe Left");
            //precisa ser x <= start.x e x >= end.x
        }
        else if (Vector2.Dot(Vector2.right, direction) > directionThreshold)
        {
            //Debug.Log("Swipe Right");
            //precisa ser x >= start.x e x <= end.x
        }
    }

    private void DetectSwipeCollision()
    {
        bool xDirPositive;
        bool yDirPositive;
        ray = new Ray(startPosition,(endPosition - startPosition));
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.GetComponent<TrailUnit>() != null)
            {
                if (startPosition.x < endPosition.x) { xDirPositive = true; }
                else { xDirPositive = false; }
                if (startPosition.y < endPosition.y) { yDirPositive = true; }
                else { yDirPositive = false; }
                if (xDirPositive && yDirPositive)
                {
                    if (hit.transform.position.x >= startPosition.x && hit.transform.position.x <= endPosition.x
                        && hit.transform.position.y >= startPosition.y && hit.transform.position.y <= endPosition.y)
                    {
                        //hit.transform.gameObject.SetActive(false);
                        Events.onCancelConnection.Invoke(hit.transform.GetComponent<TrailUnit>().unitId);
                        gizmoPoint = hit.point;
                    }
                }
                if (!xDirPositive && yDirPositive)
                {
                    if (hit.transform.position.x <= startPosition.x && hit.transform.position.x >= endPosition.x
                        && hit.transform.position.y >= startPosition.y && hit.transform.position.y <= endPosition.y)
                    {
                        //hit.transform.gameObject.SetActive(false);
                        Events.onCancelConnection.Invoke(hit.transform.GetComponent<TrailUnit>().unitId);
                        gizmoPoint = hit.point;
                    }
                }
                if (xDirPositive && !yDirPositive)
                {
                    if (hit.transform.position.x >= startPosition.x && hit.transform.position.x <= endPosition.x
                        && hit.transform.position.y <= startPosition.y && hit.transform.position.y >= endPosition.y)
                    {
                        //hit.transform.gameObject.SetActive(false);
                        Events.onCancelConnection.Invoke(hit.transform.GetComponent<TrailUnit>().unitId);
                        gizmoPoint = hit.point;
                    }
                }
                if (!xDirPositive && !yDirPositive)
                {
                    if (hit.transform.position.x <= startPosition.x && hit.transform.position.x >= endPosition.x
                        && hit.transform.position.y <= startPosition.y && hit.transform.position.y >= endPosition.y)
                    {
                        //hit.transform.gameObject.SetActive(false);
                        Events.onCancelConnection.Invoke(hit.transform.GetComponent<TrailUnit>().unitId);
                        gizmoPoint = hit.point;
                    }
                }

            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoPoint, 0.2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(ray);
    }

}
