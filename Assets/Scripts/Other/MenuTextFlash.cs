using UnityEngine;
using System.Collections;

public class MenuTextFlash : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        GetComponent<Animator>().SetBool("flash", true);
	}
	
}
