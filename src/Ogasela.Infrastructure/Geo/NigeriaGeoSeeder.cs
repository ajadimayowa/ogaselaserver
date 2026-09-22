using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Geo;

namespace Ogasela.Infrastructure.Geo;

/// <summary>
/// Seeds the 36 Nigerian states + FCT, each with a handful of starter cities, so the Geo
/// reference-data module isn't empty out of the box. Idempotent (upsert-by-Name for states,
/// upsert-by-Name-within-state for cities) and safe to run on every startup, same as RbacSeeder.
/// </summary>
public static class NigeriaGeoSeeder
{
    private static readonly (string Name, string Code, string[] Cities)[] States =
    [
        ("Abia", "AB", ["Umuahia", "Aba", "Ohafia", "Arochukwu"]),
        ("Adamawa", "AD", ["Yola", "Mubi", "Numan", "Jimeta"]),
        ("Akwa Ibom", "AK", ["Uyo", "Eket", "Ikot Ekpene", "Oron"]),
        ("Anambra", "AN", ["Awka", "Onitsha", "Nnewi", "Ekwulobia"]),
        ("Bauchi", "BA", ["Bauchi", "Azare", "Misau", "Jama'are"]),
        ("Bayelsa", "BY", ["Yenagoa", "Brass", "Ogbia", "Sagbama"]),
        ("Benue", "BE", ["Makurdi", "Gboko", "Otukpo", "Katsina-Ala"]),
        ("Borno", "BO", ["Maiduguri", "Bama", "Dikwa", "Gwoza"]),
        ("Cross River", "CR", ["Calabar", "Ikom", "Ogoja", "Obudu"]),
        ("Delta", "DE", ["Asaba", "Warri", "Sapele", "Ughelli"]),
        ("Ebonyi", "EB", ["Abakaliki", "Afikpo", "Onueke", "Ezza"]),
        ("Edo", "ED", ["Benin City", "Auchi", "Ekpoma", "Uromi"]),
        ("Ekiti", "EK", ["Ado-Ekiti", "Ikere-Ekiti", "Ijero-Ekiti", "Oye"]),
        ("Enugu", "EN", ["Enugu", "Nsukka", "Awgu", "Agbani"]),
        ("FCT", "FC", ["Abuja", "Gwagwalada", "Kuje", "Bwari"]),
        ("Gombe", "GO", ["Gombe", "Kumo", "Billiri", "Dukku"]),
        ("Imo", "IM", ["Owerri", "Orlu", "Okigwe", "Mbaise"]),
        ("Jigawa", "JI", ["Dutse", "Hadejia", "Gumel", "Birnin Kudu"]),
        ("Kaduna", "KD", ["Kaduna", "Zaria", "Kafanchan", "Kagoro"]),
        ("Kano", "KN", ["Kano", "Wudil", "Gwarzo", "Rano"]),
        ("Katsina", "KT", ["Katsina", "Funtua", "Daura", "Malumfashi"]),
        ("Kebbi", "KE", ["Birnin Kebbi", "Argungu", "Yauri", "Zuru"]),
        ("Kogi", "KO", ["Lokoja", "Okene", "Idah", "Kabba"]),
        ("Kwara", "KW", ["Ilorin", "Offa", "Omu-Aran", "Jebba"]),
        ("Lagos", "LA", ["Ikeja", "Lagos Island", "Lekki", "Badagry"]),
        ("Nasarawa", "NA", ["Lafia", "Keffi", "Akwanga", "Nasarawa"]),
        ("Niger", "NI", ["Minna", "Bida", "Kontagora", "Suleja"]),
        ("Ogun", "OG", ["Abeokuta", "Sagamu", "Ijebu-Ode", "Ota"]),
        ("Ondo", "ON", ["Akure", "Ondo City", "Owo", "Ikare"]),
        ("Osun", "OS", ["Osogbo", "Ile-Ife", "Ilesa", "Ede"]),
        ("Oyo", "OY", ["Ibadan", "Ogbomoso", "Oyo Town", "Iseyin"]),
        ("Plateau", "PL", ["Jos", "Bukuru", "Pankshin", "Shendam"]),
        ("Rivers", "RI", ["Port Harcourt", "Bonny", "Okrika", "Ahoada"]),
        ("Sokoto", "SO", ["Sokoto", "Wurno", "Tambuwal", "Gwadabawa"]),
        ("Taraba", "TA", ["Jalingo", "Wukari", "Bali", "Gembu"]),
        ("Yobe", "YO", ["Damaturu", "Potiskum", "Gashua", "Nguru"]),
        ("Zamfara", "ZA", ["Gusau", "Kaura Namoda", "Talata Mafara", "Anka"])
    ];

    public static async Task SeedAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingStateNames = await dbContext.NigeriaStates.Select(s => s.Name).ToListAsync(cancellationToken);

        foreach (var (name, code, _) in States)
        {
            if (!existingStateNames.Contains(name))
            {
                dbContext.NigeriaStates.Add(NigeriaState.Create(name, code));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var stateIdsByName = await dbContext.NigeriaStates.ToDictionaryAsync(s => s.Name, s => s.Id, cancellationToken);

        foreach (var (name, _, cities) in States)
        {
            var stateId = stateIdsByName[name];
            var existingCityNames = await dbContext.NigeriaCities
                .Where(c => c.StateId == stateId)
                .Select(c => c.Name)
                .ToListAsync(cancellationToken);

            foreach (var cityName in cities)
            {
                if (!existingCityNames.Contains(cityName))
                {
                    dbContext.NigeriaCities.Add(NigeriaCity.Create(stateId, cityName));
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
