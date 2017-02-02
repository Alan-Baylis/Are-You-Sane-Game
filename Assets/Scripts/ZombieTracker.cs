using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ZombieTracker : MonoBehaviour
{
    private const float SamePositionDelay = 1.5f;
    private float samePosTimer = SamePositionDelay;
    private int samePosCounter = 0;
    private int lastNodeIndex = 0;
    private Vector3 myLastPosition = new Vector3();
    private bool m_previouslyInSight = false;



    public SoundResources sounds;

    protected Animator myAnimation;
    public List<GameObject> pathNodes;
    public List<GameObject> openedNodes;
    public List<GameObject> closedNodes;
    Vector3 targetPos = Vector3.zero;
    int nextNodeIndex = 0;
    public GameObject onNode;
    private AudioSource mySpeaker;

    private Vector3 offsetY = new Vector3(0f, -1f, 0f);

    private GameObject playerObj = null;
    private Vector3 lastKnownPosition = new Vector3();
    private BlockPiece lastSeenNode;

    private LayerMask maskLayer;
    public string[] maskLayersString;

    private GameObject[] patrolBlocks;

    public ZombieState state = ZombieState.Idle;

    public void ConfigurePatrolBlocks(List<GameObject> avaliableBlocks)
    {
        patrolBlocks = new GameObject[avaliableBlocks.Count];

        for (int i = 0; i < avaliableBlocks.Count; i++)
        {
            patrolBlocks[i] = avaliableBlocks[i];
        }
    }

    public int GetLevel()
    {
        return onNode.GetComponent<BlockPiece>().GetY();
    }

    private void SelectPatrolPath()
    {
        List<GameObject> avaliableBlocks = new List<GameObject>();
        avaliableBlocks.AddRange(patrolBlocks);

        // If statement for potential of when we are not in a corridor (in a room instead)
        if (avaliableBlocks.Contains(onNode))
        {
            avaliableBlocks.Remove(onNode);
            int randomBlockIndex = Random.Range(0, avaliableBlocks.Count);
            GetPath(avaliableBlocks[randomBlockIndex]);
        }
        else
        {
            Debug.LogWarning("The node we are standing on is not contained in the corridors!");
        }
    }

    public enum ZombieState
    {
        Idle = 0,
        Chasing = 1,
        Patrol = 2,
        Searching = 3,
        Attacking = 4
    }

    private void InitializeRayCastLayers()
    {
        maskLayer = LayerMaskExtensions.Create("Ignore Raycast");
        foreach (string mask in maskLayersString)
        {
            maskLayer = maskLayer.AddToMask(mask);
        }

        maskLayer = maskLayer.Inverse();
    }


    // Use this for initialization
    void Start ()
    {
        myAnimation = GetComponent<Animator>();
        pathNodes = new List<GameObject>();
        openedNodes = new List<GameObject>();
        closedNodes = new List<GameObject>();
        mySpeaker = GetComponent<AudioSource>();
        InitializeRayCastLayers();
        
        myLastPosition = transform.position;
    }

    public void SetOnNode(GameObject node)
    {
        ResetLists();
        onNode = node;
    }

    private void StorePlayer(GameObject player)
    {
        lastSeenNode = player.GetComponent<PlayerHeuristics>().BlockPosition;
        if (playerObj == null)
        {
            playerObj = player;
        }
    }

    public void Startle(GameObject player)
    {
        Debug.Log("Startle!");
        if (state != ZombieState.Chasing && state != ZombieState.Attacking)
        {
            Debug.Log("Something Happened");
            StorePlayer(player);
            //GetPath(player.GetComponent<PlayerHeuristics>().BlockPosition.gameObject);
            SwitchToChase();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Zombie" && !GetComponent<Rigidbody>().isKinematic)
        {
            StartCoroutine(IgnoreCollisionDelay());
        }
    }

    private IEnumerator IgnoreCollisionDelay()
    {
        Debug.Log("Bumped Into Another Zombie");
        GetComponent<Rigidbody>().isKinematic = true;
        yield return new WaitForSeconds(1f);
        GetComponent<Rigidbody>().isKinematic = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // attacking check as there are tirgger sphreres on the attack hands
        if (other.gameObject.tag == "Player")
        {
            OccludeTrackingToggle();  
        }
    }

    void OnTriggerExit(Collider other)
    {
        // attacking check as there are tirgger sphreres on the attack hands
        if (other.gameObject.tag == "Player")
        {
            OccludeTrackingToggle();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            // we may be in range but not insight
            /*

        // This possibly needs to be checked on the player to startle all zombies around it based on their movement and other heuristics - makes more sense - do it
            Problem is here that we want more conditions to check that we startle the zombie
            if (!inSight && !chasing)
            {
                if (Vector3.Distance(other.gameObject.transform.position + offsetY, transform.position) < 5f)
                {
                    Vector3 dir = playerObj.transform.position + offsetY - transform.position;
                    RaycastHit hit;

                    if (Physics.Raycast(transform.position, dir, out hit))
                    {
                        if (hit.transform.gameObject == other.gameObject)
                        {
                            Startle(other.gameObject);
                        }
                    }
                }
            }*/
            
        }
    }

    public void OccludeTrackingToggle()
    {
        bool kin = GetComponent<Rigidbody>().isKinematic;
        GetComponent<Rigidbody>().isKinematic = !kin;

        List<Transform> myChildren = GetComponentsInChildren<Transform>(true).ToList();
        myChildren.RemoveAll(trns => trns == this.transform);

        foreach (Transform child in myChildren)
        {
            child.gameObject.SetActive(kin);
        }

    }

    private bool PlayerInSight()
    {
        Vector3 dir = playerObj.transform.position - transform.position;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, 45f, maskLayer))
        {
            if (hit.transform.tag != "Player")
            {

                //Debug.Log("Not in sight");
                //Debug.Log("Object Tag: " + hit.transform.gameObject.name);
                return false;
            }
            else
            {

                //Debug.Log("I SEE YOU");
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    private void Attack()
    {
        if (Vector3.Distance(playerObj.transform.position + offsetY, transform.position) < 1.5f)
        {
            if (!mySpeaker.isPlaying)
            {
                mySpeaker.Play();
            }
        }
        else
        {
            SwitchToChase();
        }
    }

    public void SwitchToAttack()
    {
        myAnimation.SetBool("attack", true);
        if (mySpeaker.clip != sounds.zombieSounds[4])
        {
            mySpeaker.clip = sounds.zombieSounds[4];
            mySpeaker.Play();
        }

        Debug.Log("Attacking Now");
        state = ZombieState.Attacking;
    }

    #region CHASE STATE METHODS

    public void SwitchToChase()
    {
        int randomClip = Random.Range(0, sounds.zombieSounds.Length);
        mySpeaker.clip = sounds.zombieSounds[randomClip];
        mySpeaker.Play();

        myAnimation.SetFloat("speed", 1f);
        myAnimation.SetBool("attack", false);
        state = ZombieState.Chasing;
    }

    private void Chase()
    {
        if (PlayerInSight())
        {
            if (Vector3.Distance(playerObj.transform.position + offsetY, transform.position) < 1.5f)
            {
                SwitchToAttack();
                return;
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {


            if (Vector3.Distance(lastSeenNode.transform.position + new Vector3(0f, 1f, 0f), transform.position) < 1f)
            {
                StopChase();
                SwitchToPatrol();
            }
            else
            {
                ChasePosition();
                CheckChaseReturn();
            }

        }
    }

    public void ChasePosition()
    {
        if (lastSeenNode.GetY() != onNode.GetComponent<BlockPiece>().GetY())
        {
            StopChase();
            SwitchToPatrol();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, lastSeenNode.transform.position, 2 * Time.deltaTime);
        Vector3 lookPos = new Vector3(lastSeenNode.transform.position.x, transform.position.y, lastSeenNode.transform.position.z);
        transform.LookAt(lookPos, Vector3.up);
    }

    private void ChasePlayer()
    {
        lastSeenNode = playerObj.GetComponent<PlayerHeuristics>().BlockPosition;
        transform.position = Vector3.MoveTowards(transform.position, playerObj.transform.position + offsetY, 2 * Time.deltaTime);
        Vector3 lookPos = new Vector3(playerObj.transform.position.x, transform.position.y, playerObj.transform.position.z);
        transform.LookAt(lookPos, Vector3.up);
    }

    private void CheckChaseReturn()
    {
        if (samePosTimer > 0)
        {
            samePosTimer -= Time.deltaTime;
        }
        else
        {
            if (Vector3.Distance(transform.position, myLastPosition) < 0.5f)
            {
                if (samePosCounter < 3)
                {
                    Debug.Log("Same Place Counted");
                    samePosCounter++;
                }
                else
                {
                    Debug.Log("Zombie Switch To partol ofter standing for too long");
                    SwitchToPatrol();
                    samePosCounter = 0;
                }
            }

            myLastPosition = transform.position;
            samePosTimer = SamePositionDelay;

        }
    }

    public void StopChase()
    {
        myAnimation.SetFloat("speed", 0f);
        //playerObj.GetComponent<PlayerFlashLight>().alertedZombies.Remove(this.gameObject);
        state = ZombieState.Idle;
    }

    #endregion

    public void SwitchToPatrol()
    {
        if (myAnimation == null)
        {
            myAnimation = GetComponent<Animator>();
        }
        
        myAnimation.SetFloat("speed", 1f);
        state = ZombieState.Patrol;
    }

    void Update ()
    {
        switch(state)
        {
            case ZombieState.Idle:
                break;

            case ZombieState.Chasing:
                Chase();
                //ChasePath();
                break;

            case ZombieState.Attacking:
                Attack();
                break;

            case ZombieState.Patrol:
                TravelPath();
                break;

            case ZombieState.Searching:
                break;

            default:
                break;
        }
	}

    public void ResetLists()
    {
        // reset node statistics in lists for future use
        foreach (GameObject go in pathNodes)
        {
            go.GetComponent<BlockPiece>().isSearchPath = false;
            go.GetComponent<BlockPiece>().ResetHeuristics();
            go.GetComponent<BlockPiece>().ResetBlockLink();
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
    }

    public void TravelPath()
    {
        if (nextNodeIndex < pathNodes.Count)
        {
            if (MovedtoNode(pathNodes[nextNodeIndex]))
            {
                onNode = pathNodes[nextNodeIndex];
                nextNodeIndex++;
            }
        }
        else
        {
            SelectPatrolPath();
        }

    }

    public void ReconfigureChaseToPlayer()
    {
        ResetLists();
        GetPath(playerObj.GetComponent<PlayerHeuristics>().BlockPosition.gameObject);
    }

    public void ChasePath()
    {
        if (PlayerInSight())
        {
            if (!m_previouslyInSight)
            {
                m_previouslyInSight = true;
            }

            if (Vector3.Distance(playerObj.transform.position + offsetY, transform.position) < 1.5f)
            {
                SwitchToAttack();
                return;
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {

            if (m_previouslyInSight)
            {
                m_previouslyInSight = false;
                ResetLists();
                GetPath(lastSeenNode.gameObject);
            }

            if (nextNodeIndex < pathNodes.Count)
            {
                if (MovedtoNodeSmooth(pathNodes[nextNodeIndex]))
                {
                    onNode = pathNodes[nextNodeIndex];
                    nextNodeIndex++;
                }
            }
            else
            {
                StopChase();
                SwitchToPatrol();
            }
        }

        
    }

    private bool MovedtoNode(GameObject node)
    {
        targetPos = node.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, 2 * Time.deltaTime);
        Vector3 lookPos = new Vector3(node.transform.position.x, transform.position.y, node.transform.position.z);
        transform.LookAt(lookPos, Vector3.up);

        if (Vector3.Distance(targetPos, transform.position) < 0.5f)
        {
            return true;
        }

        return false;

    }

    private bool MovedtoNode(GameObject node, float range)
    {
        targetPos = node.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, 2 * Time.deltaTime);
        Vector3 lookPos = new Vector3(node.transform.position.x, transform.position.y, node.transform.position.z);
        transform.LookAt(lookPos, Vector3.up);

        if (Vector3.Distance(targetPos, transform.position) < range)
        {
            return true;
        }

        return false;

    }


    private bool MovedtoNodeSmooth(GameObject node)
    {
        targetPos = node.transform.position;
        Vector3 direction = targetPos - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, 2 * Time.deltaTime);

        //Vector3 lookPos = new Vector3(node.transform.position.x, transform.position.y, node.transform.position.z);
        transform.LookAt(playerObj.transform, Vector3.up);

        //Quaternion toRotation = Quaternion.FromToRotation(transform.forward, direction);
        //transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 2 * Time.deltaTime);
        
        //Vector3 lookPos = new Vector3(node.transform.position.x, transform.position.y, node.transform.position.z);
        //transform.LookAt(lookPos, Vector3.up);

        if (Vector3.Distance(targetPos, transform.position) < 1f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// GetPath will determine and store the path based from A* search given a goal node.
    /// This method has been overloaded to take multiple types of parameters which bias the search.
    /// The annotation for 'this' method should be referred to in following overloaded methods
    /// </summary>
    /// <param name="goalNode"></param>
    public void GetPath(GameObject goalNode)
    {
        if (onNode == null)
        {
            Debug.Log("No onNode has been Assigned");
            return;
        }

        nextNodeIndex = 0;

        // Reset the Lists - in case of fixed node to stair node switch
        // Whereby SetOnNode is not called again to avoid repitition
        if (openedNodes != null || closedNodes != null || pathNodes != null)
        {
            ResetLists();
        }

        // Increment cycles in while loop
        int cycles = 0;
        BlockPiece lookingAt = onNode.GetComponent<BlockPiece>();

        // First node in closed list should be the one we are on
        closedNodes.Add(lookingAt.gameObject);
        lookingAt.isSearchClosed = true;

        // Currently, lookingAt is the starting node, so its G score should be 0
        lookingAt.g = 0;

        while (lookingAt.gameObject != goalNode && openedNodes.Count >= 0)
        {
            // Check neighbors for next spot in the path
            for (int i = 0; i < lookingAt.neighbors.Length; i++)
            {
                BlockPiece neighbor = null;
                if (lookingAt.neighbors[i] != null)
                {
                    neighbor = lookingAt.neighbors[i].GetComponent<BlockPiece>();

                    if (neighbor.isWalkable && !neighbor.isSearchClosed && !neighbor.isStairNode && neighbor.isCorridor)
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
                lookingAt = nextInPath.GetComponent<BlockPiece>();
            }

            cycles++;
            if (cycles > 500)
            {
                return;
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
            Debug.LogError("No clear path could be found.");
        }
        else
        {
            // Trace the path nodes based on parents from the goal node to the start node
            FillPathList(goalNode);
        }
    }

    void ScoreNeighbor(BlockPiece lookingAt, BlockPiece neighbor, GameObject goalNode, float addedG)
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


    GameObject GetBestNode()
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

    void FillPathList(GameObject goalNode)
    {
        GameObject nodeObject = goalNode;
        BlockPiece node = nodeObject.GetComponent<BlockPiece>();
        pathNodes.Add(nodeObject);

        // Loop back through all the parented nodes from the goal
        while (nodeObject != onNode)
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
    }


}
