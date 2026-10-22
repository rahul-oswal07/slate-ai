using SlateAi.Api.Ai;

namespace SlateAi.Api.Tests.Ai;

public sealed class PromptLibraryTests
{
    private static readonly string[] ModelKeys = ["Mapping", "Compression", "Script", "Check"];

    [Fact]
    public void Loads_the_header_and_the_prompt_text()
    {
        var library = PromptLibrary.Load(
            [("a1-lesson-mapping.md", "---\nid: a1-lesson-mapping\nversion: 3\nmodel_key: Mapping\n---\n\nMap the lesson.\nUse IDs.\n")],
            ModelKeys);

        Assert.Equal(new Prompt("a1-lesson-mapping", 3, "Mapping", "Map the lesson.\nUse IDs."), library.Get("a1-lesson-mapping"));
    }

    [Fact]
    public void Handles_windows_line_endings()
    {
        var library = PromptLibrary.Load(
            [("a4-verifier.md", "---\r\nid: a4-verifier\r\nversion: 1\r\nmodel_key: Check\r\n---\r\nCheck it.\r\n")], ModelKeys);

        Assert.Equal("Check it.", library.Get("a4-verifier").Text);
    }

    [Theory]
    [InlineData("Map the lesson.", "missing '---' header with id, version and model_key")]
    [InlineData("---\nid: other\nversion: 1\nmodel_key: Mapping\n---\ntext", "id must match the file name (a1)")]
    [InlineData("---\nid: a1\nversion: one\nmodel_key: Mapping\n---\ntext", "version must be a positive whole number")]
    [InlineData("---\nid: a1\nversion: 0\nmodel_key: Mapping\n---\ntext", "version must be a positive whole number")]
    [InlineData("---\nid: a1\nversion: 1\nmodel_key: Fancy\n---\ntext", "model_key 'Fancy' is not configured under Ai:Models")]
    [InlineData("---\nid: a1\nversion: 1\nmodel_key: Mapping\n---\n  \n", "prompt text is empty")]
    public void Rejects_a_bad_prompt_file(string content, string expected)
    {
        var error = Assert.Throws<PromptException>(() => PromptLibrary.Load([("a1.md", content)], ModelKeys));

        Assert.Equal($"a1.md: {expected}", Assert.Single(error.Errors));
    }

    [Fact]
    public void Unknown_prompt_ids_are_a_programming_error()
    {
        var library = PromptLibrary.Load([], ModelKeys);

        Assert.Throws<InvalidOperationException>(() => library.Get("a9-nope"));
    }

    [Fact]
    public void The_shipped_prompts_folder_loads_and_skips_its_readme()
    {
        // The API's build output carries prompts/*.md; this guards every prompt added later.
        var library = PromptLibrary.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "prompts"), ModelKeys);

        Assert.DoesNotContain(library.All, p => p.Id.Equals("README", StringComparison.OrdinalIgnoreCase));
    }
}
