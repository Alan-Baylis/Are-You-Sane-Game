using UnityEngine;
using System.Collections;

public enum LAAwareness
{
    Neutral = 0,
    Investigating = 1,
    Alerted = 2,
    Hiding = 3
}

public class LASense : LAComponent
{
    private const string PLAYER_TAG = "Player";
    // Ideas on creating a Player Makup Struct based on information we know of the player


    private const float SIGHT_TIMER = 5f;
    private float m_incresaedSightTimer = SIGHT_TIMER;

    private BlockPiece player_previouslySeenNode = null;
    private BlockPiece player_lastSeenNode;
    private Vector3 player_lastSeenPosition;
    private bool player_previouslyInSight;
    private bool m_prevoked = false;

    private bool m_startled = false;

    private bool m_previouslyInSight;
    private LAAwareness m_senseState = LAAwareness.Neutral;


    private LayerMask m_maskLayer;
    public string[] m_maskLayersString;


    private const float LAUGH_GENTLE_RESET = 9f;
    private float m_laughTimer = 0.0f;

    private bool m_lastSightInvestigated = true;

    public bool Prevoked
    {
        get { return m_prevoked; }
        set { m_prevoked = value; }
    }

    public bool Startled
    {
        get { return m_startled; }
        set { m_startled = value; }
    }

    public bool LastSightInvestigated
    {
        get { return m_lastSightInvestigated; }
        set { m_lastSightInvestigated = value; }
    }

    /// <summary>
    /// This position has been made relative to the floor level
    /// </summary>
    public Vector3 LastSeenPlayerPosition
    {
        get { return player_lastSeenPosition; }
    }

    public BlockPiece LastSeenPlayerNode
    {
        get { return player_lastSeenNode; }
    }

    public LayerMask sightMaskLayer
    {
        get { return m_maskLayer; }
    }


    public void Startle()
    {
        if (!m_previouslyInSight && !m_startled)
        {
            m_startled = true;
            Annie.Audio.Scream(false, 0.0f);
        }
    }

    private void SetOnSightProperties()
    {
        if (!m_previouslyInSight && !m_startled)
            Annie.Audio.LaughGentle(false, 0.0f);

        //player_lastSeenNode = player.GetComponent<PlayerHeuristics>().BlockPosition;
        //player_lastSeenPosition = player.transform.position;
        //player_lastSeenPosition.y = player_lastSeenNode.transform.position.y;
        m_lastSightInvestigated = false;
        m_previouslyInSight = true;
        m_startled = false;
    }

    public bool PlayerInFOV()
    {
        if (CastSight(true, PLAYER_TAG))
        {
            m_prevoked = true;
            SetOnSightProperties();
            return true;
        }

        HandleLastPlayerSight();
        m_previouslyInSight = false;
        return false;
    }

    private void HandleLastPlayerSight()
    {
        // Keep a pathfind to the last seen node if it was different from the last seen node beforehand - first time will be null
        if (m_previouslyInSight && player_previouslySeenNode != player_lastSeenNode)
        {
            Annie.Movement.SelectMovementPath(player_lastSeenNode);
            player_previouslySeenNode = player_lastSeenNode;
        }
    }

    private bool CastSight(bool fov, params string[] tags)
    {
        Vector3 dir = player.transform.position - transform.position;
        float angle = Vector3.Angle(dir, this.transform.forward);

        // If we are checking against fov then return false before the cast if the conditions do not match
        if (fov && !(Vector3.Distance(player.transform.position, this.transform.position) < 30f && angle < 40))
            return false;

        RaycastHit hit;        
        if (Physics.Raycast(transform.position, dir, out hit, 40f, m_maskLayer)) // Are we able to see the player given the obstacles through the mask layer?
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (hit.transform.tag == tags[i])
                {
                    PlayerHeuristics pH = hit.transform.GetComponent<PlayerHeuristics>();
                    player_lastSeenNode = pH.BlockPosition;
                    player_lastSeenPosition = hit.transform.position;
                    player_lastSeenPosition.y = player_lastSeenNode.transform.position.y;
                    return true;
                }
                    
            }
        }

        return false;
    }

    public bool PlayerInSight()
    {

        if (CastSight(false, PLAYER_TAG))
        {
            if (m_prevoked) // Continue to be prevoked if we have not reset the condition
                m_incresaedSightTimer = SIGHT_TIMER;

            SetOnSightProperties();
            return true;
        }
            
        HandleLastPlayerSight();

        // Heightened awareness until 5 seconds after we have reached our last sighted location for the player
        if (m_prevoked && m_lastSightInvestigated)
        {
            if (m_incresaedSightTimer > 0)
            {
                m_incresaedSightTimer -= Time.deltaTime;
            }
            else
            {
                m_incresaedSightTimer = SIGHT_TIMER;
                m_prevoked = false; // <<------- UNPREVOKED CALL HERE
            }
        }

        m_previouslyInSight = false;
        return false;
    }

    public bool isPlayerWithinRangeOnFloor()
    {
        return player.GetComponent<PlayerHeuristics>().CurrentFloor == Annie.Movement.currentFloor && Vector3.Distance(player.transform.position, transform.position) < 10f;
    }

    private void InitializeRayCastLayers()
    {
        m_maskLayer = LayerMaskExtensions.Create("Ignore Raycast");
        foreach (string mask in m_maskLayersString)
        {
            m_maskLayer = m_maskLayer.AddToMask(mask);
        }

        m_maskLayer = m_maskLayer.Inverse();
    }



    // Use this for initialization
    public override void Start()
    {
        InitializeRayCastLayers();
    }

    // Update is called once per frame
    public override void Update()
    {

    }
}
