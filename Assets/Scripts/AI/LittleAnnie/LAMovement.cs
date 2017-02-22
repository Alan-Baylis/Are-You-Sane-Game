using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum LAMove
{
    Idle = 0,
    Patrol = 1,
    MoveToTarget = 2
}



public class LAMovement : LAComponent
{
    // Used for spawning just above the ground of the floor
    private static readonly Vector3 offSetY = new Vector3(0f, 1.5f, 0f);        // The Spawn instantiation offset to we spawn above the blockpieces and fall down onto them
    private List<BlockPiece> m_currentPatrolBlocks = new List<BlockPiece>();    // Used for adding the range of corridor blocks on the current floor - potentially swap to the current floor level
    private BlockPiece m_currentNodePosition;                                   // The current Block Piece position of Annie
    private Pathfinder m_pathFinder;                                            // Used for our pathfinding - we must remotely set and get the paths & nodes through it
    private int m_pathingIndex = 0;                                             // Index used to count the progress between the block pieces while patrolling
    private bool m_reachedSelectedPath = false;                                 // Bool for stating if we have reached the target destination for pathfinding
    private bool m_chasingLastKnowPosition = false;
    private BlockPiece m_nextTargetNode;

    private float m_speedModifier = 1f;
    private bool m_climbingStairs = false;

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

    [SerializeField][Range(0f, 1f)]
    private float m_RunstepLenghten;

    [SerializeField]
    private float m_StepInterval;



    private List<GameObject> m_currentMovementBlocks = new List<GameObject>();

    private BlockPiece m_targetTempNode;
    private bool m_stairClimbing = false;

    LAMove m_moveState = LAMove.Idle;

    /// <summary>
    /// Returns the floor number from the current node position of the AI
    /// </summary>
    public int currentFloor { get { return m_currentNodePosition.GetY(); } }

    public Pathfinder pathfinder { get { return m_pathFinder; } }

    public BlockPiece currentNodePosition { get { return m_currentNodePosition; } }

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
        m_currentNodePosition = node; // WE NEVER NEED TO SET THIS - IT SHOULD ALWAYS BE DONE FOR US
        m_pathFinder.SetOnNode(node);
    }

    /// <summary>
    /// This is an indirect boolean which will automatically come true once the search for any position used with the pathfinder comes true
    /// </summary>
    private bool reachedSelectedDistination
    {
        get { return (m_pathingIndex >= m_pathFinder.combinedPathNodes.Count); }
    }

    private bool TrackToPosition(Vector3 position, bool faceTarget)
    {
        MoveToTarget(position, faceTarget);
        return inRangeOfTargetOrObject(position);
    }

    private bool TrackToObject(GameObject obj, bool faceNode)
    {
        MoveToObject(obj, faceNode);
        return inRangeOfTargetOrObject(obj.transform.position);
    }

    private bool inRangeOfTargetOrObject(Vector3 targetPosition)
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
        if (m_currentNodePosition == null)
            SetNodePosition(node);

        SetPatrolBlocks(node.GetY());
        // Reset tracking and other variables and shizzle
    }

    public void SetPatrolBlocks(int y)
    {
        m_currentPatrolBlocks.Clear();
        m_currentPatrolBlocks.AddRange(Annie.Building.Floors[y].routeBlocks.FindAll(n => !n.isStairNode));
    }

    /// <summary>
    /// This Functions assumes locates a random node from the current nodes avalaible
    /// </summary>
    private void SelectPatrolPath()
    {
        List<BlockPiece> avaliableNodes = new List<BlockPiece>();
        avaliableNodes.AddRange(m_currentPatrolBlocks);
        avaliableNodes.Remove(m_currentNodePosition);
        int chosenIndex = UnityEngine.Random.Range(0, avaliableNodes.Count);

        m_targetTempNode = avaliableNodes[chosenIndex];
        //SelectCorridorPath(m_targetTempNode.gameObject);

        SelectMovementPath(m_targetTempNode);
    }

    private void StoreNextPosition()
    {
        if (m_pathingIndex < m_pathFinder.combinedPathNodes.Count) // Otherwise we will check once out of exception
        {
            m_nextTargetNode = m_pathFinder.combinedPathNodes[m_pathingIndex].GetComponent<BlockPiece>();

            //if (m_nextTargetNode.isOccluded)
            //{
            //    Annie.Physics.DisableGravity();
            //}
            //else
            //{
            //    Annie.Physics.EnableGravity();
            //}
        }
    }

    public void SelectMovementPath(BlockPiece destination)
    {
        m_targetTempNode = destination;
        if (m_pathFinder.GetPathFullTraversal(destination.gameObject, false)) // At the moment she does not include rooms, but it is programmed ready for room testing, just need to apply the actions of opening doors on pathing
        {
            Annie.Animation.Walk(true);

            // If the first index is closer than zero then we will start pathing from that index instead
            if (m_pathFinder.combinedPathNodes.Count > 1)
            {
                m_pathingIndex = (Vector3.Distance(m_pathFinder.combinedPathNodes[0].transform.position, transform.position) >
                                Vector3.Distance(m_pathFinder.combinedPathNodes[1].transform.position, transform.position)) ? 1 : 0;
            }
            else
            {
                // Provided there is only 1 node found for a route
                m_pathingIndex = 0;
            }
            
            StoreNextPosition();
        }
        else
        {
            Debug.LogError("No Path Could be found on patrol!: Path To: " + destination.name);
            Annie.Animation.Walk(false);
        }
        
    }

    public bool InPlayerAttackRange()
    {
        Vector3 relativePlayerPosition = new Vector3(player.transform.position.x, Annie.Sense.LastSeenPlayerNode.transform.position.y, player.transform.position.z);
        return Vector3.Distance(this.transform.position, relativePlayerPosition) < 1.5f;
    }

    public BehaviourTreeStatus MoveToPlayer()
    {
        Annie.Animation.Walk(true);
        Vector3 relativePlayerPosition = new Vector3(player.transform.position.x, Annie.Sense.LastSeenPlayerNode.transform.position.y, player.transform.position.z);
        MoveToTarget(relativePlayerPosition, true);
        return BehaviourTreeStatus.Running;
    }

    public BehaviourTreeStatus TurnToFacePlayer()
    {
        // This will continue to look at the player until the player in sight is triggered
        Vector3 relativePlayerPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, DirectionRotation(relativePlayerPosition), 0.1f);
        return BehaviourTreeStatus.Running;
    }

    // Use this for initialization
    public override void Start ()
    {
        m_pathFinder = GetComponentInChildren<Pathfinder>();
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
            //Vector3 desiredMove = (transform.forward * m_MovementVector.z) + (transform.right * m_MovementVector.x);
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
    public BehaviourTreeStatus MoveToLastSeen() // We need to carefully combine this method with the fixed update or find a way of tickikng the behaviour tree through fixed update aswell as regualr update
    {
        if (TrackToObject(m_pathFinder.combinedPathNodes[m_pathingIndex], true))
        {
            m_pathingIndex++;
            StoreNextPosition();
        }

        if (Vector3.Distance(Annie.Sense.LastSeenPlayerNode.transform.position, transform.position) < 1f)
        {
            Debug.Log("Reached Last seen");
            m_reachedSelectedPath = true;
            Annie.Sense.LastSightInvestigated = true;
            return BehaviourTreeStatus.Success;
        }
        else
        {
            return BehaviourTreeStatus.Running;
        }
    }

    public bool SelectNewPatrolPosition()
    {
        if (reachedSelectedDistination)
        {
            SelectPatrolPath();
            return true;
        }
        else
        {
            return false;
        }
    }

    public BehaviourTreeStatus MoveToDestination()
    {
        if (TrackToObject(m_pathFinder.combinedPathNodes[m_pathingIndex], true))
        {
            m_pathingIndex++;
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
