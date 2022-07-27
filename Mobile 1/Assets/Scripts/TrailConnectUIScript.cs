using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrailConnectUIScript : MonoBehaviour
{
    public bool connected;

    private void Awake()
    {
        connected = false;
    }
    public void EnableConnectionUI()
    {
        transform.GetChild(0).GetComponentInChildren<Image>().color = new Color32(255,255,255,255);
        connected = true;
    }
    public void DisableConnectionUI()
    {
        transform.GetChild(0).GetComponentInChildren<Image>().color = new Color32(101, 101, 101, 255);
        connected = false;
    }

}
