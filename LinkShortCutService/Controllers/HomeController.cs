using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LinkShortCutService.Models;
using LinkShortCutService.Services.Interfaces;

namespace LinkShortCutService.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _Logger;

    public HomeController(ILogger<HomeController> Logger) => _Logger = Logger;

    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [HttpGet]
    [Route("/l={Hash}")]
    [Route("/l/{Hash}")]
    [Route("/link/{Hash}")]
    [Route("/url/{Hash}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectTo([StringLength(250, MinimumLength = 5)] string Hash, [FromServices] ILinkManager Manager, CancellationToken Cancel)
    {
        try
        {
            if (await Manager.GetAsync(Hash, Cancel) is { Url: var url })
                return Redirect(url);

            return NotFound();
        }
        catch (InvalidOperationException e)
        {
            _Logger.LogWarning(e, "Ошибка при попытке выполнения перехода по ссылке {0}", Hash);
            return BadRequest(new { e.Message });
        }
    }
}
