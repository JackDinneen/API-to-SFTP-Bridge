namespace IntegrationTests;

/// <summary>
/// Defines a test collection that ensures all integration tests run sequentially,
/// preventing race conditions on the shared static TestAuthHandler state.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
