using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using LinkShortCutService.Models;

namespace LinkShortCutService.Services.Interfaces;

public interface ILinkManager
{
    ValueTask<string> AddAsync(string Url, string? Name, string? Description, CancellationToken Cancel = default);

    ValueTask<UrlHashModel?> FindByHashAsync(string Hash, CancellationToken Cancel = default);

    ValueTask<UrlHashModel?> FindByUrlAsync(string Url, CancellationToken Cancel = default);

    ValueTask<int> GetCountAsync(CancellationToken Cancel = default);

    IAsyncEnumerable<UrlHashModel> GetAllAsync(int Skip = 0, int Take = -1, CancellationToken Cancel = default);

    ValueTask<UrlHashModel?> DeleteByHashAsync(string Hash, CancellationToken Cancel = default);

    ValueTask<UrlHashModel?> DeleteByUrlAsync(string Url, CancellationToken Cancel = default);

    ValueTask<UrlHashModel?> GetAsync(string Hash, CancellationToken Cancel = default);
}
