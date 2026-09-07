namespace CreateBatchFilesForXbox360XBLAGames.Models;

public class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public string? ReleaseUrl { get; set; }
}