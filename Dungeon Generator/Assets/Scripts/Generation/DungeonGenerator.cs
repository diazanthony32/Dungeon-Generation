using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int dungeonSize;

    public List<GameObject> spawnRooms = new List<GameObject>();

    public List<GameObject> doorways = new List<GameObject>();
    public List<GameObject> attachments = new List<GameObject>();
    
    public List<GameObject> roomsTypes = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateDungeon();
    }


    void GenerateDungeon() 
    {
        // choose a random starting room
        Room spawnRoom = Instantiate(spawnRooms[UnityEngine.Random.Range(0, spawnRooms.Count)]).GetComponent<Room>();

        // assign attachment points randomly
        AssignAttachmentPoints(spawnRoom);

    }

    // iterate through all the attachment points in a given room
    void AssignAttachmentPoints(Room room) 
    {
        while (room.attachmentPoints.Count > 0) 
        {
            Transform attachmentPoint = room.attachmentPoints[UnityEngine.Random.Range(0, room.attachmentPoints.Count)];

            // random number generation decides if an attachment point becomes a door or a wall as long as the dungeon size permits
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= 0.5f && dungeonSize > 0)
            {
                // if it CAN successfully generate a room
                if (GenerateConnectedRoom(attachmentPoint))
                {
                    
                }

                // if it CANT successfully generate a room, make a wall instead
                else
                {
                    // randomly select from various walls
                    Instantiate(attachments[UnityEngine.Random.Range(0, attachments.Count)], attachmentPoint);
                }
            }
            // decides to be a wall
            else
            {
                // randomly select from various walls
                Instantiate(attachments[UnityEngine.Random.Range(0, attachments.Count)], attachmentPoint);
            }

            // removes this attachment point as one to be selected from the future
            room.attachmentPoints.Remove(attachmentPoint);

        }

    }

    // 
    bool GenerateConnectedRoom(Transform pointToConnectTo)
    {
        // choose a random room from the room list
        Room newRoom = Instantiate(roomsTypes[UnityEngine.Random.Range(0, roomsTypes.Count)]).GetComponent<Room>();
        Transform newAttachmentPoint = FindValidAttachmentPoint(newRoom, pointToConnectTo);

        // room connections created successfully
        if (newAttachmentPoint != null)
        {
            // attach doorways to the two points between the doors
            AttachDoorways(pointToConnectTo, newAttachmentPoint);
            dungeonSize--;

            // RECURSION 4 FTW
            AssignAttachmentPoints(newRoom);

            return true;
        }
        // room type had no valid connection points
        else 
        {
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

            if (CheckNewRoomPlacement(room, newAttachmentPoint, pointToConnectTo))
            { 
                // found a valid room to place
                return newAttachmentPoint;
            }
            else 
            {
                // add point to the new attempted list and remove point from existing list and try again
                attemptedPoints.Add(newAttachmentPoint);
                room.attachmentPoints.Remove(newAttachmentPoint);

                Debug.Log("Connection #" + attemptedPoints.Count + " unsuccessful trying new point");
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
        parent.position += -parent.right * attachmentlocalPosition.x;
        parent.position += -parent.up * attachmentlocalPosition.y;
        parent.position += -parent.forward * attachmentlocalPosition.z;

        //resets local rotation, undoing step 2
        child.localRotation = attachmentlocalRotation;

        //reset local position
        child.localPosition = attachmentlocalPosition;
    }

    // adds a doorway to the room and adds it to the room's door list
    void AttachDoorways(Transform AttachmentPoint1, Transform AttachmentPoint2) 
    {
        // randomly generate door from various doorways
        GameObject doorway = doorways[UnityEngine.Random.Range(0, attachments.Count)];

        // parents door to target attachment points
        Transform door1 = Instantiate(doorway, AttachmentPoint1).transform;
        Transform door2 = Instantiate(doorway, AttachmentPoint2).transform;

        // adds point to doorways list and removes it from avalible attachment points
        Room room1 = AttachmentPoint1.parent.parent.GetComponent<Room>();
        Room room2 = AttachmentPoint2.parent.parent.GetComponent<Room>();
        
        room1.attachmentPoints.Remove(door1);
        room2.attachmentPoints.Remove(door2);
    }

    // if somehow the dungeon is "completed" but the remaining rooms are still above 0, break down a wall and force a doorway
    void ForceDoorWay() 
    { 
    
    }

    // if a wall is near another wall attachment point in another room, attempt at creating a doorway
    void AttemptLocalConnection() 
    { 
        
    }

    // attempts to close any unused existing doorways and open attachment points with walls
    void SealUpChamber() 
    { 
    
    }
}
