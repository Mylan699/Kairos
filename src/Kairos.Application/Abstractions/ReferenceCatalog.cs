using Kairos.Application.Common;
using Kairos.Domain.Instruments;
using Kairos.Domain.Reference;

namespace Kairos.Application.Abstractions;

/// <summary>The static data of the desk, indexed by id. It never changes while the application runs.</summary>
public sealed class ReferenceCatalog(
    IEnumerable<Instrument> instruments,
    IEnumerable<Book> books,
    IEnumerable<Trader> traders,
    IEnumerable<Counterparty> counterparties)
{
    private readonly Dictionary<Guid, Instrument> instrumentsById = instruments.ToDictionary(item => item.Id);
    private readonly Dictionary<Guid, Book> booksById = books.ToDictionary(item => item.Id);
    private readonly Dictionary<Guid, Trader> tradersById = traders.ToDictionary(item => item.Id);
    private readonly Dictionary<Guid, Counterparty> counterpartiesById = counterparties.ToDictionary(item => item.Id);

    public IReadOnlyCollection<Instrument> Instruments => instrumentsById.Values;

    public IReadOnlyCollection<Book> Books => booksById.Values;

    public IReadOnlyCollection<Trader> Traders => tradersById.Values;

    public IReadOnlyCollection<Counterparty> Counterparties => counterpartiesById.Values;

    public Instrument Instrument(Guid id) => Find(instrumentsById, id, "Instrument");

    public Book Book(Guid id) => Find(booksById, id, "Book");

    public Trader Trader(Guid id) => Find(tradersById, id, "Trader");

    public Counterparty Counterparty(Guid id) => Find(counterpartiesById, id, "Counterparty");

    private static T Find<T>(Dictionary<Guid, T> items, Guid id, string kind) =>
        items.TryGetValue(id, out var item) ? item : throw new NotFoundException($"{kind} {id} does not exist.");
}
