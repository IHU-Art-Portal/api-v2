namespace Theatrical.Dto.PersonDtos;

public class SearchFilters
{
    public bool? ShowAvailableAccounts { get; set; }
    public bool? AlphabeticalOrder { get; set; }
    public string? Role { get; set; }
    public int? Age { get; set; }
    public string? Height { get; set; }
    public string? Weight { get; set; }
    public string? EyeColor { get; set; }
    public string? HairColor { get; set; }
    public string? LanguageKnowledge { get; set; }

    public string? Name { get; set; }
}