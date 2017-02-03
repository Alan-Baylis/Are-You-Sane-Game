
/// <summary>
/// Interface for behaviour tree nodes with NO weights.
/// </summary>
public interface IBehaviourTreeNode
{
    /// <summary>
    /// Update the time of the behaviour tree.
    /// </summary>
    BehaviourTreeStatus Tick(TimeData time);
}

/// <summary>
/// Interface for behaviour tree nodes WITH weights.
/// </summary>
public interface IBehaviourWeightNode : IBehaviourTreeNode
{
    /// <summary>
    /// Get the weight of this node
    /// </summary>
    /// <returns>returns weight from scale 0 - 1f</returns>
    float GetWeight();
}




