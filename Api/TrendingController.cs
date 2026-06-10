using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JellyfinTrending.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JellyfinTrending.Api;

/// <summary>Trending content API.</summary>
[ApiController]
[Route("Trending")]
public class TrendingController : ControllerBase
{
    private readonly TrendingRepository _repository;

    /// <summary>Initializes a new instance of <see cref="TrendingController"/>.</summary>
    public TrendingController(TrendingRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Returns trending items across all users.
    /// </summary>
    /// <param name="type">Filter by type: Movie, Series, or omit for all.</param>
    /// <param name="days">Number of days to look back (default: 7).</param>
    /// <param name="limit">Max items to return (default: 20).</param>
    [HttpGet("Items")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<TrendingItem>> GetTrendingItems(
        [FromQuery] string? type = null,
        [FromQuery] int days = 7,
        [FromQuery] int limit = 20)
    {
        var items = _repository.GetTrending(type, days, limit);
        return Ok(items);
    }

    /// <summary>
    /// Serves the trending page HTML. No auth required — the page handles auth itself via JS.
    /// </summary>
    [HttpGet("Page")]
    [AllowAnonymous]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "JellyfinTrending.Web.trending.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return NotFound("Trending page resource not found.");

        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), "text/html");
    }
}
