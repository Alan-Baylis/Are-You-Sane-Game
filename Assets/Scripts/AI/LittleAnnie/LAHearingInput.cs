using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LAHearingInput : MonoBehaviour
{
    // Here we could just say Player or interactable - this decides how complex we want the AI to interact with its environment
    // We should put specfic perceptions - the more perceptions, the more behaviours for the tree
    public enum SoundPerception
    {
        Player,
        Door,
        Interactable,
        Miscellaneous,
    }


    private class GameObjectSoundSource
    {
        public SoundPerception m_Perception;
        public AudioSource m_Source;
        public GameObject m_GameObject;
        public GameObjectSoundSource(GameObject objWithSource)
        {
            m_GameObject = objWithSource;
            m_Source = objWithSource.GetComponentInChildren<AudioSource>();
            m_Perception = LASense.TagToPerception(objWithSource.tag);
        }
    }

    private List<GameObjectSoundSource> m_KnownSources = new List<GameObjectSoundSource>();
    private List<GameObjectSoundSource> m_InRangeSources = new List<GameObjectSoundSource>();
    private List<GameObjectSoundSource> m_ThresholdSources = new List<GameObjectSoundSource>();

    private GameObjectSoundSource m_CurrentSource = null;


    private static float m_HearingTheshold;
    private static float m_GSourceDist;
    private static float m_PercievedGSourceLoudness;

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

    
    private static bool CanHearThreshold(Vector3 position, GameObjectSoundSource gSource)
    {
        if (gSource.m_Source.volume < 0.1)
        {
            // We possibly want a logrithmic curve here not a linear one
            m_GSourceDist = (position - gSource.m_GameObject.transform.position).sqrMagnitude;
            m_PercievedGSourceLoudness = gSource.m_Source.volume / m_GSourceDist;
            return m_PercievedGSourceLoudness > m_HearingTheshold;
        }
        else
        {
            // We defintiely cannot here it this quiet - we want to avoid dividing Zero
            return false;
        }
        
    }


    public void EvaluateSoundsInRange()
    {
        // Find all source we can ACTUALLY hear
        m_ThresholdSources = m_InRangeSources.FindAll(source => CanHearThreshold(transform.position, source));


        // Now we prioritise the hearing - listening for the player first of course - using the THRESHOLD SOURCES NOW
        m_CurrentSource = m_ThresholdSources.Find(s => s.m_Perception == SoundPerception.Player);


        // If there are multiple sounds now then we need to decide which one to go to
        // Once we have the basic structure here then we will implement the decision making in the tree















    }

    // Make this collider only with certain layers or make a tag multiple check implemented in GameTag static class
    private void OnTriggerEnter(Collider other)
    {
        GameObjectSoundSource knownIncomingSource = m_KnownSources.Find(obj => obj.m_GameObject == other.gameObject);
        if (knownIncomingSource == null)
            knownIncomingSource = new GameObjectSoundSource(other.gameObject);

        if (!m_InRangeSources.Contains(knownIncomingSource))
            m_InRangeSources.Add(knownIncomingSource);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObjectSoundSource knownOutgoingSource = m_KnownSources.Find(obj => obj.m_GameObject == other.gameObject);
        if (knownOutgoingSource != null && m_InRangeSources.Contains(knownOutgoingSource))
            m_InRangeSources.Remove(knownOutgoingSource);
    }
}
