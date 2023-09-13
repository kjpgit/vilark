# Configuring Fuzzy Search

## Case Sensitivity

Vilark uses "smart" case sensitivity, on a word-by-word basis.
This is not currently configurable.

Example: If you search for "Model foo", the phrase "Model" must appear in the path
(case-sensitive), and "foo", "Foo", or "FOO", etc. must appear in the path
(case-insensitive).


## When multiple search words are used

If you search for "mo sc", Vilark searches each path for "mo" and "sc".

* If Vilark is set to "Match exact order", the "sc" must match after "mo" in the path.

* If Vilark is set to "Match any order", both "sc" and "mo" can match anywhere in the path.

To set one of the above choices, go to the Options tab in Vilark.

### Example

If you search for "mo sc":

* `Models/MyScrollItem.cs` will always match, because "sc" in Scroll comes
  after "mo" in "Model".

If you have "Match any order" selected:

* `ScrollMotion.cs` will also match, because "sc" in "Scroll" doesn't have to
  come after "mo" in "Motion".

