namespace TimelineAnimations.App.Models;

public sealed class EditorStateSnapshotComparer : IEqualityComparer<EditorStateSnapshot>
{
    public static readonly EditorStateSnapshotComparer Instance = new();

    public bool Equals(EditorStateSnapshot? x, EditorStateSnapshot? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Signature == y.Signature;
    }

    public int GetHashCode(EditorStateSnapshot obj)
    {
        return obj.Signature.GetHashCode(StringComparison.Ordinal);
    }
}
