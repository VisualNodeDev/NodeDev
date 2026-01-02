using Xunit;

namespace NodeDev.EndToEndTests.Fixtures;

[CollectionDefinition("E2E")]
public class E2ECollection : ICollectionFixture<AppServerFixture>, ICollectionFixture<PlaywrightFixture>
{
	// This class is never instantiated, it just defines the collection
}
