using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using LinkShortCutService.Data.Context;
using LinkShortCutService.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using LinkShortCutService.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkShortCutService.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _Logger;

    public HomeController(ILogger<HomeController> Logger) => _Logger = Logger;

    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [HttpGet]
    [Route("/l/{Hash}")]
    [Route("/link/{Hash}")]
    [Route("/url/{Hash}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectTo([StringLength(250, MinimumLength = 5)] string Hash, [FromServices] ContextDB db, CancellationToken Cancel) =>
        await db.Links.Where(l => l.Hash.StartsWith(Hash)).ToArrayAsync(Cancel) switch
        {
            { Length: 0 }       => NotFound(),
            { Length: > 1 }     => BadRequest(),
            [ { Url: var url }] => Redirect(url),
            _                   => BadRequest()
        };
}
