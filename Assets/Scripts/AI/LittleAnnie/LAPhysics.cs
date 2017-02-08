using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LAPhysics : LAComponent
{
    private Rigidbody m_rigidbody;
    private PlayerHeuristics m_heuristicTemp;
    private List<Transform> m_transformChildren;
    private bool m_occluded = false;
    private RigidbodyConstraints m_originConstraints;

    

    public bool isOccluded { get { return m_occluded; } }

    public Rigidbody LArigidbody { get { return m_rigidbody; } }

    public void SetHeuristicTemp()
    {
        m_heuristicTemp = player.GetComponentInChildren<PlayerHeuristics>();
    }

    // Use this for initialization
    public override void Start()
    {
        m_rigidbody = GetComponentInChildren<Rigidbody>();
        m_transformChildren = GetComponentsInChildren<Transform>(true).ToList();
        m_transformChildren.RemoveAll(t => t == this.transform);
        m_originConstraints = m_rigidbody.constraints;
    }

    /// <summary>
    /// Self Safety Handles Reset. If there is change we will make change
    /// </summary>
    public void ResetConstraints()
    {
        if (m_rigidbody.constraints != m_originConstraints)
            m_rigidbody.constraints = m_originConstraints;
    }

    private void TransformOcclusionToggle(bool setActive)
    {
        foreach (Transform child in m_transformChildren)
        {
            child.gameObject.SetActive(setActive);
        }
    }

    public void EnableGravity()
    {
        if (!m_rigidbody.useGravity)
            m_rigidbody.useGravity = true;
    }

    public void DisableGravity()
    {
        if (m_rigidbody.useGravity)
            m_rigidbody.useGravity = false;
    }

    // Update is called once per frame
    public override void Update()
    {
        if (ActiveComponent)
        {
            if (Vector3.Distance(player.transform.position, transform.position) > 38f)
            {
                if (!m_occluded)
                {
                    TransformOcclusionToggle(false);
                    m_occluded = true;
                }
            }
            else
            {
                if (m_occluded)
                {
                    TransformOcclusionToggle(true);
                    Annie.Animation.PlayAnimation(); // Must reinitialize the animation after toggling the transform and meshes
                    m_occluded = false;
                }
            }
        }

    }
}
