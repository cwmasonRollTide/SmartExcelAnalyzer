using API.Extensions;
using System.Diagnostics.CodeAnalysis;
using static API.Extensions.ProgramExtensions;

namespace API;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args) => ConfigureSmartExcelAnalyzerProgram(args).Run();
}
