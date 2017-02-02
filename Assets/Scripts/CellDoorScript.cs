using UnityEngine;
using System.Collections;

public class CellDoorScript : MonoBehaviour
{
    public Transform m_DoorHinge;

    public bool Locked = false;
    public bool isOpened { get { return m_opened; } } // Handled by the interactor script
    public bool InTransition { get { return m_transition; } } // Handled by the interactor script

    private bool m_opened = false;
    private bool m_transition = false;

    private Quaternion m_rotOpen = Quaternion.Euler(0f, 90f, 0f);
    private Quaternion m_rotClose = Quaternion.Euler(Vector3.zero);
    private Quaternion m_currentRot;

    public bool AttemptToOpen() // This will only happed if the door is not opened (rule is out of checks here)
    {
        if (Locked)
        {
            return false;
        }
        else
        {
            m_currentRot = (m_opened) ? m_rotClose : m_rotOpen;
            m_transition = true;
            return true;
        }
    }

    // Use this for initialization
    void Start ()
    {
        
       
	      
	}
	
	// Update is called once per frame
	void Update ()
    {
	    if (m_transition)
        {
            if (Quaternion.Angle(m_DoorHinge.localRotation, m_currentRot) < 5f)
            {
                m_transition = false;
                m_opened = !m_opened;                
            }

            m_DoorHinge.localRotation = Quaternion.Slerp(m_DoorHinge.localRotation, m_currentRot, 2 * Time.deltaTime); // Apply hinge motion inheritance
        }
	}
}
