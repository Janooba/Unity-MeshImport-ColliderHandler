/*
 * Script by Kolton Meier
 * This will search the hierarchy of an imported model,
 * converting meshes with the colliderTag to a meshCollider.
 * Place this in an Editor folder.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class MeshImport_ColliderHandler : AssetPostprocessor
{
    private const bool tryDissolve = true; // Set this to false if you'd rather keep the hierarchy intact
    private const string colliderTag = "col_";
    private const bool isPrefix = true; // Is the colliderTag a prefix or suffix

    void OnPostprocessModel(GameObject g)
    {
        if (tryDissolve)
            DissolveColliders(g.transform);
        else
            ConvertColliders(g.transform);
    }

    /// <summary>
    /// Converts each collider in a hierarchy gameObject to a meshcollider, removing mesh renderers.
    /// </summary>
    /// <param name="t">Top level transform</param>
    private void ConvertColliders(Transform t)
    {
        var meshFilter = t.GetComponent<MeshFilter>();
        
        // Checks for object name OR mesh filter mesh name. Sometimes they are different.
        if (GetPrefixSuffix(t.name) == colliderTag || (meshFilter && GetPrefixSuffix(meshFilter.sharedMesh.name) == colliderTag))
        {
            var meshCollider = t.gameObject.AddComponent<MeshCollider>();

            GameObject.DestroyImmediate(t.gameObject.GetComponent<MeshRenderer>());
            GameObject.DestroyImmediate(t.gameObject.GetComponent<MeshFilter>());
        }

        // Do it for each child
        foreach (Transform child in t)
            ConvertColliders(child);
    }

    private string GetPrefixSuffix(string name)
    {
        if (isPrefix)
            return name.ToLower().Substring(0, 4);
        else
            return name.ToLower().Substring(name.Length - 4, 4);
    }

    /// <summary>
    /// For each mesh in a hierarchy, this combines meshes with the same name and
    /// collider tag into the given transform as a mesh collider.
    /// </summary>
    /// <param name="t">Top level transform</param>
    private void DissolveColliders(Transform t)
    {
        // Looks for name by meshfilter rather than gameobject name
        var meshFilter = t.GetComponent<MeshFilter>();
        if (meshFilter)
        {
            string name = t.GetComponent<MeshFilter>().sharedMesh.name.ToLower();

            if (string.IsNullOrEmpty(name) || GetPrefixSuffix(name) == colliderTag)
                return;

            // Looks for siblings by starting in the parent if possible
            Transform startTransform = t.parent ? t.parent : t;

            // Look for meshes with the collider name
            var colliderTransforms = GetDeepChildren(startTransform, (x, y) =>
            {
                return x == y;
            }, isPrefix ? colliderTag + name : name + colliderTag);

            // Add a meshCollider and set it to that mesh,
            // then delete it so its only used once
            foreach (var colliderTransform in colliderTransforms)
            {
                var meshCollider = t.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = colliderTransform.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(colliderTransform.gameObject);
            }
        }

        // Do it for each child
        foreach (Transform child in t)
            DissolveColliders(child);
    }

    private List<Transform> GetDeepChildren(Transform aParent, Func<string, string, bool> check, string aName)
    {
        List<Transform> foundTransforms = new List<Transform>();
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (check(c.name.ToLower(), aName.ToLower())) foundTransforms.Add(c);
            foreach (Transform t in c) queue.Enqueue(t);
        }
        return foundTransforms;
    }
}
