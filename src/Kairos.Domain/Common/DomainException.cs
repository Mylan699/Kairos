namespace Kairos.Domain.Common;

/// <summary>Raised when an operation would break a business rule of the domain.</summary>
public class DomainException(string message) : Exception(message);
