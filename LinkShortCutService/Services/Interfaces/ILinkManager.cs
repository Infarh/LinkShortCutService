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
    Task<string> AddAsync(string Url, string? Name, string? Description, CancellationToken Cancel = default);
    Task<UrlHashModel?> FindByHashAsync(string Hash, CancellationToken Cancel = default);
    Task<UrlHashModel?> FindByUrlAsync(string Url, CancellationToken Cancel = default);
    Task<int> GetCountAsync(CancellationToken Cancel = default);
    IAsyncEnumerable<UrlHashModel> GetAllAsync(int Skip = 0, int Take = -1, CancellationToken Cancel = default);
    Task<UrlHashModel?> DeleteByHashAsync(string Hash, CancellationToken Cancel = default);
    Task<UrlHashModel?> DeleteByUrlAsync(string Url, CancellationToken Cancel = default);
    Task<UrlHashModel?> GetAsync(string Hash, CancellationToken Cancel = default);
}
