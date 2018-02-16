using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshCombiner))]
public class MeshCombinerEditor : Editor
{
    #region Public Members

    #endregion

    #region Public void

    #endregion

    #region System

    public void OnSceneGUI()
    {
        MeshCombiner mc = (MeshCombiner) base.target;

        //EditorGUI.BeginChangeCheck();
        //float areaOfEffect = Handles.RadiusHandle(Quaternion.identity, mc.transform.position, mc._Test);
        //if (EditorGUI.EndChangeCheck())
        //{
        //    Undo.RecordObject(target, "Changed Area Of Effect");
        //    mc._Test = areaOfEffect;
        //}

        if (Handles.Button(mc.transform.position + Vector3.up * 20, Quaternion.LookRotation(Vector3.up), 1, 1, Handles.SphereHandleCap))
            mc.CombineMeshes();
    }

    #endregion

    #region Class Methods

    #endregion

    #region Tools Debug and Utility

    #endregion

    #region Private and Protected Members

    #endregion
}
