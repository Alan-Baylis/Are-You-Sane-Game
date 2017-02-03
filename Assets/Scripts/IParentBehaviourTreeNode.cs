

using System;
using System.Collections.Generic; // For IDictionary


interface IParentBehaviour
{
    void AddChild<T>(T child) where T : IBehaviourTreeNode;
}


//where NodeType : IBehaviourTreeNode
public abstract class ParentBehaviourNode<ChildType> : IParentBehaviour, IBehaviourTreeNode where ChildType : IBehaviourTreeNode 
{
    private ChildType ChildrenType { get; set; }
    public abstract BehaviourTreeStatus Tick(TimeData time);
    public abstract void AddChild<U>(U child) where U : ChildType;
    void IParentBehaviour.AddChild<T>(T child)
    {
        AddChild((ChildType)(IBehaviourTreeNode)child);
    }
}

public abstract class ParentBehaviourWeighted : ParentBehaviourNode<IbehaviourWeightNode>
{
    public abstract float GetWeight();
}


//// For marker interfaces
//interface MyInterface<T> { }

//class MyClass<T, U> : MyInterface<U> { }

//class OtherClass<T, U> : MyInterface<IDictionary<U, T>> { }