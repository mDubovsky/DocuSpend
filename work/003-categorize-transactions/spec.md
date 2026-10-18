# Spec: Sort transactions into spending categories

Intent: [intent.md](intent.md)

## Behavior

**Categorizing** takes a statement the reader has read ([work/002-statement-reader](../002-statement-reader/spec.md)), the categories, and the rules, and gives back a **categorized statement**:

- for every transaction, in the statement's order: its category and sub-category, whether it counts as spending, and which rule put it there (or that none did);
- for every sub-category, every category and the statement as a whole: how many transactions and what they add up to.

Categorizing never changes the statement, never fails, and gives the same result for the same statement and rules every time. It needs only its inputs; no network.

### Categories

Every category has a **role**:

| Role | Meaning |
|---|---|
| spending | what the money was spent on; counts in the spending totals |
| income | incoming payments; never counts as spending |
| excluded | not spending (moving money between the user's own accounts, investments, …); left out of the spending totals |

A category's role applies to everything under it. There are two levels: a category contains sub-categories, and a transaction is always in a sub-category. Every category has a sub-category named **Ostatné** ("other"). The only exceptions are the two **Uncategorized** buckets, which have no sub-categories; a transaction in one of them is in the bucket itself.

#### Built-in categories

Names are Slovak ([decision 0003](../../docs/decisions/0003-slovak-interface-english-code.md)). Every category below except the two Uncategorized buckets also has an Ostatné sub-category, which the table doesn't repeat.

| Category | Role | Sub-categories (besides Ostatné) |
|---|---|---|
| Bývanie | spending | Nájom; Vybavenie a doplnky; Úvery na bývanie |
| Nákupy online | spending | Webové služby |
| Stravovanie | spending | Potraviny; Reštaurácie a kaviarne |
| Auto-moto | spending | Pohonné hmoty; Servis; Nákup vozidla |
| Ostatné výdavky | spending | Splátky a financovanie; Platby poistného |
| Elektronika, komunikácia a médiá | spending | Účty za telefón; Elektronika; TV a rádio |
| Zdravie a osobná starostlivosť | spending | Drogéria; Lekári a nemocnice |
| Doprava | spending | Taxi; Verejná doprava |
| Vzdelanie | spending | Kurzy a doučovanie; Škola a škôlka; Školské potreby |
| Poplatky a úroky | spending | Poplatky; Úroky |
| Zábava a oddych | spending | Hry a hračky |
| Príjmy | income | Mzda; Náhrady výdavkov; Dávky a prídavky |
| Prevody medzi vlastnými účtami | excluded | none |
| Investície | excluded | none |
| Nezaradené výdavky | spending | none (Uncategorized for money out) |
| Nezaradené príjmy | income | none (Uncategorized for money in) |

More categories and sub-categories will be added to this set later; an addition changes the result only for transactions its rules match.

Built-in categories can't be deleted in the MVP, and their roles are fixed.

#### User-created categories

- A user can add a category, choosing its role (spending, income or excluded) and its name. DocuSpend adds its Ostatné sub-category.
- A user can add a sub-category to any category, built-in or their own.
- A name must not be empty and must differ from its siblings' names, ignoring case and diacritics; otherwise it is refused.

### Rules

A rule has **one condition**, a **target sub-category** and a **priority number**. It is either a **default rule** (built in) or a **user rule**.

#### Conditions

A condition matches a transaction or doesn't. A condition on a field the transaction doesn't have doesn't match it (for example a counterparty-name condition never matches a card payment).

| Condition | Matches when… |
|---|---|
| Transaction type | the type is any of the chosen ones: card payment, wallet card payment, transfer, instant transfer, account fee, fee reversal |
| Text | the transaction's description or its payment purpose satisfies the text test below |
| Counterparty name | the counterparty's name satisfies the text test below |
| Counterparty account | the counterparty's account number equals the given one, ignoring spaces and case |
| Amount | the size of the amount, ignoring whether it is money in or out, is at least the lower bound and at most the upper bound; either bound may be left out but not both, and the lower may not exceed the upper |
| Variable symbol | the variable symbol equals the given one exactly |

**Text test.** The user chooses a mode when creating the condition:

- *contains*: the transaction's text contains the given text;
- *equals*: the transaction's text, apart from surrounding spaces, is the given text.

In both modes case and diacritics are ignored, so "lekaren" matches "LEKÁREŇ", and runs of spaces count as one. In *contains* mode any punctuation also counts as a space, so "mol" matches "Mol.PetExpert" and "dr.max" matches "Dr.Max 382". The given text must contain at least one letter or digit.

#### Priority

- A priority number is a whole number from 1. **1 is the highest priority**: of two rules, the one with the lower number wins.
- User rules always win over default rules, whatever their numbers; each group has its own numbers.
- **User rules may share a priority number.** Of user rules with the same number, the **newest** (the one created last) wins, so the outcome is always the same. DocuSpend remembers the order in which user rules were created.
- **A rule can be added on top.** DocuSpend can add a user rule so that it beats every existing user rule: it gets the lowest priority number any user rule has now (1 if there are none), and being the newest, it wins the tie. The user doesn't choose a number. This is how a wrongly categorized transaction is corrected by adding a rule, and how a screen can hide priority numbers unless the user asks for them.
- Default rules have distinct numbers, given by their order in the table below.

#### A rule is refused when

- its condition is incomplete or not valid, as above;
- its target isn't an existing sub-category (a category without a sub-category can't be a target);
- its priority number isn't a whole number from 1.

### Deciding a transaction's category

1. The user rules are tried in priority order, and rules with the same number from the newest to the oldest. The first one whose condition matches decides.
2. If none matches, the default rules are tried in priority order. The first match decides.
3. If none matches, the transaction goes to **Nezaradené výdavky** if it is money going out and to **Nezaradené príjmy** otherwise. A zero amount counts as money coming in.

Any rule can target any sub-category: an incoming refund can go to Stravovanie, a reversed fee to Poplatky.

### What counts, and totals

Every transaction has a signed amount, negative for money going out. A statement's closing balance is its opening balance plus all its transactions' amounts.

- A **balance** of a sub-category is the sum of its transactions' amounts; a category's balance is the sum of its sub-categories' balances (an Uncategorized bucket's own).
- **Spending** of a spending category is the negative of its balance: money out minus money that came back into it. A fee and its reversal in the same sub-category cancel to zero. Spending is negative if more came back than went out.
- The **spending total** is the sum of the spending of all spending categories. **Income** is the sum of the balances of income categories. **Excluded** is the sum of the balances of excluded categories.
- A transaction contributes to spending exactly when it is in a spending category; if it is money coming in, it reduces spending.

Because each transaction is in exactly one sub-category, **nothing is lost**: the closing balance minus the opening balance equals income plus excluded minus the spending total.

### Default rules

A first set, listed in priority order (first match wins). Rows are lists of alternatives, and each alternative is its own rule with its own consecutive priority number, in the order shown. All text conditions are "contains" and, like every text condition, ignore case and diacritics. A space at either end of a text marks a word edge, also at the very start or end of the transaction's text, and punctuation counts as a space: " MOL " matches "MOL Bratislava", "Platba MOL" and "Mol.PetExpert" but not "DOMOL", and " COOP" matches "COOP Jednota" but not "SCOOP". The list is built from common Slovak merchants and services and will grow; adding a rule only changes the result for transactions it matches.

| Condition | Target |
|---|---|
| Type is account fee or fee reversal | Poplatky a úroky › Poplatky |
| Text "SPORIACI UCET" (savings account) | Prevody medzi vlastnými účtami › Ostatné |
| Text "NAJOM" (nájom, nájomné) | Bývanie › Nájom |
| Text "HYPOTEK" (hypotéka, hypotekárny) | Bývanie › Úvery na bývanie |
| Text " OBI ", "IKEA", "JYSK", "HORNBACH" | Bývanie › Vybavenie a doplnky |
| Text "ENERGETIK" (energetika, energetické), " SPP ", " ZSE ", "VODARENS" (energy and water suppliers) | Bývanie › Ostatné |
| Text "MZD" (mzda, mzdy) | Príjmy › Mzda |
| Counterparty name "URAD PRACE", "SOCIALNA POISTOVNA" | Príjmy › Dávky a prídavky |
| Text "NETFLIX", "SPOTIFY", "DISNEY", "HBO MAX", "YOUTUBE PREMIUM", "YOUTUBEPREMIUM" | Nákupy online › Webové služby |
| Text "ALIEXPRESS", "ALLEGRO", " TEMU", "GOPAY", "PAYPAL", "PACKETA", " DPD ", "SHOPTET", "POSTA SK" | Nákupy online › Ostatné |
| Text "TESCO", "LIDL", "KAUFLAND", "BILLA", " COOP", "TERNO", "MINIMARKET", "POTRAVINY", "PEKAREN" | Stravovanie › Potraviny |
| Text "MCDONALD", " KFC ", "STARBUCKS", "BISTRO", "RESTAURACIA", "KAVIAREN", "PIZZ", "WOLT", "FOODORA" | Stravovanie › Reštaurácie a kaviarne |
| Text "SLOVNAFT", "SHELL", " OMV ", "ORLEN", " MOL ", "CERPACIA STANICA" | Auto-moto › Pohonné hmoty |
| Text "CARWASH", "AUTOSERVIS", "PNEUSERVIS", "AUTOUMYVAREN" | Auto-moto › Servis |
| Text "ORANGE", "SLOVAK TELEKOM", "O2 SLOVAKIA", "MOJEO2" | Elektronika, komunikácia a médiá › Účty za telefón |
| Text " ALZA", "DATART", "ELEKTRO", "MALL.SK" | Elektronika, komunikácia a médiá › Elektronika |
| Text "MAGNETA", "SLOVANET" | Elektronika, komunikácia a médiá › Ostatné |
| Text "DM DROGERIE", "DROGERIA", " TETA", "ROSSMANN" | Zdravie a osobná starostlivosť › Drogéria |
| Text "SYNLAB", "NEMOCNIC", "POLIKLINIK" | Zdravie a osobná starostlivosť › Lekári a nemocnice |
| Text "LEKAREN", "DR.MAX", " BENU", "OPTIKA" | Zdravie a osobná starostlivosť › Ostatné |
| Text " BOLT", " UBER", "TAXI" | Doprava › Taxi |
| Text "ZSSK", "REGIOJET", "FLIXBUS", "SLOVAK LINES", " MHD " | Doprava › Verejná doprava |
| Text "POISTOVNA", "POISTENIE", "UNIQA", "GENERALI", "ALLIANZ", "KOOPERATIVA" | Ostatné výdavky › Platby poistného |
| Text "OKRESNY URAD", "MESTSKY URAD", "MINISTERSTVO" (state and municipal payments) | Ostatné výdavky › Ostatné |
| Text "SKOLNE", "SKOLK" (škôlka, škôlku), "MATERSKA SKOLA" | Vzdelanie › Škola a škôlka |
| Text "KNIHKUPECTVO", "PAPIERNICTVO", "KNIHY" | Vzdelanie › Školské potreby |
| Text "DOUCOVANIE", "JAZYKOVA SKOLA" | Vzdelanie › Kurzy a doučovanie |
| Text "STEAM", "PLAYSTATION", "NINTENDO", " XBOX", "GOG SP" | Zábava a oddych › Hry a hračky |
| Text " KINO", "CINEMAX", "DECATHLON", "SKIPAS", "SKI RESORT" | Zábava a oddych › Ostatné |
| Text " XTB ", "TRADING 212", "FINAX", "INTERACTIVE BROKERS" | Investície › Ostatné |

Most rules for moving money between the user's own accounts can't be built in, since they depend on the user's accounts; the user creates them, for example on the counterparty account. The savings-account rule above catches only transfers that say so in their text.

### Why a transaction is where it is

For every transaction the result says which rule decided: whether it is a default or a user rule, and which. For a transaction in an Uncategorized bucket it says that no rule matched.

## Acceptance criteria

- [ ] On every committed sample statement, every transaction ends up in exactly one sub-category (or Uncategorized bucket), and nothing is lost: closing balance minus opening balance equals income plus excluded minus the spending total.
- [ ] On sample 01, the NETFLIX card payment is in Nákupy online › Webové služby, the rent payment in Bývanie › Nájom, the monthly fee and its reversal are both in Poplatky a úroky › Poplatky and cancel to a spending of 0, and the salary is in Príjmy › Mzda and not in the spending total.
- [ ] Every default rule in the table above is tested: each alternative matches a constructed transaction it is meant for and puts it in the listed sub-category, and no default rule targets a sub-category that doesn't exist.
- [ ] A category's balance equals the sum of its sub-categories' balances, for every category on every sample.
- [ ] Every built-in category except the two Uncategorized buckets has an Ostatné sub-category, and no transaction is ever directly in a category that has sub-categories.
- [ ] A user rule beats a default rule that matches the same transaction, whatever their priority numbers.
- [ ] Between two user rules with different numbers that both match, the one with the lower number decides; between two default rules too.
- [ ] Creating a user rule with a priority number below 1, or that isn't a whole number, is refused with a reason saying so.
- [ ] Two user rules with the same priority number are both accepted; when both match a transaction, the newest decides, and changing one rule's number afterwards doesn't change which was created first.
- [ ] A rule added on top beats every existing user rule that matches the same transaction, whatever their numbers, and gets the lowest existing user number, or 1 when there are no user rules; adding several rules on top in a row, each wins over the earlier ones.
- [ ] A rule with an empty text, an empty or inverted amount range, or a target that isn't an existing sub-category is refused.
- [ ] Each of the six conditions matches the transactions it describes and no others, checked on constructed transactions, including a missing field not matching.
- [ ] A text condition in "contains" mode and one in "equals" mode each match accordingly, with case, diacritics and extra spaces ignored, and in "contains" mode with punctuation counted as a space (so "Mol.PetExpert" and "Dr.Max 382" are found, but "DOMOL" is not found by " MOL "). A text with no letters or digits is refused.
- [ ] An amount condition matches money in and money out of the same size.
- [ ] A transaction no rule matches goes to Nezaradené výdavky if money out, Nezaradené príjmy otherwise, with "no rule matched" as the reason; a zero amount goes to the second.
- [ ] A transaction in an excluded category isn't counted in spending, and one in an income category never is.
- [ ] A refund (money in) matched into a spending category reduces that category's spending.
- [ ] A user-created category gets an Ostatné sub-category, takes the role the user chose, and is refused if its name repeats a sibling's, ignoring case and diacritics.
- [ ] The result for each transaction says which rule decided, or that none did.
- [ ] The same statement and rules always give the same result, and the statement is unchanged.
- [ ] An empty month categorizes to an empty list with all totals zero.

## Edge cases and errors

- Two identical transactions are categorized independently and both counted.
- A rule can target a sub-category of an income or excluded category; a transaction in the "wrong" direction there (money out in Príjmy) simply lowers that category's balance.
- Text conditions look at the description and the payment purpose; a transfer whose purpose is longer than its printed description is found by its purpose.
- Fee rows and their reversals carry the type account fee or fee reversal, which the first default rule matches, so they go to Poplatky unless a user rule decides otherwise.

## Out of scope

- Saving user rules, categories and priorities between sessions (a separate issue).
- Editing or deleting rules and categories, the rule editor, charts, views and any other screen, and the Slovak texts shown around them.
- Combining several conditions in one rule; rules on direction, card or purpose category code.
- A choice to count excluded categories as spending; changing a built-in category's role.
- Learning from the user's choices or suggesting rules.
- More than two category levels; deleting built-in categories.
- Other banks or periods.

## Open questions
None. Decided with the maintainer: refunds reduce spending in their category; 1 is the highest priority; user rules may share a priority number and the newest wins, with a way to add a rule on top; the names above; a Nájom sub-category under Bývanie; the default rule set above as a starting set; built-in roles fixed in the MVP.
