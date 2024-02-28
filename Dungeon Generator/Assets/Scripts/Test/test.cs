using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.GetComponent<BoxCollider>().size / 2, transform.rotation);
        
        //Check when there is a new collider coming into contact with the box
        for (int i = 0; i < hitColliders.Length; i++)
        {

            if (hitColliders[i].gameObject != this.gameObject)
            {
                //Output all of the collider names
                Debug.Log("Hit : " + hitColliders[i].name + i);
            }

        }
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
