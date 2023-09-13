// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

// A grab-bag of message types that we can send to the main thread
record struct Notification(
        int? Processed = null,
        int? Ignored = null,
        IEnumerable<ISelectableItem>? CompletedData = null,
        string? ErrorMessage = null,
        string? WebRequest = null,
        bool ForceRedraw = false,
        bool ChildExited = false
        );
