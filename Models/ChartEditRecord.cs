using System;

namespace MajdataEdit_Neo.Models;

public class ChartEditRecord
{
    public string ChartPath { get; set; } = string.Empty;
    public int SelectedDifficulty { get; set; }
    public double TrackTime { get; set; }
    public TimeSpan TotalEditDuration { get; set; }
}
