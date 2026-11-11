using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SmartTouchNotes.Core;

namespace SmartTouchNotes.Views;

public sealed class MainView : UserControl
{
    private readonly NoteStore _store;
    private readonly IInkRecognizer _recognizer;
    private readonly RecognitionCoordinator _recognition;
    private readonly NoteDocument _document;
    private readonly InkSurface _surface = new();
    private readonly ListBox _list = new();
    private readonly TextBlock _preview = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxHeight = 60 };
    private readonly TextBlock _status = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly Grid _content = new();
    private Note _current;
    private bool _refreshing;

    public MainView(NoteStore store, IInkRecognizer recognizer)
    {
        _store = store;
        _recognizer = recognizer;
        _recognition = new RecognitionCoordinator(recognizer);
        try { _document = store.Load(); }
        catch (Exception ex) { throw new InvalidOperationException("Notizen konnten nicht geladen werden. Die vorhandene Datei wurde nicht verändert.", ex); }
        _current = _document.Notes.FirstOrDefault(n => n.Id == _document.LastOpenedNoteId)
            ?? _document.Notes.OrderByDescending(n => n.ModifiedUtc).FirstOrDefault()
            ?? CreateNote();
        _document.LastOpenedNoteId = _current.Id;

        var newButton = new Button { Content = "Neue Notiz" };
        var listButton = new Button { Content = "Notizen" };
        var undoButton = new Button { Content = "Rückgängig" };
        var recognizeButton = new Button { Content = "Erkennen" };
        var toolbar = new WrapPanel { Orientation = Orientation.Horizontal };
        toolbar.Children.Add(newButton);
        toolbar.Children.Add(listButton);
        toolbar.Children.Add(undoButton);
        toolbar.Children.Add(recognizeButton);
        newButton.Click += (_, _) => { SwitchTo(CreateNote()); ShowList(false); };
        listButton.Click += (_, _) => { RefreshList(); ShowList(!_list.IsVisible); };
        undoButton.Click += (_, _) => Undo();
        recognizeButton.Click += (_, _) => { if (_current.Strokes.Count > 0) StartRecognition(); };

        _surface.Note = _current;
        _surface.StrokeStarted = () => _recognition.Invalidate();
        _surface.StrokeCompleted = () => { Changed(); StartRecognition(); };
        _list.IsVisible = false;
        _list.SelectionChanged += (_, _) =>
        {
            if (_refreshing || _list.SelectedItem is not NoteItem item) return;
            SwitchTo(item.Note);
            ShowList(false);
        };
        _content.Children.Add(_surface);
        _content.Children.Add(_list);

        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*") };
        layout.Children.Add(toolbar);
        Grid.SetRow(_status, 1); layout.Children.Add(_status);
        Grid.SetRow(_preview, 2); layout.Children.Add(_preview);
        Grid.SetRow(_content, 3); layout.Children.Add(_content);
        Content = layout;
        UpdateDisplay();
        Save();
    }

    private Note CreateNote()
    {
        var note = new Note();
        _document.Notes.Add(note);
        return note;
    }

    private void SwitchTo(Note note)
    {
        _recognition.Invalidate();
        _surface.CancelStroke();
        _current = note;
        _surface.Note = note;
        _document.LastOpenedNoteId = note.Id;
        _surface.InvalidateVisual();
        UpdateDisplay();
        Save();
    }

    private void Undo()
    {
        _recognition.Invalidate();
        _surface.CancelStroke();
        if (_current.Strokes.Count == 0) return;
        _current.Strokes.RemoveAt(_current.Strokes.Count - 1);
        _current.RecognizedText = "";
        _surface.InvalidateVisual();
        Changed();
        if (_current.Strokes.Count > 0) StartRecognition();
    }

    private void Changed()
    {
        _current.ModifiedUtc = DateTimeOffset.UtcNow;
        UpdateDisplay();
        Save();
    }

    private async void StartRecognition()
    {
        if (_recognizer is UnavailableRecognizer) return;
        _status.Text = "Handschrift wird erkannt; Modell wird bei Bedarf heruntergeladen …";
        var result = await _recognition.RecognizeAsync(_current.Strokes);
        if (result is null) return;
        if (result.Error is not null) { _status.Text = $"Erkennung: {result.Error}"; return; }
        _current.RecognizedText = result.Text ?? "";
        UpdateDisplay();
        Save();
    }

    private void UpdateDisplay()
    {
        _preview.Text = string.IsNullOrWhiteSpace(_current.RecognizedText)
            ? "Textvorschau: –" : $"Textvorschau: {_current.RecognizedText}";
        _status.Text = _recognizer.Status;
    }

    private void Save()
    {
        try { _store.Save(_document); }
        catch (Exception ex) { _status.Text = $"Speichern fehlgeschlagen: {ex.Message}"; }
    }

    private void RefreshList()
    {
        _refreshing = true;
        _list.ItemsSource = _document.Notes.OrderByDescending(n => n.ModifiedUtc).Select(n => new NoteItem(n)).ToArray();
        _refreshing = false;
    }

    private void ShowList(bool show)
    {
        _list.IsVisible = show;
        _surface.IsVisible = !show;
    }

    private sealed record NoteItem(Note Note)
    {
        public override string ToString() =>
            $"{(string.IsNullOrWhiteSpace(Note.Title) ? "Notiz" : Note.Title)} · {Note.ModifiedUtc.ToLocalTime():g}";
    }
}
