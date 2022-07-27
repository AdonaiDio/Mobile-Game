using System.Collections.Generic;
using System;
using UnityEngine;
public class FiniteStateMachine
{
    protected List<FsmState> states = new List<FsmState>();
    protected FsmState initialState;
    protected FsmState currentState;

    protected FsmTransition triggeredTrans;
    protected Queue<Action> actions = new Queue<Action>();

    public FiniteStateMachine(FsmState initial)
    {
        initialState = initial;
        currentState = initialState;

        // Entry action of initial state
        AddAction(initial._entryAction);
    }

    public void AddState(FsmState s)
    {
        states.Add(s);
    }

    public void AddStates(params FsmState[] stateArray)
    {
        for (int i = 0; i < stateArray.Length; i++) AddState(stateArray[i]);
    }


    public Queue<Action> Tick()
    {
        triggeredTrans = null;

        // Find first triggered transition
        foreach (FsmTransition transition in currentState.transitions)
        {
            if (transition.IsTriggered())
            {
                triggeredTrans = transition;
                break;
            }
        }


        if (triggeredTrans != null)
        {
            FsmState targetState = triggeredTrans.targetState;

            AddAction(currentState._exitAction);
            AddAction(triggeredTrans.action);
            AddAction(targetState._entryAction);

            currentState = targetState;
        }
        else
        {
            AddAction(currentState._updateAction);
        }

        return actions;

    }

    private void AddAction(Action a)
    {
        if (a != null)
        {
            actions.Enqueue(a);
        }
    }

    public void ExecuteActions(Queue<Action> actions)
    {
        if (actions == null) return;

        Action a;
        while (actions.Count > 0)
        {
            a = actions.Dequeue();
            a();
        }
    }
}