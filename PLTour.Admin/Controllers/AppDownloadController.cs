using Microsoft.AspNetCore.Mvc;

namespace PLTour.Admin.Controllers;

[Route("app/download")]
public class AppDownloadController : Controller
{
    private readonly IConfiguration _configuration;

    public AppDownloadController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var versionSection = _configuration.GetSection("AppUpdate");
        var currentVersion = versionSection["CurrentVersion"] ?? "1.0.0";
        var releaseNotes = versionSection["ReleaseNotes"] ?? "Đang cập nhật thông tin phát hành.";

        ViewBag.CurrentVersion = currentVersion;
        ViewBag.ReleaseNotes = releaseNotes;
        ViewBag.GitHubReleasesUrl = _configuration.GetSection("AppUpdate")["GitHubReleasesUrl"] ?? "";

        return View();
    }

    [HttpGet("version")]
    public IActionResult Version()
    {
        var versionSection = _configuration.GetSection("AppUpdate");
        var currentVersion = versionSection["CurrentVersion"] ?? "1.0.0";
        var releaseNotes = versionSection["ReleaseNotes"] ?? "Đang cập nhật thông tin phát hành.";

        return Ok(new
        {
            version = currentVersion,
            githubReleasesUrl = versionSection["GitHubReleasesUrl"] ?? string.Empty,
            releaseNotes,
            releasedAtUtc = DateTime.UtcNow
        });
    }
}
