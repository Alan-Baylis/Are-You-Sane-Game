using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BlockOcculsion : MonoBehaviour
{
    private const string m_wallTag = "Wall";
    private BlockPiece thisNode;
    private bool occluded = false;


    public bool isOccluded
    {
        get { return occluded; }
    }

    // Use this for initialization
    void Start ()
    {
        BoxCollider thisTrigger = gameObject.AddComponent<BoxCollider>();
        thisTrigger.isTrigger = true;
        thisTrigger.size = new Vector3(50f, 13f, 50f);
    }

    public void EnableOcculsion()
    {
        thisNode = GetComponentInParent<BlockPiece>();
        //HideNode();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (occluded)
            {
                ObjectReferenceSafety();
                ShowNode();
            }
        }
    }

    private void ObjectReferenceSafety()
    {
        if (thisNode == null)
        {
            //Debug.LogWarning("Occlusion Node was NULL and was set via the trigger to avoid Error");
            thisNode = GetComponentInParent<BlockPiece>();
        }
    }

    private void ShowNode()
    {
        Transform[] trs = thisNode.gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.tag == m_wallTag)
            {
                if (thisNode.isStairBlocked)
                {
                    t.gameObject.SetActive(true);
                }
            }
            else
            {
                if (t != gameObject.transform && t != thisNode.transform)
                {
                    t.gameObject.SetActive(true);
                }
            }
        }

        occluded = false;
    }

    private void HideNode()
    {
        //Transform[] trs = thisNode.gameObject.GetComponentsInChildren<Transform>(true);
        //foreach (Transform t in trs)
        //{
        //    if (t != gameObject.transform && t != thisNode.transform)
        //    {
        //        t.gameObject.SetActive(false);
        //    }
        //}

        occluded = true;
    }

    void OnTriggerEnter(Collider other)
    {
        ObjectReferenceSafety();
        if (other.gameObject.tag == "Player")
        {
            ShowNode();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            HideNode();
        }
    }
}
