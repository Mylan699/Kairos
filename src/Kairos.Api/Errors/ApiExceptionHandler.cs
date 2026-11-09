using FluentValidation;
using Kairos.Application.Common;
using Kairos.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.Errors;

/// <summary>Turns the exceptions the application raises on purpose into standard ProblemDetails responses.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = ToProblem(exception);

        // Anything else is a bug: the default handler answers 500 without revealing details.
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblem(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "La demande contient des valeurs invalides.",
        },
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Élément introuvable.",
            Detail = notFound.Message,
        },
        DomainException domain => new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Opération refusée par les règles métier.",
            Detail = domain.Message,
        },
        MarketNotReadyException => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Le marché démarre, réessayez dans un instant.",
        },
        _ => null,
    };
}
