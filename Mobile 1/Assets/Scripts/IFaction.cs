using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFaction
{
    public Faction faction { get; set; }

    public void UpdateToFaction(Faction.FactionName factionName);//Mudar cor, canControl, canGrow, AI
}
