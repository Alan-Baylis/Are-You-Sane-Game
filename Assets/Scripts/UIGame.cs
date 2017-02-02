using UnityEngine;
using System.Collections;

public class UIGame : MonoBehaviour
{
    public GameTimer GameTime;
    public GameObject BatteryUI;
    public UIGameMessenger Messenger;
    public void Toggle(bool active) { this.gameObject.SetActive(active); }


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
