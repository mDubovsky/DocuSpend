using DocuSpend.Categorization.Model;

namespace DocuSpend.Categorization.BuiltIn;

/// <summary>
/// The built-in categories from the spec. Names are Slovak (decision 0003) and may be corrected or extended later;
/// identifiers are fixed English keys and never change (decision 0004). Every category except the two Uncategorized
/// buckets gets an "Ostatné" sub-category, with the identifier "&lt;category&gt;.other".
/// </summary>
public static class BuiltInCatalog
{
    public static CategoryCatalog Create() => new(
    [
        Spending("housing", "Bývanie", ("rent", "Nájom"), ("equipment", "Vybavenie a doplnky"), ("mortgage", "Úvery na bývanie")),
        Spending("online-shopping", "Nákupy online", ("web-services", "Webové služby")),
        Spending("food", "Stravovanie", ("groceries", "Potraviny"), ("restaurants", "Reštaurácie a kaviarne")),
        Spending("car", "Auto-moto", ("fuel", "Pohonné hmoty"), ("service", "Servis"), ("vehicle-purchase", "Nákup vozidla")),
        Spending("other-expenses", "Ostatné výdavky", ("financing", "Splátky a financovanie"), ("insurance", "Platby poistného")),
        Spending("electronics", "Elektronika, komunikácia a médiá", ("phone", "Účty za telefón"), ("electronics", "Elektronika"), ("tv-radio", "TV a rádio")),
        Spending("health", "Zdravie a osobná starostlivosť", ("drugstore", "Drogéria"), ("doctors", "Lekári a nemocnice")),
        Spending("transport", "Doprava", ("taxi", "Taxi"), ("public", "Verejná doprava")),
        Spending("education", "Vzdelanie", ("courses", "Kurzy a doučovanie"), ("school", "Škola a škôlka"), ("supplies", "Školské potreby")),
        Spending("fees", "Poplatky a úroky", ("fees", "Poplatky"), ("interest", "Úroky")),
        Spending("leisure", "Zábava a oddych", ("games", "Hry a hračky")),
        Make("income", "Príjmy", CategoryRole.Income, ("salary", "Mzda"), ("reimbursements", "Náhrady výdavkov"), ("benefits", "Dávky a prídavky")),
        Make("own-transfers", "Prevody medzi vlastnými účtami", CategoryRole.Excluded),
        Make("investments", "Investície", CategoryRole.Excluded),
        new Category(new CategoryId("uncategorized-out"), "Nezaradené výdavky", CategoryRole.Spending, [], IsBuiltIn: true, IsUncategorized: true),
        new Category(new CategoryId("uncategorized-in"), "Nezaradené príjmy", CategoryRole.Income, [], IsBuiltIn: true, IsUncategorized: true),
    ]);

    private static Category Spending(string id, string name, params (string Key, string Name)[] subcategories) =>
        Make(id, name, CategoryRole.Spending, subcategories);

    private static Category Make(string id, string name, CategoryRole role, params (string Key, string Name)[] subcategories)
    {
        var subs = subcategories
            .Select(s => new Subcategory(new SubcategoryId($"{id}.{s.Key}"), s.Name))
            .Append(new Subcategory(new SubcategoryId($"{id}.other"), CategoryCatalog.OtherName))
            .ToList();
        return new Category(new CategoryId(id), name, role, subs, IsBuiltIn: true);
    }
}
