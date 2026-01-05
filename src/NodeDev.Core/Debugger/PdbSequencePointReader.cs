using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NodeDev.Core.Debugger;

/// <summary>
/// Reads sequence points from a PDB file to map source locations to IL offsets
/// </summary>
public class PdbSequencePointReader
{
	/// <summary>
	/// Reads sequence points from a PDB and returns IL offsets for specific source locations
	/// </summary>
	public static Dictionary<string, int> ReadILOffsetsForVirtualLines(
		string assemblyPath,
		string className,
		string methodName,
		List<NodeBreakpointInfo> breakpoints)
	{
		var result = new Dictionary<string, int>();
		
		try
		{
			// Get the PDB path (same directory as assembly, .pdb extension)
			var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
			if (!File.Exists(pdbPath))
			{
				Console.WriteLine($"PDB not found: {pdbPath}");
				return result;
			}
			
			// Open the PDB
			using var pdbStream = File.OpenRead(pdbPath);
			using var metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
			var reader = metadataReaderProvider.GetMetadataReader();
			
			// Iterate through all methods in the PDB
			foreach (var methodDebugInformationHandle in reader.MethodDebugInformation)
			{
				var methodDebugInfo = reader.GetMethodDebugInformation(methodDebugInformationHandle);
				
				// Get sequence points for this method
				var sequencePoints = methodDebugInfo.GetSequencePoints();
				
				foreach (var sp in sequencePoints)
				{
					if (sp.IsHidden)
						continue;
					
					// Get the document (source file)
					var document = reader.GetDocument(sp.Document);
					var documentName = reader.GetString(document.Name);
					
					// Check if this sequence point matches any of our breakpoints
					foreach (var bp in breakpoints)
					{
						// Match by virtual file name and line number
						if (documentName.EndsWith(bp.SourceFile, StringComparison.OrdinalIgnoreCase) &&
						    sp.StartLine == bp.LineNumber)
						{
							// Found it! Store the IL offset
							var key = $"{bp.SourceFile}:{bp.LineNumber}";
							result[key] = sp.Offset;
							Console.WriteLine($"[PDB] Found {key} -> IL offset {sp.Offset}");
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to read PDB: {ex.Message}");
		}
		
		return result;
	}
}
