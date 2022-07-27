using System.Collections.Generic;
using System;
public class FsmState
{
    public List<FsmTransition> transitions = new List<FsmTransition>();
    public Action _entryAction;
    public Action _exitAction;
    public Action _updateAction;

    public FsmState(Action entryAction, Action updateAction, Action exitAction)
    {
        _entryAction = entryAction;
        _updateAction = updateAction;
        _exitAction = exitAction;
    }

    public void When(Func<bool> cond, FsmState tState, Action ac)
    {
        FsmTransition t = new FsmTransition(cond, tState, ac);
        transitions.Add(t);
    }
}