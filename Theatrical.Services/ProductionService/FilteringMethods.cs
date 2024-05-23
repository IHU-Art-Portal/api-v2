using System.Globalization;
using Theatrical.Data.Models;
using Theatrical.Services.Curators;
using Microsoft.Extensions.Logging;

namespace Theatrical.Services.ProductionService;

public interface IProductionFilteringMethods
{
    List<Production> TitleFiltering(List<Production> productions, string? title);

    List<Production> ProducerFiltering(List<Production> productions, string? producer);
}

public class ProductionFilteringMethods : IProductionFilteringMethods
{
    private readonly ILogger<ProductionFilteringMethods> _logger;

    public ProductionFilteringMethods(ILogger<ProductionFilteringMethods> logger)
    {
        _logger = logger;
    }

    public List<Production> TitleFiltering(List<Production> productions, string? title)
    {
        if (title == null)
        {
            _logger.LogInformation("No title filter applied, returning all productions.");
            return productions;
        }

        var normalizedTitle = StringUtilities.RemoveDiacritics(title).ToLowerInvariant();
        _logger.LogInformation("Normalized title filter: {NormalizedTitle}", normalizedTitle);

        var filteredProductions = productions
            .Where(p => p.Title != null && StringUtilities.RemoveDiacritics(p.Title).ToLowerInvariant().Contains(normalizedTitle))
            .ToList();

        _logger.LogInformation("Filtered productions count: {Count}", filteredProductions.Count);

        return filteredProductions;
    }

    public List<Production> ProducerFiltering(List<Production> productions, string? producer)
    {
        if (producer == null)
        {
            _logger.LogInformation("No producer filter applied, returning all productions.");
            return productions;
        }

        var normalizedProducer = StringUtilities.RemoveDiacritics(producer).ToLowerInvariant();
        _logger.LogInformation("Normalized producer filter: {NormalizedProducer}", normalizedProducer);

        var filteredProductions = productions
            .Where(p => p.Producer != null && StringUtilities.RemoveDiacritics(p.Producer).ToLowerInvariant().Contains(normalizedProducer))
            .ToList();

        _logger.LogInformation("Filtered productions count: {Count}", filteredProductions.Count);

        return filteredProductions;
    }
}
