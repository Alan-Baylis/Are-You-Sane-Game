


public interface IBehaviourTreeNode
{
    BehaviourTreeStatus Tick(TimeData time);
}

public interface IbehaviourWeightNode : IBehaviourTreeNode
{
    float GetWeight();
}

/// <summary>
/// Interface for behaviour tree nodes WITH weights.
/// </summary>
public interface INodeWeight
{
    /// <summary>
    /// Get the weight of this node
    /// </summary>
    /// <returns>returns weight from scale 0 - 1f</returns>
    float GetWeight();
}




