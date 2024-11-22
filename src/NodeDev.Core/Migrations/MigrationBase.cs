using System.Text.Json.Nodes;

namespace NodeDev.Core.Migrations;

internal abstract class MigrationBase
{
	internal abstract string Version { get; }

	internal virtual void PerformMigrationBeforeDeserialization(JsonObject document)
	{
	}

	internal virtual void PerformMigrationAfterClassesDeserialization(Project project)
	{
	}
}
