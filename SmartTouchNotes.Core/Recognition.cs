namespace SmartTouchNotes.Core;

public interface IInkRecognizer
{
    string Status { get; }
    Task<string> RecognizeAsync(IReadOnlyList<InkStroke> strokes, CancellationToken cancellationToken);
}

public sealed class UnavailableRecognizer : IInkRecognizer
{
    public string Status => "Handschrifterkennung auf diesem Gerät nicht verfügbar.";
    public Task<string> RecognizeAsync(IReadOnlyList<InkStroke> strokes, CancellationToken cancellationToken) =>
        Task.FromException<string>(new NotSupportedException(Status));
}

public sealed class RecognitionCoordinator(IInkRecognizer recognizer, TimeSpan? pause = null)
{
    private readonly TimeSpan _pause = pause ?? TimeSpan.FromMilliseconds(500);
    private readonly object _gate = new();
    private long _revision;
    private CancellationTokenSource? _pending;

    public void Invalidate()
    {
        lock (_gate)
        {
            _revision++;
            _pending?.Cancel();
            _pending = null;
        }
    }

    public async Task<RecognitionOutcome?> RecognizeAsync(IEnumerable<InkStroke> strokes)
    {
        var snapshot = strokes.Select(s => new InkStroke { Points = s.Points.ToList() }).ToList();
        CancellationTokenSource cancellation;
        long revision;
        lock (_gate)
        {
            Invalidate();
            revision = _revision;
            cancellation = new CancellationTokenSource();
            _pending = cancellation;
        }
        try
        {
            await Task.Delay(_pause, cancellation.Token);
            var text = await recognizer.RecognizeAsync(snapshot, cancellation.Token);
            return IsCurrent() ? new RecognitionOutcome(text, null) : null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            return IsCurrent() ? new RecognitionOutcome(null, ex.Message) : null;
        }
        finally
        {
            lock (_gate)
            {
                if (_pending == cancellation) _pending = null;
                cancellation.Dispose();
            }
        }

        bool IsCurrent()
        {
            lock (_gate) return !cancellation.IsCancellationRequested && _revision == revision;
        }
    }
}

public sealed record RecognitionOutcome(string? Text, string? Error);
