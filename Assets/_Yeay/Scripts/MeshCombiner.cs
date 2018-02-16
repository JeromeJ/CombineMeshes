using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : DualBehaviour
{
    #region Public Members

    #endregion

    #region Public void

    public void CombineMeshes()
    {
        _MoveToIdentity();

        _CombineMeshes();

        _RestorePosition();

        _HideChildren();
    }

    #endregion

    #region System

    #endregion

    #region Class Methods

    private void _MoveToIdentity()
    {
        _OldRotation = transform.rotation;
        _OldPos = transform.position;

        // Always modify rotation first
        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;
    }

    private void _RestorePosition()
    {
        // Always modify rotation first
        transform.rotation = _OldRotation;
        transform.position = _OldPos;
    }

    private void _CombineMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();

        Mesh finalMesh = _CombineMeshes(filters);

        GetComponent<MeshFilter>().sharedMesh = finalMesh;
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
            transform.GetChild(a).gameObject.SetActive(false);
    }

    #endregion

    #region Tools Debug and Utility

    #endregion

    #region Private and Protected Members

    private Quaternion _OldRotation;
    private Vector3 _OldPos;

    #endregion
}
