using Kairos.Domain.Instruments;
using Kairos.Domain.Positions;

namespace Kairos.Domain.Risk;

/// <summary>A position held in one book on one instrument.</summary>
public sealed record BookPosition(Guid BookId, Instrument Instrument, Position Position);
