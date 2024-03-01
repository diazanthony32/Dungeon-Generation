using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool allowRotation = true;

    public enum RepeatMode{
        allow,
        disallowImmediate,
        disallow
    }
    public RepeatMode repeatMode;

    public AnimationCurve chanceCurve;

    public List<Socket> socketList { get; private set; } = new List<Socket>();

    [HideInInspector] public List<Socket> doorways = new List<Socket>();

    [HideInInspector] public int floodValue;

    private void Awake()
    {
        socketList.AddRange(this.GetComponentsInChildren<Socket>());
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        Gizmos.DrawWireCube(Vector3.zero, GetComponentInChildren<BoxCollider>().size);
    }
}
