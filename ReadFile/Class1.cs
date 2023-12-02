using System.Text;
using ReadFile;

string filePath = "file.txt";

using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

using var reader = new CharLineStreamReader(fs, Encoding.UTF8);

char[]? charArray = reader.ReadLineToCharArray();

// wew, your span, read directly from the stream
Span<char> chars = charArray;

Console.WriteLine(new string(charArray));
