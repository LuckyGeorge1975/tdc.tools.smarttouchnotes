using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Gms.Extensions;
using Google.MLKit.Vision.Digitalink.Common;
using Google.MLKit.Vision.Digitalink.Recognition;
using SmartTouchNotes.Core;
using Xamarin.Google.MLKit.Common.Model;

namespace SmartTouchNotes.Android;

public sealed class AndroidInkRecognizer : IInkRecognizer
{
    private Task<DigitalInkRecognitionModel>? _modelTask;
    private bool _ready;

    public string Status => _ready
        ? "Deutsches Handschriftmodell bereit."
        : "Deutsches Handschriftmodell wird bei Bedarf heruntergeladen.";

    public async Task<string> RecognizeAsync(IReadOnlyList<InkStroke> strokes, CancellationToken cancellationToken)
    {
        var modelTask = _modelTask ??= PrepareModelAsync();
        DigitalInkRecognitionModel model;
        try { model = await modelTask; }
        catch
        {
            if (_modelTask == modelTask) _modelTask = null;
            throw;
        }
        cancellationToken.ThrowIfCancellationRequested();
        _ready = true;

        var ink = Ink.InvokeBuilder();
        foreach (var stroke in strokes)
        {
            var builder = Ink.Stroke.InvokeBuilder();
            foreach (var point in stroke.Points)
                builder.AddPoint(Ink.Point.Create((float)point.X, (float)point.Y, point.TimestampMilliseconds));
            if (stroke.Points.Count > 0) ink.AddStroke(builder.Build());
        }
        var client = DigitalInkRecognition.GetClient(DigitalInkRecognizerOptions.InvokeBuilder(model).Build());
        try
        {
            var result = await client.Recognize(ink.Build()).AsAsync<RecognitionResult>();
            cancellationToken.ThrowIfCancellationRequested();
            return result.Candidates.FirstOrDefault()?.Text ?? "";
        }
        finally { client.Close(); }
    }

    private static async Task<DigitalInkRecognitionModel> PrepareModelAsync()
    {
        var identifier = DigitalInkRecognitionModelIdentifier.FromLanguageTag("de-DE")
            ?? throw new InvalidOperationException("Kein deutsches Handschriftmodell verfügbar.");
        var model = DigitalInkRecognitionModel.InvokeBuilder(identifier).Build();
        await RemoteModelManager.Instance.Download(model, new DownloadConditions.Builder().Build()).AsAsync();
        return model;
    }
}
