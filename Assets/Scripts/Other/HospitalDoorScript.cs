using UnityEngine;
using System.Collections;

public class HospitalDoorScript : MonoBehaviour
{
    public Transform m_BackPanel;
    public bool IsExit = false;
    public bool Locked = false;
    public Transform Hinge_L;
    public Transform Door_L;

    public Transform Hinge_R;
    public Transform Door_R;

    private bool m_opened = false;
    private bool m_transition = false;

    private Quaternion m_rotHingeLOpen = Quaternion.Euler(0f, -105f, 0f);
    private Quaternion m_rotHingeROpen = Quaternion.Euler(0f, 105f, 0f);    
    private Quaternion m_rotHingeBClose = Quaternion.Euler(0f, 0f, 0f);
    private float m_rotationSpeed = 2f;

    public bool isOpened { get { return m_opened; } } // Handled by the interactor script

	// Use this for initialization
	void Start ()
    {
        Door_L.SetParent(Hinge_L);
        Door_R.SetParent(Hinge_R);	
	}

    private void DisableBackPanel()
    {
        m_BackPanel.gameObject.SetActive(false);
    }

    public bool AttemptToOpen() // This will only happed if the door is not opened (rule is out of checks here)
    {
        if (Locked)
        {
            return false;
        }
        else
        {
            m_transition = true;

            if (!IsExit)
                DisableBackPanel();

            return true;
        }
    }

    void ApplyHingeMotion(Transform hinge, Quaternion rot)
    {
        hinge.localRotation = Quaternion.Slerp(hinge.localRotation, rot, m_rotationSpeed * Time.deltaTime);
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if (m_transition)
        {
            if (Quaternion.Angle(Hinge_L.localRotation, m_rotHingeLOpen) < 5f && Quaternion.Angle(Hinge_R.localRotation, m_rotHingeROpen) < 5f)
            {
                m_transition = false;
                m_opened = true;
            }

            ApplyHingeMotion(Hinge_L, m_rotHingeLOpen);
            ApplyHingeMotion(Hinge_R, m_rotHingeROpen);
        }
	}
}
