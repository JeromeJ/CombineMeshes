using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : DualBehaviour
{
    #region Public Members

    public float _Test = 1;

    #endregion

    #region Public void

    public void CombineMeshes()
    {
        using (new UndoGroup("Combined meshes"))
        {
            using (new MoveToIdentity(gameObject))
                _CombineMeshes();

            _HideChildren();
        }
    }

    #endregion

    #region System

    #endregion

    #region Class Methods

    private void _CombineMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();

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
            if(filters[a].transform == transform)
            {
                Debug.Log("Is this systemically the first? " + a);

                continue;
            }

            combiners[a].subMeshIndex = 0;

            combiners[a].mesh       = filters[a].sharedMesh;
            combiners[a].transform  = filters[a].transform.localToWorldMatrix;
        }

        finalMesh.CombineMeshes(combiners);

        return finalMesh;
    }

    private void _HideChildren()
    {
        for (int a = 0; a < transform.childCount; a++)
        {
            GameObject childGameObject = transform.GetChild(a).gameObject;

            _UndoRecordObject(childGameObject, "Hide (disabled) mesh-holder child object");

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
