using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SmartTouchNotes.Core;

namespace SmartTouchNotes.Views;

public sealed class InkSurface : Control
{
    private InkStroke? _active;
    private IPointer? _pointer;
    public Note? Note { get; set; }
    public Action? StrokeStarted { get; set; }
    public Action? StrokeCompleted { get; set; }

    public InkSurface()
    {
        Focusable = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));
        if (Note is null) return;
        foreach (var stroke in Note.Strokes)
            DrawStroke(context, stroke);
        if (_active is not null) DrawStroke(context, _active);
    }

    private static void DrawStroke(DrawingContext context, InkStroke stroke)
    {
        var pen = new Pen(Brushes.Black, 2);
        if (stroke.Points.Count == 1)
            context.DrawEllipse(Brushes.Black, null, new Point(stroke.Points[0].X, stroke.Points[0].Y), 1, 1);
        for (var i = 1; i < stroke.Points.Count; i++)
            context.DrawLine(pen, new Point(stroke.Points[i - 1].X, stroke.Points[i - 1].Y),
                new Point(stroke.Points[i].X, stroke.Points[i].Y));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Note is null || _pointer is not null || e.Pointer.Type == PointerType.Touch ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer;
        _active = new InkStroke();
        StrokeStarted?.Invoke();
        AddPoint(e);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer != _pointer) return;
        AddPoint(e);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer != _pointer || _active is null || Note is null) return;
        AddPoint(e);
        Note.Strokes.Add(_active);
        _active = null;
        _pointer = null;
        e.Pointer.Capture(null);
        StrokeCompleted?.Invoke();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (e.Pointer != _pointer) return;
        _pointer = null;
        _active = null;
        InvalidateVisual();
    }

    public void CancelStroke()
    {
        var pointer = _pointer;
        _pointer = null;
        _active = null;
        pointer?.Capture(null);
        InvalidateVisual();
    }

    private void AddPoint(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        _active!.Points.Add(new InkPoint(point.Position.X, point.Position.Y,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            e.Pointer.Type == PointerType.Pen ? point.Properties.Pressure : null));
        InvalidateVisual();
    }
}
