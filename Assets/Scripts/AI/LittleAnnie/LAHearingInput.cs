using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class LAHearingInput : MonoBehaviour
{
    private class GameObjectSoundSource
    {
        public LASense.ObjectPerception m_Perception;
        public AudioSource m_Source;
        public GameObject m_GameObject;
        public GameObjectSoundSource(GameObject objWithSource, LASense.ObjectPerception perception)
        {
            m_GameObject = objWithSource;
            m_Source = objWithSource.GetComponentInChildren<AudioSource>();
            m_Perception = perception;
        }
    }

    private List<GameObjectSoundSource> m_KnownSources = new List<GameObjectSoundSource>();
    private List<GameObjectSoundSource> m_InRangeSources = new List<GameObjectSoundSource>();
    private List<GameObjectSoundSource> m_ThresholdSources = new List<GameObjectSoundSource>();

    private GameObjectSoundSource m_CurrentSource = null;

    [SerializeField] [Range(0.1f, 1f)]
    private float m_HearingTheshold = 0.2f;

    private static float m_GSourceDist;
    private static float m_PercievedGSourceLoudness;
    public LASense m_SenseController = null;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        // This should also be evaluated once every 2 seconds or something liek dat bro cash me outside
        //foreach (GameObjectSoundSource gSource in m_InRangeSources)
        //{

        //}
    }

    public void SetSenseController(LASense sense)
    {
        m_SenseController = sense;
    }
    
    private static bool CanHearThreshold(Vector3 position, GameObjectSoundSource gSource, float threshold)
    {
        if (gSource.m_Source.volume > 0.1)
        {
            // We possibly want a logrithmic curve here not a linear one or expodential (more "human" like hearing)
            m_GSourceDist = (position - gSource.m_GameObject.transform.position).sqrMagnitude;
            m_PercievedGSourceLoudness = gSource.m_Source.volume / m_GSourceDist;
            if (m_PercievedGSourceLoudness > threshold)
            {
                Debug.Log("AI can hear Something! Watch Out!");
            }

            return m_PercievedGSourceLoudness > threshold;
        }
        else
        {
            // We defintiely cannot here it this quiet - we want to avoid dividing Zero
            return false;
        }
    }


    public bool CanHearSound()
    {
        // Find all source we can ACTUALLY hear
        m_ThresholdSources = m_InRangeSources.FindAll(source => CanHearThreshold(transform.position, source, m_HearingTheshold)); // Now we need to set alert status for sense - call this back through the tree HOYL FUCK
        if (m_ThresholdSources.Count > 0)
        {
            m_SenseController.SetSoundAlert(true);
            Debug.Log("We have been alerted");
            return true;
        }
        else
        {
            if (m_InRangeSources.Count > 0)
            {
                foreach(GameObjectSoundSource source in m_InRangeSources)
                {
                    // above 0.05 (when triggered)
                    // above 0.07 - 0.1 (triggering volume)
                    // above 0.1 (definitely heard)
                    if (source.m_Perception == LASense.ObjectPerception.Player)
                    {
                        m_GSourceDist = (transform.position - source.m_GameObject.transform.position).sqrMagnitude;
                        m_PercievedGSourceLoudness = (source.m_Source.volume / m_GSourceDist) * 10;

                        Debug.Log("Percieved Loudness of player: " + m_PercievedGSourceLoudness);
                        break;
                    }
                }


            }
            return false;
        }
    }


    public Vector3 GetPointOfHighestPrioritySound()
    {
        foreach (LASense.ObjectPerception perception in Enum.GetValues(typeof(LASense.ObjectPerception)))
        {
            m_CurrentSource = m_ThresholdSources.Find(p => p.m_Perception == perception);
            if (m_CurrentSource != null)
            {
                return m_CurrentSource.m_GameObject.transform.position;
            }
        }

        return transform.position;
    }

    /// <summary>
    /// What we want to do when we hear sounds BRAINSTORM:
    /// Player:
    /// 
    /// Move Towards the sound
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    public void EvaluateSoundsInRange()
    {
        // Now we prioritise the hearing - listening for the player first of course - using the THRESHOLD SOURCES NOW
        //m_CurrentSource = m_ThresholdSources.Find(s => s.m_Perception == SoundPerception.Player);

        


        // If there are multiple sounds now then we need to decide which one to go to
        // Once we have the basic structure here then we will implement the decision making in the tree

    }

    // Make this collider only with certain layers or make a tag multiple check implemented in GameTag static class
    private void OnTriggerEnter(Collider other)
    {
        if (m_SenseController == null)
            return;

        // If we know about the object/have a perception of the object - In my game, the AI has knowledge of every sound due to pre-emptive tagging
        // The AI should never have an sound which it has no idea about
        if (m_SenseController.GetPerceptionFromTag(other.gameObject.tag) != LASense.ObjectPerception.Unknown)
        {
            if (other.tag == GameTag.Player)
            {
                Debug.Log("Player Enterend Trigger");
            }

            GameObjectSoundSource knownIncomingSource = m_KnownSources.Find(obj => obj.m_GameObject == other.gameObject);
            if (knownIncomingSource == null)
                knownIncomingSource = new GameObjectSoundSource(other.gameObject, m_SenseController.GetPerceptionFromTag(other.gameObject.tag));

            if (!m_InRangeSources.Contains(knownIncomingSource))
                m_InRangeSources.Add(knownIncomingSource);
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_SenseController == null)
            return;

        // If we know about the object/have a perception of the object - In my game, the AI has knowledge of every sound due to pre-emptive tagging
        // The AI should never have an sound which it has no idea about
        if (m_SenseController.GetPerceptionFromTag(other.gameObject.tag) != LASense.ObjectPerception.Unknown)
        {
            GameObjectSoundSource knownOutgoingSource = m_KnownSources.Find(obj => obj.m_GameObject == other.gameObject);
            if (knownOutgoingSource != null && m_InRangeSources.Contains(knownOutgoingSource))
                m_InRangeSources.Remove(knownOutgoingSource);
        }
    }
}
