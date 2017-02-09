﻿using UnityEngine;
using UnityEngine.Serialization;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : PlayerComponent
{
    public Camera m_Camera;
    public MovementSettings movementSettings = new MovementSettings();
    public MouseLook mouseLook = new MouseLook();
    public AdvancedSettings advancedSettings = new AdvancedSettings();

    private Rigidbody m_RigidBody;
    private CapsuleCollider m_Capsule;
    private float m_YRotation;
    private Vector3 m_GroundContactNormal;
    private bool m_Jump;
    private bool m_PreviouslyGrounded;
    private bool m_Jumping;
    private bool m_IsGrounded;

    [SerializeField] [Range(0f, 1f)]private float m_RunstepLenghten;


    [SerializeField]
    private bool m_UseFovKick;
    [SerializeField]
    private FOVKick m_FovKick = new FOVKick();
    [SerializeField]
    private bool m_UseHeadBob;
    [SerializeField]
    private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField]
    private LerpControlledBob m_JumpBob = new LerpControlledBob();

    private Vector3 m_OriginalCameraPosition;

    public AnimationCurve m_AudioCurveModifier;

    [SerializeField] private float m_StepInterval;
    [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
    [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
    [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

    private float m_AudioLevelMax;

    private float m_StepCycle;
    private float m_NextStep;
    private AudioSource m_AudioSource;
    public bool doorGrabbed;

    public MouseLook playerLook { get { return mouseLook; } }

    public Vector3 Velocity     { get { return m_RigidBody.velocity; } }

    public bool Grounded        { get { return m_IsGrounded; } }

    public bool Jumping         { get { return m_Jumping; } }

    public bool Running         { get { return movementSettings.Running; } }

    public override void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();
        m_Capsule = GetComponent<CapsuleCollider>();
        m_Camera = Camera.main;
        m_OriginalCameraPosition = m_Camera.transform.localPosition;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera, m_StepInterval);
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_AudioSource = GetComponent<AudioSource>();
        m_AudioLevelMax = m_AudioSource.volume;
        doorGrabbed = false;
        mouseLook.Init(transform, m_Camera.transform);
    }

    public void DisableMouseLock()
    {
        mouseLook.lockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PlayLandingSound()
    {
        m_AudioSource.clip = m_LandSound;
        m_AudioSource.Play();
        m_NextStep = m_StepCycle + 0.5f;
    }

    private void PlayJumpSound()
    {
        m_AudioSource.clip = m_JumpSound;
        m_AudioSource.Play();
    }

    [System.Serializable]
    public class MovementSettings
    {        
        public float ForwardSpeed = 8.0f;   // Speed when walking forward
        public float BackwardSpeed = 4.0f;  // Speed when walking backwards
        public float StrafeSpeed = 4.0f;    // Speed when walking sideways
        public float RunMultiplier = 2.0f;  // Speed when sprinting
        public KeyCode RunKey = KeyCode.LeftShift;
        public float JumpForce = 30f;
        public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
        [HideInInspector]
        public float CurrentTargetSpeed = 8f;

        private bool m_Running;
        public bool Running { get { return m_Running; } }
        public void UpdateDesiredTargetSpeed(Vector2 input)
        {
            if (input == Vector2.zero) return;
            if (input.x > 0 || input.x < 0) { CurrentTargetSpeed = StrafeSpeed; }
            if (input.y < 0) { CurrentTargetSpeed = BackwardSpeed; }
            if (input.y > 0) { CurrentTargetSpeed = ForwardSpeed; }
            if (Input.GetKey(RunKey) || Input.GetButton("Sprint"))
            {
                CurrentTargetSpeed *= RunMultiplier;
                m_Running = true;
            }
            else
            {
                m_Running = false;
            }
        }

        public float CurrentSpeedPercent()
        {            
            return (float)(CurrentTargetSpeed / (ForwardSpeed * RunMultiplier));
        }
    }


    private void PlayFootStepAudio()
    {
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        Debug.Log(movementSettings.CurrentSpeedPercent());
        m_AudioSource.volume = m_AudioCurveModifier.Evaluate(movementSettings.CurrentSpeedPercent());
        m_AudioSource.clip = m_FootstepSounds[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_AudioSource.clip;
    }

    [System.Serializable]
    public class AdvancedSettings
    {
        public float groundCheckDistance = 0.01f;               // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
        public float stickToGroundHelperDistance = 0.5f;        // stops the character
        public float slowDownRate = 20f;                        // rate at which the controller comes to a stop when there is no input
        public bool airControl;                                 // can the user control the direction that is being moved in the air
        [Tooltip("set it to 0.1 or more if stuck in wall")]
        public float shellOffset;                               //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
    }

    public override void Update()
    {
        if (!m_player.IsActivated) return;

        RotateView();
        if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump)
        {
            PlayJumpSound();
            m_Jump = true;
        }
    }

    private void ProgressStepCycle(Vector2 input)
    {
        if (m_RigidBody.velocity.sqrMagnitude > 0 && (input.x != 0 || input.y != 0))
            m_StepCycle += (m_RigidBody.velocity.magnitude + (movementSettings.CurrentTargetSpeed * (!movementSettings.Running ? 1f : m_RunstepLenghten))) * Time.fixedDeltaTime;
        
        if (!(m_StepCycle > m_NextStep)) return;
        m_NextStep = m_StepCycle + m_StepInterval;
        PlayFootStepAudio();
    }

    public override void FixedUpdate()
    {
        if (!m_player.IsActivated) return;

        GroundCheck();
        Vector2 input = GetInput();

        if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded))
        {
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = m_Camera.transform.forward * input.y + m_Camera.transform.right * input.x;
            desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

            desiredMove.x = desiredMove.x * movementSettings.CurrentTargetSpeed;
            desiredMove.z = desiredMove.z * movementSettings.CurrentTargetSpeed;
            desiredMove.y = desiredMove.y * movementSettings.CurrentTargetSpeed;
            if (m_RigidBody.velocity.sqrMagnitude < (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
            {
                m_RigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
            }
        }

        if (m_IsGrounded)
        {
            m_RigidBody.drag = 5f;

            if (m_Jump)
            {
                m_RigidBody.drag = 0f;
                m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
                m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                m_Jumping = true;
            }

            if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && m_RigidBody.velocity.magnitude < 1f)
                m_RigidBody.Sleep();
        }
        else
        {
            m_RigidBody.drag = 0f;
            if (m_PreviouslyGrounded && !m_Jumping)
                StickToGroundHelper();
            
        }

        m_Jump = false;
        ProgressStepCycle(input);
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        Vector3 newCameraPosition;
        if (!m_UseHeadBob) return;
        if (m_RigidBody.velocity.magnitude > 0)
        {
            m_Camera.transform.localPosition = m_HeadBob.DoHeadBob(m_RigidBody.velocity.magnitude + (movementSettings.CurrentTargetSpeed * (!movementSettings.Running ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }

        m_Camera.transform.localPosition = newCameraPosition;
    }

    private float SlopeMultiplier()
    {
        float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
        return movementSettings.SlopeCurveModifier.Evaluate(angle);
    }


    private void StickToGroundHelper()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                ((m_Capsule.height / 2f) - m_Capsule.radius) +
                                advancedSettings.stickToGroundHelperDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
        }
    }


    private Vector2 GetInput()
    {
        Vector2 input = new Vector2
        {
            x = CrossPlatformInputManager.GetAxis("Horizontal"),
            y = CrossPlatformInputManager.GetAxis("Vertical")
        };

        movementSettings.UpdateDesiredTargetSpeed(input);
        return input;
    }


    private void RotateView()
    {
        if (!doorGrabbed)
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;
            mouseLook.LookRotation(transform, m_Camera.transform);
            if (m_IsGrounded || advancedSettings.airControl)
            {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                m_RigidBody.velocity = velRotation * m_RigidBody.velocity;
            }
        }
    }

    /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
    private void GroundCheck()
    {
        m_PreviouslyGrounded = m_IsGrounded;
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            m_IsGrounded = true;
            m_GroundContactNormal = hitInfo.normal;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundContactNormal = Vector3.up;
        }

        if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
        {
            PlayLandingSound();
            m_Jumping = false;
        }
    }
    
}