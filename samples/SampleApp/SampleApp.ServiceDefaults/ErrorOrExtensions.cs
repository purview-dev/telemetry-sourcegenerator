using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ErrorOr;

#pragma warning restore IDE0130 // Namespace does not match folder structure

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ErrorOrExtensions
{
	public static ProblemDetails ToProblemDetails(this Error error) =>
		new()
		{
			Status = GetStatusCode(error),
			Title = error.Code,
			Detail = error.Description,
		};

	static int GetStatusCode(Error error)
	{
		if (error.Type == ErrorType.NotFound)
		{
			return StatusCodes.Status404NotFound;
		}
		else if (error.Type == ErrorType.Validation)
		{
			return StatusCodes.Status400BadRequest;
		}

		return StatusCodes.Status500InternalServerError;
	}
}
