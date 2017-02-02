using UnityEngine;
using System.Collections;


public class LAAnimation : LAComponent
{
    public const string ANIMATION_WALK         = "Walk";
    public const string ANIMATION_CRAWL        = "Crawl";
    public const string ANIMATION_STM_FORWARD  = "StumbleForward";
    public const string ANIMATION_STRAFE_LEFT  = "StrafeLeft";
    public const string ANIMATION_IDLE         = "Idle";
    public const string ANIMATION_STRAFE_RIGHT = "StrafeRight";
    public const string ANIMATION_REG_ATTACK   = "RegularAttack";
    public const string ANIMATION_JUMP_ATTACK  = "JumpAttack";
    public const string ANIMATION_STM_BACKWARD = "StumbleBackward";
    public const string ANIMATION_SCREAM       = "Scream";


    public const string CONDITION_WALK = "walk";
    public const string CONDITION_CRAWL = "";
    public const string CONDITION_STM_FORWARD = "";
    public const string CONDITION_STRAFE_LEFT = "";
    public const string CONDITION_IDLE = "";
    public const string CONDITION_STRAFE_RIGHT = "";
    public const string CONDITION_REG_ATTACK = "autoAttackRange";
    public const string CONDITION_JUMP_ATTACK = "";
    public const string CONDITION_STM_BACKWARD = "";
    public const string CONDITION_SCREAM = "";


    public string currentAnimationName { get { return m_animationString; } }

    private string m_animationString = string.Empty;
    private Animator m_anim;

    public Animator Controller
    {
        get { return m_anim; }
    }

    public void Walk(bool setActive)
    {
        //SetAnimationCondition(CONDITION_WALK, setActive);
        SetAnimation(ANIMATION_WALK);
    }

    public void Crawl()
    {
        //SetAnimation(ANIMATION_CRAWL);
    }

    public void StrafeLeft()
    {
        //SetAnimation(ANIMATION_STRAFE_LEFT);
    }

    public void StrafeRight()
    {
        //SetAnimation(ANIMATION_STRAFE_RIGHT);
    }

    public void Idle()
    {
        //SetAnimation(ANIMATION_IDLE);
    }

    public void oneshot_RegAttack(bool setActive)
    {
        SetAnimation(ANIMATION_REG_ATTACK);
        //SetAnimationCondition(CONDITION_REG_ATTACK, setActive);
    }

    private void SetAnimationCondition(string condition, bool state)
    {
        if (m_anim.GetBool(condition) != state)
            m_anim.SetBool(condition, state);
    }

    private void SetAnimation(string animName)
    {
        if (m_animationString != animName)
        {
            m_animationString = animName;
            PlayAnimation();
        }
        else
        {
            if (this.m_anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !m_anim.IsInTransition(0))
            {
                //Vector3 relativePlayerPosition = new Vector3(player.transform.position.x, Annie.Sense.LastSeenPlayerNode.transform.position.y, player.transform.position.z);
                //this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, Annie.Movement.DirectionRotation(relativePlayerPosition), 5f);
                PlayAnimation();
            }
        }
    }

    // Use this for initialization
    public override void Start()
    {
        m_anim = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Play the currently stored animation
    /// </summary>
    public void PlayAnimation()
    {
        if (m_animationString != string.Empty)
        {
            m_anim.Play(m_animationString, -1, 0f);
        }
    }

    // Update is called once per frame
    public override void Update()
    {

    }
}
