using System.Text.Json.Serialization;

namespace CreateBatchFilesForXbox360XBLAGames.Models;

public class GitHubRelease
{
    [JsonPropertyName("tag_name")] public string? TagName { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
}