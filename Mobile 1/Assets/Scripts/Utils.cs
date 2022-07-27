using UnityEngine;

public class Utils : MonoBehaviour
{
    public static Vector3 ScreenToWorld(Camera camera, Vector3 position)
    {
        float offsetPositonX = 7f;
        float offsetPositonY = 12.5f;
        
        position.z = camera.nearClipPlane;

        //return camera.ScreenToWorldPoint(position);

        position.x = ((position.x - Screen.width/2)/(Screen.width/2))*offsetPositonX;
        position.y = ((position.y - Screen.height / 2) / (Screen.height / 2))*offsetPositonY;
        
        return position;
    }
}
