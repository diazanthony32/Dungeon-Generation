using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int dungeonSize;

    public GameObject[] spawnRooms;

    public GameObject[] doorways;
    public GameObject[] attachments;
    
    public GameObject[] roomsTypes;

    List<Room> allRooms = new List<Room>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateDungeon();
    }


    void GenerateDungeon() 
    {
        // choose a random starting room
        Room spawnRoom = Instantiate(spawnRooms[UnityEngine.Random.Range(0, spawnRooms.Length)]).GetComponent<Room>();

        // assign attachment points "randomly"
        AssignAttachmentPoints(spawnRoom);

        // Once generation is done, check for a successfully generated dungeon
        if (dungeonSize == 0)
        {
            // sorts all the rooms in the dungeon by their floodValue from Highest to lowest
            allRooms =  allRooms.OrderByDescending(room => room.floodValue).ToList();

            // Spawns a Marker to visually indicate the final/furthest room and logs it to console
            // credit to : https://manuel-rauber.com/2021/12/29/set-inspector/
            var iconContent = EditorGUIUtility.IconContent("sv_label_6");
            EditorGUIUtility.SetIconForObject(allRooms[0].gameObject, (Texture2D)iconContent.image);

            allRooms[0].gameObject.name = "Boss Room";

            Debug.Log("Furthest Room is " + allRooms[0].floodValue + " rooms away from the spawn room...");
        }
    }

    // iterate through all the attachment points in a given room
    void AssignAttachmentPoints(Room room) 
    {
        // adds room to list of generated rooms
        allRooms.Add(room);

        while (room.attachmentPoints.Count > 0) 
        {
            Transform targetAttachmentPoint = room.attachmentPoints[UnityEngine.Random.Range(0, room.attachmentPoints.Count)];

            // random number generation decides if an attachment point becomes a door or a wall as long as the dungeon size permits
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= 0.5f && dungeonSize > 0)
            {
                // if it CAN successfully generate a room
                if (GenerateConnectedRoom(targetAttachmentPoint))
                {
                    
                }

                // if it CANT successfully generate a room, make a wall instead
                else
                {
                    // randomly select from various walls
                    Instantiate(attachments[UnityEngine.Random.Range(0, attachments.Length)], targetAttachmentPoint);
                }
            }

            // decides to be a wall |OR| no more rooms can be generated
            else
            {
                // randomly select from various walls
                Instantiate(attachments[UnityEngine.Random.Range(0, attachments.Length)], targetAttachmentPoint);
            }

            // removes this attachment point as one to be selected from the future
            room.attachmentPoints.Remove(targetAttachmentPoint);

        }

    }

    // 
    bool GenerateConnectedRoom(Transform targetAttachmentPoint)
    {
        // choose a random room from the room list
        Room newRoom = Instantiate(roomsTypes[UnityEngine.Random.Range(0, roomsTypes.Length)]).GetComponent<Room>();

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
            AssignAttachmentPoints(newRoom);

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
                // remove point from attempted positions in the future
                room.attachmentPoints.Remove(newAttachmentPoint);
                
                return newAttachmentPoint;
            }
            else 
            {
                // add point to the new attempted list and remove point from existing list and try again
                attemptedPoints.Add(newAttachmentPoint);
                room.attachmentPoints.Remove(newAttachmentPoint);

                /*Debug.Log("Connection #" + attemptedPoints.Count + " unsuccessful trying new point");*/
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
        Collider[] collisions = Physics.OverlapBox(room.transform.position, room.GetComponentInChildren<Collider>().bounds.max);
        if (collisions.Length == 0)
        {
            // room successfully has been placed
            return true;
        }

        return false;
    }

    void AdjustParentToChildTargetTransform(Transform parent, Transform child, Transform target) 
    {
        // stores the inital position of the attachment point
        Vector3 attachmentlocalPosition = child.localPosition;
        Quaternion attachmentlocalRotation = child.localRotation;

        // sets child to desired position
        // copys target transform, rotates it by 180 and offsets it by 2 units
        child.position = target.position;
        child.rotation = Quaternion.Euler(target.rotation.eulerAngles.x, target.rotation.eulerAngles.y + 180, target.rotation.eulerAngles.z);
        child.position += child.transform.forward * 2;

        //move the parent to child's position
        parent.position = child.position;

        // HAS TO BE IN THIS ORDER
        //sort of "reverses" the quaternion so that the local rotation is 0 if it is equal to the original local rotation
        child.RotateAround(child.position, child.forward, -attachmentlocalRotation.eulerAngles.z);
        child.RotateAround(child.position, child.right, -attachmentlocalRotation.eulerAngles.x);
        child.RotateAround(child.position, child.up, -attachmentlocalRotation.eulerAngles.y);

        //rotate the parent
        parent.rotation = child.rotation;

        //moves the parent by the child's original offset from the parent
        parent.position = Vector3Int.RoundToInt(parent.position += -parent.right * attachmentlocalPosition.x);
        parent.position = Vector3Int.RoundToInt(parent.position += -parent.up * attachmentlocalPosition.y);
        parent.position = Vector3Int.RoundToInt(parent.position += -parent.forward * attachmentlocalPosition.z);

        // USE THIS INSTEAD IF ABOVE IS GIVING ISSUES (uses raw numbers instead of rounding)--------------------------------------------------------------------------------
        /*parent.position += -parent.right * attachmentlocalPosition.x;
        parent.position += -parent.up * attachmentlocalPosition.y;
        parent.position += -parent.forward * attachmentlocalPosition.z;*/

        //resets local rotation, undoing step 2
        child.localRotation = attachmentlocalRotation;

        //reset local position
        child.localPosition = attachmentlocalPosition;
    }

    // adds a doorway to the room and adds it to the room's door list
    void AttachDoorways(Transform AttachmentPoint1, Transform AttachmentPoint2) 
    {
        // randomly generate door from various doorways
        GameObject doorway = doorways[UnityEngine.Random.Range(0, doorways.Length)];

        // parents door to target attachment points
        Transform door1 = Instantiate(doorway, AttachmentPoint1).transform;
        Transform door2 = Instantiate(doorway, AttachmentPoint2).transform;
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
