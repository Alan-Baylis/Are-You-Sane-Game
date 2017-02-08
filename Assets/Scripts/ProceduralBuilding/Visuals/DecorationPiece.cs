using UnityEngine;
using System.Collections.Generic;
using System;

public enum BlockType
{
    Open = 0,
    Stairs = 1,
    UTurn = 2,
    OneWay = 3,

    CornerOpen = 4,
    CornerPillar = 5,

    TjuncX0 = 6,
    TjuncX1L = 7,
    TjuncX1R = 8,
    TjuncX2 = 9,

    PillarX1 = 10,
    PillarX2Same = 11,
    PillarX2Split = 12,
    PillarX3 = 13,
    PillarX4 = 14,

    DISABLED = 15
}

[System.Serializable]
public class DecorateEdge
{
    public int EdgeOrderedIndex;
    public int[] wallsRequired;
}



public class DecorationPiece : MonoBehaviour
{

    public string EditorMessage;

    //public BlockType m_blockType;

    /// <summary>
    /// This works the same way as the door index only that we can have multipl side which require edges to be connected on a piece
    /// </summary>
    public DecorateEdge[] EdgesRquiredOnOrdered;

    //public bool edgeRequired;

    
    //public int[] EdgesRquiredOnOrdered;


    public bool doorConnection;


    public int doorIndexOnOrdered;


    /// <summary>
    /// Always state the maximum length but you can put indicies to 0 if you dont want a decoration in that direction
    /// </summary>
    public int[] OrderedRequirements;

    //Put the Type of Decoration Enum here so we can cross reference check and predicate find all of type in main script


    // Use this for initialization
    void Start ()
    {
	    
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}
