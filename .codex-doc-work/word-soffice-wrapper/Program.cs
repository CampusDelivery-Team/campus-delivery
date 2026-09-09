using System.Runtime.InteropServices;

string? outDir = null;
string? input = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--outdir" && i + 1 < args.Length)
    {
        outDir = args[++i];
        continue;
    }

    if (!args[i].StartsWith("-") && File.Exists(args[i]))
    {
        input = args[i];
    }
}

if (outDir is null || input is null)
{
    Console.Error.WriteLine("Expected --outdir and an input document.");
    return 2;
}

Directory.CreateDirectory(outDir);
var output = Path.Combine(outDir, Path.GetFileNameWithoutExtension(input) + ".pdf");
Type? wordType = Type.GetTypeFromProgID("Word.Application");
if (wordType is null)
{
    Console.Error.WriteLine("Microsoft Word is not installed.");
    return 3;
}

dynamic? word = null;
dynamic? document = null;
try
{
    word = Activator.CreateInstance(wordType);
    word.Visible = false;
    word.DisplayAlerts = 0;
    document = word.Documents.Open(Path.GetFullPath(input), false, true);
    document.ExportAsFixedFormat(Path.GetFullPath(output), 17);
    Console.WriteLine($"convert {input} as {output}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 4;
}
finally
{
    if (document is not null)
    {
        try { document.Close(0); } catch { }
        try { Marshal.FinalReleaseComObject(document); } catch { }
    }
    if (word is not null)
    {
        try { word.Quit(); } catch { }
        try { Marshal.FinalReleaseComObject(word); } catch { }
    }
}
