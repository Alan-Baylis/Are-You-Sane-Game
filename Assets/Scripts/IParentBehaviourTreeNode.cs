

using System.Collections.Generic; // For IDictionary

/// <summary>
/// Interface for behaviour tree nodes with no weights OR children weights.
/// </summary>
public interface IParentBehaviourTreeNode : IBehaviourTreeNode
{
    /// <summary>
    /// Add a child to the parent node. Make sure the child is the correct type for its Parent.
    /// </summary>
    void AddChild<T>(T child) where T : IBehaviourTreeNode;
}


public interface IParentBehaviourNodeToWeight : IParentBehaviourTreeNode
{
    new void AddChild<U>(U child) where U : IBehaviourWeightNode;
}

/// <summary>
/// Interface for behaviour tree nodes WITH weight AND children weights.
/// </summary>
public interface IParentBehaviourWeightToWeight : IParentBehaviourNodeToWeight
{
    float GetWeight();
}



public abstract class ParentBehaviour
{
    public abstract void AddChild<T>(T child);
}

interface IParentChild
{
    void AddChild<T>(T child);
}

interface IParentChildWeight<S> : IParentChild where S : ParentBehaviour
{
    new void AddChild<R>(R child) where R : ParentBehaviour;
}

public class ParentWeight : ParentBehaviour, IParentChild
{
    public override void AddChild<T>(T child)
    {

    }
}




// For marker interfaces
interface MyInterface<T> { }

class MyClass<T, U> : MyInterface<U> { }

class OtherClass<T, U> : MyInterface<IDictionary<U, T>> { }