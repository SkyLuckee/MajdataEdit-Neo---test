using Avalonia.Data.Converters;
using MajdataEdit_Neo.Assets.Langs;
using MajdataEdit_Neo.Types.MajWs;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MajdataEdit_Neo.Converters;

/// <summary>
/// 状态栏多值转换器
/// Values[0]: StatusBarMessage (string?)
/// Values[1]: CurrentViewState (ViewStatus)
/// Values[2]: IsConnected (bool)
/// Values[3]: I18N.Culture (用于触发语言变化刷新)
/// </summary>
public class StatusBarConverter : IMultiValueConverter
{
    public static readonly StatusBarConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return "";

        // 优先显示临时消息
        if (values[0] is string message && !string.IsNullOrEmpty(message))
        {
            return message;
        }

        // 检查连接状态
        if (values[2] is bool isConnected && !isConnected)
        {
            return Langs.Status_Disconnected;
        }

        // 显示当前状态
        if (values[1] is ViewStatus state)
        {
            return state switch
            {
                ViewStatus.Idle => Langs.Status_Idle,
                ViewStatus.Loaded => Langs.Status_Loaded,
                ViewStatus.Error => Langs.Status_Error,
                ViewStatus.Playing => Langs.Status_Playing,
                ViewStatus.Paused => Langs.Status_Paused,
                ViewStatus.Busy => Langs.Status_Busy,
                _ => ""
            };
        }

        return "";
    }
}
