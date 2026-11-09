namespace Kairos.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The replay has not produced a market yet: the application is still starting.</summary>
public sealed class MarketNotReadyException() : Exception("The market is not ready yet.");
