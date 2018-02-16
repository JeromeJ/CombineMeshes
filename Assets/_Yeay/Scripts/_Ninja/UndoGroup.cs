using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UndoGroup : IDisposable
{
    private int _Group;

    public UndoGroup(string name)
    {
#if UNITY_EDITOR
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(name);

        _Group = Undo.GetCurrentGroup();
#endif
    }

    public void Dispose()
    {
#if UNITY_EDITOR
        Undo.CollapseUndoOperations(_Group);
#endif
    }
}