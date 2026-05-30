using System;
using System.Diagnostics;

namespace MajdataEdit_Neo.Models;

public class EditTimer
{
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _accumulated;

    public void Start()
    {
        if (!_stopwatch.IsRunning)
            _stopwatch.Start();
    }

    public void Pause()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _accumulated += _stopwatch.Elapsed;
            _stopwatch.Reset();
        }
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _accumulated = TimeSpan.Zero;
    }

    public TimeSpan Elapsed => _accumulated + _stopwatch.Elapsed;

    public void LoadAccumulated(TimeSpan previous)
    {
        _accumulated = previous;
    }
}
