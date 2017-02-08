using UnityEngine;
using System;
using System.Collections.Generic;
using DaTup;
using System.Linq;

/// <summary>
/// Struct for using X and Z values without the Node instance
/// </summary>
public struct NodeVector2
{
    public int x;
    public int z;

    /// <summary>
    /// Create a mimic position node.
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="z">Z position</param>
    public NodeVector2(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}

/// <summary>
/// The Component for a procedural building
/// </summary>
public class BuildingGeneration : MonoBehaviour
{
    private static readonly int[] reverseIndicies = new int[4] { 2, 3, 0, 1 };
    public static int GetOppositeNeighborIndex(int index) { return reverseIndicies[index]; }

    private const int difficultyMeasure = 4;
    private const int cubicMeasureX     = 5;
    private const int cubicMeasureY     = 3;
    private const int cubicMeasureZ     = 5;

    public GameObject m_TestPlane;
    public GameObject m_LightPrefab;

    public bool ColorTesting        = false;
    public bool DecorationTesting   = false;
    public bool RoomTesting         = false;

    public RoomDecorationCollections TILES_BATHROOM;
    public RoomDecorationCollections TILES_CELL;
    public RoomDecorationCollections TILES_CORRIDOR;
    public RoomDecorationCollections TILES_KITCHEN;
    public RoomDecorationCollections TILES_MEDICAL;
    public RoomDecorationCollections TILES_OFFICE;

    private List<GameObject>[] floorRoutes;

    private List<GameObject> floodContainer     = new List<GameObject>();
    private List<GameObject> floodConnections   = new List<GameObject>();

    public List<BlockPiece> potentialExits;

    public FloorLevel[] floorBlocks;
    public GameObject[] reworkedMeshes;
    public GameObject   exitModel;
    
    [Range(1, 3)]
    public int difficultyMultiplier = 1;

    private GameObject[][,] floors;
    private GameObject[,,]  blocks;
    private GameObject      doorBlock;

    private int absoluteX;
    private int absoluteY;
    private int absoluteZ;

    private int boundaryX;
    private int boundaryZ;
    private int boundaryY;

    private float incrementX = 0.0f;
    private float incrementY = 0.0f;
    private float incrementZ = 0.0f;

    public int Height   { get { return absoluteY; } }

    public int Width    { get { return absoluteX; } }

    public int Depth    { get { return absoluteZ; } }

    public bool SameRowOrColumn(BlockPiece node1, BlockPiece node2)             { return (node1.GetX() == node2.GetX() || node1.GetZ() == node2.GetZ()); }

    public bool SameRowOrColumn(BlockPiece nodeRef, int xRef2, int zRef2)       { return (nodeRef.GetX() == xRef2 || nodeRef.GetZ() == zRef2); }

    public bool SameRowOrColumn(int xRef1, int zRef1, int xRef2, int zRef2)     { return (xRef1 == xRef2 || xRef2 == zRef2); }

    /// <summary>
    /// Creates the values for the game's world space and scaling.
    /// </summary>
    private void InitializeGameSpace()
    {
        // -------------------------- STRUCTURE ------------------------------
        // Generate the random size for the building "4D" cube
        int difficultyRating = difficultyMultiplier * difficultyMeasure;
        absoluteX = 10;
        absoluteY = difficultyRating;
        absoluteZ = 10;
        boundaryX = absoluteX - 1;
        boundaryY = absoluteY - 1;
        boundaryZ = absoluteZ - 1;
        float scaleX = absoluteX * cubicMeasureX;
        float scaleY = absoluteY * cubicMeasureY;
        float scaleZ = absoluteZ * cubicMeasureZ;
        incrementX = (1.0f / absoluteX);
        incrementY = (1.0f / absoluteY);
        incrementZ = (1.0f / absoluteZ);
        Debug.Log("Building Dimension X = " + absoluteX + " with increments of x = " + incrementX);
        Debug.Log("Building Dimension Y = " + absoluteY + " with increments of y = " + incrementY);
        Debug.Log("Building Dimension Z = " + absoluteZ + " with increments of z = " + incrementZ);
        this.gameObject.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        this.gameObject.transform.position = new Vector3(0, (scaleY / 2) + 0.1f, 0);
    }

    /// <summary>
    /// Initializes arrays to the generated world space.
    /// </summary>
    public void InitializeParameters()
    {
        potentialExits = new List<BlockPiece>();
        floorBlocks = new FloorLevel[absoluteY];
        blocks = new GameObject[absoluteX, absoluteY, absoluteZ];
        floors = new GameObject[absoluteY][,];
    }

    /// <summary>
    /// Creates a random exist within the building (upon nodes with 1 connection)
    /// </summary>
    private void CreateRandomExit()
    {
        if (potentialExits.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, potentialExits.Count);
            potentialExits[randomIndex].CreateExit(exitModel, false, true);
            Debug.Log("Potential Exit Count: " + potentialExits.Count);
        }
        else
        {
            Debug.LogWarning("There were NO Potential Exits... Fix for certainty");
        }
    }

    /// <summary>
    /// Used to safely create floors
    /// </summary>
    private struct SafetyGeneration
    {
        /// <summary>
        /// The floor number for this generation
        /// </summary>
        public int floor;

        /// <summary>
        /// Type of demolition for this generation
        /// </summary>
        public DemolishType type;

        /// <summary>
        /// Create a safety struct for floor regeneration
        /// </summary>
        /// <param name="y">Floor number</param>
        /// <param name="type">Safety demolition type</param>
        public SafetyGeneration(int y, DemolishType type)
        {
            this.floor = y;
            this.type = type;
        }        
    }

    /// <summary>
    /// Used for Converting a demolition type into the corresponding regeneration type
    /// </summary>
    /// <param name="type">The demolition type</param>
    /// <returns>Converted type</returns>
    private RegenerationType GetRegenerationType(DemolishType type)
    {
        if (type == DemolishType.Full)
        {
            Debug.LogWarning("Full Demolition should not be used for reconstruction");
        }

        return (type == DemolishType.Prime || type == DemolishType.Full) ? RegenerationType.Full : RegenerationType.FixedOnly;
    }


    /// <summary>
    /// The Primary Method for initializing the procedural bulding
    /// </summary>
    public void GenerateBuilding()
    {
        InitializeGameSpace();
        InitializeParameters();
        InstantiateAllFloors();
        SetDoorNode();
        CatchGeneratedFloorsAll();
        CompleteBuilding();
        CreateRandomExit();
    }

    /// <summary>
    /// Instantiates a block with attached nodes components into game space
    /// </summary>
    /// <param name="floorParent">Instance of floor the node belongs to.</param>
    /// <param name="x">X value for the node.</param>
    /// <param name="z">Z value for the node.</param>
    private void CreateBlock(FloorLevel floorParent, int x, int z)
    {
        GameObject thisBlock = new GameObject("Block (" + x + ", " + z + ")");
        thisBlock.AddComponent<BlockPiece>();
        thisBlock.transform.SetParent(this.gameObject.transform);
        thisBlock.transform.localPosition = new Vector3(((-0.5f + (incrementX / 2.0f)) + ((float)x * incrementX)), floorParent.transform.localPosition.y, ((-0.5f + (incrementZ / 2.0f)) + ((float)z * incrementZ)));
        thisBlock.transform.SetParent(floorParent.transform);
        floors[floorParent.FloorNumber][x, z] = thisBlock;
        blocks[x, floorParent.FloorNumber, z] = thisBlock;

        floorParent.floorBlocks.Add(thisBlock.GetComponent<BlockPiece>());
        //floorParent.floorNodes[x, z] = thisBlock.GetComponent<BlockPiece>();

        thisBlock.GetComponent<BlockPiece>().SetCoordinates(x, floorParent.FloorNumber, z);
        thisBlock.GetComponent<BlockPiece>().attachedBuilding = this;
        thisBlock.GetComponent<BlockPiece>().attachedFloor = floorParent;

        AddOcclusionTrigger(thisBlock);
        AddPositionTrigger(thisBlock);

        if (x == floorBlocks[floorParent.FloorNumber].StartX && z == floorBlocks[floorParent.FloorNumber].StartZ)        
            floorBlocks[floorParent.FloorNumber].startBlock = thisBlock;        

        if (floorParent.virtualUnwalkables.Count > 0)
        {
            if (floorParent.VirtualCoordsExist(x, z))
            {
                thisBlock.GetComponent<BlockPiece>().isWalkable = false;
                floorParent.unWalkableBlocks.Add(thisBlock.GetComponent<BlockPiece>());
                floorParent.virtualUnwalkables.RemoveAll(coords => (coords.x == x && coords.z == z));
            }
        }
    }

    /// <summary>
    /// Sets the neighbors for every node on a given floor. Introduces the grid system for every node.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void SetNeighbors(int y)
    {
        if (floorBlocks[y].floorBlocks.Count == 0)
            Debug.LogError("neighbor setting was invalid");
        
        foreach (BlockPiece node in floorBlocks[y].floorBlocks)
        {
            int x = node.GetX();
            int z = node.GetZ();

            if (node.neighbors == null || node.neighbors.Length == 0)
                node.neighbors = new GameObject[4];
            
            if (node.diagonalNeighbors == null || node.diagonalNeighbors.Length == 0)
                node.diagonalNeighbors = new GameObject[4];
            

            // We use the absolute bondaries as we want to check if neighbors are null, not checking neighbor length as it allows us to define specific rotations and direction
            // --------------------------------------------------------- STRAIGHT LINES -------------------------------------------------------------------

            // CHECK TOP
            if ((z - 1) >= 0) //indicates we have a row above the current one
                node.neighbors[0] = floors[y][x, z - 1]; // Top
            
            // CHECK RIGHT
            if ((x + 1) <= boundaryX) //indicates we have a row to the right of the current one
                node.neighbors[1] = floors[y][x + 1, z]; // Right
            
            // CHECK BOTTOM
            if ((z + 1) <= boundaryZ) //indicates we have a row under the current one 
                node.neighbors[2] = floors[y][x, z + 1]; // Bottom
            
            // CHECK LEFT
            if ((x - 1) >= 0) //indicates we have a column to the left of the current one
                node.neighbors[3] = floors[y][x - 1, z]; //left
            
            // --------------------------------------------------------- DIAGONALS -------------------------------------------------------------------

            // CHECK TOP - LEFT
            if ((z - 1) >= 0 && (x - 1) >= 0)
                node.diagonalNeighbors[0] = floors[y][x - 1, z - 1];

            // CHECK TOP - RIGHT
            if ((z - 1) >= 0 && (x + 1) <= boundaryX)
                node.diagonalNeighbors[1] = floors[y][x + 1, z - 1];

            // CHECK BOTTOM - RIGHT
            if ((z + 1) <= boundaryZ && (x + 1) <= boundaryX)
                node.diagonalNeighbors[2] = floors[y][x + 1, z + 1];

            // CHECK BOTTOM - LEFT
            if ((z + 1) <= boundaryZ && (x - 1) >= 0)
                node.diagonalNeighbors[3] = floors[y][x - 1, z + 1];

            // _____________________________________________________________________________________________________________________________________________
            // -------------------------------------------------------- SET MULTIDIMENSIONS ----------------------------------------------------------------

            // --------------------------------------------------------- STRAIGHT LINES --------------------------------------------------------------------
            node.neighborCoords[0, 0] = x;      // Top X value
            node.neighborCoords[0, 1] = z - 1;  // Top Z value

            node.neighborCoords[1, 0] = x + 1;  // Right X value
            node.neighborCoords[1, 1] = z;      // Right Z value

            node.neighborCoords[2, 0] = x;      // Bottom X value
            node.neighborCoords[2, 1] = z + 1;  // Bottom Z value

            node.neighborCoords[3, 0] = x - 1;  // Left X value
            node.neighborCoords[3, 1] = z;      // Left Z value

            // --------------------------------------------------------- DIAGONALS ------------------------------------------------------------------------

            node.diagonalNeigborCoords[0, 0] = x - 1;   // TOP LEFT - X value
            node.diagonalNeigborCoords[0, 1] = z - 1;   // TOP LEFT - Z value

            node.diagonalNeigborCoords[1, 0] = x + 1;   // TOP RIGHT - X value
            node.diagonalNeigborCoords[1, 1] = z - 1;   // TOP RIGHT - Z value

            node.diagonalNeigborCoords[2, 0] = x + 1;   // BOTTOM RIGHT - X value
            node.diagonalNeigborCoords[2, 1] = z + 1;   // BOTTOM RIGHT - Z value

            node.diagonalNeigborCoords[3, 0] = x - 1;   // BOTTOM LEFT - X value
            node.diagonalNeigborCoords[3, 1] = z + 1;   // BOTTOM LEFT - Z value

            //_______________________________________________________________________________________________________________________________________________

        }

        Debug.Log("Finished Setting Neighbors for Floor: " + y);
    }

    /// <summary>
    /// Attempts to sucessfully create a floor with a given safety reconstruction
    /// </summary>
    /// <param name="safety">The safety generation used upon unsucessful constructions</param>
    /// <returns></returns>
    private bool CompleteFloor(SafetyGeneration safety)
    {
        int safetyCycle = 0;
        while(!SafetlyCreateFloor(safety.floor))
        {
            safety.type = DemolishFloor(safety);
            CatchGeneratedFloor(safety);
            safetyCycle++;
            if (safetyCycle > 5)
            {
                Debug.LogError("Building Broke!");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Completes every Floor in the building with the safety demolition type PRIME
    /// </summary>
    private void CompleteBuilding()
    { 
        for (int y = 0; y < absoluteY; y++)
        {
            if (!CompleteFloor(new SafetyGeneration(y, DemolishType.Prime)))
            {
                Debug.LogError("Building has failed to intially start");
                break;
            }
        }
    }

    /// <summary>
    /// Addes the Occlusion component to a block in game space
    /// </summary>
    /// <param name="block">Chosen Block</param>
    private void AddOcclusionTrigger(GameObject block)
    {
        GameObject thisOcclusion = new GameObject("Occlusion Trigger");
        thisOcclusion.transform.SetParent(block.transform);
        thisOcclusion.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        thisOcclusion.AddComponent<BlockOcculsion>();
        thisOcclusion.layer = 8;
    }

    /// <summary>
    /// Adds the position component to a block in game space
    /// </summary>
    /// <param name="block">Chosen block</param>
    private void AddPositionTrigger(GameObject block)
    {
        GameObject thisPosition = new GameObject("Position Trigger");
        thisPosition.transform.SetParent(block.transform);
        thisPosition.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        thisPosition.AddComponent<BlockPosition>();
        thisPosition.layer = 8;
    }

    /// <summary>
    /// Searches the first floor to set an entrance for the building on a random edge node.
    /// </summary>
    public void SetDoorNode()
    {
        Debug.Log("Called Set Door");
        List<BlockPiece> edgeNodes = floorBlocks[0].floorBlocks.FindAll(node => node.isEdgeNodeAbs && !node.isCornerNodeAbs);
        if (edgeNodes.Count != 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, edgeNodes.Count);
            InstantiateDoor(edgeNodes[randomIndex]);
        }
        else
        {
            Debug.LogError("Edge Node count for first floor doesnt exist");
        }
    }

    /// <summary>
    /// Instantiates the intial door on the entrance of the building.
    /// </summary>
    /// <param name="doorNode">The node instance of the entrance.</param>
    private void InstantiateDoor(BlockPiece doorNode)
    {
        this.doorBlock = doorNode.gameObject;
        doorNode.isMainEntNode = true;
        doorNode.isDoorNode = true;
        for (int n = 0; n < doorNode.neighbors.Length; n++)
        {
            if (doorNode.neighbors[n] == null)
            {
                // Remeber we use the stair indicies to create walls or not - they are needed - maybe use this loop to save another loop later on for index of null reference
                doorNode.stairIndicies.Add(n); // Add the virtual node here so we can select an exit prefab
                break;
            }
        }

        doorNode.CreateExit(exitModel, true, false);
        floorBlocks[0].doorBlocks.Add(doorNode);
        Debug.Log("Door at END : " + doorNode.name);
    }

    /// <summary>
    /// Gets a random node within game space.
    /// </summary>
    /// <returns>Random node.</returns>
    public BlockPiece GetRandomNode()
    {
        int chosenY = UnityEngine.Random.Range(0, absoluteY);
        int chosenX = UnityEngine.Random.Range(floorBlocks[chosenY].StartX, floorBlocks[chosenY].EndX);
        int chosenZ = UnityEngine.Random.Range(floorBlocks[chosenY].StartZ, floorBlocks[chosenY].EndZ);
        return blocks[chosenX, chosenY, chosenZ].GetComponent<BlockPiece>();
    }

    /// <summary>
    /// Retrieves a block from game space if it exists.
    /// </summary>
    /// <param name="x">X value grid reference</param>
    /// <param name="y">Y value grid reference</param>
    /// <param name="z">Z value grid reference</param>
    /// <returns>Block in game space</returns>
    public GameObject GetBlock(int x, int y, int z)
    {
        if (x < blocks.GetLength(0) && y < blocks.GetLength(1) && z < blocks.GetLength(2) && x >= 0 && y >= 0 && z >= 0)
        {
            return blocks[x, y, z];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Enables the dynamic active occlusion of every node in game space
    /// </summary>
    public void EnableBuildingOcculsion()
    {
        for (int y = 0; y < absoluteY; y++)
        {
            for (int x = 0; x < absoluteX; x++)
            {
                for (int z = 0; z < absoluteZ; z++)
                {
                    if (floors[y][x, z] != null)
                    {
                        BlockOcculsion occ = floors[y][x, z].GetComponentInChildren<BlockOcculsion>();

                        if (!occ.isOccluded)
                        {
                            occ.EnableOcculsion();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Moves the player to the entrace node of the building
    /// </summary>
    public void MovePlayerToStart()
    {
        GameObject player = GameObject.FindGameObjectWithTag(GameTag.Player);
        player.transform.position = doorBlock.transform.position + new Vector3(0f, 1.75f, 0f);
    }

    /// <summary>
    /// Invokes the Decoration for 3D models within all rooms of a given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    public void DecorateFloorRooms(int y)
    {
        foreach(Room room in floorBlocks[y].AllRooms)
        {
            switch (room.scene)
            {
                case RoomType.Cell:
                    room.DecorateRoom(TILES_CELL);
                    break;

                case RoomType.Bathroom:
                    room.DecorateRoom(TILES_BATHROOM);                    
                    break;

                case RoomType.Medical:
                    room.DecorateRoom(TILES_MEDICAL);                    
                    break;

                case RoomType.Kitchen:
                    room.DecorateRoom(TILES_KITCHEN);                    
                    break;

                case RoomType.Office:
                    room.DecorateRoom(TILES_OFFICE);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Uses the static Decorate to instantiate 3D Models onto the stair nodes of a given floor. These are handled seperately to avoid errors with route searching.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void DecorateStairNodes(int y)
    {
        List<DecorationPiece> decorationPieces = new List<DecorationPiece>();
        foreach (BlockPiece node in floorBlocks[y].stairBlocks)
        {
            if (node.nodeType == BlockType.DISABLED)
                continue;

            DecorationCollection collection = Decorate.GetBlockTypeCollection(TILES_CORRIDOR.Collections, node);
            decorationPieces.RepopulateDecorationList(collection);

            if (decorationPieces.Count != 0)
            {
                Decorate.SetDecorative(decorationPieces[UnityEngine.Random.Range(0, decorationPieces.Count)], node);
            }
            else
            {
                node.SetDecoration(collection.nonDecorationPiece);
            }
        }
    }

    /// <summary>
    /// Uses the static Decorate to instantiate 3D Models onto the corridor nodes of a given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void DecorateFloorCorridor(int y)
    {
        floorBlocks[y].routeBlocks.RemoveAll(n => n == null);
        int numberToDecorate = floorBlocks[y].routeBlocks.Count / 2;
        int currentDecorated = 0;
        List<DecorationPiece> decorationPieces = new List<DecorationPiece>();

        foreach (BlockPiece node in floorBlocks[y].routeBlocks)
        {
            if (node.nodeType == BlockType.DISABLED)
                continue;
            
            DecorationCollection collection = Decorate.GetBlockTypeCollection(TILES_CORRIDOR.Collections, node);
            if (collection == null)
                Debug.LogError("Cannot find collection for type: " + node.nodeType);
            
            int moduloAddition = Mathf.RoundToInt(node.eulerMeshAngle / 90f);
            decorationPieces.RepopulateDecorationList(collection); 

            if (currentDecorated < numberToDecorate)
            {
                if (node.nodeType == BlockType.OneWay || node.nodeType == BlockType.PillarX4) 
                {
                    List<Decoration4W> decorationsWRotations = Decorate.W4Rotations(decorationPieces, node);
                    if (decorationsWRotations.Count != 0)
                    {
                        Decorate.SetDecorative(decorationsWRotations[UnityEngine.Random.Range(0, decorationsWRotations.Count)], node);
                        currentDecorated++;
                    }
                    else
                    {
                        node.SetDecoration(collection.nonDecorationPiece);
                    }
                }
                else
                {
                    List<DecorationPiece> piecesToChose = new List<DecorationPiece>();
                    piecesToChose.AddRange(decorationPieces.FindAll(decoration => Decorate.DecorationCanFit(decoration, node)));

                    if (piecesToChose.Count != 0)
                    {
                        Decorate.SetDecorative(piecesToChose[UnityEngine.Random.Range(0, piecesToChose.Count)], node);
                        currentDecorated++;
                    }
                    else
                    {
                        node.SetDecoration(collection.nonDecorationPiece);
                    }
                }
            }
            else
            {
                node.SetDecoration(collection.nonDecorationPiece);
            }
        }
    }

    /// <summary>
    /// Combines the pathing obtained from the pathfinder into a given floor's route blocks list.
    /// </summary>
    /// <param name="floor">Floor number</param>
    private void CombinePathing(int floor)
    {
        if (floor < absoluteY)
        {
            List<GameObject> floorPartition = GetComponent<Pathfinder>().GetPathPartition();
            if (floorPartition != null)
            {
                foreach (GameObject node in floorPartition)
                {
                    if (!floorBlocks[floor].routeBlocks.Contains(node.GetComponent<BlockPiece>()))
                        floorBlocks[floor].routeBlocks.Add(node.GetComponent<BlockPiece>()); // REVISE THE OBTAINING LIST TO GET BLOCK PIECES NOT GAMEOBJECTS
                }
            }
            else
            {
                Debug.LogError("Floor [" + floor + "] : No path Partition obtained from a fixed Node");
            }
        }
    }

    /// <summary>
    /// Generates stair nodes on the given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    /// <returns>Successful generation?</returns>
    private bool SafetyGenStairNodes(int y)
    {
        #region Configure Stairs

        // If we are not on the Top floor we want to create stairs
        if (y < boundaryY)
        {
            // Potenetial Optimization we can remove from qurantinedList any common nodes between usedCornerParents list
            List<BlockPiece> quarantinedStairNodes = new List<BlockPiece>(floorBlocks[y].floorBlocks);

            // Make a progressive counter to assign incrementing values in array
            int incrementCounter = 0;

            // Have to take the number based on the floor above to help prevent over populating the floor above with entrances and generating unwalkable paths
            int stairBlocks = floorBlocks[y + 1].totalBlocks;
            int maxStairChance = Mathf.RoundToInt(stairBlocks / 16f);
            int stairCount = (maxStairChance >= 1) ? UnityEngine.Random.Range(1, maxStairChance) : 1;
            int proximityFailCount = 0;

            while (incrementCounter < stairCount)
            {
                if (quarantinedStairNodes.Count == 0 || proximityFailCount > 2)
                {
                    // We have run out of avaliable nodes on this floor and must set the amount of fixed nodes to the reduced count
                    return false;
                }

                int randomNode = UnityEngine.Random.Range(0, quarantinedStairNodes.Count); // Get a random Node on the floor
                BlockPiece node = quarantinedStairNodes[randomNode];


                // Check this node chosen
                if (node.isWalkable && !node.isStairConnection && !node.isDoorNode)
                {
                    GameObject aboveBlock = GetBlock(node.GetX(), node.GetY() + 1, node.GetZ());
                    BlockPiece aboveNode = null;

                    // Check above this node because when regenerating a floor dynamically then we can could problems here
                    if (aboveBlock != null)
                    {
                        aboveNode = aboveBlock.GetComponent<BlockPiece>();
                        if (aboveNode.isDoorNode || aboveNode.isStairNode || aboveNode.isRoom || aboveNode.isStairConnection)
                        {
                            quarantinedStairNodes.Remove(node);
                            continue;
                        }
                    }

                    // Check for a suitable parent

                    // Assure there can be at least one connection to the stairs from this floor
                    for (int n = 0; n < node.neighbors.Length; n++)
                    {
                        if (node.neighbors[n] != null)
                        {
                            BlockPiece neighborNode = node.neighbors[n].GetComponent<BlockPiece>();
                            if (neighborNode.isStairNode || (neighborNode.isStairShared && neighborNode.isEdgeNodeAbs) || !neighborNode.isWalkable)
                            {
                                // Parent cannot be only parent for other stair nodes and cannot be a stair node itself
                                continue;
                            }
                            else
                            {

                                // Every Door must have a path to every stair connection otherwise is it discounted
                                if (floorBlocks[y].doorBlocks.Count == 0)
                                    Debug.LogError("No Door Block Count");
                                

                                bool doorsToNeighborFlag = false;
                                foreach (BlockPiece dNode in floorBlocks[y].doorBlocks)
                                {
                                    GetComponent<Pathfinder>().SetOnNode(dNode);
                                    if (!GetComponent<Pathfinder>().GetPath(neighborNode.gameObject, l => l != node.gameObject))
                                    {
                                        doorsToNeighborFlag = true;
                                        break;
                                    }
                                }

                                if (doorsToNeighborFlag)
                                {
                                    proximityFailCount++;
                                    continue;
                                }


                                if (floorBlocks[y].stairBlocks.Count > 0)
                                {
                                    bool notAllPaths = false;
                                    foreach (BlockPiece dNode in floorBlocks[y].doorBlocks)
                                    {
                                        GetComponent<Pathfinder>().SetOnNode(dNode);

                                        foreach (BlockPiece pNode in floorBlocks[y].stairBlocks)
                                        {
                                            if (!GetComponent<Pathfinder>().GetPath(pNode.parent, l => l != node.gameObject))
                                            {
                                                notAllPaths = true;
                                                break;
                                            }
                                        }

                                        if (notAllPaths)
                                        {
                                            break;
                                        }
                                    }

                                    if (notAllPaths)
                                    {
                                        proximityFailCount++;
                                        continue;
                                    }
                                }
                                
                                // Always get the opposite co-ordinates even if the opposite block does not exist
                                int oppX = 0;
                                int oppZ = 0;
                                BlockPiece oppositeNode = null;
                                GameObject oppositeBlock = node.neighbors[GetOppositeNeighborIndex(n)];
                                if (oppositeBlock != null)
                                {
                                    oppositeNode = oppositeBlock.GetComponent<BlockPiece>();
                                    oppX = oppositeNode.GetX();
                                    oppZ = oppositeNode.GetZ();
                                }
                                else
                                {
                                    oppX = (neighborNode.GetX() != node.GetX()) ? ((neighborNode.GetX() < node.GetX()) ? (node.GetX() + 1) : (node.GetX() - 1)) : node.GetX();
                                    oppZ = (neighborNode.GetZ() != node.GetZ()) ? ((neighborNode.GetZ() < node.GetZ()) ? (node.GetZ() + 1) : (node.GetZ() - 1)) : node.GetZ();
                                }


                                GameObject startAbove = GetBlock(oppX, node.GetY() + 1, oppZ);
                                if (startAbove != null)
                                {
                                    BlockPiece startNode = startAbove.GetComponent<BlockPiece>();
                                    if (startNode.isWalkable && !startNode.isStairNode)
                                    {
                                        node.validParents.Add(neighborNode.gameObject);
                                        GetComponent<Pathfinder>().SetOnNode(floorBlocks[y].doorBlocks[0]);
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    } //////// END OF NEIGHBOR CHECKS

                    // Do we have any valid parents after checking then we can create a stair node here
                    if (node.validParents.Count != 0)
                    {
                        node.isStairNode = true;
                        node.testing = true;
                        node.isWalkable = false;
                        node.roomBelonging = RoomType.CORRIDOR;

                        if (aboveBlock != null) // If it does exist then we dont need to add it as it will be checked in the list instead of virtual coords
                        {
                            aboveNode.testing = true;
                            aboveNode.isWalkable = false;
                            if (!floorBlocks[y + 1].unWalkableBlocks.Contains(aboveNode)) // Reminder to add co-ordinates to the floor above to keep the node at this position
                                floorBlocks[y + 1].unWalkableBlocks.Add(aboveNode);
                        }
                        else
                        {
                            // This node does not exist but may exist when regenerated
                            floorBlocks[y + 1].virtualUnwalkables.Add(new NodeVector2(node.GetX(), node.GetZ()));
                        }

                        int parentIndex = UnityEngine.Random.Range(0, node.validParents.Count);
                        node.parent = node.validParents[parentIndex];

                        BlockPiece parentNode = node.parent.GetComponent<BlockPiece>();
                        int oppX = (parentNode.GetX() != node.GetX()) ? ((parentNode.GetX() < node.GetX()) ? (node.GetX() + 1) : (node.GetX() - 1)) : node.GetX();
                        int oppZ = (parentNode.GetZ() != node.GetZ()) ? ((parentNode.GetZ() < node.GetZ()) ? (node.GetZ() + 1) : (node.GetZ() - 1)) : node.GetZ();

                        GameObject startAbove = GetBlock(oppX, node.GetY() + 1, oppZ);
                        BlockPiece startNode = startAbove.GetComponent<BlockPiece>();
                        parentNode.nextFloorParent = startNode;
                        startNode.previousFloorParent = parentNode;

                        if (parentNode.isStairConnection)
                            node.parent.GetComponent<BlockPiece>().isStairShared = true;

                        if (!floorBlocks[y].stairConnectionBlocks.Contains(parentNode))
                            floorBlocks[y].stairConnectionBlocks.Add(parentNode); // stair connections added

                        parentNode.isStairConnection = true;
                        if (!node.minimumConnections.Contains(node.parent))
                            node.minimumConnections.Add(node.parent);

                        if (!parentNode.minimumConnections.Contains(node.gameObject))
                            parentNode.minimumConnections.Add(node.gameObject);

                        for (int i = 0; i < node.neighbors.Length; i++)
                        {
                            if (node.neighbors[i] != null)
                            {
                                if (node.parent == node.neighbors[i])
                                {
                                    if (!startNode.minimumConnections.Contains(startNode.neighbors[i]))
                                        startNode.minimumConnections.Add(startNode.neighbors[i]);

                                    if (!startNode.stairIndicies.Contains(i))
                                        startNode.stairIndicies.Add(i);

                                    break;
                                }
                            }
                        }

                        if (!floorBlocks[y + 1].doorBlocks.Contains(startNode))
                        {
                            floorBlocks[y + 1].doorBlocks.Add(startNode); // door nodes added to the floor above
                        }
                        else
                        {
                            Debug.LogWarning("Found Shared Connection: Floor [" + startNode.GetY() + "] : node (" + startNode.GetX() + ", " + startNode.GetZ() + ")");
                        }

                        startNode.isCorridor = true;
                        startNode.testing = true;
                        startNode.isDoorNode = true;
                        floorBlocks[y].stairBlocks.Add(node); // Stair blocks added
                        incrementCounter++;

                        // TWO lines below not needed ___
                        quarantinedStairNodes.Remove(node);
                        continue;
                    }
                }

                quarantinedStairNodes.Remove(node);
            } //////////////// End of While Loop
        }

        #endregion
        return true;
    }

    /// <summary>
    /// Generates fixed nodes on the given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    /// <returns>Successful generation?</returns>
    private bool SafetyGenFixedNodes(int y)
    {
        #region Configure Fixed Nodes

        // Fixed nodes are based on the current floor and will only affect the generation of this Floor
        // Irrelevant of the floor index, we want to create fixed nodes on the floor
        // Reset Counter
        List<BlockPiece> floorNodes = new List<BlockPiece>(floorBlocks[y].floorBlocks);
        int incrementCounter = 0;
        int fixedBlocks = floorBlocks[y].totalBlocks;
        int maxFixedChance = Mathf.RoundToInt(fixedBlocks / 6f);
        int fixedCount = (maxFixedChance >= 1) ? UnityEngine.Random.Range(1, maxFixedChance) : 1;
        if (floorBlocks[y].floorBlocks.Count == 0)
            Debug.LogError("NO FLOOR BLOCKS - GENERATE FIXED");

        int safetyCycle = 0;
        int proximityFailCount = 0; // Generally when we cannot find 2 fixed nodes then the floor generation has failed - cuts out redundant proccesing
        while (incrementCounter < fixedCount)
        {
            if (floorNodes.Count == 0 || proximityFailCount > 2)
            {
                // We have run out of avaliable nodes on this floor and must set the amount of fixed nodes to the reduced count
                Debug.LogWarning("No more Fixed nodes avaliable on Floor: " + y + "Could not reach total of " + fixedCount + " | Reached Amount: " + incrementCounter);
                return false;
            }

            int randomNode = UnityEngine.Random.Range(0, floorNodes.Count);
            BlockPiece node = floorNodes[randomNode];
            if (node != null)
            {
                if (!node.isDoorNode && !node.isStairNode && node.isWalkable && !node.isRoom)
                {

                    bool doorFlag = false;
                    foreach (BlockPiece dNode in floorBlocks[y].doorBlocks)
                    {
                        GetComponent<Pathfinder>().SetOnNode(dNode);
                        if (!GetComponent<Pathfinder>().GetPath(node))
                        {
                            Debug.Log("Door Node Connection Broke");
                            doorFlag = true;
                            break;
                        }
                    }

                    if (doorFlag)
                    {
                        floorNodes.Remove(node);
                        proximityFailCount++;
                        continue;
                    }


                    bool stairFlag = false;                    
                    foreach (BlockPiece sNode in floorBlocks[y].stairBlocks)
                    {
                        GetComponent<Pathfinder>().SetOnNode(sNode.parent);
                        if (!GetComponent<Pathfinder>().GetPath(node))
                        {
                            Debug.Log("Stair Node Connection Broke");
                            stairFlag = true;
                            break;
                        }
                    }

                    if (stairFlag)
                    {
                        floorNodes.Remove(node);
                        proximityFailCount++;
                        continue;
                    }

                    node.testing = true;
                    node.isFixedNode = true;
                    floorBlocks[y].fixedNodes.Add(node);
                    incrementCounter++;
                    continue;
                }
            }

            safetyCycle++;
            floorNodes.Remove(node);
            if (safetyCycle > 200)
            {
                Debug.LogError("Generate Fixed Nodes Safety Break");
                break;
            }
        }
        #endregion
        return true;
    }

    /// <summary>
    /// Safetly calls the respective generation of a floor and invokes the appropriate demolition if generation fails.
    /// </summary>
    /// <param name="safety">Generation information.</param>
    private void CatchGeneratedFloor(SafetyGeneration safety)
    {
        bool fail = true;
        int safetyCycle = 0;
        while (safetyCycle < 5)
        {
            bool complete = true;
            if (GetRegenerationType(safety.type) == RegenerationType.Full) // Can ? operator this boolean yes ~ This means PRIME or FULL - Demolish To Regeneration Conversion
                complete = SafetyGenStairNodes(safety.floor);
            
            if (complete)
            {
                if (SafetyGenFixedNodes(safety.floor))
                {
                    fail = false;
                    break;
                }
            }
            
            Debug.LogWarning("Reattempting to construct floor base ~ requirements not met");
            safetyCycle++;
            safety.type = DemolishFloor(safety);            
        }

        if (fail)
            Debug.LogError("Reconstruction still didnt work - your out of ideas danny!");
    }

    /// <summary>
    /// Regeneration types for configuring a floor's grid system
    /// </summary>
    private enum RegenerationType
    {
        /// <summary>
        /// Only fixed nodes will be generated.
        /// </summary>
        FixedOnly = 0,

        /// <summary>
        /// Both Fixed nodes and Stair nodes will be generated. 
        /// </summary>
        Full = 1
    }

    /// <summary>
    /// Catches the Regeneration of ALL floors with the default demolition type PRIME
    /// </summary>
    private void CatchGeneratedFloorsAll()
    {
        for (int y = 0; y < floors.Length; y++)
        {
            CatchGeneratedFloor(new SafetyGeneration(y, DemolishType.Prime));
        }           
    }

    /// <summary>
    /// Combines the Path for all fixed nodes of a given floor. Additional chance to path to staircases is included.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void GenerateRandomPath(int y)
    {
        floorBlocks[y].fixedNodes.RemoveAll(n => n == null);
        for (int i = 0; i < floorBlocks[y].fixedNodes.Count; i++)
        {
            List<BlockPiece> floorStairs = new List<BlockPiece>(floorBlocks[y].stairBlocks);
            GetComponent<Pathfinder>().SetOnNode(floorBlocks[y].fixedNodes[i].gameObject);

            // If we are not the last iteration
            if (i < floorBlocks[y].fixedNodes.Count - 1) 
            {
                // Find the next node for all fixed nodes with a chance of pathing to the stair node
                if (y < boundaryY)
                {
                    if (floorStairs.Count != 0) // Savior
                    {
                        int respectiveChance = UnityEngine.Random.Range(0, floorBlocks[y].fixedNodes.Count);
                        if (respectiveChance == 0)
                        {
                            int stairIndex = UnityEngine.Random.Range(0, floorStairs.Count);
                            BlockPiece stairNode = floorStairs[stairIndex];

                            if (stairNode.parent == null)                            
                                Debug.LogError(stairNode.name + "NO STAIR PARENT On Floor: " + stairNode.GetY());                            

                            if (GetComponent<Pathfinder>().GetPath(stairNode.parent))
                            {
                                CombinePathing(y);
                                break;
                            }
                            else
                            {
                                Debug.LogWarning("Failed Search Flag");
                            }
                        }
                    }
                }

                if (i + 1 < floorBlocks[y].fixedNodes.Count)
                {
                    if (GetComponent<Pathfinder>().GetPath(floorBlocks[y].fixedNodes[i + 1].gameObject))
                    {
                        CombinePathing(y);
                    }
                    else
                    {
                        Debug.LogError("Cannot connect to futher fixed nodes");
                    }                                    
                }
                else
                {
                    Debug.LogError("Out of Index exception");
                }
                
            }
            else
            {
                if (y < boundaryY)
                {
                    foreach (BlockPiece node in floorBlocks[y].stairBlocks)
                    {
                        if (node.parent == null)
                            Debug.LogError(node.name + "NO STAIR PARENT On Floor: " + node.GetY());

                        // Go from the first node - or here we could pick a random fixed node ( looping it out of other loop? ) ~ suspicions
                        if (GetComponent<Pathfinder>().GetPath(node.parent))
                        {
                            CombinePathing(y);
                        }
                        else
                        {
                            Debug.LogWarning("Failed Search Flag");
                        }
                    }
                }                
            }
        }
    }

    /// <summary>
    /// Loops the game space to instantiate floors.
    /// </summary>
    private void InstantiateAllFloors()
    {
        for (int y = 0; y < absoluteY; y++)        
            InstantiateFloor(y);               
    }

    /// <summary>
    /// Instantiates a new floor into the game and sets it's properties into the grid system.
    /// </summary>
    /// <param name="y">Floor number</param>
    public void InstantiateFloor(int y)
    {
        GameObject thisFloor = new GameObject("Floor (" + y.ToString() + ")");
        thisFloor.transform.SetParent(this.gameObject.transform);
        thisFloor.transform.localPosition = new Vector3(0, -0.5f + (float)y * incrementY, 0);
        thisFloor.AddComponent<FloorLevel>();
        thisFloor.GetComponent<FloorLevel>().FloorNumber = y;
        floors[y] = new GameObject[absoluteX, absoluteZ];                 // Absolute Matrix - however it is likely to not be fully filled
        floorBlocks[y] = thisFloor.GetComponent<FloorLevel>();

        int newFloorX = UnityEngine.Random.Range(3, 10);
        int newFloorZ = UnityEngine.Random.Range(3, 10);
        int startX = 0;
        int startZ = 0;

        if (y != 0)
        {
            NodeVector2 selected = ProccessedFrameStart(newFloorX, newFloorZ, y, null);
            startX = selected.x;
            startZ = selected.z;
        }

        floorBlocks[y].SetDimensions(startX, startZ, newFloorX, newFloorZ);
        BoxCollider floorEvent = thisFloor.AddComponent<BoxCollider>();
        floorEvent.isTrigger = true;
        floorEvent.center = new Vector3(0f, 1.75f, 0f);
        floorEvent.size = new Vector3((5 * absoluteX), 3f, (5 * absoluteZ));
        Debug.Log("Floor[" + y + "] : contains " + floorBlocks[y].totalBlocks + " blocks");
        ConfigureFloorGrid(y);
    }

    /// <summary>
    /// Initializes a given floor within the grid system.
    /// </summary>
    /// <param name="floor">Floor instance.</param>
    private void InitializeFloorGrid(FloorLevel floor)
    {
        for (int x = floor.StartX; x < floor.EndX; x++)
        {
            for (int z = floor.StartZ; z < floor.EndZ; z++)
            {
                // Null check here - should handle the reconstruct around on regen
                if (blocks[x, floor.FloorNumber, z] == null)                
                    CreateBlock(floor, x, z);
                
                // Need to reapply the edge and corner checks per regeneration - important
                floors[floor.FloorNumber][x, z].GetComponent<BlockPiece>().isEdgeNodeAbs = floor.isEdgeNode(x, z);
                floors[floor.FloorNumber][x, z].GetComponent<BlockPiece>().isCornerNodeAbs = floor.isCornerNode(x, z);
            }
        }

        Debug.Log("Finished Initializing Floor Grid");
    }

    /// <summary>
    /// Has a floor successfully connected to the building? Composition and post proccessing are invoked upon a successful connection. 
    /// </summary>
    /// <param name="y">Floor number</param>
    /// <returns>The state of connection</returns>
    private bool SafetlyCreateFloor(int y)
    {
        if (SuccessfulConnection(y))
        {
            ComposeFloor(y);
            PostProccessFloor(y);
            return true;
        }
        else
        {
            Debug.LogWarning("Unsafely Created Floor");
            return false;
        }
    }

    /// <summary>
    /// Resets a given floor's visited property.
    /// </summary>
    /// <param name="y">Floor number</param>
    private void ResetFloorVisited(int y)
    {
        floorBlocks[y].isVisited = false;
    }

    /// <summary>
    /// Used for testing purposes to instantiate basic planes on each node of a given floor
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void TestPlaneDecoration(int y)
    {
        foreach(BlockPiece node in floorBlocks[y].floorBlocks)
        {
            node.SetDecoration(m_TestPlane);   
        }
    }

    /// <summary>
    /// Applies the main methods for a given floor to create the buildings features.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void ComposeFloor(int y)
    {
        GenerateRandomPath(y);          // Loops Fixed Nodes    Combines path from each fixed node to each one with random chance to connect to stairs. Connect to all stairs upon last fixed node.
        SetCorridorConnections(y);      // Loops Route Blocks   Set them as corridors and add minimum connections - could be set to corridor earlier upon adding to new list
        GenerateRooms(y);               // Loops Route Blocks   Foreach one will start a flood fill for neighbors that arent corridors - Applies Rooms Algorithm
        AdjustFloorNodeProperties(y);   // Loops Floor Blocks   Configures the rotation and type along with wallEdge indicies

        if (ColorTesting)
        {
            TestPlaneDecoration(y);         // Loops Floor Blocks   Creates basic planes on every node for color manipulation ~ visual debugging
        }
        else
        {
            DecorateFloorRooms(y);          // Loops Rooms in Floor and Decorates based upon type set from room algorithm ~ Generate Rooms(y)
            DecorateFloorCorridor(y);       // Loops Route Blocks   Decorate using same logic as the room decoration
            DecorateStairNodes(y);          // Loops Stair Blocks   Decorate using same logic as above (loop for some actions)
            CreateLightsOnFloor(y);
        }

        ResetFloorVisited(y);                // Reset a Floor's virtual contruction
        
    }

    private void CreateLightsOnFloor(int y)
    {
        floorBlocks[y].CreateLights(m_LightPrefab);
    }

    /// <summary>
    /// All post proccessing floor properties are set here.
    /// - Currently sets corridor narrow property
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void PostProccessFloor(int y)
    {
        List<int> iNeighbours = new List<int>();
        foreach (BlockPiece node in floorBlocks[y].routeBlocks) // Route blocks no longer contains stair nodes
        {
            for (int n = 0; n < node.neighbors.Length; n++)
            {
                if (node.neighbors[n] != null)
                {
                    BlockPiece neighbour = node.neighbors[n].GetComponent<BlockPiece>();
                    if (!neighbour.isCorridor || neighbour.isStairNode)
                    {
                        iNeighbours.Add(n);
                    }
                }
            }

            node.isCorridorNarrow = false;
            if (iNeighbours.Count == 2)
            {
                int delta = Mathf.Abs(iNeighbours[0] - iNeighbours[1]);
                node.isCorridorNarrow = (delta == 1 || delta == 3);
            }

            iNeighbours.Clear();
        }
    }

    /// <summary>
    /// Clears the local flood fill container
    /// </summary>
    private void ClearFloodFill()
    {
        floodConnections.Clear();
        floodContainer.Clear();
    }

    /// <summary>
    /// Has the floor successfully combined path from it's connections to the first fixed nodes on the next floor?
    /// </summary>
    /// <param name="y">Floor number.</param>
    /// <returns>Status of floor connection to the next.</returns>
    private bool SuccessfulConnection(int y)
    {        
        int successCounter = 0;

        // We can maybe have null references in list where the nodes used to be
        floorBlocks[y].fixedNodes.RemoveAll(n => n == null); 
        foreach (BlockPiece node in floorBlocks[y].doorBlocks)
        {
            if (node.isStairConnection || node == floorBlocks[y].fixedNodes[0])
            {
                successCounter++;
                continue;
            }

            GetComponent<Pathfinder>().SetOnNode(node.gameObject);
            if (GetComponent<Pathfinder>().GetPath(floorBlocks[y].fixedNodes[0].gameObject))
            {
                successCounter++;
            }
            
            // Important to combine the pathing to make the floor continious
            CombinePathing(y);
        }

        return (successCounter > 0);
    }

    /// <summary>
    /// Loops through the route of a given floor and invokes a flood fill on neighbours that are not also part of the route
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void GenerateRooms(int y)
    {
        // Could Potentially optimize search into the same one for other route block loops
        floorBlocks[y].floorBlocks.RemoveAll(n => n == null);
        foreach (BlockPiece node in floorBlocks[y].routeBlocks)
        {
            for (int n = 0; n < node.neighbors.Length; n++)
            {
                // Check the neighbors of all nodes on the path
                if (node.neighbors[n] != null)
                {
                    BlockPiece neighborNode = node.neighbors[n].GetComponent<BlockPiece>();
                    if (!neighborNode.isCorridor && !neighborNode.isRoom && !neighborNode.isStairNode && neighborNode.isWalkable)
                    {
                        FloodNodeOut(node.neighbors[n]);                            // Flood fill out using reccursion
                        floorBlocks[y].AllRooms.Add(new Room(floodContainer));      // Create a room with the captured nodes from reccursion
                        SortFloodEntrances();                                       // Sort the random entrances to the rooms from the current route
                        ClearFloodFill();                                           // Clear the flood fill container ready for the next iteration
                    }
                }
            }
        }        
    }

    /// <summary>
    /// Uses the local flood container to set properties on nodes for entering the captured area
    /// </summary>
    private void SortFloodEntrances()
    {
        // Sometimes the rooms dont have an entrance - occured with bottom floor entrance node and two stair nodes blocking a small room (only entrance node could be it be wasnt)
        int edgeCount = floodConnections.Count;
        int roomConnections = Mathf.Max(1, Mathf.RoundToInt(0.3f * edgeCount));
        for (int r = 0; r < roomConnections; r++)
        {
            int randomIndex = UnityEngine.Random.Range(0, floodConnections.Count);
            GameObject block = floodConnections[randomIndex];
            BlockPiece node = block.GetComponent<BlockPiece>();
            for (int n = 0; n < node.neighbors.Length; n++)
            {
                if (node.neighbors[n] != null)
                {
                    BlockPiece neightborNode = node.neighbors[n].GetComponent<BlockPiece>();
                    if (neightborNode.isCorridor && !neightborNode.isStairNode)
                    {
                        // Add these two as connections manually
                        node.roomConnectionIndex = n;
                        node.isRoomConnection = true;
                        neightborNode.isCorridorConnection = true;
                        if (!node.minimumConnections.Contains(neightborNode.gameObject))
                            node.minimumConnections.Add(neightborNode.gameObject);
                        
                        if (!neightborNode.minimumConnections.Contains(node.gameObject))
                            neightborNode.minimumConnections.Add(node.gameObject);
                        
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Invokes each node on a given floor to calculate its properties for external traversal and connections
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void AdjustFloorNodeProperties(int y)
    {
        foreach (BlockPiece node in floorBlocks[y].floorBlocks)
        {
            node.ConfigureAcceptedIndicies();
            for (int n = 0; n < node.neighbors.Length; n++)
            {
                if (!node.acceptedIndicies.Contains(n))                
                    node.wallEdgeIndicies.Add(n);                
            }

            node.ConfigureNodeOutlay();
        }
    }

    /// <summary>
    /// Sets routes to corridors with properties and neighbors of a given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void SetCorridorConnections(int y)
    {
        GetComponent<Pathfinder>().ResetLists(false);

        // Pathing is Complete for this floor
        // Now we need to set data for the next iteration of the FLOOR much like the pathing did for the next FIXED Node
        // Here we can set minimum connections allowed for each piece based on how many nieghbors are also on the path
        // Stair node will only have 1 neighbor already set still as the parent from PATHFINDER
        floorBlocks[y].routeBlocks.RemoveAll(n => n == null);
        foreach (BlockPiece node in floorBlocks[y].routeBlocks)
        {
            node.roomBelonging = RoomType.CORRIDOR;
            node.isCorridor = true;
            node.testing = true;

            // If we are not a stair node, we will add all minimum connections to our piece
            // Stairs connections have already been assigned
            if (!node.isStairNode)
            {
                // Set minimum connections based on if the neighbors are also on the path and the neighbor is NOT already on our minimum connection list
                for (int i = 0; i < node.neighbors.Length; i++)
                {
                    if (node.neighbors[i] != null)
                    {
                        BlockPiece neighbor = node.neighbors[i].GetComponent<BlockPiece>();
                        if (floorBlocks[y].routeBlocks.Contains(neighbor) && !node.minimumConnections.Contains(node.neighbors[i]) && !neighbor.isStairNode)                        
                            node.minimumConnections.Add(node.neighbors[i]);                        
                    }
                }
            }
        }
    }

    /// <summary>
    /// Flood fills the local container to hold nodes for a new room around corridors.
    /// </summary>
    /// <param name="fromBlock">Recursive previous block.</param>
    private void FloodNodeOut(GameObject fromBlock)
    {
        BlockPiece fromNode = fromBlock.GetComponent<BlockPiece>();
        if (fromNode.isCorridor || fromNode.isRoom || !fromNode.isWalkable || fromNode.isStairNode)
            return;

        fromNode.isRoom = true;
        floodContainer.Add(fromBlock);
        for (int n = 0; n < fromNode.neighbors.Length; n++)
        {
            if (fromNode.neighbors[n] != null)
            {
                if (fromNode.neighbors[n].GetComponent<BlockPiece>().isCorridor)
                {
                    floodConnections.Add(fromBlock);
                    fromNode.isCorridorEdge = true;
                }
                else
                {
                    fromNode.minimumConnections.Add(fromNode.neighbors[n]);
                }

                FloodNodeOut(fromNode.neighbors[n]);
            }
        }
    }

    /// <summary>
    /// Regenerates all visited floors after demolition has taken place.
    /// </summary>
    /// <param name="playerFloor">Player's floor number.</param>
    public void RegenerateVisitedFloors(int playerFloor)
    {
        SafetyGeneration[] demolitions = GetDemolishments(playerFloor);
        for (int y = 0; y < demolitions.Length; y++)
        {
            // By this point, all demolitions will have been converted into appropriate types for regeneration
            CatchGeneratedFloor(demolitions[y]);
            if (!CompleteFloor(demolitions[y]))
            {
                Debug.LogError("Stopped Regeneration from looping");
                break;
            }
        }
    }

    /// <summary>
    /// Clears all floor blocks for the given floor.
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void ClearFloorAll(int y)
    {
        floorBlocks[y].floorBlocks.Clear();
    }

    /// <summary>
    /// Falses all Door nodes which are also stair connections for the current floor
    /// </summary>
    /// <param name="y"></param>
    private void ClearConsecutiveStair(int y)
    {
        floorBlocks[y].RemoveConsecutiveStairs();
    }

    /// <summary>
    /// Clear all rooms and remove specified nodes from the route on the given floor.
    /// </summary>
    /// <param name="y">Floor number</param>
    /// <param name="routeRemoval">Route removal match</param>
    private void ClearRoomsAndRoutes(int y, Predicate<BlockPiece> routeRemoval)
    {
        floorBlocks[y].AllRooms.Clear();
        floorBlocks[y].routeBlocks.RemoveAll(routeRemoval);
    }

    /// <summary>
    /// Clear the stair nodes and connections for the given floor
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void ClearStairNodes(int y)
    {
        floorBlocks[y].stairBlocks.Clear();
        floorBlocks[y].stairConnectionBlocks.Clear();
    }

    /// <summary>
    /// Clears the Unwalkable nodes for the given floor
    /// </summary>
    /// <param name="y">Floor number.</param>
    private void ClearUnwalkables(int y)
    {
        floorBlocks[y].unWalkableBlocks.Clear();
    }

    /// <summary>
    /// Clears the nodes for the floor above: Door, Unwalkable
    /// </summary>
    /// <param name="y"></param>
    private void ClearNextConnections(int y)
    {
        if (y + 1 < floorBlocks.Length)
        {
            floorBlocks[y + 1].doorBlocks.Clear();
            floorBlocks[y + 1].unWalkableBlocks.Clear();
        }
    }

    /// <summary>
    /// Deletes and nulls all nodes that are specified from a given floor number
    /// </summary>
    /// <param name="y">Floor number.</param>
    /// <param name="nodes">Node search.</param>
    private void FreeUnvaluedNodes(int y, Predicate<BlockPiece> nodes)
    {
        List<BlockPiece> nodesToRemove = floorBlocks[y].floorBlocks.FindAll(nodes);
        floorBlocks[y].fixedNodes.Clear(); // IMPORTANT CLEAR THE FIXED NODES HERE <---------------------------
        int safetyCycle = 0;
        while (nodesToRemove.Count != 0)
        {
            BlockPiece node = nodesToRemove[0];
            blocks[node.GetX(), y, node.GetZ()] = null;
            floors[y][node.GetX(), node.GetZ()] = null;
            floorBlocks[y].floorBlocks.Remove(node);
            nodesToRemove.Remove(node);

            // Potential to remove all other node conditions here
            if (potentialExits.Contains(node)) // This may already remove however it could leave null references in list        
                potentialExits.Remove(node);
            
            safetyCycle++;
            if (safetyCycle > 250)
            {
                Debug.LogError("FreeUnvalued Nodes Safety Break");
                break;
            }

            Destroy(node.gameObject);
        }
    }

    /// <summary>
    /// All Demolition types used before Regeneration
    /// </summary>
    public enum DemolishType
    {
        /// <summary>
        /// Intersection Above AND Below.
        /// Create a random floor maintaining connection to the floor above AND below.
        /// </summary>
        Still = 0,

        /// <summary>
        /// Intersection Below ONLY, Demolished Above ONLY.
        /// Create a random floor maintaining connections to the floor below.
        /// </summary>
        Prime = 1,

        /// <summary>
        /// Intersection Above ONLY, Demolished Below ONLY.
        /// Create a random floor maintaining connections to the floor Above.
        /// </summary>
        Limit = 2,

        /// <summary>
        /// Already Demolished Above AND Below.
        /// Fully recreate a random floor.
        /// </summary>
        Full = 3,

        /// <summary>
        /// There is no demolition requirments or changes to be made.
        /// </summary>
        None = 4
    }

    /// <summary>
    /// Obtain the framework of a floor for restricted or free configuration.
    /// </summary>
    /// <param name="safety">Generation information.</param>
    /// <param name="types">Nodes to base the framework upon.</param>
    /// <returns>A framework respective of the demolition type.</returns>
    private FloorRebuilder IdentifyFrame(SafetyGeneration safety, FloorNodeType[] types)
    {
        if (safety.type == DemolishType.Full) return null;                          // No Frame is needed for a full demolition
        FloorRebuilder frame = floorBlocks[safety.floor].GetFloorFrameWork(types);  // Obtain the frame with the type blocks for minimum dimensions
        if (safety.type == DemolishType.Limit)                                      // Limit demolitions must set an additional overlap
            frame.SetFloorOverlap(floorBlocks[safety.floor - 1]);        

        Debug.Log("Lowest Node Coords  [ " + frame.Xminimum + ", " + frame.Zminimum + " ]");
        Debug.Log("Highest Node Coords [ " + frame.Xmaximum + ", " + frame.Zmaximum + " ]");
        return frame;
    }

    /// <summary>
    /// Determines a demolition type upon floor positioning
    /// </summary>
    /// <param name="y">Questioned floor number.</param>
    /// <param name="playerY">Number of the player's floor.</param>
    /// <returns>Type of demolition for the given floor.</returns>
    private DemolishType GetDemolishType(int y, int playerY)
    {
        bool entranceExists = (floorBlocks[y].doorBlocks.Count != 0);
        return (y + 1 < floorBlocks.Length && (y + 1 == playerY || !floorBlocks[y + 1].isVisited)) ?
                (entranceExists ? DemolishType.Still : DemolishType.Limit) :
                (entranceExists ? DemolishType.Prime : DemolishType.Full);
    }

    /// <summary>
    /// Condition for FULL demolition nodes within a free floor framework
    /// </summary>
    /// <param name="x">X value of node.</param>
    /// <param name="z">Z value of node.</param>
    /// <param name="boundX">X Boundary on free grid iteration</param>
    /// <param name="boundZ">Z Boundary on free grid iteration</param>
    /// <param name="y">Current floor number.</param>
    /// <returns>Has the node met the free requirement?</returns>
    private bool ConditionNewFrame(int x, int z, int boundX, int boundZ, int y)
    {
        // Check there is at least 1 overlap in blocks to assure a possible stair node can be created
        return (floorBlocks[y - 1].floorBlocks.FindAll(node => node.GetX() >= x && node.GetZ() >= z && node.GetX() < boundX && node.GetZ() < boundZ).Count != 0);
    }

    /// <summary>
    /// Condition for non-FULL demoltion nodes within a restricted floor framework
    /// </summary>
    /// <param name="frame">Current floor's framework.</param>
    /// <param name="x">X value of node.</param>
    /// <param name="z">Z value of node.</param>
    /// <param name="boundX">X Boundary of frame on iteration</param>
    /// <param name="boundZ">Z Boundary of frame on iteration</param>
    /// <returns>Has the node met the framework requirement?</returns>
    private bool ConditionExistingFrame(FloorRebuilder frame, int x, int z, int boundX, int boundZ)
    {
        bool conditionX = boundX >= frame.Xmaximum && x <= frame.Xminimum;
        bool conditionZ = boundZ >= frame.Zmaximum && z <= frame.Zminimum;
        return (conditionX && conditionZ);
    }

    /// <summary>
    /// Get the start node for a floor's construction
    /// </summary>
    /// <param name="newFloorX">New floor width.</param>
    /// <param name="newFloorZ">New floor depth.</param>
    /// <param name="y">Floor Number.</param>
    /// <param name="frame">Framework for the current floor.</param>
    /// <returns>Start node for reconstruction</returns>
    private NodeVector2 ProccessedFrameStart(int newFloorX, int newFloorZ, int y, FloorRebuilder frame)
    {
        List<NodeVector2> nodes = new List<NodeVector2>();
        for (int x = 0; x < absoluteX; x++)
        {
            for (int z = 0; z < absoluteZ; z++)
            {
                int boundX = x + newFloorX;
                int boundZ = z + newFloorZ;
                bool queryX = boundX < absoluteX;
                bool queryZ = boundZ < absoluteZ;
                if (queryX && queryZ)
                {
                    if (frame == null) // This happens for FULL demolitions
                    {
                        if (ConditionNewFrame(x, z, boundX, boundZ, y))
                            nodes.Add(new NodeVector2(x, z));
                    }
                    else
                    {
                        if (ConditionExistingFrame(frame, x, z, boundX, boundZ))
                            nodes.Add(new NodeVector2(x, z));
                    }
                }
            }
        }

        if (nodes.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, nodes.Count);
            return nodes[randomIndex];
        }
        else
        {
            Debug.LogError("No Proccessing could find a floor Start: Floor [" + y + "]");
            return new NodeVector2(0, 0);
        }
    }

    /// <summary>
    /// Sets the dimensions of a floor to a configured framework.
    /// </summary>
    /// <param name="safety">Generation information.</param>
    /// <param name="types">Types of nodes for inverse clipping</param>
    private void ConfigureFrame(SafetyGeneration safety, FloorNodeType[] types)
    {
        FloorRebuilder frame = IdentifyFrame(safety, types);
        int deltaX = (frame != null) ? Math.Max(3, ((frame.Xmaximum - frame.Xminimum) + 1)) : 3;
        int deltaZ = (frame != null) ? Math.Max(3, ((frame.Zmaximum - frame.Zminimum) + 1)) : 3;
        int newFloorX = UnityEngine.Random.Range(deltaX, 10);
        int newFloorZ = UnityEngine.Random.Range(deltaZ, 10);
        NodeVector2 selected = ProccessedFrameStart(newFloorX, newFloorZ, safety.floor, frame);
        floorBlocks[safety.floor].SetDimensions(selected.x, selected.z, newFloorX, newFloorZ);
    }

    /// <summary>
    /// Initializes the Floor Grid and Set Neighbours for all the nodes on the given floor.
    /// </summary>
    /// <param name="y">The Floor number.</param>
    private void ConfigureFloorGrid(int y)
    {
        InitializeFloorGrid(floorBlocks[y]);
        SetNeighbors(y);
    }

    /// <summary>
    /// A switch of procceses for cleaning up a demolition.
    /// </summary>
    /// <param name="safety">Generation information.</param>
    private void DemolishCleanUp(SafetyGeneration safety)
    {
        switch(safety.type)
        {
            case DemolishType.Full:
                ClearStairNodes(safety.floor);
                ClearFloorAll(safety.floor); // Just to be sure - can remove once checked <----- THIS LINE
                ClearNextConnections(safety.floor);
                break;

            case DemolishType.Limit:
                ClearConsecutiveStair(safety.floor);
                ClearUnwalkables(safety.floor);
                break;

            case DemolishType.Prime:
                ClearStairNodes(safety.floor);
                ClearNextConnections(safety.floor);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// A container for information of specific demolition types.
    /// </summary>
    private struct DemolishRequest
    {
        public FloorNodeType[] types;
        public Predicate<BlockPiece> search;
        public DemolishRequest(FloorNodeType[] types, Predicate<BlockPiece> search)
        {
            this.types = types;
            this.search = search;
        }
    }

    /// <summary>
    /// Obtain the information for a demolition.
    /// </summary>
    /// <param name="demolition">Type of demolition desired.</param>
    /// <returns>A struct with search tools for a specific demolition type.</returns>
    private DemolishRequest DemolishCall(DemolishType demolition)
    {
        switch (demolition)
        {
            case DemolishType.Full:
                return new DemolishRequest(null, node => true);

            case DemolishType.Prime:
                return new DemolishRequest(new FloorNodeType[2] { FloorNodeType.Doors, FloorNodeType.Unwalkable },
                                            node => ((!node.isDoorNode && node.isWalkable) || node.isStairNode));

            case DemolishType.Limit:
                return new DemolishRequest(new FloorNodeType[2] { FloorNodeType.StairConnections, FloorNodeType.Stairs },
                                            node => (!node.isStairNode && !node.isStairConnection));

            case DemolishType.Still:
                return new DemolishRequest(new FloorNodeType[4] { FloorNodeType.Doors, FloorNodeType.Unwalkable, FloorNodeType.StairConnections, FloorNodeType.Stairs },
                                            node => (node.isWalkable && !node.isDoorNode && !node.isStairConnection && !node.isStairNode));

            default:
                break;
        }

        return new DemolishRequest(null, null);
    }

    /// <summary>
    /// Demolishes a Floor with grid reconstruction.
    /// </summary>
    /// <param name="safety">Floor's generation information.</param>
    /// <returns>Post initial demolition type.</returns>
    private DemolishType DemolishFloor(SafetyGeneration safety)
    {
        DemolishRequest request = DemolishCall(safety.type);
        floorBlocks[safety.floor].ClearNodesOfType(request.types);

        FreeUnvaluedNodes(safety.floor, request.search);        // Removes all the unvalued nodes to keep for this regeneration from the lists on this floor
        ClearRoomsAndRoutes(safety.floor, request.search);      // Clears the rooms and routes on the given floor ready for another regeneration
        DemolishCleanUp(safety);                                // Clears the respective lists which would contain the null references. This also clears the lists on any corresponding floors above or below.
        ConfigureFrame(safety, request.types);                  // Set the new floor dimensions as a minimum based on the nodes that have been kept.
        ConfigureFloorGrid(safety.floor);                       // Recreates a grid to fill out the new floor with empty gameobjects ready for population
        return InitialDemolition(safety.type);                  // Returns the type conversion so the floor can move to the next phase (population - instantiating models and configuration)
    }

    /// <summary>
    /// Converts a demoltion type for after initiation.
    /// </summary>
    /// <param name="type">Initial demolition type.</param>
    /// <returns>Post initial demolition type.</returns>
    private DemolishType InitialDemolition(DemolishType type)
    {
        switch(type)
        {
            case DemolishType.Full:
                return DemolishType.Prime;

            case DemolishType.Limit:
                return DemolishType.Still;

            default:
                return type;
        }
    }

    /// <summary>
    /// Runs the initial demolitions for the floors visited around the player.
    /// </summary>
    /// <param name="playerFloor">The player's floor number.</param>
    /// <returns>An array of post initial demolitions for reconstruction.</returns>
    private SafetyGeneration[] GetDemolishments(int playerFloor)
    {
        List<SafetyGeneration> demolitions = new List<SafetyGeneration>();
        for (int y = 0; y < absoluteY; y++)
        {
            if (floorBlocks[y].isVisited && y != playerFloor)
            {
                DemolishType demolition = GetDemolishType(y, playerFloor);
                SafetyGeneration safety = new SafetyGeneration(y, demolition);
                safety.type = DemolishFloor(safety);
                demolitions.Add(safety);
            }
        }

        return demolitions.ToArray();
    }

}
