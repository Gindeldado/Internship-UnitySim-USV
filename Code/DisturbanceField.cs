using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Holds UI information about disturbance field  
/// </summary>
[Serializable]
public struct DisturbanceField
{
    public TMP_InputField input;
    public int id;
    public string defaultValue;
}
