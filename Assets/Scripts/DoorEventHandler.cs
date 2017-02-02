using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public class DoorEventHandler : MonoBehaviour
{

    public Texture2D handOpen;
    public Texture2D handGrab;

    public int textureScale = 3;

    private float xDeltaScale = 0.1f;
    private float yDeltaScale = 0.2f;
    public bool doorSelected = false;
    private bool doorHovered = false;
    private bool doorClosed = true;
    private bool joystickTriggered = false;
    private bool canInteract = true;
    private AudioSource doorSound = null;
    private GameObject doorObject = null;
    private Quaternion doorOpenAngle = Quaternion.Euler(0f, 90f, 0f);
    private Quaternion doorCloseAngle = Quaternion.Euler(0f, 0f, 0f);
    private PlayerHeuristics myStats = null;
    public SoundResources sounds;

    private float xMin = 0.0f;
    private float yMin = 0.0f;


    // Use this for initialization
    void Start ()
    {
        myStats = GetComponent<PlayerHeuristics>();
        xMin = (Screen.width / 2) - (handOpen.width / 2) + (Screen.width * xDeltaScale);
        yMin = (Screen.height / 2) - (handOpen.height / 2) + (Screen.height * yDeltaScale);

    }

    void OnGUI()
    {
        if (doorHovered)
        {
            if (doorSelected)
            {
                GUI.DrawTexture(new Rect(xMin, yMin, (handGrab.width / textureScale), (handGrab.height / textureScale)), handGrab);
            }
            else
            {
                GUI.DrawTexture(new Rect(xMin, yMin, (handOpen.width / textureScale), (handOpen.height / textureScale)), handOpen);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (Input.GetAxis("Interact") > -0.2f && !doorSelected)
        {
            if (joystickTriggered)
            {
                joystickTriggered = false;
            }

            canInteract = true;
        }

        if (Input.GetAxis("Interact") < -0.2f)
        {
            joystickTriggered = true;

            if (!doorHovered)
            {
                canInteract = false;
            } 
        }

        if (!doorSelected)
        {
            doorHovered = false;

            Vector3 myFoward = transform.TransformDirection(Vector3.forward);
            Ray ray = new Ray(transform.position, myFoward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 2f))
            {
                if (hit.collider.gameObject.tag == "Door")
                {

                    doorHovered = true;
                    if (Input.GetMouseButtonDown(0) || (Input.GetAxis("Interact") < -0.2f && canInteract))
                    {
                        Debug.Log("Hit A Door");
                        doorObject = hit.collider.gameObject;
                        doorSound = doorObject.GetComponent<AudioSource>();
                        doorSelected = true;
                        GetComponent<PlayerController>().doorGrabbed = doorSelected;
                    }
                }
            }
        }
        else
        {
            if (!Input.GetMouseButton(0) || (Input.GetAxis("Interact") > -0.2f && joystickTriggered))
            {
                doorSelected = false;
                GetComponent<PlayerController>().doorGrabbed = doorSelected;
            }
            else
            {
                float mousePull = Input.GetAxis("Mouse Y");

                if (mousePull != 0)
                {
                    Vector3 myFoward = transform.TransformDirection(Vector3.forward);

                    if (myStats.BlockPosition != null)
                    {
                        if (!myStats.BlockPosition.isRoom) // If we are in corridor
                        {
                            if (mousePull > 0)
                            {
                                doorObject.transform.localRotation = Quaternion.Slerp(doorObject.transform.localRotation, doorOpenAngle, mousePull * 2 * Time.deltaTime);

                                if (doorClosed)
                                {
                                    doorClosed = false;
                                    // Play Door Open Sound
                                    int randomSoundIndex = Random.Range(0, sounds.openDoors.Length);
                                    doorSound.clip = sounds.openDoors[randomSoundIndex];
                                    doorSound.Play();
                                }
                            }

                            if (mousePull < 0)
                            {
                                mousePull *= -1;
                                doorObject.transform.localRotation = Quaternion.Slerp(doorObject.transform.localRotation, doorCloseAngle, mousePull * 2 * Time.deltaTime);

                                if (Quaternion.Angle(doorObject.transform.localRotation, doorCloseAngle) < 5f)
                                {
                                    doorClosed = true;
                                    // Play Door Closed Sound
                                    int randomSoundIndex = Random.Range(0, sounds.closeDoors.Length);
                                    doorSound.clip = sounds.closeDoors[randomSoundIndex];
                                    doorSound.Play();
                                }

                            }
                        }
                        else // If we are in room
                        {
                            if (mousePull > 0)
                            {
                                doorObject.transform.localRotation = Quaternion.Slerp(doorObject.transform.localRotation, doorCloseAngle, mousePull * 4 * Time.deltaTime);
                                if (Quaternion.Angle(doorObject.transform.localRotation, doorCloseAngle) < 5f)
                                {
                                    doorClosed = true;
                                    // Play Door Closed Sound
                                    int randomSoundIndex = Random.Range(0, sounds.closeDoors.Length);
                                    doorSound.clip = sounds.closeDoors[randomSoundIndex];
                                    doorSound.Play();
                                }

                            }

                            if (mousePull < 0)
                            {
                                mousePull *= -1;
                                doorObject.transform.localRotation = Quaternion.Slerp(doorObject.transform.localRotation, doorOpenAngle, mousePull * 4 * Time.deltaTime);
                                if (doorClosed)
                                {
                                    doorClosed = false;
                                    // Play Door Open Sound
                                    int randomSoundIndex = Random.Range(0, sounds.openDoors.Length);
                                    doorSound.clip = sounds.openDoors[randomSoundIndex];
                                    doorSound.Play();
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Players Node has not been Recorded!");
                    }
                    
                }
            }
        }
	    
	}
}
