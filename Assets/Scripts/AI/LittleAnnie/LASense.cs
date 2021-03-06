﻿using UnityEngine;
using System.Collections;
using DaTup;
using System;
using System.Collections.Generic;

public enum LAAwareness
{
    Neutral = 0,
    Investigating = 1,
    Alerted = 2,
    Hiding = 3
}

// Create different startle events - maybe eventually create an event class for my own events
public enum StartleEvent
{
    PlayerFlashLight = 0,
    Sound = 1,
    ObjectInteraction = 2,
}

public class InputAIEvent
{
    // When something happens we need to know information about the event

    public InputAIEvent()
    {

    }
}





public enum SenseType
{
    Sound = 0, // Human-like responses for learning sound - e.g. hear something: "oh thats an X and quite close" ~or something
    Sight = 1, // Human-like responses for learning sight - e.g. see something: "oh thats an X, this is what I should do"

    // For Interaction with colliding with player. although this can be simplified with collision boxes i have left this in maybe for usefulness in future
    Touch = 2  // Human-like responses for learning touch - e.g. touch something: "oh that's hot"
}

public class LASense : LAComponent
{
    // Object perceptions are put inside the LASense class because different AI may have the ability to know about different perceptions
    // Future improvements for AI for post game refactoring would include the ability to have a perception for every sense per object.
    // Order these in priority of importance to the AI and make respective decision making in the perception loops
    public enum ObjectPerception
    {
        Player,
        Door,
        Interactable,
        Miscellaneous,
        Unknown // every AI must have an "Unknown Rank of perception" otherwise called NONE
    }

    // Create a dictionary of tags and Perceptions in knowledge zone: Note - this is because different AI might have different perceptions
    // There isnt really any otherway other than coding this dictionary for every AI as they will all have different percptions (theorectically)
    // Thankfully the GameTags remain the same and are put in the GameData class :)
    private Dictionary<string, ObjectPerception> m_TagPerceptions = new Dictionary<string, ObjectPerception>()
    {
        { GameTag.Player, ObjectPerception.Player },
        { GameTag.Door, ObjectPerception.Door },
        
        // Once other tags are put in here we can set the corresponding perception of the tag.
    };


    public ObjectPerception GetPerceptionFromTag(string tag)
    {
        return (m_TagPerceptions.ContainsKey(tag)) ? m_TagPerceptions[tag] : ObjectPerception.Unknown;
    }

    private class AlertedPerception
    {
        public bool Alerted;
        private SenseType senseType;
        public AlertedPerception(SenseType sense)
        {
            senseType = sense;
            Alerted = false;
        }
    }

    private class AlertedBehaviour
    {   
        private AlertedPerception[] m_Perceptions = new LASense.AlertedPerception[Enum.GetNames(typeof(SenseType)).Length];

        public AlertedBehaviour()
        {
            for (int i = 0; i < m_Perceptions.Length; i++)
                m_Perceptions[i] = new AlertedPerception((SenseType)i);
        }

        public AlertedPerception Sound
        {
            get { return m_Perceptions[(int)SenseType.Sound]; }
            set { m_Perceptions[(int)SenseType.Sound] = value; }
        }

        public AlertedPerception Sight
        {
            get { return m_Perceptions[(int)SenseType.Sight]; }
            set { m_Perceptions[(int)SenseType.Sight] = value; }
        }

        public AlertedPerception Touch
        {
            get { return m_Perceptions[(int)SenseType.Touch]; }
            set { m_Perceptions[(int)SenseType.Touch] = value; }
        }

        public bool Alerted()
        {
            // If any of our AI perceptions are alerted then we can be alerted - maybe this is where we can add up some weights? Theory! Work this back into the tree if so
            for (int i = 0; i < m_Perceptions.Length; i++)
                if (m_Perceptions[i].Alerted)
                    return true;
            
            return false;
        }
    }

    public void SetSoundAlert(bool active)
    {
        m_Awareness.Sound.Alerted = active;
    }

    private const float LAUGH_GENTLE_RESET = 9F;
    private const float SIGHT_TIMER_RESET = 5F;

    private const float FOV_MIN = 10F;
    private const float FOV_MAX = 50F;

    private const float SIGHT_DISTANCE_MIN = 20F;
    private const float SIGHT_DISTANCE_MAX = 60F;

    private LAHearingInput m_HearingInput;
    public LAHearingInput Ears { get { return m_HearingInput; } }

    private AlertedBehaviour m_Awareness = new AlertedBehaviour();
    // Ideas on creating a Player Makup Struct based on information we know of the player


    // Eventually make a player struct with information gained that we know about the player
    private BlockPiece player_previouslySeenNode = null;
    private BlockPiece m_LastSeenPlayerNode;
    private Vector3 player_lastSeenPosition;


    private BlockPiece m_NodeOfInterest;
    private BlockPiece m_PreviousNodeOfInterest;
    public BlockPiece NodeOfInterest { get { return m_NodeOfInterest; } }


    [SerializeField]
    [Range(SIGHT_DISTANCE_MIN, SIGHT_DISTANCE_MAX)]
    private float m_SightDistance = 40f;

    [SerializeField]
    [Range(FOV_MIN, FOV_MAX)]
    private float m_FieldOfView = 40f;

    [SerializeField]
    [Range(FOV_MAX,  360F)]
    private float m_FieldOfViewAlerted = 60f;


    private bool m_PlayerInSight = false;
    private bool m_PlayerPreviouslyInSight;



    private delegate void TransformSightInfo(Transform otherTransform);
    private GameInfoTimer m_SightTimer = new GameInfoTimer(SIGHT_TIMER_RESET);



    private bool m_startled = false;

    private LAAwareness m_senseState = LAAwareness.Neutral;


    private LayerMask m_maskLayer;
    public string[] m_maskLayersString;

    private float m_laughTimer = 0.0f;

    // At first we have reached all nodes of interest - meaning we must look for a new one initially
    private bool m_ReachedNodeOfInterest = true;


    public bool PlayerInSight
    {
        get { return m_PlayerInSight; }
    }

    public bool Startled
    {
        get { return m_startled; }
        set { m_startled = value; }
    }

    public bool ReachedNodeOfInterest
    {
        get { return m_ReachedNodeOfInterest; }
    }

    public LayerMask sightMaskLayer
    {
        get { return m_maskLayer; }
    }

    public void ReachNodeOfInterest()
    {
        m_ReachedNodeOfInterest = true;
    }

    public void ListenForSound()
    {
        //var hearingThreshold: float;   // Depends on character's hearing ability

        //var sqDist: float = (transform.position - obj.position).sqrMagnitude;
        //var perceivedLoudness: float = audio.volume / sqDist;

        //if (perceivedLoudness > hearingThreshold)
        //{
        //    // Target can hear sound
        //}

        // Idea is:
        // WE might have a stack or list of sounds - Audio sources which relate to an instance of the sound
        // Every few seconds we evaluate the list of sounds we can hear
        // Based on the evaluate we set an action - something will have to be worked into the tree for this to come correct

        // Sound listening might not change an action in the tree but change a heuristic aswell...

    }


    public void Startle(BlockPiece nodeOfInterest, StartleEvent eventType)
    {
        if (eventType == StartleEvent.PlayerFlashLight)
        {
            if (!m_PlayerPreviouslyInSight && !m_startled)
            {
                //m_CurrentPointOfInterest = pointOfInterest;
                Annie.Audio.Scream(false, 0.0f);
                m_startled = true;
            }
        }
    }

    private void OnPlayerSight(Transform playerTransformOnSight)
    {
        if (!m_PlayerPreviouslyInSight)
            Annie.Audio.LaughGentle(false, 0.0f);

        PlayerHeuristics pH = playerTransformOnSight.GetComponent<PlayerHeuristics>();
        m_NodeOfInterest = pH.BlockPosition;
        m_ReachedNodeOfInterest = false; // As long as we are in sight then we will not reach interest - this will remain until we lose sight : Sight has priority over interest
        m_PlayerPreviouslyInSight = true;
    }

    private bool CastSight(bool alert, string lookForObjTag, Vector3 directedPosition, TransformSightInfo storeInfoAction) // Change this to tag with action tuple
    {
        Vector3 dir = directedPosition - transform.position;
        float angle = Vector3.Angle(dir, this.transform.forward);

        // Check if the angle is less than our FOV - which is increased if we are alert. If not, then we cannot see the player for sure
        if (angle > Mathf.Min(((alert) ? m_FieldOfViewAlerted : m_FieldOfView), 360F))
            return false;

        // Are we able to see the object given the obstacles through the mask layer and vision distance?
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, m_SightDistance, m_maskLayer)) 
        {
            if (hit.transform.tag == lookForObjTag)
            {
                // Store the information passing the transform of whatever object we want to identify
                storeInfoAction(hit.transform);

                // If the AI preivously was not alerted by sight - it now is!
                if (!alert)
                    m_Awareness.Sight.Alerted = true;

                return true;
            }
        }
        
        return false;
    }

    public void SetNodeOfInterest(BlockPiece nodeOfInterest)
    {
        m_NodeOfInterest = nodeOfInterest;
        m_ReachedNodeOfInterest = false;
    }

    public BehaviourTreeStatus EvaluateSoundToInterest()
    {
        Vector3 positionOfSound = m_HearingInput.GetPointOfHighestPrioritySound();
        SetNodeOfInterest(Annie.Building.GetNodeClosestToPoint(positionOfSound));
        return BehaviourTreeStatus.Failure; // Return failure so the tree can continue to procces other tasks
    }

    /// <summary>
    /// FOV will change based on the awareness of the AI - this is handled internally and doesnt need to be worried about from external classes
    /// </summary>
    /// <returns>Can we see the player?</returns>
    public bool PlayerInFOV()
    {
        //(flashExtended ? (Action)FlashExtend : FlashReturn)();

        bool alert = m_Awareness.Alerted(); // Are we alerted by ANY of our perceptions?

        // If we are alerted by any of our perceptions then we will have increased FOV (more aware AI)
        m_PlayerInSight = CastSight(alert, GameTag.Player, player.transform.position, OnPlayerSight);
        if (m_PlayerInSight)
            return true;
        
        if (alert)
            m_Awareness.Sight.Alerted = m_SightTimer.Tick(Time.deltaTime, true);

        // This timer wont Tick unless the player is not in sight
        // If the last time we saw the player then now we can get a path to the interest node of when we last saw the player
        if (m_PlayerPreviouslyInSight && !m_ReachedNodeOfInterest)
            Annie.Movement.GetPathToInterest();
        

        m_PlayerPreviouslyInSight = m_PlayerInSight; // false
        return false;
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
        GetInputSensorComponents(); // These should not be avaliable to other components directly but indirectly through this class/component e.g. see "Ears" get property
    }

    private void GetInputSensorComponents()
    {
        // This has a similiar heirarchical system to the object controller - think of refactoring this aswell!
        m_HearingInput = GetComponentInChildren<LAHearingInput>();
        if (m_HearingInput)
        {
            m_HearingInput.SetSenseController(this);
        }
    }

    // Update is called once per frame
    public override void Update()
    {

    }
}
