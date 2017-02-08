using System;

/// <summary>
/// Interface for All behaviour tree nodes.
/// </summary>
public interface IBehaviourTreeNode
{
    /// <summary>
    /// Tick through this node on the tree.
    /// </summary>
    /// <param name="time"></param>
    /// <returns>Returns the status used to traverse the tree</returns>
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




