using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Types;

internal class MutSimaiChartMetadata
{
    public string Level { get; set; } = string.Empty;
    public string Designer { get; set; } = string.Empty;
    public string Fumen { get; set; } = string.Empty;

    public static readonly MutSimaiChartMetadata Empty = new();
}
