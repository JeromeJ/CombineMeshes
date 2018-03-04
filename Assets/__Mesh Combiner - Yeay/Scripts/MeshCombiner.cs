using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using ImportNinja;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : AdvancedBehaviour
{
    #region Public Members

    public float _Test = 10;

    #endregion

    #region Public void

    public void CombineMeshes()
    {
        using (new UndoGroup("Combined meshes"))
        {
            _HideChildren();

            using (new MoveToIdentity(gameObject))
                _CombineMeshes();
        }
    }

    public void AdvancedMerge()
    {
        // All our children (and us)
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);

        // All the meshes in our children (just a big list)
        List<Material> materials = new List<Material>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false); // <-- you can optimize this
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform == transform)
                continue;
            Material[] localMats = renderer.sharedMaterials;
            foreach (Material localMat in localMats)
                if (!materials.Contains(localMat))
                    materials.Add(localMat);
        }

        // Each material will have a mesh for it.
        List<Mesh> submeshes = new List<Mesh>();
        foreach (Material material in materials)
        {
            // Make a combiner for each (sub)mesh that is mapped to the right material.
            List<CombineInstance> combiners = new List<CombineInstance>();
            foreach (MeshFilter filter in filters)
            {
                if (filter.transform == transform) continue;
                // The filter doesn't know what materials are involved, get the renderer.
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();  // <-- (Easy optimization is possible here, give it a try!)
                if (renderer == null)
                {
                    Debug.LogError(filter.name + " has no MeshRenderer");
                    continue;
                }

                // Let's see if their materials are the one we want right now.
                Material[] localMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < localMaterials.Length; materialIndex++)
                {
                    if (localMaterials[materialIndex] != material)
                        continue;
                    // This submesh is the material we're looking for right now.
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        subMeshIndex = materialIndex,
                        transform = Matrix4x4.identity
                    };
                    combiners.Add(ci);
                }
            }
            // Flatten into a single mesh.
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combiners.ToArray(), true);
            submeshes.Add(mesh);
        }

        // The final mesh: combine all the material-specific meshes as independent submeshes.
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        foreach (Mesh mesh in submeshes)
        {
            CombineInstance ci = new CombineInstance
            {
                mesh = mesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
            finalCombiners.Add(ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.sharedMesh = finalMesh;

        Debug.Log("Final mesh has " + submeshes.Count + " materials.");
    }

    #endregion

    #region System

    #endregion

    #region Class Methods

    private void _CombineMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);

        Mesh finalMesh = _CombineMeshes(filters);

        MeshFilter mf = GetComponent<MeshFilter>();

        _UndoRecordObject(mf, "Set combinedMesh in MeshFilter");
        mf.sharedMesh = finalMesh;
    }

    private Mesh _CombineMeshes(MeshFilter[] filters)
    {
        Debug.Log("Combining " + filters.Length + " meshe(s)!");

        Mesh finalMesh = new Mesh();

        CombineInstance[] combiners = new CombineInstance[filters.Length];

        for (int a = 0; a < filters.Length; a++)
        {
            if (filters[a].transform == transform)
                continue;

            combiners[a].subMeshIndex = 0;

            combiners[a].mesh = filters[a].sharedMesh;
            combiners[a].transform = filters[a].transform.localToWorldMatrix;
        }

        finalMesh.CombineMeshes(combiners);

        return finalMesh;
    }

    private void _HideChildren()
    {
        for (int a = 0; a < transform.childCount; a++)
        {
            GameObject childGameObject = transform.GetChild(a).gameObject;

            _UndoRecordObject(childGameObject, "Disabled GameObject of MeshFilter.");

            childGameObject.SetActive(false);
        }
    }

    #endregion

    #region Tools Debug and Utility

    /// <summary>
    /// Shortcut to Undo.RecordObject wrapped in #if UNITY_EDITOR preprocessor
    /// </summary>
    private void _UndoRecordObject(UnityEngine.Object _objectToUndo, string _name)
    {
#if UNITY_EDITOR
        Undo.RecordObject(_objectToUndo, name);
#endif
    }

    #endregion

    #region Private and Protected Members

    #endregion
}

public class MoveToIdentity : IDisposable
{
    public MoveToIdentity(GameObject _go)
    {
        _OldRotation = _go.transform.rotation;
        _OldPos = _go.transform.position;

        // Always modify rotation first
        _go.transform.rotation = Quaternion.identity;
        _go.transform.position = Vector3.zero;

        _GameObject = _go;
    }

    public void Dispose()
    {
        // Always modify rotation first
        _GameObject.transform.rotation = _OldRotation;
        _GameObject.transform.position = _OldPos;
    }

    private GameObject _GameObject;

    private Quaternion _OldRotation;
    private Vector3 _OldPos;
}
