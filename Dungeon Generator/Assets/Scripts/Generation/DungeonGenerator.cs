using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int dungeonSize = 25;
    private int sizeCount;

    public int maxFailedAttempts = 20;
    private int attemptCount;

    [Header("Root/Spawn")]
    public GameObject spawnTile;

    [Header("Attachments")]
    public GameObject[] connectors;
    public GameObject[] blockers;

    [Header("Tile Sets")]
    [SerializeField] public Tiles[] allTiles;

    [Space(10)]

    [Range(0.0f, 2.5f)]
    public float pauseBeweenRooms = 0;

    public float tilePadding = 0;

    [Space(10)]

    public bool restrictToBounds = false;
    public Vector3 center = Vector3.zero;
    public Vector3 extents = new Vector3(100.0f, 25.0f, 100.0f);

    private List<Tile> allRooms = new List<Tile>();
    private List<Tile> mainPath = new List<Tile>();

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
        Tile spawnRoom = Instantiate(spawnTile).GetComponent<Tile>();
        allRooms.Add(spawnRoom);

        // assign attachment points "randomly"
        yield return StartCoroutine(AssignAttachmentPoints(spawnRoom));

        EvaluateDungeon();
    }

    // iterate through all the attachment points in a given room
    IEnumerator AssignAttachmentPoints(Tile room) 
    {
        // stores the original amount of attachment points in a tile
        int totalPointCount = room.socketList.Count;

        // iterates through all the attachment points in a tile
        while (room.socketList.Count > 0) 
        {
            // choose a random remaining attactment point
            Socket targetSocket = room.socketList[Random.Range(0, room.socketList.Count)];
         
            // waiting in between generation steps
            // small native delay to prevent items from spawning on top of eachother with 0 delay
            yield return new WaitForSeconds(pauseBeweenRooms + 0.05f);

            // if more rooms can be generated, allow attempts. Otherwise place a blocker
            if (sizeCount < dungeonSize)
            {
                // uses an animation curve to determine the likelyhood of a door to be created and spawned in
                // with every successful connection created in a tile, the less likely-hood of another being made 
                float chance = room.chanceCurve.Evaluate((float)room.doorways.Count / (float)totalPointCount);

                //Debug.Log(chance);

                if (Random.Range(0.00f, 1.00f) <= chance)
                {
                    Tile newTile = FindValidTile(targetSocket);

                    if (newTile != null)
                    {
                        // increase new tile distance from spawn by 1
                        // adds new tile to room list
                        newTile.floodValue = room.floodValue + 1;
                        allRooms.Add(newTile);

                        sizeCount++;

                        // ROOM RECURSION GENERATION BEGINS HERE
                        yield return StartCoroutine(AssignAttachmentPoints(newTile));
                    }
                    else
                    {
                        // randomly select from various blockers
                        Instantiate(blockers[Random.Range(0, blockers.Length)], targetSocket.transform);
                    }
                }
                else
                {
                    // randomly select from various blockers
                    Instantiate(blockers[Random.Range(0, blockers.Length)], targetSocket.transform);
                }
            }
            else
            {
                // randomly select from various blocker
                Instantiate(blockers[Random.Range(0, blockers.Length)], targetSocket.transform);
            }

            // removes this attachment point from being selected
            room.socketList.Remove(targetSocket);
        }

    }

    // 
    Tile FindValidTile(Socket targetSocket)
    {
        // choose a random tile
        Tile newTile = Instantiate(DetermineWeightedTileSet()).GetComponent<Tile>();

        // TO DO:
        // go through tile list to find a tile that might work

        // get an attachment point in in the new tile that has a valid room placement
        Socket validAttachmentPoint = FindValidAttachmentPoint(newTile, targetSocket);

        // if a valid room position has been created successfully
        if (validAttachmentPoint != null)
        {
            // attach doorways to the two points between the doors
            AttachDoorways(targetSocket, validAttachmentPoint);

            return newTile;
        }

        // destroys unusable tile
        GameObject.Destroy(newTile.gameObject);

        return null;

    }

    // Iterates through attachment points in room to connect to previous room's attachment point
    Socket FindValidAttachmentPoint(Tile tile, Socket targetAttachmentPoint)
    {
        // list to remember which points have been attempted as not to try them again
        List<Socket> attemptedPoints = new List<Socket>();

        while (tile.socketList.Count > 0)
        {
            Socket attachmentPoint = tile.socketList[Random.Range(0, tile.socketList.Count)];

            // found a valid room to place
            if (CheckTilePlacement(tile, attachmentPoint, targetAttachmentPoint))
            {
                //Debug.LogWarning("Attached Connecting Room @ " + newAttachmentPoint.parent.parent.name + ":" + newAttachmentPoint.name);

                // remove point from attempted positions in the future
                tile.socketList.Remove(attachmentPoint);

                // adds attempted points back to original list to be sorted through afterwards
                tile.socketList.AddRange(attemptedPoints);

                return attachmentPoint;
            }
            else 
            {
                //Debug.Log("Connection #" + attemptedPoints.Count + " to" + newAttachmentPoint.parent.parent.name + " : " + newAttachmentPoint + " unsuccessful trying new point");

                // add point to the new attempted list and remove point from existing list and try again
                attemptedPoints.Add(attachmentPoint);
                tile.socketList.Remove(attachmentPoint);
            }
        }

        return null;
    }

    // verifies the placement of a room by checking if a room is NOT overlapping another placed room
    bool CheckTilePlacement(Tile tile, Socket attachmentPoint, Socket pointToConnectTo)
    {
        //Rotates room to align selected attachment points next to eachother
        AdjustParentToChildTargetTransform(tile.transform, attachmentPoint.transform, pointToConnectTo.transform);

        Collider[] hitColliders = Physics.OverlapBox(tile.transform.position, tile.GetComponent<BoxCollider>().size / 2, tile.transform.rotation);

        //Check when there is a new collider coming into contact with the tile
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].gameObject != tile.gameObject)
            {
                //Debug.Log("Hit : " + hitColliders[i].gameObject.name);
                return false;
            }
        }

        if (restrictToBounds)
        {
            Vector3 boundary = center + extents;

            if (Mathf.Abs(tile.transform.position.x) > Mathf.Abs(boundary.x) ||
                Mathf.Abs(tile.transform.position.y) > Mathf.Abs(boundary.y) ||
                Mathf.Abs(tile.transform.position.z) > Mathf.Abs(boundary.z)){

                //Debug.LogError("Tile Position is out of allowed bounds...\n TileX:" + tile.transform.position + "\n BoundaryX: " + boundary);
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

        if (parent.GetComponent<Tile>().allowRotation)
        {
            child.rotation = Quaternion.Euler(target.rotation.eulerAngles.x, target.rotation.eulerAngles.y + 180, target.rotation.eulerAngles.z);
        }

        parent.SetParent(null);
        child.SetParent(parent);
    }

    // adds a doorway to the room and adds it to the room's door list
    void AttachDoorways(Socket socket1, Socket socket2) 
    {
        // randomly generate door from various doorways
        GameObject doorway = connectors[Random.Range(0, connectors.Length)];

        // parents door prefab to target attachment points
        Transform door1 = Instantiate(doorway, socket1.transform).transform;
        Transform door2 = Instantiate(doorway, socket2.transform).transform;

        // adds attachment points to doorway list
        socket1.GetComponentInParent<Tile>().doorways.Add(socket1);
        socket2.GetComponentInParent<Tile>().doorways.Add(socket2);
    }

    GameObject DetermineWeightedTileSet()
    {
        // determines the total of all the weights of all the rooms
        float totalChance = 0.0f;
        foreach (Tiles tile in allTiles)
        {
            totalChance += tile.tileWeight;
        }

        // the random number determining the room that is going to spawn
        float rand = Random.Range(0.0f, totalChance);
        float cumulativeChance = 0.0f;

        foreach (Tiles tile in allTiles)
        {
            cumulativeChance += tile.tileWeight;

            if (cumulativeChance >= rand)
            {
                return tile.tilePrefab;
            }
        }

        Debug.LogError("Failed to choose a weighted tile.");
        return null;
    }

    void EvaluateDungeon()
    {
        Debug.Log("Beginning Dungeon Evalation...");

        // Once generation is done, check for a successfully generated dungeon
        if (sizeCount == dungeonSize)
        {
            // sorts all the rooms in the dungeon by their floodValue from Highest to lowest
            Tile furthestRoom = allRooms.OrderByDescending(room => room.floodValue).ToList()[0];

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

                foreach (Tile room in mainPath)
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
            Debug.LogWarning("Dungeon Evaluation Failed. \n Failed to reach the set amount of rooms in the Dungeon. Attempting generation again...");
            ResetGeneration();
        }
    }

    // used to reset dungeon generation
    private void ResetGeneration()
    {
        if (attemptCount < maxFailedAttempts)
        {
            attemptCount++;

            foreach (Tile tile in allRooms)
            {
                Destroy(tile.gameObject);
            }

            allRooms.Clear();
            sizeCount = 0;

            // restart generation
            StartCoroutine(GenerateDungeon());
        }
        else
        {
            Debug.LogError("ERROR: Failed to generate a Dungeon under the Max Failed attempts in the Editor");
        }
    }
}

[System.Serializable]
public class Tiles
{   
    public GameObject tilePrefab;
    public float tileWeight;
}
