using System.Text;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using LinkShortCutService.Data.Context;
using LinkShortCutService.Options;
using LinkShortCutService.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using LinkShortCutService.Models;
using Microsoft.EntityFrameworkCore;
using LinkShortCutService.Data.Entities;

namespace LinkShortCutService.Services;

public class DbLinkManager : ILinkManager
{
    private static readonly Regex __RegexUrlSchemePrefix = new(@"^(?<scheme>[A-z]+)://", RegexOptions.Compiled);

    private readonly ContextDB              _db;
    private readonly ILogger<DbLinkManager> _Logger;

    private readonly string _EncodingName;
    private readonly int    _HashLength;
    private readonly string _HashAlgorithmName;
    private readonly int    _ConcurrentTryCount;
    private readonly int    _ConcurrentTimeout;

    public DbLinkManager(ContextDB db, IOptions<LinkManagerOptions> Options, ILogger<DbLinkManager> Logger)
    {
        _db     = db;
        _Logger = Logger;

        (
            _HashLength,
            _HashAlgorithmName,
            _EncodingName,
            _ConcurrentTimeout,
            _ConcurrentTryCount
        ) = Options.Value;
    }

    public async ValueTask<int> GetCountAsync(CancellationToken Cancel = default)
    {
        var count = await _db.Links.CountAsync(Cancel).ConfigureAwait(false);
        return count;
    }

    public async ValueTask<string> AddAsync(string Url, string? Name, string? Description, CancellationToken Cancel = default)
    {
        if (!__RegexUrlSchemePrefix.IsMatch(Url))
            Url = "http://" + Url;

        var encoding = Encoding.GetEncoding(_EncodingName);
        var bytes    = new MemoryStream(encoding.GetBytes(Url));

        var hasher = HashAlgorithm.Create(_HashAlgorithmName)
            ?? throw new InvalidOperationException($"Не удалось получить алгоритм хеширования {_HashAlgorithmName}");
        var hash_bytes = await hasher.ComputeHashAsync(bytes, Cancel).ConfigureAwait(false);
        var hash       = Convert.ToBase64String(hash_bytes);

        if (await _db.Links.FirstOrDefaultAsync(l => l.Hash == hash, Cancel) is { } link)
            _Logger.LogInformation("Запись о {0} существует в БД с хешем {1}.", Url, link.Hash);
        else
        {
            link = new()
            {
                Url         = Url,
                Hash        = hash,
                Name        = Name,
                Description = Description
            };
            _db.Add(link);
            await _db.SaveChangesAsync(Cancel);

            _Logger.LogInformation("Запись о {0} добавлена в БД с хешем {1}", Url, hash);
        }

        return hash[..Math.Min(hash.Length, _HashLength)];
    }

    public async ValueTask<UrlHashModel?> FindByHashAsync(string Hash, CancellationToken Cancel = default)
    {
        if (await _db.Links.FirstOrDefaultAsync(l => l.Hash.StartsWith(Hash), Cancel).ConfigureAwait(false) is
        {
            Url        : var url,
            Hash       : var hash,
            Name       : var name,
            Description: var description
        })
            return new UrlHashModel(url, hash, name, description);

        return null;
    }

    public async ValueTask<UrlHashModel?> FindByUrlAsync(string Url, CancellationToken Cancel = default)
    {
        if (!__RegexUrlSchemePrefix.IsMatch(Url))
            Url = "http://" + Url;

        if (await _db.Links.FirstOrDefaultAsync(l => l.Url == Url, Cancel).ConfigureAwait(false) is
        {
            Url        : var url,
            Hash       : var hash,
            Name       : var name,
            Description: var description
        })
            return new UrlHashModel(url, hash, name, description);

        return null;
    }

    public async IAsyncEnumerable<UrlHashModel> GetAllAsync(int Skip = 0, int Take = -1, [EnumeratorCancellation] CancellationToken Cancel = default)
    {
        if (Take == 0)
            yield break;

        IQueryable<Link> query = _db.Links.OrderBy(l => l.Id);

        if (Skip > 0)
            query = query.Skip(Skip);

        if (Take > 0)
            query = query.Take(Take);

        await foreach (var (url, hash, name, description) in query.AsAsyncEnumerable().WithCancellation(Cancel))
            yield return new(url, hash, name, description);
    }

    public async ValueTask<UrlHashModel?> DeleteByHashAsync(string Hash, CancellationToken Cancel = default)
    {
        switch (await _db.Links.Where(l => l.Hash.StartsWith(Hash)).ToArrayAsync(Cancel))
        {
            default: throw new InvalidOperationException("Неизвестная ошибка");

            case { Length: > 1 and var Count }: throw new InvalidOperationException($"По указанному хешу {Hash} найдено {Count} записей");
            case { Length: 0 }:                 return null;

            case [ { Url: var url, Hash: var hash, Name: var name, Description: var description } db_link]:
                _db.Remove(db_link);
                await _db.SaveChangesAsync(Cancel);
                _Logger.LogInformation("Запись {0} удалена", db_link);

                return new(url, hash, name, description);
        }
    }

    public async ValueTask<UrlHashModel?> DeleteByUrlAsync(string Url, CancellationToken Cancel = default)
    {
        Url = Regex.IsMatch(Url, "^[A-z]+://")
            ? Url
            : "http://" + Url;

        switch (await _db.Links.Where(l => l.Url == Url).ToArrayAsync(Cancel))
        {
            default: throw new InvalidOperationException("Неизвестная ошибка");

            case { Length: > 1 and var Count }: throw new InvalidOperationException($"По указанному url {Url} найдено {Count} записей");
            case { Length: 0 }:                 return null;

            case [{ Url: var url, Hash: var hash, Name: var name, Description: var description } db_link]:
                _db.Remove(db_link);
                await _db.SaveChangesAsync(Cancel);
                _Logger.LogInformation("Запись {0} удалена", db_link);

                return new (url, hash, name, description);
        }
    }

    public async ValueTask<UrlHashModel?> GetAsync(string Hash, CancellationToken Cancel = default)
    {
        var query = _db.Links.Where(l => l.Hash.StartsWith(Hash));

        var count = await query.CountAsync(Cancel).ConfigureAwait(false);

        switch (count)
        {
            default: throw new InvalidOperationException("Неизвестная ошибка");

            case > 1: throw new InvalidOperationException($"Неоднозначный запрос для хеша {Hash}");
            case 0:   return null;

            case 1:
                for(var (i, try_count) = (0, _ConcurrentTryCount); i < try_count; i++)
                    try
                    {
                        var link = await query.FirstAsync(Cancel);
                        link.LastAccess = DateTimeOffset.Now;
                        link.AccessCount++;
                        _db.Update(link);
                        await _db.SaveChangesAsync(Cancel);

                        return new(link.Url, link.Hash, link.Name, link.Description);
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        _Logger.LogWarning(
                            "Ошибка {0} конкурентного доступа на запись к БД при обновлении записи с hash: {1}", 
                            i + 1, Hash);

                        await Task.Delay(_ConcurrentTimeout, Cancel);
                    }

                throw new InvalidOperationException();
        }
    }
}
