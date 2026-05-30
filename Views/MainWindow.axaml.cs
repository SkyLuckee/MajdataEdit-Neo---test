using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Utils;
using MajdataEdit_Neo.Controls;
using MajdataEdit_Neo.Models;
using MajdataEdit_Neo.Models.SimaiAnalyzer;
using MajdataEdit_Neo.Types.MajSetting;
using MajdataEdit_Neo.Types.SimaiAnalyzer;
using MajdataEdit_Neo.ViewModels;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using MsBoxIcon = MsBox.Avalonia.Enums.Icon;

namespace MajdataEdit_Neo.Views;

public partial class MainWindow : Window
{
    MainWindowViewModel viewModel => (MainWindowViewModel)DataContext;
    TextEditor textEditor;
    TextMarkerService markerService;
    SimaiVisualizerControl simaiVisual;

    DispatcherTimer _debounceTimer;
    string? _currentTooltipMessage;

    private readonly HashSet<Key> _pressedKeys = new();
    bool isCtrlKeyDown => _pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl);

    public MainWindow()
    {
        //pull up MajdataView
        var viewPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "MajdataView.exe");

        if (File.Exists(viewPath) &&
            Process.GetProcessesByName("MajdataView").Length <= 0 &&
            Process.GetProcessesByName("Unity").Length <= 0)
        {
            Process.Start(viewPath);
        }

        InitializeComponent();
        //setup editor
        textEditor = this.FindControl<TextEditor>("Editor");
        textEditor.TextChanged += TextEditor_TextChanged;
        textEditor.TextArea.TextEntered += TextEditor_TextArea_TextEntered;
        textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        textEditor.TextArea.AddHandler(InputElement.KeyDownEvent, TextEditor_PreviewKeyDown, RoutingStrategies.Tunnel);
        textEditor.Options.HighlightCurrentLine = true;
        textEditor.Options.EnableTextDragDrop = true;
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _install = TextMate.InstallTextMate(textEditor, _registryOptions);
        var registry = new Registry(_install.RegistryOptions);
        _install.SetGrammarFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simai.tmLanguage.json"));
        _debounceTimer = new DispatcherTimer{ Interval = TimeSpan.FromMilliseconds(114.5) };
        _debounceTimer.Tick += _debounceTimer_Tick;
        markerService = new TextMarkerService(textEditor.Document, textEditor.TextArea.TextView);
        textEditor.TextArea.TextView.BackgroundRenderers.Add(markerService);
        textEditor.PointerMoved += TextEditor_PointerMoved;
        //setup visualizer
        simaiVisual = this.FindControl<SimaiVisualizerControl>("SimaiVisual");
        simaiVisual.PointerWheelChanged += SimaiVisual_PointerWheelChanged;
        simaiVisual.PointerMoved += SimaiVisual_PointerMoved;
        //zoom buttons
        this.FindControl<Button>("ZoomIn").Click += ZoomIn_Click;
        this.FindControl<Button>("ZoomOut").Click += ZoomOut_Click;
        //control panel
        First.PointerWheelChanged += First_PointerWheelChanged;
        //this window
        this.KeyDown += MainWindow_KeyDown;
        this.KeyUp += MainWindow_KeyUp;
        this.LostFocus += MainWindow_LostFocus;
        this.Closing += MainWindow_Closing;
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        var setting = viewModel.Settings.WindowSetting;
        this.Position = new PixelPoint(setting.PosX, setting.PosY);
        this.Width = setting.Width;
        this.Height = setting.Height;

        if (viewModel.Settings.EditSetting.AutoCheckUpdatesOnStartup)
        {
            await viewModel.CheckUpdateAsync(true);
        }
        await viewModel.ConnectToPlayerAsync();
    }

    bool haveAsked = false;
    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (haveAsked) return;
        e.Cancel = true;
        haveAsked = true;
        viewModel.SetWindowLastState(this);
        viewModel.OnWindowClosing();
        if (!await viewModel.AskSave())
        {
            Process.GetProcessesByName("MajdataView").FirstOrDefault()?.Kill();
            this.Close();
        }
        else haveAsked = false;
    }

    private void MainWindow_LostFocus(object? sender, RoutedEventArgs e)
    {
        _pressedKeys.Clear();
    }

    private void MainWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
    }

    private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);
    }

    private void Caret_PositionChanged(object? sender, System.EventArgs e)
    {
        var seek = textEditor.SelectionStart;
        viewModel.SetCaretTime(seek, isCtrlKeyDown);
        viewModel.CaretLine = textEditor.TextArea.Caret.Line;
    }

    static double? lastX = null;
    private void SimaiVisual_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as SimaiVisualizerControl);
        var x = point.Position.X;
        viewModel.IsPointerPressedSimaiVisual = point.Properties.IsLeftButtonPressed;
        if (lastX is null) lastX = x;
        var delta = x - lastX;
        if (point.Properties.IsLeftButtonPressed)
        {
            var docseek = viewModel.SlideTrackTime((float)delta*10f/Width);
            viewModel.SeekToDocPos(docseek,textEditor);
        }
        lastX = x;
    }

    private void ZoomIn_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.SlideZoomLevel(-0.3f);
    }
    private void ZoomOut_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.SlideZoomLevel(0.3f);
    }

    private void First_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        First.Value += (decimal)(e.Delta.Y / 100d);
    }

    private void SimaiVisual_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (isCtrlKeyDown)
        {
            viewModel.SlideZoomLevel(-0.3f * (float)e.Delta.Y);
        }
        else
        {
            var docseek = viewModel.SlideTrackTime(e.Delta.Y);
            viewModel.SeekToDocPos(docseek,textEditor);
        }
    }

    private void TextEditor_PreviewKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        bool hasShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool hasCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        //fix: when selection is not empty, left/right key will move caret to start/end of selection,
        //instead of moving caret from the start by one char.
        if (!textEditor.TextArea.Selection.IsEmpty && !hasShift)
        {
            if (e.Key == Key.Right)
            {
                int endOffset = textEditor.TextArea.Selection.SurroundingSegment.EndOffset;
                textEditor.TextArea.Caret.Offset = endOffset;
                textEditor.TextArea.ClearSelection();
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                int startOffset = textEditor.TextArea.Selection.SurroundingSegment.Offset;
                textEditor.TextArea.Caret.Offset = startOffset;
                textEditor.TextArea.ClearSelection();
                e.Handled = true;
            }
        }

        //fix: SB avaloniaEdit ate my ctrl+up/down
        if (hasCtrl && !hasShift)
        {
            if (e.Key == Key.Up)
            {
                textEditor.TextArea.Caret.Line = Math.Max(1, textEditor.TextArea.Caret.Line - 1);
                textEditor.TextArea.Caret.BringCaretToView();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                textEditor.TextArea.Caret.Line = Math.Min(textEditor.Document.LineCount, textEditor.TextArea.Caret.Line + 1);
                textEditor.TextArea.Caret.BringCaretToView();
                e.Handled = true;
            }
        }
    }
    private async void TextEditor_TextChanged(object? sender, System.EventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
        await viewModel.SetFumenContent(((TextEditor)sender).Text);
        var seek = textEditor.SelectionStart;
        viewModel.SetCaretTime(seek, isCtrlKeyDown);
    }
    private void _debounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        TextEditor_DebouncedTextChanged();
    }
    private async void TextEditor_DebouncedTextChanged()
    {
        var fumen = viewModel.CurrentFumen;

        var diags = await Task.Run(() => SimaiChecker.Check(fumen));
        viewModel.SimaiDiagnostics = diags;
        markerService.UpdateDiags(diags);

        viewModel.Signatures.Clear();
        var annos = await Task.Run(() => SimaiAnnotationParser.Parse(fumen));
        if (!annos.Any()) return;
        foreach (var annotation in annos)
        {
            switch (annotation)
            {
                case SignatureAnnotation s:
                    var timing = viewModel.GetNearestCommaTimingFromPos(s.Position);
                    if (timing == null) continue;

                    viewModel.Signatures.Add((timing.Timing, s.Numerator, s.Denominator));
                    break;
            }
        }
    }
    private void TextEditor_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var textView = textEditor.TextArea.TextView;
        var pos = e.GetPosition(textView);
        var visualPos = textView.GetPosition(pos + textView.ScrollOffset);

        string? newMessage = null;
        if (visualPos != null)
        {
            int offset = textEditor.Document.GetOffset(visualPos.Value.Line, visualPos.Value.Column);
            var marker = markerService.GetMarkerAtOffset(offset);
            newMessage = marker?.Message;
        }
        
        if (_currentTooltipMessage != newMessage)
        {
            _currentTooltipMessage = newMessage;
            if (!string.IsNullOrEmpty(newMessage))
            {
                ToolTip.SetTip(textEditor.TextArea, newMessage);
                ToolTip.SetIsOpen(textEditor.TextArea, true);
            }
            else
            {
                ToolTip.SetIsOpen(textEditor.TextArea, false);
            }
        }
    }

    private void TextEditor_TextArea_TextEntered(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        if (SimaiCompletionData.SIMAI_COMPLETIONS.ContainsKey(e.Text?[0] ?? '\0'))
        {
            var completionWindow = new CompletionWindow(textEditor.TextArea);
            completionWindow.Closed += (o, args) => completionWindow = null;

            var data = completionWindow.CompletionList.CompletionData;
            data.AddRange(SimaiCompletionData.SIMAI_COMPLETIONS[e.Text![0]]);

            completionWindow.Show();
        }
    }

    private async void FindReplace_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (textEditor.SearchPanel.IsOpened)
            textEditor.SearchPanel.Close();
        else
        {
            textEditor.TextArea.Focus();
            await Task.Delay(100); // focus will cost time, or the searchpanel buttons wont work.
            textEditor.SearchPanel.Open();
        }
    }

    private string GetFfmpegPath()
    {
        return Path.Combine(Environment.CurrentDirectory, "MajdataView_Data", "StreamingAssets", "ffmpeg.exe");
    }

    private async void MediaQuickProcess_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (viewModel.CurrentChartData is null || viewModel.CurrentChartData.CommaTimings.Length == 0)
            {
                await Utils.MessageBox.ShowWindowDialogAsync(
                    Assets.Langs.Langs.Msg_NoBpmInChart,
                    Assets.Langs.Langs.Gui_Error,
                    ButtonEnum.Ok, MsBoxIcon.Error);
                return;
            }

            var firstTiming = viewModel.CurrentChartData.CommaTimings[0];
            var bpm = firstTiming.Bpm;
            var offset = viewModel.Offset;

            var beatsCountBox = this.FindControl<NumericUpDown>("BeatsCountBox");
            var freezeFrameCheckBox = this.FindControl<CheckBox>("FreezeFrameCheckBox");

            if (beatsCountBox?.Value is null)
            {
                await Utils.MessageBox.ShowWindowDialogAsync(
                    Assets.Langs.Langs.Msg_InvalidBeatsCount,
                    Assets.Langs.Langs.Gui_Error,
                    ButtonEnum.Ok, MsBoxIcon.Error);
                return;
            }

            var beatsCount = (int)beatsCountBox.Value;
            var freezeFrame = freezeFrameCheckBox?.IsChecked == true;
            var ffmpegPath = GetFfmpegPath();

            if (!File.Exists(ffmpegPath))
            {
                await Utils.MessageBox.ShowWindowDialogAsync(
                    Assets.Langs.Langs.Status_NoFfmpeg,
                    Assets.Langs.Langs.Gui_Error,
                    ButtonEnum.Ok, MsBoxIcon.Error);
                return;
            }

            var maidataDir = viewModel.MaidataDir;
            var audioPath = Path.Combine(maidataDir, "track.mp3");
            if (!File.Exists(audioPath))
                audioPath = Path.Combine(maidataDir, "track.ogg");

            viewModel.ShowStatusMessage(Assets.Langs.Langs.Status_Processing);

            await Task.Run(() =>
            {
                TrackProcessor.AdjustMediaTime(ffmpegPath, audioPath, 60.0 / bpm * beatsCount, offset);

                string? videoPath = null;
                foreach (var name in new[] { "pv.mp4", "mv.mp4", "bg.mp4" })
                {
                    var dir = Path.Combine(maidataDir, name);
                    if (File.Exists(dir))
                    {
                        videoPath = dir;
                        break;
                    }
                }

                if (videoPath != null)
                {
                    TrackProcessor.AdjustMediaTime(ffmpegPath, videoPath, 60.0 / bpm * beatsCount, offset, freezeFrame);
                }
            });

            viewModel.Offset = 0;
            viewModel.SaveFile();
            await Task.Delay(30);
            await viewModel.ReloadFile();

            viewModel.ResetStatusMessage();
            await Utils.MessageBox.ShowWindowDialogAsync(
                Assets.Langs.Langs.Msg_MediaProcessComplete,
                Assets.Langs.Langs.Gui_Success,
                ButtonEnum.Ok, MsBoxIcon.Success);
        }
        catch (Exception ex)
        {
            viewModel.ResetStatusMessage();
            await Utils.MessageBox.ShowWindowDialogAsync(
                string.Format(Assets.Langs.Langs.Msg_MediaProcessFailed, ex.Message),
                Assets.Langs.Langs.Gui_Error,
                ButtonEnum.Ok, MsBoxIcon.Error);
        }
    }

    private async void NewChartFromVideo_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Video File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Video Files") { Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.flv", "*.wmv" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                },
                AllowMultiple = false
            });

            if (files.Count == 0) return;

            var file = files[0].TryGetLocalPath();
            if (file is null) return;

            var parent = Path.GetDirectoryName(file)!;
            var newFile = Path.Combine(parent, "pv.mp4");

            if (file != newFile)
            {
                if (File.Exists(newFile))
                    File.Delete(newFile);
                File.Move(file, newFile);
            }

            var ffmpegPath = GetFfmpegPath();
            if (!File.Exists(ffmpegPath))
            {
                await Utils.MessageBox.ShowWindowDialogAsync(
                    Assets.Langs.Langs.Status_NoFfmpeg,
                    Assets.Langs.Langs.Gui_Error,
                    ButtonEnum.Ok, MsBoxIcon.Error);
                return;
            }

            viewModel.ShowStatusMessage(Assets.Langs.Langs.Status_ExtractingAudio);

            var audioPath = Path.Combine(parent, "track.mp3");
            await Task.Run(() => TrackProcessor.ExtractAudio(ffmpegPath, newFile, audioPath));

            viewModel.ResetStatusMessage();
            await viewModel.NewChart(parent);
            viewModel.OpenChartInfoWindow();
        }
        catch (Exception ex)
        {
            viewModel.ResetStatusMessage();
            await Utils.MessageBox.ShowWindowDialogAsync(
                string.Format(Assets.Langs.Langs.Msg_ExtractAudioFailed, ex.Message),
                Assets.Langs.Langs.Gui_Error,
                ButtonEnum.Ok, MsBoxIcon.Error);
        }
    }
}
