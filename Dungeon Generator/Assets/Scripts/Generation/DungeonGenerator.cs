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
        Room spawnRoom = Instantiate(spawnRooms[Random.Range(0, spawnRooms.Count)]).GetComponent<Room>();
        dungeonSize--;

        // assign attachment points randomly
        AssignAttachmentPoints(spawnRoom.attachmentPoints);

    }

    // iterate through all the attachment points in a given room
    void AssignAttachmentPoints(List<Transform> attachmentPoints) 
    {
        // iterate through all the attachment points in a given room
        foreach (Transform attachmentPoint in attachmentPoints)
        {
            // random number generation decides if an attachment point becomes a door or a wall, adds to a List<> of newly made doorways
            float _randNum = Random.Range(0.0f, 1.0f);

            // 50/50 chance
            if (_randNum <= 0.5f && dungeonSize > 0)
            {
                AttachDoorway(attachmentPoint);
                GenerateConnectedRoom(attachmentPoint);
            }
            else
            {
                // randomly select from various walls
                Instantiate(attachments[Random.Range(0, attachments.Count)], attachmentPoint);
            }
        }

    }

    // 
    void GenerateConnectedRoom(Transform doorToConnectTo)
    {
        // choose a random room from the room list
        GameObject room = Instantiate(roomsTypes[Random.Range(0, roomsTypes.Count)]);

        if (!FindValidRoomPlacement(room, doorToConnectTo)) 
        { 
            GameObject.Destroy(room);
            
            // choose another room not already checked
        }

    }

    // verifies the placement of a room by checking if a room is NOT overlapping another placed room
    bool FindValidRoomPlacement(GameObject room, Transform doorToConnectTo)
    {
        // list to remember which points have been attempted as not to try them again
        List<Transform> attemptedPoints = new List<Transform>();

        //gets list of all attachment points in the room
        List<Transform> roomAttachmentPoints = room.GetComponent<Room>().attachmentPoints;

        //ITERATE THROUGH LIST IN CASE THE ROOM PLACEMENT IS NOT VALID
        while (roomAttachmentPoints.Count > 0)
        {
            // selects a random attachment point to attempt to connect the previous room to 
            Transform selectedAttachmentPoint = roomAttachmentPoints[Random.Range(0, roomAttachmentPoints.Count)];

            //Rotates room to align selected attachment points next to eachother
            AdjustParentToChildTargetTransform(room.transform, selectedAttachmentPoint, doorToConnectTo);

            // checking if room placement is valid if not change attachment point
            Collider[] collisions = Physics.OverlapBox(room.transform.position, room.GetComponentInChildren<Collider>().bounds.max);
            if (collisions.Length == 0)
            {
                AttachDoorway(selectedAttachmentPoint);
                dungeonSize--;

                // adds new rooms as long as there is space
                AssignAttachmentPoints(roomAttachmentPoints);
                return true;
            }
            else
            {
                // remove attempted attachment point and try again
                attemptedPoints.Add(selectedAttachmentPoint);
                roomAttachmentPoints.Remove(selectedAttachmentPoint);

                Debug.Log("Connection #" + attemptedPoints.Count + " unsuccessful trying new point");
            }
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
    void AttachDoorway(Transform targetAttachmentPoint) 
    {
        // randomly generate door from various doorways and parents it to target attachment point
        Transform door = Instantiate(doorways[Random.Range(0, attachments.Count)], targetAttachmentPoint).transform;

        // adds transform to doorways in room
        targetAttachmentPoint.parent.parent.GetComponent<Room>().doorways.Add(door);
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
