using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Types.SimaiAnalyzer;

public abstract class SimaiAnnotation
{
    public int Position { get; init; }
}
public sealed class SignatureAnnotation : SimaiAnnotation
{
    public int Numerator { get; init; }
    public int Denominator { get; init; }
}