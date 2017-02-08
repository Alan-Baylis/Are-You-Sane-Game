using UnityEngine;
using System.Collections;

public class LAAttack : LAComponent
{
    private bool m_aimingAttack = false;
    private bool m_attacking = false;

    public bool isAttacking
    {
        get { return m_attacking; }
    }

    // Use this for initialization
    public override void Start()
    {

    }

    // Update is called once per frame
    public override void Update()
    {

    }

    private IEnumerator attackDelay()
    {
        Annie.Audio.Scream(false, 0.0f);
        yield return new WaitForSeconds(2f);
        m_attacking = false;
    }

    public bool Aiming
    {
        get { return m_aimingAttack; }
    }

    public bool AimAttack(Vector3 targetPosition)
    {
        //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, )
        return false;
    }

    public BehaviourTreeStatus SlapPlayer()
    {
        if (!m_attacking)
        {
            m_attacking = true;
            Annie.Animation.oneshot_RegAttack(true);
            StartCoroutine(attackDelay());
            return BehaviourTreeStatus.Success;
        }
        else
        {
            return BehaviourTreeStatus.Running;
        }
    }

    public void Slap()
    {
        if (!m_attacking)
        {
            m_attacking = true;
            Annie.Animation.oneshot_RegAttack(true);
            StartCoroutine(attackDelay());
        }        
    }
}
