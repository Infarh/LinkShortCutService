using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LinkShortCutService.Data.Context;
using LinkShortCutService.Data.Entities;
using LinkShortCutService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkShortCutService.Controllers.API;

[ApiController, Route("api/link")]
public class LinkApiController : ControllerBase
{
    private readonly ContextDB _db;
    private readonly ILogger<LinkApiController> _Logger;

    public LinkApiController(ContextDB db, ILogger<LinkApiController> Logger)
    {
        _db     = db;
        _Logger = Logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UrlInfoModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Add(UrlInfoModel Model, CancellationToken Cancel)
    {
        var (url, name, description) = Model;

        if (!Regex.IsMatch(url, @"^[A-z]+://", RegexOptions.Compiled))
        {
            url   = "http://" + url;
            Model = Model with { Url = url };
        }

        var bytes = new MemoryStream(Encoding.UTF32.GetBytes(url));

        using var md5       = MD5.Create();
        var       md5_bytes = await md5.ComputeHashAsync(bytes, Cancel);
        var       hash      = Convert.ToBase64String(md5_bytes);

        if (await _db.Links.FirstOrDefaultAsync(l => l.Hash == hash, Cancel) is { } link)
            _Logger.LogInformation("Запись о {0} существует в БД с хешем {1}.", url, link.Hash);
        else
        {
            link = new()
            {
                Url         = url,
                Hash        = hash,
                Name        = name,
                Description = description
            };
            _db.Add(link);
            await _db.SaveChangesAsync(Cancel);

            _Logger.LogInformation("Запись о {0} добавлена в БД с хешем {1}", url, hash);
        }

        return CreatedAtAction(nameof(HashInfo), new { Hash = hash[..5] }, Model);
    }

    [HttpGet("hash/{Hash}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HashInfo([StringLength(maximumLength: 255, MinimumLength = 5)] string Hash, CancellationToken Cancel)
    {
        if (await _db.Links.FirstOrDefaultAsync(l => l.Hash.StartsWith(Hash), Cancel) is
        {
            Url        : var url,
            Hash       : var hash,
            Name       : var name,
            Description: var description
        })
            return Ok(new UrlHashModel(url, hash, name, description));

        return NotFound(new { Hash });
    }

    [HttpGet("url/{Url}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UrlInfo(string Url, CancellationToken Cancel)
    {
        var url = Regex.IsMatch(Url, "^[A-z]+://")
            ? Url
            : "http://" + Url;

        if (await _db.Links.FirstOrDefaultAsync(l => l.Url == url, Cancel) is
        {
            Hash       : var hash,
            Name       : var name,
            Description: var description
        })
            return Ok(new UrlHashModel(url, hash, name, description));

        return NotFound(new { Url });
    }

    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount(CancellationToken Cancel)
    {
        var count = await _db.Links.CountAsync(Cancel);
        return Ok(count);
    }

    [HttpGet]
    [HttpGet("({Skip:int}:{Take:int})")]
    [ProducesResponseType(typeof(UrlHashModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UrlHashModel[]), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAll(int Skip = 0, int Take = -1, CancellationToken Cancel = default)
    {
        if (Take == 0)
            return NoContent();

        IQueryable<Link> query = _db.Links.OrderBy(l => l.Id);

        if (Skip > 0)
            query = query.Skip(Skip);

        if (Take > 0)
            query = query.Take(Take);

        var items = await query
           .Select(l => new UrlHashModel(l.Url, l.Hash, l.Name, l.Description))
           .ToArrayAsync(Cancel);

        return Ok(items);
    }

    [HttpDelete("delete/hash/{Hash}")]
    [HttpPost("delete/hash/{Hash}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHash([StringLength(250, MinimumLength = 5)] string Hash, CancellationToken Cancel)
    {
        switch (await _db.Links.Where(l => l.Hash.StartsWith(Hash)).ToArrayAsync(Cancel))
        {
            default:                            return BadRequest();

            case { Length: > 1 and var Count }: return BadRequest(new { Count });
            case { Length:   0               }: return NotFound(new { Hash });

            case [ { Url: var url, Hash: var hash, Name: var name, Description: var description } db_link ]:
                _db.Remove(db_link);
                await _db.SaveChangesAsync(Cancel);
                _Logger.LogInformation("Запись {0} удалена", db_link);
                return Ok(new UrlHashModel(url, hash, name, description));
        }
    }

    [HttpDelete("{Url}")]
    [HttpPost("delete/{Url}")]
    [HttpPost("delete/url/{Url}")]
    [ProducesResponseType(typeof(UrlHashModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUrl(string Url, CancellationToken Cancel)
    {
        Url = Regex.IsMatch(Url, "^[A-z]+://")
            ? Url
            : "http://" + Url;

        switch (await _db.Links.Where(l => l.Url == Url).ToArrayAsync(Cancel))
        {
            default:                            return BadRequest();

            case { Length: > 1 and var Count }: return BadRequest(new { Count });
            case { Length:   0               }: return NotFound(new { Url });

            case [ { Url: var url, Hash: var hash, Name: var name, Description: var description } db_link ]:
                _db.Remove(db_link);
                await _db.SaveChangesAsync(Cancel);
                _Logger.LogInformation("Запись {0} удалена", db_link);
                return Ok(new UrlHashModel(url, hash, name, description));
        }
    }
}
