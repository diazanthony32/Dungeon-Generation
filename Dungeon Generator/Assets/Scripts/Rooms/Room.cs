using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public int floodValue;

    public List<Transform> attachmentPoints = new List<Transform>();

    [HideInInspector] public List<Transform> doorways = new List<Transform>();

}
