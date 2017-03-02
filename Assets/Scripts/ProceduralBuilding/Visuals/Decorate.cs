using UnityEngine;
using System;
using System.Collections.Generic;

public struct Decoration4W
{
    public float rotation;
    public DecorationPiece piece;

    public Decoration4W(DecorationPiece piece, float rotation)
    {
        this.piece = piece;
        this.rotation = rotation;
    }
}

public struct OrientD
{
    public int orderedIndex;
    public int directionAount;
    public DecorateEdge edgeProperties;

    public OrientD(int orderedIndex, int directionAount, DecorateEdge edgeProperties)
    {
        this.orderedIndex = orderedIndex;
        this.directionAount = directionAount;
        this.edgeProperties = edgeProperties;
    }
}

public static class Decorate
{
    private static void DecorateDirection(int direction, int amount, BlockPiece node)
    {
        BlockPiece d_node = node;
        for (int i = 0; i < amount; i++)
        {
            d_node.isDecordated = true;
            if (d_node.Neighbours[direction] != null)
            {
                d_node = d_node.Neighbours[direction].GetComponent<BlockPiece>();
            }
            else
            {
                Debug.LogError("No Node in Direction! Check Decoration");
                return;
            }
        }
    }

    private static int spaceInNeighborDirection(int neighborDirection, BlockPiece node, Predicate<BlockPiece> stopSearch)
    {
        int freeSpace = 0;
        GameObject neighbor = node.Neighbours[neighborDirection];

        while (neighbor != null)
        {
            BlockPiece neighborNode = neighbor.GetComponent<BlockPiece>();
            if (stopSearch.Invoke(neighborNode))
                break;

            freeSpace++;
            neighbor = neighborNode.Neighbours[neighborDirection];
        }

        return freeSpace;
    }

    private static bool OrientationMatch(List<OrientD> orientations, BlockPiece node, int moduloAddition)
    {
        bool[] freeSpaceWays = new bool[orientations.Count];
        int modulusFromRotation = Mathf.RoundToInt(node.eulerMeshAngle / 90f);
        modulusFromRotation += moduloAddition; // We are checking additional rotation beyond the preliminary modulus set by default rotation

        for (int i = 0; i < orientations.Count; i++)
        {
            if (orientations[i].edgeProperties != null) // Use the null factor as the bool to see if we need edges or not
            {
                // If we are narrow then we are not going to pad the walls heavily like open nodes
                freeSpaceWays[i] = orientations[i].directionAount <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[(orientations[i].orderedIndex + moduloAddition % 4)], node, n => (n.isRoom != node.isRoom) || n.isDecordated || !MatchWallRequirements(orientations[i].edgeProperties, n, modulusFromRotation));
            }
            else
            {
                freeSpaceWays[i] = orientations[i].directionAount <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[(orientations[i].orderedIndex + moduloAddition % 4)], node, n => (n.isRoom != node.isRoom) || n.isDecordated || (n.isRoomConnection || n.isCorridorConnection));
            }
        }

        return freeSpaceWays.AreAllTheSame();
    }

    private static bool AttemptToFitPiece(OrientD orientation, BlockPiece node, int moduloAddition)
    {
        if (orientation.edgeProperties != null)
        {
            int modulusAddition = Mathf.RoundToInt(node.eulerMeshAngle / 90f);
            return orientation.directionAount <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[(orientation.orderedIndex + moduloAddition % 4)], node, n => (n.isRoom != node.isRoom) || n.isDecordated || !MatchWallRequirements(orientation.edgeProperties, n, modulusAddition));
        }
        else
        {
            return orientation.directionAount <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[(orientation.orderedIndex + moduloAddition % 4)], node, n => (n.isRoom != node.isRoom) || n.isDecordated || (n.isRoomConnection || n.isCorridorConnection));
        }
    }

    private static bool AttemptToFitPiece(DecorationPiece decoration, BlockPiece node, int direction)
    {
        if (decoration.EdgesRquiredOnOrdered.Length > 0)
        {
            int modulusAddition = Mathf.RoundToInt(node.eulerMeshAngle / 90f);
            for (int e = 0; e < decoration.EdgesRquiredOnOrdered.Length; e++)
            {
                if (decoration.EdgesRquiredOnOrdered[e].EdgeOrderedIndex == direction)
                {
                    return decoration.OrderedRequirements[direction] <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[direction], node, n => !n.isRoom || n.isDecordated || !MatchWallRequirements(decoration.EdgesRquiredOnOrdered[e], n, modulusAddition));
                }
            }
        }

        return decoration.OrderedRequirements[direction] <= spaceInNeighborDirection(node.OrderedAcceptedIndicies[direction], node, n => !n.isRoom || n.isDecordated || n.isRoomConnection);
    }

    private static OrientD AqquireOrientFromEdgeMatch(DecorationPiece decoration, int orderedIndex)
    {
        if (decoration.EdgesRquiredOnOrdered.Length > 0)
        {
            for (int e = 0; e < decoration.EdgesRquiredOnOrdered.Length; e++)
            {
                if (decoration.EdgesRquiredOnOrdered[e].EdgeOrderedIndex == orderedIndex)
                    return new OrientD(orderedIndex, decoration.OrderedRequirements[orderedIndex], decoration.EdgesRquiredOnOrdered[e]);
            }
        }

        return new OrientD(orderedIndex, decoration.OrderedRequirements[orderedIndex], null);
    }

    private static bool MatchWallRequirements(DecorateEdge directionRequirements, BlockPiece node, int modulusAddition)
    {
        int[] wallRequiredArray = new int[directionRequirements.wallsRequired.Length];
        for (int i = 0; i < wallRequiredArray.Length; i++)
        {
            wallRequiredArray[i] = (directionRequirements.wallsRequired[i] + modulusAddition % 4);
        }

        return wallRequiredArray.ContainsAll(node.WallEdgeIndicies);
        //return node.wallEdgeIndicies.ContainsAll(directionRequirements.wallsRequired);
    }


    public static float GetPillarX4DoorRotation(DecorationPiece decoration, BlockPiece node)
    {
        for (int i = 0; i < 4; i++)
        {
            if (node.RoomConnectionIndex == node.OrderedAcceptedIndicies[((decoration.doorIndexOnOrdered + i) % 4)])
            {
                return (i * 90f);
            }
        }

        return 0.0f;
    }

    public static List<Decoration4W> W4Rotations(IEnumerable<DecorationPiece> decorationPieces, BlockPiece node)
    {
        var listReturn = new List<Decoration4W>();
        foreach (DecorationPiece decoration in decorationPieces)
        {
            List<OrientD> orientations = new List<OrientD>();
            for (int i = 0; i < decoration.OrderedRequirements.Length; i++)
            {
                if (decoration.OrderedRequirements[i] > 0)
                {
                    orientations.Add(AqquireOrientFromEdgeMatch(decoration, i));
                }
            }

            for (int m = 0; m < 4; m++)
            {
                if (OrientationMatch(orientations, node, m))
                {
                    listReturn.Add(new Decoration4W(decoration, m * 90f));
                }
            }
        }

        return listReturn;
    }

    public static bool DecorationMultiDoorConnection(DecorationPiece decoration, BlockPiece node)
    {
        return decoration.doorConnection && (node.RoomConnectionIndex == node.OrderedAcceptedIndicies[decoration.doorIndexOnOrdered]);
    }

    public static DecorationCollection GetBlockTypeCollection(DecorationCollection[] collections, BlockPiece node)
    {
        for (int i = 0; i < collections.Length; i++)
        {
            if (collections[i].BlockTypeCollection == node.nodeType)
            {
                return collections[i];
            }
        }

        return null;
    }

    public static void RepopulateDecorationList(this List<DecorationPiece> list, DecorationCollection collection)
    {
        list.Clear();
        if (collection != null)
        {
            if (collection.Storages != null)
            {
                if (collection.Storages.Length > 0)
                {
                    for (int i = 0; i < collection.Storages.Length; i++)
                    {
                        if (collection.Storages[i].pieces.Length > 0)
                        {
                            list.AddRange(collection.Storages[i].pieces);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Storage has no length: " + collection.Storages.ToString());
                }
            }
            else
            {
                Debug.LogError("No Door Storge attached in the inspector for " + collection.name);
            }
        }
        else
        {
            Debug.LogError("Collection NULL Reference for List");
        }
       
    }

    public static void PopulateDecorationListDoors(this List<DecorationPiece> list, DecorationCollection collection)
    {
        list.Clear();
        if (collection.DoorStorage != null)
        {
            if (collection.DoorStorage.pieces.Length > 0)
            {
                list.AddRange(collection.DoorStorage.pieces);
            }
            else
            {
                Debug.LogWarning("No Door Pieces were added from storage: " + collection.DoorStorage.name);
            }
        }
        else
        {
            Debug.LogError("No Door Storge attached in the inspector for " + collection.name);
        }
    }

    public static bool DecorationCanFit(DecorationPiece decoration, BlockPiece node)
    {
        bool[] freeSpaceWays = new bool[decoration.OrderedRequirements.Length];

        for (int i = 0; i < decoration.OrderedRequirements.Length; i++)
        {
            freeSpaceWays[i] = AttemptToFitPiece(decoration, node, i);
        }

        return freeSpaceWays.AreAllTheSame();
    }

    public static void SetDecorative(DecorationPiece decoration, BlockPiece node)
    {
        for (int i = 0; i < decoration.OrderedRequirements.Length; i++)
        {
            DecorateDirection(node.OrderedAcceptedIndicies[i], decoration.OrderedRequirements[i], node);
        }

        node.SetDecoration(decoration);
    }

    public static void SetDecorative(Decoration4W decoration, BlockPiece node)
    {
        for (int i = 0; i < decoration.piece.OrderedRequirements.Length; i++)
        {
            DecorateDirection(node.OrderedAcceptedIndicies[i], decoration.piece.OrderedRequirements[i], node);
        }

        node.SetDecoration(decoration.piece, decoration.rotation);
    }
}
