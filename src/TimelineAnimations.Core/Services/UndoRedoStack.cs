namespace TimelineAnimations.Core.Services;

public sealed class UndoRedoStack<T>
{
    private readonly Stack<T> _undo = [];
    private readonly Stack<T> _redo = [];
    private readonly IEqualityComparer<T> _comparer;

    public UndoRedoStack(T initialState, IEqualityComparer<T>? comparer = null)
    {
        Current = initialState;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Current { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Reset(T state)
    {
        _undo.Clear();
        _redo.Clear();
        Current = state;
    }

    public bool Record(T nextState)
    {
        if (_comparer.Equals(Current, nextState))
        {
            return false;
        }

        _undo.Push(Current);
        Current = nextState;
        _redo.Clear();
        return true;
    }

    public bool TryUndo(out T state)
    {
        if (!CanUndo)
        {
            state = Current;
            return false;
        }

        _redo.Push(Current);
        Current = _undo.Pop();
        state = Current;
        return true;
    }

    public bool TryRedo(out T state)
    {
        if (!CanRedo)
        {
            state = Current;
            return false;
        }

        _undo.Push(Current);
        Current = _redo.Pop();
        state = Current;
        return true;
    }
}
