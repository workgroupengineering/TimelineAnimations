using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Services;

public sealed class MainWindowNativeMenuFactory
{
    public NativeMenu Create(MainWindowViewModel viewModel, MainWindowNativeMenuActions actions)
    {
        var state = new NativeMenuDynamicState();
        var menu = new NativeMenu();

        menu.Items.Add(BuildFileMenu(viewModel, actions, state));
        menu.Items.Add(BuildEditMenu(viewModel, state));
        menu.Items.Add(BuildViewMenu(viewModel, actions, state));
        menu.Items.Add(BuildInsertMenu(viewModel));
        menu.Items.Add(BuildModifyMenu(viewModel, state));
        menu.Items.Add(BuildTextMenu(viewModel, state));
        menu.Items.Add(BuildCommandsMenu(viewModel));
        menu.Items.Add(BuildControlMenu(viewModel, state));
        menu.Items.Add(BuildWindowMenu(viewModel, state));
        menu.Items.Add(BuildHelpMenu(viewModel));

        menu.NeedsUpdate += (_, _) => UpdateDynamicState(viewModel, state);
        UpdateDynamicState(viewModel, state);

        return menu;
    }

    private static NativeMenuItem BuildFileMenu(
        MainWindowViewModel viewModel,
        MainWindowNativeMenuActions actions,
        NativeMenuDynamicState state)
    {
        var fileMenu = Submenu("_File");
        fileMenu.Menu!.Items.Add(CommandItem("New Blank Animation", viewModel.ResetBlankCommand));
        fileMenu.Menu!.Items.Add(CommandItem("New Sample Composition", viewModel.ResetSampleCommand));
        fileMenu.Menu.Items.Add(ActionItem("Open...", actions.OpenDocumentAsync, canExecute: actions.CanOpenDocuments));
        fileMenu.Menu.Items.Add(ActionItem("Save...", actions.SaveDocumentAsync, canExecute: actions.CanSaveDocuments));
        fileMenu.Menu.Items.Add(new NativeMenuItemSeparator());

        var flashMenu = Submenu("Flash Authoring");
        flashMenu.Menu!.Items.Add(ActionItem("Open XFL Folder...", actions.OpenFlashXflFolderAsync, canExecute: actions.CanPickFolders));
        flashMenu.Menu.Items.Add(ActionItem("Save XFL Folder...", actions.SaveFlashXflFolderAsync, canExecute: actions.CanPickFolders));
        flashMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        flashMenu.Menu.Items.Add(ActionItem("Convert FLA to XFL Folder...", actions.ConvertFlashFlaToXflFolderAsync, canExecute: () => actions.CanOpenDocuments() && actions.CanPickFolders()));
        flashMenu.Menu.Items.Add(ActionItem("Convert XFL Folder to FLA...", actions.ConvertFlashXflFolderToFlaAsync, canExecute: () => actions.CanPickFolders() && actions.CanSaveDocuments()));

        var importMenu = Submenu("Import");
        state.ImportAvalonia = RadioActionItem("Avalonia XAML...", actions.ImportAnimationFormatAsync, AnimationExchangeFormat.AvaloniaXaml);
        state.ImportFlash = RadioActionItem("Flash XFL...", actions.ImportAnimationFormatAsync, AnimationExchangeFormat.FlashXfl);
        state.ImportSvg = RadioActionItem("SVG / SMIL...", actions.ImportAnimationFormatAsync, AnimationExchangeFormat.SvgSmil);
        state.ImportHtml = RadioActionItem("HTML / CSS...", actions.ImportAnimationFormatAsync, AnimationExchangeFormat.HtmlCss);
        importMenu.Menu!.Items.Add(state.ImportAvalonia);
        importMenu.Menu.Items.Add(state.ImportFlash);
        importMenu.Menu.Items.Add(state.ImportSvg);
        importMenu.Menu.Items.Add(state.ImportHtml);
        importMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        importMenu.Menu.Items.Add(ActionItem("Audio...", actions.ImportAudioAsync, canExecute: actions.CanOpenDocuments));
        importMenu.Menu.Items.Add(ActionItem("Video Sequence...", actions.ImportVideoAsync, canExecute: actions.CanOpenDocuments));

        var exportMenu = Submenu("Export");
        state.ExportAvalonia = RadioActionItem("Avalonia XAML...", actions.ExportAnimationFormatAsync, AnimationExchangeFormat.AvaloniaXaml);
        state.ExportFlash = RadioActionItem("Flash XFL...", actions.ExportAnimationFormatAsync, AnimationExchangeFormat.FlashXfl);
        state.ExportSvg = RadioActionItem("SVG / SMIL...", actions.ExportAnimationFormatAsync, AnimationExchangeFormat.SvgSmil);
        state.ExportHtml = RadioActionItem("HTML / CSS...", actions.ExportAnimationFormatAsync, AnimationExchangeFormat.HtmlCss);
        exportMenu.Menu!.Items.Add(state.ExportAvalonia);
        exportMenu.Menu.Items.Add(state.ExportFlash);
        exportMenu.Menu.Items.Add(state.ExportSvg);
        exportMenu.Menu.Items.Add(state.ExportHtml);
        exportMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        exportMenu.Menu.Items.Add(ActionItem("Current Frame PNG...", actions.ExportFrameAsync, canExecute: actions.CanSaveDocuments));
        exportMenu.Menu.Items.Add(ActionItem("PNG Sequence...", actions.RenderSequenceAsync, canExecute: actions.CanPickFolders));
        exportMenu.Menu.Items.Add(ActionItem("Project Sequence...", actions.RenderProjectAsync, canExecute: actions.CanPickFolders));

        var publishMenu = Submenu("Publish");
        state.PreviewPublishProfile = ActionItem("Preview Selected Profile", actions.PreviewPublishProfile, canExecute: () => viewModel.CanPreviewSelectedPublishProfile);
        state.ExportPublishProfile = ActionItem("Export Selected Profile...", actions.ExportPublishProfileAsync, canExecute: () => viewModel.CanExportSelectedPublishProfile);
        publishMenu.Menu!.Items.Add(state.PreviewPublishProfile);
        publishMenu.Menu.Items.Add(state.ExportPublishProfile);

        fileMenu.Menu.Items.Add(importMenu);
        fileMenu.Menu.Items.Add(exportMenu);
        fileMenu.Menu.Items.Add(flashMenu);
        fileMenu.Menu.Items.Add(publishMenu);
        return fileMenu;
    }

    private static NativeMenuItem BuildEditMenu(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        var editMenu = Submenu("_Edit");
        editMenu.Menu!.Items.Add(CommandItem("Undo", viewModel.UndoCommand, gesture: Gesture(Key.Z, KeyModifiers.Control)));
        editMenu.Menu.Items.Add(CommandItem("Redo", viewModel.RedoCommand, gesture: Gesture(Key.Z, KeyModifiers.Control | KeyModifiers.Shift)));
        editMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        editMenu.Menu.Items.Add(CommandItem("Duplicate Selection", viewModel.DuplicateSelectionCommand));
        editMenu.Menu.Items.Add(CommandItem("Delete Selection", viewModel.DeleteSelectionCommand));
        state.ToggleVisibility = CommandItem(viewModel.SelectedLayerVisibilityLabel, viewModel.ToggleSelectedLayerVisibilityCommand);
        state.ToggleLock = CommandItem(viewModel.SelectedLayerLockLabel, viewModel.ToggleSelectedLayerLockCommand);
        editMenu.Menu.Items.Add(state.ToggleVisibility);
        editMenu.Menu.Items.Add(state.ToggleLock);
        editMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        editMenu.Menu.Items.Add(CommandItem("Add Scene", viewModel.AddSceneCommand));
        editMenu.Menu.Items.Add(CommandItem("Duplicate Scene", viewModel.DuplicateSceneCommand));
        editMenu.Menu.Items.Add(CommandItem("Delete Scene", viewModel.DeleteSceneCommand));
        editMenu.Menu.Items.Add(CommandItem("Move Scene Back", viewModel.MoveSceneBackwardCommand));
        editMenu.Menu.Items.Add(CommandItem("Move Scene Forward", viewModel.MoveSceneForwardCommand));
        return editMenu;
    }

    private static NativeMenuItem BuildViewMenu(
        MainWindowViewModel viewModel,
        MainWindowNativeMenuActions actions,
        NativeMenuDynamicState state)
    {
        var viewMenu = Submenu("_View");
        viewMenu.Menu!.Items.Add(ActionItem("Fit Stage", actions.FitCanvas));
        viewMenu.Menu.Items.Add(ActionItem("Actual Size", actions.ActualSizeCanvas));
        state.SnapToGrid = CheckActionItem("Snap To Grid", () => viewModel.SnapToGrid = !viewModel.SnapToGrid);
        state.OnionSkin = CheckActionItem("Onion Skin", () => viewModel.OnionSkinEnabled = !viewModel.OnionSkinEnabled);
        viewMenu.Menu.Items.Add(state.SnapToGrid);
        viewMenu.Menu.Items.Add(state.OnionSkin);
        viewMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        state.FramesView = RadioCommandItem("Frames View", viewModel.SetTimelineWorkspaceViewCommand, "Frames");
        state.CurvesView = RadioCommandItem("Curves View", viewModel.SetTimelineWorkspaceViewCommand, "Curves");
        viewMenu.Menu.Items.Add(state.FramesView);
        viewMenu.Menu.Items.Add(state.CurvesView);
        viewMenu.Menu.Items.Add(new NativeMenuItemSeparator());

        var focusMenu = Submenu("Workspace Focus");
        focusMenu.Menu!.Items.Add(CommandItem("Classic", viewModel.FocusWorkspaceAreaCommand, "classic", Gesture(Key.D1, KeyModifiers.Control)));
        focusMenu.Menu.Items.Add(CommandItem("Stage", viewModel.FocusWorkspaceAreaCommand, "stage", Gesture(Key.D2, KeyModifiers.Control)));
        focusMenu.Menu.Items.Add(CommandItem("Timeline", viewModel.FocusWorkspaceAreaCommand, "timeline", Gesture(Key.D3, KeyModifiers.Control)));
        focusMenu.Menu.Items.Add(CommandItem("Tools", viewModel.FocusWorkspaceAreaCommand, "tools", Gesture(Key.D4, KeyModifiers.Control)));
        focusMenu.Menu.Items.Add(CommandItem("Inspector", viewModel.FocusWorkspaceAreaCommand, "inspector", Gesture(Key.D5, KeyModifiers.Control)));

        var toolMenu = Submenu("Active Tool");
        state.SelectTool = RadioCommandItem("Select", viewModel.SelectDrawingToolCommand, "Select");
        state.SubselectTool = RadioCommandItem("Subselect", viewModel.SelectDrawingToolCommand, "Subselect");
        state.LassoTool = RadioCommandItem("Lasso", viewModel.SelectDrawingToolCommand, "Lasso");
        state.HandTool = RadioCommandItem("Hand", viewModel.SelectDrawingToolCommand, "Hand");
        state.ZoomTool = RadioCommandItem("Zoom", viewModel.SelectDrawingToolCommand, "Zoom");
        state.RotateViewTool = RadioCommandItem("Rotate View", viewModel.SelectDrawingToolCommand, "RotateView");
        state.EyedropperTool = RadioCommandItem("Eyedropper", viewModel.SelectDrawingToolCommand, "Eyedropper");
        state.PaintBucketTool = RadioCommandItem("Paint Bucket", viewModel.SelectDrawingToolCommand, "PaintBucket");
        state.InkBottleTool = RadioCommandItem("Ink Bottle", viewModel.SelectDrawingToolCommand, "InkBottle");
        state.WidthTool = RadioCommandItem("Width", viewModel.SelectDrawingToolCommand, "Width");
        state.WarpTool = RadioCommandItem("Warp", viewModel.SelectDrawingToolCommand, "Warp");
        state.RigTool = RadioCommandItem("Rig", viewModel.SelectDrawingToolCommand, "Rig");
        state.RectangleTool = RadioCommandItem("Rectangle", viewModel.SelectDrawingToolCommand, "Rectangle");
        state.EllipseTool = RadioCommandItem("Ellipse", viewModel.SelectDrawingToolCommand, "Ellipse");
        state.PolyStarTool = RadioCommandItem("PolyStar", viewModel.SelectDrawingToolCommand, "PolyStar");
        state.TextTool = RadioCommandItem("Text", viewModel.SelectDrawingToolCommand, "Text");
        state.LineTool = RadioCommandItem("Line", viewModel.SelectDrawingToolCommand, "Line");
        state.PenTool = RadioCommandItem("Pen", viewModel.SelectDrawingToolCommand, "Pen");
        state.BrushTool = RadioCommandItem("Brush", viewModel.SelectDrawingToolCommand, "Brush");
        state.PencilTool = RadioCommandItem("Pencil", viewModel.SelectDrawingToolCommand, "Pencil");
        state.EraserTool = RadioCommandItem("Eraser", viewModel.SelectDrawingToolCommand, "Eraser");
        toolMenu.Menu!.Items.Add(state.SelectTool);
        toolMenu.Menu.Items.Add(state.SubselectTool);
        toolMenu.Menu.Items.Add(state.LassoTool);
        toolMenu.Menu.Items.Add(state.HandTool);
        toolMenu.Menu.Items.Add(state.ZoomTool);
        toolMenu.Menu.Items.Add(state.RotateViewTool);
        toolMenu.Menu.Items.Add(state.EyedropperTool);
        toolMenu.Menu.Items.Add(state.PaintBucketTool);
        toolMenu.Menu.Items.Add(state.InkBottleTool);
        toolMenu.Menu.Items.Add(state.WidthTool);
        toolMenu.Menu.Items.Add(state.WarpTool);
        toolMenu.Menu.Items.Add(state.RigTool);
        toolMenu.Menu.Items.Add(state.RectangleTool);
        toolMenu.Menu.Items.Add(state.EllipseTool);
        toolMenu.Menu.Items.Add(state.PolyStarTool);
        toolMenu.Menu.Items.Add(state.TextTool);
        toolMenu.Menu.Items.Add(state.LineTool);
        toolMenu.Menu.Items.Add(state.PenTool);
        toolMenu.Menu.Items.Add(state.BrushTool);
        toolMenu.Menu.Items.Add(state.PencilTool);
        toolMenu.Menu.Items.Add(state.EraserTool);

        viewMenu.Menu.Items.Add(focusMenu);
        viewMenu.Menu.Items.Add(toolMenu);
        return viewMenu;
    }

    private static NativeMenuItem BuildInsertMenu(MainWindowViewModel viewModel)
    {
        var insertMenu = Submenu("_Insert");
        insertMenu.Menu!.Items.Add(CommandItem("Rectangle", viewModel.AddRectangleCommand));
        insertMenu.Menu.Items.Add(CommandItem("Ellipse", viewModel.AddEllipseCommand));
        insertMenu.Menu.Items.Add(CommandItem("Text", viewModel.AddTextCommand));
        insertMenu.Menu.Items.Add(CommandItem("Camera Layer", viewModel.AddCameraLayerCommand));

        var controlMenu = Submenu("Avalonia Control");
        foreach (var controlKind in new[]
                 {
                     "Border", "Button", "TextBlock", "TextBox", "CheckBox", "ToggleButton",
                     "Slider", "ProgressBar", "ComboBox", "ListBox", "TabControl", "Grid",
                     "StackPanel", "PathIcon", "Image", "Panel"
                 })
        {
            controlMenu.Menu!.Items.Add(CommandItem(controlKind, viewModel.InsertAvaloniaControlCommand, controlKind));
        }

        insertMenu.Menu.Items.Add(controlMenu);
        insertMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        insertMenu.Menu.Items.Add(CommandItem("Selected Library Item", viewModel.InsertSelectedLibraryItemCommand));
        insertMenu.Menu.Items.Add(CommandItem("Selected Component", viewModel.InsertSelectedComponentCommand));
        insertMenu.Menu.Items.Add(CommandItem("Selected Media Asset", viewModel.InsertSelectedMediaAssetCommand));
        return insertMenu;
    }

    private static NativeMenuItem BuildModifyMenu(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        var modifyMenu = Submenu("Modify");
        modifyMenu.Menu!.Items.Add(CommandItem("Align Left", viewModel.AlignLeftCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Align Center", viewModel.AlignCenterCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Align Right", viewModel.AlignRightCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Align Top", viewModel.AlignTopCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Align Middle", viewModel.AlignMiddleCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Align Bottom", viewModel.AlignBottomCommand));
        modifyMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        modifyMenu.Menu.Items.Add(CommandItem("Add Keyframe", viewModel.AddCurrentKeyframeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Delete Keyframe", viewModel.DeleteCurrentKeyframeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Add Folder", viewModel.AddFolderCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Group Into Folder", viewModel.GroupSelectionIntoFolderCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Move To Root", viewModel.RemoveSelectionFromFolderCommand));
        state.ToggleMute = CommandItem(viewModel.SelectedLayerMuteLabel, viewModel.ToggleSelectedLayerMuteCommand);
        state.ToggleSolo = CommandItem(viewModel.SelectedLayerSoloLabel, viewModel.ToggleSelectedLayerSoloCommand);
        modifyMenu.Menu.Items.Add(state.ToggleMute);
        modifyMenu.Menu.Items.Add(state.ToggleSolo);
        modifyMenu.Menu.Items.Add(CommandItem("Add Cel", viewModel.AddFrameKeyframeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Add Blank Cel", viewModel.AddBlankFrameKeyframeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Extend Hold", viewModel.ExtendFrameHoldCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Copy Frame Range", viewModel.CopyFrameRangeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Paste Frame Range", viewModel.PasteFrameRangeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Razor Split", viewModel.RazorSelectionCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Slip Earlier", viewModel.ApplyEditorialOperationCommand, "slip-back"));
        modifyMenu.Menu.Items.Add(CommandItem("Slip Later", viewModel.ApplyEditorialOperationCommand, "slip-forward"));
        modifyMenu.Menu.Items.Add(CommandItem("Slide Earlier", viewModel.ApplyEditorialOperationCommand, "slide-back"));
        modifyMenu.Menu.Items.Add(CommandItem("Slide Later", viewModel.ApplyEditorialOperationCommand, "slide-forward"));
        modifyMenu.Menu.Items.Add(CommandItem("Ripple Shorter", viewModel.ApplyEditorialOperationCommand, "ripple-shorter"));
        modifyMenu.Menu.Items.Add(CommandItem("Ripple Longer", viewModel.ApplyEditorialOperationCommand, "ripple-longer"));
        modifyMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        modifyMenu.Menu.Items.Add(CommandItem("Capture Shape Key", viewModel.CaptureShapeKeyframeCommand));
        modifyMenu.Menu.Items.Add(CommandItem("Delete Shape Key", viewModel.DeleteCurrentShapeKeyframeCommand));

        var canvasPresetMenu = Submenu("Canvas Preset");
        canvasPresetMenu.Menu!.Items.Add(CommandItem("HD 1280 × 720", viewModel.ApplyCanvasPresetCommand, "hd"));
        canvasPresetMenu.Menu.Items.Add(CommandItem("Full HD 1920 × 1080", viewModel.ApplyCanvasPresetCommand, "fhd"));
        canvasPresetMenu.Menu.Items.Add(CommandItem("Square 1080 × 1080", viewModel.ApplyCanvasPresetCommand, "square"));
        canvasPresetMenu.Menu.Items.Add(CommandItem("Story 1080 × 1920", viewModel.ApplyCanvasPresetCommand, "story"));
        modifyMenu.Menu.Items.Add(canvasPresetMenu);

        return modifyMenu;
    }

    private static NativeMenuItem BuildTextMenu(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        var textMenu = Submenu("_Text");
        textMenu.Menu!.Items.Add(CommandItem("Add Text Layer", viewModel.AddTextCommand));
        state.TextToolMenuItem = RadioCommandItem("Select Text Tool", viewModel.SelectDrawingToolCommand, "Text");
        textMenu.Menu.Items.Add(state.TextToolMenuItem);
        textMenu.Menu.Items.Add(CommandItem("Increase Font Size", viewModel.IncreaseTextSizeCommand));
        textMenu.Menu.Items.Add(CommandItem("Decrease Font Size", viewModel.DecreaseTextSizeCommand));
        return textMenu;
    }

    private static NativeMenuItem BuildCommandsMenu(MainWindowViewModel viewModel)
    {
        var commandsMenu = Submenu("_Commands");
        commandsMenu.Menu!.Items.Add(CommandItem("Create Symbol From Selection", viewModel.CreateSymbolFromSelectionCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Create Component From Selection", viewModel.CreateComponentFromSelectionCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Enter Symbol Edit", viewModel.EnterSelectedSymbolEditCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Exit Symbol Edit", viewModel.ExitSymbolEditCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Update Linked Symbol", viewModel.UpdateLinkedSymbolFromSelectionCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Relink Selection To Selected Library Item", viewModel.RelinkSelectionToSelectedLibraryItemCommand));
        commandsMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        commandsMenu.Menu.Items.Add(CommandItem("Add Behavior", viewModel.AddBehaviorCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Delete Selected Behavior", viewModel.DeleteSelectedBehaviorCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Save Frame Label", viewModel.AddOrUpdateFrameLabelCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Clear Frame Label", viewModel.RemoveFrameLabelCommand));
        commandsMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        commandsMenu.Menu.Items.Add(CommandItem("Add Publish Profile", viewModel.AddPublishProfileCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Duplicate Publish Profile", viewModel.DuplicatePublishProfileCommand));
        commandsMenu.Menu.Items.Add(CommandItem("Delete Publish Profile", viewModel.DeletePublishProfileCommand));
        return commandsMenu;
    }

    private static NativeMenuItem BuildControlMenu(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        var controlMenu = Submenu("C_ontrol");
        controlMenu.Menu!.Items.Add(CommandItem("Play / Pause", viewModel.TogglePlaybackCommand));
        controlMenu.Menu.Items.Add(CommandItem("Stop", viewModel.StopPlaybackCommand));
        state.PrototypeMode = CheckCommandItem("Prototype Mode", viewModel.TogglePrototypeModeCommand);
        state.LoopPlayback = CheckActionItem("Loop Playback", () => viewModel.LoopPlayback = !viewModel.LoopPlayback);
        state.PlayAllScenes = CheckActionItem("Play All Scenes", () => viewModel.PlayAllScenes = !viewModel.PlayAllScenes);
        state.AutoKey = CheckActionItem("Auto Key", () => viewModel.AutoKey = !viewModel.AutoKey);
        controlMenu.Menu.Items.Add(state.PrototypeMode);
        controlMenu.Menu.Items.Add(new NativeMenuItemSeparator());
        controlMenu.Menu.Items.Add(state.LoopPlayback);
        controlMenu.Menu.Items.Add(state.PlayAllScenes);
        controlMenu.Menu.Items.Add(state.AutoKey);
        return controlMenu;
    }

    private static NativeMenuItem BuildWindowMenu(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        var windowMenu = Submenu("_Window");
        windowMenu.Menu!.Items.Add(CommandItem("Restore Workspace", viewModel.RestoreSavedWorkspaceLayoutCommand));

        var panelsMenu = Submenu("Show / Hide Panels");
        state.ToggleLeftPanel = CommandItem(viewModel.LeftPanelToggleLabel, viewModel.ToggleWorkspacePanelCommand, "left", Gesture(Key.D1, KeyModifiers.Control | KeyModifiers.Shift));
        state.ToggleRightPanel = CommandItem(viewModel.RightPanelToggleLabel, viewModel.ToggleWorkspacePanelCommand, "right", Gesture(Key.D2, KeyModifiers.Control | KeyModifiers.Shift));
        state.ToggleTimelinePanel = CommandItem(viewModel.TimelinePanelToggleLabel, viewModel.ToggleWorkspacePanelCommand, "timeline", Gesture(Key.D3, KeyModifiers.Control | KeyModifiers.Shift));
        panelsMenu.Menu!.Items.Add(state.ToggleLeftPanel);
        panelsMenu.Menu.Items.Add(state.ToggleRightPanel);
        panelsMenu.Menu.Items.Add(state.ToggleTimelinePanel);

        var panelModesMenu = Submenu("Panel Modes");
        panelModesMenu.Menu!.Items.Add(PanelModeSubmenu("Tools Dock", viewModel.SetWorkspacePanelModeCommand, "left"));
        panelModesMenu.Menu.Items.Add(PanelModeSubmenu("Inspector Dock", viewModel.SetWorkspacePanelModeCommand, "right"));
        panelModesMenu.Menu.Items.Add(PanelModeSubmenu("Timeline Dock", viewModel.SetWorkspacePanelModeCommand, "timeline"));

        windowMenu.Menu.Items.Add(panelsMenu);
        windowMenu.Menu.Items.Add(panelModesMenu);
        return windowMenu;
    }

    private static NativeMenuItem BuildHelpMenu(MainWindowViewModel viewModel)
    {
        var helpMenu = Submenu("_Help");
        helpMenu.Menu!.Items.Add(CommandItem("Show Current Context", viewModel.ShowWorkspaceHelpCommand));
        helpMenu.Menu.Items.Add(CommandItem("Show Selection Context", viewModel.ShowSelectionContextCommand));
        helpMenu.Menu.Items.Add(CommandItem("Show Keyboard Shortcuts", viewModel.ShowKeyboardShortcutsCommand));
        return helpMenu;
    }

    private static void UpdateDynamicState(MainWindowViewModel viewModel, NativeMenuDynamicState state)
    {
        SetChecked(state.ImportAvalonia, viewModel.IsAvaloniaXamlExchangeSelected);
        SetChecked(state.ImportFlash, viewModel.IsFlashXflExchangeSelected);
        SetChecked(state.ImportSvg, viewModel.IsSvgAnimationExchangeSelected);
        SetChecked(state.ImportHtml, viewModel.IsHtmlAnimationExchangeSelected);
        SetChecked(state.ExportAvalonia, viewModel.IsAvaloniaXamlExchangeSelected);
        SetChecked(state.ExportFlash, viewModel.IsFlashXflExchangeSelected);
        SetChecked(state.ExportSvg, viewModel.IsSvgAnimationExchangeSelected);
        SetChecked(state.ExportHtml, viewModel.IsHtmlAnimationExchangeSelected);
        SetChecked(state.SnapToGrid, viewModel.SnapToGrid);
        SetChecked(state.OnionSkin, viewModel.OnionSkinEnabled);
        SetChecked(state.FramesView, viewModel.IsFramesTimelineViewActive);
        SetChecked(state.CurvesView, viewModel.IsCurvesTimelineViewActive);
        SetChecked(state.SelectTool, viewModel.IsSelectToolActive);
        SetChecked(state.SubselectTool, viewModel.IsSubselectToolActive);
        SetChecked(state.LassoTool, viewModel.IsLassoToolActive);
        SetChecked(state.HandTool, viewModel.IsHandToolActive);
        SetChecked(state.ZoomTool, viewModel.IsZoomToolActive);
        SetChecked(state.RotateViewTool, viewModel.IsRotateViewToolActive);
        SetChecked(state.EyedropperTool, viewModel.IsEyedropperToolActive);
        SetChecked(state.PaintBucketTool, viewModel.IsPaintBucketToolActive);
        SetChecked(state.InkBottleTool, viewModel.IsInkBottleToolActive);
        SetChecked(state.WidthTool, viewModel.IsWidthToolActive);
        SetChecked(state.WarpTool, viewModel.IsWarpToolActive);
        SetChecked(state.RigTool, viewModel.IsRigToolActive);
        SetChecked(state.RectangleTool, viewModel.IsRectangleToolActive);
        SetChecked(state.EllipseTool, viewModel.IsEllipseToolActive);
        SetChecked(state.PolyStarTool, viewModel.IsPolyStarToolActive);
        SetChecked(state.TextTool, viewModel.IsTextToolActive);
        SetChecked(state.LineTool, viewModel.IsLineToolActive);
        SetChecked(state.PenTool, viewModel.IsPenToolActive);
        SetChecked(state.BrushTool, viewModel.IsBrushToolActive);
        SetChecked(state.PencilTool, viewModel.IsPencilToolActive);
        SetChecked(state.EraserTool, viewModel.IsEraserToolActive);
        SetChecked(state.TextToolMenuItem, viewModel.IsTextToolActive);
        SetChecked(state.PrototypeMode, viewModel.IsPrototypeMode);
        SetChecked(state.LoopPlayback, viewModel.LoopPlayback);
        SetChecked(state.PlayAllScenes, viewModel.PlayAllScenes);
        SetChecked(state.AutoKey, viewModel.AutoKey);

        SetHeader(state.ToggleVisibility, viewModel.SelectedLayerVisibilityLabel);
        SetHeader(state.ToggleLock, viewModel.SelectedLayerLockLabel);
        SetHeader(state.ToggleMute, viewModel.SelectedLayerMuteLabel);
        SetHeader(state.ToggleSolo, viewModel.SelectedLayerSoloLabel);
        SetHeader(state.ToggleLeftPanel, viewModel.LeftPanelToggleLabel);
        SetHeader(state.ToggleRightPanel, viewModel.RightPanelToggleLabel);
        SetHeader(state.ToggleTimelinePanel, viewModel.TimelinePanelToggleLabel);

        if (state.PreviewPublishProfile is not null)
        {
            state.PreviewPublishProfile.IsEnabled = viewModel.CanPreviewSelectedPublishProfile;
        }

        if (state.ExportPublishProfile is not null)
        {
            state.ExportPublishProfile.IsEnabled = viewModel.CanExportSelectedPublishProfile;
        }
    }

    private static NativeMenuItem Submenu(string header) => new(header) { Menu = new NativeMenu() };

    private static NativeMenuItem PanelModeSubmenu(string header, ICommand command, string side)
    {
        var submenu = Submenu(header);
        submenu.Menu!.Items.Add(CommandItem("Dock", command, $"{side}:dock"));
        submenu.Menu.Items.Add(CommandItem("Overlay", command, $"{side}:over"));
        submenu.Menu.Items.Add(CommandItem("Hide", command, $"{side}:hide"));
        return submenu;
    }

    private static NativeMenuItem CommandItem(string header, ICommand command, object? parameter = null, KeyGesture? gesture = null)
    {
        return new NativeMenuItem(header)
        {
            Command = command,
            CommandParameter = parameter,
            Gesture = gesture
        };
    }

    private static NativeMenuItem RadioCommandItem(string header, ICommand command, object? parameter = null)
    {
        return new NativeMenuItem(header)
        {
            Command = command,
            CommandParameter = parameter,
            ToggleType = NativeMenuItemToggleType.Radio
        };
    }

    private static NativeMenuItem CheckCommandItem(string header, ICommand command, object? parameter = null)
    {
        return new NativeMenuItem(header)
        {
            Command = command,
            CommandParameter = parameter,
            ToggleType = NativeMenuItemToggleType.CheckBox
        };
    }

    private static NativeMenuItem ActionItem(string header, Action action, Func<bool>? canExecute = null)
    {
        var item = new NativeMenuItem(header)
        {
            IsEnabled = canExecute?.Invoke() ?? true
        };
        item.Click += (_, _) =>
        {
            if (canExecute?.Invoke() == false)
            {
                return;
            }

            action();
        };
        return item;
    }

    private static NativeMenuItem ActionItem(string header, Func<Task> action, Func<bool>? canExecute = null)
    {
        var item = new NativeMenuItem(header)
        {
            IsEnabled = canExecute?.Invoke() ?? true
        };
        item.Click += async (_, _) =>
        {
            if (canExecute?.Invoke() == false)
            {
                return;
            }

            await action();
        };
        return item;
    }

    private static NativeMenuItem RadioActionItem(string header, Func<AnimationExchangeFormat, Task> action, AnimationExchangeFormat format)
    {
        var item = new NativeMenuItem(header)
        {
            ToggleType = NativeMenuItemToggleType.Radio
        };
        item.Click += async (_, _) => await action(format);
        return item;
    }

    private static NativeMenuItem CheckActionItem(string header, Action action)
    {
        var item = new NativeMenuItem(header)
        {
            ToggleType = NativeMenuItemToggleType.CheckBox
        };
        item.Click += (_, _) => action();
        return item;
    }

    private static KeyGesture Gesture(Key key, KeyModifiers modifiers) => new(key, modifiers);

    private static void SetChecked(NativeMenuItem? item, bool isChecked)
    {
        if (item is not null)
        {
            item.IsChecked = isChecked;
        }
    }

    private static void SetHeader(NativeMenuItem? item, string header)
    {
        if (item is not null)
        {
            item.Header = header;
        }
    }

    private sealed class NativeMenuDynamicState
    {
        public NativeMenuItem? ImportAvalonia { get; set; }
        public NativeMenuItem? ImportFlash { get; set; }
        public NativeMenuItem? ImportSvg { get; set; }
        public NativeMenuItem? ImportHtml { get; set; }
        public NativeMenuItem? ExportAvalonia { get; set; }
        public NativeMenuItem? ExportFlash { get; set; }
        public NativeMenuItem? ExportSvg { get; set; }
        public NativeMenuItem? ExportHtml { get; set; }
        public NativeMenuItem? PreviewPublishProfile { get; set; }
        public NativeMenuItem? ExportPublishProfile { get; set; }
        public NativeMenuItem? ToggleVisibility { get; set; }
        public NativeMenuItem? ToggleLock { get; set; }
        public NativeMenuItem? SnapToGrid { get; set; }
        public NativeMenuItem? OnionSkin { get; set; }
        public NativeMenuItem? FramesView { get; set; }
        public NativeMenuItem? CurvesView { get; set; }
        public NativeMenuItem? SelectTool { get; set; }
        public NativeMenuItem? SubselectTool { get; set; }
        public NativeMenuItem? LassoTool { get; set; }
        public NativeMenuItem? HandTool { get; set; }
        public NativeMenuItem? ZoomTool { get; set; }
        public NativeMenuItem? RotateViewTool { get; set; }
        public NativeMenuItem? EyedropperTool { get; set; }
        public NativeMenuItem? PaintBucketTool { get; set; }
        public NativeMenuItem? InkBottleTool { get; set; }
        public NativeMenuItem? WidthTool { get; set; }
        public NativeMenuItem? WarpTool { get; set; }
        public NativeMenuItem? RigTool { get; set; }
        public NativeMenuItem? RectangleTool { get; set; }
        public NativeMenuItem? EllipseTool { get; set; }
        public NativeMenuItem? PolyStarTool { get; set; }
        public NativeMenuItem? TextTool { get; set; }
        public NativeMenuItem? LineTool { get; set; }
        public NativeMenuItem? PenTool { get; set; }
        public NativeMenuItem? BrushTool { get; set; }
        public NativeMenuItem? PencilTool { get; set; }
        public NativeMenuItem? EraserTool { get; set; }
        public NativeMenuItem? ToggleMute { get; set; }
        public NativeMenuItem? ToggleSolo { get; set; }
        public NativeMenuItem? TextToolMenuItem { get; set; }
        public NativeMenuItem? PrototypeMode { get; set; }
        public NativeMenuItem? LoopPlayback { get; set; }
        public NativeMenuItem? PlayAllScenes { get; set; }
        public NativeMenuItem? AutoKey { get; set; }
        public NativeMenuItem? ToggleLeftPanel { get; set; }
        public NativeMenuItem? ToggleRightPanel { get; set; }
        public NativeMenuItem? ToggleTimelinePanel { get; set; }
    }
}

public sealed record MainWindowNativeMenuActions(
    Func<Task> OpenDocumentAsync,
    Func<Task> SaveDocumentAsync,
    Func<Task> OpenFlashXflFolderAsync,
    Func<Task> SaveFlashXflFolderAsync,
    Func<Task> ConvertFlashFlaToXflFolderAsync,
    Func<Task> ConvertFlashXflFolderToFlaAsync,
    Func<AnimationExchangeFormat, Task> ImportAnimationFormatAsync,
    Func<AnimationExchangeFormat, Task> ExportAnimationFormatAsync,
    Func<Task> ImportAudioAsync,
    Func<Task> ImportVideoAsync,
    Action PreviewPublishProfile,
    Func<Task> ExportPublishProfileAsync,
    Func<Task> ExportFrameAsync,
    Func<Task> RenderSequenceAsync,
    Func<Task> RenderProjectAsync,
    Action FitCanvas,
    Action ActualSizeCanvas,
    Func<bool> CanOpenDocuments,
    Func<bool> CanSaveDocuments,
    Func<bool> CanPickFolders);
