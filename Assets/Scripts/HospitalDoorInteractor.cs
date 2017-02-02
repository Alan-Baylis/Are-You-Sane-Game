using UnityEngine;
using System.Collections;

public class HospitalDoorInteractor : MonoBehaviour
{
    public HospitalDoorScript DoorScript;
    private bool m_playerInRange = false;
    private BoxCollider m_trigger;
    private PlayerObject m_player = null;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            if (m_player == null)
                m_player = other.transform.GetComponent<PlayerObject>();

            if (DoorScript.isOpened)
            {
                m_trigger.enabled = false;
            }
            else
            {
                m_playerInRange = true;
                m_player.UI.PromptDoorMessage();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            m_playerInRange = false;
            m_player.UI.ClearInteractionMessage();
        }
    }

	// Use this for initialization
	void Start ()
    {
        m_trigger = GetComponent<BoxCollider>();
	}
	
    private IEnumerator EscapeDoor()
    {
        yield return new WaitForSeconds(0.5f);
        m_player.PlayerEnd(EndCondition.Escape);
    }

	// Update is called once per frame
	void Update ()
    {
	    if (m_playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (DoorScript.AttemptToOpen())
                {
                    m_player.UI.ClearInteractionMessage();
                    if (DoorScript.IsExit)
                        StartCoroutine(EscapeDoor());
                }
                else
                {
                    m_player.UI.LockedDoorMessage();
                }
            }
        }
	}
}
