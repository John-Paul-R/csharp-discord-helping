using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

// ```
// one two three four five
// six seven eight nine ten
// ```
//
// ```
// three apple orange cherry eight orchid sunflower
// ```
//
// ```
// one two three apple orange cherry eight four five six seven eight nine ten orchid sunflower
//           *                         *                                    *
//         match                     match                            end of file 1
// ```

var result = MergeAllTextLookAhead(
    str1: """
    one two three four five
    six seven eight nine ten
    """,

    str2: """
    three apple orange cherry eight orchid sunflower
    """
);

Console.WriteLine();
Console.Write("Result:\n\t");
Console.WriteLine(result);

static string CleanString(string input)
{
    return new string(
        input
            .Where(c => char.IsLetter(c))
            .ToArray()
    );
}

static string[] GetWords(string input)
{
    return input
        .Split(' ')
        .Select(CleanString)
        .Where(v => v != "")
        .ToArray();
}

static bool IsWordEndingCharacter(int ch)
{
    return ch is ' ' or '\n' or '\t';
}

/// <returns>whether the file has ended</returns>
static bool ReadUntilMatch(
    TextReader reader,
    HashSet<string> notYetCopiedWordsFromThisFile,
    HashSet<string> notYetCopiedWordsFromOtherFile,
    StringBuilder sb)
{
    StringBuilder wordBuilder = new();

    bool isFileEnd = false;

    while (true) { // one iteration per word
        while (true) { // one iteration per character

            // this peek-1, read-1 isn't efficient, generally
            //  _kinda_ ok for strings instead of files, but still not great
            var peekedChar = reader.Peek();
            if (peekedChar == -1) {
                isFileEnd = true;
                Console.WriteLine("file end");
                break;
            }
            if (IsWordEndingCharacter(peekedChar)) {
                reader.Read();
                Console.WriteLine("wordbound");
                break; // not file end, but word is done
            }

            wordBuilder.Append((char)reader.Read());
        }

        var fullWordIncludingSymbols = wordBuilder.ToString();
        var cleanWord = CleanString(fullWordIncludingSymbols);

        // the word is one of the "not yet copied" words from the
        // other file, so we'll break here to switch to that file
        bool isMatchedWord = notYetCopiedWordsFromOtherFile.Contains(cleanWord);

        // no special handling to do, just copy the word to output
        sb.Append(' ');
        sb.Append(fullWordIncludingSymbols);
        // we've now copied the word, so remove it from our "not yet copied list"
        notYetCopiedWordsFromThisFile.Remove(cleanWord);

        Console.WriteLine("fn outer - {0}", wordBuilder.ToString());
        wordBuilder.Clear();
        if (isFileEnd || isMatchedWord) {
            return isFileEnd;
        }
    }
}

/// <summary>
/// a function that accepts 2 strings and merges them into a 3rd string via the following steps:
///   1. Copy words (and their attached punctuation) from `str1`
///      until encountering a word from `str2` that has not yet
///      been copied to the output, or until EOF.
///   2. If both files are at EOF, exit.
///   3. Copy words (and their attached punctuation) from `str2`
///      until encountering a word from `str1` that has not yet
///      been copied to the output, or until EOF.
///   4. If both files are at EOF, exit, otherwise goto 1
/// </summary>
static string MergeAllTextLookAhead(string str1, string str2)
{
    // store a collection of words in each file in a way that offers quick,
    // case-insensitive "contains" checks
    // we'll remove words from these collections as we process them
    HashSet<string> notCopiedStr1Words = new(GetWords(str1), StringComparer.InvariantCultureIgnoreCase);
    HashSet<string> notCopiedStr2Words = new(GetWords(str2), StringComparer.InvariantCultureIgnoreCase);

    StringReader reader1 = new(str1);
    StringReader reader2 = new(str2);

    // indicator for which file to read from, handles switching
    bool isReadingFile2 = false;

    StringBuilder outputBuilder = new();

    // whether we've hit EOF on either file
    bool isFileOneEnded = false;
    bool isFileTwoEnded = false;

    // read from target file until a match, removing words we see from the file's hashset as we go
    // stop when we've gone through both files to their ends
    while (!isFileOneEnded && !isFileTwoEnded) {
        Console.WriteLine("outer - {0} - {1}", isReadingFile2, outputBuilder.ToString());
        if (!isReadingFile2 && !isFileOneEnded) {
            isFileOneEnded = ReadUntilMatch(
                reader1,
                notYetCopiedWordsFromThisFile: notCopiedStr1Words,
                notYetCopiedWordsFromOtherFile: notCopiedStr2Words,
                outputBuilder
            );
            isReadingFile2 = true;
        } else if (isReadingFile2 && !isFileTwoEnded) {
            isFileTwoEnded = ReadUntilMatch(
                reader2,
                notYetCopiedWordsFromThisFile: notCopiedStr2Words,
                notYetCopiedWordsFromOtherFile: notCopiedStr1Words,
                outputBuilder
            );
            isReadingFile2 = false;
        }
    }

    return outputBuilder.ToString();
}
