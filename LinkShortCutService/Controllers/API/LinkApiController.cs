using System.ComponentModel.DataAnnotations;
using LinkShortCutService.Models;
using LinkShortCutService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortCutService.Controllers.API;

[ApiController, Route("api/link")]
public class LinkApiController : ControllerBase
{
    private readonly ILinkManager _Manager;
    private readonly ILogger<LinkApiController> _Logger;

    public LinkApiController(ILinkManager Manager, ILogger<LinkApiController> Logger)
    {
        _Manager = Manager;
        _Logger       = Logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UrlInfoModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Add(UrlInfoModel Model, CancellationToken Cancel)
    {
        var hash = await _Manager.AddAsync(Model.Url, Model.Name, Model.Description, Cancel);
        return CreatedAtAction(nameof(HashInfo), new { Hash = hash }, Model);
    }

    [HttpGet("hash/{Hash}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HashInfo([MinLength(5)] string Hash, CancellationToken Cancel)
    {
        if (await _Manager.FindByHashAsync(Hash, Cancel) is { } info)
            return Ok(info);

        return NotFound(new { Hash });
    }

    [HttpGet("url/{Url}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UrlInfo([MinLength(3)] string Url, CancellationToken Cancel)
    {
        if (await _Manager.FindByUrlAsync(Url, Cancel) is { } info)
            return Ok(info);

        return NotFound(new { Url });
    }

    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount(CancellationToken Cancel)
    {
        var count = await _Manager.GetCountAsync(Cancel);
        return Ok(count);
    }

    [HttpGet]
    [HttpGet("({Skip:int}:{Take:int})")]
    [ProducesResponseType(typeof(UrlHashModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UrlHashModel[]), StatusCodes.Status204NoContent)]
    public IActionResult GetAll(int Skip = 0, int Take = -1, CancellationToken Cancel = default)
    {
        if (Take == 0)
            return NoContent();

        var items = _Manager.GetAllAsync(Skip, Take, Cancel);

        return Ok(items);
    }

    [HttpDelete("delete/hash/{Hash}")]
    [HttpPost("delete/hash/{Hash}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHash([MinLength(5)] string Hash, CancellationToken Cancel)
    {
        try
        {
            if(await _Manager.DeleteByHashAsync(Hash, Cancel) is { } link)
                return Ok(link);

            return NotFound(new { Hash });
        }
        catch (InvalidOperationException e)
        {
            _Logger.LogWarning(e, "Ошибка при попытке удаления записи по хешу {0}", Hash);
            return BadRequest(new { e.Message });
        }
    }

    [HttpDelete("{Url}")]
    [HttpPost("delete/{Url}")]
    [HttpPost("delete/url/{Url}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUrl([MinLength(3)] string Url, CancellationToken Cancel)
    {
        try
        {
            if(await _Manager.DeleteByUrlAsync(Url, Cancel) is { } link)
                return Ok(link);

            return NotFound(new { Url });
        }
        catch (InvalidOperationException e)
        {
            _Logger.LogWarning(e, "Ошибка при попытке удаления записи по url {0}", Url);
            return BadRequest(new { e.Message });
        }
    }
}
