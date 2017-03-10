using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LAMovement : LAComponent
{
    // Used for spawning just above the ground of the floor
    private static readonly Vector3 offSetY = new Vector3(0f, 1.5f, 0f);        // The Spawn instantiation offset to we spawn above the blockpieces and fall down onto them
    private List<BlockPiece> m_CurrentFloorNodes = new List<BlockPiece>();    // Used for adding the range of corridor blocks on the current floor - potentially swap to the current floor level
    private BlockPiece m_CurrentNode;                                   // The current Block Piece position of Annie
    private Pathfinder m_Pathfinder;                                            // Used for our pathfinding - we must remotely set and get the paths & nodes through it
    private int m_PathingIndex = 0;                                             // Index used to count the progress between the block pieces while patrolling
    private BlockPiece m_NextTargetNode;

    private float m_speedModifier = 1f;

    private float m_StepCycle;
    private float m_NextStep;
    private Rigidbody m_Rigidbody;
    private Vector3 m_MovementVector;
    private bool m_PreviouslyGrounded;
    private bool m_IsGrounded;
    private Vector3 m_GroundContactNormal;
    private bool m_Jumping;
    private bool m_Jump;
    private CapsuleCollider m_Capsule;
    public AdvancedSettings advancedSettings = new AdvancedSettings();
    public MovementSettings movementSettings = new MovementSettings();

    [SerializeField]
    [Range(0f, 1f)]
    private float m_RunstepLenghten; // 0.8f works quite well

    [SerializeField]
    private float m_StepInterval;


    /// <summary>
    /// Returns the floor number from the current node position of the AI
    /// </summary>
    public int CurrentFloor { get { return m_CurrentNode.GetY(); } }

    public Pathfinder Pathfinder { get { return m_Pathfinder; } }

    public BlockPiece CurrentNode { get { return m_CurrentNode; } }

    [System.Serializable]
    public class AdvancedSettings
    {
        public float groundCheckDistance = 0.01f;               // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
        public float stickToGroundHelperDistance = 0.5f;        // stops the character
        public float slowDownRate = 20f;                        // rate at which the controller comes to a stop when there is no input
        public bool airControl;                                 // can the user control the direction that is being moved in the air
        [Tooltip("set it to 0.1 or more if stuck in wall")]
        public float shellOffset;                               //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
    }

    [System.Serializable]
    public class MovementSettings
    {
        private bool m_Running = false;
        public float RunMultiplier = 2.0f;      // Speed when sprinting
        public float CurrentTargetSpeed = 8f;
        public float JumpForce = 30f;
        public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
        public bool Running { get { return m_Running; } }

        public void SetRunning(bool active)
        {
            if (m_Running != active)
            {
                m_Running = active;
                CurrentTargetSpeed = (active) ? CurrentTargetSpeed * RunMultiplier : CurrentTargetSpeed / RunMultiplier;
            }
        }
    }

    public void SetNodePosition(BlockPiece node)
    {
        m_CurrentNode = node; // WE NEVER NEED TO SET THIS - IT SHOULD ALWAYS BE DONE FOR US
        m_Pathfinder.SetOnNode(node);
    }

    private bool TrackToPosition(Vector3 position, bool faceTarget)
    {
        MoveToTarget(position, faceTarget);
        return InRangeOfTarget(position);
    }

    private bool TrackToObject(GameObject obj, bool faceNode)
    {
        MoveToObject(obj, faceNode);
        return InRangeOfTarget(obj.transform.position);
    }

    private bool InRangeOfTarget(Vector3 targetPosition)
    {
        return Vector3.Distance(targetPosition, transform.position) < 0.7f;
    }

    private void MoveToObject(GameObject obj, bool faceNode)
    {
        MoveToTarget(obj.transform.position, faceNode);
    }

    private void MoveToTarget(Vector3 position, bool faceNode)
    {
        position += new Vector3(0f, 0.5f, 0f);
        m_MovementVector = (Vector3.MoveTowards(transform.position, position, m_speedModifier * Time.deltaTime) - transform.position).normalized;
        //transform.position = Vector3.MoveTowards(transform.position, position, SlopeMultiplier() * Time.deltaTime);

        if (faceNode)
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, DirectionRotation(position), 0.1f);
    }

    public Quaternion DirectionRotation(Vector3 lookAtPosition)
    {
        Vector3 direction = lookAtPosition - this.transform.position;
        direction.y = 0;
        return (direction != Vector3.zero) ? Quaternion.LookRotation(direction.normalized, Vector3.up) : this.transform.rotation;
    }

    public void InstantlyMoveToNode(BlockPiece node)
    {        
        Annie.gameObject.transform.position = node.transform.position + offSetY;
        if (m_CurrentNode == null)
            SetNodePosition(node);

        CollectBlocksOnFloor(node.GetY());
        // Reset tracking and other variables and shizzle
    }

    public void CollectBlocksOnFloor(int y)
    {
        m_CurrentFloorNodes.Clear();
        m_CurrentFloorNodes.AddRange(Annie.Building.Floors[y].floorBlocks.FindAll(n => !n.isStairNode && n.isWalkable));
    }

    /// <summary>
    /// This Functions assumes locates a random node from the current nodes avalaible
    /// </summary>
    private void SelectRandomPathOnFloor()
    {
        List<BlockPiece> avaliableNodes = new List<BlockPiece>();
        avaliableNodes.AddRange(m_CurrentFloorNodes);
        avaliableNodes.Remove(m_CurrentNode);
        int chosenIndex = UnityEngine.Random.Range(0, avaliableNodes.Count);
        SetAndGetPathToInterest(avaliableNodes[chosenIndex]);
    }

    public void SetAndGetPathToInterest(BlockPiece nodeOfInterest)
    {
        Annie.Sense.SetNodeOfInterest(nodeOfInterest);
        GetPathToInterest();
    }

    private bool StoreNextPosition()
    {
        if (m_PathingIndex < m_Pathfinder.CombinedPathNodes.Count) // Otherwise we will check once out of exception
        {
            m_NextTargetNode = m_Pathfinder.CombinedPathNodes[m_PathingIndex].GetComponent<BlockPiece>();
            OpenIncomingDoors();
            return true;

            //if (m_nextTargetNode.isOccluded)
            //{
            //    Annie.Physics.DisableGravity();
            //}
            //else
            //{
            //    Annie.Physics.EnableGravity();
            //}
        }
        else
        {
            // We have reached the destination
            Annie.Sense.ReachNodeOfInterest();
            return false;
        }
    }

    private void OpenIncomingDoors()
    {
        if (m_CurrentNode != null)
        {
            bool leave = m_CurrentNode.isRoomConnection && m_NextTargetNode.isCorridorConnection;
            bool enter = m_CurrentNode.isCorridorConnection && m_NextTargetNode.isRoomConnection;
            if (leave || enter) 
            {
                Debug.Log("Attempting To Open A Door");
                CellDoorScript door = (enter) ? m_NextTargetNode.GetComponentInChildren<CellDoorScript>() : m_CurrentNode.GetComponentInChildren<CellDoorScript>();
                if (door != null)
                {
                    if (!door.isOpened)
                        door.AttemptToOpen();
                }
                else
                {
                    Debug.LogError("IT WAS NULL");
                }
                    
            }
        }
    }

    public void GetPathToInterest()
    {
        if (m_Pathfinder.GetPathFullTraversal(Annie.Sense.NodeOfInterest.gameObject, true)) // At the moment she does not include rooms, but it is programmed ready for room testing, just need to apply the actions of opening doors on pathing
        {
            Annie.Animation.Walk(true);

            // If the first index is closer than zero then we will start pathing from that index instead
            if (m_Pathfinder.CombinedPathNodes.Count > 1)
            {
                m_PathingIndex = (Vector3.Distance(m_Pathfinder.CombinedPathNodes[0].transform.position, transform.position) >
                                Vector3.Distance(m_Pathfinder.CombinedPathNodes[1].transform.position, transform.position)) ? 1 : 0;
            }
            else
            {
                // Provided there is only 1 node found for a route
                m_PathingIndex = 0;
            }
            
            StoreNextPosition();
        }
        else
        {
            Annie.Animation.Walk(false);
        }
        
    }

    public bool InPlayerAttackRange()
    {
        return Vector3.Distance(this.transform.position, player.transform.position) < 1.5f;
    }

    public BehaviourTreeStatus MoveToPlayer()
    {
        Annie.Animation.Walk(true);
        MoveToTarget(player.transform.position, true);
        return BehaviourTreeStatus.Running;
    }

    public BehaviourTreeStatus SelectRandomNodeOfInterestOnFloor()
    {
        SelectRandomPathOnFloor();
        return BehaviourTreeStatus.Success;
    }

    public BehaviourTreeStatus TurnToPointOfInterest()
    {
        // This will continue to look at the player until the player in sight is triggered
        //Vector3 relativePlayerPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, DirectionRotation(Annie.Sense.PointOfInterest), 0.1f);
        return BehaviourTreeStatus.Running;
    }

    // Use this for initialization
    public override void Start ()
    {
        m_Pathfinder = GetComponentInChildren<Pathfinder>();
        m_Capsule = GetComponent<CapsuleCollider>();
        m_Rigidbody = GetComponent<Rigidbody>();
	}

    // Update is called once per frame
    public override void Update ()
    {

	}

    private float SlopeMultiplier()
    {
        float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
        return (movementSettings.SlopeCurveModifier.Evaluate(angle));
    }

    public override void FixedUpdate() // After completeing the fixed update we must remove the transform update form the method funcitons as all of the movement will be handled here
    {
        if (!ActiveComponent) return;
        if (!Annie.Active) return;

        GroundCheck();

        if ((Mathf.Abs(m_MovementVector.x) > float.Epsilon || Mathf.Abs(m_MovementVector.z) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded))
        {
            m_MovementVector = Vector3.ProjectOnPlane(m_MovementVector, m_GroundContactNormal).normalized;
            m_MovementVector.x = m_MovementVector.x * movementSettings.CurrentTargetSpeed;
            m_MovementVector.z = m_MovementVector.z * movementSettings.CurrentTargetSpeed;
            m_MovementVector.y = m_MovementVector.y * movementSettings.CurrentTargetSpeed;

            if (m_Rigidbody.velocity.sqrMagnitude < (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
            {
                float velocityY = m_Rigidbody.velocity.y;
                m_Rigidbody.velocity = ((m_MovementVector / 2) * SlopeMultiplier() * 0.5f) + new Vector3(0f, velocityY, 0f);
            }
        }

        if (m_IsGrounded)
        {
            m_Rigidbody.drag = 10f;

            if (m_Jump)
            {
                m_Rigidbody.drag = 0f;
                m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, 0f, m_Rigidbody.velocity.z);
                m_Rigidbody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                m_Jumping = true;
            }

            if (!m_Jumping && Mathf.Abs(m_MovementVector.x) < float.Epsilon && Mathf.Abs(m_MovementVector.z) < float.Epsilon && m_Rigidbody.velocity.magnitude < 1f)
                m_Rigidbody.Sleep();
        }
        else
        {
            m_Rigidbody.drag = 0f;
            if (m_PreviouslyGrounded && !m_Jumping)
                StickToGroundHelper();

        }

        m_Jump = false;
        ProgressStepCycle(m_MovementVector);
    }

    private void ProgressStepCycle(Vector3 input)
    {
        if (m_Rigidbody.velocity.sqrMagnitude > 0 && (input.x != 0 || input.z != 0))
            m_StepCycle += (m_Rigidbody.velocity.magnitude + (movementSettings.CurrentTargetSpeed * (!movementSettings.Running ? 1f : m_RunstepLenghten))) * Time.fixedDeltaTime;

        if (!(m_StepCycle > m_NextStep) || Annie.Attack.isAttacking) return;
        m_NextStep = m_StepCycle + m_StepInterval;
        Annie.Audio.PlayFootStepAudio();
    }

    private void StickToGroundHelper()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                ((m_Capsule.height / 2f) - m_Capsule.radius) +
                                advancedSettings.stickToGroundHelperDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                m_Rigidbody.velocity = Vector3.ProjectOnPlane(m_Rigidbody.velocity, hitInfo.normal);
        }
    }

    /// <summary>
    /// This is the method used when updateing the movement from the behaviour tree
    /// </summary>
    /// <returns></returns>
    public BehaviourTreeStatus MoveToNodeOfInterest() // We need to carefully combine this method with the fixed update or find a way of tickikng the behaviour tree through fixed update aswell as regualr update
    {
        if (TrackToObject(m_Pathfinder.CombinedPathNodes[m_PathingIndex], true))
        {
            m_PathingIndex++;
            if (StoreNextPosition())
            {
                return BehaviourTreeStatus.Running;
            }
            else
            {
                return BehaviourTreeStatus.Success;
            }
        }

        return BehaviourTreeStatus.Running;

        //if (Vector3.Distance(Annie.Sense.NodeOfInterest.transform.position, transform.position) < 1f)
        //{
        //    Debug.Log("Reached Last seen");
        //    Annie.Sense.ReachNodeOfInterest();
        //    return BehaviourTreeStatus.Success;
        //}
        //else
        //{
        //    return BehaviourTreeStatus.Running;
        //}
    }

    public BehaviourTreeStatus MoveToDestination()
    {
        if (TrackToObject(m_Pathfinder.CombinedPathNodes[m_PathingIndex], true))
        {
            m_PathingIndex++;
            StoreNextPosition();
        }

        return BehaviourTreeStatus.Running; // Or true.. not sure just yet
    }

    /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
    private void GroundCheck()
    {
        m_PreviouslyGrounded = m_IsGrounded;
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            m_IsGrounded = true;
            m_GroundContactNormal = hitInfo.normal;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundContactNormal = Vector3.up;
        }

        if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
        {
            //PlayLandingSound();
            m_Jumping = false;
        }
    }


}
