using CodeReviewer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeReviewer.Api.Controllers;

[ApiController]
[Route("healthz")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnect = await _db.Database.CanConnectAsync(ct);
        return canConnect ? Ok(new { status = "ok" }) : StatusCode(503, new { status = "db unreachable" });
    }
}
