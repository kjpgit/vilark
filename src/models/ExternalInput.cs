// Copyright (C) 2023 Karl Pickett / ViLark Project
namespace vilark;

class ExternalInputEntry : ISelectableItem
{
    private string itemData;
    private bool displayAsFile = false;

    public ExternalInputEntry(string itemData, bool displayAsFile) {
        this.itemData = itemData;
        this.displayAsFile = displayAsFile;
    }

    public string GetDisplayString() => displayAsFile ? TextHelper.GetNiceFileDisplayString(itemData) : itemData;

    public string GetChoiceString() => itemData;

    public string GetSearchString() => itemData;

}

