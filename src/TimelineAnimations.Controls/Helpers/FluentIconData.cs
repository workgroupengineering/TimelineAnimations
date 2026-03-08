using Avalonia.Media;

namespace TimelineAnimations.App.Helpers;

/// <summary>
/// Microsoft Fluent System Icons path data reused by authoring chrome.
/// </summary>
public static class FluentIconData
{
    public const string OpenFolder20RegularPath =
        "M3 5.5v6.6l1.5-2.6A3 3 0 0 1 7.1 8H15v-.5c0-.83-.67-1.5-1.5-1.5h-4a.5.5 0 0 1-.35-.15l-1.71-1.7A.5.5 0 0 0 7.09 4H4.5C3.67 4 3 4.67 3 5.5Zm1.28 10.48.22.02h9.4a2 2 0 0 0 1.73-1l2.17-3.75A1.5 1.5 0 0 0 16.5 9H7.1a2 2 0 0 0-1.73 1L3.2 13.75a1.5 1.5 0 0 0 1.08 2.23ZM2 14.46V5.5A2.5 2.5 0 0 1 4.5 3h2.59c.4 0 .78.16 1.06.44L9.7 5h3.79A2.5 2.5 0 0 1 16 7.5V8h.5a2.5 2.5 0 0 1 2.16 3.75L16.5 15.5a3 3 0 0 1-2.6 1.5H4.5a2.54 2.54 0 0 1-1.62-.6A2.5 2.5 0 0 1 2 14.46Z";

    public const string Save20RegularPath =
        "M3 5c0-1.1.9-2 2-2h8.38a2 2 0 0 1 1.41.59l1.62 1.62A2 2 0 0 1 17 6.62V15a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5Zm2-1a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1v-4.5c0-.83.67-1.5 1.5-1.5h7c.83 0 1.5.67 1.5 1.5V16a1 1 0 0 0 1-1V6.62a1 1 0 0 0-.3-.7L14.1 4.28a1 1 0 0 0-.71-.29H13v2.5c0 .83-.67 1.5-1.5 1.5h-4A1.5 1.5 0 0 1 6 6.5V4H5Zm2 0v2.5c0 .28.22.5.5.5h4a.5.5 0 0 0 .5-.5V4H7Zm7 12v-4.5a.5.5 0 0 0-.5-.5h-7a.5.5 0 0 0-.5.5V16h8Z";

    public const string Undo20RegularPath =
        "M5 2.5a.5.5 0 0 0-1 0v4.9c0 .33.27.6.6.6h4.9a.5.5 0 0 0 0-1H5.9l3.48-3.02a4 4 0 0 1 5.25 6.04l-8.17 7.1a.5.5 0 0 0 .65.76l8.17-7.1a5 5 0 0 0-6.56-7.55L5 6.46V2.5Z";

    public const string Redo20RegularPath =
        "M15 2.5a.5.5 0 0 1 1 0v4.9a.6.6 0 0 1-.6.6h-4.9a.5.5 0 0 1 0-1h3.6l-3.48-3.02a4 4 0 1 0-5.24 6.04l8.17 7.1a.5.5 0 1 1-.66.76l-8.17-7.1a5 5 0 1 1 6.56-7.55L15 6.46V2.5Z";

    public const string Play20RegularPath =
        "M17.22 8.69a1.5 1.5 0 0 1 0 2.62l-10 5.5A1.5 1.5 0 0 1 5 15.5v-11A1.5 1.5 0 0 1 7.22 3.2l10 5.5Zm-.48 1.75a.5.5 0 0 0 0-.88l-10-5.5A.5.5 0 0 0 6 4.5v11c0 .38.4.62.74.44l10-5.5Z";

    public const string Stop20RegularPath =
        "M15.5 4c.28 0 .5.22.5.5v11a.5.5 0 0 1-.5.5h-11a.5.5 0 0 1-.5-.5v-11c0-.28.22-.5.5-.5h11Zm-11-1C3.67 3 3 3.67 3 4.5v11c0 .83.67 1.5 1.5 1.5h11c.83 0 1.5-.67 1.5-1.5v-11c0-.83-.67-1.5-1.5-1.5h-11Z";

    public const string Edit20RegularPath =
        "M17.18 2.93a2.97 2.97 0 0 0-4.26-.06l-9.37 9.38c-.33.32-.56.74-.66 1.2l-.88 3.94a.5.5 0 0 0 .6.6l3.92-.88c.47-.1.9-.33 1.24-.67l7.98-7.98.34.33a1 1 0 0 1 0 1.42l-.94.94a.5.5 0 0 0 .7.7l.94-.94a2 2 0 0 0 0-2.82l-.33-.34.67-.67a2.97 2.97 0 0 0 .05-4.15Zm-3.55.65a1.97 1.97 0 0 1 2.79 2.8l-9.36 9.35c-.2.2-.46.35-.74.4l-3.16.71.7-3.18c.07-.27.2-.51.4-.7l9.37-9.38Z";

    public const string Target20RegularPath =
        "M10 11.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3ZM5 10a5 5 0 1 1 10 0 5 5 0 0 1-10 0Zm5-4a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm-8 4a8 8 0 1 1 16 0 8 8 0 0 1-16 0Zm8-7a7 7 0 1 0 0 14 7 7 0 0 0 0-14Z";

    public const string Eye20RegularPath =
        "M3.26 11.6A6.97 6.97 0 0 1 10 6c3.2 0 6.06 2.33 6.74 5.6a.5.5 0 0 0 .98-.2A7.97 7.97 0 0 0 10 5a7.97 7.97 0 0 0-7.72 6.4.5.5 0 0 0 .98.2ZM10 8a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7Zm-2.5 3.5a2.5 2.5 0 1 1 5 0 2.5 2.5 0 0 1-5 0Z";

    public const string ArrowClockwise20RegularPath =
        "M4 10a6 6 0 0 1 10.47-4H12.5a.5.5 0 0 0 0 1h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-1 0v1.6a7 7 0 1 0 1.98 4.36.5.5 0 1 0-1 .08L16 10a6 6 0 0 1-12 0Z";

    public const string Cursor20RegularPath =
        "M5 3.06a1 1 0 0 1 1.64-.77l11 9.06a1 1 0 0 1-.63 1.77h-5.6c-.43 0-.85.19-1.13.52L6.76 17.7A1 1 0 0 1 5 17.06v-14Zm12 9.06L6 3.06v14l3.52-4.08a2.5 2.5 0 0 1 1.9-.86H17Z";

    public const string CursorClick20RegularPath =
        "M7.5 2c.28 0 .5.22.5.5v2a.5.5 0 0 1-1 0v-2c0-.28.22-.5.5-.5ZM3.61 3.61c.2-.2.51-.2.7 0l1.42 1.42a.5.5 0 1 1-.7.7L3.6 4.32a.5.5 0 0 1 0-.7Zm7.78 0c.2.2.2.51 0 .7L9.97 5.74a.5.5 0 1 1-.7-.7l1.41-1.42c.2-.2.51-.2.7 0ZM2 7.5c0-.28.22-.5.5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5Zm6.64-.2A1 1 0 0 0 7 8.07v9.1a1 1 0 0 0 1.75.66l2.03-2.32c.28-.32.7-.51 1.13-.51h3.2a1 1 0 0 0 .65-1.77L8.64 7.3ZM8 17.17v-9.1L15.12 14H11.9c-.72 0-1.4.31-1.88.85L8 17.17Z";

    public const string Lasso20RegularPath =
        "M8.16 2.21a8.02 8.02 0 0 1 3.68 0 .5.5 0 0 1-.23.98 7.02 7.02 0 0 0-3.22 0 .5.5 0 0 1-.23-.98ZM6.48 3.36a.5.5 0 0 1-.16.68 7.04 7.04 0 0 0-2.28 2.28.5.5 0 1 1-.85-.53 8.04 8.04 0 0 1 2.6-2.6.5.5 0 0 1 .7.17Zm7.04 0a.5.5 0 0 1 .69-.17 8.04 8.04 0 0 1 2.6 2.6.5.5 0 0 1-.85.53 7.04 7.04 0 0 0-2.28-2.28.5.5 0 0 1-.16-.68ZM2.82 7.79a.5.5 0 0 1 .37.6 7.02 7.02 0 0 0 0 3.22.5.5 0 0 1-.98.23 8.02 8.02 0 0 1 0-3.68.5.5 0 0 1 .6-.37Zm14.37 0a.5.5 0 0 1 .6.37 8.03 8.03 0 0 1 0 3.68.5.5 0 0 1-.98-.23 7.02 7.02 0 0 0 0-3.22.5.5 0 0 1 .38-.6ZM3.36 13.52a.5.5 0 0 1 .68.16c.58.92 1.36 1.7 2.28 2.28a.5.5 0 1 1-.53.85 8.04 8.04 0 0 1-2.6-2.6.5.5 0 0 1 .17-.7Zm13.57.73a.5.5 0 1 0-.86-.5l-.02.03a3.6 3.6 0 0 1-.32.46 7.8 7.8 0 0 1-1.16 1.22A6.55 6.55 0 0 0 10.5 14c-1.52 0-2.49.9-2.49 2s.97 2 2.49 2a7.1 7.1 0 0 0 4.03-1.26 8.6 8.6 0 0 1 1.5 1.95l.02.03a.5.5 0 1 0 .9-.44s-.13-.24 0 0l-.01-.02a3.37 3.37 0 0 0-.1-.18 9.6 9.6 0 0 0-1.49-1.93l-.02-.03a8.8 8.8 0 0 0 1.6-1.86.9.9 0 0 1 0-.01ZM10.5 15c1.3 0 2.38.46 3.23 1.07-.85.53-1.93.93-3.23.93-1.13 0-1.49-.6-1.49-1s.36-1 1.49-1Z";

    public const string HandLeft20RegularPath =
        "M16 12.02c0 1.06-.2 2.1-.6 3.08l-.6 1.42a2.55 2.55 0 0 1-1.17 1.29c-.27.14-.56.21-.86.21h-2.55c-.77 0-1.49-.41-1.87-1.08-.5-.87-1.02-1.7-1.72-2.43l-1.32-1.39c-.44-.46-.97-.84-1.49-1.23l-.59-.45a.6.6 0 0 1-.23-.47c0-.75.54-1.57 1.22-1.79A3.34 3.34 0 0 1 7 9.47V4.5a1.5 1.5 0 0 1 2.05-1.4 1.5 1.5 0 0 1 2.9 0A1.5 1.5 0 0 1 14 4.5v.09A1.5 1.5 0 0 1 16 6v6.02ZM12 4.5v4a.5.5 0 0 1-1 0v-5a.5.5 0 0 0-1 0v5a.5.5 0 0 1-1 0v-4a.5.5 0 0 0-1 0v6a.5.5 0 0 1-.85.37h-.01c-.22-.22-.44-.44-.72-.58-.7-.35-2.22-.57-2.4.5l.53.4c.52.4 1.04.78 1.48 1.24l1.33 1.38c.75.79 1.31 1.7 1.85 2.63.21.36.6.58 1.01.58h2.55c.13 0 .27-.03.4-.1.32-.17.57-.44.71-.78l.59-1.42c.35-.86.53-1.78.53-2.7V6a.5.5 0 0 0-1 0v3.5a.5.5 0 0 1-1 0v-5a.5.5 0 0 0-1 0Z";

    public const string ZoomIn20RegularPath =
        "M8.5 5.5c.28 0 .5.22.5.5v2h2a.5.5 0 0 1 0 1H9v2a.5.5 0 0 1-1 0V9H6a.5.5 0 0 1 0-1h2V6c0-.28.22-.5.5-.5Zm0-3.5a6.5 6.5 0 0 1 4.94 10.73l3.41 3.42a.5.5 0 0 1-.63.76l-.07-.06-3.42-3.41A6.5 6.5 0 1 1 8.5 2Zm0 1a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11Z";

    public const string Eyedropper20RegularPath =
        "M17.25 2.75a2.62 2.62 0 0 0-3.71 0L12.5 3.8l-.35-.35a1.5 1.5 0 0 0-2.12 0l-.59.59a1.5 1.5 0 0 0 0 2.12l.35.35-6.35 6.35A1.5 1.5 0 0 0 3 13.91v.5l-.96 2.26a1 1 0 0 0 1.32 1.31L5.6 17h.49c.4 0 .78-.16 1.06-.44l6.35-6.35.35.35a1.5 1.5 0 0 0 2.12 0l.59-.58a1.5 1.5 0 0 0 0-2.13l-.35-.35 1.04-1.04a2.62 2.62 0 0 0 0-3.7Zm-3 .71a1.62 1.62 0 0 1 2.29 2.3l-1.4 1.39a.5.5 0 0 0 0 .7l.71.71c.2.2.2.51 0 .7l-.58.6a.5.5 0 0 1-.71 0l-4.41-4.42a.5.5 0 0 1 0-.7l.58-.59c.2-.2.52-.2.71 0l.7.7a.5.5 0 0 0 .71 0l1.4-1.39ZM12.79 9.5l-6.35 6.35a.5.5 0 0 1-.35.15H5.5a.5.5 0 0 0-.2.04l-2.34 1.03 1-2.36a.5.5 0 0 0 .04-.2v-.6a.5.5 0 0 1 .15-.35l6.35-6.35 2.3 2.3Z";

    public const string PaintBucket20RegularPath =
        "M9 2.5a.5.5 0 0 0-1 0V4c-.2.07-.4.19-.56.35L2.35 9.44a1.5 1.5 0 0 0 0 2.12L6.7 15.9a1.5 1.5 0 0 0 2.12 0l5.09-5.09a1.5 1.5 0 0 0 0-2.12L9.56 4.35A1.5 1.5 0 0 0 9 4V2.5ZM8 5.2v1.3a.5.5 0 0 0 1 0V5.2l4.19 4.2a.5.5 0 0 1 .08.6H3.2L8 5.2Zm-.6 9.99L3.2 11h9.1l-4.2 4.19a.5.5 0 0 1-.7 0Zm8.62-3.8a.6.6 0 0 0-1.04 0l-1.65 2.83a2.51 2.51 0 1 0 4.34 0l-1.65-2.83Zm-1.82 3.34 1.3-2.24 1.3 2.24a1.51 1.51 0 1 1-2.6 0Z";

    public const string Resize20RegularPath =
        "M8.5 3H6a3 3 0 0 0-3 3v.5a.5.5 0 0 0 1 0V6c0-1.1.9-2 2-2h2.5a.5.5 0 0 0 0-1ZM5.8 15.99A2 2 0 0 1 4 14v-3c0-1.1.9-2 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2H6l-.2-.01ZM3 14a3 3 0 0 0 3 3h3a3 3 0 0 0 3-3v-3a3 3 0 0 0-3-3H6a3 3 0 0 0-3 3v3Zm10.5 3a.5.5 0 0 1 0-1h.5a2 2 0 0 0 2-2v-2.5a.5.5 0 0 1 1 0V14a3 3 0 0 1-3 3h-.5ZM17 8.5a.5.5 0 0 1-1 0V6a2 2 0 0 0-2-2h-2.5a.5.5 0 0 1 0-1H14a3 3 0 0 1 3 3v2.5Z";

    public const string BranchFork20RegularPath =
        "M9 5a3 3 0 1 0-3.5 2.96v4.08a3 3 0 1 0 1 0V11H12a2.5 2.5 0 0 0 2.5-2.5v-.54a3 3 0 1 0-1 0v.54c0 .83-.67 1.5-1.5 1.5H6.5V7.96A3 3 0 0 0 9 5ZM6 7a2 2 0 1 1 0-4 2 2 0 0 1 0 4Zm0 10a2 2 0 1 1 0-4 2 2 0 0 1 0 4ZM16 5a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z";

    public const string RectangleLandscape20RegularPath =
        "M2 7a3 3 0 0 1 3-3h10a3 3 0 0 1 3 3v6a3 3 0 0 1-3 3H5a3 3 0 0 1-3-3V7Zm3-2a2 2 0 0 0-2 2v6c0 1.1.9 2 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2H5Z";

    public const string Circle20RegularPath =
        "M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14Zm-8 7a8 8 0 1 1 16 0 8 8 0 0 1-16 0Z";

    public const string Star20RegularPath =
        "M9.1 2.9a1 1 0 0 1 1.8 0l1.93 3.91 4.31.63a1 1 0 0 1 .56 1.7l-3.12 3.05.73 4.3a1 1 0 0 1-1.45 1.05L10 15.51l-3.86 2.03a1 1 0 0 1-1.45-1.05l.74-4.3L2.3 9.14a1 1 0 0 1 .56-1.7l4.31-.63L9.1 2.9Zm.9.44L8.07 7.25a1 1 0 0 1-.75.55L3 8.43l3.12 3.04a1 1 0 0 1 .3.89l-.75 4.3 3.87-2.03a1 1 0 0 1 .93 0l3.86 2.03-.74-4.3a1 1 0 0 1 .29-.89L17 8.43l-4.32-.63a1 1 0 0 1-.75-.55L10 3.35Z";

    public const string TextT20RegularPath =
        "M4 3.5c0-.28.22-.5.5-.5h10c.28 0 .5.22.5.5v2a.5.5 0 0 1-1 0V4h-4v12h1.5a.5.5 0 0 1 0 1h-4a.5.5 0 0 1 0-1H9V4H5v1.5a.5.5 0 0 1-1 0v-2Z";

    public const string Line20RegularPath =
        "M17.85 2.15c.2.2.2.51 0 .7l-15 15a.5.5 0 0 1-.7-.7l15-15c.2-.2.5-.2.7 0Z";

    public const string PaintBrush20RegularPath =
        "M5.5 2a.5.5 0 0 0-.5.5V11c0 1.1.9 2 2 2h1v3a2 2 0 1 0 4 0v-3h1a2 2 0 0 0 2-2V2.5a.5.5 0 0 0-.5-.5h-9Zm.5 8h8v1a1 1 0 0 1-1 1h-1.5a.5.5 0 0 0-.5.5V16a1 1 0 1 1-2 0v-3.5a.5.5 0 0 0-.5-.5H7a1 1 0 0 1-1-1v-1Zm8-1H6V3h4v1.5a.5.5 0 0 0 1 0V3h1v2.5a.5.5 0 0 0 1 0V3h1v6Z";

    public const string Eraser20RegularPath =
        "M11.2 2.44a1.5 1.5 0 0 1 2.12 0l4.24 4.24a1.5 1.5 0 0 1 0 2.12L9.36 17h5.14a.5.5 0 1 1 0 1H7.82a1.5 1.5 0 0 1-1.14-.44l-4.24-4.24a1.5 1.5 0 0 1 0-2.12l8.76-8.76Zm1.41.7a.5.5 0 0 0-.7 0L5.53 9.52l4.95 4.95 6.36-6.36a.5.5 0 0 0 0-.71l-4.24-4.24ZM9.78 15.18l-4.95-4.95-1.69 1.69a.5.5 0 0 0 0 .7l4.25 4.25c.2.2.5.2.7 0l1.7-1.7Z";

    public static Geometry OpenFolder20Regular { get; } = StreamGeometry.Parse(OpenFolder20RegularPath);
    public static Geometry Save20Regular { get; } = StreamGeometry.Parse(Save20RegularPath);
    public static Geometry Undo20Regular { get; } = StreamGeometry.Parse(Undo20RegularPath);
    public static Geometry Redo20Regular { get; } = StreamGeometry.Parse(Redo20RegularPath);
    public static Geometry Play20Regular { get; } = StreamGeometry.Parse(Play20RegularPath);
    public static Geometry Stop20Regular { get; } = StreamGeometry.Parse(Stop20RegularPath);
    public static Geometry Edit20Regular { get; } = StreamGeometry.Parse(Edit20RegularPath);
    public static Geometry Target20Regular { get; } = StreamGeometry.Parse(Target20RegularPath);
    public static Geometry Eye20Regular { get; } = StreamGeometry.Parse(Eye20RegularPath);
    public static Geometry ArrowClockwise20Regular { get; } = StreamGeometry.Parse(ArrowClockwise20RegularPath);
    public static Geometry Cursor20Regular { get; } = StreamGeometry.Parse(Cursor20RegularPath);
    public static Geometry CursorClick20Regular { get; } = StreamGeometry.Parse(CursorClick20RegularPath);
    public static Geometry Lasso20Regular { get; } = StreamGeometry.Parse(Lasso20RegularPath);
    public static Geometry HandLeft20Regular { get; } = StreamGeometry.Parse(HandLeft20RegularPath);
    public static Geometry ZoomIn20Regular { get; } = StreamGeometry.Parse(ZoomIn20RegularPath);
    public static Geometry Eyedropper20Regular { get; } = StreamGeometry.Parse(Eyedropper20RegularPath);
    public static Geometry PaintBucket20Regular { get; } = StreamGeometry.Parse(PaintBucket20RegularPath);
    public static Geometry Resize20Regular { get; } = StreamGeometry.Parse(Resize20RegularPath);
    public static Geometry BranchFork20Regular { get; } = StreamGeometry.Parse(BranchFork20RegularPath);
    public static Geometry RectangleLandscape20Regular { get; } = StreamGeometry.Parse(RectangleLandscape20RegularPath);
    public static Geometry Circle20Regular { get; } = StreamGeometry.Parse(Circle20RegularPath);
    public static Geometry Star20Regular { get; } = StreamGeometry.Parse(Star20RegularPath);
    public static Geometry TextT20Regular { get; } = StreamGeometry.Parse(TextT20RegularPath);
    public static Geometry Line20Regular { get; } = StreamGeometry.Parse(Line20RegularPath);
    public static Geometry PaintBrush20Regular { get; } = StreamGeometry.Parse(PaintBrush20RegularPath);
    public static Geometry Eraser20Regular { get; } = StreamGeometry.Parse(Eraser20RegularPath);
}
