using UnityEngine;
using System.Collections;

public class MenuSFX : MonoBehaviour
{
    private float soundTimer = 10f;
    private AudioSource ourAudio;
	// Use this for initialization
	void Start ()
    {
        ourAudio = GetComponent<AudioSource>();
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if (soundTimer > 0)
        {
            soundTimer -= Time.deltaTime;
            Debug.Log(soundTimer.ToString());
        }
        else
        {
            Debug.Log("Played Sound");
            ourAudio.Play();
            soundTimer = Random.Range(12, 30);
        }
	}
}
