using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieMenuMove : MonoBehaviour
{
    protected Animator myAnimation;

    public GameObject[] leftProps;
    public GameObject[] rightProps;
    public GameObject centreProp;


    public GameObject[] spawnNodes;
    public GameObject[] finishNodes;
    private GameObject endNode;

    private List<GameObject> allProps = new List<GameObject>();

    private void StartAtRandomNode()
    {
        DisableAllProps();

        int randomStart = Random.Range(0, spawnNodes.Length);
        transform.position = spawnNodes[randomStart].transform.position;

        int randomEnd = Random.Range(0, finishNodes.Length);
        endNode = finishNodes[randomEnd];
        transform.LookAt(endNode.transform.position, Vector3.up);

        if (randomStart != 1)
        {
            int randomProp = Random.Range(0, 2);
            if (randomEnd == 0)
            {
                rightProps[randomProp].SetActive(true);
            }
            else
            {
                leftProps[randomProp].SetActive(true);
            }
        }
        else
        {
            centreProp.SetActive(true);
        }
    }

    private int OppositeBinaryIndex(int n)
    {
        if (n == 0)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    private void DisableAllProps()
    {
        for (int i = 0; i < allProps.Count; i++)
        {
            allProps[i].SetActive(false);
        }
    }



	// Use this for initialization
	void Start ()
    {
        allProps.AddRange(leftProps);
        allProps.AddRange(rightProps);
        allProps.Add(centreProp);

        myAnimation = GetComponent<Animator>();
        myAnimation.SetFloat("speed", 1f);
        StartAtRandomNode();
    }
	
	// Update is called once per frame
	void Update ()
    {
        transform.position = Vector3.MoveTowards(transform.position, endNode.transform.position, 2 * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Wall")
        {
            StartAtRandomNode();
        }
    }
}
