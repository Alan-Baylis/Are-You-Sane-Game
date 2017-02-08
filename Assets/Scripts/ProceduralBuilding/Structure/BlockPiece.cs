using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using DaTup;

public class BlockPiece : MonoBehaviour
{
    private const string m_wallTag = "Wall";
    private const string m_zombieTag = "Zombie";

    public Room thisRoom                    = null;
    private BuildingGeneration thisBuilding = null;
    private FloorLevel thisFloor            = null;
    private GameObject m_InstantiatedModel  = null;
    private LightBehaviour m_Light          = null;

    public bool topLeft     = false;
    public bool topRight    = false;
    public bool bottomRight = false;
    public bool bottomLeft  = false;

    private bool[] diagonalMatches;
    public List<int> sameTypeDiagonals      = new List<int>();
    public List<int> nonSameTypeDiagonals   = new List<int>();
    public List<int> pillarNeighborIndicies = new List<int>();

    public RoomType roomBelonging = RoomType.NONE;

    public bool isMainEntNode           = false;

    public bool isWalkable              = true;
    public bool isFixedNode             = false;
    public bool isEdgeNodeAbs           = false;
    public bool isCornerNodeAbs         = false;

    public bool isSearchPath            = false;
    public bool isSearchOpened          = false;
    public bool isSearchClosed          = false;
    public bool isSearchCornerCut       = false;

    public bool isDoorNode              = false;
    public bool isCorridor              = false;
    public bool isCorridorEdge          = false;
    public bool isCorridorNarrow        = false;
    public bool isCorridorConnection    = false;

    public bool isStairNode             = false; 
    public bool isStairShared           = false;
    public bool isStairBlocked          = false;
    public bool isStairConnection       = false;

    public bool isDecordated            = false;
    public bool isExit                  = false;
    
    public bool isRoom                  = false;
    public bool isRoomEnd               = false;
    public bool isRoomEdge              = false;
    public bool isRoomCorner            = false;
    public bool isRoomNarrow            = false;
    public bool isRoomConnection        = false;
    
    public int roomConnectionIndex;
    public List<GameObject> minimumConnections  = new List<GameObject>();
    public List<GameObject> validParents        = new List<GameObject>();

    public List<int> roomEdgeIndicies   = new List<int>();
    public List<int> wallEdgeIndicies   = new List<int>();
    public List<int> acceptedIndicies   = new List<int>();
    public List<int> stairIndicies      = new List<int>();

    public int[] orderedAcceptedIndicies;
    public int[,] neighborCoords        = new int[4, 2];
    public int[,] diagonalNeigborCoords = new int[4, 2];

    public float g;
    public float h;
    public float f;

    public bool testing = false;

    public GameObject[] neighbors;
    public BlockPiece[] roomNeighbors = new BlockPiece[4];
    public GameObject[] diagonalNeighbors;

    public GameObject parent;
    public BlockPiece nextFloorParent;
    public BlockPiece previousFloorParent;

    private int xCoord;
    private int yCoord;
    private int zCoord;
    private MeshRenderer meshType;
    private Color unWalkableColor       = Color.blue;
    private Color OpenedColor           = Color.red;
    private Color ClosedColor           = Color.yellow;
    private Color PathColor             = Color.green;
    private Color RoomColor             = Color.black;
    private Color CorridorEdgeColor     = Color.cyan;
    private Color RoomConnectionColor   = Color.magenta;
    private Color decoratedColor        = Color.white;
    float colorSmooth = 5;

    public FloorLevel attachedFloor
    {
        get { return thisFloor; }
        set { thisFloor = value; }
    }

    public BuildingGeneration attachedBuilding
    {
        set { thisBuilding = value; }
    }

    public LightBehaviour Light
    {
        get { return m_Light; }
        set { m_Light = value; }
    }
    

    private void ResetPreNeighborProperties()
    {
        topLeft = false;
        topRight = false;
        bottomLeft = false;
        bottomRight = false;
        pillarNeighborIndicies.Clear();
        sameTypeDiagonals.Clear();
        nonSameTypeDiagonals.Clear();
    }

    public void ClearAcceptedIndicies()
    {
        for (int i = 0; i < this.acceptedIndicies.Count; i++)
        {
            if (this.neighbors[this.acceptedIndicies[i]] != null)
            {
                BlockPiece neighbor = this.neighbors[this.acceptedIndicies[i]].GetComponent<BlockPiece>();
                if (neighbor.isWalkable && this.minimumConnections.Contains(neighbor.gameObject))
                    this.minimumConnections.Remove(neighbor.gameObject);
            }
        }

        this.acceptedIndicies.Clear();
        foreach (int index in this.stairIndicies)
            this.acceptedIndicies.Add(index);
    }

    public void ConfigureAcceptedIndicies()
    {
        ResetPreNeighborProperties();
        if (isRoom)        
            if (minimumConnections.Count > 0)            
                minimumConnections.RemoveAll(block => !block.GetComponent<BlockPiece>().isWalkable);                    

        for (int m = 0; m < minimumConnections.Count; m++)
        {
            for (int n = 0; n < neighbors.Length; n++)
            {
                if (neighbors[n] != null)
                {
                    if (minimumConnections[m] != null && neighbors[n] != null)
                    {
                        if (minimumConnections[m].gameObject == neighbors[n].gameObject)
                        {
                            if (!acceptedIndicies.Contains(n))
                            {
                                // Note here - if the added gameObject or node is NULL then the count will still be increased and will add in that index
                                // If a stairNodes bounds on a floor's width and depth creating a null walkable above this will happen for the door node
                                // Door node will have a minimum connection to a null gameObject - still increasing the count and therefore adding the index here
                                acceptedIndicies.Add(n);
                            }
                        }
                    }
                }
                else
                {
                    if (acceptedIndicies.Contains(n))                    
                        acceptedIndicies.Remove(n);                    
                }
            }
        }

        if (isDoorNode)        
            foreach (int stairIndex in stairIndicies)            
                if (!acceptedIndicies.Contains(stairIndex))                
                    acceptedIndicies.Add(stairIndex);                
                    
        // Order them in ascending order - this sorts the list itself. Unlike the linq orderby extension which returns an order enumberable of the list
        acceptedIndicies.Sort((a, b) => a.CompareTo(b));

        foreach (int index in acceptedIndicies)        
            if (isUnindexableNeighbor(index))            
                pillarNeighborIndicies.Add(index);
                
    }

    private float m_eulerMeshAngle = 0f;
    public float eulerMeshAngle
    {
        get { return m_eulerMeshAngle; }
    }

    private BlockType m_nodeType = BlockType.DISABLED;
    public BlockType nodeType
    {
        get { return m_nodeType; }
    }

    // 0 - Open Roof
    // 1 - Tjunc Roof
    // 2 - Tjunc Open
    // 3 - Stairs
    // 4 - UTurn
    // 5 - One Way
    // 6 - Intersection Pillars
    // 7 - Corner Open
    // 8 - Corner with Pillar
    // 9 - Tjunc pillar Left
    // 10 - Tjunc pillar Right
    // 11 - Open pillar single
    // 12 - Open pillar double
    // 13 - Open pillar spilt diag
    // 14 - Open pillar triple


    private static bool unindexableNode(BlockPiece node)
    {
        return (node.isRoomConnection || (node.isStairNode && node) || !node.isWalkable);
    }

    private static bool corridorNode(BlockPiece node)
    {
        return (node.isCorridor && !node.isStairNode && node.isWalkable);
    }
    
    private bool isDeterminedDiagonalOfType(int index)
    {
        if (index < diagonalNeighbors.Length)
        {
            if (diagonalNeighbors[index] != null)
            {
                if (isRoom)
                {
                    return diagonalNeighbors[index].GetComponent<BlockPiece>().isRoom;
                }
                else
                {
                    return corridorNode(diagonalNeighbors[index].GetComponent<BlockPiece>());
                }
            }
        }

        return false;
    }

    private bool isUnindexableNeighbor(int index)
    {
        if (index < neighbors.Length)
        {
            if (neighbors[index] != null)
            {
                BlockPiece neighborNode = neighbors[index].GetComponent<BlockPiece>();
                if (!isRoom)
                {
                    return (neighborNode.isRoomConnection || (neighborNode.isStairNode && isStairConnection) || !neighborNode.isWalkable);
                }
                else
                {
                    return (neighborNode.isCorridorConnection);
                }            
            }
            else
            {
                if (!isRoom)
                {
                    return isStairConnection;
                }
                else
                {
                    return false;
                }
            }
        }
        else
        {
            Debug.LogError("Neighbor index requested was out of bounds!");
            return false;
        }
        
    }

    private bool NeighborMatrixMatchAny(int index)
    {
        return (neighborCoords[index, 0] == diagonalNeigborCoords[index, 0] || neighborCoords[index, 1] == diagonalNeigborCoords[index, 1]);
    }

    private bool NeighborMatrixMatchAny(int nIndex, int dIndex)
    {
        return (neighborCoords[nIndex, 0] == diagonalNeigborCoords[dIndex, 0] || neighborCoords[nIndex, 1] == diagonalNeigborCoords[dIndex, 1]);
    }

    private bool NeighborMatrixMatchExactly(int index)
    {
        return (neighborCoords[index, 0] == diagonalNeigborCoords[index, 0] && neighborCoords[index, 1] == diagonalNeigborCoords[index, 1]);
    }

    private float EntranceRotation(int index)
    {
        switch(index)
        {
            case 0:
                return 0f;
            case 1:
                return 270f;
            case 2:
                return 180f;
            case 3:
                return 90f;
            default:
                return 0f;
        }
    }

    private float MeshAngleCount1(int index)
    {
        switch(index)
        {
            case 0:
                if (isStairNode)
                    return 360f;
                else if (isWalkable)
                    return 180f;
                break;

            case 1:
                if (isStairNode)
                    return 270f;
                else if (isWalkable)
                    return 90f;
                break;

            case 2:
                if (isStairNode)
                    return 180f;
                else if (isWalkable)
                    return 360f;
                break;

            case 3:
                if (isStairNode)
                    return 90f;
                else if (isWalkable)
                    return 270f;
                break;

            default:
                break;
        }

        return 0f;
    }

    private BlockType MeshCornerType(bool diagonal, bool local)
    {
        return (diagonal) ? ((local) ? BlockType.CornerPillar : BlockType.CornerOpen) : BlockType.CornerPillar;
    }

    private BlockType MeshCornerRoomConversion(int diagonalIndex)
    {
        return (isRoom) ? MeshCornerType(isDiagonalNeighborRoom(diagonalIndex), isRoomConnection) : MeshCornerType((isDiagonalNeighborCorridor(diagonalIndex) && !isStairConnection && !isCorridorConnection), (isDoorNode && !isMainEntNode));
    }

    private bool isDiagonalNeighborRoomConversion(int index)
    {
        return (isRoom) ? isDiagonalNeighborRoom(index) : isDiagonalNeighborCorridor(index);
    }

    private struct Accepted3TypesOrdered
    {
        public BlockType[] typesOrdered;

        public Accepted3TypesOrdered(BlockType first, BlockType second, BlockType third)
        {
            typesOrdered = new BlockType[3] { first, second, third };
        }
    }

    private BlockType TjuncSwitchType(Accepted3TypesOrdered types)
    {
        for (int i = 0; i < acceptedIndicies.Count; i++)
        {
            if (pillarNeighborIndicies[0] == acceptedIndicies[i])
            {
                return types.typesOrdered[i];
            }
        }

        Debug.LogWarning("Accepted indices has not returned a match to the pillar index");
        return BlockType.DISABLED;
    }


    private BlockType TjuncDiagonalType(int diagonalIndex, Accepted3TypesOrdered types)
    {
        return (thisBuilding.SameRowOrColumn(diagonalNeighbors[diagonalIndex].GetComponent<BlockPiece>(), neighbors[pillarNeighborIndicies[0]].GetComponent<BlockPiece>())) ? BlockType.TjuncX2 : TjuncSwitchType(types);
    }

    private float PillarX2Accept4Rotation
    {
        get
        {
            if (diagonalMatches[0] && diagonalMatches[1])
            {
                return 180f;
            }
            else if (diagonalMatches[1] && diagonalMatches[2])
            {
                return 90f;
            }
            else if (diagonalMatches[2] && diagonalMatches[3])
            {
                return 0f;
            }
            else // if (intersections[3] && intersections[0] are true
            {
                return 270f;
            }
        }
    }

    private float PillarX2SplitRotation
    {
        get
        {
            return (diagonalMatches[0] && diagonalMatches[2]) ? 90f : 0f;
        }
    }

    private float PillarX2Rotation
    {
        get
        {
            switch(pillarNeighborIndicies[0])
            {
                case 0:
                    return 0f;                    
                case 1:
                    return 270f;
                case 2:
                    return 180f;
                case 3:
                    return 90f;
                default:
                    return 0f;
            }
        }
    }

    private bool PillarX2DiagonalMatch()
    {
        foreach (int dINode in nonSameTypeDiagonals)
        {
            bool rc1 = NeighborMatrixMatchAny(pillarNeighborIndicies[0], dINode);
            bool rc2 = NeighborMatrixMatchAny(pillarNeighborIndicies[1], dINode);
            if ((rc1 || rc2))
                return true;
        }

        return false;
    }

    private bool PillarX2DiagonalSandwich()
    {
        foreach (int dINode in nonSameTypeDiagonals)
        {
            bool rc1 = NeighborMatrixMatchAny(pillarNeighborIndicies[0], dINode);
            bool rc2 = NeighborMatrixMatchAny(pillarNeighborIndicies[1], dINode);
            if ((rc1 && rc2))
                return true;
        }

        return false;
    }

    private float PillarX3DiagonalOnlyRotation
    {
        get
        {
            switch (sameTypeDiagonals[0]) // This might be ONLY if corridor connection
            {
                case 0:
                    return 270f;
                case 1:
                    return 180f;                    
                case 2:
                    return 90f;
                case 3:
                    return 0f;
                default:
                    return 0f;                    
            }
        }
    }

    private float PillarX3RotationLinearSingle
    {
        get
        {
            switch (pillarNeighborIndicies[0])
            {
                case 0:
                    return 90f; // Approved
                case 1:
                    return 270f; // Approved through assumption
                case 2:
                    return 270f; // Approved
                case 3:
                    return 90f; // Approved through assumption
                default:
                    return 0f;
            }
        }
    }

    private float PillarX3Rotation
    {
        get
        {
            if (pillarNeighborIndicies.Contains(0) && pillarNeighborIndicies.Contains(1))
            {
                return 0f;              
            }
            else if (pillarNeighborIndicies.Contains(1) && pillarNeighborIndicies.Contains(2))
            {
                return 270f;                                                             
            }
            else if (pillarNeighborIndicies.Contains(2) && pillarNeighborIndicies.Contains(3))
            {
                return 180f;              
            }
            else //if (pillarNeighborIndicies.Contains(3) && pillarNeighborIndicies.Contains(0))
            {
                return 90f;
            }
        }
    }

    private float PillarX3DiagonalRotation
    {
        get
        {
            if (sameTypeDiagonals.Contains(0) && sameTypeDiagonals.Contains(1))
            {
                return (pillarNeighborIndicies[0] == 1) ? 270f : 180f; // Case 1 or 3
            }
            else if (sameTypeDiagonals.Contains(1) && sameTypeDiagonals.Contains(2))
            {
                return (pillarNeighborIndicies[0] == 0) ? 90f : 180f; // Case 0 or 2
            }
            else if (sameTypeDiagonals.Contains(2) && sameTypeDiagonals.Contains(3))
            {
                return (pillarNeighborIndicies[0] == 1) ? 0f : 90f; // Case 1 or 3
            }
            else //if (sameTypeDiagonals.Contains(3) && sameTypeDiagonals.Contains(0))
            {
                return (pillarNeighborIndicies[0] == 0) ? 0f : 270f; // Case 0 or 2
            }
        }
    }

    private float PillarX3nonDiagonalRotation
    {
        get
        {
            switch (nonSameTypeDiagonals[0])
            {
                case 0:
                    return (pillarNeighborIndicies[0] == 1) ? 0f : 180f;    // Case 1 or 2

                case 1:
                    return (pillarNeighborIndicies[0] == 2) ? 270f : 90f;   // Case 2 or 3

                case 2:
                    return (pillarNeighborIndicies[0] == 3) ? 180f : 0f;    // Case 3 or 0

                case 3:
                    return (pillarNeighborIndicies[0] == 0) ? 90f : 270f;   // Case 0 or 1

                default:
                    return 0f;
            }
        }
    }

    private static float modulate(int value)
    {
        return value % 4;
    }

    private float PillarX1nonDiagonalRotation
    {
        get
        {
            switch (nonSameTypeDiagonals[0]) // Set with 1 pillar
            {
                case 0:
                    return 0f;
                case 1:
                    return 270f;
                case 2:
                    return 180f;
                case 3:
                    return 90f;
                default:
                    return 0f;
            }
        }
    }

    private bool LinearToDiagonalOnlyAny()
    {
        if (pillarNeighborIndicies.Count > 0)
        {
            foreach (int pIRNode in pillarNeighborIndicies)
            {
                if (NeighborMatrixMatchAny(pIRNode, sameTypeDiagonals[0]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool DiagonalToLinearOnlyCount(out int pillarCount)
    {
        pillarCount = 0;
        foreach (int dINode in sameTypeDiagonals)
        {
            if (NeighborMatrixMatchAny(pillarNeighborIndicies[0], dINode))
            {
                pillarCount++;
            }
        }

        return (pillarCount > 0);
    }

    private bool DeltaBool(int abs, params int[] matches)
    {
        for (int i = 0; i < matches.Length; i++)
        {
            if (abs == matches[i])
                return true;
        }

        return false;
    }

    private void OrderAcceptedArray(int moduloAddition)
    {
        for (int i = 0; i < orderedAcceptedIndicies.Length; i++)
        {
            orderedAcceptedIndicies[i] = (i + moduloAddition % 4);
        }
    }

    private void OrderAcceptedCopy()
    {
        if (orderedAcceptedIndicies.Length == acceptedIndicies.Count)
        {
            for (int i = 0; i < orderedAcceptedIndicies.Length; i++)
            {
                orderedAcceptedIndicies[i] = acceptedIndicies[i];
            }
        }
    }


    public void ConfigureNodeOutlay()
    {
        orderedAcceptedIndicies = new int[acceptedIndicies.Count];

        if (acceptedIndicies.Count == 1)
        {
            orderedAcceptedIndicies[0] = acceptedIndicies[0];
            m_eulerMeshAngle = MeshAngleCount1(acceptedIndicies[0]);
            if (isStairNode)
            {
                m_nodeType = BlockType.Stairs;
            }
            else if (isWalkable)
            {
                m_nodeType = BlockType.UTurn;
                if (!isMainEntNode && !isRoom)
                    thisBuilding.potentialExits.Add(this);
            }
        }
        else if (acceptedIndicies.Count == 2)
        {
            // 0 = 1, 2 (90)    - 2
            // 1 = 0, 1 (180)   - 1
            // 2 = 3, 0 (270)   - 0
            // 3 = 2, 3 (360)   - 3

            if (acceptedIndicies.Contains(0) && acceptedIndicies.Contains(1)) // Top-Right                  // CORNERS ----------------------------------------
            {
                OrderAcceptedArray(0);
                m_eulerMeshAngle = 180f;
                m_nodeType = MeshCornerRoomConversion(1);
            }
            else if (acceptedIndicies.Contains(1) && acceptedIndicies.Contains(2)) // Down-Right
            {
                OrderAcceptedArray(1);
                m_eulerMeshAngle = 90f;
                m_nodeType = MeshCornerRoomConversion(2);
            }
            else if (acceptedIndicies.Contains(2) && acceptedIndicies.Contains(3)) // Down-Left
            {
                OrderAcceptedArray(2);
                m_eulerMeshAngle = 360f;
                m_nodeType = MeshCornerRoomConversion(3);
            }
            else if (acceptedIndicies.Contains(3) && acceptedIndicies.Contains(0)) // Top-Left
            {
                OrderAcceptedArray(3);
                m_eulerMeshAngle = 270f;
                m_nodeType = MeshCornerRoomConversion(0);
            }
            else if (acceptedIndicies.Contains(0) && acceptedIndicies.Contains(2)) // Top-Down              // STRIAGHTS --------------------------------------
            {
                OrderAcceptedCopy();
                m_nodeType = BlockType.OneWay;
                m_eulerMeshAngle = 0f;
            }
            else if (acceptedIndicies.Contains(1) && acceptedIndicies.Contains(3)) // Left-Right
            {
                OrderAcceptedCopy();
                m_nodeType = BlockType.OneWay;
                m_eulerMeshAngle = 90f;
            }
        }
        else if (acceptedIndicies.Count == 3)
        {
            // IS CORRIDOR CONNECTIONS IMPLIES +1 TO ANY PILLARS ON BLOCK PIECES SET
            if (acceptedIndicies.Contains(0) && acceptedIndicies.Contains(1) && acceptedIndicies.Contains(2)) // Top-Right-Down
            {
                // i +0 % 4

                OrderAcceptedArray(0);
                #region INDEX { 0 , 1 , 2 }
                topRight = isDiagonalNeighborRoomConversion(1);
                bottomRight = isDiagonalNeighborRoomConversion(2);
                m_eulerMeshAngle = 180f;
                Accepted3TypesOrdered types = new Accepted3TypesOrdered(BlockType.TjuncX1R, BlockType.TjuncX2, BlockType.TjuncX1L);
                // 0 = R
                // 1 = B
                // 2 = L

                if (pillarNeighborIndicies.Count >= 2)
                {
                    m_nodeType = BlockType.TjuncX2;
                }
                else
                {
                    if (topRight && bottomRight)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncSwitchType(types) : BlockType.TjuncX0;
                    }
                    else if (topRight)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(1, types) : BlockType.TjuncX1L;
                    }
                    else if (bottomRight)
                    {

                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(2, types) : BlockType.TjuncX1R;
                    }
                    else
                    {
                        // Set as both pillars normal
                        m_nodeType = BlockType.TjuncX2;
                    }
                }

                #endregion
            }
            else if (acceptedIndicies.Contains(1) && acceptedIndicies.Contains(2) && acceptedIndicies.Contains(3)) // Right-Down-Left
            {
                OrderAcceptedArray(1);
                // i +1 % 4

                // 1 = R
                // 2 = B
                // 3 = L

                #region INDEX { 1 , 2 , 3 }
                bottomRight = isDiagonalNeighborRoomConversion(2);
                bottomLeft = isDiagonalNeighborRoomConversion(3);
                m_eulerMeshAngle = 90f;
                Accepted3TypesOrdered types = new Accepted3TypesOrdered(BlockType.TjuncX1R, BlockType.TjuncX2, BlockType.TjuncX1L);

                if (pillarNeighborIndicies.Count >= 2)
                {
                    m_nodeType = BlockType.TjuncX2;
                }
                else
                {
                    if (bottomRight && bottomLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncSwitchType(types) : BlockType.TjuncX0;
                    }
                    else if (bottomRight)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(2, types) : BlockType.TjuncX1L;
                    }
                    else if (bottomLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(3, types) : BlockType.TjuncX1R;
                    }
                    else
                    {
                        m_nodeType = BlockType.TjuncX2;
                    }
                }

                #endregion
            }
            else if (acceptedIndicies.Contains(2) && acceptedIndicies.Contains(3) && acceptedIndicies.Contains(0)) // Down-Left-Top
            {
                // i +2 % 4

                // 0 = L
                // 2 = R
                // 3 = B
                OrderAcceptedArray(2);

                #region INDEX { 2 , 3 , 0 }
                topLeft = isDiagonalNeighborRoomConversion(0);
                bottomLeft = isDiagonalNeighborRoomConversion(3);
                m_eulerMeshAngle = 0f; // 360f
                Accepted3TypesOrdered types = new Accepted3TypesOrdered(BlockType.TjuncX1L, BlockType.TjuncX1R, BlockType.TjuncX2);


                if (pillarNeighborIndicies.Count >= 2)
                {
                    m_nodeType = BlockType.TjuncX2;
                }
                else
                {
                    if (topLeft && bottomLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncSwitchType(types) : BlockType.TjuncX0;
                    }
                    else if (topLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(0, types) : BlockType.TjuncX1R;
                    }
                    else if (bottomLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(3, types) : BlockType.TjuncX1L;                        
                    }
                    else
                    {
                        m_nodeType = BlockType.TjuncX2;
                    }
                }

                #endregion
            }
            else if (acceptedIndicies.Contains(3) && acceptedIndicies.Contains(0) && acceptedIndicies.Contains(1)) // Left-Top-Right
            {
                // i +3 % 4

                // 0 = B
                // 1 = L
                // 3 = R
                OrderAcceptedArray(3);

                #region INDEX { 3 , 0 , 1 }
                topLeft = isDiagonalNeighborRoomConversion(0);
                topRight = isDiagonalNeighborRoomConversion(1);
                m_eulerMeshAngle = 270f;
                Accepted3TypesOrdered types = new Accepted3TypesOrdered(BlockType.TjuncX2, BlockType.TjuncX1L, BlockType.TjuncX1R);

                if (pillarNeighborIndicies.Count >= 2)
                {
                    m_nodeType = BlockType.TjuncX2;
                }
                else
                {
                    if (topLeft && topRight)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncSwitchType(types) : BlockType.TjuncX0;                        
                    }
                    else if (topLeft)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(0, types) : BlockType.TjuncX1L;
                    }
                    else if (topRight)
                    {
                        m_nodeType = (pillarNeighborIndicies.Count == 1) ? TjuncDiagonalType(1, types) : BlockType.TjuncX1R;
                    }
                    else
                    {
                        m_nodeType = BlockType.TjuncX2;
                    }
                }
                #endregion
            }
        }
        else if (acceptedIndicies.Count == 4)
        {
            if (acceptedIndicies.Contains(0) && acceptedIndicies.Contains(1) && acceptedIndicies.Contains(2) && acceptedIndicies.Contains(3)) // All Directions
            {
                OrderAcceptedArray(0);
                // Check top left and top right
                //        0
                //    [*] ^ [*]
                //  3 <---|---> 1
                //    [*] v [*]
                //        2

                topLeft = isDiagonalNeighborRoomConversion(0);
                topRight = isDiagonalNeighborRoomConversion(1);
                bottomRight = isDiagonalNeighborRoomConversion(2);
                bottomLeft = isDiagonalNeighborRoomConversion(3);

                diagonalMatches = new bool[diagonalNeighbors.Length];

                for (int i = 0; i < diagonalNeighbors.Length; i++)
                {
                    diagonalMatches[i] = isDiagonalNeighborRoomConversion(i);
                    if (diagonalMatches[i])
                    {
                        sameTypeDiagonals.Add(i);
                    }
                    else
                    {
                        nonSameTypeDiagonals.Add(i);
                    }
                }

                if (sameTypeDiagonals.Count == 4) // Set piece with 0 pillars --------------------------------------------------------------------------------------------------------------
                {
                    if (pillarNeighborIndicies.Count >= 2)
                    {
                        m_nodeType = BlockType.PillarX4;
                        m_eulerMeshAngle = 0f;
                    }
                    else if (pillarNeighborIndicies.Count == 1)
                    {
                        m_nodeType = BlockType.PillarX2Same;
                        m_eulerMeshAngle = PillarX2Rotation;
                    }
                    else
                    {
                        m_nodeType = BlockType.Open;
                        m_eulerMeshAngle = 0f;
                    }
                }
                else if (sameTypeDiagonals.Count == 3) // Set piece with 1 pillars -------------------------------------------------------------------------------------------------------
                {
                    if (pillarNeighborIndicies.Count >= 2)
                    {
                        if (pillarNeighborIndicies.Count >= 3)
                        {
                            m_nodeType = BlockType.PillarX4;
                            m_eulerMeshAngle = 0f;
                        }
                        else
                        {                            
                            if (DeltaBool(Mathf.Abs(pillarNeighborIndicies[0] - pillarNeighborIndicies[1]), 2)) // If these are at opposite sides from eachother
                            {
                                m_nodeType = BlockType.PillarX4;
                                m_eulerMeshAngle = 0f;
                            }
                            else
                            {
                                if (PillarX2DiagonalMatch())
                                {
                                    if (PillarX2DiagonalSandwich())
                                    {
                                        m_nodeType = BlockType.PillarX3;
                                        m_eulerMeshAngle = PillarX3Rotation;
                                    }
                                    else
                                    {
                                        m_nodeType = BlockType.PillarX4;
                                        m_eulerMeshAngle = 0f;
                                    }
                                    
                                }
                                else
                                {
                                    m_nodeType = BlockType.PillarX3;
                                    m_eulerMeshAngle = PillarX3Rotation;
                                }
                            }
                        }

                    }
                    else if (pillarNeighborIndicies.Count == 1)
                    {
                        // If diagonal and linear are same row or column then we only need 2 pillars not 3
                        if (NeighborMatrixMatchAny(pillarNeighborIndicies[0], nonSameTypeDiagonals[0]))
                        {
                            m_nodeType = BlockType.PillarX2Same;
                            m_eulerMeshAngle = PillarX2Rotation;
                        }
                        else
                        {
                            m_nodeType = BlockType.PillarX3;
                            m_eulerMeshAngle = PillarX3nonDiagonalRotation;
                        }
                    }
                    else
                    {
                        m_nodeType = BlockType.PillarX1;
                        m_eulerMeshAngle = PillarX1nonDiagonalRotation;
                    }
                }
                else if (sameTypeDiagonals.Count == 2) // Set piece with 2 pillars -----------------------------------------------------------------------------------------------------------
                {
                    if (DeltaBool(Mathf.Abs(sameTypeDiagonals[0] - sameTypeDiagonals[1]), 1, 3)) // are the index differences equal to 1 or 3 (from index 3 to 0 - full cycle) are they next to eachother
                    {
                        if (pillarNeighborIndicies.Count >= 3)
                        {
                            m_nodeType = BlockType.PillarX4;
                            m_eulerMeshAngle = 0f;
                        }
                        else if (pillarNeighborIndicies.Count == 2)
                        {
                            if (DeltaBool(Mathf.Abs(pillarNeighborIndicies[0] - pillarNeighborIndicies[1]), 2)) // If these are at opposite sides from eachother
                            {
                                m_nodeType = BlockType.PillarX4;
                                m_eulerMeshAngle = 0f;
                            }
                            else
                            {
                                if (PillarX2DiagonalMatch())
                                {
                                    if (PillarX2DiagonalSandwich())
                                    {
                                        m_nodeType = BlockType.PillarX3;
                                        m_eulerMeshAngle = PillarX3Rotation;
                                    }
                                    else
                                    {
                                        m_nodeType = BlockType.PillarX4;
                                        m_eulerMeshAngle = 0f;
                                    }
                                }
                                else
                                {
                                    m_nodeType = BlockType.PillarX3;
                                    m_eulerMeshAngle = PillarX3Rotation;
                                }
                            }
                        }
                        else if (pillarNeighborIndicies.Count == 1) // Set 3 Pillars Open
                        {
                            int count = 0;
                            if (DiagonalToLinearOnlyCount(out count))
                            {
                                // Chance here of 4 pillars too !!
                                if (count == 2)
                                {
                                    m_nodeType = BlockType.PillarX4;
                                    m_eulerMeshAngle = 0f;
                                }
                                else
                                {
                                    m_nodeType = BlockType.PillarX3;
                                    m_eulerMeshAngle = PillarX3DiagonalRotation;
                                }
                            }
                            else
                            {
                                m_nodeType = BlockType.PillarX2Same;
                                m_eulerMeshAngle = PillarX2Rotation;
                            }
                        }
                        else
                        {
                            m_nodeType = BlockType.PillarX2Same;
                            m_eulerMeshAngle = PillarX2Accept4Rotation;
                        }
                    }
                    else ///////// DIFFERENCE BETWEEN DIAGONALS IS 2 -------------------- ################################
                    {
                        // At this point, we have a split piece at best (2 opposite pillars)
                        if (pillarNeighborIndicies.Count >= 2)
                        {
                            m_nodeType = BlockType.PillarX4;
                            m_eulerMeshAngle = 0f;
                        }
                        else if (pillarNeighborIndicies.Count == 1)
                        {
                            m_nodeType = BlockType.PillarX3;
                            m_eulerMeshAngle = PillarX3RotationLinearSingle;
                        }
                        else
                        {
                            m_nodeType = BlockType.PillarX2Split;
                            m_eulerMeshAngle = PillarX2SplitRotation;
                        }
                    }
                }
                else if (sameTypeDiagonals.Count == 1) // Set piece with 3 pillar -----------------------------------------------------------------------------------------------------------
                {
                    if (LinearToDiagonalOnlyAny())
                    {
                        m_nodeType = BlockType.PillarX4;
                        m_eulerMeshAngle = 0f;
                    }
                    else
                    {
                        m_nodeType = BlockType.PillarX3;
                        m_eulerMeshAngle = PillarX3DiagonalOnlyRotation;
                    }
                }
                else // Set piece with 4 pillars
                {
                    m_nodeType = BlockType.PillarX4;
                    m_eulerMeshAngle = 0f;
                }
            }
        }
        
    }
    

    public void CreateExit(GameObject exitDoorModel, bool entrance, bool exit)
    {
        if (entrance)
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] == null)
                {
                    SetExit(exitDoorModel, i, 4.155f, true, exit);
                }
            }
        }
        else
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (acceptedIndicies.Contains(i))
                {
                    SetExit(exitDoorModel, BuildingGeneration.GetOppositeNeighborIndex(i), 3.732f, false, exit);
                    Debug.Log("Created Exit Node at: (" + xCoord + ", " + zCoord + ") on floor [" + yCoord + "]");
                    break;
                }
            }
        }
    }

    
    private bool isDiagonalNeighborType(int index, Predicate<BlockPiece> nodeMatch)
    {
        if (index < diagonalNeighbors.Length)
        {
            if (diagonalNeighbors[index] != null)
            {
                return nodeMatch.Invoke(diagonalNeighbors[index].GetComponent<BlockPiece>());
            }
        }

        return false;
    }

    private bool isDiagonalNeighborCorridor(int index)
    {
        if (index < diagonalNeighbors.Length)
        {
            if (diagonalNeighbors[index] != null)
            {
                BlockPiece diaN = diagonalNeighbors[index].GetComponent<BlockPiece>();
                if (diaN.isCorridor && !diaN.isStairNode && diaN.isWalkable)
                    return true; 
            }
        }

        return false;
    }

    private bool isDiagonalNeighborRoom(int index)
    {
        if (index < diagonalNeighbors.Length)
        {
            if (diagonalNeighbors[index] != null)
                return diagonalNeighbors[index].GetComponent<BlockPiece>().isRoom;  
        }

        return false;
        
    }

    public bool isNeighbor(GameObject checkBlock)
    {
        for (int n = 0; n < neighbors.Length; n++)
        {
            if (neighbors[n] != null)
            {
                if (checkBlock == neighbors[n])
                    return true;
            }
        }

        return false;
    }

    void SetExit(GameObject model, int index, float amount, bool locked, bool exit)
    {
        GameObject thisMesh = Instantiate(model, transform.position, model.transform.rotation) as GameObject;
        thisMesh.transform.SetParent(transform);
        thisMesh.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        thisMesh.transform.localRotation = Quaternion.Euler(0f, EntranceRotation(index), 0f);

        HospitalDoorScript script = GetComponentInChildren<HospitalDoorScript>();
        script.Locked = locked;
        script.IsExit = exit;
        
        if (index == 0 || index == 3)
            amount = -amount;

        if (index == 0 || index == 2)
        {
            thisMesh.transform.localPosition = new Vector3(0, 0, amount);
        }
        else
        {
            thisMesh.transform.localPosition = new Vector3(amount, 0, 0);
        }
    }

    public void RemoveDecoration()
    {
        if (m_InstantiatedModel != null)
        {
            Destroy(m_InstantiatedModel);
            m_InstantiatedModel = null;
            meshType = null;
        }
    }

    public void SetDecoration(GameObject obj)
    {
        if (m_InstantiatedModel == null)
        {
            m_InstantiatedModel = Instantiate(obj, transform.position, obj.transform.rotation) as GameObject;
            m_InstantiatedModel.transform.SetParent(transform);

            if (!thisBuilding.ColorTesting)
            {
                m_InstantiatedModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                m_InstantiatedModel.transform.localRotation = Quaternion.Euler(270f, m_eulerMeshAngle, 0f);
            }
            

            if (meshType == null)
            {
                meshType = m_InstantiatedModel.GetComponentInChildren<MeshRenderer>();
            }
        }
    }

    public void SetDecoration(DecorationPiece piece)
    {

        if (m_InstantiatedModel == null)
        {
            m_InstantiatedModel = Instantiate(piece.gameObject, transform.position, piece.gameObject.transform.rotation) as GameObject;
            m_InstantiatedModel.transform.SetParent(transform);
            m_InstantiatedModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            m_InstantiatedModel.transform.localRotation = Quaternion.Euler(270f, m_eulerMeshAngle, 0f);

            if (meshType == null)
            {
                meshType = m_InstantiatedModel.GetComponentInChildren<MeshRenderer>();
            }
        }
    }

    public void SetDecoration(DecorationPiece piece, float rotation)
    {
        if (m_InstantiatedModel == null)
        {
            m_InstantiatedModel = Instantiate(piece.gameObject, transform.position, piece.gameObject.transform.rotation) as GameObject;
            m_InstantiatedModel.transform.SetParent(transform);
            m_InstantiatedModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            m_InstantiatedModel.transform.localRotation = Quaternion.Euler(270f, rotation, 0f);

            if (meshType == null)
            {
                meshType = m_InstantiatedModel.GetComponentInChildren<MeshRenderer>();
            }
        }
        
    }    

    public void SetCoordinates(int x, int y, int z)
    {
        xCoord = x;
        yCoord = y;
        zCoord = z;
    }

    public int GetX()
    {
        return xCoord;
    }

    public int GetY()
    {
        return yCoord;
    }

    public int GetZ()
    {
        return zCoord;
    }

	// Update is called once per frame
	void Update ()
    {
        if (thisBuilding.ColorTesting)
        {
            // Shows the algorithm after all checks and pathfinding searches have been completed
            
            if (isDoorNode)
            {
                ChangeMaterialColor(OpenedColor);
                return;
            }
            else if (isStairNode)
            {
                ChangeMaterialColor(ClosedColor);
                return;
            }
            else if (isFixedNode)
            {
                ChangeMaterialColor(unWalkableColor);
                return;
            }
            else if (isRoomConnection)
            {
                ChangeMaterialColor(RoomConnectionColor);
                return;
            }
            else if (isCorridorEdge)
            {
                ChangeMaterialColor(CorridorEdgeColor);
                return;
            }
            else if (isDecordated)
            {
                ChangeMaterialColor(decoratedColor);
                return;
            }
            else if (isRoom)
            {
                ChangeMaterialColor(RoomColor);
                return;
            }
            else
            {
                if (isWalkable)
                {
                    ChangeMaterialColor(PathColor);
                    return;
                }
            }
        }
        else if (thisBuilding.DecorationTesting)
        {
            if (isDecordated)
            {
                ChangeMaterialColor(unWalkableColor);
                return;
            }
        }
        else if (thisBuilding.RoomTesting)
        {
            switch(roomBelonging)
            {
                case RoomType.Bathroom:
                    ChangeMaterialColor(unWalkableColor);
                    return;

                case RoomType.Cell:
                    ChangeMaterialColor(OpenedColor);
                    return;

                case RoomType.Kitchen:
                    ChangeMaterialColor(ClosedColor);
                    return;

                case RoomType.Medical:
                    ChangeMaterialColor(RoomConnectionColor);
                    return;

                case RoomType.Office:
                    ChangeMaterialColor(CorridorEdgeColor);
                    return;

                case RoomType.CORRIDOR:
                    ChangeMaterialColor(PathColor);
                    return;

                case RoomType.NONE:
                    ChangeMaterialColor(RoomColor);
                    return;

                default:
                    break;

            }
        }
    }

    

    public void ChangeMaterialColor(Color toColor)
    {
        if (meshType != null)
        {
            meshType.material.color = Color.Lerp(meshType.material.color, toColor, colorSmooth * Time.deltaTime);
        }
    }
    
    public void ResetHeuristics()
    {
        this.g = 0f;
        this.f = 0f;
        this.h = 0f;
    }

    public void ResetBlockLink()
    {
        parent = null;
    }
}
