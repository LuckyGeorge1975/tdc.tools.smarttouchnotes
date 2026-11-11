using SmartTouchNotes.Core;

namespace SmartTouchNotes.Tests;

public class NoteStoreTests
{
    [Fact]
    public void MultipleNotesAndLastOpenedSurviveReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var store = new NoteStore(Path.Combine(directory, "notes.json"));
            var first = new Note { RecognizedText = "Hallo" };
            first.Strokes.Add(new InkStroke { Points = [new(10.5, 20, 123, 0.4f), new(12, 22, 124, null)] });
            var second = new Note { Title = "Zwei" };
            var document = new NoteDocument { Notes = [first, second], LastOpenedNoteId = second.Id };
            store.Save(document);

            var loaded = store.Load();
            Assert.Equal(1, loaded.Version);
            Assert.Equal(second.Id, loaded.LastOpenedNoteId);
            Assert.Equal(2, loaded.Notes.Count);
            Assert.Equal(first.Id, loaded.Notes[0].Id);
            Assert.Equal("Hallo", loaded.Notes[0].RecognizedText);
            Assert.Equal(first.Strokes[0].Points, loaded.Notes[0].Strokes[0].Points);
            loaded.LastOpenedNoteId = first.Id;
            store.Save(loaded);
            Assert.Equal(first.Id, store.Load().LastOpenedNoteId);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void UnknownVersionIsNotOverwritten()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"Version":2,"Notes":[]}""");
            Assert.Throws<InvalidDataException>(() => new NoteStore(path).Load());
            Assert.Contains("\"Version\":2", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }
}
