using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTFCounter : MonoBehaviour
{
    public ulong simulationTime;
    public ulong prevSimulationTime;

    public Movement movementScript;
    public ulong dt;
    private IEnumerator Start()
    {
        GUI.depth = 2;

        while (true)
        {
            simulationTime = movementScript.simulationTime;
            dt = (simulationTime - prevSimulationTime);//simulation time passed in microseconds
            prevSimulationTime = simulationTime;
            yield return new WaitForSeconds(1f);
        }
    }

/// <summary>
/// Unity draw call multiple times per frame
/// </summary>
    private void OnGUI()
    {
        GUI.color = Color.black;
        float rtf = (float)dt/1000000; //to  seconds
        GUI.Label(new Rect(Screen.width /2, 20, 100, 25), "RTF: " + rtf.ToString());
    }
}
