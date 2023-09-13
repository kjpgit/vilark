// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Text;

namespace vilark;

class FuzzyTextQuery
{
    private FuzzySearchMode mode;
    private string query;
    private List<FuzzyTextWord> query_words = new();

    public FuzzyTextQuery(string query, FuzzySearchMode mode) {
        this.mode = mode;
        this.query = query;
        var words = query.Split().ToList();
        foreach (var word in words) {
            bool has_upper = TextHelper.HasUppercaseChar(word);
            query_words.Add(new FuzzyTextWord(word, has_upper));
        }
    }

    public FuzzyTextResult RunQuery(string line) {
        if (line == String.Empty) {
            return new FuzzyTextResult(false);
        }

        bool complete_match = false;
        int pos = -1;
        int startPos = 0;
        foreach (var word in query_words) {
            var cmp_mode = word.has_upper ? StringComparison.Ordinal :
                    StringComparison.OrdinalIgnoreCase;
            pos = line.IndexOf(word.text, startPos, cmp_mode);
            if (pos >= 0) {
                if (mode == FuzzySearchMode.FUZZY_WORD_ORDERED) {
                    // Advance where next word starts
                    startPos = pos + word.text.Length;
                }

                // Mark match location
                //matches.Add(new FuzzyTextMatch(pos, startPos));
            } else {
                break;
            }
        }
        complete_match = (pos != -1);
        return new FuzzyTextResult(complete_match);
    }

    private record struct FuzzyTextWord(string text, bool has_upper);
}


record struct FuzzyTextResult(
    bool is_complete_match
    //string line,
    //List<FuzzyTextMatch>? matches,
);

//record struct FuzzyTextMatch(int startCol, int endCol);


