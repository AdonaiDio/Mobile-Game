using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[DefaultExecutionOrder(-1)]
public class InputManager : MonoBehaviour
{
    public static InputManager current;

    private TouchControls touchControls;
    private Camera mainCamera;

    private void Awake()
    {
        current = this;
        touchControls = new TouchControls();
        mainCamera = Camera.main;
    }
    private void OnEnable()
    {
        touchControls.Enable();
    }
    private void OnDisable()
    {
        touchControls.Disable();
    }

    private void Start()
    {
        touchControls.Touch.TouchPress.started += ctx => StartTouch(ctx);
        touchControls.Touch.TouchPress.canceled += ctx => EndTouch(ctx);
    }

    private void StartTouch(InputAction.CallbackContext ctx)
    {
        //Debug.Log("Touch started " + touchControls.Touch.TouchPosition.ReadValue<Vector2>());
        Events.onStartTouchEvent.Invoke(touchControls.Touch.TouchPosition.ReadValue<Vector2>(), (float)ctx.startTime);
    }

    private void EndTouch(InputAction.CallbackContext ctx)
    {
        //Debug.Log("touch ended" + touchControls.Touch.TouchPosition.ReadValue<Vector2>());
        Events.onEndTouchEvent.Invoke(touchControls.Touch.TouchPosition.ReadValue<Vector2>(), (float)ctx.startTime);
    }
    public Vector2 PrimaryPosition()
    {
        return Utils.ScreenToWorld(mainCamera, touchControls.Touch.TouchPosition.ReadValue<Vector2>());
    }
}