using SmartTouchNotes.Core;

namespace SmartTouchNotes.Tests;

public class RecognitionTests
{
    [Theory]
    [InlineData("stroke")]
    [InlineData("undo")]
    [InlineData("note switch")]
    public async Task InvalidatedRecognitionCannotPublishLateResult(string change)
    {
        var recognizer = new DelayedRecognizer();
        var coordinator = new RecognitionCoordinator(recognizer, TimeSpan.Zero);
        var pending = coordinator.RecognizeAsync([new InkStroke { Points = [new(1, 2, 3, null)] }]);
        await recognizer.Started.Task;
        coordinator.Invalidate();
        recognizer.Completion.SetResult(change);
        Assert.Null(await pending);
    }

    [Fact]
    public async Task NewRecognitionWinsAndUsesSnapshot()
    {
        var recognizer = new DelayedRecognizer();
        var coordinator = new RecognitionCoordinator(recognizer, TimeSpan.Zero);
        var strokes = new List<InkStroke> { new() { Points = [new(1, 2, 3, null)] } };
        var pending = coordinator.RecognizeAsync(strokes);
        await recognizer.Started.Task;
        strokes[0].Points.Add(new(4, 5, 6, null));
        Assert.Single(recognizer.Snapshot![0].Points);
        recognizer.Completion.SetResult("Text");
        Assert.Equal("Text", (await pending)!.Text);
    }

    [Fact]
    public async Task LaterRequestWinsEvenWhenEarlierCompletesLast()
    {
        var recognizer = new TwoRequestsRecognizer();
        var coordinator = new RecognitionCoordinator(recognizer, TimeSpan.Zero);
        var first = coordinator.RecognizeAsync([new InkStroke()]);
        var second = coordinator.RecognizeAsync([new InkStroke()]);
        recognizer.Second.SetResult("Neu");
        Assert.Equal("Neu", (await second)!.Text);
        recognizer.First.SetResult("Alt");
        Assert.Null(await first);
    }

    [Fact]
    public async Task InvalidationDuringPausePreventsRecognition()
    {
        var recognizer = new DelayedRecognizer();
        var coordinator = new RecognitionCoordinator(recognizer, TimeSpan.FromMinutes(1));
        var pending = coordinator.RecognizeAsync([new InkStroke()]);
        coordinator.Invalidate();
        Assert.Null(await pending);
        Assert.False(recognizer.Started.Task.IsCompleted);
    }

    private sealed class TwoRequestsRecognizer : IInkRecognizer
    {
        private int _calls;
        public string Status => "Ready";
        public TaskCompletionSource<string> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> Second { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<string> RecognizeAsync(IReadOnlyList<InkStroke> strokes, CancellationToken cancellationToken) =>
            Interlocked.Increment(ref _calls) == 1 ? First.Task : Second.Task;
    }

    private sealed class DelayedRecognizer : IInkRecognizer
    {
        public string Status => "Ready";
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IReadOnlyList<InkStroke>? Snapshot { get; private set; }
        public Task<string> RecognizeAsync(IReadOnlyList<InkStroke> strokes, CancellationToken cancellationToken)
        {
            Snapshot = strokes;
            Started.SetResult();
            return Completion.Task;
        }
    }
}
