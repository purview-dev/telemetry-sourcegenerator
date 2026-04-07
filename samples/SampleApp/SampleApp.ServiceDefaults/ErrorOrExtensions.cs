using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErrorOr;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ErrorOrExtensions
{
	extension(Error error)
	{
		public ProblemDetails ToProblemDetails() =>
			new()
			{
				Status = GetStatusCode(error),
				Title = error.Code,
				Detail = error.Description,
			};
	}

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
