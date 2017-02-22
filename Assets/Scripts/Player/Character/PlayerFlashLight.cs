using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerFlashLight : PlayerComponent
{
    public AudioClip toggleLight;
    public AudioClip toggleBeam;

    private const float switchCooldown = 0.15f;
    private float extensionTimer = 0.0f;
    private float toggleTimer = 0.0f;

    private AudioSource flashSound;
    private Light flashLight;
    private float originAngle;
    private float originRange;
    private bool flashOn = false;
    private bool flashExtended = false;

    private LayerMask maskLayer;
    public string[] maskLayersString;

    // Use this for initialization
    public override void Start ()
    {
        //flashLight = GetComponentInChildren<Light>();
        //flashSound = flashLight.gameObject.GetComponent<AudioSource>();
        //InitializeExclusionLayers();
        //TurnFlashOff();
	}

    private void InitializeExclusionLayers()
    {
        maskLayer = LayerMaskExtensions.Create("Ignore Raycast");
        foreach (string mask in maskLayersString) { maskLayer = maskLayer.AddToMask(mask); }
        maskLayer = maskLayer.Inverse();
    }

    private void TurnFlashOn()
    {
        flashLight.intensity = (flashExtended) ? 2.5f : 1f;
    }

    private void TurnFlashOff()
    {
        flashLight.intensity = 0;
    }

    private void FlashExtend()
    {
        flashLight.range = 37f;
        flashLight.spotAngle = 20f;
    }

    private void FlashReturn()
    {
        flashLight.range = 18f;
        flashLight.spotAngle = 77f;
    }

    private void HandleFlashExtension()
    {
        flashSound.clip = toggleBeam;
        flashSound.Play();
        flashExtended = !flashExtended;
        (flashExtended ? (Action)FlashExtend : FlashReturn)();
    }

    public override void FixedUpdate()
    {
        if (!m_player.IsActivated) return;
        if (flashOn) { HitEnemies(); }
    }

    private void HitEnemies()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, flashLight.range);
        foreach (Collider col in cols)
        {
            if (col.gameObject.tag == GameTag.Annie)
            {
                LAObject annie = col.gameObject.GetComponent<LAObject>();
                if (annie != null && (annie.Movement.currentFloor != m_player.Heuristics.CurrentFloor))
                    continue;

                Vector3 dir = col.gameObject.transform.position - transform.position;
                Vector3 myForward = flashLight.transform.TransformDirection(Vector3.forward);

                // Divide 2 becasue we need half the checking diameter either side of point of interest - the zombie
                // Transform forward raycasts to the epicentre of circle - so we need the radius
                if (Vector3.Angle(dir, myForward) <= flashLight.spotAngle/2)
                {

                    Debug.Log("Annie In Angle");
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, myForward, out hit, flashLight.range, maskLayer))
                    {
                        if (hit.transform.tag == GameTag.Annie)
                        {
                            hit.transform.gameObject.GetComponent<LAObject>().Sense.Startle(transform.position, StartleEvent.PlayerFlashLight);
                        }
                    }
                }
            }
        }
    }


    private void HandleFlashLight()
    {
        flashSound.clip = toggleLight;
        flashSound.Play();
        flashOn = !flashOn;
        (flashOn ? (Action)TurnFlashOn : TurnFlashOff)();
    }
	
	// Update is called once per frame
	public override void Update ()
    {
        if (!m_player.IsActivated) return;

	    if (extensionTimer > 0)
        {
            extensionTimer -= Time.deltaTime;
        }
        else
        {
            if (Input.GetMouseButtonDown(1) || Input.GetButton("LightBeam"))
            {
                if (flashOn)
                {
                    HandleFlashExtension();
                    extensionTimer = switchCooldown;
                }
            }
        }

        if (toggleTimer > 0)
        {
            toggleTimer -= Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetButton("LightToggle"))
            {
                HandleFlashLight();
                toggleTimer = switchCooldown;
            }
        }
	}
}
