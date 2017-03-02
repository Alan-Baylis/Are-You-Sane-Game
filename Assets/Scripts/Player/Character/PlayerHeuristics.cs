using UnityEngine;
using System.Collections;

public class PlayerHeuristics : PlayerComponent
{
    private int m_currentFloor = 0;
    private BlockPiece m_currentBlockPosition;
    private int m_insanity = 0;

    public int CurrentFloor
    {
        get { return m_currentFloor;  }
        set { m_currentFloor = value; }
    }

    public BlockPiece BlockPosition
    {
        get { return m_currentBlockPosition;  }
        set { m_currentBlockPosition = value; }
    }

    public void IncreaseInsanity()
    {
        // When insanity reaches a certain level the player will lose the game - essentially matching with a time? as it will always periodically increase
        Debug.Log("Insanity Increased On Player");
        m_insanity++;

        if (m_insanity >= (int)GameData.Difficulty)
        {
            Debug.Log("Player Has Died From Insanity");
            m_Player.PlayerEnd(EndCondition.Insane);
        }
    }

	// Use this for initialization
	public override void Start ()
    {
	
	}
	
	// Update is called once per frame
	public override void Update ()
    {
	
	}
}
