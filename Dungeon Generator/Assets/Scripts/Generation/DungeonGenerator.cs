using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int dungeonSize;

    [Header("Root/Spawn")]
    public GameObject spawnTile;

    [Header("Attachments")]
    public GameObject[] connectors;
    public GameObject[] blockers;

    [Header("Tile Sets")]
    public GameObject[] allTiles;

    [Range(0.0f, 1.0f)]
    public float pauseBeweenRooms = 0;

    public float tilePadding = 0;
    public AnimationCurve roomChanceCurve;

    private List<Room> allRooms = new List<Room>();
    private List<Room> mainPath = new List<Room>();

    LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GenerateDungeon());
    }


    IEnumerator GenerateDungeon() 
    {
        // spawn the starting room and add it to the total rooms list
        Room spawnRoom = Instantiate(spawnTile).GetComponent<Room>();
        allRooms.Add(spawnRoom);

        // assign attachment points "randomly"
        yield return StartCoroutine(AssignAttachmentPoints(spawnRoom));

        EvaluateDungeon();
    }

    // iterate through all the attachment points in a given room
    IEnumerator AssignAttachmentPoints(Room room) 
    {
        // stores the original amount of attachment points in a tile
        int totalPointCount = room.attachmentPoints.Count;

        // iterates through all the attachment points in a tile
        while (room.attachmentPoints.Count > 0) 
        {
            // choose a random remaining attactment point
            Transform targetAttachmentPoint = room.attachmentPoints[UnityEngine.Random.Range(0, room.attachmentPoints.Count)];
         
            // waiting in between generation steps
            // small native delay to prevent items from spawning on top of eachother with 0 delay
            yield return new WaitForSeconds(pauseBeweenRooms + 0.05f);

            // if more rooms can be generated, allow attempts. Otherwise place a blocker
            if (dungeonSize > 0)
            {
                // uses an animation curve to determine the likelyhood of a door to be created and spawned in
                // with every successful connection created in a tile, the less likely-hood of another being made 
                float chance = roomChanceCurve.Evaluate((float)room.doorways.Count / (float)totalPointCount);

                Debug.Log(chance);

                if (UnityEngine.Random.Range(0.00f, 1.00f) <= chance)
                {
                    Room newTile = FindValidTile(targetAttachmentPoint);

                    if (newTile != null)
                    {
                        // increase new tile distance from spawn by 1
                        // adds new tile to room list
                        newTile.floodValue = room.floodValue + 1;
                        allRooms.Add(newTile);

                        dungeonSize--;

                        // ROOM RECURSION GENERATION BEGINS HERE
                        yield return StartCoroutine(AssignAttachmentPoints(newTile));
                    }
                    else
                    {
                        // randomly select from various blockers
                        Instantiate(blockers[UnityEngine.Random.Range(0, blockers.Length)], targetAttachmentPoint);
                    }
                }
                else
                {
                    // randomly select from various blockers
                    Instantiate(blockers[UnityEngine.Random.Range(0, blockers.Length)], targetAttachmentPoint);
                }
            }
            else
            {
                // randomly select from various blocker
                Instantiate(blockers[UnityEngine.Random.Range(0, blockers.Length)], targetAttachmentPoint);
            }

            // removes this attachment point from being selected
            room.attachmentPoints.Remove(targetAttachmentPoint);
        }

    }

    // 
    Room FindValidTile(Transform targetAttachmentPoint)
    {
        // choose a random tile
        Room newTile = Instantiate(allTiles[UnityEngine.Random.Range(0, allTiles.Length)]).GetComponent<Room>();

        // TO DO:
        // go through tile list to find a tile that might work

        // get an attachment point in in the new tile that has a valid room placement
        Transform validAttachmentPoint = FindValidAttachmentPoint(newTile, targetAttachmentPoint);

        // if a valid room position has been created successfully
        if (validAttachmentPoint != null)
        {
            // attach doorways to the two points between the doors
            AttachDoorways(targetAttachmentPoint, validAttachmentPoint);

            return newTile;
        }

        // destroys unusable tile
        GameObject.Destroy(newTile.gameObject);

        return null;

    }

    // Iterates through attachment points in room to connect to previous room's attachment point
    Transform FindValidAttachmentPoint(Room tile, Transform targetAttachmentPoint)
    {
        // list to remember which points have been attempted as not to try them again
        List<Transform> attemptedPoints = new List<Transform>();

        while (tile.attachmentPoints.Count > 0)
        {
            Transform attachmentPoint = tile.attachmentPoints[UnityEngine.Random.Range(0, tile.attachmentPoints.Count)];

            // found a valid room to place
            if (CheckTilePlacement(tile.transform, attachmentPoint, targetAttachmentPoint))
            {
                //Debug.LogWarning("Attached Connecting Room @ " + newAttachmentPoint.parent.parent.name + ":" + newAttachmentPoint.name);

                // remove point from attempted positions in the future
                tile.attachmentPoints.Remove(attachmentPoint);

                // adds attempted points back to original list to be sorted through afterwards
                tile.attachmentPoints.AddRange(attemptedPoints);

                return attachmentPoint;
            }
            else 
            {
                //Debug.Log("Connection #" + attemptedPoints.Count + " to" + newAttachmentPoint.parent.parent.name + " : " + newAttachmentPoint + " unsuccessful trying new point");

                // add point to the new attempted list and remove point from existing list and try again
                attemptedPoints.Add(attachmentPoint);
                tile.attachmentPoints.Remove(attachmentPoint);
            }
        }

        return null;
    }

    // verifies the placement of a room by checking if a room is NOT overlapping another placed room
    bool CheckTilePlacement(Transform tile, Transform attachmentPoint, Transform pointToConnectTo)
    {
        //Rotates room to align selected attachment points next to eachother
        AdjustParentToChildTargetTransform(tile, attachmentPoint, pointToConnectTo);

        Collider[] hitColliders = Physics.OverlapBox(tile.position, tile.GetComponent<BoxCollider>().size / 2, tile.rotation);

        //Check when there is a new collider coming into contact with the tile
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].gameObject != tile.gameObject)
            {
                //Debug.Log("Hit : " + hitColliders[i].gameObject.name);
                return false;
            }
        }

        return true;
    }

    void AdjustParentToChildTargetTransform(Transform parent, Transform child, Transform target) 
    {
        //// Store initial local position and rotation of the child
        //Vector3 attachmentLocalPosition = child.localPosition;
        //Quaternion attachmentLocalRotation = child.localRotation;

        //// Set child to desired position and rotation
        //child.position = target.position + (-target.forward * 2);
        //child.rotation = Quaternion.Euler(target.rotation.eulerAngles.x, target.rotation.eulerAngles.y + 180, target.rotation.eulerAngles.z);

        //// Move the parent to child's position
        //parent.position = child.position;

        //// Calculate the reverse rotation
        //Quaternion reverseRotation = Quaternion.Inverse(attachmentLocalRotation);

        //// Rotate the child and parent
        //child.RotateAround(child.position, child.forward, reverseRotation.eulerAngles.z);
        //child.RotateAround(child.position, child.right, reverseRotation.eulerAngles.x);
        //child.RotateAround(child.position, child.up, reverseRotation.eulerAngles.y);

        //parent.rotation = child.rotation;

        //// Adjust parent position
        //parent.position -= parent.right * attachmentLocalPosition.x;
        //parent.position -= parent.up * attachmentLocalPosition.y;
        //parent.position -= parent.forward * attachmentLocalPosition.z;

        //// Reset local position and rotation of the child
        //child.SetLocalPositionAndRotation(attachmentLocalPosition, attachmentLocalRotation);

        //------------------------------------------------------------------------------------------------------------------------------------------

        // IDK WHATS MORE PERFORMANT YET BUT THEY BOTH WORK
        child.SetParent(null);
        parent.SetParent(child);

        // Set child to desired position and rotation
        child.position = target.position + (-target.forward * tilePadding);

        if (parent.GetComponent<Room>().isRotatable)
        {
            child.rotation = Quaternion.Euler(target.rotation.eulerAngles.x, target.rotation.eulerAngles.y + 180, target.rotation.eulerAngles.z);
        }

        parent.SetParent(null);
        child.SetParent(parent);
    }

    // adds a doorway to the room and adds it to the room's door list
    void AttachDoorways(Transform AttachmentPoint1, Transform AttachmentPoint2) 
    {
        // randomly generate door from various doorways
        GameObject doorway = connectors[UnityEngine.Random.Range(0, connectors.Length)];

        // parents door prefab to target attachment points
        Transform door1 = Instantiate(doorway, AttachmentPoint1).transform;
        Transform door2 = Instantiate(doorway, AttachmentPoint2).transform;

        // adds attachment points to doorway list
        AttachmentPoint1.GetComponentInParent<Room>().doorways.Add(AttachmentPoint1);
        AttachmentPoint2.GetComponentInParent<Room>().doorways.Add(AttachmentPoint2);
    }

    void EvaluateDungeon()
    {
        Debug.LogWarning("Beginning Dungeon Evalation...");

        // Once generation is done, check for a successfully generated dungeon
        if (dungeonSize == 0)
        {
            // sorts all the rooms in the dungeon by their floodValue from Highest to lowest
            Room furthestRoom = allRooms.OrderByDescending(room => room.floodValue).ToList()[0];

            //---------------------------------------------------------------------------------------------------------------------------- 1
            // Spawns a Marker to visually indicate the final/furthest room and logs it to console
            // credit to : https://manuel-rauber.com/2021/12/29/set-inspector/
            var iconContent = EditorGUIUtility.IconContent("sv_label_6");
            EditorGUIUtility.SetIconForObject(furthestRoom.gameObject, (Texture2D)iconContent.image);

            furthestRoom.gameObject.name = "Boss Room";

            // --------------------------------------------------------------------------------------------------------------------------- 1

            Debug.Log("Furthest Room is " + furthestRoom.floodValue + " rooms away from the spawn room...");

            // finding the main path
            for (int i = allRooms.IndexOf(furthestRoom); i >= 0; i--)
            {
                bool shouldAdd = true;

                foreach (Room room in mainPath)
                {
                    if (allRooms[i].floodValue == room.floodValue)
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    mainPath.Add(allRooms[i]);
                }

            }

            // re-organizes list by flood value acending
            mainPath = mainPath.OrderBy(room => room.floodValue).ToList();

            // --------------------------------------------------------------------------------------------------------------------------- 2

            // line renderer shenanagians
            lineRenderer.positionCount = mainPath.Count;
            for (int x = 0; x < mainPath.Count; x++)
            {
                lineRenderer.SetPosition(x, mainPath[x].transform.position + (Vector3.up * 1));
            }

            // ---------------------------------------------------------------------------------------------------------------------------- 2 
        }
        else
        {
            Debug.LogError("Dungeon Evaluation Failed. \n Failed to reach the set amount of rooms in the Dungeon.");
        }
    }
}
