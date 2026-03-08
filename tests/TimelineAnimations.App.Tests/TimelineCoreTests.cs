using System.Text.Json;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.App.Services;
using Dock.Model.Controls;
using Dock.Model.Core;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;
using TimelineAnimations.FlashFormats;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;
using TimelineAnimations.Rendering.SkiaSharp.Services;

namespace TimelineAnimations.App.Tests;

public class TimelineCoreTests
{
    [Fact]
    public void SampleProperty_Interpolates_BetweenNeighbouringKeyframes()
    {
        var layer = new TimelineLayer
        {
            Defaults = new LayerDefaults { X = 120 },
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.X,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 1, Value = 200 },
                        new KeyframeModel { Time = 3, Value = 400 }
                    ]
                }
            ]
        };

        var beforeFirst = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 0.5);
        var between = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 2);
        var afterLast = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 4);

        Assert.Equal(120, beforeFirst);
        Assert.Equal(300, between);
        Assert.Equal(400, afterLast);
    }

    [Fact]
    public void MoveKeyframe_ReordersAndMergesConflictingTimes()
    {
        var layer = new TimelineLayer
        {
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.Opacity,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 0, Value = 0.2 },
                        new KeyframeModel { Time = 1, Value = 0.5 },
                        new KeyframeModel { Time = 2, Value = 1.0 }
                    ]
                }
            ]
        };

        var movedId = layer.Tracks[0].Keyframes[2].Id;
        var moved = TimelineEditingService.MoveKeyframe(layer, AnimatedProperty.Opacity, movedId, 1, 6);

        Assert.True(moved);
        Assert.Equal(2, layer.Tracks[0].Keyframes.Count);
        Assert.Collection(
            layer.Tracks[0].Keyframes,
            first => Assert.Equal(0, first.Time),
            second => Assert.Equal(1, second.Time));
    }

    [Fact]
    public void UpdateDefaultValue_SynchronizesFrameZeroTrackValue()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 40d, 50d, 0);

        TimelineEditingService.UpdateDefaultValue(layer, AnimatedProperty.ScaleX, 1.35d);

        Assert.Equal(1.35d, layer.Defaults.ScaleX, 3);
        Assert.Equal(1.35d, layer.Tracks.First(track => track.Property == AnimatedProperty.ScaleX).Keyframes.First(keyframe => keyframe.Time == 0d).Value, 3);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_SampleComposition()
    {
        var document = SampleProjectFactory.Create();
        document.Layers[0].IsLocked = true;
        document.Layers[1].IsVisible = false;
        var json = DocumentSerializer.ToJson(document);
        var restored = DocumentSerializer.FromJson(json);

        Assert.Equal(document.Name, restored.Name);
        Assert.Equal(document.Duration, restored.Duration);
        Assert.Equal(document.Layers.Count, restored.Layers.Count);
        Assert.Equal(document.Scenes.Count, restored.Scenes.Count);
        Assert.Equal(document.LibraryItems.Count, restored.LibraryItems.Count);
        Assert.Equal(document.MediaAssets.Count, restored.MediaAssets.Count);
        Assert.Equal(document.ActiveSceneId, restored.ActiveSceneId);
        Assert.True(restored.Layers[0].IsLocked);
        Assert.False(restored.Layers[1].IsVisible);
        Assert.Equal(document.Layers[0].SourceLibraryItemId, restored.Layers[0].SourceLibraryItemId);
        Assert.Equal(document.Layers[2].Style.Text, restored.Layers[2].Style.Text);
        Assert.Equal(document.Layers[1].Tracks[0].Keyframes.Count, restored.Layers[1].Tracks[0].Keyframes.Count);
        Assert.Equal(document.Scenes[0].FrameRate, restored.Scenes[0].FrameRate);
        Assert.Equal(document.Scenes[0].FrameLabels.Count, restored.Scenes[0].FrameLabels.Count);
        Assert.Equal(document.Scenes[0].Markers.Count, restored.Scenes[0].Markers.Count);
        Assert.Equal(document.Scenes[0].OutgoingTransition.Kind, restored.Scenes[0].OutgoingTransition.Kind);
        Assert.Equal(document.Scenes[0].Layers[2].FrameSpans.Count, restored.Scenes[0].Layers[2].FrameSpans.Count);
        Assert.Equal(document.Layers[0].VisualStateGroups.Count, restored.Layers[0].VisualStateGroups.Count);
        Assert.Equal(document.Layers[4].Media.SourceMediaAssetId, restored.Layers[4].Media.SourceMediaAssetId);
        Assert.Equal(document.Layers[5].Kind, restored.Layers[5].Kind);
    }

    [Fact]
    public void BlankProjectFactory_CreatesEmptyAuthoringDocument()
    {
        var document = BlankProjectFactory.Create();

        Assert.Equal("Blank Animation", document.Name);
        Assert.Equal(AnimateDocumentType.Html5Canvas, document.Animate.Type);
        Assert.Single(document.Scenes);
        Assert.Equal(document.ActiveSceneId, document.Scenes[0].Id);
        Assert.Empty(document.Layers);
        Assert.Empty(document.Scenes[0].Layers);
        Assert.NotEmpty(document.PublishProfiles);
    }

    [Fact]
    public void FrameTimelineService_TimeToFrame_HoldsFrameUntilBoundary()
    {
        const double frameRate = 24d;
        const int totalFrames = 145;
        var frameDuration = FrameTimelineService.FrameToTime(1, frameRate);

        Assert.Equal(0, FrameTimelineService.TimeToFrame(0d, frameRate, totalFrames));
        Assert.Equal(0, FrameTimelineService.TimeToFrame(frameDuration * 0.5d, frameRate, totalFrames));
        Assert.Equal(0, FrameTimelineService.TimeToFrame(frameDuration - 0.0001d, frameRate, totalFrames));
        Assert.Equal(1, FrameTimelineService.TimeToFrame(frameDuration, frameRate, totalFrames));
        Assert.Equal(1, FrameTimelineService.TimeToFrame(frameDuration * 1.5d, frameRate, totalFrames));
        Assert.Equal(2, FrameTimelineService.TimeToFrame(frameDuration * 2d, frameRate, totalFrames));
    }

    [Fact]
    public void FrameTimelineService_GetSampleTime_DoesNotAdvanceCelBeforeNextFrame()
    {
        const double frameRate = 24d;
        const int totalFrames = 32;
        var layer = new TimelineLayer
        {
            FrameSpans =
            [
                new FrameSpanModel { StartFrame = 0, EndFrame = 0, IsBlank = false },
                new FrameSpanModel { StartFrame = 1, EndFrame = totalFrames - 1, IsBlank = false }
            ]
        };

        var frameDuration = FrameTimelineService.FrameToTime(1, frameRate);

        Assert.Equal(0d, FrameTimelineService.GetSampleTime(layer, frameDuration * 0.5d, frameRate, totalFrames));
        Assert.Equal(frameDuration, FrameTimelineService.GetSampleTime(layer, frameDuration, frameRate, totalFrames));
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_AnimateDocumentSettings()
    {
        var document = BlankProjectFactory.Create();
        document.Animate.Type = AnimateDocumentType.ActionScript3;
        document.Animate.Units = AnimateDocumentUnits.Points;
        document.Animate.IsResponsive = false;
        document.Animate.UseExternalScriptFile = false;
        document.Animate.UseWebFonts = false;
        document.Animate.UseAdvancedLayers = true;
        document.Animate.TemplateName = "Custom AS3";

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));

        Assert.Equal(AnimateDocumentType.ActionScript3, restored.Animate.Type);
        Assert.Equal(AnimateDocumentUnits.Points, restored.Animate.Units);
        Assert.False(restored.Animate.IsResponsive);
        Assert.False(restored.Animate.UseExternalScriptFile);
        Assert.False(restored.Animate.UseWebFonts);
        Assert.True(restored.Animate.UseAdvancedLayers);
        Assert.Equal("Custom AS3", restored.Animate.TemplateName);
    }

    [Fact]
    public void PublishValidationService_Flags_AnimateTargetCompatibilityIssues()
    {
        var document = BlankProjectFactory.Create();
        document.Animate.Type = AnimateDocumentType.Html5Canvas;
        var controlLayer = TimelineEditingService.CreateAvaloniaControlLayer(
            AvaloniaControlKind.Button,
            "CTA Button",
            "#1F7DFF",
            "#FFFFFF",
            "Click",
            120,
            120,
            0);
        document.Scenes[0].Layers.Add(controlLayer);
        document.Layers = document.Scenes[0].Layers;
        document.Scenes[0].FrameLabels.Add(new FrameLabelModel { Frame = 0, Name = "start", Script = "play" });

        var issues = PublishValidationService.Validate(document, document.PublishProfiles[0]);

        Assert.Contains(issues, issue => issue.Message.Contains("manual translation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("script glue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorkspaceLayoutPersistenceService_RoundTrips_LayoutState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.workspace.json");
        var state = new WorkspaceLayoutState
        {
            SelectedPreset = WorkspaceLayoutPreset.Custom,
            LeftPanelMode = WorkspacePanelMode.Hidden,
            RightPanelMode = WorkspacePanelMode.Overlay,
            TimelinePanelMode = WorkspacePanelMode.Docked,
            LeftRestoreMode = WorkspacePanelMode.Overlay,
            RightRestoreMode = WorkspacePanelMode.Docked,
            TimelineRestoreMode = WorkspacePanelMode.Hidden,
            LeftDockWidth = 250,
            RightDockWidth = 324,
            TimelineDockHeight = 360
        };

        try
        {
            Assert.True(WorkspaceLayoutPersistenceService.Save(path, state));

            var restored = WorkspaceLayoutPersistenceService.Load(path);

            Assert.Equal(WorkspaceLayoutPreset.Custom, restored.SelectedPreset);
            Assert.Equal(WorkspacePanelMode.Hidden, restored.LeftPanelMode);
            Assert.Equal(WorkspacePanelMode.Overlay, restored.RightPanelMode);
            Assert.Equal(WorkspacePanelMode.Docked, restored.TimelinePanelMode);
            Assert.Equal(WorkspacePanelMode.Overlay, restored.LeftRestoreMode);
            Assert.Equal(WorkspacePanelMode.Docked, restored.RightRestoreMode);
            Assert.Equal(WorkspacePanelMode.Docked, restored.TimelineRestoreMode);
            Assert.Equal(250, restored.LeftDockWidth);
            Assert.Equal(324, restored.RightDockWidth);
            Assert.Equal(360, restored.TimelineDockHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MainWindowViewModel_Switches_TimelineWorkspaceViews()
    {
        var viewModel = new MainWindowViewModel();

        Assert.True(viewModel.IsFramesTimelineViewActive);
        Assert.False(viewModel.IsCurvesTimelineViewActive);

        viewModel.SetTimelineWorkspaceViewCommand.Execute("Curves");

        Assert.True(viewModel.IsCurvesTimelineViewActive);
        Assert.False(viewModel.IsFramesTimelineViewActive);
        Assert.Equal("Curves", viewModel.TimelineWorkspaceViewLabel);
    }

    [Fact]
    public void MainWindowViewModel_Updates_OnionSkinCount_FromCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetOnionSkinCountCommand.Execute("before:4");
        viewModel.SetOnionSkinCountCommand.Execute("after:1");

        Assert.Equal(4, viewModel.OnionSkinBefore);
        Assert.Equal(1, viewModel.OnionSkinAfter);
    }

    [Fact]
    public void MainWindowViewModel_ResetBlank_CreatesEmptyAnimation()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ResetBlankCommand.Execute(null);

        Assert.Equal("Blank Animation", viewModel.DocumentName);
        Assert.Equal("Blank Animation", viewModel.FileLabel);
        Assert.Empty(viewModel.Document.Layers);
        Assert.Single(viewModel.Document.Scenes);
        Assert.Empty(viewModel.Document.Scenes[0].Layers);
        Assert.Contains("Blank animation created", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowViewModel_ApplyColorSwatch_UpdatesStyleAndCanvasEditors()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SelectedLayer = viewModel.Layers.FirstOrDefault();

        viewModel.ApplyColorSwatchCommand.Execute("fill:#ABCDEF");
        viewModel.ApplyColorSwatchCommand.Execute("backgroundFrom:#102030");

        Assert.Equal("#ABCDEF", viewModel.FillHexEditor);
        Assert.Equal("#102030", viewModel.BackgroundFrom);
    }

    [Fact]
    public void MainWindowViewModel_HistoryEntries_DedupeNormalizedStatusMessages()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.HistoryEntries.Clear();

        viewModel.StatusMessage = "Selection updated ";
        viewModel.StatusMessage = "Selection updated";
        viewModel.StatusMessage = "Timeline scrubbed";

        Assert.Equal(2, viewModel.HistoryEntries.Count);
        Assert.Equal("Timeline scrubbed", viewModel.HistoryEntries[0].Message);
        Assert.Equal("Selection updated", viewModel.HistoryEntries[1].Message);
    }

    [Fact]
    public void MainWindowViewModel_FocusMovieExplorerNode_SelectsTargetLayer()
    {
        var viewModel = new MainWindowViewModel();
        var sceneNode = viewModel.MovieExplorerNodes.FirstOrDefault();
        Assert.NotNull(sceneNode);

        var layerNode = sceneNode!.Children.FirstOrDefault(node => string.Equals(node.KindLabel, "Layer", StringComparison.Ordinal));
        Assert.NotNull(layerNode);

        viewModel.FocusMovieExplorerNodeCommand.Execute(layerNode);

        Assert.NotNull(viewModel.SelectedScene);
        Assert.NotNull(viewModel.SelectedLayer);
        Assert.Equal(layerNode!.Title, viewModel.SelectedLayer!.Name);
    }

    [Fact]
    public void MainWindowViewModel_BuildsHierarchicalCurveRows_AndSupportsLayerCurveCollapse()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(CreateHierarchyInteropDocument(), "hierarchy.timeline.json");

        var childLayer = viewModel.Layers.First(layer => layer.Name == "Child Card");
        Assert.Contains(viewModel.TimelineRows, row => row.LayerId == childLayer.Id && row.RowKind == TimelineTrackRowKind.LayerHeader);
        Assert.Contains(viewModel.TimelineRows, row => row.LayerId == childLayer.Id && row.RowKind == TimelineTrackRowKind.PropertyGroupHeader);
        Assert.Contains(viewModel.TimelineRows, row => row.LayerId == childLayer.Id && row.RowKind == TimelineTrackRowKind.PropertyTrack);

        viewModel.ToggleTimelineHierarchy(childLayer.Id);

        Assert.Contains(viewModel.TimelineRows, row => row.LayerId == childLayer.Id && row.RowKind == TimelineTrackRowKind.LayerHeader);
        Assert.DoesNotContain(viewModel.TimelineRows, row => row.LayerId == childLayer.Id && row.RowKind != TimelineTrackRowKind.LayerHeader);
    }

    [Fact]
    public void MainWindowViewModel_ToggleFrameHierarchy_CollapsesFolderChildren()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(CreateHierarchyInteropDocument(), "hierarchy.timeline.json");

        var folderLayer = viewModel.Layers.First(layer => layer.Name == "Group");

        Assert.Contains(viewModel.FrameRows, row => row.LayerName == "Child Card");

        viewModel.ToggleFrameHierarchy(folderLayer.Id);

        Assert.DoesNotContain(viewModel.FrameRows, row => row.LayerName == "Child Card");
        Assert.Contains(viewModel.FrameRows, row => row.LayerId == folderLayer.Id && row.RowKind == FrameTimelineRowKind.FolderHeader);
    }

    [Fact]
    public void SceneTimelineService_SetsWorkAreaEdgesIndependently()
    {
        var scene = new SceneModel
        {
            Duration = 3d,
            FrameRate = 24d,
            InFrame = 2,
            OutFrame = 20,
            WorkAreaStartFrame = 4,
            WorkAreaEndFrame = 18
        };

        SceneTimelineService.SetWorkAreaStart(scene, 8, 24);
        SceneTimelineService.SetWorkAreaEnd(scene, 14, 24);

        Assert.Equal(8, scene.WorkAreaStartFrame);
        Assert.Equal(14, scene.WorkAreaEndFrame);
    }

    [Fact]
    public void MainWindowViewModel_AppliesFrameRulerInteractions_ToSceneRangesAndOnionSkin()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(CreateHierarchyInteropDocument(), "hierarchy.timeline.json");
        viewModel.SeekFrame(10);

        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.InPoint, 3);
        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.OutPoint, 40);
        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.WorkAreaStart, 6);
        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.WorkAreaEnd, 24);
        viewModel.SeekFrame(10);
        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.OnionBefore, 8);
        viewModel.ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind.OnionAfter, 16);

        Assert.Equal(3, viewModel.CurrentSceneInFrame);
        Assert.Equal(40, viewModel.CurrentSceneOutFrame);
        Assert.Equal(6, viewModel.CurrentSceneWorkAreaStartFrame);
        Assert.Equal(24, viewModel.CurrentSceneWorkAreaEndFrame);
        Assert.Equal(2, viewModel.OnionSkinBefore);
        Assert.Equal(6, viewModel.OnionSkinAfter);
    }

    [Fact]
    public void MainWindowViewModel_SampleLayerStyle_TransfersStyleIntoSelectedLayer()
    {
        var viewModel = new MainWindowViewModel();
        var source = viewModel.Layers.First(layer => layer.Name == "Signal Path");
        var target = viewModel.Layers.First(layer => layer.Name == "Hero Card");
        viewModel.SelectLayer(target.Id);

        viewModel.SampleLayerStyle(source.Id);

        Assert.Equal(source.Model.Style.Fill, target.Model.Style.Fill);
        Assert.Equal(source.Model.Style.Stroke, target.Model.Style.Stroke);
        Assert.Equal(source.Model.Style.UseGradient, target.Model.Style.UseGradient);
        Assert.Equal(source.Model.Style.GradientFrom, target.Model.Style.GradientFrom);
        Assert.Equal(source.Model.Style.StrokeThickness, target.Model.Style.StrokeThickness, 3);
    }

    [Fact]
    public void MainWindowViewModel_ApplyStageStyle_UsesInspectorFillAndStrokeSettings()
    {
        var viewModel = new MainWindowViewModel();
        var styleSource = viewModel.Layers.First(layer => layer.Name == "Hero Card");
        var target = viewModel.Layers.First(layer => layer.Name == "Solar Orb");
        viewModel.SelectLayer(styleSource.Id);
        viewModel.FillHexEditor = "#123456";
        viewModel.UseGradientEditor = true;
        viewModel.GradientKindEditor = LayerGradientKind.Radial;
        viewModel.GradientFromEditor = "#111111";
        viewModel.GradientToEditor = "#222222";
        viewModel.StrokeHexEditor = "#ABCDEF";
        viewModel.StrokeThicknessEditor = 7.25d;
        viewModel.StrokeCapStyleEditor = LayerStrokeCapStyle.Square;
        viewModel.StrokeJoinStyleEditor = LayerStrokeJoinStyle.Bevel;

        viewModel.ApplyStageStyle(target.Id, CanvasStyleApplicationKind.Fill);
        viewModel.ApplyStageStyle(target.Id, CanvasStyleApplicationKind.Stroke);

        Assert.Equal("#123456", target.Model.Style.Fill);
        Assert.True(target.Model.Style.UseGradient);
        Assert.Equal(LayerGradientKind.Radial, target.Model.Style.GradientKind);
        Assert.Equal("#111111", target.Model.Style.GradientFrom);
        Assert.Equal("#222222", target.Model.Style.GradientTo);
        Assert.Equal("#ABCDEF", target.Model.Style.Stroke);
        Assert.Equal(7.25d, target.Model.Style.StrokeThickness, 3);
        Assert.Equal(LayerStrokeCapStyle.Square, target.Model.Style.StrokeCapStyle);
        Assert.Equal(LayerStrokeJoinStyle.Bevel, target.Model.Style.StrokeJoinStyle);
    }

    [Fact]
    public void MainWindowViewModel_Inserts_TargetAwareActionScriptSnippets()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SelectedActionScriptSnippet = viewModel.ActionScriptSnippets.First(snippet => snippet.Name == "Set State Variable");

        viewModel.InsertSelectedSnippetIntoFrameActionCommand.Execute(null);

        Assert.Contains("set_var state ready", viewModel.FrameActionScriptEditor, StringComparison.OrdinalIgnoreCase);

        var layer = viewModel.Layers.First(layerItem => layerItem.Name == "Hero Card");
        viewModel.SelectLayer(layer.Id);
        viewModel.AddBehaviorCommand.Execute(null);
        viewModel.SelectedActionScriptSnippet = viewModel.ActionScriptSnippets.First(snippet => snippet.Name == "Apply Visual State");

        viewModel.InsertSelectedSnippetIntoBehaviorScriptCommand.Execute(null);

        Assert.Contains("set_state", viewModel.SelectedBehaviorScriptEditor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CardStates", viewModel.SelectedBehaviorScriptEditor, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainWindowViewModel_Clamps_CanvasZoom_And_TimelineHeight()
    {
        var viewModel = new MainWindowViewModel
        {
            CanvasZoom = 12
        };

        viewModel.SetTimelineDockHeightPixels(120);

        Assert.Equal(8, viewModel.CanvasZoom);
        Assert.Equal("800%", viewModel.CanvasZoomLabel);
        Assert.Equal(300, viewModel.TimelineDockHeight.Value);
    }

    [Fact]
    public void MainWindowViewModel_Updates_ActiveSceneCanvasProperties()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetCanvasSize(1920, 1080);
        viewModel.Duration = 8.5d;
        viewModel.BackgroundFrom = "#112233";
        viewModel.BackgroundTo = "#445566";

        Assert.Equal(1920d, viewModel.CanvasWidth);
        Assert.Equal(1080d, viewModel.CanvasHeight);
        Assert.Equal("1920 × 1080", viewModel.CanvasSizeLabel);
        Assert.Equal(8.5d, viewModel.Duration);
        Assert.Equal("#112233", viewModel.BackgroundFrom);
        Assert.Equal("#445566", viewModel.BackgroundTo);
        Assert.NotNull(viewModel.SelectedScene);
        Assert.Equal(1920d, viewModel.SelectedScene!.Model.CanvasWidth);
        Assert.Equal(1080d, viewModel.SelectedScene.Model.CanvasHeight);
        Assert.Equal(8.5d, viewModel.SelectedScene.Model.Duration);
        Assert.Equal("#112233", viewModel.SelectedScene.Model.BackgroundFrom);
        Assert.Equal("#445566", viewModel.SelectedScene.Model.BackgroundTo);
    }

    [Fact]
    public void MainWindowViewModel_Updates_StageSurfaceEditors()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.BackgroundFrom = "#223344";
        viewModel.TransparentStageBackground = true;
        viewModel.PasteboardColor = "#C7CBD3";
        viewModel.MatchPasteboardToStageColor = true;

        Assert.Equal("#223344", viewModel.BackgroundFrom);
        Assert.True(viewModel.TransparentStageBackground);
        Assert.Equal("#C7CBD3", viewModel.PasteboardColor);
        Assert.True(viewModel.MatchPasteboardToStageColor);
        Assert.Contains("Stage transparent", viewModel.CanvasBackgroundSummary, StringComparison.Ordinal);
        Assert.Contains("pasteboard matched", viewModel.CanvasBackgroundSummary, StringComparison.Ordinal);
        Assert.NotNull(viewModel.SelectedScene);
        Assert.True(viewModel.SelectedScene!.Model.TransparentStageBackground);
        Assert.Equal("#223344", viewModel.SelectedScene.Model.BackgroundFrom);
    }

    [Fact]
    public void MainWindowViewModel_Applies_CanvasPreset_And_Clamps_SmallSizes()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ApplyCanvasPresetCommand.Execute("story");

        Assert.Equal(1080d, viewModel.CanvasWidth);
        Assert.Equal(1920d, viewModel.CanvasHeight);

        viewModel.SetCanvasSize(8d, 20d);

        Assert.Equal(SceneEditingService.MinCanvasExtent, viewModel.CanvasWidth);
        Assert.Equal(SceneEditingService.MinCanvasExtent, viewModel.CanvasHeight);
    }

    [Fact]
    public void MainWindowViewModel_MenuContext_TracksSelectionAndMode()
    {
        var viewModel = new MainWindowViewModel();
        var textLayer = viewModel.Layers.First(layer => layer.Kind == LayerKind.Text);

        viewModel.SelectLayer(textLayer.Id);

        Assert.True(viewModel.CanUseSelectionActions);
        Assert.True(viewModel.CanEditTextMenu);
        Assert.Contains(textLayer.Name, viewModel.ModifyMenuHeader, StringComparison.Ordinal);

        var originalFontSize = viewModel.InspectorFontSize;
        viewModel.IncreaseTextSizeCommand.Execute(null);

        Assert.True(viewModel.InspectorFontSize > originalFontSize);

        viewModel.TogglePrototypeModeCommand.Execute(null);

        Assert.False(viewModel.CanInsertStageItems);
        Assert.False(viewModel.CanUseSelectionActions);
        Assert.Contains("Prototype mode", viewModel.ControlMenuSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainWindowViewModel_SelectAnimationExchangeFormat_UpdatesMenuState()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SelectAnimationExchangeFormatCommand.Execute("SvgSmil");

        Assert.Equal(AnimationExchangeFormat.SvgSmil, viewModel.SelectedAnimationExchangeFormat);
        Assert.False(viewModel.IsAvaloniaXamlExchangeSelected);
        Assert.True(viewModel.IsSvgAnimationExchangeSelected);
        Assert.False(viewModel.IsHtmlAnimationExchangeSelected);
    }

    [Fact]
    public void UndoRedoStack_TraversesRecordedStates()
    {
        var history = new UndoRedoStack<int>(1);

        Assert.True(history.Record(2));
        Assert.True(history.Record(3));
        Assert.True(history.TryUndo(out var undoState));
        Assert.Equal(2, undoState);
        Assert.True(history.TryUndo(out undoState));
        Assert.Equal(1, undoState);
        Assert.True(history.TryRedo(out var redoState));
        Assert.Equal(2, redoState);
    }

    [Fact]
    public void SampleProperty_UsesHoldEasing_FromNextKeyframe()
    {
        var layer = new TimelineLayer
        {
            Defaults = new LayerDefaults { Opacity = 0.1 },
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.Opacity,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 0, Value = 0.1 },
                        new KeyframeModel { Time = 1, Value = 1.0, Easing = EasingKind.Hold }
                    ]
                }
            ]
        };

        var sampled = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.Opacity, 0.6);

        Assert.Equal(0.1, sampled);
    }

    [Fact]
    public void TimelineEasingService_BackOut_OvershootsLinearCurve()
    {
        var linear = TimelineEasingService.Apply(EasingKind.Linear, 0.75);
        var backOut = TimelineEasingService.Apply(EasingKind.BackOut, 0.75);

        Assert.True(backOut > linear);
    }

    [Fact]
    public void CanvasSnapService_SnapsMove_ToStageCenter()
    {
        var snapped = CanvasSnapService.SnapMove(
            x: 495,
            y: 214,
            width: 300,
            height: 100,
            canvasWidth: 1280,
            canvasHeight: 720,
            snapEnabled: true);

        Assert.Equal(490, snapped.X);
        Assert.Equal(220, snapped.Y);
        Assert.Equal(640, snapped.VerticalGuide);
        Assert.Null(snapped.HorizontalGuide);
    }

    [Fact]
    public void CanvasSnapService_SnapsResize_ToGrid()
    {
        var snapped = CanvasSnapService.SnapResize(
            x: 143,
            y: 118,
            width: 237,
            height: 151,
            canvasWidth: 1280,
            canvasHeight: 720,
            snapEnabled: true);

        Assert.Equal(140, snapped.X);
        Assert.Equal(120, snapped.Y);
        Assert.Equal(240, snapped.Width);
        Assert.Equal(160, snapped.Height);
    }

    [Fact]
    public void CreateLayerFromLibraryItem_OffsetsPositionTracks()
    {
        var sourceLayer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 180, 6);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.Y, 1, 140, 6);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(sourceLayer, "Card Symbol", SymbolKind.Graphic);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 300, 240, 1);
        var document = new TimelineDocument
        {
            Duration = 2,
            LibraryItems = [libraryItem],
            Layers = [instance]
        };
        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 1, document.Duration, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(libraryItem.Id, instance.SourceLibraryItemId);
        Assert.Equal(300, instance.Defaults.X);
        Assert.Equal(240, instance.Defaults.Y);
        Assert.Equal(380, sample.Snapshot.X);
        Assert.Equal(300, sample.Snapshot.Y);
    }

    [Fact]
    public void SynchronizeLibraryItemInstances_UpdatesLinkedLayers()
    {
        var document = new TimelineDocument();
        var sourceLayer = TimelineEditingService.CreateLayer(LayerKind.Text, "Caption", "#F7F5ED", "HELLO", 120, 90, 0);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 200, 6);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(sourceLayer, "Caption Symbol", SymbolKind.MovieClip);
        TimelineEditingService.AddLibraryItem(document, libraryItem);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 420, 220, 0);
        instance.Name = "Caption Instance";
        TimelineEditingService.AddLayer(document, instance);

        sourceLayer.Style.Fill = "#61E6FF";
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 240, 6);
        TimelineEditingService.UpdateLibraryItemFromLayer(libraryItem, sourceLayer);

        var updatedInstances = TimelineEditingService.SynchronizeLibraryItemInstances(document, libraryItem.Id);
        var synchronized = document.Layers.Single();
        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.5, 3, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(1, updatedInstances);
        Assert.Equal("Caption Instance", synchronized.Name);
        Assert.Equal("#61E6FF", sample.Snapshot.Fill);
        Assert.Equal(420, synchronized.Defaults.X);
        Assert.Equal(480, sample.Snapshot.X);
    }

    [Fact]
    public void LibraryManagementService_EnsuresMetadata_ForLegacyLibraryItems()
    {
        var document = new TimelineDocument
        {
            LibraryItems =
            [
                new LibraryItem
                {
                    Name = "Hero Symbol",
                    SymbolKind = SymbolKind.Graphic
                },
                new LibraryItem
                {
                    Name = "Hero Symbol",
                    SymbolKind = SymbolKind.MovieClip,
                    LinkageId = "Hero Symbol"
                },
                new LibraryItem
                {
                    Name = "CTA Button",
                    SymbolKind = SymbolKind.Button,
                    IsComponent = true,
                    ComponentCategory = "Controls"
                }
            ]
        };

        LibraryManagementService.EnsureLibraryMetadata(document);

        Assert.Equal("Symbols/Graphics", document.LibraryItems[0].FolderPath);
        Assert.Equal("hero_symbol", document.LibraryItems[0].LinkageId);
        Assert.Equal("Hero Symbol 2", document.LibraryItems[1].Name);
        Assert.Equal("hero_symbol_2", document.LibraryItems[1].LinkageId);
        Assert.Equal("Components/Controls", document.LibraryItems[2].FolderPath);
    }

    [Fact]
    public void LibraryManagementService_FiltersItems_ByFolderAndSearch()
    {
        var items = new[]
        {
            new LibraryItem
            {
                Name = "Hero Card",
                FolderPath = "Symbols/Graphics",
                LinkageId = "hero_card",
                SourceAssetPath = "assets/hero.svg"
            },
            new LibraryItem
            {
                Name = "Ambient Loop",
                FolderPath = "Media/Audio",
                LinkageId = "ambient_loop",
                SourceAssetPath = "audio/ambient.wav"
            }
        };

        var filtered = LibraryManagementService.FilterItems(items, "hero", "Symbols/Graphics").ToList();

        var match = Assert.Single(filtered);
        Assert.Equal("Hero Card", match.Name);
    }

    [Fact]
    public void LibraryManagementService_RelinksLayerToLibraryItem_PreservesInstanceState()
    {
        var sourceItem = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0),
            "Card Symbol",
            SymbolKind.Graphic);
        var targetItem = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Text, "Caption", "#F7F5ED", "HELLO", 200, 120, 0),
            "Caption Symbol",
            SymbolKind.MovieClip);
        var instance = TimelineEditingService.CreateLayerFromLibraryItem(sourceItem, 320, 240, 4);
        instance.Name = "Instance A";
        instance.Defaults.Width = 540;
        instance.Defaults.Height = 180;
        instance.Defaults.Rotation = 18;
        instance.Defaults.Opacity = 0.72;
        instance.SymbolPlaybackMode = SymbolPlaybackMode.IndependentLoop;
        instance.SymbolPlaybackOffset = 0.8;
        instance.SymbolLockedFrame = 6;
        instance.Behaviors.Add(InteractionBehaviorService.CreateBehavior("Hover"));

        var relinked = LibraryManagementService.RelinkLayerToLibraryItem(instance, targetItem);

        Assert.Equal(instance.Id, relinked.Id);
        Assert.Equal("Instance A", relinked.Name);
        Assert.Equal(targetItem.Id, relinked.SourceLibraryItemId);
        Assert.Equal(320, relinked.Defaults.X);
        Assert.Equal(240, relinked.Defaults.Y);
        Assert.Equal(540, relinked.Defaults.Width);
        Assert.Equal(180, relinked.Defaults.Height);
        Assert.Equal(18, relinked.Defaults.Rotation);
        Assert.Equal(0.72, relinked.Defaults.Opacity);
        Assert.Equal(SymbolPlaybackMode.IndependentLoop, relinked.SymbolPlaybackMode);
        Assert.Equal(0.8, relinked.SymbolPlaybackOffset);
        Assert.Equal(6, relinked.SymbolLockedFrame);
        Assert.Single(relinked.Behaviors);
    }

    [Fact]
    public void SceneEditingService_EnsuresLegacyDocumentIntoSingleScene()
    {
        var document = new TimelineDocument
        {
            Name = "Legacy",
            Duration = 5,
            CanvasWidth = 1024,
            CanvasHeight = 576,
            BackgroundFrom = "#111111",
            BackgroundTo = "#222222",
            Layers =
            [
                TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0)
            ]
        };

        SceneEditingService.EnsureScenes(document);

        Assert.Single(document.Scenes);
        Assert.Equal(document.ActiveSceneId, document.Scenes[0].Id);
        Assert.Equal(5, document.Scenes[0].Duration);
        Assert.Single(document.Scenes[0].Layers);
    }

    [Fact]
    public void DuplicateScene_CreatesIndependentLayerGraph()
    {
        var scene = new SceneModel
        {
            Name = "Intro",
            Duration = 4,
            Layers =
            [
                TimelineEditingService.CreateLayer(LayerKind.Text, "Caption", "#FFFFFF", "HELLO", 100, 80, 0)
            ]
        };

        var duplicate = SceneEditingService.DuplicateScene(scene, "Intro Copy");

        Assert.Equal("Intro Copy", duplicate.Name);
        Assert.Single(duplicate.Layers);
        Assert.NotEqual(scene.Layers[0].Id, duplicate.Layers[0].Id);
        Assert.NotEqual(
            scene.Layers[0].Tracks[0].Keyframes[0].Id,
            duplicate.Layers[0].Tracks[0].Keyframes[0].Id);
    }

    [Fact]
    public void InsertContentKeyframe_SplitsContentSpan()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);

        var span = FrameTimelineService.InsertContentKeyframe(layer, 12, 49);
        var spans = FrameTimelineService.GetEffectiveSpans(layer, 49);

        Assert.False(span.IsBlank);
        Assert.Equal(2, spans.Count);
        Assert.Equal((0, 11, false), (spans[0].StartFrame, spans[0].EndFrame, spans[0].IsBlank));
        Assert.Equal((12, 48, false), (spans[1].StartFrame, spans[1].EndFrame, spans[1].IsBlank));
    }

    [Fact]
    public void GetSampleTime_UsesContentSpanStartForHeldFrames()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 0, 100, 2);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 0.5, 220, 2);
        FrameTimelineService.InsertContentKeyframe(layer, 12, 49);

        var heldTime = FrameTimelineService.GetSampleTime(layer, 0.75, 24, 49);
        var sampled = FrameTimelineService.SampleProperty(layer, AnimatedProperty.X, 0.75, 24, 49);

        Assert.Equal(0.5, heldTime);
        Assert.Equal(220, sampled);
    }

    [Fact]
    public void CopyPasteRange_CopiesExposureAndTrackKeys()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 0, 120, 3);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 0.5, 320, 3);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 520, 3);
        FrameTimelineService.InsertBlankKeyframe(source, 12, 73);
        FrameTimelineService.InsertContentKeyframe(source, 24, 73);

        var clipboard = FrameTimelineService.CopyRange(source, 12, 24, 73, 24);

        var target = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Target", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(target, AnimatedProperty.X, 0, 40, 3);
        FrameTimelineService.PasteRange(target, 36, clipboard, 73, 24);

        var pastedSpans = FrameTimelineService.GetEffectiveSpans(target, 73)
            .Where(span => span.EndFrame >= 36 && span.StartFrame <= 48)
            .ToList();
        var pastedValue = FrameTimelineService.SampleProperty(target, AnimatedProperty.X, 2.0, 24, 73);

        Assert.Equal(2, pastedSpans.Count);
        Assert.True(pastedSpans[0].IsBlank);
        Assert.False(pastedSpans[1].IsBlank);
        Assert.Equal(520, pastedValue);
    }

    [Fact]
    public void SetFrameLabel_AddsAndRemovesSceneMarkers()
    {
        var scene = new SceneModel { FrameRate = 24, Duration = 2 };

        Assert.True(FrameTimelineService.SetFrameLabel(scene, 12, "intro", 49));
        Assert.Equal("intro", FrameTimelineService.GetFrameLabel(scene, 12)?.Name);
        Assert.True(FrameTimelineService.RemoveFrameLabel(scene, 12));
        Assert.Null(FrameTimelineService.GetFrameLabel(scene, 12));
    }

    [Fact]
    public void SceneTimelineService_ManagesMarkersAndPlaybackRanges()
    {
        var scene = new SceneModel
        {
            Duration = 4d,
            FrameRate = 24d
        };
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);

        Assert.True(SceneTimelineService.AddOrUpdateMarker(scene, null, 12, "Beat", SceneMarkerKind.Beat, "Sync music hit.", "#FF8A4C", totalFrames));
        SceneTimelineService.SetInPoint(scene, 6, totalFrames);
        SceneTimelineService.SetOutPoint(scene, 72, totalFrames);
        SceneTimelineService.SetWorkArea(scene, 10, 42, totalFrames);

        var marker = Assert.Single(scene.Markers);
        var playbackRange = SceneTimelineService.GetPlaybackRange(scene, totalFrames, useWorkArea: true);

        Assert.Equal("Beat", marker.Name);
        Assert.Equal(SceneMarkerKind.Beat, marker.Kind);
        Assert.Equal(6, scene.InFrame);
        Assert.Equal(72, scene.OutFrame);
        Assert.Equal((10, 42), playbackRange);
    }

    [Fact]
    public void VisualStateEditingService_CapturesAndAppliesStateTemplates()
    {
        var layer = TimelineEditingService.CreateAvaloniaControlLayer(AvaloniaControlKind.Button, "CTA", "#1F7DFF", "#E6F1FF", "Launch", 80d, 120d, 0);
        layer.Defaults.Width = 220d;
        layer.Defaults.Height = 58d;
        layer.Style.Fill = "#1F7DFF";

        var intro = VisualStateEditingService.CaptureState(layer, "CommonStates", "Intro", 0d);
        layer.Defaults.Width = 260d;
        layer.Style.Fill = "#24E5C1";
        var active = VisualStateEditingService.CaptureState(layer, "CommonStates", "Active", 0d);

        Assert.True(VisualStateEditingService.ApplyState(layer, layer.VisualStateGroups[0].Id, intro.Id));
        Assert.Equal(220d, layer.Defaults.Width);
        Assert.Equal("#1F7DFF", layer.Style.Fill);
        Assert.Equal("Intro", layer.VisualStateGroups[0].ActiveStateName);

        Assert.True(VisualStateEditingService.RemoveState(layer, layer.VisualStateGroups[0].Id, active.Id));
        Assert.Single(layer.VisualStateGroups[0].States);
    }

    [Fact]
    public void FrameExportService_UsesTransitionOverlapForSequenceDuration()
    {
        var firstScene = new SceneModel
        {
            Name = "A",
            Duration = 2d,
            FrameRate = 24d,
            CanvasWidth = 640d,
            CanvasHeight = 360d,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            OutgoingTransition = new SceneTransitionModel
            {
                Kind = SceneTransitionKind.CrossDissolve,
                Duration = 0.5d
            },
            Layers = [TimelineEditingService.CreateLayer(LayerKind.Rectangle, "A", "#24E5C1", string.Empty, 20d, 20d, 0)]
        };
        var secondScene = new SceneModel
        {
            Name = "B",
            Duration = 2d,
            FrameRate = 24d,
            CanvasWidth = 640d,
            CanvasHeight = 360d,
            BackgroundFrom = "#120C20",
            BackgroundTo = "#26174A",
            Layers = [TimelineEditingService.CreateLayer(LayerKind.Rectangle, "B", "#FF8A4C", string.Empty, 40d, 40d, 0)]
        };
        var document = new TimelineDocument
        {
            Name = "Sequence",
            Scenes = [firstScene, secondScene],
            ActiveSceneId = firstScene.Id,
            Layers = firstScene.Layers,
            Duration = firstScene.Duration,
            CanvasWidth = firstScene.CanvasWidth,
            CanvasHeight = firstScene.CanvasHeight,
            BackgroundFrom = firstScene.BackgroundFrom,
            BackgroundTo = firstScene.BackgroundTo
        };

        var duration = FrameExportService.GetPlaybackDuration(document, playAllScenes: true);

        Assert.Equal(3.5d, duration, 3);
    }

    [Fact]
    public void CreateLibraryItemFromLayer_LocalizesNestedTimeline()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 180, 2);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.Y, 1, 140, 2);

        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "Card Symbol", SymbolKind.Graphic);
        var nestedLayer = libraryItem.Layers.Single();

        Assert.Equal(0, libraryItem.Template.Defaults.X);
        Assert.Equal(0, libraryItem.Template.Defaults.Y);
        Assert.Equal(0, nestedLayer.Defaults.X);
        Assert.Equal(0, nestedLayer.Defaults.Y);
        Assert.Equal(80, nestedLayer.Tracks.Single(track => track.Property == AnimatedProperty.X).Keyframes.Last().Value);
        Assert.Equal(60, nestedLayer.Tracks.Single(track => track.Property == AnimatedProperty.Y).Keyframes.Last().Value);
    }

    [Fact]
    public void ButtonSymbol_PersistsIndependentStateLayers()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Button", "#24E5C1", string.Empty, 80, 60, 0);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "CTA Button", SymbolKind.Button);
        var downLayers = TimelineEditingService.GetEditableSymbolLayers(libraryItem, ButtonVisualState.Down);

        downLayers[0].Style.Fill = "#FF8A4C";
        TimelineEditingService.SetEditableSymbolLayers(libraryItem, downLayers, 1.5, 24, ButtonVisualState.Down);

        Assert.Equal(4, libraryItem.ButtonStates.Count);
        Assert.Equal("#24E5C1", libraryItem.ButtonStates.First(state => state.State == ButtonVisualState.Up).Layers[0].Style.Fill);
        Assert.Equal("#FF8A4C", libraryItem.ButtonStates.First(state => state.State == ButtonVisualState.Down).Layers[0].Style.Fill);
    }

    [Fact]
    public void SymbolRenderService_RendersMovieClipInstancesWithIndependentTimeline()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Pulse", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 220, 2);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "Pulse Clip", SymbolKind.MovieClip);

        var document = new TimelineDocument
        {
            Duration = 4,
            LibraryItems = [libraryItem]
        };

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 300, 160, 0);
        TimelineEditingService.AddLayer(document, instance);

        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 2.5, document.Duration, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(LayerKind.Rectangle, sample.Kind);
        Assert.Equal(360, sample.Snapshot.X);
    }

    [Fact]
    public void CreatePathLayer_NormalizesPointsIntoLayerBounds()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Stroke",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 120, Y = 80 },
                new VectorPointModel { X = 260, Y = 140 },
                new VectorPointModel { X = 520, Y = 180 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        Assert.Equal(LayerKind.Path, layer.Kind);
        Assert.Equal(120, layer.Defaults.X);
        Assert.Equal(80, layer.Defaults.Y);
        Assert.Equal(400, layer.Defaults.Width);
        Assert.Equal(100, layer.Defaults.Height);
        Assert.Equal(3, layer.Style.PathPoints.Count);
        Assert.Equal(0, layer.Style.PathPoints[0].X);
        Assert.Equal(0.35, layer.Style.PathPoints[1].X, 2);
        Assert.Equal(1, layer.Style.PathPoints[2].X);
    }

    [Fact]
    public void VectorPathService_TryMovePoint_UpdatesNormalizedPoint()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Stroke",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 50 },
                new VectorPointModel { X = 300, Y = 150 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 4);
        var snapshot = TimelineInterpolationService.SampleLayer(layer, 0);

        var changed = VectorPathService.TryMovePoint(
            layer,
            snapshot,
            1,
            new VectorPointModel { X = 250, Y = 100 });

        Assert.True(changed);
        Assert.Equal(0.75, layer.Style.PathPoints[1].X, 2);
        Assert.Equal(0.5, layer.Style.PathPoints[1].Y, 2);
    }

    [Fact]
    public void VectorPathEditingService_InsertPointAfter_SplitsBezierSegment()
    {
        var points = new List<VectorPointModel>
        {
            new()
            {
                X = 0,
                Y = 0,
                HandleMode = VectorHandleMode.Smooth,
                OutHandleX = 2,
                OutHandleY = 4
            },
            new()
            {
                X = 10,
                Y = 0,
                HandleMode = VectorHandleMode.Smooth,
                InHandleX = 8,
                InHandleY = 4
            }
        };

        var changed = VectorPathEditingService.InsertPointAfter(points, 0, isClosed: false);

        Assert.True(changed);
        Assert.Equal(3, points.Count);
        Assert.Equal(5d, points[1].X, 3);
        Assert.Equal(3d, points[1].Y, 3);
        Assert.Equal(VectorHandleMode.Smooth, points[1].HandleMode);
        Assert.NotNull(points[1].InHandleX);
        Assert.NotNull(points[1].InHandleY);
        Assert.NotNull(points[1].OutHandleX);
        Assert.NotNull(points[1].OutHandleY);
    }

    [Fact]
    public void TimelineEditingService_SetPathPointHandleMode_CreatesSmoothHandles()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Stroke",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0, Y = 0 },
                new VectorPointModel { X = 0.5, Y = 1 },
                new VectorPointModel { X = 1, Y = 0 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 4);

        var changed = TimelineEditingService.SetPathPointHandleMode(
            layer,
            1,
            isClosed: false,
            VectorHandleMode.Smooth,
            time: 0,
            createKeyframe: false,
            duration: 2);

        Assert.True(changed);
        Assert.Equal(VectorHandleMode.Smooth, layer.Style.PathPoints[1].HandleMode);
        Assert.NotNull(layer.Style.PathPoints[1].InHandleX);
        Assert.NotNull(layer.Style.PathPoints[1].OutHandleX);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_VectorLayerStyle()
    {
        var document = new TimelineDocument
        {
            Layers =
            [
                TimelineEditingService.CreatePathLayer(
                    "Stroke",
                    "#24E5C1",
                    "#93A6D8",
                    [
                        new VectorPointModel { X = 100, Y = 50 },
                        new VectorPointModel { X = 200, Y = 160 },
                        new VectorPointModel { X = 320, Y = 90 }
                    ],
                    isClosed: true,
                    zIndex: 0,
                    strokeThickness: 8,
                    useGradient: true,
                    gradientFrom: "#24E5C1",
                    gradientTo: "#61E6FF")
            ]
        };
        var sourcePathLayer = Assert.Single(document.Layers);
        sourcePathLayer.Style.GradientCenterX = 0.35d;
        sourcePathLayer.Style.GradientCenterY = 0.65d;
        sourcePathLayer.Style.GradientScaleX = 1.4d;
        sourcePathLayer.Style.GradientScaleY = 0.8d;
        sourcePathLayer.Style.PathPoints[1].HandleMode = VectorHandleMode.Smooth;
        sourcePathLayer.Style.PathPoints[1].InHandleX = 0.3d;
        sourcePathLayer.Style.PathPoints[1].InHandleY = 0.45d;
        sourcePathLayer.Style.PathPoints[1].OutHandleX = 0.55d;
        sourcePathLayer.Style.PathPoints[1].OutHandleY = 0.65d;
        sourcePathLayer.Style.PathPoints[1].StrokeWidthScale = 1.8d;

        var json = DocumentSerializer.ToJson(document);
        var restored = DocumentSerializer.FromJson(json);
        var pathLayer = Assert.Single(restored.Layers);

        Assert.Equal(LayerKind.Path, pathLayer.Kind);
        Assert.True(pathLayer.Style.UseGradient);
        Assert.Equal(0.35d, pathLayer.Style.GradientCenterX, 3);
        Assert.Equal(0.65d, pathLayer.Style.GradientCenterY, 3);
        Assert.Equal(1.4d, pathLayer.Style.GradientScaleX, 3);
        Assert.Equal(0.8d, pathLayer.Style.GradientScaleY, 3);
        Assert.True(pathLayer.Style.IsClosed);
        Assert.Equal(8, pathLayer.Style.StrokeThickness);
        Assert.Equal(3, pathLayer.Style.PathPoints.Count);
        Assert.Equal(VectorHandleMode.Smooth, pathLayer.Style.PathPoints[1].HandleMode);
        Assert.Equal(1.8d, pathLayer.Style.PathPoints[1].StrokeWidthScale, 3);
        Assert.Equal("#61E6FF", pathLayer.Style.GradientTo);
    }

    [Fact]
    public void TimelineEasingService_CustomBezier_UsesControlPoints()
    {
        var keyframe = new KeyframeModel
        {
            Easing = EasingKind.Custom,
            ControlPoint1X = 0.15,
            ControlPoint1Y = 0.9,
            ControlPoint2X = 0.8,
            ControlPoint2Y = 0.1
        };

        var linear = TimelineEasingService.Apply(EasingKind.Linear, 0.35);
        var custom = TimelineEasingService.Apply(keyframe, 0.35);

        Assert.NotEqual(linear, custom);
        Assert.InRange(custom, 0d, 1d);
    }

    [Fact]
    public void SampleLayer_Interpolates_PathShapeKeyframes()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Morph",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        TimelineEditingService.SetShapeKeyframe(
            layer,
            0,
            [
                new VectorPointModel { X = 0, Y = 0 },
                new VectorPointModel { X = 1, Y = 0 },
                new VectorPointModel { X = 1, Y = 1 }
            ],
            isClosed: false,
            duration: 2);
        TimelineEditingService.SetShapeKeyframe(
            layer,
            1,
            [
                new VectorPointModel { X = 0, Y = 0.5 },
                new VectorPointModel { X = 1, Y = 0.25 },
                new VectorPointModel { X = 0.5, Y = 1 }
            ],
            isClosed: true,
            duration: 2);

        var sampled = TimelineInterpolationService.SampleLayer(layer, 0.5);

        Assert.Equal(3, sampled.PathPoints.Count);
        Assert.Equal(0.25, sampled.PathPoints[0].Y, 2);
        Assert.Equal(0.75, sampled.PathPoints[2].X, 2);
        Assert.False(sampled.IsClosed);
    }

    [Fact]
    public void SampleLayer_Interpolates_PathHandlesAndStrokeScale()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Morph",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        TimelineEditingService.SetShapeKeyframe(
            layer,
            0,
            [
                new VectorPointModel { X = 0, Y = 0, OutHandleX = 0.2, OutHandleY = 0.1, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 0.8 },
                new VectorPointModel { X = 0.8, Y = 0.2, InHandleX = 0.6, InHandleY = 0.1, OutHandleX = 0.9, OutHandleY = 0.4, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 1.2 },
                new VectorPointModel { X = 1, Y = 1, InHandleX = 0.9, InHandleY = 0.8, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 1.5 }
            ],
            isClosed: false,
            duration: 2);
        TimelineEditingService.SetShapeKeyframe(
            layer,
            1,
            [
                new VectorPointModel { X = 0, Y = 0.2, OutHandleX = 0.3, OutHandleY = 0.2, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 1.4 },
                new VectorPointModel { X = 0.6, Y = 0.5, InHandleX = 0.5, InHandleY = 0.3, OutHandleX = 0.8, OutHandleY = 0.7, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 2.0 },
                new VectorPointModel { X = 0.9, Y = 1, InHandleX = 0.8, InHandleY = 0.9, HandleMode = VectorHandleMode.Smooth, StrokeWidthScale = 2.5 }
            ],
            isClosed: false,
            duration: 2);

        var sampled = TimelineInterpolationService.SampleLayer(layer, 0.5);

        Assert.Equal(0.1d, sampled.PathPoints[0].Y, 3);
        Assert.Equal(0.25d, sampled.PathPoints[0].OutHandleX!.Value, 3);
        Assert.Equal(1.1d, sampled.PathPoints[0].StrokeWidthScale, 3);
        Assert.Equal(1.6d, sampled.PathPoints[1].StrokeWidthScale, 3);
        Assert.Equal(0.85d, sampled.PathPoints[2].InHandleX!.Value, 3);
    }

    [Fact]
    public void CopyPasteRange_PreservesShapeMorphKeys()
    {
        var source = TimelineEditingService.CreatePathLayer(
            "Morph",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);
        TimelineEditingService.SetShapeKeyframe(
            source,
            0.5,
            [
                new VectorPointModel { X = 0, Y = 0.4 },
                new VectorPointModel { X = 1, Y = 0.2 },
                new VectorPointModel { X = 0.7, Y = 1 }
            ],
            isClosed: true,
            duration: 3);

        var clipboard = FrameTimelineService.CopyRange(source, 0, 24, 73, 24);
        var target = TimelineEditingService.CreatePathLayer(
            "Target",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        FrameTimelineService.PasteRange(target, 24, clipboard, 73, 24);

        Assert.NotEmpty(target.ShapeKeyframes);
        Assert.Contains(target.ShapeKeyframes, keyframe => Math.Abs(keyframe.Time - 1.0) < 0.0001d);
        Assert.True(target.ShapeKeyframes.Last().IsClosed);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_CompositingSettings()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        layer.Compositing.Role = LayerCompositeRole.Mask;
        layer.Compositing.MaskLayerCount = 2;
        layer.Compositing.BlendMode = LayerBlendMode.Screen;
        layer.Compositing.GlowColor = "#FFD166";
        layer.Compositing.GlowOpacity = 0.55d;
        layer.Compositing.ShadowOpacity = 0.32d;
        layer.Compositing.ShadowOffsetX = 18d;
        layer.Compositing.ShadowOffsetY = 12d;
        layer.Compositing.TintColor = "#61E6FF";
        layer.Compositing.TintStrength = 0.2d;
        layer.Compositing.Brightness = 0.08d;
        layer.Compositing.Saturation = 1.4d;
        layer.Compositing.ParallaxDepth = 1.3d;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument { Layers = [layer] }));
        var compositing = Assert.Single(restored.Layers).Compositing;

        Assert.Equal(LayerCompositeRole.Mask, compositing.Role);
        Assert.Equal(2, compositing.MaskLayerCount);
        Assert.Equal(LayerBlendMode.Screen, compositing.BlendMode);
        Assert.Equal("#FFD166", compositing.GlowColor);
        Assert.Equal(0.32d, compositing.ShadowOpacity);
        Assert.Equal(1.3d, compositing.ParallaxDepth);
    }

    [Fact]
    public void CompositeFrameRenderer_TransformsSnapshotWithCameraAndParallax()
    {
        var snapshot = new LayerSnapshot(
            200,
            120,
            240,
            140,
            1,
            1,
            0,
            0,
            0,
            1,
            28,
            28,
            28,
            28,
            28,
            "#24E5C1",
            "#FFFFFF",
            string.Empty,
            42,
            2,
            false,
            "#24E5C1",
            "#FFFFFF",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            false,
            0d,
            360d,
            false,
            "#57C9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings { ParallaxDepth = 2d },
            new LayerTextSettings());
        var camera = new LayerSnapshot(
            80,
            40,
            640,
            360,
            1,
            1,
            0,
            0,
            0,
            1,
            0,
            0,
            0,
            0,
            0,
            "#15223A",
            "#FFD166",
            string.Empty,
            42,
            2,
            false,
            "#15223A",
            "#FFD166",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            false,
            0d,
            360d,
            false,
            "#57C9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings { Role = LayerCompositeRole.Camera },
            new LayerTextSettings());

        var transformed = CompositeFrameRenderer.TransformSnapshot(snapshot, camera, 1280, 720, snapshot.Compositing.ParallaxDepth);

        Assert.Equal(320d, transformed.X, 3);
        Assert.Equal(200d, transformed.Y, 3);
        Assert.Equal(480d, transformed.Width, 3);
        Assert.Equal(280d, transformed.Height, 3);
    }

    [Fact]
    public void CompositeFrameRenderer_ResolvesTopmostCameraSample()
    {
        var baseSettings = new LayerCompositeSettings();
        var cameraSettings = new LayerCompositeSettings { Role = LayerCompositeRole.Camera };
        var firstCamera = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 0,
            Snapshot = new LayerSnapshot(0, 0, 1280, 720, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, "#000000", "#000000", string.Empty, 0, 1, false, "#000000", "#000000", ShapeDrawingMode.Merge, PrimitiveShapeType.None, false, 0d, 360d, false, "#57C9FF", [], 5, 0.46d, true, new AvaloniaControlSettings(), cameraSettings, new LayerTextSettings())
        };
        var topCamera = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 2,
            Snapshot = new LayerSnapshot(80, 24, 1120, 630, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, "#000000", "#000000", string.Empty, 0, 1, false, "#000000", "#000000", ShapeDrawingMode.Merge, PrimitiveShapeType.None, false, 0d, 360d, false, "#57C9FF", [], 5, 0.46d, true, new AvaloniaControlSettings(), new LayerCompositeSettings { Role = LayerCompositeRole.Camera }, new LayerTextSettings())
        };
        var content = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 1,
            Snapshot = new LayerSnapshot(120, 80, 320, 180, 1, 1, 0, 0, 0, 1, 28, 28, 28, 28, 28, "#24E5C1", "#FFFFFF", string.Empty, 0, 1, false, "#24E5C1", "#FFFFFF", ShapeDrawingMode.Merge, PrimitiveShapeType.Rectangle, false, 0d, 360d, false, "#57C9FF", [], 5, 0.46d, true, new AvaloniaControlSettings(), baseSettings, new LayerTextSettings())
        };

        var resolved = CompositeFrameRenderer.ResolveActiveCamera([firstCamera, content, topCamera]);

        Assert.NotNull(resolved);
        Assert.Equal(80, resolved.Value.X);
        Assert.Equal(1120, resolved.Value.Width);
    }

    [Fact]
    public void RendererPreferencesPersistenceService_RoundTrips_SelectedEngine()
    {
        var state = new RendererPreferencesState
        {
            SelectedEngine = RenderingEngineKind.SkiaSharp
        };

        RendererPreferencesPersistenceService.Save(state);
        var restored = RendererPreferencesPersistenceService.Load();

        Assert.Equal(RenderingEngineKind.SkiaSharp, restored.SelectedEngine);
    }

    [Fact]
    public void MainWindowViewModel_Updates_RenderingEngineHost_WhenSelectionChanges()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SelectedRenderingEngine = RenderingEngineKind.SkiaSharp;

        Assert.Equal(RenderingEngineKind.SkiaSharp, RenderingEngineHostService.SelectedEngine);
        Assert.Contains("SkiaSharp", viewModel.RenderingEngineSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void SceneRenderPreparationBuilder_ReusesDocumentAssetLookups_WhenAssetsAreStable()
    {
        var document = SampleProjectFactory.Create();
        var frameRate = document.Scenes[0].FrameRate;

        var first = SceneRenderPreparationBuilder.Build(document, document.Layers, 0.5d, document.Duration, frameRate);
        var second = SceneRenderPreparationBuilder.Build(document, document.Layers, 0.75d, document.Duration, frameRate);

        Assert.Same(first.LibraryItems, second.LibraryItems);
        Assert.Same(first.MediaAssets, second.MediaAssets);
    }

    [Fact]
    public void LayerHierarchyService_GetRenderableLayers_ReusesCachedResult_AndInvalidatesOnHierarchyChange()
    {
        var document = CreateHierarchyInteropDocument();

        var first = LayerHierarchyService.GetRenderableLayers(document.Layers);
        var second = LayerHierarchyService.GetRenderableLayers(document.Layers);

        Assert.Same(first, second);

        document.Layers[0].IsExpanded = false;

        var third = LayerHierarchyService.GetRenderableLayers(document.Layers);

        Assert.NotSame(first, third);
    }

    [Fact]
    public void SkiaSceneRenderEngine_Uses_InjectedFallback_ForUnsupportedSamples()
    {
        var fallback = new RecordingLayerRenderFallback();
        var engine = new SkiaSceneRenderEngine(fallback);
        var sample = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 0,
            Snapshot = new LayerSnapshot(
                0d,
                0d,
                8d,
                8d,
                1d,
                1d,
                0d,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d,
                0d,
                "#FF0000",
                "#000000",
                string.Empty,
                0d,
                1d,
                false,
                "#FF0000",
                "#000000",
                ShapeDrawingMode.Merge,
                PrimitiveShapeType.Rectangle,
                false,
                0d,
                360d,
                false,
                "#57C9FF",
                [],
                5,
                0.46d,
                true,
                new AvaloniaControlSettings(),
                new LayerCompositeSettings(),
                new LayerTextSettings()),
            Warp = new LayerWarpSettings
            {
                IsEnabled = true,
                Pins =
                [
                    new WarpPinModel { X = 2d, Y = 2d, BindX = 2d, BindY = 2d }
                ]
            }
        };
        var request = new SceneRenderRequest(
            0d,
            8d,
            8d,
            false,
            "#000000",
            "#000000",
            new AnimateDocumentSettings(),
            new Dictionary<Guid, LibraryItem>(),
            new Dictionary<Guid, MediaAsset>(),
            [sample],
            activeCamera: null);

        var rendered = engine.Render(request, includeBackground: false);

        Assert.Equal(1, fallback.CallCount);
        Assert.False(fallback.LastRenderAsMask);
        Assert.Equal((byte)255, rendered.Pixels[2]);
        Assert.Equal((byte)255, rendered.Pixels[3]);
    }

    [Fact]
    public void SkiaSceneRenderEngine_Renders_BlurShadowAndGlow_Natively_WithoutFallback()
    {
        var fallback = new RecordingLayerRenderFallback();
        var engine = new SkiaSceneRenderEngine(fallback);
        var compositing = new LayerCompositeSettings
        {
            BlurRadius = 12d,
            BlurQuality = LayerFilterQuality.Medium,
            GlowSize = 16d,
            GlowOpacity = 0.42d,
            GlowColor = "#61E6FF",
            GlowQuality = LayerFilterQuality.Low,
            ShadowBlur = 14d,
            ShadowOpacity = 0.35d,
            ShadowOffsetX = 10d,
            ShadowOffsetY = 6d,
            ShadowColor = "#09111F",
            ShadowQuality = LayerFilterQuality.Medium
        };
        var sample = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 0,
            Snapshot = new LayerSnapshot(
                12d,
                10d,
                36d,
                24d,
                1d,
                1d,
                0d,
                0d,
                0d,
                1d,
                10d,
                10d,
                10d,
                10d,
                10d,
                "#24E5C1",
                "#09111F",
                string.Empty,
                6d,
                1d,
                false,
                "#24E5C1",
                "#09111F",
                ShapeDrawingMode.Merge,
                PrimitiveShapeType.Rectangle,
                false,
                0d,
                360d,
                false,
                "#57C9FF",
                [],
                5,
                0.46d,
                true,
                new AvaloniaControlSettings(),
                compositing,
                new LayerTextSettings()),
            Warp = new LayerWarpSettings(),
            Rig = new LayerRigSettings()
        };
        var request = new SceneRenderRequest(
            0d,
            96d,
            64d,
            false,
            "#000000",
            "#000000",
            new AnimateDocumentSettings(),
            new Dictionary<Guid, LibraryItem>(),
            new Dictionary<Guid, MediaAsset>(),
            [sample],
            activeCamera: null);

        var rendered = engine.Render(request, includeBackground: false);

        Assert.Equal(0, fallback.CallCount);
        Assert.True(rendered.Pixels.Any(static channel => channel != 0));
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_MediaAssetsAndLayerSettings()
    {
        var video = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 2.4d,
            FrameRate = 12d,
            Width = 640,
            Height = 360,
            PreviewFill = "#223352",
            PreviewAccent = "#FFD166",
            Frames =
            [
                new MediaFrameModel { Duration = 0.2d, Fill = "#223352", Accent = "#61E6FF", Label = "A" },
                new MediaFrameModel { Duration = 0.2d, Fill = "#30435F", Accent = "#FFD166", Label = "B" }
            ]
        };
        var audio = new MediaAsset
        {
            Name = "Bed",
            Kind = MediaAssetKind.Audio,
            Duration = 3d,
            SampleRate = 48000,
            ChannelCount = 2,
            WaveformSamples = [0.12d, 0.42d, 0.28d, 0.58d]
        };

        var videoLayer = TimelineEditingService.CreateVideoLayer(video, 120, 80, 0, 0.5d);
        videoLayer.Media.PlaybackMode = MediaPlaybackMode.Start;
        videoLayer.Media.ClipOffset = 0.2d;
        videoLayer.Media.ClipDuration = 1.4d;
        var audioLayer = TimelineEditingService.CreateAudioLayer(audio, 1, 0.25d);
        audioLayer.Media.Loop = true;
        audioLayer.Media.Volume = 0.7d;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument
        {
            MediaAssets = [video, audio],
            Layers = [videoLayer, audioLayer]
        }));

        Assert.Equal(2, restored.MediaAssets.Count);
        Assert.Equal(MediaAssetKind.Video, restored.MediaAssets[0].Kind);
        Assert.Equal(2, restored.MediaAssets[0].Frames.Count);
        Assert.Equal(MediaAssetKind.Audio, restored.MediaAssets[1].Kind);
        Assert.Equal(4, restored.MediaAssets[1].WaveformSamples.Count);
        Assert.Equal(MediaPlaybackMode.Start, restored.Layers[0].Media.PlaybackMode);
        Assert.Equal(0.2d, restored.Layers[0].Media.ClipOffset);
        Assert.True(restored.Layers[1].Media.Loop);
        Assert.Equal(0.7d, restored.Layers[1].Media.Volume);
    }

    [Fact]
    public void MediaTimelineService_ResolvesStreamClipTime()
    {
        var asset = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 2d,
            FrameRate = 10d
        };
        var layer = TimelineEditingService.CreateVideoLayer(asset, 100, 80, 0, 0.5d);
        layer.Media.ClipOffset = 0.25d;
        layer.Media.ClipDuration = 1d;
        layer.Media.PlaybackMode = MediaPlaybackMode.Stream;

        Assert.Null(MediaTimelineService.ResolveMediaTime(layer, asset, 0.4d));
        Assert.Equal(0.5d, MediaTimelineService.ResolveMediaTime(layer, asset, 0.75d));
        Assert.Null(MediaTimelineService.ResolveMediaTime(layer, asset, 1.8d));
    }

    [Fact]
    public void MediaTimelineService_ResolvesLoopingClipTime()
    {
        var asset = new MediaAsset
        {
            Name = "Loop",
            Kind = MediaAssetKind.Audio,
            Duration = 1.2d,
            SampleRate = 44100
        };
        var layer = TimelineEditingService.CreateAudioLayer(asset, 0, 0d);
        layer.Media.ClipOffset = 0.4d;
        layer.Media.ClipDuration = 3d;
        layer.Media.Loop = true;

        var mediaTime = MediaTimelineService.ResolveMediaTime(layer, asset, 2.2d);

        Assert.NotNull(mediaTime);
        Assert.Equal(0.2d, mediaTime!.Value, 3);
    }

    [Fact]
    public void SymbolRenderService_ActivatesVideoSamplesOnlyWhenClipIsPlaying()
    {
        var asset = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 1d,
            FrameRate = 6d,
            Width = 640,
            Height = 360,
            Frames =
            [
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#223352", Accent = "#61E6FF", Label = "A" }
            ]
        };
        var layer = TimelineEditingService.CreateVideoLayer(asset, 120, 90, 0, 0.6d);
        var document = new TimelineDocument
        {
            Duration = 3d,
            MediaAssets = [asset],
            Layers = [layer]
        };

        var beforeClip = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.2d, document.Duration, 24d);
        var duringClip = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.9d, document.Duration, 24d);

        Assert.Empty(beforeClip);
        var sample = Assert.Single(duringClip);
        Assert.Equal(LayerKind.Video, sample.Kind);
        Assert.Equal(asset.Id, sample.SourceMediaAssetId);
        Assert.NotNull(sample.MediaTime);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_ComponentBehaviors()
    {
        var component = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0),
            "CTA",
            SymbolKind.Button);
        component.IsComponent = true;
        component.ComponentCategory = "Controls";
        component.ComponentDescription = "Reusable button";
        component.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Jump",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.GoToFrameLabel,
                TargetFrameLabel = "reveal"
            }
        ];

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(component, 220, 180, 0);
        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument
        {
            LibraryItems = [component],
            Layers = [instance]
        }));

        Assert.True(restored.LibraryItems[0].IsComponent);
        Assert.Equal("Controls", restored.LibraryItems[0].ComponentCategory);
        Assert.Single(restored.LibraryItems[0].DefaultBehaviors);
        Assert.Single(restored.Layers[0].Behaviors);
        Assert.Equal("reveal", restored.Layers[0].Behaviors[0].TargetFrameLabel);
    }

    [Fact]
    public void CreateLayerFromLibraryItem_ClonesComponentBehaviors()
    {
        var component = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0),
            "Toggle Card",
            SymbolKind.Button);
        component.IsComponent = true;
        component.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Hide Card",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.SetLayerVisibility,
                BoolValue = false
            }
        ];

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(component, 320, 220, 0);
        component.DefaultBehaviors[0].VariableName = "mutated";

        Assert.Single(instance.Behaviors);
        Assert.Equal("Hide Card", instance.Behaviors[0].Name);
        Assert.True(string.IsNullOrWhiteSpace(instance.Behaviors[0].VariableName));
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_ComponentParametersAndOverrides()
    {
        var document = CreateComponentInteropDocument();

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));

        var component = Assert.Single(restored.LibraryItems);
        var definition = Assert.Single(component.ComponentParameters);
        var instance = Assert.Single(restored.Layers);
        var overrideValue = Assert.Single(instance.ComponentParameterOverrides);

        Assert.True(component.IsComponent);
        Assert.Equal("buttonLabel", definition.Name);
        Assert.Equal(ComponentParameterBindingTarget.Content, definition.BindingTarget);
        Assert.Equal("Launch", definition.DefaultValue);
        Assert.Equal("buttonLabel", overrideValue.Name);
        Assert.Equal("Launch", overrideValue.Value);
    }

    [Fact]
    public void InteractionBehaviorService_MatchesFrameLabelTrigger()
    {
        var behavior = new InteractionBehaviorModel
        {
            Trigger = InteractionTriggerKind.FrameLabelReached,
            TriggerArgument = "detail"
        };

        Assert.True(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.FrameLabelReached, "detail"));
        Assert.False(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.FrameLabelReached, "intro"));
        Assert.False(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.PointerClick, null));
    }

    [Fact]
    public void SampleProjectFactory_CreatesComponentLibraryItems()
    {
        var document = SampleProjectFactory.Create();

        Assert.Contains(document.LibraryItems, item => item.IsComponent);
        Assert.Contains(document.Scenes[0].Layers, layer => layer.Behaviors.Count > 0);
    }

    [Fact]
    public void MainWindowViewModel_PrototypeInteraction_RestoresEditorStateOnExit()
    {
        var viewModel = new MainWindowViewModel();
        var introSceneId = viewModel.SelectedScene?.Id;
        var triggerLayer = viewModel.Document.Layers.First(layer => layer.Behaviors.Count > 0);

        viewModel.TogglePrototypeModeCommand.Execute(null);
        viewModel.HandlePrototypeTrigger(triggerLayer.Id, InteractionTriggerKind.PointerClick);

        Assert.True(viewModel.IsPrototypeMode);
        Assert.NotEqual(introSceneId, viewModel.SelectedScene?.Id);

        viewModel.TogglePrototypeModeCommand.Execute(null);

        Assert.False(viewModel.IsPrototypeMode);
        Assert.Equal(introSceneId, viewModel.SelectedScene?.Id);
    }

    [Fact]
    public void MainWindowViewModel_WorkspaceCommands_UpdatePanelModes()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.FocusWorkspaceAreaCommand.Execute("inspector");
        viewModel.SetWorkspacePanelModeCommand.Execute("right:over");
        viewModel.SetWorkspacePanelModeCommand.Execute("timeline:hide");

        Assert.Equal(WorkspacePanelMode.Hidden, viewModel.LeftPanelMode);
        Assert.Equal(WorkspacePanelMode.Overlay, viewModel.RightPanelMode);
        Assert.Equal(WorkspacePanelMode.Hidden, viewModel.TimelinePanelMode);
        Assert.True(viewModel.IsRightPanelOverlay);
        Assert.True(viewModel.IsTimelinePanelHidden);
        Assert.True(viewModel.IsTimelinePanelCollapsedHandleVisible);
        Assert.True(viewModel.HasHiddenWorkspacePanels);
        Assert.Contains("Ctrl+1", viewModel.WorkspaceShortcutSummary, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Shift+3", viewModel.WorkspaceShortcutSummary, StringComparison.Ordinal);

        viewModel.ToggleWorkspacePanelCommand.Execute("left");
        viewModel.ToggleWorkspacePanelCommand.Execute("timeline");

        Assert.NotEqual(WorkspacePanelMode.Hidden, viewModel.LeftPanelMode);
        Assert.NotEqual(WorkspacePanelMode.Hidden, viewModel.TimelinePanelMode);
        Assert.False(viewModel.HasHiddenWorkspacePanels);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_PublishProfiles()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        document.PublishProfiles[0].OutputKind = PublishOutputKind.Gif;
        document.PublishProfiles[0].Width = 1920;
        document.PublishProfiles[0].Height = 1080;
        document.PublishProfiles[0].FrameRate = 30;
        document.PublishProfiles[0].TransparentBackground = true;
        document.PublishProfiles[0].SpriteSheetColumns = 8;
        document.PublishProfiles[0].Quality = 72;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));
        var restoredProfile = Assert.Single(restored.PublishProfiles, item => item.Id == document.PublishProfiles[0].Id);

        Assert.Equal(document.PublishProfiles.Count, restored.PublishProfiles.Count);
        Assert.Equal(PublishOutputKind.Gif, restoredProfile.OutputKind);
        Assert.Equal(1920, restoredProfile.Width);
        Assert.Equal(1080, restoredProfile.Height);
        Assert.Equal(30, restoredProfile.FrameRate);
        Assert.True(restoredProfile.TransparentBackground);
        Assert.Equal(8, restoredProfile.SpriteSheetColumns);
        Assert.Equal(72, restoredProfile.Quality);
    }

    [Fact]
    public void PublishValidationService_FlagsMissingBehaviorTargets()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        var triggerLayer = document.Scenes[0].Layers.First();
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Scene",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.GoToScene,
            TargetSceneId = Guid.NewGuid()
        });
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Label",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.GoToFrameLabel,
            TargetFrameLabel = "missing_label"
        });
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Layer",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.SetLayerVisibility,
            TargetLayerId = Guid.NewGuid(),
            BoolValue = false
        });

        var issues = PublishValidationService.Validate(document, document.PublishProfiles[0]);

        Assert.Contains(issues, issue => issue.Message.Contains("targets a missing scene", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("targets a frame label that does not exist", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("targets a layer that does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishExportService_ExportsSceneGraphJson()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        var profile = document.PublishProfiles[0].Clone();
        profile.Name = "Scene Graph Review";
        profile.OutputKind = PublishOutputKind.JsonSceneGraph;

        var outputRoot = Path.Combine(Path.GetTempPath(), $"timeline_publish_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);
        var outputPath = Path.Combine(outputRoot, "scene-graph.json");

        try
        {
            var result = await PublishExportService.ExportAsync(document, profile, outputPath);

            Assert.True(File.Exists(outputPath));
            Assert.Equal(outputPath, result.PrimaryOutputPath);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            Assert.Equal(document.Name, json.RootElement.GetProperty("document").GetProperty("name").GetString());
            Assert.Equal("Scene Graph Review", json.RootElement.GetProperty("profile").GetProperty("name").GetString());
            Assert.True(json.RootElement.GetProperty("scenes").GetArrayLength() >= 1);
            Assert.True(json.RootElement.GetProperty("library").GetArrayLength() >= 1);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ActionScriptService_ParsesQuotedCommands()
    {
        var script = """
                     set_text "Launch Caption" "FLASH READY"
                     set_button this Over
                     trace "Cue fired"
                     """;

        var result = ActionScriptService.Parse(script);

        Assert.False(result.HasErrors);
        Assert.Collection(
            result.Commands,
            first =>
            {
                Assert.Equal(ActionScriptCommandKind.SetText, first.Kind);
                Assert.Equal(["Launch Caption", "FLASH READY"], first.Arguments);
            },
            second =>
            {
                Assert.Equal(ActionScriptCommandKind.SetButtonState, second.Kind);
                Assert.Equal(["this", "Over"], second.Arguments);
            },
            third =>
            {
                Assert.Equal(ActionScriptCommandKind.Trace, third.Kind);
                Assert.Equal(["Cue fired"], third.Arguments);
            });
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_ActionScripts()
    {
        var document = SampleProjectFactory.Create();
        document.Scenes[0].FrameLabels[0].Script = "trace \"intro\"";
        document.Scenes[0].Layers.First(layer => layer.Behaviors.Count > 0).Behaviors[0].Script = "set_var beat one";

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));

        Assert.Equal("trace \"intro\"", restored.Scenes[0].FrameLabels[0].Script);
        Assert.Equal("set_var beat one", restored.Scenes[0].Layers.First(layer => layer.Behaviors.Count > 0).Behaviors[0].Script);
    }

    [Fact]
    public void TimelineDocumentFileService_DetectsSupportedFormats()
    {
        Assert.Equal(TimelineDocumentFileFormat.NativeProject, TimelineDocumentFileService.DetectFormat("scene.timeline.json"));
        Assert.Equal(TimelineDocumentFileFormat.AvaloniaXaml, TimelineDocumentFileService.DetectFormat("view.axaml"));
        Assert.Equal(TimelineDocumentFileFormat.FlashXfl, TimelineDocumentFileService.DetectFormat("authoring.xfl"));
        Assert.Equal(TimelineDocumentFileFormat.FlashXfl, TimelineDocumentFileService.DetectFormat("authoring.fla"));
        Assert.Equal(TimelineDocumentFileFormat.SvgSmil, TimelineDocumentFileService.DetectFormat("motion.svg"));
        Assert.Equal(TimelineDocumentFileFormat.HtmlCss, TimelineDocumentFileService.DetectFormat("motion.html"));
        Assert.Equal(
            TimelineDocumentFileFormat.AvaloniaXaml,
            TimelineDocumentFileService.DetectFormat("unknown.txt", """<Canvas xmlns="https://github.com/avaloniaui" Width="320" Height="180" />"""));
        Assert.Equal(
            TimelineDocumentFileFormat.FlashXfl,
            TimelineDocumentFileService.DetectFormat("unknown.txt", """<DOMDocument xmlns="http://ns.adobe.com/xfl/2008/" name="FlashDoc" />"""));
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadsAndSaves_NativeProject()
    {
        var document = CreateInteropDocument();
        await using var stream = new MemoryStream();

        var save = await TimelineDocumentFileService.SaveAsync(stream, document, TimelineDocumentFileFormat.NativeProject);
        stream.Position = 0;
        var loaded = await TimelineDocumentFileService.LoadAsync(stream, "interop.timeline.json");

        Assert.Equal(TimelineDocumentFileFormat.NativeProject, save.Format);
        Assert.Equal(TimelineDocumentFileFormat.NativeProject, loaded.Format);
        Assert.Equal(document.Name, loaded.Document.Name);
        Assert.Empty(loaded.Issues);
    }

    [Theory]
    [InlineData(TimelineDocumentFileFormat.AvaloniaXaml, "interop.axaml")]
    [InlineData(TimelineDocumentFileFormat.FlashXfl, "interop.xfl")]
    [InlineData(TimelineDocumentFileFormat.SvgSmil, "interop.svg")]
    [InlineData(TimelineDocumentFileFormat.HtmlCss, "interop.html")]
    public async Task TimelineDocumentFileService_LoadsAndSaves_ExchangeFormats(TimelineDocumentFileFormat format, string fileName)
    {
        var document = CreateInteropDocument();
        await using var stream = new MemoryStream();

        var save = await TimelineDocumentFileService.SaveAsync(stream, document, format);
        stream.Position = 0;
        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var exportedContent = await reader.ReadToEndAsync();
            Assert.DoesNotContain("TimelineAnimationsMetadata", exportedContent, StringComparison.Ordinal);
            Assert.DoesNotContain("timeline-animations-metadata", exportedContent, StringComparison.Ordinal);
        }

        stream.Position = 0;
        var loaded = await TimelineDocumentFileService.LoadAsync(stream, fileName);

        Assert.Equal(format, save.Format);
        Assert.Equal(format, loaded.Format);
        Assert.True(loaded.Document.Layers.Count >= 2);
        Assert.DoesNotContain(save.Issues, issue => string.Equals(issue.Source, "Metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadsFlashPackage_FromFlaArchive()
    {
        var document = CreateInteropDocument();
        var xfl = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl).Content;
        await using var stream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("DOMDocument.xml");
            await using var entryStream = entry.Open();
            await using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(xfl);
            await writer.FlushAsync();
        }

        stream.Position = 0;
        var loaded = await TimelineDocumentFileService.LoadAsync(stream, "interop.fla");

        Assert.Equal(TimelineDocumentFileFormat.FlashXfl, loaded.Format);
        Assert.True(loaded.Document.Scenes.Count >= 1);
        Assert.True(loaded.Document.Layers.Count >= 2);
    }

    [Fact]
    public async Task TimelineDocumentFileService_SavesFlashPackage_ToFlaArchive()
    {
        var document = CreateInteropDocument();
        await using var stream = new MemoryStream();

        var save = await TimelineDocumentFileService.SaveAsync(stream, document, TimelineDocumentFileFormat.FlashXfl, "interop.fla");
        stream.Position = 0;

        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("DOMDocument.xml");

        Assert.NotNull(entry);
        Assert.Equal(TimelineDocumentFileFormat.FlashXfl, save.Format);
        Assert.Contains(save.Issues, issue => issue.Message.Contains("packaged Flash authoring archive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FlashAuthoringPackageService_DetectsLegacyBinaryFla()
    {
        byte[] legacyBytes = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        var detection = FlashAuthoringPackageService.Detect(legacyBytes, "legacy.fla");

        Assert.Equal(FlashAuthoringContainerKind.LegacyBinaryFla, detection.Kind);
        Assert.False(detection.IsSupported);
        Assert.Contains("legacy binary FLA", detection.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimelineDocumentFileService_SavesAndLoads_FlashXflFolder()
    {
        var document = CreateInteropDocument();
        document.Animate.Type = AnimateDocumentType.AirDesktop;
        document.Animate.IsResponsive = false;
        document.Animate.UseExternalScriptFile = false;
        document.Animate.UseWebFonts = false;
        document.Animate.TemplateName = "AIR Desktop";
        document.PublishProfiles =
        [
            new PublishProfile
            {
                Name = "Archive",
                OutputKind = PublishOutputKind.SpriteSheet,
                Width = 1920,
                Height = 1080,
                FrameRate = 30d,
                PlayAllScenes = true,
                IncludeSceneGraph = true,
                IncludeValidationReport = false,
                IncludeAudio = true,
                SpriteSheetColumns = 8,
                Quality = 82
            }
        ];
        document.MediaAssets.Add(new MediaAsset
        {
            Name = "Impact",
            Kind = MediaAssetKind.Audio,
            SourceLabel = "impact.wav",
            Duration = 0.5d,
            SampleRate = 44100,
            ChannelCount = 2,
            AudioBase64 = Convert.ToBase64String([0x52, 0x49, 0x46, 0x46, 0x00])
        });
        document.MediaAssets.Add(new MediaAsset
        {
            Name = "Poster",
            Kind = MediaAssetKind.Video,
            SourceLabel = "poster.png",
            Duration = 1d,
            FrameRate = 24d,
            Width = 1,
            Height = 1,
            Frames =
            [
                new MediaFrameModel
                {
                    Duration = 1d / 24d,
                    ImageBase64 = OnePixelPngBase64,
                    Label = "F01"
                }
            ]
        });
        document.FlashAuthoringSupplementalFiles.Add(new FlashAuthoringSupplementalFile
        {
            RelativePath = "META-INF/custom-note.txt",
            Data = Encoding.UTF8.GetBytes("preserve-me")
        });
        var rootPath = Path.Combine(Path.GetTempPath(), $"timeline_xfl_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(rootPath);
            var save = await TimelineDocumentFileService.SaveFlashFolderAsync(rootPath, document);
            var folderPath = Path.Combine(rootPath, save.SuggestedFileName);

            Assert.True(Directory.Exists(folderPath));
            Assert.True(File.Exists(Path.Combine(folderPath, "DOMDocument.xml")));
            Assert.True(File.Exists(Path.Combine(folderPath, "PublishSettings.xml")));
            Assert.True(File.Exists(Path.Combine(folderPath, "MEDIA", "Impact.wav")));
            Assert.True(File.Exists(Path.Combine(folderPath, "MEDIA", "Poster.png")));
            Assert.True(File.Exists(Path.Combine(folderPath, "META-INF", "custom-note.txt")));

            var loaded = await TimelineDocumentFileService.LoadFlashFolderAsync(folderPath);

            Assert.Equal(TimelineDocumentFileFormat.FlashXfl, loaded.Format);
            Assert.Equal(document.Name, loaded.Document.Name);
            Assert.True(loaded.Document.Scenes.Count >= 1);
            Assert.True(loaded.Document.Layers.Count >= 2);
            Assert.Equal(AnimateDocumentType.AirDesktop, loaded.Document.Animate.Type);
            Assert.Equal("Archive", Assert.Single(loaded.Document.PublishProfiles).Name);
            Assert.Equal(2, loaded.Document.MediaAssets.Count);
            Assert.Contains(loaded.Document.MediaAssets, asset => asset.Kind == MediaAssetKind.Audio && !string.IsNullOrWhiteSpace(asset.AudioBase64));
            Assert.Contains(loaded.Document.MediaAssets, asset => asset.Kind == MediaAssetKind.Video && asset.Frames.Count == 1 && !string.IsNullOrWhiteSpace(asset.Frames[0].ImageBase64));
            Assert.Contains(loaded.Document.FlashAuthoringSupplementalFiles, file => file.RelativePath == "META-INF/custom-note.txt");
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TimelineDocumentFileService_SaveFlashFolder_WritesAuthoringHelperFiles()
    {
        var document = CreateInteropDocument();
        var rootPath = Path.Combine(Path.GetTempPath(), $"timeline_xfl_helpers_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(rootPath);
            var save = await TimelineDocumentFileService.SaveFlashFolderAsync(rootPath, document);
            var folderPath = Path.Combine(rootPath, save.SuggestedFileName);

            Assert.True(File.Exists(Path.Combine(folderPath, "mimetype")));
            Assert.True(File.Exists(Path.Combine(folderPath, "META-INF", "metadata.xml")));
            Assert.True(File.Exists(Path.Combine(folderPath, "MobileSettings.xml")));
            Assert.Single(Directory.EnumerateFiles(folderPath, "*.xfl", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadFlashFolder_SupportsRootProjectXflDocument()
    {
        var document = CreateInteropDocument();
        var rootPath = Path.Combine(Path.GetTempPath(), $"timeline_xfl_project_{Guid.NewGuid():N}.xfl");

        try
        {
            Directory.CreateDirectory(rootPath);
            var save = await TimelineDocumentFileService.SaveFlashFolderAsync(rootPath, document, "interop-project.xfl");
            var folderPath = Path.Combine(rootPath, save.SuggestedFileName);
            File.Delete(Path.Combine(folderPath, "DOMDocument.xml"));

            var loaded = await TimelineDocumentFileService.LoadFlashFolderAsync(folderPath);

            Assert.Equal(TimelineDocumentFileFormat.FlashXfl, loaded.Format);
            Assert.Equal(document.Name, loaded.Document.Name);
            Assert.True(loaded.Document.Scenes.Count >= 1);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TimelineDocumentFileService_Converts_FlaArchive_ToXflFolder_AndBack()
    {
        var document = CreateInteropDocument();
        await using var archiveStream = new MemoryStream();
        var rootPath = Path.Combine(Path.GetTempPath(), $"timeline_fla_convert_{Guid.NewGuid():N}");

        try
        {
            await TimelineDocumentFileService.SaveAsync(archiveStream, document, TimelineDocumentFileFormat.FlashXfl, "interop.fla");
            archiveStream.Position = 0;

            Directory.CreateDirectory(rootPath);
            var toFolder = await TimelineDocumentFileService.ConvertFlashArchiveToFolderAsync(archiveStream, "interop.fla", rootPath);

            Assert.True(Directory.Exists(toFolder.OutputPath));
            Assert.True(File.Exists(Path.Combine(toFolder.OutputPath, "DOMDocument.xml")));

            await using var rebuiltArchive = new MemoryStream();
            var toArchive = await TimelineDocumentFileService.ConvertFlashFolderToArchiveAsync(toFolder.OutputPath, rebuiltArchive, "roundtrip.fla");
            rebuiltArchive.Position = 0;

            using var zip = new System.IO.Compression.ZipArchive(rebuiltArchive, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            Assert.NotNull(zip.GetEntry("DOMDocument.xml"));
            Assert.Equal("roundtrip.fla", toArchive.OutputPath);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadsFlashArchive_PreservingSupplementalEntriesAndMediaPayloads()
    {
        var document = CreateInteropDocument();
        document.MediaAssets.Add(new MediaAsset
        {
            Name = "Sound FX",
            Kind = MediaAssetKind.Audio,
            SourceLabel = "sound-fx.wav",
            Duration = 0.25d,
            SampleRate = 44100,
            ChannelCount = 1,
            AudioBase64 = Convert.ToBase64String([0x52, 0x49, 0x46, 0x46, 0xAA, 0xBB])
        });

        await using var stream = new MemoryStream();
        await TimelineDocumentFileService.SaveAsync(stream, document, TimelineDocumentFileFormat.FlashXfl, "authoring.fla");
        stream.Position = 0;

        await using (var rewritten = new MemoryStream())
        {
            using (var sourceArchive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true))
            using (var targetArchive = new System.IO.Compression.ZipArchive(rewritten, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var sourceEntry in sourceArchive.Entries)
                {
                    var targetEntry = targetArchive.CreateEntry(sourceEntry.FullName);
                    await using var sourceEntryStream = sourceEntry.Open();
                    await using var targetEntryStream = targetEntry.Open();
                    await sourceEntryStream.CopyToAsync(targetEntryStream);
                }

                var customEntry = targetArchive.CreateEntry("META-INF/extensibility.xml");
                await using var customStream = customEntry.Open();
                await customStream.WriteAsync(Encoding.UTF8.GetBytes("<extra enabled=\"true\" />"));
            }

            rewritten.Position = 0;
            var loaded = await TimelineDocumentFileService.LoadAsync(rewritten, "authoring.fla", TimelineDocumentFileFormat.FlashXfl);
            var audio = Assert.Single(loaded.Document.MediaAssets);
            var supplemental = Assert.Single(
                loaded.Document.FlashAuthoringSupplementalFiles,
                file => string.Equals(file.RelativePath, "META-INF/extensibility.xml", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(MediaAssetKind.Audio, audio.Kind);
            Assert.False(string.IsNullOrWhiteSpace(audio.AudioBase64));
            Assert.Equal("META-INF/extensibility.xml", supplemental.RelativePath);
            Assert.Contains("enabled", Encoding.UTF8.GetString(supplemental.Data), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadsFlashArchive_UsesMediaManifestFallback_WhenDomMediaPathsDrift()
    {
        var document = CreateInteropDocument();
        document.MediaAssets.Add(new MediaAsset
        {
            Name = "Fallback Audio",
            Kind = MediaAssetKind.Audio,
            SourceLabel = "fallback-audio.wav",
            Duration = 0.5d,
            SampleRate = 44100,
            ChannelCount = 2,
            AudioBase64 = Convert.ToBase64String([0x52, 0x49, 0x46, 0x46, 0x11, 0x22])
        });
        document.MediaAssets.Add(new MediaAsset
        {
            Name = "Fallback Poster",
            Kind = MediaAssetKind.Video,
            SourceLabel = "fallback-poster.png",
            Duration = 1d,
            FrameRate = 24d,
            Width = 1,
            Height = 1,
            Frames =
            [
                new MediaFrameModel
                {
                    Duration = 1d / 24d,
                    ImageBase64 = OnePixelPngBase64,
                    Label = "F01"
                }
            ]
        });

        await using var stream = new MemoryStream();
        await TimelineDocumentFileService.SaveAsync(stream, document, TimelineDocumentFileFormat.FlashXfl, "media-fallback.fla");
        stream.Position = 0;

        await using var rewritten = new MemoryStream();
        using (var sourceArchive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true))
        using (var targetArchive = new System.IO.Compression.ZipArchive(rewritten, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var sourceEntry in sourceArchive.Entries)
            {
                var targetEntry = targetArchive.CreateEntry(sourceEntry.FullName);
                await using var targetEntryStream = targetEntry.Open();
                if (string.Equals(sourceEntry.FullName, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase))
                {
                    await using var sourceEntryStream = sourceEntry.Open();
                    using var reader = new StreamReader(sourceEntryStream);
                    var xml = await reader.ReadToEndAsync();
                    var documentXml = System.Xml.Linq.XDocument.Parse(xml);
                    var mediaElements = documentXml.Root?
                        .Elements()
                        .FirstOrDefault(element => element.Name.LocalName == "media")?
                        .Elements()
                        .ToList();
                    if (mediaElements is not null)
                    {
                        foreach (var mediaElement in mediaElements)
                        {
                            mediaElement.SetAttributeValue("relativePath", "BROKEN/missing.asset");
                            mediaElement.SetAttributeValue("frameFolder", "BROKEN/missing-folder");
                        }
                    }

                    await using var writer = new StreamWriter(targetEntryStream);
                    await writer.WriteAsync(documentXml.ToString());
                }
                else
                {
                    await using var sourceEntryStream = sourceEntry.Open();
                    await sourceEntryStream.CopyToAsync(targetEntryStream);
                }
            }
        }

        rewritten.Position = 0;
        var loaded = await TimelineDocumentFileService.LoadAsync(rewritten, "media-fallback.fla", TimelineDocumentFileFormat.FlashXfl);
        var fallbackAudio = Assert.Single(loaded.Document.MediaAssets, asset => asset.Name == "Fallback Audio");
        var fallbackPoster = Assert.Single(loaded.Document.MediaAssets, asset => asset.Name == "Fallback Poster");

        Assert.False(string.IsNullOrWhiteSpace(fallbackAudio.AudioBase64));
        Assert.True(fallbackPoster.Frames.Count >= 1);
        Assert.False(string.IsNullOrWhiteSpace(fallbackPoster.Frames[0].ImageBase64));
    }

    [Fact]
    public void MainWindowViewModel_LoadDocument_TracksCurrentDocumentFormat()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.LoadDocument(CreateInteropDocument(), "interop.svg", TimelineDocumentFileFormat.SvgSmil);

        Assert.Equal(TimelineDocumentFileFormat.SvgSmil, viewModel.CurrentDocumentFileFormat);
        Assert.Contains("SVG", viewModel.DocumentFileSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublishValidationService_FlagsInvalidActionScripts()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        document.Scenes[0].FrameLabels[0].Script = "goto_scene \"Missing Scene\"\nunknown_cmd";
        document.Scenes[0].Layers.First().Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Bad Script",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.Play,
            Script = "set_button \"Hero Card\" InvalidState"
        });

        var issues = PublishValidationService.Validate(document, document.PublishProfiles[0]);

        Assert.Contains(issues, issue => issue.Message.Contains("unknown action command", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("missing scene", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("invalid button state", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MainWindowViewModel_PrototypeBehaviorScript_ExecutesCommands()
    {
        var viewModel = new MainWindowViewModel();
        var triggerLayer = viewModel.Document.Layers.First(layer => layer.Behaviors.Count > 0);
        triggerLayer.Behaviors[0].Script = """
                                          set_var mode scripted
                                          trace "CTA fired"
                                          """;

        viewModel.TogglePrototypeModeCommand.Execute(null);
        viewModel.HandlePrototypeTrigger(triggerLayer.Id, InteractionTriggerKind.PointerClick);

        Assert.True(viewModel.IsPrototypeMode);
        Assert.Equal("Feature Focus", viewModel.SelectedScene?.Name);
        Assert.Contains("mode=scripted", viewModel.PrototypeVariableSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CTA fired", viewModel.PrototypeSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnimationExchangeService_AvaloniaXaml_ExportsTargetOnlyData_AndReimports()
    {
        var document = CreateInteropDocument();

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.AvaloniaXaml);
        Assert.DoesNotContain("TimelineAnimationsMetadata", export.Content, StringComparison.Ordinal);
        Assert.Contains("Selector=\"Border#", export.Content, StringComparison.Ordinal);
        Assert.Contains("Selector=\"Button#", export.Content, StringComparison.Ordinal);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.AvaloniaXaml, export.Content, "interop.axaml");

        Assert.Contains(imported.Document.Layers, layer => layer.Kind == LayerKind.AvaloniaControl && layer.Style.AvaloniaControl.Kind == AvaloniaControlKind.Button);
        Assert.DoesNotContain(imported.Issues, issue => string.Equals(issue.Source, "Legacy Metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_ExportsAndReimportsAuthoringData()
    {
        var document = CreateInteropDocument();
        document.Scenes[0].FrameLabels.Add(new FrameLabelModel { Frame = 6, Name = "flash_label", Script = "trace \"flash\"" });
        document.LibraryItems.Add(TimelineEditingService.CreateLibraryItemFromLayer(document.Layers[0], "Flash Symbol", SymbolKind.Graphic));

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "interop.xfl");

        Assert.Contains("<DOMDocument", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.True(imported.Document.Scenes.Count >= 1);
        Assert.True(imported.Document.LibraryItems.Count >= 1);
        Assert.Contains(imported.Document.Scenes[0].FrameLabels, label => label.Name == "flash_label");
        Assert.True(imported.Document.Layers.Count >= 2);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesExplicitTracksEffectsAndShapeTweens()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];
        var rectangle = scene.Layers[0];
        var xTrack = rectangle.Tracks.First(track => track.Property == AnimatedProperty.X);
        xTrack.Keyframes[1].Easing = EasingKind.Custom;
        xTrack.Keyframes[1].ControlPoint1X = 0.18d;
        xTrack.Keyframes[1].ControlPoint1Y = 0.82d;
        xTrack.Keyframes[1].ControlPoint2X = 0.7d;
        xTrack.Keyframes[1].ControlPoint2Y = 0.22d;
        rectangle.Compositing.BlendMode = LayerBlendMode.Screen;
        rectangle.Compositing.BlurRadius = 14d;
        rectangle.Compositing.GlowOpacity = 0.45d;
        rectangle.Compositing.GlowColor = "#FFAA55";
        rectangle.Compositing.TintStrength = 0.35d;
        rectangle.Compositing.TintColor = "#FFDD66";
        rectangle.Compositing.Saturation = 1.4d;
        rectangle.Compositing.ParallaxDepth = 1.75d;

        var pathLayer = TimelineEditingService.CreatePathLayer(
            "Morph Path",
            "#4FE1FF",
            "#FFFFFF",
            [new VectorPointModel { X = 0d, Y = 1d }, new VectorPointModel { X = 1d, Y = 0d }],
            false,
            scene.Layers.Count,
            3d);
        TimelineEditingService.SetShapeKeyframe(
            pathLayer,
            1d,
            [new VectorPointModel { X = 0d, Y = 0d }, new VectorPointModel { X = 1d, Y = 1d }, new VectorPointModel { X = 0.5d, Y = 0.2d }],
            false,
            scene.Duration);
        scene.Layers.Add(pathLayer);
        document.Layers = scene.Layers;

        var imported = AnimationExchangeService.Import(
            AnimationExchangeFormat.FlashXfl,
            AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl).Content,
            "rich.xfl");

        var importedRectangle = imported.Document.Layers.First(layer => layer.Name == rectangle.Name);
        var importedTrack = importedRectangle.Tracks.First(track => track.Property == AnimatedProperty.X);
        var importedKeyframe = importedTrack.Keyframes.First(keyframe => Math.Abs(keyframe.Time - 2d) < 0.0001d);
        var importedPath = imported.Document.Layers.First(layer => layer.Name == "Morph Path");

        Assert.Equal(EasingKind.Custom, importedKeyframe.Easing);
        Assert.Equal(0.18d, importedKeyframe.ControlPoint1X, 3);
        Assert.Equal(0.82d, importedKeyframe.ControlPoint1Y, 3);
        Assert.Equal(LayerBlendMode.Screen, importedRectangle.Compositing.BlendMode);
        Assert.Equal(14d, importedRectangle.Compositing.BlurRadius, 3);
        Assert.Equal(0.45d, importedRectangle.Compositing.GlowOpacity, 3);
        Assert.Equal("#FFAA55", importedRectangle.Compositing.GlowColor);
        Assert.Equal(0.35d, importedRectangle.Compositing.TintStrength, 3);
        Assert.Equal(1.75d, importedRectangle.Compositing.ParallaxDepth, 3);
        Assert.NotEmpty(importedPath.ShapeKeyframes);
        Assert.Equal(3, importedPath.ShapeKeyframes[0].PathPoints.Count);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesMediaSyncSettings()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];
        var media = new MediaAsset
        {
            Name = "FeatureClip",
            Kind = MediaAssetKind.Video,
            Duration = 3d,
            FrameRate = 24d,
            Width = 640,
            Height = 360,
            SourceLabel = "feature_clip"
        };
        document.MediaAssets.Add(media);
        var videoLayer = TimelineEditingService.CreateVideoLayer(media, 120d, 88d, scene.Layers.Count, 0.5d);
        videoLayer.Name = "FeatureClip";
        videoLayer.Media.PlaybackMode = MediaPlaybackMode.Event;
        videoLayer.Media.ClipOffset = 0.25d;
        videoLayer.Media.ClipDuration = 1.75d;
        videoLayer.Media.Loop = true;
        videoLayer.Media.Volume = 1.35d;
        videoLayer.Media.GainDb = 4.5d;
        videoLayer.Media.Pan = -0.2d;
        videoLayer.Media.FadeInDuration = 0.15d;
        videoLayer.Media.FadeOutDuration = 0.35d;
        scene.Layers.Add(videoLayer);
        document.Layers = scene.Layers;

        var imported = AnimationExchangeService.Import(
            AnimationExchangeFormat.FlashXfl,
            AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl).Content,
            "media.xfl");

        var importedVideo = imported.Document.Layers.First(layer => layer.Name == "FeatureClip");

        Assert.Equal(MediaPlaybackMode.Event, importedVideo.Media.PlaybackMode);
        Assert.Equal(0.25d, importedVideo.Media.ClipOffset, 3);
        Assert.Equal(1.75d, importedVideo.Media.ClipDuration, 3);
        Assert.True(importedVideo.Media.Loop);
        Assert.Equal(1.35d, importedVideo.Media.Volume, 3);
        Assert.Equal(4.5d, importedVideo.Media.GainDb, 3);
        Assert.Equal(-0.2d, importedVideo.Media.Pan, 3);
        Assert.Equal(0.15d, importedVideo.Media.FadeInDuration, 3);
        Assert.Equal(0.35d, importedVideo.Media.FadeOutDuration, 3);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesLibraryMetadataInstanceNamesAndTextSettings()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];
        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(scene.Layers[0], "Flash Shared Symbol", SymbolKind.MovieClip);
        symbol.LinkageId = "flash.shared.symbol";
        symbol.BaseClassName = "Project.SharedSymbol";
        symbol.ExportForRuntimeSharing = true;
        symbol.ImportForRuntimeSharing = true;
        symbol.ExportInFirstFrame = false;
        symbol.SharedLibraryPath = "libs/shared_symbols.fla";
        symbol.UpdateAutomatically = true;
        symbol.UseScale9Grid = true;
        symbol.Scale9Left = 18d;
        symbol.Scale9Top = 12d;
        symbol.Scale9Right = 164d;
        symbol.Scale9Bottom = 96d;
        TimelineEditingService.AddLibraryItem(document, symbol);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(symbol, 240d, 120d, scene.Layers.Count);
        instance.Name = "Shared Instance";
        instance.InstanceName = "heroCardInstance";
        scene.Layers.Add(instance);

        var caption = TimelineEditingService.CreateLayer(LayerKind.Text, "Flash Caption", "#F7F5ED", "HELLO\nFLASH", 88d, 312d, scene.Layers.Count);
        caption.Style.FontSize = 38d;
        caption.Style.TextSettings.FontFamily = "Arial";
        caption.Style.TextSettings.IsBold = true;
        caption.Style.TextSettings.IsItalic = true;
        caption.Style.TextSettings.Alignment = LayerTextAlignment.Center;
        caption.Style.TextSettings.LetterSpacing = 1.5d;
        caption.Style.TextSettings.LineHeight = 42d;
        caption.Style.TextSettings.RenderAsHtml = true;
        scene.Layers.Add(caption);
        document.Layers = scene.Layers;

        var imported = AnimationExchangeService.Import(
            AnimationExchangeFormat.FlashXfl,
            AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl).Content,
            "authoring.xfl");

        var importedSymbol = imported.Document.LibraryItems.First(item => item.Name == "Flash Shared Symbol");
        var importedInstance = imported.Document.Layers.First(layer => layer.Name == "Shared Instance");
        var importedCaption = imported.Document.Layers.First(layer => layer.Name == "Flash Caption");

        Assert.Equal("Project.SharedSymbol", importedSymbol.BaseClassName);
        Assert.True(importedSymbol.ExportForRuntimeSharing);
        Assert.True(importedSymbol.ImportForRuntimeSharing);
        Assert.False(importedSymbol.ExportInFirstFrame);
        Assert.Equal("libs/shared_symbols.fla", importedSymbol.SharedLibraryPath);
        Assert.True(importedSymbol.UpdateAutomatically);
        Assert.True(importedSymbol.UseScale9Grid);
        Assert.Equal(18d, importedSymbol.Scale9Left, 3);
        Assert.Equal(96d, importedSymbol.Scale9Bottom, 3);
        Assert.Equal("heroCardInstance", importedInstance.InstanceName);
        Assert.Equal("Arial", importedCaption.Style.TextSettings.FontFamily);
        Assert.True(importedCaption.Style.TextSettings.IsBold);
        Assert.True(importedCaption.Style.TextSettings.IsItalic);
        Assert.Equal(LayerTextAlignment.Center, importedCaption.Style.TextSettings.Alignment);
        Assert.Equal(1.5d, importedCaption.Style.TextSettings.LetterSpacing, 3);
        Assert.Equal(42d, importedCaption.Style.TextSettings.LineHeight, 3);
        Assert.True(importedCaption.Style.TextSettings.RenderAsHtml);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesGuideBindingsRegistrationPointsAndFieldModes()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];

        var guide = TimelineEditingService.CreatePathLayer(
            "Motion Guide",
            "#1E3352",
            "#8EEAFF",
            [new VectorPointModel { X = 0d, Y = 0.25d }, new VectorPointModel { X = 0.55d, Y = 0.1d }, new VectorPointModel { X = 1d, Y = 0.8d }],
            false,
            scene.Layers.Count,
            3d);
        guide.Compositing.Role = LayerCompositeRole.Guide;
        scene.Layers.Insert(0, guide);

        var guidedLayer = scene.Layers.First(layer => layer.Name == "CTA");
        guidedLayer.GuidedByLayerId = guide.Id;
        guidedLayer.OrientToGuidePath = true;
        guidedLayer.SnapToGuidePath = true;

        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(scene.Layers[1], "Registered Symbol", SymbolKind.Graphic);
        symbol.RegistrationPointX = 0.5d;
        symbol.RegistrationPointY = 0.75d;
        TimelineEditingService.AddLibraryItem(document, symbol);

        var field = TimelineEditingService.CreateLayer(LayerKind.Text, "Flash Field", "#F7F5ED", "name\nvalue", 84d, 300d, scene.Layers.Count);
        field.Style.TextSettings.FieldKind = FlashTextFieldKind.Input;
        field.Style.TextSettings.LineMode = FlashTextLineMode.MultilineNoWrap;
        field.Style.TextSettings.IsSelectable = false;
        field.Style.TextSettings.ShowBorder = true;
        field.Style.TextSettings.VariableName = "statusField";
        scene.Layers.Add(field);
        document.Layers = scene.Layers;

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "authoring.xfl");

        var importedGuide = imported.Document.Layers.First(layer => layer.Name == "Motion Guide");
        var importedGuidedLayer = imported.Document.Layers.First(layer => layer.Name == "CTA");
        var importedSymbol = imported.Document.LibraryItems.First(item => item.Name == "Registered Symbol");
        var importedField = imported.Document.Layers.First(layer => layer.Name == "Flash Field");

        Assert.Contains("DOMInputText", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(importedGuide.Id, importedGuidedLayer.GuidedByLayerId);
        Assert.True(importedGuidedLayer.OrientToGuidePath);
        Assert.True(importedGuidedLayer.SnapToGuidePath);
        Assert.Equal(0.5d, importedSymbol.RegistrationPointX, 3);
        Assert.Equal(0.75d, importedSymbol.RegistrationPointY, 3);
        Assert.Equal(FlashTextFieldKind.Input, importedField.Style.TextSettings.FieldKind);
        Assert.Equal(FlashTextLineMode.MultilineNoWrap, importedField.Style.TextSettings.LineMode);
        Assert.False(importedField.Style.TextSettings.IsSelectable);
        Assert.True(importedField.Style.TextSettings.ShowBorder);
        Assert.Equal("statusField", importedField.Style.TextSettings.VariableName);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesComponentParametersAndOverrides()
    {
        var document = CreateComponentInteropDocument();
        document.Layers[0].ComponentParameterOverrides[0].Value = "Watch now";

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "component.xfl");

        var component = Assert.Single(imported.Document.LibraryItems);
        var definition = Assert.Single(component.ComponentParameters);
        var instance = Assert.Single(imported.Document.Layers);
        var overrideValue = Assert.Single(instance.ComponentParameterOverrides);

        Assert.True(component.IsComponent);
        Assert.Equal("buttonLabel", definition.Name);
        Assert.Equal(ComponentParameterKind.String, definition.Kind);
        Assert.Equal(ComponentParameterBindingTarget.Content, definition.BindingTarget);
        Assert.Equal("Launch", definition.DefaultValue);
        Assert.Equal("buttonLabel", overrideValue.Name);
        Assert.Equal("Watch now", overrideValue.Value);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesScaleSkewDefaultsAndTracks()
    {
        var document = CreateInteropDocument();
        var rectangle = document.Scenes[0].Layers.First(layer => layer.Name == "Card");
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.ScaleX, 1.2d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.ScaleY, 0.85d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.SkewX, 12d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.SkewY, -6d);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.ScaleX, 2d, 1.45d, document.Duration);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.ScaleY, 2d, 0.92d, document.Duration);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.SkewX, 2d, 18d, document.Duration);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.SkewY, 2d, -11d, document.Duration);

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "transform.xfl");
        var importedRectangle = imported.Document.Layers.First(layer => layer.Name == "Card");

        Assert.Contains("scaleX=", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("skewX=", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1.2d, importedRectangle.Defaults.ScaleX, 3);
        Assert.Equal(0.85d, importedRectangle.Defaults.ScaleY, 3);
        Assert.Equal(12d, importedRectangle.Defaults.SkewX, 3);
        Assert.Equal(-6d, importedRectangle.Defaults.SkewY, 3);
        Assert.Equal(1.45d, importedRectangle.Tracks.First(track => track.Property == AnimatedProperty.ScaleX).Keyframes.Last().Value, 3);
        Assert.Equal(0.92d, importedRectangle.Tracks.First(track => track.Property == AnimatedProperty.ScaleY).Keyframes.Last().Value, 3);
        Assert.Equal(18d, importedRectangle.Tracks.First(track => track.Property == AnimatedProperty.SkewX).Keyframes.Last().Value, 3);
        Assert.Equal(-11d, importedRectangle.Tracks.First(track => track.Property == AnimatedProperty.SkewY).Keyframes.Last().Value, 3);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesOutlineCacheAndAdvancedTextMetadata()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];

        var outlinedLayer = scene.Layers.First(layer => layer.Name == "Card");
        outlinedLayer.ShowAsOutline = true;
        outlinedLayer.OutlineColor = "#FFD166";

        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(outlinedLayer, "Bitmap Cached Symbol", SymbolKind.MovieClip);
        TimelineEditingService.AddLibraryItem(document, symbol);
        var instance = TimelineEditingService.CreateLayerFromLibraryItem(symbol, 420d, 180d, scene.Layers.Count);
        instance.Name = "Cached Instance";
        instance.CacheAsBitmap = true;
        instance.BitmapCacheBackgroundColor = "#112233";
        scene.Layers.Add(instance);

        var field = TimelineEditingService.CreateLayer(LayerKind.Text, "Advanced Field", "#F7F5ED", "secret-code", 90d, 280d, scene.Layers.Count);
        field.Style.TextSettings.FieldKind = FlashTextFieldKind.Input;
        field.Style.TextSettings.MaxCharacters = 6;
        field.Style.TextSettings.IsPassword = true;
        field.Style.TextSettings.UseDeviceFonts = true;
        field.Style.TextSettings.AntiAliasMode = FlashTextAntiAliasMode.Readability;
        scene.Layers.Add(field);
        document.Layers = scene.Layers;

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "advanced.xfl");

        var importedOutlined = imported.Document.Layers.First(layer => layer.Name == "Card");
        var importedInstance = imported.Document.Layers.First(layer => layer.Name == "Cached Instance");
        var importedField = imported.Document.Layers.First(layer => layer.Name == "Advanced Field");

        Assert.Contains("outlineColor", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cacheAsBitmap", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("antiAliasMode", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.True(importedOutlined.ShowAsOutline);
        Assert.Equal("#FFD166", importedOutlined.OutlineColor);
        Assert.True(importedInstance.CacheAsBitmap);
        Assert.Equal("#112233", importedInstance.BitmapCacheBackgroundColor);
        Assert.Equal(6, importedField.Style.TextSettings.MaxCharacters);
        Assert.True(importedField.Style.TextSettings.IsPassword);
        Assert.True(importedField.Style.TextSettings.UseDeviceFonts);
        Assert.Equal(FlashTextAntiAliasMode.Readability, importedField.Style.TextSettings.AntiAliasMode);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesNativeColorEffectsAndFilters()
    {
        var document = CreateInteropDocument();
        var rectangle = document.Scenes[0].Layers.First(layer => layer.Name == "Card");
        rectangle.Compositing.FlashColorEffectMode = FlashColorEffectMode.Advanced;
        rectangle.Compositing.FlashAdvancedAlphaPercent = 75d;
        rectangle.Compositing.FlashRedPercent = 120d;
        rectangle.Compositing.FlashGreenPercent = 80d;
        rectangle.Compositing.FlashBluePercent = 140d;
        rectangle.Compositing.FlashAlphaOffset = 16d;
        rectangle.Compositing.FlashRedOffset = 12d;
        rectangle.Compositing.FlashGreenOffset = -8d;
        rectangle.Compositing.FlashBlueOffset = 4d;
        rectangle.Compositing.BlurRadius = 10d;
        rectangle.Compositing.BlurQuality = LayerFilterQuality.High;
        rectangle.Compositing.GlowColor = "#FFD166";
        rectangle.Compositing.GlowSize = 22d;
        rectangle.Compositing.GlowOpacity = 0.55d;
        rectangle.Compositing.GlowQuality = LayerFilterQuality.Medium;
        rectangle.Compositing.GlowKnockout = true;
        rectangle.Compositing.ShadowColor = "#09111F";
        rectangle.Compositing.ShadowBlur = 18d;
        rectangle.Compositing.ShadowOpacity = 0.4d;
        rectangle.Compositing.ShadowOffsetX = 12d;
        rectangle.Compositing.ShadowOffsetY = 6d;
        rectangle.Compositing.ShadowQuality = LayerFilterQuality.Medium;
        rectangle.Compositing.ShadowKnockout = true;
        rectangle.Compositing.ShadowHideObject = true;
        rectangle.Compositing.Contrast = 0.22d;
        rectangle.Compositing.HueShift = 32d;
        rectangle.Compositing.BevelType = LayerBevelType.Outer;
        rectangle.Compositing.BevelSize = 12d;
        rectangle.Compositing.BevelStrength = 0.85d;
        rectangle.Compositing.BevelAngle = 62d;
        rectangle.Compositing.BevelDistance = 7d;
        rectangle.Compositing.BevelHighlightColor = "#F7F5ED";
        rectangle.Compositing.BevelShadowColor = "#0A1324";
        rectangle.Compositing.BevelQuality = LayerFilterQuality.High;
        rectangle.Compositing.BevelKnockout = true;
        rectangle.Compositing.GradientGlowSize = 16d;
        rectangle.Compositing.GradientGlowStrength = 0.72d;
        rectangle.Compositing.GradientGlowAngle = 108d;
        rectangle.Compositing.GradientGlowDistance = 9d;
        rectangle.Compositing.GradientGlowFrom = "#61E6FF";
        rectangle.Compositing.GradientGlowTo = "#09111F";
        rectangle.Compositing.GradientGlowQuality = LayerFilterQuality.Medium;
        rectangle.Compositing.GradientGlowKnockout = true;
        rectangle.Compositing.GradientBevelType = LayerBevelType.Inner;
        rectangle.Compositing.GradientBevelSize = 10d;
        rectangle.Compositing.GradientBevelStrength = 0.65d;
        rectangle.Compositing.GradientBevelAngle = 124d;
        rectangle.Compositing.GradientBevelDistance = 5d;
        rectangle.Compositing.GradientBevelFrom = "#FFFFFF";
        rectangle.Compositing.GradientBevelTo = "#24E5C1";
        rectangle.Compositing.GradientBevelQuality = LayerFilterQuality.High;
        rectangle.Compositing.GradientBevelKnockout = true;

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "visual-effects.xfl");
        var importedRectangle = imported.Document.Layers.First(layer => layer.Name == "Card");

        Assert.Contains("colorMode=\"advanced\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redMultiplier", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alphaOffset", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BlurFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GlowFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DropShadowFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BevelFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GradientGlowFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GradientBevelFilter", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quality=\"3\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("knockout=\"true\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hideObject=\"true\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FlashColorEffectMode.Advanced, importedRectangle.Compositing.FlashColorEffectMode);
        Assert.Equal(75d, importedRectangle.Compositing.FlashAdvancedAlphaPercent, 3);
        Assert.Equal(120d, importedRectangle.Compositing.FlashRedPercent, 3);
        Assert.Equal(80d, importedRectangle.Compositing.FlashGreenPercent, 3);
        Assert.Equal(140d, importedRectangle.Compositing.FlashBluePercent, 3);
        Assert.Equal(16d, importedRectangle.Compositing.FlashAlphaOffset, 3);
        Assert.Equal(12d, importedRectangle.Compositing.FlashRedOffset, 3);
        Assert.Equal(-8d, importedRectangle.Compositing.FlashGreenOffset, 3);
        Assert.Equal(4d, importedRectangle.Compositing.FlashBlueOffset, 3);
        Assert.Equal(10d, importedRectangle.Compositing.BlurRadius, 3);
        Assert.Equal(LayerFilterQuality.High, importedRectangle.Compositing.BlurQuality);
        Assert.Equal("#FFD166", importedRectangle.Compositing.GlowColor);
        Assert.Equal(22d, importedRectangle.Compositing.GlowSize, 3);
        Assert.Equal(0.55d, importedRectangle.Compositing.GlowOpacity, 3);
        Assert.Equal(LayerFilterQuality.Medium, importedRectangle.Compositing.GlowQuality);
        Assert.True(importedRectangle.Compositing.GlowKnockout);
        Assert.Equal("#09111F", importedRectangle.Compositing.ShadowColor);
        Assert.Equal(18d, importedRectangle.Compositing.ShadowBlur, 3);
        Assert.Equal(0.4d, importedRectangle.Compositing.ShadowOpacity, 3);
        Assert.Equal(12d, importedRectangle.Compositing.ShadowOffsetX, 3);
        Assert.Equal(6d, importedRectangle.Compositing.ShadowOffsetY, 3);
        Assert.Equal(LayerFilterQuality.Medium, importedRectangle.Compositing.ShadowQuality);
        Assert.True(importedRectangle.Compositing.ShadowKnockout);
        Assert.True(importedRectangle.Compositing.ShadowHideObject);
        Assert.Equal(0.22d, importedRectangle.Compositing.Contrast, 3);
        Assert.Equal(32d, importedRectangle.Compositing.HueShift, 3);
        Assert.Equal(LayerBevelType.Outer, importedRectangle.Compositing.BevelType);
        Assert.Equal(12d, importedRectangle.Compositing.BevelSize, 3);
        Assert.Equal(0.85d, importedRectangle.Compositing.BevelStrength, 3);
        Assert.Equal(62d, importedRectangle.Compositing.BevelAngle, 3);
        Assert.Equal(7d, importedRectangle.Compositing.BevelDistance, 3);
        Assert.Equal("#F7F5ED", importedRectangle.Compositing.BevelHighlightColor);
        Assert.Equal("#0A1324", importedRectangle.Compositing.BevelShadowColor);
        Assert.Equal(LayerFilterQuality.High, importedRectangle.Compositing.BevelQuality);
        Assert.True(importedRectangle.Compositing.BevelKnockout);
        Assert.Equal(16d, importedRectangle.Compositing.GradientGlowSize, 3);
        Assert.Equal(0.72d, importedRectangle.Compositing.GradientGlowStrength, 3);
        Assert.Equal(108d, importedRectangle.Compositing.GradientGlowAngle, 3);
        Assert.Equal(9d, importedRectangle.Compositing.GradientGlowDistance, 3);
        Assert.Equal("#61E6FF", importedRectangle.Compositing.GradientGlowFrom);
        Assert.Equal("#09111F", importedRectangle.Compositing.GradientGlowTo);
        Assert.Equal(LayerFilterQuality.Medium, importedRectangle.Compositing.GradientGlowQuality);
        Assert.True(importedRectangle.Compositing.GradientGlowKnockout);
        Assert.Equal(LayerBevelType.Inner, importedRectangle.Compositing.GradientBevelType);
        Assert.Equal(10d, importedRectangle.Compositing.GradientBevelSize, 3);
        Assert.Equal(0.65d, importedRectangle.Compositing.GradientBevelStrength, 3);
        Assert.Equal(124d, importedRectangle.Compositing.GradientBevelAngle, 3);
        Assert.Equal(5d, importedRectangle.Compositing.GradientBevelDistance, 3);
        Assert.Equal("#FFFFFF", importedRectangle.Compositing.GradientBevelFrom);
        Assert.Equal("#24E5C1", importedRectangle.Compositing.GradientBevelTo);
        Assert.Equal(LayerFilterQuality.High, importedRectangle.Compositing.GradientBevelQuality);
        Assert.True(importedRectangle.Compositing.GradientBevelKnockout);
    }

    [Fact]
    public void ColorHelpers_ApplyFlashColorEffect_UsesAdvancedMultipliersAndOffsets()
    {
        var compositing = new LayerCompositeSettings
        {
            FlashColorEffectMode = FlashColorEffectMode.Advanced,
            FlashAdvancedAlphaPercent = 50d,
            FlashRedPercent = 120d,
            FlashGreenPercent = 50d,
            FlashBluePercent = 25d,
            FlashAlphaOffset = -30d,
            FlashRedOffset = 10d,
            FlashGreenOffset = -20d,
            FlashBlueOffset = 5d
        };

        var adjusted = ColorHelpers.ApplyFlashColorEffect(
            Avalonia.Media.Color.FromArgb(200, 100, 120, 140),
            compositing);

        Assert.Equal((byte)70, adjusted.A);
        Assert.Equal((byte)130, adjusted.R);
        Assert.Equal((byte)40, adjusted.G);
        Assert.Equal((byte)40, adjusted.B);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesShapeStyleMetadata()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];

        var styledPath = TimelineEditingService.CreatePathLayer(
            "Styled Path",
            "#24E5C1",
            "#FFD166",
            [new VectorPointModel { X = 0d, Y = 0.9d }, new VectorPointModel { X = 0.5d, Y = 0.1d }, new VectorPointModel { X = 1d, Y = 0.8d }],
            false,
            scene.Layers.Count,
            8d,
            true,
            "#24E5C1",
            "#09111F",
            LayerGradientKind.Radial,
            135d,
            true,
            true,
            LayerStrokeCapStyle.Square,
            LayerStrokeJoinStyle.Bevel,
            9d);
        scene.Layers.Add(styledPath);

        var card = scene.Layers.First(layer => layer.Name == "Card");
        card.Style.HasFill = false;
        card.Style.DrawingMode = ShapeDrawingMode.Primitive;
        card.Style.PrimitiveShape = PrimitiveShapeType.Rectangle;
        card.Style.CornerRadiusTopLeft = 36d;
        card.Style.CornerRadiusTopRight = 18d;
        card.Style.CornerRadiusBottomRight = 42d;
        card.Style.CornerRadiusBottomLeft = 12d;

        var orb = TimelineEditingService.CreateLayer(LayerKind.Ellipse, "Strokeless Orb", "#FFD166", string.Empty, 480d, 220d, scene.Layers.Count);
        orb.Style.HasStroke = false;
        orb.Style.DrawingMode = ShapeDrawingMode.Primitive;
        orb.Style.PrimitiveShape = PrimitiveShapeType.Ellipse;
        orb.Style.EllipseStartAngle = 24d;
        orb.Style.EllipseSweepAngle = 220d;
        scene.Layers.Add(orb);

        var polyStar = TimelineEditingService.CreatePathLayer(
            "Signal Star",
            "#24E5C1",
            "#FFFFFF",
            PrimitiveShapeService.CreatePolyStarPoints(7, 0.38d, true),
            true,
            scene.Layers.Count,
            4d);
        polyStar.Style.DrawingMode = ShapeDrawingMode.Primitive;
        polyStar.Style.PrimitiveShape = PrimitiveShapeType.PolyStar;
        polyStar.Style.PolyStarSides = 7;
        polyStar.Style.PolyStarInnerRadius = 0.38d;
        polyStar.Style.PolyStarIsStar = true;
        scene.Layers.Add(polyStar);

        document.Layers = scene.Layers;

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "shape-style.xfl");
        var importedPath = imported.Document.Layers.First(layer => layer.Name == "Styled Path");
        var importedCard = imported.Document.Layers.First(layer => layer.Name == "Card");
        var importedOrb = imported.Document.Layers.First(layer => layer.Name == "Strokeless Orb");
        var importedPolyStar = imported.Document.Layers.First(layer => layer.Name == "Signal Star");

        Assert.Contains("gradientKind=\"radial\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gradientAngle=\"135", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("strokeCap=\"square\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("strokeJoin=\"bevel\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hasFill=\"false\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hasStroke=\"false\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("drawingMode=\"Primitive\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("primitiveShape=\"PolyStar\"", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ellipseSweepAngle=\"220", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LayerGradientKind.Radial, importedPath.Style.GradientKind);
        Assert.Equal(135d, importedPath.Style.GradientAngle, 3);
        Assert.Equal(LayerStrokeCapStyle.Square, importedPath.Style.StrokeCapStyle);
        Assert.Equal(LayerStrokeJoinStyle.Bevel, importedPath.Style.StrokeJoinStyle);
        Assert.Equal(9d, importedPath.Style.StrokeMiterLimit, 3);
        Assert.True(importedPath.Style.HasFill);
        Assert.True(importedPath.Style.HasStroke);
        Assert.False(importedCard.Style.HasFill);
        Assert.False(importedOrb.Style.HasStroke);
        Assert.Equal(ShapeDrawingMode.Primitive, importedCard.Style.DrawingMode);
        Assert.Equal(36d, importedCard.Style.CornerRadiusTopLeft, 3);
        Assert.Equal(18d, importedCard.Style.CornerRadiusTopRight, 3);
        Assert.Equal(42d, importedCard.Style.CornerRadiusBottomRight, 3);
        Assert.Equal(12d, importedCard.Style.CornerRadiusBottomLeft, 3);
        Assert.Equal(PrimitiveShapeType.Ellipse, importedOrb.Style.PrimitiveShape);
        Assert.Equal(24d, importedOrb.Style.EllipseStartAngle, 3);
        Assert.Equal(220d, importedOrb.Style.EllipseSweepAngle, 3);
        Assert.Equal(PrimitiveShapeType.PolyStar, importedPolyStar.Style.PrimitiveShape);
        Assert.Equal(7, importedPolyStar.Style.PolyStarSides);
        Assert.Equal(0.38d, importedPolyStar.Style.PolyStarInnerRadius, 3);
        Assert.True(importedPolyStar.Style.PolyStarIsStar);
    }

    [Fact]
    public void LayerParentingService_ComposesChildWorldSnapshot()
    {
        var parent = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Parent", "#24E5C1", string.Empty, 100d, 100d, 0);
        var child = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Child", "#FFFFFF", string.Empty, 160d, 170d, 1);
        var childSnapshot = TimelineInterpolationService.SampleLayer(child, 0d);
        var parentSnapshot = TimelineInterpolationService.SampleLayer(parent, 0d);
        LayerParentingService.BindToParent(child, parent, childSnapshot, parentSnapshot);
        TimelineEditingService.UpdateDefaultValue(parent, AnimatedProperty.X, 220d);
        TimelineEditingService.UpdateDefaultValue(parent, AnimatedProperty.Y, 240d);

        var totalFrames = FrameTimelineService.GetTotalFrames(2d, 24d);
        var worldSnapshot = LayerParentingService.SampleWorldSnapshot([parent, child], child, 0d, 24d, totalFrames);

        Assert.NotNull(worldSnapshot);
        Assert.Equal(parent.Id, child.TransformParenting.ParentLayerId);
        Assert.Equal(280d, worldSnapshot!.Value.X, 3);
        Assert.Equal(310d, worldSnapshot.Value.Y, 3);
    }

    [Fact]
    public void LayerDeformationService_WarpAndRig_UpdatesHandleStateAndProducesDeformedGeometry()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Deform Path",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0.08d, Y = 0.08d },
                new VectorPointModel { X = 0.32d, Y = 0.28d },
                new VectorPointModel { X = 0.5d, Y = 0.5d },
                new VectorPointModel { X = 0.76d, Y = 0.72d },
                new VectorPointModel { X = 0.92d, Y = 0.92d }
            ],
            false,
            0,
            4d);
        var snapshot = TimelineInterpolationService.SampleLayer(layer, 0d);
        var originalPoints = VectorPathService.GetAbsolutePoints(snapshot).Select(point => (point.X, point.Y)).ToArray();

        LayerDeformationService.EnsureDefaultWarpPins(layer);
        LayerDeformationService.EnsureDefaultRig(layer);
        var originalRotation = layer.Rig.Bones[0].Rotation;
        var originalLength = layer.Rig.Bones[0].Length;
        Assert.True(LayerDeformationService.MoveWarpPin(layer, layer.Warp.Pins[0].Id, 0.18d, 0.24d));
        Assert.True(LayerDeformationService.MoveRigBoneHandle(layer, layer.Rig.Bones[0].Id, RigBoneHandleKind.End, 0.94d, 0.82d));

        var resolvedPins = LayerDeformationService.ResolveWarpPins(snapshot, layer.Warp);
        var movedPin = resolvedPins.First(pin => pin.Pin.Id == layer.Warp.Pins[0].Id);
        var resolvedBones = LayerDeformationService.ResolveRigBones(snapshot, layer.Rig, 0d);
        var deformedPoints = LayerDeformationService.GetDeformedPathPoints(snapshot, layer, 0d);

        Assert.NotEqual(movedPin.BindPoint, movedPin.CurrentPoint);
        Assert.NotEqual(originalRotation, layer.Rig.Bones[0].Rotation);
        Assert.NotEqual(originalLength, layer.Rig.Bones[0].Length);
        Assert.NotEmpty(resolvedBones);
        Assert.Equal(originalPoints.Length, deformedPoints.Count);
    }

    [Fact]
    public void AnimationExchangeService_FlashXfl_PreservesParentingWarpAndRigMetadata()
    {
        var document = CreateInteropDocument();
        var parent = document.Scenes[0].Layers.First(layer => layer.Name == "Card");
        var child = document.Scenes[0].Layers.First(layer => layer.Name == "CTA");
        var childSnapshot = TimelineInterpolationService.SampleLayer(child, 0d);
        var parentSnapshot = TimelineInterpolationService.SampleLayer(parent, 0d);
        LayerParentingService.BindToParent(child, parent, childSnapshot, parentSnapshot);

        var path = TimelineEditingService.CreatePathLayer(
            "Deform Path",
            "#24E5C1",
            "#FFFFFF",
            [new VectorPointModel { X = 0d, Y = 0.75d }, new VectorPointModel { X = 0.35d, Y = 0.1d }, new VectorPointModel { X = 1d, Y = 0.82d }],
            false,
            document.Scenes[0].Layers.Count,
            5d);
        LayerDeformationService.EnsureDefaultWarpPins(path);
        LayerDeformationService.EnsureDefaultRig(path);
        LayerDeformationService.CapturePose(path, "Reach", 0.5d);
        document.Scenes[0].Layers.Add(path);
        document.Layers = document.Scenes[0].Layers;

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.FlashXfl);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.FlashXfl, export.Content, "deform-authoring.xfl");
        var importedParent = imported.Document.Layers.First(layer => layer.Name == "Card");
        var importedChild = imported.Document.Layers.First(layer => layer.Name == "CTA");
        var importedPath = imported.Document.Layers.First(layer => layer.Name == "Deform Path");

        Assert.Contains("transformParentLayerId", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<warp", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<rig", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(importedParent.Id, importedChild.TransformParenting.ParentLayerId);
        Assert.True(importedPath.Warp.IsEnabled);
        Assert.Equal(path.Warp.MeshColumns, importedPath.Warp.MeshColumns);
        Assert.Equal(path.Warp.MeshRows, importedPath.Warp.MeshRows);
        Assert.Equal(path.Warp.Pins.Count, importedPath.Warp.Pins.Count);
        Assert.True(importedPath.Rig.IsEnabled);
        Assert.Equal(path.Rig.Bones.Count, importedPath.Rig.Bones.Count);
        Assert.Equal(path.Rig.Poses.Count, importedPath.Rig.Poses.Count);
    }

    [Fact]
    public void LayerStyleRenderHelper_CreatesExpectedBrushesAndPens()
    {
        var snapshot = new LayerSnapshot(
            0d,
            0d,
            100d,
            100d,
            1d,
            1d,
            0d,
            0d,
            0d,
            1d,
            0d,
            0d,
            0d,
            0d,
            0d,
            "#24E5C1",
            "#FFFFFF",
            string.Empty,
            24d,
            6d,
            true,
            "#24E5C1",
            "#09111F",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            false,
            0d,
            360d,
            false,
            "#57C9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings(),
            new LayerTextSettings())
        {
            HasFill = true,
            HasStroke = true,
            GradientKind = LayerGradientKind.Radial,
            GradientAngle = 120d,
            StrokeCapStyle = LayerStrokeCapStyle.Square,
            StrokeJoinStyle = LayerStrokeJoinStyle.Miter,
            StrokeMiterLimit = 7d
        };

        var fillBrush = LayerStyleRenderHelper.CreateFillBrush(snapshot, Color.Parse("#24E5C1"), Color.Parse("#24E5C1"), Color.Parse("#09111F"), 1d);
        var strokePen = LayerStyleRenderHelper.CreateStrokePen(snapshot, new ImmutableSolidColorBrush(Colors.White), 6d);
        var hiddenFill = LayerStyleRenderHelper.CreateFillBrush(snapshot with { HasFill = false }, Color.Parse("#24E5C1"), Color.Parse("#24E5C1"), Color.Parse("#09111F"), 1d);
        var hiddenStroke = LayerStyleRenderHelper.CreateStrokePen(snapshot with { HasStroke = false }, new ImmutableSolidColorBrush(Colors.White), 6d);

        Assert.IsType<ImmutableRadialGradientBrush>(fillBrush);
        Assert.Equal(PenLineCap.Square, strokePen.LineCap);
        Assert.Equal(PenLineJoin.Miter, strokePen.LineJoin);
        Assert.Equal(7d, strokePen.MiterLimit, 3);
        Assert.Equal(0, Assert.IsType<ImmutableSolidColorBrush>(hiddenFill).Color.A);
        Assert.Equal(0, Assert.IsType<ImmutableSolidColorBrush>(hiddenStroke.Brush!).Color.A);
    }

    [Fact]
    public void TimelineEditingService_CreateLayerFromLibraryItem_UsesRegistrationPoint()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 20d, 30d, 0);
        source.Defaults.Width = 220d;
        source.Defaults.Height = 132d;
        var item = TimelineEditingService.CreateLibraryItemFromLayer(source, "Registered Symbol", SymbolKind.Graphic);
        item.RegistrationPointX = 0.5d;
        item.RegistrationPointY = 0.25d;

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(item, 400d, 240d, 3);

        Assert.Equal(400d - (instance.Defaults.Width * 0.5d), instance.Defaults.X, 3);
        Assert.Equal(240d - (instance.Defaults.Height * 0.25d), instance.Defaults.Y, 3);
    }

    [Fact]
    public void LayerTransformHelper_UsesRegistrationPointAsTransformOrigin()
    {
        var snapshot = new LayerSnapshot(
            100d,
            120d,
            80d,
            40d,
            2d,
            1d,
            0d,
            0d,
            0d,
            1d,
            0d,
            0d,
            0d,
            0d,
            0d,
            "#24E5C1",
            "#FFFFFF",
            string.Empty,
            18d,
            1d,
            false,
            "#24E5C1",
            "#FFFFFF",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            false,
            0d,
            360d,
            false,
            "#57C9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings(),
            new LayerTextSettings());
        var rect = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        var topLeft = rect.TopLeft;

        var aroundTopLeft = LayerTransformHelper.TransformPoint(
            topLeft,
            rect,
            snapshot,
            new LibraryItem { RegistrationPointX = 0d, RegistrationPointY = 0d });
        var aroundCenter = LayerTransformHelper.TransformPoint(
            topLeft,
            rect,
            snapshot,
            new LibraryItem { RegistrationPointX = 0.5d, RegistrationPointY = 0.5d });

        Assert.Equal(topLeft.X, aroundTopLeft.X, 3);
        Assert.Equal(topLeft.Y, aroundTopLeft.Y, 3);
        Assert.Equal(60d, aroundCenter.X, 3);
        Assert.Equal(topLeft.Y, aroundCenter.Y, 3);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesFlashInteropEditors()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];

        var guide = TimelineEditingService.CreatePathLayer(
            "Guide Path",
            "#1E3352",
            "#8EEAFF",
            [new VectorPointModel { X = 0d, Y = 0.25d }, new VectorPointModel { X = 1d, Y = 0.8d }],
            false,
            scene.Layers.Count,
            3d);
        guide.Compositing.Role = LayerCompositeRole.Guide;
        scene.Layers.Add(guide);

        var guidedLayer = scene.Layers.First(layer => layer.Name == "CTA");
        guidedLayer.GuidedByLayerId = guide.Id;
        guidedLayer.OrientToGuidePath = true;
        guidedLayer.SnapToGuidePath = true;

        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(scene.Layers[0], "Registered Symbol", SymbolKind.Graphic);
        symbol.RegistrationPointX = 0.33d;
        symbol.RegistrationPointY = 0.66d;
        TimelineEditingService.AddLibraryItem(document, symbol);

        var field = TimelineEditingService.CreateLayer(LayerKind.Text, "Flash Field", "#F7F5ED", "bind me", 90d, 250d, scene.Layers.Count);
        field.Style.TextSettings.FieldKind = FlashTextFieldKind.Dynamic;
        field.Style.TextSettings.LineMode = FlashTextLineMode.Multiline;
        field.Style.TextSettings.IsSelectable = false;
        field.Style.TextSettings.ShowBorder = true;
        field.Style.TextSettings.VariableName = "captionText";
        scene.Layers.Add(field);
        document.Layers = scene.Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "authoring.xfl", TimelineDocumentFileFormat.FlashXfl);

        viewModel.SelectedLibraryItem = viewModel.LibraryItems.First(item => item.Name == "Registered Symbol");
        Assert.Equal(0.33d, viewModel.LibraryRegistrationPointXEditor, 3);
        Assert.Equal(0.66d, viewModel.LibraryRegistrationPointYEditor, 3);

        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Flash Field");
        Assert.Equal(FlashTextFieldKind.Dynamic, viewModel.TextFieldKindEditor);
        Assert.Equal(FlashTextLineMode.Multiline, viewModel.TextLineModeEditor);
        Assert.False(viewModel.TextSelectableEditor);
        Assert.True(viewModel.TextShowBorderEditor);
        Assert.Equal("captionText", viewModel.TextVariableNameEditor);

        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "CTA");
        Assert.NotNull(viewModel.SelectedGuideLayer);
        Assert.Equal("Guide Path", viewModel.SelectedGuideLayer!.Name);
        Assert.True(viewModel.GuideOrientToPathEditor);
        Assert.True(viewModel.GuideSnapToPathEditor);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesFlashEditorMetadataEditors()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];

        var outlined = scene.Layers.First(layer => layer.Name == "Card");
        outlined.ShowAsOutline = true;
        outlined.OutlineColor = "#FFD166";

        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(outlined, "Bitmap Cached Symbol", SymbolKind.Graphic);
        TimelineEditingService.AddLibraryItem(document, symbol);
        var instance = TimelineEditingService.CreateLayerFromLibraryItem(symbol, 360d, 200d, scene.Layers.Count);
        instance.Name = "Cached Instance";
        instance.CacheAsBitmap = true;
        instance.BitmapCacheBackgroundColor = "#101820";
        scene.Layers.Add(instance);

        var field = TimelineEditingService.CreateLayer(LayerKind.Text, "Advanced Field", "#F7F5ED", "secret-code", 90d, 280d, scene.Layers.Count);
        field.Style.TextSettings.MaxCharacters = 5;
        field.Style.TextSettings.IsPassword = true;
        field.Style.TextSettings.UseDeviceFonts = true;
        field.Style.TextSettings.AntiAliasMode = FlashTextAntiAliasMode.Device;
        scene.Layers.Add(field);
        document.Layers = scene.Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "advanced.xfl", TimelineDocumentFileFormat.FlashXfl);

        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Card");
        Assert.True(viewModel.LayerOutlineEditor);
        Assert.Equal("#FFD166", viewModel.LayerOutlineColorEditor);

        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Cached Instance");
        Assert.True(viewModel.LayerCacheAsBitmapEditor);
        Assert.Equal("#101820", viewModel.LayerBitmapCacheBackgroundEditor);

        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Advanced Field");
        Assert.Equal(5, viewModel.TextMaxCharactersEditor);
        Assert.True(viewModel.TextPasswordEditor);
        Assert.True(viewModel.TextUseDeviceFontsEditor);
        Assert.Equal(FlashTextAntiAliasMode.Device, viewModel.TextAntiAliasModeEditor);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesAndEditsComponentParameterEditors()
    {
        var document = CreateComponentInteropDocument();
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "component.xfl", TimelineDocumentFileFormat.FlashXfl);

        viewModel.SelectedComponentItem = Assert.Single(viewModel.ComponentItems);
        Assert.Single(viewModel.SelectedComponentParameters);
        Assert.Equal("buttonLabel", viewModel.ComponentParameterNameEditor);
        Assert.Equal(ComponentParameterKind.String, viewModel.ComponentParameterKindEditor);
        Assert.Equal(ComponentParameterBindingTarget.Content, viewModel.ComponentParameterBindingTargetEditor);
        Assert.Equal("Launch", viewModel.ComponentParameterDefaultValueEditor);

        viewModel.SelectedLayer = Assert.Single(viewModel.Layers);
        Assert.True(viewModel.SelectedLayerIsComponentInstance);
        Assert.Single(viewModel.SelectedComponentOverrideItems);
        Assert.Equal("Launch", viewModel.ComponentOverrideValueEditor);

        viewModel.ComponentParameterNameEditor = "ctaLabel";

        Assert.Equal("ctaLabel", Assert.Single(viewModel.SelectedComponentParameters).Name);
        Assert.Equal("ctaLabel", Assert.Single(viewModel.SelectedLayer.Model.ComponentParameterOverrides).Name);

        viewModel.ComponentOverrideValueEditor = "Watch now";
        Assert.Equal("Watch now", Assert.Single(viewModel.SelectedLayer.Model.ComponentParameterOverrides).Value);

        viewModel.ResetSelectedComponentOverrideCommand.Execute(null);
        Assert.Equal("Launch", Assert.Single(viewModel.SelectedLayer.Model.ComponentParameterOverrides).Value);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesFlashTransformEditors()
    {
        var document = CreateInteropDocument();
        var rectangle = document.Scenes[0].Layers.First(layer => layer.Name == "Card");
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.ScaleX, 1.18d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.ScaleY, 0.88d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.SkewX, 9d);
        TimelineEditingService.UpdateDefaultValue(rectangle, AnimatedProperty.SkewY, -4d);

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "transform.xfl", TimelineDocumentFileFormat.FlashXfl);
        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Card");

        Assert.Equal(1.18d, viewModel.InspectorScaleX, 3);
        Assert.Equal(0.88d, viewModel.InspectorScaleY, 3);
        Assert.Equal(9d, viewModel.InspectorSkewX, 3);
        Assert.Equal(-4d, viewModel.InspectorSkewY, 3);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesFlashColorEffectEditors()
    {
        var document = CreateInteropDocument();
        var rectangle = document.Scenes[0].Layers.First(layer => layer.Name == "Card");
        rectangle.Compositing.FlashColorEffectMode = FlashColorEffectMode.Advanced;
        rectangle.Compositing.FlashAdvancedAlphaPercent = 66d;
        rectangle.Compositing.FlashRedPercent = 112d;
        rectangle.Compositing.FlashGreenPercent = 84d;
        rectangle.Compositing.FlashBluePercent = 135d;
        rectangle.Compositing.FlashAlphaOffset = 8d;
        rectangle.Compositing.FlashRedOffset = 6d;
        rectangle.Compositing.FlashGreenOffset = -4d;
        rectangle.Compositing.FlashBlueOffset = 12d;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "visual-effects.xfl", TimelineDocumentFileFormat.FlashXfl);
        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Card");

        Assert.Equal(FlashColorEffectMode.Advanced, viewModel.LayerFlashColorEffectModeEditor);
        Assert.True(viewModel.SelectedLayerUsesFlashAdvancedColorEffect);
        Assert.False(viewModel.SelectedLayerUsesFlashTintEffect);
        Assert.Equal(66d, viewModel.LayerFlashAdvancedAlphaPercentEditor, 3);
        Assert.Equal(112d, viewModel.LayerFlashRedPercentEditor, 3);
        Assert.Equal(84d, viewModel.LayerFlashGreenPercentEditor, 3);
        Assert.Equal(135d, viewModel.LayerFlashBluePercentEditor, 3);
        Assert.Equal(8d, viewModel.LayerFlashAlphaOffsetEditor, 3);
        Assert.Equal(6d, viewModel.LayerFlashRedOffsetEditor, 3);
        Assert.Equal(-4d, viewModel.LayerFlashGreenOffsetEditor, 3);
        Assert.Equal(12d, viewModel.LayerFlashBlueOffsetEditor, 3);
        Assert.Contains("Advanced RGBA", viewModel.FlashColorEffectSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowViewModel_RefreshesFlashShapeStyleEditors()
    {
        var document = CreateInteropDocument();
        var scene = document.Scenes[0];
        var styledPath = TimelineEditingService.CreatePathLayer(
            "Styled Path",
            "#24E5C1",
            "#FFD166",
            [new VectorPointModel { X = 0d, Y = 0.9d }, new VectorPointModel { X = 0.45d, Y = 0.12d }, new VectorPointModel { X = 1d, Y = 0.8d }],
            false,
            scene.Layers.Count,
            6d,
            true,
            "#24E5C1",
            "#09111F",
            LayerGradientKind.Radial,
            120d,
            false,
            true,
            LayerStrokeCapStyle.Square,
            LayerStrokeJoinStyle.Miter,
            8d);
        scene.Layers.Add(styledPath);
        document.Layers = scene.Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "shape-style.xfl", TimelineDocumentFileFormat.FlashXfl);
        viewModel.SelectedLayer = viewModel.Layers.First(layer => layer.Name == "Styled Path");

        Assert.False(viewModel.FillEnabledEditor);
        Assert.True(viewModel.StrokeEnabledEditor);
        Assert.True(viewModel.UseGradientEditor);
        Assert.Equal(LayerGradientKind.Radial, viewModel.GradientKindEditor);
        Assert.Equal(120d, viewModel.GradientAngleEditor, 3);
        Assert.Equal(LayerStrokeCapStyle.Square, viewModel.StrokeCapStyleEditor);
        Assert.Equal(LayerStrokeJoinStyle.Miter, viewModel.StrokeJoinStyleEditor);
        Assert.True(viewModel.SelectedLayerUsesMiterJoin);
        Assert.Equal(8d, viewModel.StrokeMiterLimitEditor, 3);
    }

    [Fact]
    public void MainWindowViewModel_CapturesAppliesAndDeletesMotionPresets()
    {
        var document = CreateInteropDocument();
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "motion.timeline.json");

        var sourceLayer = viewModel.Layers.First(layer => layer.Name == "Card");
        var targetLayer = viewModel.Layers.First(layer => layer.Name == "CTA");
        viewModel.SelectedLayer = sourceLayer;
        var initialPresetCount = viewModel.MotionPresets.Count;

        viewModel.MotionPresetNameEditor = "Hero Sweep";
        viewModel.MotionPresetDescriptionEditor = "Primary entrance";
        viewModel.CaptureMotionPresetCommand.Execute(null);

        Assert.Equal(initialPresetCount + 1, viewModel.MotionPresets.Count);
        Assert.StartsWith("Hero Sweep", viewModel.SelectedMotionPreset?.Name, StringComparison.Ordinal);

        viewModel.SelectedLayer = targetLayer;
        viewModel.ApplySelectedMotionPresetCommand.Execute(null);

        Assert.Contains(targetLayer.Model.Tracks, track => track.Property == AnimatedProperty.X && track.Keyframes.Count > 0);
        Assert.True(viewModel.CanDeleteSelectedMotionPreset);

        var presetId = viewModel.SelectedMotionPreset!.Id;
        viewModel.DeleteSelectedMotionPresetCommand.Execute(null);

        Assert.DoesNotContain(viewModel.MotionPresets, preset => preset.Id == presetId);
    }

    [Fact]
    public void MainWindowViewModel_AssignsTransformParent_AndCapturesRelationship()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(CreateInteropDocument(), "parenting.xfl", TimelineDocumentFileFormat.FlashXfl);

        var parent = viewModel.Layers.First(layer => layer.Name == "Card");
        var child = viewModel.Layers.First(layer => layer.Name == "CTA");
        viewModel.SelectedLayer = child;
        viewModel.SelectedTransformParentLayer = parent;

        Assert.Equal(parent.Id, child.Model.TransformParenting.ParentLayerId);
        Assert.True(viewModel.HasTransformParent);
        Assert.Contains("Card", viewModel.TransformParentingSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowViewModel_CapturesAndAppliesRigPose()
    {
        var scene = new SceneModel
        {
            Name = "Rig Scene",
            Duration = 2d,
            FrameRate = 24d,
            CanvasWidth = 800d,
            CanvasHeight = 450d,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748"
        };
        var pathLayer = TimelineEditingService.CreatePathLayer(
            "Rig Path",
            "#24E5C1",
            "#FFFFFF",
            [new VectorPointModel { X = 0d, Y = 0.85d }, new VectorPointModel { X = 0.45d, Y = 0.15d }, new VectorPointModel { X = 1d, Y = 0.82d }],
            false,
            0,
            5d);
        scene.Layers.Add(pathLayer);
        var document = new TimelineDocument
        {
            Name = "Rigging",
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            Layers = scene.Layers,
            Scenes = [scene],
            ActiveSceneId = scene.Id
        };
        PublishProfileService.EnsureProfiles(document);

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "rigging.xfl", TimelineDocumentFileFormat.FlashXfl);
        viewModel.SelectedLayer = viewModel.Layers.First();

        viewModel.EnsureDefaultRigCommand.Execute(null);
        viewModel.RigPoseNameEditor = "Reach";
        var originalRotation = viewModel.SelectedLayer!.Model.Rig.Bones[0].Rotation;
        var originalLength = viewModel.SelectedLayer.Model.Rig.Bones[0].Length;

        viewModel.CaptureRigPoseCommand.Execute(null);
        Assert.True(viewModel.HasRigPoses);
        Assert.Equal("Reach", viewModel.SelectedRigPose!.Name);

        var bone = viewModel.SelectedLayer.Model.Rig.Bones[0];
        viewModel.UpdateRigBoneHandle(viewModel.SelectedLayer.Id, bone.Id, RigBoneHandleKind.End, new Point(0.95d, 0.92d));
        Assert.NotEqual(originalRotation, bone.Rotation);

        viewModel.ApplySelectedRigPoseCommand.Execute(null);
        Assert.Equal(originalRotation, bone.Rotation, 3);
        Assert.Equal(originalLength, bone.Length, 3);

        viewModel.DeleteSelectedRigPoseCommand.Execute(null);
        Assert.False(viewModel.HasRigPoses);
    }

    [Fact]
    public async Task TimelineDocumentFileService_SavesFlashPackage_WithLibraryFilesAndSettings()
    {
        var document = CreateInteropDocument();
        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(document.Layers[0], "Flash Shared Symbol", SymbolKind.Graphic);
        TimelineEditingService.AddLibraryItem(document, symbol);
        await using var stream = new MemoryStream();

        await TimelineDocumentFileService.SaveAsync(stream, document, TimelineDocumentFileFormat.FlashXfl, "interop.fla");
        stream.Position = 0;

        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
        Assert.NotNull(archive.GetEntry("DOMDocument.xml"));
        Assert.NotNull(archive.GetEntry("LIBRARY/manifest.xml"));
        Assert.NotNull(archive.GetEntry("PublishSettings.xml"));
        Assert.Contains(archive.Entries, entry =>
            entry.FullName.StartsWith("LIBRARY/", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(entry.Name, "manifest.xml", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TimelineDocumentFileService_LoadsFlashPackage_FromSeparateLibraryFiles()
    {
        var document = CreateInteropDocument();
        var symbol = TimelineEditingService.CreateLibraryItemFromLayer(document.Layers[0], "Flash Shared Symbol", SymbolKind.MovieClip);
        TimelineEditingService.AddLibraryItem(document, symbol);
        var scene = document.Scenes[0];
        var instance = TimelineEditingService.CreateLayerFromLibraryItem(symbol, 260d, 140d, scene.Layers.Count);
        instance.Name = "Separate Library Instance";
        instance.InstanceName = "separateLibraryInstance";
        scene.Layers.Add(instance);
        document.Layers = scene.Layers;

        await using var sourceStream = new MemoryStream();
        await TimelineDocumentFileService.SaveAsync(sourceStream, document, TimelineDocumentFileFormat.FlashXfl, "interop.fla");
        sourceStream.Position = 0;

        string domDocumentContent;
        var libraryEntries = new List<(string FullName, string Content)>();
        using (var sourceArchive = new System.IO.Compression.ZipArchive(sourceStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true))
        {
            using (var documentReader = new StreamReader(sourceArchive.GetEntry("DOMDocument.xml")!.Open()))
            {
                domDocumentContent = await documentReader.ReadToEndAsync();
            }

            foreach (var entry in sourceArchive.Entries.Where(entry =>
                         entry.FullName.StartsWith("LIBRARY/", StringComparison.OrdinalIgnoreCase) &&
                         entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(entry.Name, "manifest.xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var reader = new StreamReader(entry.Open());
                libraryEntries.Add((entry.FullName, await reader.ReadToEndAsync()));
            }
        }

        var documentXml = System.Xml.Linq.XDocument.Parse(domDocumentContent);
        documentXml.Root?.Elements().FirstOrDefault(element => element.Name.LocalName == "symbols")?.Remove();

        await using var repackagedStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(repackagedStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var documentEntry = archive.CreateEntry("DOMDocument.xml");
            await using (var entryStream = documentEntry.Open())
            await using (var writer = new StreamWriter(entryStream))
            {
                await writer.WriteAsync(documentXml.ToString());
            }

            foreach (var libraryEntry in libraryEntries)
            {
                var entry = archive.CreateEntry(libraryEntry.FullName);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync(libraryEntry.Content);
            }
        }

        repackagedStream.Position = 0;
        var loaded = await TimelineDocumentFileService.LoadAsync(repackagedStream, "interop.fla");
        var loadedInstance = loaded.Document.Layers.First(layer => layer.Name == "Separate Library Instance");

        Assert.NotEmpty(loaded.Document.LibraryItems);
        Assert.NotNull(loadedInstance.SourceLibraryItemId);
        Assert.Equal("separateLibraryInstance", loadedInstance.InstanceName);
    }

    [Fact]
    public void AnimationExchangeService_AvaloniaXaml_FallbackImports_KeyframeStyles()
    {
        const string xaml = """
                            <UserControl xmlns="https://github.com/avaloniaui"
                                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                              <UserControl.Styles>
                                <Style Selector="#cta">
                                  <Style.Animations>
                                    <Animation Duration="0:0:2" FillMode="Forward" IterationCount="1">
                                      <KeyFrame Cue="0%">
                                        <Setter Property="Canvas.Left" Value="40" />
                                        <Setter Property="Opacity" Value="0.4" />
                                      </KeyFrame>
                                      <KeyFrame Cue="100%">
                                        <Setter Property="Canvas.Left" Value="180" />
                                        <Setter Property="Opacity" Value="1" />
                                      </KeyFrame>
                                    </Animation>
                                  </Style.Animations>
                                </Style>
                              </UserControl.Styles>
                              <Canvas Width="640" Height="360">
                                <Button x:Name="cta"
                                        Canvas.Left="40"
                                        Canvas.Top="60"
                                        Width="200"
                                        Height="52"
                                        Content="CTA" />
                              </Canvas>
                            </UserControl>
                            """;

        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.AvaloniaXaml, xaml, "cta.axaml");
        var layer = Assert.Single(imported.Document.Layers);
        var xTrack = layer.Tracks.First(track => track.Property == AnimatedProperty.X);
        var opacityTrack = layer.Tracks.First(track => track.Property == AnimatedProperty.Opacity);

        Assert.Equal(LayerKind.AvaloniaControl, layer.Kind);
        Assert.Equal(AvaloniaControlKind.Button, layer.Style.AvaloniaControl.Kind);
        Assert.Contains(xTrack.Keyframes, keyframe => Math.Abs(keyframe.Time - 2d) < 0.0001d && Math.Abs(keyframe.Value - 180d) < 0.0001d);
        Assert.Contains(opacityTrack.Keyframes, keyframe => Math.Abs(keyframe.Value - 1d) < 0.0001d);
    }

    [Fact]
    public void AnimationExchangeService_AvaloniaXaml_FallbackImports_RootCanvasAndExpandedControls()
    {
        const string xaml = """
                            <Canvas xmlns="https://github.com/avaloniaui"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                    Width="640"
                                    Height="360">
                              <ComboBox Canvas.Left="24"
                                        Canvas.Top="32"
                                        Width="220"
                                        Height="52">
                                <ComboBoxItem Content="Alpha" />
                                <ComboBoxItem Content="Beta" />
                                <ComboBoxItem Content="Gamma" />
                              </ComboBox>
                              <PathIcon Canvas.Left="260"
                                        Canvas.Top="96"
                                        Width="64"
                                        Height="64"
                                        Data="M 12 2 L 22 22 L 2 22 Z" />
                            </Canvas>
                            """;

        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.AvaloniaXaml, xaml, "surface.axaml");
        var combo = imported.Document.Layers.First(layer => layer.Style.AvaloniaControl.Kind == AvaloniaControlKind.ComboBox);
        var icon = imported.Document.Layers.First(layer => layer.Style.AvaloniaControl.Kind == AvaloniaControlKind.PathIcon);

        Assert.Equal(2, imported.Document.Layers.Count);
        Assert.Equal("Alpha|Beta|Gamma", combo.Style.AvaloniaControl.SecondaryContent);
        Assert.Equal("M 12 2 L 22 22 L 2 22 Z", icon.Style.AvaloniaControl.SecondaryContent);
        Assert.DoesNotContain(imported.Issues, issue => string.Equals(issue.Source, "Avalonia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnimationExchangeService_AvaloniaXaml_FallbackImports_ProjectSurfaceWithoutCanvas()
    {
        const string xaml = """
                            <Window xmlns="https://github.com/avaloniaui"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                    Width="640"
                                    Height="360">
                              <Grid Width="640" Height="360">
                                <TextBlock Text="Project Title"
                                           Width="220"
                                           Height="60"
                                           FontSize="28" />
                              </Grid>
                            </Window>
                            """;

        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.AvaloniaXaml, xaml, "window.axaml");

        Assert.Single(imported.Document.Layers);
        Assert.Contains(imported.Issues, issue => string.Equals(issue.Source, "Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(LayerKind.Text, imported.Document.Layers[0].Kind);
    }

    [Fact]
    public void AnimationExchangeService_SvgFallback_ImportsGeneratedAnimation()
    {
        var document = CreateInteropDocument();
        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.SvgSmil);
        Assert.DoesNotContain("TimelineAnimationsMetadata", export.Content, StringComparison.Ordinal);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.SvgSmil, export.Content, "interop.svg");
        var rectangle = imported.Document.Layers.First(layer => layer.Kind == LayerKind.Rectangle);
        var xTrack = rectangle.Tracks.First(track => track.Property == AnimatedProperty.X);

        Assert.True(imported.Document.Layers.Count >= 2);
        Assert.True(xTrack.Keyframes.Count > 2);
    }

    [Fact]
    public void AnimationExchangeService_HtmlFallback_ImportsGeneratedAnimation()
    {
        var document = CreateInteropDocument();
        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.HtmlCss);
        Assert.DoesNotContain("timeline-animations-metadata", export.Content, StringComparison.Ordinal);
        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.HtmlCss, export.Content, "interop.html");
        var button = imported.Document.Layers.First(layer => layer.Kind == LayerKind.AvaloniaControl);
        var yTrack = button.Tracks.First(track => track.Property == AnimatedProperty.Y);

        Assert.Equal(AvaloniaControlKind.Button, button.Style.AvaloniaControl.Kind);
        Assert.True(yTrack.Keyframes.Count > 2);
    }

    [Fact]
    public void AnimationExchangeService_HtmlFallback_ImportsExpandedControlsAndTransformHints()
    {
        const string html = """
                            <html xmlns="http://www.w3.org/1999/xhtml">
                              <head>
                                <style>
                                  .layer { position:absolute; box-sizing:border-box; }
                                </style>
                              </head>
                              <body>
                                <div id="stage"
                                     data-canvas-width="640"
                                     data-canvas-height="360"
                                     style="width:640px;height:360px;background:linear-gradient(135deg,#09111F,#182748);">
                                  <select data-layer-kind="AvaloniaControl"
                                          data-control-kind="ComboBox"
                                          style="position:absolute;left:20px;top:24px;width:220px;height:52px;">
                                    <option selected="selected">Alpha</option>
                                    <option>Beta</option>
                                  </select>
                                  <div data-layer-kind="AvaloniaControl"
                                       data-control-kind="PathIcon"
                                       data-path="M 0 0 L 24 12 L 0 24 Z"
                                       style="position:absolute;left:120px;top:80px;width:64px;height:64px;transform:translate(10px,5px) scale(1.5);"></div>
                                </div>
                              </body>
                            </html>
                            """;

        var imported = AnimationExchangeService.Import(AnimationExchangeFormat.HtmlCss, html, "controls.html");
        var combo = imported.Document.Layers.First(layer => layer.Style.AvaloniaControl.Kind == AvaloniaControlKind.ComboBox);
        var icon = imported.Document.Layers.First(layer => layer.Style.AvaloniaControl.Kind == AvaloniaControlKind.PathIcon);

        Assert.Equal("Alpha", combo.Style.AvaloniaControl.Content);
        Assert.Equal("Alpha|Beta", combo.Style.AvaloniaControl.SecondaryContent);
        Assert.Equal("M 0 0 L 24 12 L 0 24 Z", icon.Style.AvaloniaControl.SecondaryContent);
        Assert.Equal(130d, icon.Defaults.X, 3);
        Assert.Equal(96d, icon.Defaults.Width, 3);
    }

    [Fact]
    public void AnimationExchangeService_Export_ReportsStructuredInteropIssues()
    {
        var document = CreateInteropDocument();
        var grid = TimelineEditingService.CreateAvaloniaControlLayer(AvaloniaControlKind.Grid, "Metrics Grid", "#233149", "#E6F1FF", "2x2 Grid", 180d, 120d, document.Layers.Count);
        grid.Style.AvaloniaControl.SecondaryContent = "A|B|C|D";
        TimelineEditingService.AddLayer(document, grid);

        var export = AnimationExchangeService.Export(document, AnimationExchangeFormat.HtmlCss);

        Assert.Contains(export.Issues, issue => issue.Source == "Motion" && issue.Severity == AnimationExchangeIssueSeverity.Info);
        Assert.Contains(export.Issues, issue => issue.Source == "Controls" && issue.Severity == AnimationExchangeIssueSeverity.Info);
        Assert.DoesNotContain(export.Issues, issue => string.Equals(issue.Source, "Metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(AnimationExchangeFormat.AvaloniaXaml)]
    [InlineData(AnimationExchangeFormat.FlashXfl)]
    [InlineData(AnimationExchangeFormat.SvgSmil)]
    [InlineData(AnimationExchangeFormat.HtmlCss)]
    public void AnimationExchangePreviewService_BuildsPreviewSnapshots(AnimationExchangeFormat format)
    {
        var preview = AnimationExchangePreviewService.BuildPreview(CreateInteropDocument(), format);

        Assert.Equal(format, preview.Format);
        Assert.False(string.IsNullOrWhiteSpace(preview.Code));
        Assert.False(string.IsNullOrWhiteSpace(preview.SuggestedFileName));
        Assert.False(string.IsNullOrWhiteSpace(preview.Summary));
        Assert.False(string.IsNullOrWhiteSpace(preview.VisualSummary));
        Assert.NotNull(preview.PreviewDocument);
        Assert.True(preview.PreviewDocument.Layers.Count >= 2);
    }

    [Fact]
    public void MainWindowViewModel_AnimationExchangePreview_TracksFormatAndLayout()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SelectedAnimationExchangePreviewMode = AnimationExchangePreviewMode.Preview;
        viewModel.SelectedAnimationExchangeFormat = AnimationExchangeFormat.HtmlCss;

        Assert.False(viewModel.IsAnimationExchangeCodeVisible);
        Assert.True(viewModel.IsAnimationExchangeVisualVisible);
        Assert.True(viewModel.AnimationExchangeVisualColumnWidth.IsStar);
        Assert.Equal(0d, viewModel.AnimationExchangeCodeColumnWidth.Value);
        Assert.Contains("<html", viewModel.AnimationExchangePreviewCode, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(viewModel.AnimationExchangePreviewDocument);

        viewModel.RefreshAnimationExchangePreviewCommand.Execute(null);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.AnimationExchangePreviewSummaryDisplay));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.AnimationExchangePreviewFileSummary));
    }

    [Fact]
    public void MainWindowViewModel_AddLayerFromPalette_CreatesAvaloniaControlInstance()
    {
        var viewModel = new MainWindowViewModel();
        var initialCount = viewModel.Document.Layers.Count;

        viewModel.AddLayerFromPalette(LayerKind.AvaloniaControl, new Avalonia.Point(320, 180), AvaloniaControlKind.Slider);

        var created = Assert.Single(viewModel.Document.Layers.Skip(initialCount));
        Assert.Equal(LayerKind.AvaloniaControl, created.Kind);
        Assert.Equal(AvaloniaControlKind.Slider, created.Style.AvaloniaControl.Kind);
    }

    [Fact]
    public void MainWindowViewModel_AvaloniaToolbox_IncludesExpandedControls()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Contains(viewModel.AvaloniaToolboxItems, item => item.AvaloniaControlKind == AvaloniaControlKind.ComboBox);
        Assert.Contains(viewModel.AvaloniaToolboxItems, item => item.AvaloniaControlKind == AvaloniaControlKind.Grid);
        Assert.Contains(viewModel.AvaloniaToolboxItems, item => item.AvaloniaControlKind == AvaloniaControlKind.PathIcon);
    }

    [Fact]
    public void LayerHierarchyService_FlattensFolders_And_HonorsCollapse()
    {
        var folder = TimelineEditingService.CreateFolderLayer("Folder", 2);
        var child = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Child", "#24E5C1", string.Empty, 0, 0, 1);
        var root = TimelineEditingService.CreateLayer(LayerKind.Text, "Root", "#FFFFFF", "Text", 0, 0, 0);
        child.ParentLayerId = folder.Id;

        var flattened = LayerHierarchyService.Flatten([root, child, folder]);

        Assert.Collection(
            flattened,
            first =>
            {
                Assert.Equal(folder.Id, first.Layer.Id);
                Assert.Equal(0, first.Depth);
                Assert.True(first.HasChildren);
            },
            second =>
            {
                Assert.Equal(child.Id, second.Layer.Id);
                Assert.Equal(1, second.Depth);
            },
            third => Assert.Equal(root.Id, third.Layer.Id));

        folder.IsExpanded = false;
        flattened = LayerHierarchyService.Flatten([root, child, folder]);
        Assert.DoesNotContain(flattened, item => item.Layer.Id == child.Id);
    }

    [Fact]
    public void LayerHierarchyService_SoloFolder_KeepsChildRenderable()
    {
        var folder = TimelineEditingService.CreateFolderLayer("Folder", 2);
        folder.IsSolo = true;
        var child = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Child", "#24E5C1", string.Empty, 0, 0, 1);
        child.ParentLayerId = folder.Id;
        var root = TimelineEditingService.CreateLayer(LayerKind.Text, "Root", "#FFFFFF", "Text", 0, 0, 0);

        var renderable = LayerHierarchyService.GetRenderableLayers([root, child, folder]);

        Assert.Contains(renderable, item => item.Id == child.Id);
        Assert.DoesNotContain(renderable, item => item.Id == root.Id);
    }

    [Fact]
    public void PowerEditingService_AppliesValueAcrossSelectedFrames()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 0, 0, 0);

        var updated = PowerEditingService.ApplyValueAcrossFrames(layer, AnimatedProperty.X, 320d, 3, 5, 24d, 6d);

        Assert.True(updated);
        Assert.Contains(layer.Tracks.First(track => track.Property == AnimatedProperty.X).Keyframes, item => item.Time == FrameTimelineService.FrameToTime(3, 24d));
        Assert.Contains(layer.Tracks.First(track => track.Property == AnimatedProperty.X).Keyframes, item => item.Time == FrameTimelineService.FrameToTime(5, 24d));
    }

    [Fact]
    public void EditorialEditingService_SplitsAndRipplesMediaClips()
    {
        var asset = new MediaAsset
        {
            Name = "VO",
            Kind = MediaAssetKind.Audio,
            Duration = 6d,
            PreviewFill = "#61E6FF",
            PreviewAccent = "#FFFFFF",
            WaveformSamples = [0.1d, 0.4d, 0.8d, 0.3d]
        };
        var first = TimelineEditingService.CreateAudioLayer(asset, 0, 0d);
        var second = TimelineEditingService.CreateAudioLayer(asset, 1, 2d);

        var split = EditorialEditingService.RazorSplitLayer(second, asset, 3d, 2);

        Assert.NotNull(split);
        Assert.Equal(1d, second.Media.ClipDuration, 3);
        Assert.Equal(3d, split!.Media.StartTime, 3);

        var slipped = EditorialEditingService.Slip(second, asset, 0.4d);
        Assert.True(slipped);
        Assert.Equal(0.4d, second.Media.ClipOffset, 3);

        var rippled = EditorialEditingService.Ripple([first, second, split], second.Id, 0.5d);
        Assert.True(rippled);
        Assert.True(split.Media.StartTime > 3d);
    }

    [Fact]
    public void AudioMixService_BuildsMeterSnapshot_WithPanAndFades()
    {
        var asset = new MediaAsset
        {
            Name = "Pulse",
            Kind = MediaAssetKind.Audio,
            Duration = 4d,
            PreviewFill = "#61E6FF",
            PreviewAccent = "#FFFFFF",
            WaveformSamples = [0.2d, 0.4d, 0.8d, 0.6d, 0.3d]
        };
        var layer = TimelineEditingService.CreateAudioLayer(asset, 0, 0d);
        layer.Media.Volume = 0.8d;
        layer.Media.GainDb = 3d;
        layer.Media.Pan = 0.5d;
        layer.Media.FadeInDuration = 0.5d;
        layer.Media.FadeOutDuration = 0.5d;

        var meter = AudioMixService.BuildMeterSnapshot(layer, asset, 1.5d);

        Assert.True(meter.IsAudible);
        Assert.True(meter.Peak > 0d);
        Assert.True(meter.Rms > 0d);
        Assert.True(meter.RightGain > meter.LeftGain);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_HierarchyAndAudioFinishing()
    {
        var asset = new MediaAsset
        {
            Name = "Narration",
            Kind = MediaAssetKind.Audio,
            Duration = 3d,
            PreviewFill = "#61E6FF",
            PreviewAccent = "#FFFFFF"
        };
        var folder = TimelineEditingService.CreateFolderLayer("Folder", 1);
        var clip = TimelineEditingService.CreateAudioLayer(asset, 0, 0.5d);
        clip.ParentLayerId = folder.Id;
        clip.IsMuted = true;
        clip.IsSolo = true;
        clip.Media.GainDb = 4d;
        clip.Media.Pan = -0.3d;
        clip.Media.FadeInDuration = 0.2d;
        clip.Media.FadeOutDuration = 0.3d;

        var document = new TimelineDocument
        {
            Name = "Hierarchy",
            Layers = [clip, folder],
            MediaAssets = [asset]
        };

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));
        var restoredClip = restored.Layers.First(item => item.Kind == LayerKind.Audio);
        var restoredFolder = restored.Layers.First(item => item.Kind == LayerKind.Folder);

        Assert.Equal(restoredFolder.Id, restoredClip.ParentLayerId);
        Assert.True(restoredClip.IsMuted);
        Assert.True(restoredClip.IsSolo);
        Assert.Equal(4d, restoredClip.Media.GainDb);
        Assert.Equal(-0.3d, restoredClip.Media.Pan);
        Assert.Equal(0.2d, restoredClip.Media.FadeInDuration);
        Assert.Equal(0.3d, restoredClip.Media.FadeOutDuration);
    }

    [Fact]
    public void MainWindowViewModel_InitializesDockWorkspace()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.DockWorkspace.ResetLayoutCommand.Execute(null);

        Assert.True(viewModel.IsDockWorkspaceVisible);
        Assert.NotNull(viewModel.DockWorkspace.Layout);
        Assert.NotEmpty(viewModel.DockWorkspace.Layout!.VisibleDockables ?? []);
        Assert.All(DockWorkspaceSurfaceIds.All, surfaceId => Assert.Contains(surfaceId, EnumerateDockableIds(viewModel.DockWorkspace.Layout)));
    }

    [Fact]
    public void DockWorkspaceHostViewModel_AppliesPreset_AndCreatesAllEditingSurfaces()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.DockWorkspace.SelectedPreset = DockWorkspacePreset.Editorial;
        viewModel.DockWorkspace.ApplySelectedPresetCommand.Execute(null);

        Assert.Equal("Editorial", viewModel.DockWorkspace.CurrentPresetLabel);
        Assert.All(DockWorkspaceSurfaceIds.All, surfaceId => Assert.Contains(surfaceId, EnumerateDockableIds(viewModel.DockWorkspace.Layout)));
        Assert.Contains("first-class dockables", viewModel.DockWorkspace.WorkspaceDragHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DockWorkspaceHostViewModel_CanHideOverlayAndRestoreSurfaces()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.DockWorkspace.HideSurfaceCommand.Execute(DockWorkspaceSurfaceIds.SourceMonitorTool);
        Assert.Contains("Hidden", viewModel.DockWorkspace.SourceSurfaceMenuLabel, StringComparison.Ordinal);

        viewModel.DockWorkspace.OverlaySurfaceCommand.Execute(DockWorkspaceSurfaceIds.ProgramMonitorTool);
        Assert.Contains("Overlay", viewModel.DockWorkspace.ProgramSurfaceMenuLabel, StringComparison.Ordinal);

        viewModel.DockWorkspace.FocusSurfaceCommand.Execute(DockWorkspaceSurfaceIds.SourceMonitorTool);
        viewModel.DockWorkspace.RestoreAllSurfacesCommand.Execute(null);

        Assert.Contains("Docked", viewModel.DockWorkspace.SourceSurfaceMenuLabel, StringComparison.Ordinal);
        Assert.Contains("Docked", viewModel.DockWorkspace.ProgramSurfaceMenuLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void DockWorkspaceHostViewModel_TracksWhetherSurfacesAreActivelyVisible()
    {
        var viewModel = new MainWindowViewModel();

        Assert.True(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.StageDocument));
        Assert.True(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.FramesTimelineTool));
        Assert.True(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.InspectorTool));
        Assert.False(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.ProgramMonitorTool));

        viewModel.DockWorkspace.FocusSurfaceCommand.Execute(DockWorkspaceSurfaceIds.ProgramMonitorTool);

        Assert.True(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.ProgramMonitorTool));
        Assert.False(viewModel.DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.InspectorTool));
    }

    [Fact]
    public void MainWindowViewModel_AddBehavior_DoesNotAutoFillVisualStateTargets_OrCreateExtraUndoEntries()
    {
        var document = CreateInteropDocument();
        var layer = document.Scenes[0].Layers.First(item => item.Name == "Card");
        _ = VisualStateEditingService.CaptureState(layer, "CommonStates", "Focused", 0d);

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "behavior.timeline.json");
        viewModel.SelectedLayer = viewModel.Layers.First(item => item.Name == "Card");

        viewModel.AddBehaviorCommand.Execute(null);

        Assert.NotNull(viewModel.SelectedBehavior);
        Assert.Equal(InteractionActionKind.Play, viewModel.SelectedBehaviorActionEditor);
        Assert.Equal(string.Empty, viewModel.SelectedBehavior!.Model.TargetVisualStateGroup);
        Assert.Equal(string.Empty, viewModel.SelectedBehavior.Model.TargetVisualState);
        Assert.True(viewModel.CanUndo);

        viewModel.UndoCommand.Execute(null);

        Assert.Empty(viewModel.SelectedLayer!.Model.Behaviors);
    }

    [Fact]
    public void MainWindowViewModel_ApplyVisualStateBehavior_UsesSingleUndoStep()
    {
        var document = CreateInteropDocument();
        var layer = document.Scenes[0].Layers.First(item => item.Name == "Card");
        _ = VisualStateEditingService.CaptureState(layer, "CommonStates", "Focused", 0d);

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "behavior.timeline.json");
        viewModel.SelectedLayer = viewModel.Layers.First(item => item.Name == "Card");
        viewModel.AddBehaviorCommand.Execute(null);

        viewModel.SelectedBehaviorActionEditor = InteractionActionKind.ApplyVisualState;

        Assert.NotNull(viewModel.SelectedBehavior);
        Assert.Equal("CommonStates", viewModel.SelectedBehavior!.Model.TargetVisualStateGroup);
        Assert.Equal("Focused", viewModel.SelectedBehavior.Model.TargetVisualState);
        Assert.True(viewModel.CanUndo);

        viewModel.UndoCommand.Execute(null);

        Assert.Equal(InteractionActionKind.Play, viewModel.SelectedBehavior!.Model.Action);
        Assert.Equal(string.Empty, viewModel.SelectedBehavior.Model.TargetVisualStateGroup);
        Assert.Equal(string.Empty, viewModel.SelectedBehavior.Model.TargetVisualState);
    }

    [Fact]
    public void MainWindowViewModel_MoveKeyframe_DuringInteractiveChange_KeepsTrackRowsAlive()
    {
        var viewModel = new MainWindowViewModel();
        var layer = viewModel.Layers.First(item => item.Model.Tracks.Any(track => track.Property == AnimatedProperty.X && track.Keyframes.Count > 1));
        var modelTrack = layer.Model.Tracks.First(track => track.Property == AnimatedProperty.X);
        var keyframe = modelTrack.Keyframes.OrderBy(item => item.Time).Last();

        viewModel.SelectedLayer = layer;
        viewModel.SelectedProperty = AnimatedProperty.X;
        viewModel.SelectedKeyframeId = keyframe.Id;

        var initialCanUndo = viewModel.CanUndo;

        viewModel.BeginInteractiveChange();
        viewModel.MoveKeyframe(layer.Id, AnimatedProperty.X, keyframe.Id, keyframe.Time - 0.35d);

        var updatedTrack = viewModel.Tracks.First(item => item.Property == AnimatedProperty.X);
        var updatedRow = viewModel.TimelineRows.First(item => item.LayerId == layer.Id && item.Property == AnimatedProperty.X);
        var expectedTime = modelTrack.Keyframes.First(candidate => candidate.Id == keyframe.Id).Time;

        Assert.Contains(updatedTrack.Keyframes, item => item.Id == keyframe.Id && Math.Abs(item.Time - expectedTime) < 0.0001d);
        Assert.Contains(updatedRow.Keyframes, item => item.Id == keyframe.Id && Math.Abs(item.Time - expectedTime) < 0.0001d);
        Assert.Equal(initialCanUndo, viewModel.CanUndo);

        viewModel.CommitInteractiveChange("Keyframe moved");

        Assert.True(viewModel.CanUndo);
    }

    [Fact]
    public void MainWindowViewModel_SetSceneInPointFrame_DuringInteractiveChange_DoesNotMovePlayhead()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SeekFrame(24);
        var originalFrame = viewModel.CurrentFrame;

        viewModel.BeginInteractiveChange();
        viewModel.SetSceneInPointFrame(6);

        Assert.Equal(originalFrame, viewModel.CurrentFrame);
        Assert.Equal(6, viewModel.CurrentSceneInFrame);

        viewModel.CommitInteractiveChange("Frame range updated");
    }

    [Fact]
    public void MainWindowViewModel_PrototypeBehavior_CanApplyVisualState()
    {
        var viewModel = new MainWindowViewModel();
        var layer = viewModel.Layers.First(item => item.Kind == LayerKind.Rectangle);
        layer.Model.Tracks.Clear();
        layer.Model.Defaults.X = 128d;
        layer.Model.Defaults.Y = 96d;
        _ = VisualStateEditingService.CaptureState(layer.Model, "CommonStates", "Focused", viewModel.CurrentTime);
        layer.Model.Defaults.X = 412d;
        layer.Model.Defaults.Y = 288d;
        layer.Model.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Focus",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.ApplyVisualState,
            TargetVisualStateGroup = "CommonStates",
            TargetVisualState = "Focused"
        });
        viewModel.TogglePrototypeModeCommand.Execute(null);

        viewModel.HandlePrototypeTrigger(layer.Id, InteractionTriggerKind.PointerClick);

        Assert.Equal(128d, layer.Model.Defaults.X);
        Assert.Equal(96d, layer.Model.Defaults.Y);
    }

    private static TimelineDocument CreateInteropDocument()
    {
        var rectangle = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 40, 54, 0);
        rectangle.Defaults.Width = 220;
        rectangle.Defaults.Height = 132;
        rectangle.Defaults.Rotation = -4;
        rectangle.Defaults.Opacity = 0.45d;
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.X, 0d, 40d, 2d);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.X, 2d, 220d, 2d);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.Opacity, 0d, 0.45d, 2d);
        TimelineEditingService.SetKeyframe(rectangle, AnimatedProperty.Opacity, 2d, 1d, 2d);

        var button = TimelineEditingService.CreateAvaloniaControlLayer(AvaloniaControlKind.Button, "CTA", "#1F7DFF", "#E6F1FF", "Launch", 420, 220, 1);
        button.Defaults.Width = 210;
        button.Defaults.Height = 56;
        button.Style.AvaloniaControl.Content = "Launch";
        TimelineEditingService.SetKeyframe(button, AnimatedProperty.Y, 0d, 220d, 2d);
        TimelineEditingService.SetKeyframe(button, AnimatedProperty.Y, 2d, 280d, 2d);
        TimelineEditingService.SetKeyframe(button, AnimatedProperty.Rotation, 0d, 0d, 2d);
        TimelineEditingService.SetKeyframe(button, AnimatedProperty.Rotation, 2d, 8d, 2d);

        var scene = new SceneModel
        {
            Name = "Interop Scene",
            Duration = 2d,
            FrameRate = 24d,
            CanvasWidth = 800d,
            CanvasHeight = 450d,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            Layers = [rectangle, button]
        };

        var document = new TimelineDocument
        {
            Name = "Interop Demo",
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            Layers = scene.Layers,
            Scenes = [scene],
            ActiveSceneId = scene.Id
        };

        PublishProfileService.EnsureProfiles(document);
        return document;
    }

    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO2p3a4AAAAASUVORK5CYII=";

    private static TimelineDocument CreateHierarchyInteropDocument()
    {
        var folder = TimelineEditingService.CreateFolderLayer("Group", 0);
        var child = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Child Card", "#24E5C1", string.Empty, 80d, 96d, 1);
        child.ParentLayerId = folder.Id;
        child.Defaults.Width = 240d;
        child.Defaults.Height = 144d;
        TimelineEditingService.SetKeyframe(child, AnimatedProperty.X, 0d, 80d, 3d);
        TimelineEditingService.SetKeyframe(child, AnimatedProperty.X, 1.5d, 260d, 3d);
        TimelineEditingService.SetKeyframe(child, AnimatedProperty.Opacity, 0d, 0.5d, 3d);
        TimelineEditingService.SetKeyframe(child, AnimatedProperty.Opacity, 2d, 1d, 3d);

        var title = TimelineEditingService.CreateLayer(LayerKind.Text, "Title", "#FFFFFF", "Hierarchy", 420d, 72d, 2);

        var scene = new SceneModel
        {
            Name = "Hierarchy Scene",
            Duration = 3d,
            FrameRate = 24d,
            CanvasWidth = 1280d,
            CanvasHeight = 720d,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            Layers = [folder, child, title]
        };

        var document = new TimelineDocument
        {
            Name = "Hierarchy Demo",
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            Layers = scene.Layers,
            Scenes = [scene],
            ActiveSceneId = scene.Id
        };

        PublishProfileService.EnsureProfiles(document);
        return document;
    }

    private static TimelineDocument CreateComponentInteropDocument()
    {
        var template = TimelineEditingService.CreateAvaloniaControlLayer(
            AvaloniaControlKind.Button,
            "CTA Template",
            "#1F7DFF",
            "#E6F1FF",
            "Launch",
            120d,
            160d,
            0);

        var component = TimelineEditingService.CreateLibraryItemFromLayer(template, "CTA Component", SymbolKind.Button);
        component.IsComponent = true;
        component.ComponentCategory = "Controls";
        component.ComponentDescription = "Primary call-to-action component";
        component.ComponentParameters =
        [
            new ComponentParameterDefinition
            {
                Name = "buttonLabel",
                Kind = ComponentParameterKind.String,
                BindingTarget = ComponentParameterBindingTarget.Content,
                DefaultValue = "Launch",
                Description = "Primary button caption"
            }
        ];
        ComponentParameterService.NormalizeDefinitions(component);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(component, 260d, 180d, 0);
        instance.Name = "CTA Instance";

        var scene = new SceneModel
        {
            Name = "Component Scene",
            Duration = 2d,
            FrameRate = 24d,
            CanvasWidth = 800d,
            CanvasHeight = 450d,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            Layers = [instance]
        };

        var document = new TimelineDocument
        {
            Name = "Component Demo",
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            Layers = scene.Layers,
            Scenes = [scene],
            ActiveSceneId = scene.Id,
            LibraryItems = [component]
        };

        PublishProfileService.EnsureProfiles(document);
        return document;
    }

    private static HashSet<string> EnumerateDockableIds(IRootDock? root)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (root is null)
        {
            return ids;
        }

        foreach (var dockable in EnumerateDockables(root))
        {
            if (!string.IsNullOrWhiteSpace(dockable.Id))
            {
                ids.Add(dockable.Id);
            }
        }

        return ids;
    }

    private static IEnumerable<IDockable> EnumerateDockables(IDockable? dockable)
    {
        if (dockable is null)
        {
            yield break;
        }

        yield return dockable;

        if (dockable is IDock dock && dock.VisibleDockables is { Count: > 0 } visibleDockables)
        {
            foreach (var child in visibleDockables)
            {
                foreach (var nested in EnumerateDockables(child))
                {
                    yield return nested;
                }
            }
        }

        if (dockable is IRootDock rootDock)
        {
            foreach (var child in EnumerateCollection(rootDock.HiddenDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateCollection(rootDock.LeftPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateCollection(rootDock.RightPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateCollection(rootDock.TopPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateCollection(rootDock.BottomPinnedDockables))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<IDockable> EnumerateCollection(IEnumerable<IDockable>? dockables)
    {
        if (dockables is null)
        {
            yield break;
        }

        foreach (var dockable in dockables)
        {
            foreach (var nested in EnumerateDockables(dockable))
            {
                yield return nested;
            }
        }
    }

    [Fact]
    public void MainWindowViewModel_TracksNativeMenuLayoutState()
    {
        var viewModel = new MainWindowViewModel();

        Assert.True(viewModel.IsFallbackAppMenuVisible);
        Assert.False(viewModel.UseNativeMenuTitleBarLayout);
        Assert.True(viewModel.ShowWorkspacePresetStrip);
        Assert.True(viewModel.ShowSecondaryWorkspaceActions);
        Assert.True(viewModel.ShowDockWorkspaceOrganizerMenus);

        viewModel.SetNativeMenuExported(true);

        Assert.True(viewModel.IsNativeMenuExported);
        Assert.False(viewModel.IsFallbackAppMenuVisible);
        Assert.True(viewModel.UseNativeMenuTitleBarLayout);
        Assert.False(viewModel.ShowWorkspacePresetStrip);
        Assert.False(viewModel.ShowSecondaryWorkspaceActions);
        Assert.False(viewModel.ShowDockWorkspaceOrganizerMenus);
    }

    [Fact]
    public void ToolAuthoringService_RefinePencilStroke_StraightenProducesStraightLine()
    {
        var points = new[]
        {
            new VectorPointModel { X = 0, Y = 0 },
            new VectorPointModel { X = 12, Y = 5 },
            new VectorPointModel { X = 24, Y = 10 },
            new VectorPointModel { X = 36, Y = 4 },
            new VectorPointModel { X = 48, Y = 1 }
        };

        var refined = ToolAuthoringService.RefinePencilStroke(points, PencilToolMode.Straighten);

        Assert.Equal(2, refined.Count);
        Assert.Equal(refined[0].Y, refined[1].Y, 3);
    }

    [Fact]
    public void ToolAuthoringService_LayerIntersectsLasso_DetectsPathGeometry()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Path",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0.1d, Y = 0.2d },
                new VectorPointModel { X = 0.4d, Y = 0.25d },
                new VectorPointModel { X = 0.7d, Y = 0.55d }
            ],
            false,
            0,
            4d,
            false,
            "#24E5C1",
            "#FFFFFF",
            LayerGradientKind.Linear,
            45d,
            true,
            true,
            LayerStrokeCapStyle.Round,
            LayerStrokeJoinStyle.Round,
            3d);
        var snapshot = TimelineInterpolationService.SampleLayer(layer, 0d);
        var lasso = new[]
        {
            new VectorPointModel { X = snapshot.X - 10d, Y = snapshot.Y - 10d },
            new VectorPointModel { X = snapshot.X + snapshot.Width + 10d, Y = snapshot.Y - 10d },
            new VectorPointModel { X = snapshot.X + snapshot.Width + 10d, Y = snapshot.Y + snapshot.Height + 10d },
            new VectorPointModel { X = snapshot.X - 10d, Y = snapshot.Y + snapshot.Height + 10d }
        };

        var hit = ToolAuthoringService.LayerIntersectsLasso(layer.Kind, snapshot, lasso, contactSensitive: true);

        Assert.True(hit);
    }

    [Fact]
    public void ToolAuthoringService_ApplyEraser_ObjectModeRequestsLayerRemoval()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120d, 160d, 0);
        var snapshot = TimelineInterpolationService.SampleLayer(layer, 0d);

        var result = ToolAuthoringService.ApplyEraser(
            layer,
            snapshot,
            new VectorPointModel { X = 140d, Y = 180d },
            36d,
            EraserToolMode.Object,
            0d,
            createKeyframe: false,
            duration: 6d);

        Assert.True(result.Changed);
        Assert.True(result.RemoveLayer);
    }

    [Fact]
    public void MainWindowViewModel_ExposesNewAdobeStyleToolsAndOptions()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SelectDrawingToolCommand.Execute("RotateView");
        Assert.True(viewModel.IsRotateViewToolActive);
        Assert.True(viewModel.ShowRotateViewToolOptions);

        viewModel.SelectDrawingToolCommand.Execute("Lasso");
        Assert.True(viewModel.IsLassoToolActive);
        Assert.True(viewModel.ShowLassoToolOptions);

        viewModel.SelectDrawingToolCommand.Execute("Pencil");
        Assert.True(viewModel.IsPencilToolActive);
        Assert.True(viewModel.ShowPencilToolOptions);
        Assert.True(viewModel.ShowStrokeProfileToolOptions);

        viewModel.SelectDrawingToolCommand.Execute("PaintBucket");
        Assert.True(viewModel.IsPaintBucketToolActive);
        Assert.True(viewModel.ShowPaintBucketToolOptions);

        viewModel.SelectDrawingToolCommand.Execute("Width");
        Assert.True(viewModel.IsWidthToolActive);
        Assert.True(viewModel.ShowWidthToolOptions);
        Assert.True(viewModel.ShowStrokeProfileToolOptions);

        viewModel.SelectDrawingToolCommand.Execute("Eraser");
        Assert.True(viewModel.IsEraserToolActive);
        Assert.True(viewModel.ShowEraserToolOptions);
    }

    [Fact]
    public void VectorStrokeProfileService_ApplyProfile_TaperBoth_AdjustsPointWidths()
    {
        var points = new[]
        {
            new VectorPointModel { X = 0, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 10, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 20, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 30, Y = 0, StrokeWidthScale = 1d }
        };

        VectorStrokeProfileService.ApplyProfile(points, VectorStrokeProfilePreset.TaperBoth, 1d);

        Assert.True(points[0].StrokeWidthScale < points[1].StrokeWidthScale);
        Assert.True(points[^1].StrokeWidthScale < points[1].StrokeWidthScale);
        Assert.True(points[1].StrokeWidthScale >= 0.99d);
        Assert.True(points[2].StrokeWidthScale >= 0.99d);
    }

    [Fact]
    public void VectorStrokeProfileService_ApplyLocalizedWidthScale_AdjustsNeighbors()
    {
        var points = new[]
        {
            new VectorPointModel { X = 0, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 10, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 20, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 30, Y = 0, StrokeWidthScale = 1d },
            new VectorPointModel { X = 40, Y = 0, StrokeWidthScale = 1d }
        };

        VectorStrokeProfileService.ApplyLocalizedWidthScale(points, 2, 1.6d);

        Assert.Equal(1.6d, points[2].StrokeWidthScale, 3);
        Assert.True(points[1].StrokeWidthScale > 1d);
        Assert.True(points[3].StrokeWidthScale > 1d);
        Assert.True(points[0].StrokeWidthScale <= points[1].StrokeWidthScale);
        Assert.True(points[4].StrokeWidthScale <= points[3].StrokeWidthScale);
    }

    [Fact]
    public void PaintBucketToolService_AnalyzeFillTarget_RespectsGapThresholds()
    {
        var openLayer = TimelineEditingService.CreatePathLayer(
            "Open Path",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0d, Y = 0d },
                new VectorPointModel { X = 0.5d, Y = 0.14d },
                new VectorPointModel { X = 0.06d, Y = 0.02d }
            ],
            false,
            0,
            4d);
        var openPath = TimelineInterpolationService.SampleLayer(openLayer, 0d);

        var smallGap = PaintBucketToolService.AnalyzeFillTarget(openPath, PaintBucketGapSize.Small);
        var noGapAllowance = PaintBucketToolService.AnalyzeFillTarget(openPath, PaintBucketGapSize.None);

        Assert.True(smallGap.CanApply);
        Assert.True(smallGap.ClosePath);
        Assert.False(noGapAllowance.CanApply);
    }

    [Fact]
    public void MainWindowViewModel_UpdatePathPointWidthScale_UpdatesSelectedPathWidths()
    {
        var document = BlankProjectFactory.Create();
        var pathLayer = TimelineEditingService.CreatePathLayer(
            "Signal",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0.1d, Y = 0.3d, StrokeWidthScale = 1d },
                new VectorPointModel { X = 0.4d, Y = 0.35d, StrokeWidthScale = 1d },
                new VectorPointModel { X = 0.8d, Y = 0.45d, StrokeWidthScale = 1d }
            ],
            false,
            0,
            6d,
            false,
            "#24E5C1",
            "#FFFFFF",
            LayerGradientKind.Linear,
            45d,
            true,
            true,
            LayerStrokeCapStyle.Round,
            LayerStrokeJoinStyle.Round,
            4d);
        document.Scenes[0].Layers.Add(pathLayer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "vector-width.timeline.json");
        viewModel.SelectLayer(pathLayer.Id);
        viewModel.AutoKey = false;
        var before = viewModel.SelectedLayer!.Model.Style.PathPoints.Select(point => point.StrokeWidthScale).ToArray();

        viewModel.UpdatePathPointWidthScale(pathLayer.Id, 1, 1.65d);

        var updated = viewModel.SelectedLayer!.Model.Style.PathPoints;
        Assert.True(updated[1].StrokeWidthScale > before[1]);
        Assert.True(updated[0].StrokeWidthScale >= before[0]);
        Assert.True(updated[2].StrokeWidthScale >= before[2]);
    }

    [Fact]
    public void MainWindowViewModel_ApplyStageStyle_PreservesGradientTransform_WhenLockFillIsEnabled()
    {
        var document = BlankProjectFactory.Create();
        var pathLayer = TimelineEditingService.CreatePathLayer(
            "Gradient Path",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 0d, Y = 0d },
                new VectorPointModel { X = 80d, Y = 10d },
                new VectorPointModel { X = 40d, Y = 80d }
            ],
            true,
            0,
            6d,
            false,
            "#24E5C1",
            "#FFFFFF",
            LayerGradientKind.Radial,
            33d,
            true,
            true,
            LayerStrokeCapStyle.Round,
            LayerStrokeJoinStyle.Round,
            3d);
        pathLayer.Style.UseGradient = true;
        pathLayer.Style.GradientCenterX = 0.37d;
        pathLayer.Style.GradientCenterY = 0.64d;
        pathLayer.Style.GradientScaleX = 1.18d;
        pathLayer.Style.GradientScaleY = 0.82d;
        document.Scenes[0].Layers.Add(pathLayer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "lockfill.timeline.json");
        viewModel.PaintBucketLockFillEditor = true;
        viewModel.FillHexEditor = "#FFAA33";

        viewModel.ApplyStageStyle(pathLayer.Id, CanvasStyleApplicationKind.Fill);

        var style = viewModel.Layers.Single(item => item.Id == pathLayer.Id).Model.Style;
        Assert.Equal(0.37d, style.GradientCenterX, 3);
        Assert.Equal(0.64d, style.GradientCenterY, 3);
        Assert.Equal(1.18d, style.GradientScaleX, 3);
        Assert.Equal(0.82d, style.GradientScaleY, 3);
        Assert.Equal("#FFAA33", style.Fill, ignoreCase: true);
    }

    [Fact]
    public void MainWindowViewModel_ApplyCanvasErase_RemovesLayerInObjectMode()
    {
        var document = BlankProjectFactory.Create();
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120d, 160d, 0);
        document.Scenes[0].Layers.Add(layer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "tools.xfl", TimelineDocumentFileFormat.FlashXfl);
        viewModel.EraserToolModeEditor = EraserToolMode.Object;

        viewModel.ApplyCanvasErase(layer.Id, new Point(140d, 180d), 42d);

        Assert.DoesNotContain(viewModel.Layers, item => item.Id == layer.Id);
    }

    [Fact]
    public void MainWindowViewModel_ExposesAnimateTargetCompatibilityIssues()
    {
        var document = BlankProjectFactory.Create();
        var controlLayer = TimelineEditingService.CreateAvaloniaControlLayer(
            AvaloniaControlKind.Button,
            "CTA Button",
            "#1F7DFF",
            "#FFFFFF",
            "Click",
            120,
            120,
            0);
        document.Scenes[0].Layers.Add(controlLayer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "compatibility.timeline.json");
        viewModel.AnimateDocumentTypeEditor = AnimateDocumentType.WebGl;

        Assert.True(viewModel.HasAnimateDocumentCompatibilityIssues);
        Assert.NotEmpty(viewModel.AnimateDocumentCompatibilityIssues);
        Assert.Contains(viewModel.AnimateDocumentCompatibilityIssues, issue => issue.Message.Contains("manual translation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Convert To WebGL", viewModel.AnimateDocumentConvertButtonLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void LayerViewModel_ExposesPrimitiveShapeAuthoringSummary()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 80d, 90d, 0);
        layer.Style.DrawingMode = ShapeDrawingMode.Primitive;
        layer.Style.CornerRadiusTopLeft = 12d;
        layer.Style.CornerRadiusTopRight = 20d;
        layer.Style.CornerRadiusBottomRight = 28d;
        layer.Style.CornerRadiusBottomLeft = 16d;

        var viewModel = new LayerViewModel(layer);

        Assert.True(viewModel.HasShapeAuthoringSummary);
        Assert.Contains("rectangle", viewModel.ShapeAuthoringSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("12/20/28/16", viewModel.ShapeAuthoringSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void LayerViewModel_ExposesPolyStarShapeAuthoringSummary()
    {
        var layer = TimelineEditingService.CreatePolyStarLayer(
            "Burst",
            "#24E5C1",
            "#FFFFFF",
            7,
            0.42d,
            true,
            0,
            120d,
            140d,
            220d,
            220d,
            4d,
            ShapeDrawingMode.Primitive);

        var viewModel = new LayerViewModel(layer);

        Assert.True(viewModel.HasShapeAuthoringSummary);
        Assert.Contains("star", viewModel.ShapeAuthoringSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7 sides", viewModel.ShapeAuthoringSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderPixelBufferComposer_BlendsScreen_With_IntegerFastPath()
    {
        byte[] destination = [40, 100, 200, 255];
        byte[] source = [220, 180, 80, 255];

        RenderPixelBufferComposer.BlendPixels(destination, source, LayerBlendMode.Screen);

        Assert.Equal(new byte[] { 225, 209, 217, 255 }, destination);
    }

    [Fact]
    public void RenderPixelBufferComposer_BlendsMultiply_With_IntegerFastPath()
    {
        byte[] destination = [40, 100, 200, 255];
        byte[] source = [220, 180, 80, 255];

        RenderPixelBufferComposer.BlendPixels(destination, source, LayerBlendMode.Multiply);

        Assert.Equal(new byte[] { 35, 71, 63, 255 }, destination);
    }

    [Fact]
    public void MainWindowViewModel_PlaybackInspectorRefresh_DoesNotInsertInterpolatedKeyframes()
    {
        var document = BlankProjectFactory.Create();
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 40d, 60d, 0);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 2d, 220d, document.Duration);
        document.Scenes[0].Layers.Add(layer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel
        {
            UseDockWorkspace = false,
            AutoKey = false
        };
        viewModel.LoadDocument(document, "playback-keyframe-bug.timeline.json");
        viewModel.SelectLayer(layer.Id);

        var track = viewModel.SelectedLayer!.Model.Tracks.First(item => item.Property == AnimatedProperty.X);
        var beforeCount = track.Keyframes.Count;

        viewModel.IsPlaying = true;
        viewModel.Seek(1d);
        viewModel.IsPlaying = false;

        Assert.Equal(beforeCount, track.Keyframes.Count);
        Assert.DoesNotContain(track.Keyframes, item => Math.Abs(item.Time - 1d) < 0.0001d);
    }

    [Fact]
    public void MainWindowViewModel_PlaybackIgnoresInspectorEdits()
    {
        var document = BlankProjectFactory.Create();
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 40d, 60d, 0);
        document.Scenes[0].Layers.Add(layer);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "playback-ignore-inspector.timeline.json");
        viewModel.SelectLayer(layer.Id);

        var originalX = viewModel.SelectedLayer!.Model.Defaults.X;
        viewModel.IsPlaying = true;
        viewModel.InspectorX = originalX + 120d;
        viewModel.IsPlaying = false;

        var track = viewModel.SelectedLayer.Model.Tracks.First(item => item.Property == AnimatedProperty.X);
        Assert.Equal(originalX, viewModel.SelectedLayer.Model.Defaults.X);
        Assert.DoesNotContain(track.Keyframes, item => Math.Abs(item.Time - viewModel.CurrentTime) < 0.0001d && Math.Abs(item.Value - (originalX + 120d)) < 0.0001d);
    }

    [Fact]
    public void MainWindowViewModel_PlaybackDefaultsToFullSceneRange()
    {
        var document = BlankProjectFactory.Create();
        var totalFrames = FrameTimelineService.GetTotalFrames(document.Scenes[0].Duration, document.Scenes[0].FrameRate);
        SceneTimelineService.SetWorkArea(document.Scenes[0], 24, 48, totalFrames);
        document.Layers = document.Scenes[0].Layers;

        var viewModel = new MainWindowViewModel();
        viewModel.LoadDocument(document, "playback-range.timeline.json");

        Assert.False(viewModel.UseWorkAreaPlayback);

        viewModel.Seek(0d);
        viewModel.TogglePlaybackCommand.Execute(null);

        Assert.True(viewModel.IsPlaying);
        Assert.Equal(0, viewModel.CurrentFrame);

        viewModel.StopPlaybackCommand.Execute(null);
    }
}

file sealed class RecordingLayerRenderFallback : ILayerRenderFallback
{
    public int CallCount { get; private set; }

    public bool LastRenderAsMask { get; private set; }

    public RenderedFrame RenderLayer(SceneRenderRequest request, RenderableLayerSample sample, bool renderAsMask)
    {
        CallCount++;
        LastRenderAsMask = renderAsMask;
        var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
        var pixels = new byte[width * height * 4];
        pixels[2] = 255;
        pixels[3] = 255;
        return new RenderedFrame(width, height, width * 4, pixels);
    }
}
