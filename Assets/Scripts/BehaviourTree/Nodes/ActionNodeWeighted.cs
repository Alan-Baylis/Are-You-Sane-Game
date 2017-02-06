using System;

public class ActionNodeWeighted : ActionNode, IBehaviourWeightNode
{
    private Func<float> m_weightCalculation;

    public ActionNodeWeighted(string name, Func<TimeData, BehaviourTreeStatus> fn, Func<float> weight) : base (name, fn)
    {
        m_weightCalculation = weight;
    }

    public float GetWeight()
    {
        return m_weightCalculation.Invoke();
    }
}
