using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Models;

public sealed class EditorStateSnapshot
{
    public EditorStateSnapshot(
        TimelineDocument document,
        Guid? selectedSceneId,
        Guid? editingLibraryItemId,
        ButtonVisualState editingButtonState,
        Guid? selectedLayerId,
        AnimatedProperty selectedProperty,
        Guid? selectedKeyframeId,
        double currentTime)
    {
        Document = DocumentSerializer.Clone(document);
        SelectedSceneId = selectedSceneId;
        EditingLibraryItemId = editingLibraryItemId;
        EditingButtonState = editingButtonState;
        SelectedLayerId = selectedLayerId;
        SelectedProperty = selectedProperty;
        SelectedKeyframeId = selectedKeyframeId;
        CurrentTime = currentTime;
        Signature = $"{DocumentSerializer.ToJson(Document)}|{SelectedSceneId}|{EditingLibraryItemId}|{EditingButtonState}|{SelectedLayerId}|{SelectedProperty}|{SelectedKeyframeId}|{CurrentTime:0.####}";
    }

    public TimelineDocument Document { get; }

    public Guid? SelectedSceneId { get; }

    public Guid? EditingLibraryItemId { get; }

    public ButtonVisualState EditingButtonState { get; }

    public Guid? SelectedLayerId { get; }

    public AnimatedProperty SelectedProperty { get; }

    public Guid? SelectedKeyframeId { get; }

    public double CurrentTime { get; }

    public string Signature { get; }
}
