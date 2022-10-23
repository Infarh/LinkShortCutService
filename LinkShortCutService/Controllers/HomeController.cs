using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LinkShortCutService.Models;
using LinkShortCutService.Services.Interfaces;

namespace LinkShortCutService.Controllers;

public class HomeController : Controller
{
    private readonly ILinkManager _Manager;
    private readonly ILogger<HomeController> _Logger;

    public HomeController(ILinkManager Manager, ILogger<HomeController> Logger)
    {
        _Manager = Manager;
        _Logger  = Logger;
    }

    public IActionResult Index(int Skip = 0, int Take = -1, CancellationToken Cancel = default) => 
        View(_Manager.GetAllAsync(Skip, Take, Cancel));

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
    public async Task<IActionResult> RedirectTo([MinLength(5)] string Hash, CancellationToken Cancel)
    {
        try
        {
            if (await _Manager.GetAsync(Hash, Cancel) is { Url: var url })
                return Redirect(url);

            return NotFound();
        }
        catch (InvalidOperationException e)
        {
            _Logger.LogWarning(e, "Ошибка при попытке выполнения перехода по ссылке {0}", Hash);
            return BadRequest(new { e.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLink(UrlInfoModel Link)
    {
        _ = await _Manager.AddAsync(Link.Url, Link.Name, Link.Description);

        return RedirectToAction(nameof(ViewList));
    }

    public IActionResult ViewList(CancellationToken Cancel)
    {
        var links = _Manager.GetAllAsync(Cancel: Cancel);

        return View(links);
    }
}
