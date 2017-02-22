using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class BlockPieceWaypoint
{
    public BlockPiece node = null;
    public float previousF = 0.0f;
    public BlockPieceWaypoint()
    {
        node = null;
        previousF = 0.0f;
    }
}

public class Pathfinder : MonoBehaviour
{

    public List<GameObject> combinedPathNodes;

    public List<GameObject> pathNodes;
    public List<GameObject> openedNodes;
    public List<GameObject> closedNodes;
    private GameObject onBlock;
    private BlockPiece onNode;

    // Use this for initialization
    void Start ()
    {
        pathNodes = new List<GameObject>();
        openedNodes = new List<GameObject>();
        closedNodes = new List<GameObject>();
	}

    public void CombineLocalPath()
    {
        combinedPathNodes.AddRange(pathNodes);
    }

    public void SetOnNode(GameObject node)
    {
        onBlock = node;
        onNode = node.GetComponent<BlockPiece>();
    }

    public void SetOnNode(BlockPiece node)
    {
        onBlock = node.gameObject;
        onNode = node;
    }

    public GameObject GetOnNode()
    {
        return onBlock;
    }

    private void ScoreNeighbor(BlockPiece lookingAt, BlockPiece neighbor, GameObject goalNode, float addedG)
    {
        neighbor.g = lookingAt.g + addedG;

        // Set the H (Heuristic)
        float max = Mathf.Max(Mathf.Abs(goalNode.transform.position.x - neighbor.gameObject.transform.position.x),
                                Mathf.Abs(goalNode.transform.position.z - neighbor.gameObject.transform.position.z));

        float min = Mathf.Min(Mathf.Abs(goalNode.transform.position.x - neighbor.gameObject.transform.position.x),
                                Mathf.Abs(goalNode.transform.position.z - neighbor.gameObject.transform.position.z));

        // This is the octile heuristic ( max(dx, dy) * min(dx, dy) as we are working with 1 by 1 g costs)
        // If we are working with diagonals we would use something like ~( max(dx, dy) + 0.41 * min(dx, dy) assuming the diagonal cost is 1.41 )
        neighbor.h = max * min;

        // F = G + H     
        neighbor.f = neighbor.g + neighbor.h; 
    }

    private GameObject GetBestNode()
    {
        GameObject bestNode = null;

        // Check our list is no broken and assign a gameObject to the first one from the list
        if (openedNodes != null)
        {
            if (openedNodes.Count > 0)
            {
                bestNode = openedNodes[0];
            }
        }
        
        // cycle through all nodes opened re-assigning the best node upon a lower estimated distance
        for (int i = 1; i < openedNodes.Count; i++)
        {
            BlockPiece n1 = openedNodes[i].GetComponent<BlockPiece>();
            BlockPiece n2 = bestNode.GetComponent<BlockPiece>();
            if (n1.f < n2.f)
            {
                bestNode = n1.gameObject;
            }
        }

        // Return the object with the lowest estimated distance based from it's heuristics
        return bestNode;
    }

    private GameObject GetBestNode(IEnumerable<BlockPiece> queryNodes, bool includeRooms)
    {
        BlockPieceWaypoint waypoint = new BlockPieceWaypoint(); // We use a private class to store the previous values of F caluclated from the distance heuristic
        foreach (BlockPiece node in queryNodes) // This will get us the closest Node using the search and not distance
        {
            if (GetPathOnFloorRoomTraversal(node.gameObject, includeRooms, false)) // Avoid recusive function and imply this searches the same floor without combining the path
            {
                if (waypoint.node == null)
                {
                    waypoint.node = node;
                    waypoint.previousF = node.f;
                    continue;
                }
                else
                {
                    if (node.f < waypoint.previousF)
                    {
                        waypoint.node = node;
                        waypoint.previousF = node.f;
                    }
                }
            }
        }

        return waypoint.node.gameObject;
    }

    /// <summary>
    /// Adds a range into the current path list and reverses the current list
    /// </summary>
    /// <param name="goalNode"></param>
    private void FillPathList(GameObject goalNode, bool combinePath)
    {
        GameObject nodeObject = goalNode;
        BlockPiece node = nodeObject.GetComponent<BlockPiece>();
        pathNodes.Add(nodeObject);

        // Loop back through all the parented nodes from the goal
        while (nodeObject != onBlock)
        {
            // Change the object to it's object's parent
            nodeObject = node.parent;

            // Re-apply and re-take the node component
            node = nodeObject.GetComponent<BlockPiece>();

            if (nodeObject != null)
            {
                node.isSearchClosed = false;
                node.isSearchPath = true;
                closedNodes.Remove(nodeObject);
                pathNodes.Add(nodeObject);
            }
        }

        // This will add the list in reverse therefore we must flip it
        pathNodes.Reverse();

        if (combinePath)
            combinedPathNodes.AddRange(pathNodes);
    }

    public void ResetLists(bool combinedReset)
    {
        // reset node statistics in lists for future use
        foreach (GameObject go in pathNodes)
        {
            go.GetComponent<BlockPiece>().isSearchPath = false;
            go.GetComponent<BlockPiece>().ResetHeuristics();
            //go.GetComponent<BlockPiece>().ResetBlockLink();
        }
            
        foreach (GameObject go in openedNodes)
        {
            go.GetComponent<BlockPiece>().isSearchOpened = false;
        }
            
        foreach (GameObject go in closedNodes)
        {
            go.GetComponent<BlockPiece>().isSearchClosed = false;
        }
        
        // Clear all old lists
        pathNodes.Clear(); 
        openedNodes.Clear();
        closedNodes.Clear();

        if (combinedReset)
            combinedPathNodes.Clear();
    }


    public List<GameObject> GetPathPartition()
    {
        return pathNodes;
    }

    // Overloaded for the gameObject property from the Blockpiece monobehaviour
    public bool GetPath(BlockPiece goalNode) { return GetPath(goalNode.gameObject); }
    public bool GetPath(GameObject goalNode)
    {
        if (goalNode == null)
        {
            Debug.LogWarning("Attempted to reach goal node that doesnt exist!");
            return false;
        }

        ResetLists(false);
        int cycles = 0;
        BlockPiece lookingAt = onBlock.GetComponent<BlockPiece>();
        closedNodes.Add(lookingAt.gameObject);
        lookingAt.isSearchClosed = true;
        lookingAt.g = 0;

        while (lookingAt.gameObject != goalNode && openedNodes.Count >= 0)
        {
            for (int i = 0; i < lookingAt.neighbors.Length; i++)
            {
                BlockPiece neighbor = null;
                if (lookingAt.neighbors[i] != null)
                {
                    // Check if this node exists, since there could be empty slots in the neighbors array due to being out of bounds
                    // Check if it is on the closed list, if not then continue process
                    // TO NOTE: following overloaded methods will implement their additional checks here
                    neighbor = lookingAt.neighbors[i].GetComponent<BlockPiece>();
                    if (neighbor.isWalkable && !neighbor.isSearchClosed && !neighbor.isStairNode)
                    {
                        
                        // Determine the added G based on directional relationship 
                        // ADD-ON: DIAGONALS make addedG = 0 then is set by if statement below corresponsidng to the index of the neighbor (diagonals will cost more 1.41)
                        // We are only moving Horizontal and Vertical therefore addedG will always be 1 when moved in any given direction
                        float addedG = 1;
                        if (!neighbor.isSearchOpened)
                        {
                            // All nodes looked at will be placed in the opened list
                            ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            openedNodes.Add(neighbor.gameObject);
                            neighbor.isSearchOpened = true;
                            neighbor.parent = lookingAt.gameObject;
                        }
                        else
                        {
                            // Couple and link the nodes based on G cost
                            // Check if moving to this node from the current node has a lower G cost than its current G cost, 
                            // if so change the G and F costs accordingly
                            if (lookingAt.g + addedG < neighbor.g)
                            {
                                neighbor.parent = lookingAt.gameObject;
                                ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            }
                        }
                    }
                }
            }

            // All neighbors have been scored and coupled as needed (still inside loop here)
            // Next step is to find the best node to move to
            // Assign the new "current" node to look at
            // Drop and select nodes from the opened list and place them in the closed list
            GameObject nextInPath = GetBestNode();
            if (nextInPath != null && !closedNodes.Contains(nextInPath))
            {
                nextInPath.GetComponent<BlockPiece>().isSearchOpened = false;
                nextInPath.GetComponent<BlockPiece>().isSearchClosed = true;
                openedNodes.Remove(nextInPath);
                closedNodes.Add(nextInPath);

                // This will eventually place the goal node
                lookingAt = nextInPath.GetComponent<BlockPiece>();
            }

            cycles++;
            if (cycles > 1500)
            {
                Debug.Log("Starting Search: + " + onBlock.ToString() + " Floor: [" + onBlock.GetComponent<BlockPiece>().GetY() + "] ~ Cannot Find::");
                Debug.Log(goalNode.ToString() + " Floor: [" + goalNode.GetComponent<BlockPiece>().GetY() + "]");
                Debug.Log("Could not find a path in time.");
                return false;
            }
        }
        ////////// END OF WHILE LOOP

        // If the closed nodes list doesnt contain the goal node then something has gone wrong and no paths can be found
        if (!closedNodes.Contains(goalNode))
        {
            foreach (GameObject go in closedNodes)
            {
                go.GetComponent<BlockPiece>().isSearchClosed = false;
            }

            closedNodes.Clear();
            return false;
        }
        else
        {
            // Trace the path nodes based on parents from the goal node to the start node
            FillPathList(goalNode, false);
            return true;
        }
    }


    // For a clean path make the exclusive search true
    public bool GetPath(GameObject goalNode, Predicate<BlockPiece> exlusiveSearch)
    {
        if (goalNode == null)
        {
            Debug.LogWarning("Attempted to reach goal node that doesnt exist!");
            return false;
        }

        ResetLists(false);
        int cycles = 0;
        BlockPiece lookingAt = onBlock.GetComponent<BlockPiece>();
        closedNodes.Add(lookingAt.gameObject);
        lookingAt.isSearchClosed = true;
        lookingAt.g = 0;

        while (lookingAt.gameObject != goalNode && openedNodes.Count >= 0)
        {
            for (int i = 0; i < lookingAt.neighbors.Length; i++)
            {
                BlockPiece neighbor = null;
                if (lookingAt.neighbors[i] != null)
                {
                    neighbor = lookingAt.neighbors[i].GetComponent<BlockPiece>();
                    if (neighbor.isWalkable && !neighbor.isSearchClosed && !neighbor.isStairNode && exlusiveSearch.Invoke(neighbor))
                    {
                        float addedG = 1;
                        if (!neighbor.isSearchOpened)
                        {
                            ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            openedNodes.Add(neighbor.gameObject);
                            neighbor.isSearchOpened = true;
                            neighbor.parent = lookingAt.gameObject;
                        }
                        else
                        {
                            if (lookingAt.g + addedG < neighbor.g)
                            {
                                neighbor.parent = lookingAt.gameObject;
                                ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            }
                        }
                    }
                }
            }

            GameObject nextInPath = GetBestNode();
            if (nextInPath != null && !closedNodes.Contains(nextInPath))
            {
                nextInPath.GetComponent<BlockPiece>().isSearchOpened = false;
                nextInPath.GetComponent<BlockPiece>().isSearchClosed = true;
                openedNodes.Remove(nextInPath);
                closedNodes.Add(nextInPath);

                // This will eventually place the goal node
                lookingAt = nextInPath.GetComponent<BlockPiece>();
            }

            cycles++;
            if (cycles > 1500)
            {
                Debug.Log("Starting Search: + " + onBlock.ToString() + " Floor: [" + onBlock.GetComponent<BlockPiece>().GetY() + "] ~ Cannot Find::");
                Debug.Log(goalNode.ToString() + " Floor: [" + goalNode.GetComponent<BlockPiece>().GetY() + "]");
                Debug.Log("Could not find a path in time.");
                return false;
            }
        }
        ////////// END OF WHILE LOOP

        // If the closed nodes list doesnt contain the goal node then something has gone wrong and no paths can be found
        if (!closedNodes.Contains(goalNode))
        {
            foreach (GameObject go in closedNodes)
            {
                go.GetComponent<BlockPiece>().isSearchClosed = false;
            }

            closedNodes.Clear();
            return false;
        }
        else
        {
            // Trace the path nodes based on parents from the goal node to the start node
            FillPathList(goalNode, false);
            return true;
        }
    }

    private bool GetPathOnFloorRoomTraversal(GameObject goalNode, bool includeRooms, bool combinePath)
    {
        if (goalNode == null)
        {
            Debug.LogWarning("Attempted to reach goal node that doesnt exist!");
            return false;
        }

        ResetLists(false);
        BlockPiece lookingAt = onBlock.GetComponent<BlockPiece>();
        int cycles = 0;
        closedNodes.Add(lookingAt.gameObject);
        lookingAt.isSearchClosed = true;
        lookingAt.g = 0;
        bool roomEntered = lookingAt.isRoom; // Are we already in a room?
        Room lastEnteredRoom = (roomEntered) ? lookingAt.thisRoom : null;

        while (lookingAt.gameObject != goalNode && openedNodes.Count >= 0)
        {
            for (int i = 0; i < lookingAt.neighbors.Length; i++) // Remember, this only looks at and scores neighbors, no actions should be done here to change the bias of the search
            {
                BlockPiece neighbor = null;
                if (lookingAt.neighbors[i] != null)
                {
                    neighbor = lookingAt.neighbors[i].GetComponent<BlockPiece>();
                    if (neighbor.isWalkable && !neighbor.isSearchClosed && !neighbor.isStairNode)
                    {
                        if (includeRooms)
                        {
                            if (!roomEntered)
                            {
                                if (!neighbor.isCorridor && !neighbor.isRoomConnection)
                                    continue;
                            }
                            else
                            {
                                if (!neighbor.isRoom && !neighbor.isCorridorConnection)
                                {
                                    if (neighbor.thisRoom != lastEnteredRoom) // Possible this can be only check in if statement or check room name here - careful of null reference for name check
                                        continue;
                                }
                            }
                        }
                        else
                        {
                            // The neighbor must be a corridor
                            if (!neighbor.isCorridor)
                                continue;
                        }

                        // If we got to this point then we can score this neighbor
                        // If we count the node then do this { BELOW } : -------------------------------
                        float addedG = 1; // possibly make this a constant value for the pathfinder but remember this changes for other A* searches if we count diagonals to 1.41 LARGE FUTURE IMPROVEMENT
                        if (!neighbor.isSearchOpened)
                        {
                            ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            openedNodes.Add(neighbor.gameObject);
                            neighbor.isSearchOpened = true;
                            neighbor.parent = lookingAt.gameObject;
                        }
                        else
                        {
                            if (lookingAt.g + addedG < neighbor.g)
                            {
                                neighbor.parent = lookingAt.gameObject;
                                ScoreNeighbor(lookingAt, neighbor, goalNode, addedG);
                            }
                        }
                    }
                }
            }

            GameObject nextInPath = GetBestNode(); // Get the best node of the neighbors we have searched for this node
            if (nextInPath != null && !closedNodes.Contains(nextInPath))
            {
                nextInPath.GetComponent<BlockPiece>().isSearchOpened = false;
                nextInPath.GetComponent<BlockPiece>().isSearchClosed = true;
                openedNodes.Remove(nextInPath);
                closedNodes.Add(nextInPath);

                // This will eventually place the goal node
                lookingAt = nextInPath.GetComponent<BlockPiece>();

                if (includeRooms)
                {
                    if (lookingAt.isRoomConnection) // Room traversal logic here
                    {
                        if (!roomEntered)
                        {
                            roomEntered = true;
                            lastEnteredRoom = lookingAt.thisRoom;
                        }
                    }
                    else if (lookingAt.isCorridorConnection)
                    {
                        if (roomEntered)
                        {
                            roomEntered = false;
                            lastEnteredRoom = null;
                        }
                    }
                }
            }

            cycles++;
            if (cycles > 1500)
            {
                Debug.Log("Starting Search: + " + onBlock.ToString() + " Floor: [" + onBlock.GetComponent<BlockPiece>().GetY() + "] ~ Cannot Find::");
                Debug.Log(goalNode.ToString() + " Floor: [" + goalNode.GetComponent<BlockPiece>().GetY() + "]");
                Debug.Log("Could not find a path in time.");
                return false;
            }
        }
        ////////// END OF WHILE LOOP

        // If the closed nodes list doesnt contain the goal node then something has gone wrong and no paths can be found
        if (!closedNodes.Contains(goalNode))
        {
            foreach (GameObject go in closedNodes)
            {
                go.GetComponent<BlockPiece>().isSearchClosed = false;
            }

            closedNodes.Clear();
            return false;
        }
        else
        {
            // Trace the path nodes based on parents from the goal node to the start node
            FillPathList(goalNode, combinePath);            
            return true;
        }
    }

    public bool GetPathFullTraversal(GameObject goalNode, bool includeRooms)
    {
        if (goalNode == null)
        {
            Debug.LogWarning("Attempted to reach goal node that doesnt exist!");
            return false;
        }

        if (goalNode.GetComponent<BlockPiece>().isStairNode)
        {
            if (onNode.GetY() != goalNode.GetComponent<BlockPiece>().GetY()) // We are looking down the stairs
            {
                goalNode = goalNode.GetComponent<BlockPiece>().parent;      
            }
            else // We are looking up the stairs
            {
                goalNode = goalNode.GetComponent<BlockPiece>().parent.GetComponent<BlockPiece>().nextFloorParent.gameObject;
            }
        }

        ResetLists(true); // Reset the Combined path here aswell
        BlockPiece lookingAt = onBlock.GetComponent<BlockPiece>(); // Store this so we have a reference to the starting onNode when the search begun
        BlockPiece lookingGoal = goalNode.GetComponent<BlockPiece>(); // Store this so we always know the building Y value of the goal node
        int y = onNode.GetY(); // Create an integer to increment the Y iterations 
        
        if (y != lookingGoal.GetY()) // This funciton is recursive for call which do not have this condition
        {
            // Partitition the path searching for each floor different to traverse to the nearest floor connection and continue pathing below - simple :)
            // Remember the "else" statement applies for each individual search and addition to the overall pathlist (Check for room searches)
            FloorLevel floor;

            if (lookingGoal.GetY() < y)
            {
                while(lookingGoal.GetY() < y)
                {
                    floor = onNode.attachedFloor;
                    // Possibly need to split up the per floor traversal
                    GameObject bestFloorGoal = GetBestNode(floor.routeBlocks.FindAll(n => n.isDoorNode), includeRooms); // Search the current floor for the nearest exit traversing as stated
                    if (GetPathOnFloorRoomTraversal(bestFloorGoal, includeRooms, true))
                    {
                        SetOnNode(bestFloorGoal.GetComponent<BlockPiece>().previousFloorParent); // This should result in the onNode being set on the same floor
                        y--;
                    }
                }

                bool getReturn = GetPathOnFloorRoomTraversal(goalNode, includeRooms, true);
                SetOnNode(lookingAt); // Reset the on node to the one that initialized the search
                return getReturn;

            }
            else
            {
                while(y < lookingGoal.GetY())
                {
                    floor = onNode.attachedFloor;
                    // Possibly need to split up the per floor traversal
                    GameObject bestFloorGoal = GetBestNode(floor.routeBlocks.FindAll(n => n.isStairConnection), includeRooms); // Search the current floor for the nearest exit traversing as stated
                    if (GetPathOnFloorRoomTraversal(bestFloorGoal, includeRooms, true))
                    {
                        SetOnNode(bestFloorGoal.GetComponent<BlockPiece>().nextFloorParent);
                        y++;
                    }                    
                }

                bool getReturn = GetPathOnFloorRoomTraversal(goalNode, includeRooms, true);
                SetOnNode(lookingAt); // Reset the on node to the one that initialized the search
                return getReturn;
            }
        }
        else
        {
            // We can do an ordinary search but we must take into consideration if the search can path through rooms or not
            return GetPathOnFloorRoomTraversal(goalNode, includeRooms, true);
        }
    }








}


