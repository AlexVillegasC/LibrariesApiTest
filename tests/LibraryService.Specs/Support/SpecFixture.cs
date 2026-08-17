namespace LibraryService.Specs.Support;

public abstract class SpecFixture : IAsyncLifetime
{
    protected ApiFactory Api { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Api = new ApiFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await Api.DisposeAsync();
}
