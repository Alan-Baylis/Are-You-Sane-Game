using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class MyHelper
{
    public static T FindComponentInChildrenWithTag<T>(this GameObject parent, string tag) where T : Component
    {
        Transform child = FindChildWithTag(parent, tag);
        if (child != null)
        {
            return child.GetComponent<T>();
        }

        Debug.Log("No Child was found in " + parent.name + " with Tag : " + tag);
        return null;
    }

    public static void ClearNull<T>(this List<T> list)
    {
        list.RemoveAll(t => t == null);
    }

    public static bool AreAll<T>(this T[] source, Func<T, bool> condition)
    {
        return source.Where(condition).Count() == source.Count();
    }

    public static bool AreAllTheSame<T>(this IEnumerable<T> source)
    {
        return source.Distinct().Count() == 1;
    }

    /// <summary>
    /// Works Oppositely Finding all of this list within the parameter given
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="containingList"></param>
    /// <param name="lookupList"></param>
    /// <returns></returns>
    public static bool ContainsAll<T>(this IEnumerable<T> containingList, IEnumerable<T> lookupList)
    {
        return !lookupList.Except(containingList).Any();
    }

    public static Transform FindChildWithTag(this GameObject parent, string tag)
    {
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.tag == tag)
            {
                return t;
            }
        }

        return null;
    }

    public static List<GameObject> FindChildrenWithTag(this GameObject parent, string tag)
    {
        List<GameObject> children = new List<GameObject>();

        Transform[] trs = (Transform[])parent.GetComponentsInChildren(typeof(Transform), true);
        foreach(Transform t in trs)
        {
            if (t.tag == tag)
            {
                children.Add(t.gameObject);
            }
        }

        return children;

    }

    public static void SetActiveChildrenWithTag(this GameObject parent, string tag, bool active)
    {
        foreach (Transform t in parent.GetComponentsInChildren(typeof(Transform), true))
        {
            if (t.tag == tag)
            {
                t.gameObject.SetActive(active);
            }
        }
    }

    public static IList<int> GetOrderedSequence(IList<int> sequence, int modulus)
    {
        if (modulus < sequence.Count)
            Debug.LogError(sequence.ToString() + "Sequence can not contain more elements than specified modulus.");

        foreach (var validSequence in getAllModulusSequences(modulus, sequence.Count))
        {
            if (validSequence.All(item => sequence.Contains(item)))
                return validSequence.ToList();
        }

        return null;
    }

    private static IEnumerable<int> ShiftLeft(IEnumerable<int> sequence)
    {
        if (!sequence.Any())
            yield break;

        foreach (var i in sequence.Skip(1))
        {
            yield return i;
        }

        yield return sequence.First();
    }

    private static IEnumerable<IEnumerable<int>> getAllModulusSequences(int modulus, int length)
    {
        var sequence = Enumerable.Range(0, modulus);

        for (var i = 0; i < modulus; i++)
        {
            yield return sequence.Take(length);
            sequence = ShiftLeft(sequence);
        }
    }
}
