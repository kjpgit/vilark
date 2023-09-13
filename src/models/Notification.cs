// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

record struct LoadingProgress(int Processed, int Ignored);

// A grab-bag of message types that we can send to the main thread
record struct Notification(
        LoadingProgress? LoadingProgress = null,
        IEnumerable<ISelectableItem>? CompletedData = null,
        string? FatalErrorMessage = null,
        string? WebRequest = null,
        bool ForceRedraw = false,
        bool ChildExited = false
    );
