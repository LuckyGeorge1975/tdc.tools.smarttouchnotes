using System.Text.Json;

namespace SmartTouchNotes.Core;

public sealed record InkPoint(double X, double Y, long TimestampMilliseconds, float? Pressure);

public sealed class InkStroke
{
    public List<InkPoint> Points { get; set; } = [];
}

public sealed class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Title { get; set; }
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<InkStroke> Strokes { get; set; } = [];
    public string RecognizedText { get; set; } = "";
}

public sealed class NoteDocument
{
    public int Version { get; set; } = 1;
    public Guid? LastOpenedNoteId { get; set; }
    public List<Note> Notes { get; set; } = [];
}

public sealed class NoteStore(string path)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public NoteDocument Load()
    {
        if (!File.Exists(path)) return new NoteDocument();
        var document = JsonSerializer.Deserialize<NoteDocument>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Leere Notizdatei.");
        if (document.Version != 1) throw new InvalidDataException($"Nicht unterstützte Notizversion: {document.Version}");
        return document;
    }

    public void Save(NoteDocument document)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".notes-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, document, Options);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
