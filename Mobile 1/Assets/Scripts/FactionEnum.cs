using UnityEngine;

public class Faction
{
    public enum FactionName
    {
        Neutral,
        Player,
        Red,
        Purple
    }
    public FactionName currentFaction;

    public Color color;
    public void ChangeFaction(FactionName factionName)
    {
        currentFaction = factionName;
        //ChangeColorFaction();
        switch (currentFaction)
        {
            case FactionName.Neutral:
                color = Color.black;
                break;
            case FactionName.Player:
                color = new Color(0f, 0.7411765f, 0.8117647f, 1f);
                break;
            case FactionName.Red:
                color = Color.red;
                break;
            case FactionName.Purple:
                color = new Color(0.5802096f, 0f, 0.8301887f, 1f);
                break;
            default:
                color = Color.black;
                break;
        }
    }

    public FactionName GetFaction()
    {
        return currentFaction;
    }

    private void ChangeColorFaction()
    {
        switch (currentFaction)
        {
            case FactionName.Neutral:
                color = Color.black;
                break;
            case FactionName.Player:
                color = new Color(0f, 0.7411765f, 0.8117647f, 1f);
                break;
            case FactionName.Red:
                color = Color.red;
                break;
            case FactionName.Purple:
                color = new Color(0.5802096f, 0f, 0.8301887f, 1f);
                break;
            default:
                color = Color.black;
                break;
        }
    }
}

//public class TestCode : MonoBehaviour
//{
//    private Faction faction;

//    private Color myColor;
//    private void Start()
//    {
//        OnNewFaction();
//        Debug.Log(faction.GetFaction().ToString());
//    }

//    private void OnNewFaction()
//    {
//        faction.ChangeFaction(Faction.FactionName.Player);
//        myColor = faction.color;
//    }
//}