using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

public enum GamePhase
{
    WatchAndTaunt = 0,
    WatchTauntAndFollow = 1,
    MainCycle = 2,
    ExitCycle = 3
}

public interface LAConfig
{
    void Configure(GameObject player, LAObject annie);
    //void SwitchState();
}

public struct LAConfigStruct
{
    LAComponent component;


}

[RequireComponent(typeof(LAPhysics))]
[RequireComponent(typeof(LAAudio))]
[RequireComponent(typeof(LASense))]
[RequireComponent(typeof(LAAnimation))]
[RequireComponent(typeof(LAAttack))]
[RequireComponent(typeof(LAMovement))]
[RequireComponent(typeof(LANeurology))]
public class LAObject : MonoBehaviour // This Class has to be responsible for managing the behaviour
{
    private const int COMPONENT_COUNT = 7;
    private LAComponent[] m_components = new LAComponent[COMPONENT_COUNT];
    private BuildingGeneration m_building;

    private LAAnimation m_animation;    // How the AI is animating
    private LAAttack m_attacks;         // How the AI is attacking
    private LAMovement m_movement;      // How the AI is moving
    private LASense m_sense;            // How the AI is sensing
    private LANeurology m_emotion;      // How the AI is feeling
    private LAAudio m_audio;            // How the AI makes sounds

    private LAPhysics m_physics;    // Physics of the AI including occlusion handling

    private IBehaviourTreeNode m_tree;

    public LAAnimation Animation    { get { return m_animation; } }
    public LAAttack Attack          { get { return m_attacks; } }
    public LAMovement Movement      { get { return m_movement; } }
    public LASense Sense            { get { return m_sense; } }
    public LANeurology Emotion      { get { return m_emotion; } }
    public LAAudio Audio            { get { return m_audio; } }

    public LAPhysics Physics        { get { return m_physics; } }

    public BuildingGeneration Building { get { return m_building; } }

    private bool m_activated = false;

    public bool Active { get { return m_activated; } }

    public void Activate(GameObject player, BlockPiece spawnNode, BuildingGeneration building)
    {
        m_building = building;

        m_animation = GetComponentInChildren<LAAnimation>();
        m_attacks   = GetComponentInChildren<LAAttack>();
        m_movement  = GetComponentInChildren<LAMovement>();
        m_sense     = GetComponentInChildren<LASense>();
        m_emotion   = GetComponentInChildren<LANeurology>();
        m_audio     = GetComponentInChildren<LAAudio>();

        m_physics   = GetComponentInChildren<LAPhysics>();


        //Component[] comps = GetComponentsInChildren(typeof(LAComponent));
        //m_components = (LAComponent[])comps;

        m_components[0] = m_animation;
        m_components[1] = m_attacks;
        m_components[2] = m_movement;
        m_components[3] = m_sense;
        m_components[4] = m_emotion;
        m_components[5] = m_audio;
        m_components[6] = m_physics;

        foreach (LAComponent component in m_components)
        {
            component.Configure(player, this);
        }

        m_physics.SetHeuristicTemp();
        BehaviourTreeBuilder builder = new BehaviourTreeBuilder();
        this.m_tree = builder

            
            .Selector("Investigation")
            #region INVESTIGATION

                .Selector("Player Interactions")
                #region PLAYER INTERACTIONS

                    .Sequence("Sight Checks")
                    #region SIGHT

                        .Condition("Player In Sight", t => (m_sense.Prevoked) ? m_sense.PlayerInSight() : m_sense.PlayerInFOV()) // If we have been initially prevoked then just lock on to sight
                        // We got back to FOV sight once we have lost first sight - makes sense so the player can hide again, but maybe increase the fov here or inscript based on time since last encounter

                        .Splice(MoveAttackSubTree())

                    #endregion
                    .End()

                    .Sequence("Startled Checks")
                    #region STARTLED

                        .Condition("Startled?", t => m_sense.Startled)
                        .DoAction("Turn To Startle", t =>
                        {
                            return m_movement.TurnToFacePlayer();
                        })

                    #endregion
                    .End()

                #endregion
                .End()

                // Need to Move to Last Seen Node
                // We will only get here if we do not see the Player
                .Selector("Navigation")
                #region NAVIGATION

                    .Sequence("Move To Last Seen")
                    #region LAST SEEN MOVEMENT

                        .Condition("Have we been to the Last seen Node?", t => !m_sense.LastSightInvestigated)

                         // Only continue if we have NOT investigated the last seen
                        .DoAction("Move to Last Seen", t =>
                        {
                            // This will changed the last sight investigation if we have reached proximity
                            return m_movement.MoveToLastSeen();
                        })

                    #endregion
                    .End()

                    // For the purposes of testing, we are allowed to select a new patrol path here and test movement as it does not affect any internal working thanks to the booleans implicit checks
                    // We can simply just set the new movement path externally
                    // It will do its own thing and patrol on the current blocks it has if otherwise stated to move somewhere else by a director - BOOM BIATCH

                    // Move
                    .Condition("Select New Waypoint", t => m_movement.SelectNewPatrolPosition()) // If we cannot select a new way point then continue
                    .DoAction("Move to Waypoint", t =>
                    {
                        return m_movement.MoveToDestination();
                    })

                #endregion
                .End() // End the Navigation Selector

            #endregion
            .End() // End Investigation

            .Build();


        // Possible set up other components variable here
        m_movement.InstantlyMoveToNode(spawnNode);
        GetComponent<Rigidbody>().useGravity = true;
        StartCoroutine(ActivateDelay());
        
    }

    private IEnumerator ActivateDelay()
    {
        yield return new WaitForSeconds(2f);
        m_audio.Sing(false, 0f);
        m_activated = true;
    }

    private IBehaviourTreeNode MoveAttackSubTree()
    {
        var builder = new BehaviourTreeBuilder();
        return builder
            .Selector("Attack OR MoveToPlayer")
            #region ATTACK or MOVE TO PLAYER

                .Sequence("Attack")
                #region ATTACK

                    .Condition("In Range to Attack?", t => m_movement.InPlayerAttackRange())
                    .DoAction("Attack Slap Player", t =>
                    {
                        return m_attacks.SlapPlayer();
                    })

                #endregion
                .End() // End Attack Sequence

                .DoAction("MoveToPlayer", t =>
                {
                    return m_movement.MoveToPlayer();
                })

            #endregion
            .End() // End Attack or MoveToPlayer Selector
            .Build();
    }


    // Use this for initialization
    void Start ()
    {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (m_activated)
        {
            this.m_tree.Tick(new TimeData(Time.deltaTime));
        }
	}
}
