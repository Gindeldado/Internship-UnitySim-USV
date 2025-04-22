using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float count;

    private IEnumerator Start()
    {
        GUI.depth = 2;   
        while (true)
        {
            count = 1f / Time.unscaledDeltaTime;
            yield return new WaitForSeconds(1f);
        }
    }
    
    private void OnGUI()
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width /2, 5, 100, 25), "FPS: " + Mathf.Round(count));
    }
}