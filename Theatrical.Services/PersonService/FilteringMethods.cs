using Theatrical.Data.enums;
using Theatrical.Data.Models;
using Theatrical.Services.Curators;

namespace Theatrical.Services.PersonService;

public interface IFilteringMethods
{
    List<Person> LanguageFiltering(List<Person> people, string? languageKnowledge);
    List<Person> HairColorFiltering(List<Person> people, string? hairColor);
    List<Person> EyeColorFiltering(List<Person> people, string? eyeColor);
    List<Person> WeightFiltering(List<Person> people, string? weight);
    List<Person> ClaimingStatusOrdering(List<Person> people, bool? showAvailableAccounts);
    List<Person> AlphabeticalOrdering(List<Person> people, bool? order);
    List<Person> RoleFiltering(List<Person> people, string? role);
    List<Person> AgeFiltering(List<Person> people, int? age);
    List<Person> HeightFiltering(List<Person> people, string? height);
    List<Person> NameFiltering(List<Person> people, string? name);

}

public class FilteringMethods : IFilteringMethods
{
    public List<Person> LanguageFiltering(List<Person> people, string? languageKnowledge)
    {
        if (languageKnowledge is null) return people;

        var filteredPeople = people
            .Where(p => p.Languages != null && p.Languages.Contains(languageKnowledge, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return filteredPeople;
    }

    public List<Person> HairColorFiltering(List<Person> people, string? hairColor)
    {
        if (hairColor is null) return people;

        var filteredPeople = people
            .Where(p => p.HairColor != null && p.HairColor.Contains(hairColor, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return filteredPeople;
    }

    public List<Person> EyeColorFiltering(List<Person> people, string? eyeColor)
    {
        if (eyeColor is not null)
        {
            var filteredPeople = people
                .Where(p => p.EyeColor != null && p.EyeColor.Contains(eyeColor, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return filteredPeople;
        }

        return people;
    }

    public List<Person> WeightFiltering(List<Person> people, string? weight)
    {
        if (weight is not null)
        {
            var filteredPeople = people
                .Where(p => p.Weight != null && p.Weight.Equals(weight, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return filteredPeople;
        }

        return people;
    }

    public List<Person> ClaimingStatusOrdering(List<Person> people, bool? showAvailableAccounts)
    {
        ClaimingStatus? claimingStatus = showAvailableAccounts switch
        {
            true => ClaimingStatus.Available, 
            false => ClaimingStatus.Unavailable,
            _ => null
        };
        
        if (claimingStatus is not null)
        {
            var claimingStatusFiltered = people.Where(p => p.ClaimingStatus == claimingStatus).ToList();
            return claimingStatusFiltered;
        }
        
        return people;
    }

    //Makes the list in alphabetical order.
    public List<Person> AlphabeticalOrdering(List<Person> people, bool? order)
    {
        if (order == true)
        {
            var sortedPeople = people
                .OrderBy(p => p.Fullname, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            return sortedPeople;
        }

        return people;
    }

    public List<Person> RoleFiltering(List<Person> people, string? role)
{
    if (!string.IsNullOrEmpty(role))
    {
        var allRoles = role.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (allRoles.Any())
        {
            var filteredPeople = people
                .Where(person => person.Roles != null &&
                                 person.Roles.Any(r => allRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            return filteredPeople;
        }
    }
    return people;
}


    public List<Person> AgeFiltering(List<Person> people, int? age)
    {
        if (age.HasValue)
        {
            // Calculate the birthdate range based on the specified age
            var maxBirthdate = DateTime.Today.AddYears(-age.Value);
            var minBirthdate = DateTime.Today.AddYears(-(age.Value + 1));

            var filteredPeople = people
                .Where(p => p.Birthdate.HasValue && p.Birthdate.Value <= maxBirthdate && p.Birthdate.Value > minBirthdate)
                .ToList();

            return filteredPeople;
        }

        return people;
    }

    public List<Person> HeightFiltering(List<Person> people, string? height)
    {
        if (height is not null)
        {
            var filteredPeople = people
                .Where(p => p.Height != null && p.Height.Contains(height))
                .ToList();

            return filteredPeople;
        }

        return people;
    }


    public List<Person> NameFiltering(List<Person> people, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedSearchName = StringUtilities.RemoveDiacritics(name).ToLowerInvariant();

            var filteredPeople = people
                .Where(p => p.Fullname != null && StringUtilities.RemoveDiacritics(p.Fullname).ToLowerInvariant().Contains(normalizedSearchName))
                .ToList();

            Console.WriteLine($"Filtered by name '{name}': {filteredPeople.Count} results.");
            return filteredPeople;
        }

        return people;
    }

}