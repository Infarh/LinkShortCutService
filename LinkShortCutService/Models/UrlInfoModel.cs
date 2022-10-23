using System.ComponentModel.DataAnnotations;

namespace LinkShortCutService.Models;

public record UrlInfoModel
(
    [DataType(DataType.Url)]
    [Display(Name = "Адрес", Prompt = "www.ya.ru")]
    [Required, MinLength(3)]
    string Url,
    [Display(Name = "Название", Prompt = "Яндекс")]
    string? Name = null,
    [Display(Name = "Описание", Prompt = "Введите описание")]
    string? Description = null
);
