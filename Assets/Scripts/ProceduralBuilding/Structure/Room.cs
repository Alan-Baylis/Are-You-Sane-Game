using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public enum RoomType
{
    Cell = 0,
    Bathroom = 1,
    Medical = 2,
    Kitchen = 3,
    Office = 4,
    CORRIDOR = 5,
    NONE = 6
}


public class Room
{
    private bool m_hasCorridorConnection = false;
    private BlockPiece[] nodes;
    private RoomType m_scene;
    private string m_roomName;

    public string roomName
    {
        get { return m_roomName; }
    }

    public RoomType scene
    {
        get { return m_scene; }
    }

    public Room(List<BlockPiece> nodes)
    {        
        this.nodes = nodes.ToArray();
        DetermineRoomType();
        InitialConfiguration(); // Must be done after we have a length aqquired
        m_roomName = "Room (" + nodes[0].GetY() + ", " +  nodes[0].attachedFloor.AllRooms.Count + ")";
    }

    public Room (List<GameObject> blocks)
    {
        this.nodes = new BlockPiece[blocks.Count];
        DetermineRoomType();
        for (int i = 0; i < this.nodes.Length; i++)
        {
            this.nodes[i] = blocks[i].GetComponent<BlockPiece>();
            //this.nodes[i].roomBelonging = m_scene;
        }

        InitialConfiguration(); // Must be done after we have a length aqquired
        m_roomName = "Room (" + nodes[0].GetY() + ", " + nodes[0].attachedFloor.AllRooms.Count + ")";


    }

    public void ConfigureRoomProperties()
    {
        DetermineRoomType();
    }

    public void DecorateRoom(RoomDecorationCollections AllRoomCollections)
    {
        // Room Type is already assumed and we have been given default pieces and decorative pieces to select from
        List<BlockPiece> nodeContainer = new List<BlockPiece>();
        nodeContainer.AddRange(nodes); // Revise this search after complete TODO We ARE removing from this list therefore we need a mutation
        List<DecorationPiece> decorationPieces = new List<DecorationPiece>(); // We are not removing this list therefore we dont need a mutation

        int numberToDecorate = nodeContainer.Count / 2; // At the moment decorate HALF the room
        int currentDecorated = 0;

        int safetyCycle = 0;
        while (nodeContainer.Count != 0)
        {
            // Keep track of last found type so we dont have to keep re-added loads of things to the list if we get the same type :) #TODO
            int randomRoomNode = UnityEngine.Random.Range(0, nodeContainer.Count);
            BlockPiece node = nodeContainer[randomRoomNode];

            if (node.nodeType == BlockType.DISABLED)
            {
                nodeContainer.Remove(node);
                continue;
            }

            DecorationCollection collection = Decorate.GetBlockTypeCollection(AllRoomCollections.Collections, node); // This contains all the storages for this particular blocktype
            int moduloAddition = Mathf.RoundToInt(node.eulerMeshAngle / 90f);

            if (collection == null)
            {
                Debug.LogError("Cannot find collection for type: " + node.nodeType);
                
            }

            if (node.isRoomConnection)
            {
                DecorationPiece chosenDoorDefault = null;
                if (node.nodeType == BlockType.PillarX4)
                {
                    chosenDoorDefault = collection.DoorStorage.pieces[0];
                    float rotation = Decorate.GetPillarX4DoorRotation(chosenDoorDefault, node);
                    node.SetDecoration(chosenDoorDefault, rotation);
                }
                else
                {
                    decorationPieces.PopulateDecorationListDoors(collection);
                    if (decorationPieces.Count != 0)
                    {
                        foreach(DecorationPiece decoration in decorationPieces)
                        {
                            if (decoration.doorIndexOnOrdered == ((node.roomConnectionIndex + moduloAddition) % 4))
                            {
                                chosenDoorDefault = decoration;
                                break;
                            }
                        }

                        if (chosenDoorDefault == null)
                        {
                            Debug.LogError("Could not find Door Piece avaliable for collection: " + collection.name + " : Pieces count: " + decorationPieces.Count);
                            node.SetDecoration(collection.nonDecorationPiece);
                        }
                        else
                        {
                            node.SetDecoration(chosenDoorDefault);
                        }
                    }
                    else
                    {
                        Debug.LogError("No pieces in door storage");
                        node.SetDecoration(collection.nonDecorationPiece);
                    }
                }
                
                nodeContainer.Remove(node);
                continue;
            }




            // Expensive Call to add everything - This contains all the storages for this particular blocktype
            decorationPieces.RepopulateDecorationList(collection); // The more sub classes nad inheritance/abstraction we create foreach storage, the less searching and adding has to be done here
            if (currentDecorated < numberToDecorate)
            {
                if (node.nodeType == BlockType.OneWay || node.nodeType == BlockType.PillarX4) // These pieces can be rotated for more customizations
                {
                    List<Decoration4W> decorationsWRotations = Decorate.W4Rotations(decorationPieces, node);                    
                    if (decorationsWRotations.Count != 0) // If there are pieces avaliable to fit here then we will select one
                    {
                        Decorate.SetDecorative(decorationsWRotations[UnityEngine.Random.Range(0, decorationsWRotations.Count)], node);
                        nodeContainer.Remove(node);
                        currentDecorated++;
                    }
                    else
                    {
                        // If there were no pieces avalialbe to fit here then we will default decorate it
                        // normally every piece should have a deocoration node which doesnt take up space so this shouldnt be called... hypothetically... but we may have missed one out
                        node.SetDecoration(collection.nonDecorationPiece);
                        nodeContainer.Remove(node);
                    }
                }
                else
                {
                    // Decorate Piece if place avaliable
                    List<DecorationPiece> piecesToChose = new List<DecorationPiece>();
                    piecesToChose.AddRange(decorationPieces.FindAll(decoration => Decorate.DecorationCanFit(decoration, node))); // Add the range of pieces which match the node's type

                    if (piecesToChose.Count != 0) // If there are pieces avaliable to fit here then we will select one
                    {
                        Decorate.SetDecorative(piecesToChose[UnityEngine.Random.Range(0, piecesToChose.Count)], node);
                        nodeContainer.Remove(node);
                        currentDecorated++;
                    }
                    else
                    {
                        // If there were no pieces avalialbe to fit here then we will default decorate it
                        // normally every piece should have a deocoration node which doesnt take up space so this shouldnt be called... hypothetically... but we may have missed one out
                        node.SetDecoration(collection.nonDecorationPiece);
                        nodeContainer.Remove(node);
                    }

                }

            }
            else
            {
                // Spawn default piece
                // The blocks handle the angle themselves once you give them a piece to set (remember its clever danny)
                node.SetDecoration(collection.nonDecorationPiece);
                nodeContainer.Remove(node);
                // This should work as is.
            }


            safetyCycle++;
            if (safetyCycle > 200)
            {
                Debug.LogError("Room Piece Infinite Loop ERROR");
                break;
            }

        } /////////// END OF WHILE LOOP
    }

    private void DetermineRoomType()
    {
        m_scene = (nodes.Length > 1) ? (RoomType)UnityEngine.Random.Range(1, 5) : RoomType.Cell; // 1 - 5 excludes room type NONE
    }

    private void InitialConfiguration()
    {
        foreach (BlockPiece node in nodes)
        {
            node.roomBelonging = m_scene;
            node.roomNeighbors = new BlockPiece[4];
            node.thisRoom = this;
            
            for (int n = 0; n < node.neighbors.Length; n++)
            {
                if (node.neighbors[n] == null || !node.neighbors[n].GetComponent<BlockPiece>().isRoom) // Either elses of these conditions would imply the node DOES exist
                {
                    // Check whether or not this node is a narrow corridor inside the Room (random shaped rooms)
                    node.isRoomEdge = true;
                    node.roomEdgeIndicies.Add(n);

                    int opp = BuildingGeneration.GetOppositeNeighborIndex(n);
                    if (!node.isRoomNarrow)
                    {
                        if (node.neighbors[opp] == null || !node.neighbors[opp].GetComponent<BlockPiece>().isRoom)
                        {
                            node.isRoomNarrow = true;
                        }
                    }
                }
                else
                {
                    // Set the official room neighbor - it will only be a room node here as it is checked for that above
                    node.roomNeighbors[n] = node.neighbors[n].GetComponent<BlockPiece>();
                }
            }

            // Check for the node being a room corner - could be done with the room is narrow check to apply overall logic of checking node properties at set location in code
            if (node.roomEdgeIndicies.Count > 2)
            {
                node.isRoomEnd = true;
            }
            else if (node.roomEdgeIndicies.Count == 2)
            {
                int delta = Math.Abs(node.roomEdgeIndicies[0] - node.roomEdgeIndicies[1]);
                if (delta == 1 || delta == 3)
                {
                    node.isRoomCorner = true;
                }
            }

        }
    }



}
