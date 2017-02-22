using UnityEngine;
using System.Collections;

public class LAAudio : LAComponent
{
    // Potentially make serialized class with audio clip and volume scale for inspector

    [SerializeField]
    private AudioClip AUDIO_CRYING;

    [SerializeField]
    private AudioClip AUDIO_LAUGH_GENTLE;

    [SerializeField]
    private AudioClip AUDIO_LAUGH_JOY;

    [SerializeField]
    private AudioClip AUDIO_SING;

    [SerializeField]
    private AudioClip AUDIO_SCREAM;

    [SerializeField]
    private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.

    [SerializeField]
    private AudioSource m_VoiceSource;

    [SerializeField]
    private AudioSource m_FeetSource;

    

    private AudioClip m_currentClip = new AudioClip();


    public AnimationCurve m_SoundFloorRollOff = new AnimationCurve();
    public AnimationCurve m_SoundReverbEffectRollOff = new AnimationCurve();


    private float m_audioTimer = 0.0f;
    private bool m_playLoopDelay;
    private float m_loopDelay = 0.0f;

    private float m_InitialVolumeFeetSource = 0.0f;
    private float m_InitialVolumeVoiceSource = 0.0f;
    private float m_VoiceVolumeScale = 1.0f;

    private float m_DirectorFeetVolume = 0.0f;
    private float m_DirectorVoiceVolume = 0.0f;


    public bool isPlaying { get { return m_VoiceSource.isPlaying; } }

	// Use this for initialization
	public override void Start ()
    {
        m_InitialVolumeFeetSource = m_FeetSource.volume;
        m_InitialVolumeVoiceSource = m_VoiceSource.volume;
    }

    public void DirectorSetEffectIntensity(float ratio)
    {
        
    }

    public void DirectorSetVolume(float closestF)
    {
        float evaluation = 0.0f;
        if (closestF > 5)
        {
            m_DirectorFeetVolume = 0f;
            m_DirectorVoiceVolume = 0f;
        }
        else
        {
            if (closestF > 0)
            {
                evaluation = 1 / (closestF * 2);
            }
            else
            {
                evaluation = 1f;
            }

            Debug.Log("Evaluation scale: " + evaluation);
            m_DirectorFeetVolume = m_InitialVolumeFeetSource * evaluation;
            m_DirectorVoiceVolume = (m_InitialVolumeVoiceSource * evaluation) * m_VoiceVolumeScale;
        }

        // Find a way to make this az smooth transition
        m_FeetSource.volume = Mathf.Lerp(m_FeetSource.volume, m_DirectorFeetVolume, 10f);
        m_VoiceSource.volume = Mathf.Lerp(m_VoiceSource.volume, m_DirectorFeetVolume, 10f);
    }

    public void PlayFootStepAudio()
    {
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_FeetSource.clip = m_FootstepSounds[n];
        m_FeetSource.PlayOneShot(m_FeetSource.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_FeetSource.clip;
    }

    private IEnumerator LoopDelay(AudioClip clip, float delay)
    {
        while (m_VoiceSource.clip == clip)
        {
            yield return new WaitForSeconds(delay);

            if (!m_VoiceSource.isPlaying)
            {                
                m_VoiceSource.PlayOneShot(clip);
            }
        }
    }

    private void AssignClip(AudioClip clip, bool loop, float delay, float volumeScale)
    {
        if (m_VoiceSource.clip == null || m_VoiceSource.clip.name != clip.name)
        {
            m_VoiceSource.Stop();
            m_VoiceVolumeScale = volumeScale;

            if (loop)
            {
                if (delay > 0)
                {
                    //m_playLoopDelay = true;
                    //m_loopDelay = delay;
                    StartCoroutine(LoopDelay(clip, delay));
                }
                else
                {
                    m_VoiceSource.clip = clip;
                    m_VoiceSource.loop = true;
                    m_VoiceSource.Play();
                }
            }
            else
            {
                //m_source.clip = clip;
                //m_source.Play();
                m_VoiceSource.PlayOneShot(clip);
            }
        }
        
    }

    public void Cry(bool loop, float delay)
    {
        AssignClip(AUDIO_CRYING, loop, delay, 1f);
    }

    public void LaughGentle(bool loop, float delay)
    {
        AssignClip(AUDIO_LAUGH_GENTLE, loop, delay, 1f);
    }

    public void LaughJoy(bool loop, float delay)
    {
        AssignClip(AUDIO_LAUGH_JOY, loop, delay, 1f);
    }

    public void Sing(bool loop, float delay)
    {
        AssignClip(AUDIO_SING, loop, delay, 0.3f);
    }

    public void Scream(bool loop, float delay)
    {
        AssignClip(AUDIO_SCREAM, loop, delay, 0.2f);
    }

    // Update is called once per frame
    public override void Update ()
    {
        
        //if (!m_source.isPlaying)
        //{
        //    if (m_audioTimer > 0)
        //    {
        //        m_audioTimer -= Time.deltaTime;
        //    }
        //    else
        //    {
        //        m_audioTimer = m_loopDelay;
        //        m_source.Play();
        //    }
        //}
    }
}
