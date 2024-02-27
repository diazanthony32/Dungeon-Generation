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
    public GameObject[] spawnTiles;

    [Header("Attachments")]
    public GameObject[] connectors;
    public GameObject[] blockers;

    [Header("Tile Sets")]
    public GameObject[] allTiles;

    [Range(0.0f, 5.0f)]
    public float pauseBeweenRooms = 0;

    private List<Room> allRooms = new List<Room>();
    private List<Room> mainPath = new List<Room>();

    LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        GenerateDungeon();

        //AssignDungeon();
    }


    void GenerateDungeon() 
    {
        // choose a random starting room
        Room spawnRoom = Instantiate(spawnTiles[UnityEngine.Random.Range(0, spawnTiles.Length)]).GetComponent<Room>();

        // assign attachment points "randomly"
        StartCoroutine(AssignAttachmentPoints(spawnRoom));
        //AssignAttachmentPoints(spawnRoom);
    }

    // iterate through all the attachment points in a given room
    IEnumerator AssignAttachmentPoints(Room room) 
    {
        // adds room to list of generated rooms
        allRooms.Add(room);

        while (room.attachmentPoints.Count > 0) 
        {
            //choose a random remaining attactment point
            Transform targetAttachmentPoint = room.attachmentPoints[UnityEngine.Random.Range(0, room.attachmentPoints.Count)];

            //wait
            yield return new WaitForSeconds(pauseBeweenRooms);

            // random number generation decides if an attachment point becomes a door or a wall as long as the dungeon size permits
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= 0.80f && dungeonSize > 0)
            {
                Debug.LogWarning("Attempt Creation of a Connecting Room @ " + targetAttachmentPoint.parent.parent.name+ ":"+ targetAttachmentPoint.name);

                // if it CAN successfully generate a room
                if (GenerateConnectedRoom(targetAttachmentPoint))
                {

                }

                // if it CANT successfully generate a room, make a wall instead
                else
                {
                    Debug.LogError("ERR01 : Failed Attempt of a Connecting Room @ " + targetAttachmentPoint.parent.parent.name + ":" + targetAttachmentPoint.name);

                    // randomly select from various walls
                    Instantiate(blockers[UnityEngine.Random.Range(0, blockers.Length)], targetAttachmentPoint);
                }
            }

            // decides to be a wall |OR| no more rooms can be generated
            else
            {
                Debug.Log("Placing a Wall @ " + targetAttachmentPoint.parent.parent.name + ":" + targetAttachmentPoint.name);

                // randomly select from various walls
                Instantiate(blockers[UnityEngine.Random.Range(0, blockers.Length)], targetAttachmentPoint);
            }

            // removes this attachment point as one to be selected from the future
            room.attachmentPoints.Remove(targetAttachmentPoint);
            Debug.Log("REMOVED : " + targetAttachmentPoint.name + " @ " + targetAttachmentPoint.parent.parent.name);
        }

    }

    // 
    bool GenerateConnectedRoom(Transform targetAttachmentPoint)
    {
        // choose a random room from the room list
        Room newRoom = Instantiate(allTiles[UnityEngine.Random.Range(0, allTiles.Length)]).GetComponent<Room>();

        // get a new attachment point that has a valid room placement
        Transform newRoomValidAttachmentPoint = FindValidAttachmentPoint(newRoom, targetAttachmentPoint);

        // if a valid room position has been created successfully
        if (newRoomValidAttachmentPoint != null)
        {
            // attach doorways to the two points between the doors
            AttachDoorways(targetAttachmentPoint, newRoomValidAttachmentPoint);
            dungeonSize--;

            // increase room distance from spawn by 1 more than the previous room
            newRoom.floodValue = targetAttachmentPoint.GetComponentInParent<Room>().floodValue + 1;

            // ROOM RECURSION GENERATION BEGINS HERE
            StartCoroutine(AssignAttachmentPoints(newRoom));
            //AssignAttachmentPoints(newRoom);

            return true;
        }
        // room type had no valid connection points
        else 
        {
            // destroys unusable room gameobject
            GameObject.Destroy(newRoom.gameObject);

            // TO-DO:
            // choose another room not already checked
        }

        return false;

    }

    // only deals with choosing a attachment point in newly generated room to connect to previous room's attachment point
    Transform FindValidAttachmentPoint(Room room, Transform pointToConnectTo)
    {
        // list to remember which points have been attempted as not to try them again
        List<Transform> attemptedPoints = new List<Transform>();

        while (room.attachmentPoints.Count > 0)
        {
            Transform newAttachmentPoint = room.attachmentPoints[UnityEngine.Random.Range(0, room.attachmentPoints.Count)];

            // found a valid room to place
            if (CheckNewRoomPlacement(room, newAttachmentPoint, pointToConnectTo))
            {
                Debug.LogWarning("Attached Connecting Room @ " + newAttachmentPoint.parent.parent.name + ":" + newAttachmentPoint.name);

                // remove point from attempted positions in the future
                room.attachmentPoints.Remove(newAttachmentPoint);

                // adds attempted points back to original list to be sorted through afterwards
                room.attachmentPoints.AddRange(attemptedPoints);

                return newAttachmentPoint;
            }
            else 
            {
                Debug.Log("Connection #" + attemptedPoints.Count + " to" + newAttachmentPoint.parent.parent.name + " : " + newAttachmentPoint + " unsuccessful trying new point");

                // add point to the new attempted list and remove point from existing list and try again
                attemptedPoints.Add(newAttachmentPoint);
                room.attachmentPoints.Remove(newAttachmentPoint);
            }
        }

        return null;
    }

    // verifies the placement of a room by checking if a room is NOT overlapping another placed room
    bool CheckNewRoomPlacement(Room room, Transform newAttachmentPoint, Transform pointToConnectTo)
    {
        //Rotates room to align selected attachment points next to eachother
        AdjustParentToChildTargetTransform(room.transform, newAttachmentPoint, pointToConnectTo);

        // checking if room placement is valid if not change attachment point
        bool collisions = Physics.CheckBox(room.transform.position, room.GetComponentInChildren<Collider>().bounds.size / 2, room.transform.rotation);
        if (!collisions)
        {
            // room successfully has been placed
            return true;
        }

        return false;
    }

    void AdjustParentToChildTargetTransform(Transform parent, Transform child, Transform target) 
    {
        // Store initial local position and rotation of the child
        Vector3 attachmentLocalPosition = child.localPosition;
        Quaternion attachmentLocalRotation = child.localRotation;

        // Set child to desired position and rotation
        child.position = target.position + (-target.forward * 2);
        child.rotation = Quaternion.Euler(target.rotation.eulerAngles.x, target.rotation.eulerAngles.y + 180, target.rotation.eulerAngles.z);

        // Move the parent to child's position
        parent.position = child.position;

        // Calculate the reverse rotation
        Quaternion reverseRotation = Quaternion.Inverse(attachmentLocalRotation);

        // Rotate the child and parent
        child.RotateAround(child.position, child.forward, reverseRotation.eulerAngles.z);
        child.RotateAround(child.position, child.right, reverseRotation.eulerAngles.x);
        child.RotateAround(child.position, child.up, reverseRotation.eulerAngles.y);

        parent.rotation = child.rotation;

        // Adjust parent position
        parent.position -= parent.right * attachmentLocalPosition.x;
        parent.position -= parent.up * attachmentLocalPosition.y;
        parent.position -= parent.forward * attachmentLocalPosition.z;

        // Reset local position and rotation of the child
        child.SetLocalPositionAndRotation(attachmentLocalPosition, attachmentLocalRotation);
    }

    // adds a doorway to the room and adds it to the room's door list
    void AttachDoorways(Transform AttachmentPoint1, Transform AttachmentPoint2) 
    {
        // randomly generate door from various doorways
        GameObject doorway = connectors[UnityEngine.Random.Range(0, connectors.Length)];

        // parents door to target attachment points
        Transform door1 = Instantiate(doorway, AttachmentPoint1).transform;
        Transform door2 = Instantiate(doorway, AttachmentPoint2).transform;
    }

    void AssignDungeon()
    {
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
    }

    // if somehow the dungeon is "completed" but the remaining rooms are still above 0, break down a wall and force a doorway
    void ForceDoorWay() 
    { 
    
    }

    // if a wall is near another wall attachment point in another room, attempt at creating a doorway
    void AttemptLocalConnection() 
    { 
        
    }

}
