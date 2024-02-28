using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public bool isRotatable = true;

    public enum RepeatMode{
        allow,
        disallowImmediate,
        disallow
    }
    public RepeatMode repeatMode;

    public List<Transform> attachmentPoints = new List<Transform>();

    [HideInInspector] public List<Transform> doorways = new List<Transform>();

    [HideInInspector] public int floodValue;

    private int frameTime;

    private void Awake()
    {
        frameTime = Time.frameCount;
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        Gizmos.DrawWireCube(Vector3.zero, GetComponentInChildren<BoxCollider>().size);
    }
}
