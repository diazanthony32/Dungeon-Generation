using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public int floodValue;

    public List<Transform> attachmentPoints = new List<Transform>();

    private int spawnTime;

    void Awake()
    {
        spawnTime = Time.frameCount;
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Gizmos.DrawWireCube uses full box size, not half size like Phycis.OverlapBox, so we x2
        Gizmos.DrawWireCube(Vector3.zero, GetComponentInChildren<Collider>().bounds.size);
    }
}
