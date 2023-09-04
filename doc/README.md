# Vilark User Documentation

## Options Tab

### "When multiple search words are used"

If you search for "mo sc", ViLark searches each path for "mo" and "sc".

If you use "Match exact order", the "sc" must match after "mo" in the path.

If you use "Match any order", both "sc" and "mo" can match anywhere in the path.

Example, if you search for "mo sc":

* "Models\\MyScrollView.cs" will always match, because "sc" in Scroll comes
  after "mo" in "Model".

If you have "Match any order" selected:

* "ScrollModel.cs" will also match, because "sc" in "Scroll" doesn't have to
  come after "Model".

