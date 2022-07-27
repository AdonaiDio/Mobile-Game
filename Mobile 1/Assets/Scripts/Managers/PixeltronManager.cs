using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixeltronManager : MonoBehaviour
{
    public static PixeltronManager current;

    private int selectionLimit = 2;
    private int currentSelection;

    private int actingId;
    private int targetId;

    private void Awake()
    {
        current = this;
        currentSelection = 0;
        actingId = 0;
        targetId = 0;

        FrameIdSetter();
        TrailIdSetter();
    }


    private void OnEnable()
    {
        //InputManager.current.OnStartTouchEvent += OnDebugSelection;
        Events.onStartTouchEvent.AddListener(OnDebugSelection);

    }
    private void OnDisable()
    {
        //InputManager.current.OnStartTouchEvent -= OnDebugSelection;
        Events.onStartTouchEvent.RemoveListener(OnDebugSelection);

    }
    private void TrailIdSetter()
    {
        int idCounter = 0;
        TrailScript[] allTrails = FindObjectsOfType<TrailScript>();
        foreach (TrailScript t in allTrails)
        {
            idCounter += 1;
            t.trailId = idCounter;
        }
    }

    private void FrameIdSetter()
    {
        int idCounter = 0;
        FrameScript[] allFrames = FindObjectsOfType<FrameScript>();
        foreach (FrameScript f in allFrames)
        {
            idCounter += 1;
            f.id = idCounter;
        }
    }

    //Metodo para identificar em quem foi clicado
    private void OnDebugSelection(Vector3 touchPosition, float touchTime)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //Pixeltron pixeltron = hit.collider.gameObject.GetComponent<Pixeltron>();
            FrameScript frame = hit.collider.gameObject.GetComponentInParent<FrameScript>();
            if (frame != null && currentSelection < selectionLimit && actingId != frame.id)
            {
                currentSelection += 1;
                //Debug.Log("Pixeltron Na Mira!");
                frame.selection.SetActive(true);
                //Debug.Log(frame.id);
                if (actingId == 0)
                {
                    actingId = frame.id;
                }else if (targetId == 0)
                {
                    targetId = frame.id;
                    //EVENTO DE AÇÃO CONEXÃO ou seilá passando os 2 IDs!
                    Events.onConnect.Invoke(actingId ,targetId);
                }
            }
            else if (frame == null || currentSelection >= selectionLimit || actingId == frame.id)
            {
                foreach (FrameScript f in FindObjectsOfType<FrameScript>())
                {
                    f.selection.SetActive(false);
                }
                currentSelection = 0;
                actingId = 0;
                targetId = 0;
            }
        }
    }
}