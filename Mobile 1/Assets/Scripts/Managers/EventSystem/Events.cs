using System;
using UnityEngine;
using UnityEngine.Events;

public static class Events {
    //public static readonly Evt onGameOver = new Evt();

    //public static readonly Evt onLevelLoaded = new Evt();

    //Events with parameters, using Evt<T>.
    //public static readonly Evt<GameObject> onGameObjectSelection = new Evt<GameObject>();

    public static readonly TouchEvt<Vector3, float> onStartTouchEvent = new TouchEvt<Vector3, float>();
    public static readonly TouchEvt<Vector3, float> onEndTouchEvent = new TouchEvt<Vector3, float>();

    public static readonly Evt<int, int> onConnect = new Evt<int, int>();
    public static readonly Evt<string> onCancelConnection = new Evt<string>();
    public static readonly Evt<Faction.FactionName, Faction.FactionName> onChangeFaction = new Evt<Faction.FactionName, Faction.FactionName>();
    public static readonly Evt<FrameScript, TrailScript> onShootEnergy = new Evt<FrameScript, TrailScript>();
}
public class Evt
{
    private event Action _action = delegate { };

    public void Invoke() => _action.Invoke();
    public void AddListener(Action listener) => _action += listener;
    public void RemoveListener(Action listener) => _action -= listener;
}
public class Evt<T>
{
    private event Action<T> _action = delegate { };

    public void Invoke(T param) => _action.Invoke(param);
    public void AddListener(Action<T> listener) => _action += listener;
    public void RemoveListener(Action<T> listener) => _action -= listener;
}

public class Evt<T0, T1>
{
    private event Action<T0, T1> _action = delegate { };

    public void Invoke(T0 param1, T1 param2) => _action.Invoke(param1, param2);
    public void AddListener(Action<T0, T1> listener) => _action += listener;
    public void RemoveListener(Action<T0, T1> listener) => _action -= listener;
}

public class TouchEvt<T0, T1>
{
    private event Action<T0, T1> _action = delegate { };

    public void Invoke(T0 position, T1 time) => _action.Invoke(position, time);
    public void AddListener(Action<T0, T1> listener) => _action += listener;
    public void RemoveListener(Action<T0, T1> listener) => _action -= listener;
}
