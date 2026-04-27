using CodeReviewer.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewer.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewRepository _repo;

    public ReviewsController(IReviewRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _repo.CountAsync(ct);
        var items = await _repo.ListAsync((page - 1) * pageSize, pageSize, ct);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items = items.Select(r => new
            {
                r.Id,
                r.RepoOwner,
                r.RepoName,
                r.PrNumber,
                r.PrTitle,
                r.PrUrl,
                r.Score,
                r.CommentCount,
                r.CreatedAt
            })
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var review = await _repo.GetAsync(id, ct);
        return review is null ? NotFound() : Ok(review);
    }
}
