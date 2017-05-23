using UnityEngine;
using System.Collections;

public class Zanim : MonoBehaviour
{
    protected Animator myAnimation;

	// Use this for initialization
	void Start ()
    {
        myAnimation = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        myAnimation.SetFloat("speed", Input.GetAxis("Vertical"));
	}
}
