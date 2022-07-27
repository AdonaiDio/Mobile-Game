using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailUnit : MonoBehaviour, IFaction
{
    public Faction faction { get; set; }
    //[HideInInspector]
    public string unitId;
    //[HideInInspector]
    public float emissionIntensity = 5f;

    public Faction.FactionName initialFaction = Faction.FactionName.Neutral;
    public Material material;

    void Awake()
    {
        faction = new Faction();
        material = gameObject.GetComponent<MeshRenderer>().materials[1];
        material.EnableKeyword("_EMISSION");
    }
    private void Start()
    {
        UpdateToFaction(initialFaction);
    }
    private void OnEnable()
    {
    }
    public void UpdateToFaction(Faction.FactionName factionName)
    {
        faction.ChangeFaction(factionName);
        //atualizar a cor, e outros estados variaveis das facções
        ChangeColor();
        Debug.Log(faction.currentFaction);
    }
    public void ChangeColor()
    {
        material.SetColor("_EmissionColor", faction.color * emissionIntensity);
        Debug.Log("a cor é " + faction.color);
    }

}
