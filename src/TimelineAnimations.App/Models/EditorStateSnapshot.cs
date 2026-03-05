using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Models;

public sealed class EditorStateSnapshot
{
    public EditorStateSnapshot(
        TimelineDocument document,
        Guid? selectedLayerId,
        AnimatedProperty selectedProperty,
        Guid? selectedKeyframeId,
        double currentTime)
    {
        Document = DocumentSerializer.Clone(document);
        SelectedLayerId = selectedLayerId;
        SelectedProperty = selectedProperty;
        SelectedKeyframeId = selectedKeyframeId;
        CurrentTime = currentTime;
        Signature = $"{DocumentSerializer.ToJson(Document)}|{SelectedLayerId}|{SelectedProperty}|{SelectedKeyframeId}|{CurrentTime:0.####}";
    }

    public TimelineDocument Document { get; }

    public Guid? SelectedLayerId { get; }

    public AnimatedProperty SelectedProperty { get; }

    public Guid? SelectedKeyframeId { get; }

    public double CurrentTime { get; }

    public string Signature { get; }
}
