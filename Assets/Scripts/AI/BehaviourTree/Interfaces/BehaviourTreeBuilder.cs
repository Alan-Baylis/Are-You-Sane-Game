using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Fluent API for building a behaviour tree.
/// </summary>
public class BehaviourTreeBuilder
{
    /// <summary>
    /// Last node created.
    /// </summary>
    private IBehaviourTreeNode curNode = null;

    /// <summary>
    /// Stack node nodes that we are build via the fluent API.
    /// </summary>
    private Stack<IParentBehaviour> parentNodeStack = new Stack<IParentBehaviour>();

    /********************************* ACTION NODE ******************************************/

    /// <summary>
    /// Create an action node.
    /// </summary>
    public BehaviourTreeBuilder DoAction(string name, Func<TimeData, BehaviourTreeStatus> fn)
    {
        if (parentNodeStack.Count <= 0)
            Debug.LogError("Can't create an unnested ActionNode, it must be a leaf node.");
        
        parentNodeStack.Peek().AddChild(new ActionNode(name, fn));
        return this;
    }

    public BehaviourTreeBuilder DoActionWeighted(string name, Func<TimeData, BehaviourTreeStatus> fn, Func<float> weight)
    {
        if (parentNodeStack.Count <= 0)
            Debug.LogError("Can't create an unnested ActionNode, it must be a leaf node.");

        parentNodeStack.Peek().AddChild(new ActionNodeWeighted(name, fn, weight));
        return this;
    }

    /********************************* UTILITY NODE TYPES ******************************************/

    /// <summary>
    /// Like an action node... but the function can return true/false and is mapped to success/failure.
    /// </summary>
    public BehaviourTreeBuilder Condition(string name, Func<TimeData, bool> fn)
    {
        return DoAction(name, t => fn(t) ? BehaviourTreeStatus.Success : BehaviourTreeStatus.Failure);
    }

    /// <summary>
    /// Splice a sub tree into the parent tree.
    /// </summary>
    public BehaviourTreeBuilder Splice(IBehaviourTreeNode subTree)
    {
        if (subTree == null)
            Debug.LogError("Sub-Tree is NULL and cannot combine with the tree");
        
        if (parentNodeStack.Count <= 0)
            Debug.LogError("Can't splice an unnested sub-tree, there must be a parent-tree.");

        parentNodeStack.Peek().AddChild(subTree);
        return this;
    }


    /********************************* NON-WEIGHTED PARENT NODE TYPES ******************************************/

    /// <summary>
    /// Create an inverter node that inverts the success/failure of its children. Currently this Node is LIMITED to ONE child.
    /// </summary>
    public BehaviourTreeBuilder Inverter(string name)
    {
        AddParentToTop(new InverterNode(name));
        return this;
    }

    /// <summary>
    /// Create a sequence node.
    /// </summary>
    public BehaviourTreeBuilder Sequence(string name)
    {
        AddParentToTop(new SequenceNode(name));
        return this;
    }

    public BehaviourTreeBuilder RandomSequence(string name)
    {
        AddParentToTop(new RandomSequenceNode(name));
        return this;
    }

    /// <summary>
    /// Create a parallel node.
    /// </summary>
    public BehaviourTreeBuilder Parallel(string name, int numRequiredToFail, int numRequiredToSucceed) // This applies depth search
    {
        AddParentToTop(new ParallelNode(name, numRequiredToFail, numRequiredToSucceed));
        return this;
    }

    /// <summary>
    /// Create a selector node.
    /// </summary>
    public BehaviourTreeBuilder Selector(string name)
    {
        AddParentToTop(new SelectorNode(name));
        return this;
    }

    public BehaviourTreeBuilder SelectorRandom(string name)
    {
        AddParentToTop(new RandomSelectorNode(name));
        return this;
    }

    /******************************** WEIGHTED CHILDREN INVOLVED ****************************************/


    public BehaviourTreeBuilder SelectorLowestWeight(string name)
    {
        AddParentToTop(new SelectorLowestWeight(name));
        return this;
    }

    public BehaviourTreeBuilder SelectorHighestWeight(string name)
    {
        AddParentToTop(new SelectorHighestWeight(name));
        return this;
    }

    public BehaviourTreeBuilder SelectorConditionNode(string name, Func<float, bool> fn)
    {
        AddParentToTop(new SelectorConditionNode(name, fn));
        return this;
    }

    /********************************* TREE FUNCTIONALITY ******************************************/

    /// <summary>
    /// Build the actual tree.
    /// </summary>
    public IBehaviourTreeNode Build()
    {
        if (curNode == null)
            Debug.LogError("Can't create a behaviour tree with zero nodes");
        
        return curNode;
    }

    /// <summary>
    /// Ends a sequence of children.
    /// </summary>
    public BehaviourTreeBuilder End()
    {
        curNode = parentNodeStack.Pop();
        return this;
    }

    /// <summary>
    /// Function to add a recently create parent node to the top of the tree
    /// </summary>
    /// <param name="node"></param>
    private void AddParentToTop(IParentBehaviour node)
    {
        if (parentNodeStack.Count > 0)
            parentNodeStack.Peek().AddChild(node);

        parentNodeStack.Push(node);
    }

}
