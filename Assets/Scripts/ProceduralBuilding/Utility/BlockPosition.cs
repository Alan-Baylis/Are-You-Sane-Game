using UnityEngine;
using System.Collections;

public class BlockPosition : MonoBehaviour
{
    private BlockPiece m_Node;

    void OnTriggerEnter(Collider other) // This is handled here so that the Trigger Enter funciton isnt being called loads by the player or the AI
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PlayerHeuristics>().BlockPosition = m_Node;
            //Debug.Log("Player Poisition: " + m_Node + " ------------------------------------------");
            PAIG.AIDirector.MovementTriggerCallBack();
        }
        else if (other.gameObject.tag == "Zombie")
        {
            other.gameObject.GetComponent<ZombieTracker>().onNode = m_Node.gameObject;
        }
        else if (other.gameObject.tag == "AnnieAI")
        {
            other.gameObject.GetComponent<LAObject>().Movement.SetNodePosition(m_Node);
            PAIG.AIDirector.MovementTriggerCallBack();
        }
    }

	// Use this for initialization
	void Start ()
    {
        m_Node = GetComponentInParent<BlockPiece>();
        BoxCollider thisTrigger = gameObject.AddComponent<BoxCollider>();
        thisTrigger.isTrigger = true;
        thisTrigger.size = new Vector3(3.5f, 1f, 3.5f);
        //this.gameObject.layer = 8;
    }

}
