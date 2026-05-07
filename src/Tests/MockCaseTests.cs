using MultiAgentLab.Api.Infrastructure.Mocks;

namespace MultiAgentLab.Tests;

public class MockCaseTests
{
    [Fact]
    public async Task MockCaseLoader_LoadsCasesFromDirectory()
    {
        var mockDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "mock_inputs");
        var fullPath = Path.GetFullPath(mockDir);

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        var loader = new MockCaseLoader(fullPath);
        var cases = await loader.ListCasesAsync();

        Assert.True(cases.Count >= 5, $"Expected at least 5 mock cases, found {cases.Count}");
    }

    [Fact]
    public async Task MockCaseLoader_EachCaseHasRequiredFields()
    {
        var mockDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "mock_inputs");
        var fullPath = Path.GetFullPath(mockDir);

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        var loader = new MockCaseLoader(fullPath);
        var cases = await loader.ListCasesAsync();

        foreach (var c in cases)
        {
            Assert.False(string.IsNullOrWhiteSpace(c.CaseId), "CaseId should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Title), "Title should not be empty");
            Assert.NotNull(c.Request);
            Assert.False(string.IsNullOrWhiteSpace(c.Request.StoryId), "StoryId should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Request.StoryText), "StoryText should not be empty");
            Assert.NotNull(c.Request.Provider);
        }
    }

    [Fact]
    public async Task MockCaseLoader_GetSpecificCase_ReturnsCorrect()
    {
        var mockDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "mock_inputs");
        var fullPath = Path.GetFullPath(mockDir);

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        var loader = new MockCaseLoader(fullPath);
        var case1 = await loader.GetCaseAsync("mock-01");

        Assert.NotNull(case1);
        Assert.Equal("mock-01", case1!.CaseId);
        Assert.Contains("button", case1.Request.StoryText.ToLowerInvariant());
    }

    [Fact]
    public async Task MockCaseLoader_NonExistentCase_ReturnsNull()
    {
        var mockDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "mock_inputs");
        var fullPath = Path.GetFullPath(mockDir);

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        var loader = new MockCaseLoader(fullPath);
        var result = await loader.GetCaseAsync("does-not-exist");

        Assert.Null(result);
    }
}
