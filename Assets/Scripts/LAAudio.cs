using UnityEngine;
using System.Collections;

public class LAAudio : LAComponent
{
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

    private AudioSource m_source;
    private AudioClip m_currentClip = new AudioClip();

    private float m_audioTimer = 0.0f;
    private bool m_playLoopDelay;
    private float m_loopDelay = 0.0f;

    public bool isPlaying { get { return m_source.isPlaying; } }

	// Use this for initialization
	public override void Start ()
    {
        m_source = GetComponentInChildren<AudioSource>();
	}

    public void PlayFootStepAudio()
    {
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_source.volume = 0.5f;
        m_source.clip = m_FootstepSounds[n];
        m_source.PlayOneShot(m_source.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_source.clip;
    }

    private IEnumerator LoopDelay(AudioClip clip, float delay)
    {
        while (m_source.clip == clip)
        {
            yield return new WaitForSeconds(delay);

            if (!m_source.isPlaying)
            {                
                m_source.PlayOneShot(clip);
            }
        }
    }

    private void AssignClip(AudioClip clip, bool loop, float delay)
    {
        if (m_source.clip == null || m_source.clip.name != clip.name)
        {
            m_source.Stop();

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
                    m_source.clip = clip;
                    m_source.loop = true;
                    m_source.Play();
                }
            }
            else
            {
                //m_source.clip = clip;
                //m_source.Play();
                m_source.PlayOneShot(clip);
            }
        }
        
    }

    public void Cry(bool loop, float delay)
    {
        AssignClip(AUDIO_CRYING, loop, delay);
    }

    public void LaughGentle(bool loop, float delay)
    {
        AssignClip(AUDIO_LAUGH_GENTLE, loop, delay);
    }

    public void LaughJoy(bool loop, float delay)
    {
        AssignClip(AUDIO_LAUGH_JOY, loop, delay);
    }

    public void Sing(bool loop, float delay)
    {
        AssignClip(AUDIO_SING, loop, delay);
    }

    public void Scream(bool loop, float delay)
    {
        AssignClip(AUDIO_SCREAM, loop, delay);
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
