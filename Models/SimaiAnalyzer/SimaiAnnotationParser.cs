using MajdataEdit_Neo.Types.SimaiAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Models.SimaiAnalyzer;

public static class SimaiAnnotationParser
{
    public static IEnumerable<SimaiAnnotation> Parse(string fumen)
    {
        var lines = fumen.Split('\n', StringSplitOptions.None);
        var annotations = new List<SimaiAnnotation>();

        var position = 0;

        foreach (var line in lines)
        {
            if (!line.StartsWith("||"))
            {
                position += line.Length + 1;
                continue;
            }
            
            var c = line[2..].Trim();
            if (c.StartsWith('s'))  //Signature
            {
                var content = c[1..].Trim();
                if (c.StartsWith("signature")) content = c[9..].Trim();

                var parts = content.Split('/', 2);
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int numerator) &&
                        int.TryParse(parts[1], out int denominator))
                    {
                        annotations.Add(new SignatureAnnotation 
                        { 
                            Position = position,
                            Numerator = numerator,
                            Denominator = denominator 
                        });
                    }
                }
            }

            position += line.Length + 1;
        }
        return annotations;
    }
}
