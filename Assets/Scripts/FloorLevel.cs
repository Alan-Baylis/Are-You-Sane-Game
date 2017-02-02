using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;


public enum FloorNodeType
{
    All = 0,
    Stairs = 1,
    StairConnections = 2,
    Doors = 3,
    Unwalkable = 4,
    Fixed = 5,
    Route = 6
}

/// <summary>
/// Container to manage the dimensions of a new floor
/// </summary>
public class FloorRebuilder
{
    private int m_XMin;
    private int m_XMax;
    private int m_ZMin;
    private int m_ZMax;

    public int Xminimum { get { return m_XMin; } }
    public int Xmaximum { get { return m_XMax; } }
    public int Zminimum { get { return m_ZMin; } }
    public int Zmaximum { get { return m_ZMax; } }

    /// <summary>
    /// Build a floor framework
    /// </summary>
    /// <param name="virtuals"></param>
    /// <param name="nodes"></param>
    public FloorRebuilder(IEnumerable<NodeVector2> virtuals, IEnumerable<BlockPiece> nodes)
    {
        ClampToNodes(nodes);
        ClampFloorVirtuals(virtuals);
    }

    public void SetFloorOverlap(FloorLevel level)
    {
        if (m_XMin > level.EndX - 1) m_XMin = level.EndX;
        if (m_ZMin > level.EndZ - 1) m_ZMin = level.EndZ;
        if (m_XMax < level.StartX + 1) m_XMax = level.StartX;
        if (m_ZMax < level.StartZ + 1) m_ZMax = level.StartZ;
    }    

    private void ClampValues(int x, int z)
    {
        if (x > m_XMax)
        {
            m_XMax = x;
        }
        else
        {
            if (x < m_XMin)
                m_XMin = x;
        }

        if (z > m_ZMax)
        {
            m_ZMax = z;
        }
        else
        {
            if (z < m_ZMin)
                m_ZMin = z;
        }
    }

    private void ClampToNodes(IEnumerable<BlockPiece> nodes)
    {
        foreach (BlockPiece node in nodes)
        {
            ClampValues(node.GetX(), node.GetZ());
        }
    }

    private void ClampFloorVirtuals(IEnumerable<NodeVector2> virtuals)
    {
        foreach (NodeVector2 node in virtuals)
        {
            ClampValues(node.x, node.z);
        }
    }
};

public class FloorLevel : MonoBehaviour
{
    
    public int overlappingBelowCount = 0;

    public bool isVisited = false;
    private int floorNumber = 0;
    private int floorX = 0;
    private int floorZ = 0;
    private int m_totalBlocks = 0;
    public GameObject startBlock;

    public BlockPiece[,] floorNodes;

    public List<BlockPiece> floorBlocks             = new List<BlockPiece>();
    public List<BlockPiece> stairBlocks             = new List<BlockPiece>();
    public List<BlockPiece> stairConnectionBlocks   = new List<BlockPiece>();
    public List<BlockPiece> doorBlocks              = new List<BlockPiece>();
    public List<BlockPiece> unWalkableBlocks        = new List<BlockPiece>();
    public List<BlockPiece> fixedNodes              = new List<BlockPiece>();
    public List<BlockPiece> routeBlocks             = new List<BlockPiece>();

    public List<BlockPiece> m_LightNodes            = new List<BlockPiece>();


    public List<Room> AllRooms = new List<Room>();

    public List<NodeVector2> virtualUnwalkables = new List<NodeVector2>();

    private int xMin = 0;
    private int xMax = 0;
    private int zMin = 0;
    private int zMax = 0;

    public float incX;
    public float incZ;

    public void CreateLights(GameObject prefab)
    {
        m_LightNodes.Clear();
        foreach(BlockPiece node in floorBlocks)
        {
            if (!node.isStairNode && node.isWalkable)
            {
                if (node.Light != null)
                {
                    m_LightNodes.Add(node);
                }
                else
                {
                    int chance = (node.isCorridor) ? 4 : 3;
                    int random = UnityEngine.Random.Range(1, 11);
                    if (chance >= random)
                    {
                        GameObject spawnLight = Instantiate(prefab, node.transform.position, Quaternion.identity) as GameObject;
                        spawnLight.transform.SetParent(node.transform);
                        spawnLight.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        spawnLight.transform.localPosition = new Vector3(0f, 2.935f, 0f);

                        node.Light = spawnLight.GetComponent<LightBehaviour>();
                        m_LightNodes.Add(node);
                    }
                }
            }
        }
    }



    public void RemoveConsecutiveStairs()
    {
        foreach (BlockPiece node in stairConnectionBlocks) { node.isDoorNode = false; }
    }

    public FloorRebuilder GetFloorFrameWork(params FloorNodeType[] types)
    {
        return new FloorRebuilder(virtualUnwalkables, CopyNodesOfType(types));
    }

    private List<BlockPiece> GetNodesOfType(FloorNodeType type)
    {
        switch(type)
        {
            case FloorNodeType.All:
                return floorBlocks;

            case FloorNodeType.Doors:
                return doorBlocks;

            case FloorNodeType.Fixed:
                return fixedNodes;

            case FloorNodeType.Route:
                return routeBlocks;

            case FloorNodeType.StairConnections:
                return stairConnectionBlocks;

            case FloorNodeType.Stairs:
                return stairBlocks;

            case FloorNodeType.Unwalkable:
                return unWalkableBlocks;

            default:
                return null;
        }
    }


    private List<BlockPiece> CopyNodesOfType(params FloorNodeType[] types)
    {
        List<BlockPiece> copy = new List<BlockPiece>();
        if (types != null)
        {
            for (int i = 0; i < types.Length; i++)
            {
                foreach (BlockPiece node in GetNodesOfType(types[i]))
                {
                    if (!copy.Contains(node))
                        copy.Add(node);
                }
            }
        }

        copy.RemoveAll(node => node == null);
        return copy;
    }


    public void ClearNodesOfType(params FloorNodeType[] types)
    {
        if (types != null)
        {
            for (int i = 0; i < types.Length; i++)
            {
                foreach (BlockPiece node in GetNodesOfType(types[i]))
                {
                    node.RemoveDecoration();
                    node.ClearAcceptedIndicies();
                }
            }
        }        
    } 

    public bool VirtualCoordsExist(int x, int z)
    {
        foreach(NodeVector2 coords in virtualUnwalkables)
        {
            if (coords.x == x && coords.z == z)
                return true;
        }

        return false;
    }

    public BlockPiece GetNode(int x, int z)
    {
        foreach (BlockPiece node in floorBlocks)
        {
            if (node.GetX() == x && node.GetZ() == z)
            {
                return node;
            }
        }

        return null;
    }


    public void SetDimensions(int startX, int startZ, int width, int depth)
    {
        xMin = startX;
        zMin = startZ;
        xMax = xMin + width;
        zMax = zMin + depth;
        Width = width;
        Depth = depth;
        floorNodes = new BlockPiece[width, depth];
    }

    public BlockPiece GetArrayNode(int x, int z)
    {
        return floorNodes[x, z];
    }

    public bool isEdgeNode(int x, int z)
    {
        return (x == xMin || x == xMax - 1 || z == zMin || z == zMax - 1);
    }

    public bool isCornerNode(int x, int z)
    {
        return (x == xMin && z == zMin || x == xMin && z == zMax - 1 || z == zMin && x == xMax - 1 || x == xMax - 1 && z == zMax - 1);
    }

    public int StartX
    {
        get { return xMin; }
        set { xMin = value; }
    }

    public int EndX
    {
        get { return xMax; }
        set { xMax = value; }
    }

    public int StartZ
    {
        get { return zMin; }
        set { zMin = value; }
    }

    public int EndZ
    {
        get { return zMax; }
        set { zMax = value; }
    }

    public int FloorNumber
    {
        get { return floorNumber; }
        set { floorNumber = value; }
    }

    public int Width
    {
        get { return floorX; }
        set
        {
            floorX = value;
            m_totalBlocks = floorX * floorZ;
        }
    }

    public int Depth
    {
        get { return floorZ; }
        set
        {
            floorZ = value;
            m_totalBlocks = floorX * floorZ;
        }
    }

    public int totalBlocks
    {
        get { return m_totalBlocks; }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == GameTag.Player)
        {
            other.gameObject.GetComponent<PlayerHeuristics>().CurrentFloor = floorNumber;
            Debug.Log("Player has entered Floor Number: " + floorNumber);
        }
        else if (other.gameObject.tag == GameTag.Annie)
        {
            //other.gameObject.GetComponent<LAObject>().Movement.currentFloor = floorNumber;
            other.gameObject.GetComponent<LAObject>().Movement.SetPatrolBlocks(floorNumber); // The current floor for annie will change upon the actual node enter
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == GameTag.Player)
        {
            isVisited = true;
        }
    }
    

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
