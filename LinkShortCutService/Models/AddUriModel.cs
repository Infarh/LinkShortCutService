namespace LinkShortCutService.Models;

public record UrlInfoModel(string Url, string? Name = null, string? Description = null);

public record UrlHashModel(string Url, string Hash, string? Name = null, string? Description = null) : UrlInfoModel(Url, Name, Description);
