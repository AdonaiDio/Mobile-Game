using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameScript : MonoBehaviour
{
    //[HideInInspector]
    public int id; //Lembrar de começar a partir do id 1 !!!!
    //[HideInInspector]
    public List<TrailScript> trailsList;

    public int _maxConnections;
    public List<TrailScript> _activeConnections;
    public TextMeshProUGUI _energyPointsUI;

    //lista de icones que mostram as conexões
    public List<TrailConnectUIScript> _allConnectionsUI;
    public List<TrailConnectUIScript> _currentConnectionsUI;

    public Transform thisPixeltron;

    public GameObject selection;

    private float passiveGrouthCountdown;

    private void Awake()
    {
        
    }

    void Start()
    {
        selection = transform.Find("SelectionGizmo").gameObject;
        thisPixeltron = transform.GetChild(0);
        _activeConnections.Clear();
        foreach(TrailScript t in FindObjectsOfType<TrailScript>())
        {
            if (t.frame_A.id == this.id || t.frame_B.id == this.id)
            {
                trailsList.Add(t);
            }
        }
        _energyPointsUI = GetComponentInChildren<TextMeshProUGUI>();

        _energyPointsUI.text = thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();

        //adicionar todas os iconesUI de conexão na lista
        for (int i=0; i<=3 ; i++)
        {
            _allConnectionsUI.Add(transform.GetChild(1).GetChild(0).GetChild(i).GetComponent<TrailConnectUIScript>());

        }

        //mostrar só o limite de conexões possiveis
        int uiCount = 0;
        foreach (TrailConnectUIScript iconUI in _allConnectionsUI) {
            if (uiCount < _maxConnections)
            {
                _currentConnectionsUI.Add(iconUI);
            }
            else
            {
                iconUI.gameObject.SetActive(false);
            }
                uiCount += 1;
        }

        passiveGrouthCountdown = thisPixeltron.GetComponent<Pixeltron>()._passiveGrowthWaitTime;

    }
    private void Update()
    {
        PassiveGrowth();
    }

    public void AddTrailToActiveConnections(TrailScript trail)
    {
        //add a lista
        _activeConnections.Add(trail);
        //ativar uma UI no inicio da lista dos que não estão connected
        
        foreach (TrailConnectUIScript iconUI in _currentConnectionsUI)
        {
            if (iconUI.connected == false)
            {
                iconUI.EnableConnectionUI();
                break;
            }
        }
    }
    public void RemoveTrailFromActiveConnections(TrailScript trail)
    {
        //remove da lista
        _activeConnections.Remove(trail);
        //desativa a ultima UI da lista que estão connected

        for (int i = _currentConnectionsUI.Count - 1; i >= 0; i--)
        {
            if (_currentConnectionsUI[i].connected == true)
            {
                _currentConnectionsUI[i].DisableConnectionUI();
                break;
            }
        }
    }
    private void PassiveGrowth() {
        if (thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction != Faction.FactionName.Neutral)
        {
            passiveGrouthCountdown -= Time.deltaTime;
            if(passiveGrouthCountdown <= 0f)
            {
                ChangeEnergyValue(1, thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction);
                passiveGrouthCountdown = thisPixeltron.GetComponent<Pixeltron>()._passiveGrowthWaitTime;
            }
        }
    }
    public void ChangeEnergyValue(int value, Faction.FactionName energyFactionName)
    {
        if (energyFactionName == thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction)
        {
            thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += value;
            if (thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints > thisPixeltron.GetComponent<Pixeltron>()._maxEnergyPoints)
            {
                thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints = thisPixeltron.GetComponent<Pixeltron>()._maxEnergyPoints;
            }
        }
        else 
        {
            thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints -= value;
            
            if (thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints == 0) {
                thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints += 1;
                //evento de onChangeFaction
                Events.onChangeFaction.Invoke(thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction, energyFactionName);
                //change faction
                thisPixeltron.GetComponent<Pixeltron>().UpdateToFaction(energyFactionName);
            }
            else if (thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints < 0) {
                thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints *= (-1);
                //evento de onChangeFaction
                Events.onChangeFaction.Invoke(thisPixeltron.GetComponent<Pixeltron>().faction.currentFaction, energyFactionName);
                //change faction
                thisPixeltron.GetComponent<Pixeltron>().UpdateToFaction(energyFactionName);
            }
        }
        _energyPointsUI.text = thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints.ToString();
        UpdatePixeltronScale();
    }
    private void UpdatePixeltronScale()
    {
        float energyProportion = ((float)thisPixeltron.GetComponent<Pixeltron>()._currentEnergyPoints / (float)thisPixeltron.GetComponent<Pixeltron>()._maxEnergyPoints) * 2f;

        float tweenTime = .5f;
        
        LeanTween.scale(thisPixeltron.gameObject, new Vector3(energyProportion, energyProportion, energyProportion), tweenTime);
    }
}
