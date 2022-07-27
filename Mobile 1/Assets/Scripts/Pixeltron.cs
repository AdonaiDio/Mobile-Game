using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pixeltron : MonoBehaviour, IFaction
{
    public Faction faction { get; set; }

    public Faction.FactionName initialFaction;
    public float emissionIntensity = 5f;
    public Material material;

    public int _maxEnergyPoints;
    public int _currentEnergyPoints;

    public float _passiveGrowthWaitTime = 4f;
    public float _energyPulseWaitTime = 3f;
    public float _energyPulseTimeToMove = .4f;

    void Awake()
    {
        faction = new Faction();
        material = gameObject.GetComponent<MeshRenderer>().materials[0];
        material.EnableKeyword("_EMISSION");
    }
    private void Start()
    {
        UpdateToFaction(initialFaction);
    }
    public void UpdateToFaction(Faction.FactionName factionName)
    {
        faction.ChangeFaction(factionName);
        //atualizar a cor, e outros estados variaveis das facções
        ChangeColor();    
    }
    public void ChangeColor()
    {
        material.SetColor("_EmissionColor", faction.color * emissionIntensity);
    }
}