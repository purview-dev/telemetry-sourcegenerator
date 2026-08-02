using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Purview.Telemetry.SourceGenerator;

public static class ModuleInitialiser
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifyDiffPlex.Initialize(OutputType.Compact);
		DiffEngine.DiffRunner.MaxInstancesToLaunch(20);
		VerifySourceGenerators.Initialize();
	}
}
