using UnityEngine;
using System.Collections;

public class BlockPosition : MonoBehaviour
{
    private BlockPiece thisNode;

    void OnTriggerEnter(Collider other) // This is handled here so that the Trigger Enter funciton isnt being called loads by the player or the AI
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PlayerHeuristics>().BlockPosition = thisNode;
            Debug.Log("Player Poisition: " + thisNode + " ------------------------------------------");
            PAIG.AIDirector.MovementTriggerCallBack();
        }
        else if (other.gameObject.tag == "Zombie")
        {
            other.gameObject.GetComponent<ZombieTracker>().onNode = thisNode.gameObject;
        }
        else if (other.gameObject.tag == "AnnieAI")
        {
            other.gameObject.GetComponent<LAObject>().Movement.SetNodePosition(thisNode);
            PAIG.AIDirector.MovementTriggerCallBack();
        }
    }

	// Use this for initialization
	void Start ()
    {
        thisNode = GetComponentInParent<BlockPiece>();
        BoxCollider thisTrigger = gameObject.AddComponent<BoxCollider>();
        thisTrigger.isTrigger = true;
        thisTrigger.size = new Vector3(3.5f, 1f, 3.5f);
        //this.gameObject.layer = 8;
    }

    // Update is called once per frame
    void Update ()
    {
	
	}
}
