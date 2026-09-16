using System.Net;
using FluentValidation;
using FluentValidation.Results;

namespace SharedKernel.Application.Exceptions;

public enum ErrorCode
{
    Unhandled = 0,
    Invalid = 1,
    NotFound = 2,
    Unauthorized = 3
}

public class RejectException : ValidationException
{
    public ErrorCode Code { get; }

    public RejectException(string message)
        : base(new[] { new ValidationFailure(string.Empty, message) })
    {
        Code = ErrorCode.Invalid;
    }

    public RejectException(ErrorCode code, string message)
        : base(new[] { new ValidationFailure(code.ToString(), message) })
    {
        Code = code;
    }
}

public static class ErrorCtr
{
    public static ErrorInfo ExtractErrorInfo(Exception ex)
    {
        if (ex is RejectException rejectEx)
        {
            var statusCode = rejectEx.Code switch
            {
                ErrorCode.NotFound => HttpStatusCode.NotFound,
                ErrorCode.Unauthorized => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.BadRequest
            };

            return new ErrorInfo
            {
                errorCode = statusCode,
                errors = rejectEx.Errors.Select(e => e.ErrorMessage).ToList(),
                description = string.Join("; ", rejectEx.Errors.Select(e => e.ErrorMessage))
            };
        }

        if (ex is ValidationException valEx)
        {
            return new ErrorInfo
            {
                errorCode = HttpStatusCode.BadRequest,
                errors = valEx.Errors.Select(e => e.ErrorMessage).ToList(),
                description = string.Join("; ", valEx.Errors.Select(e => e.ErrorMessage))
            };
        }

        return new ErrorInfo
        {
            errorCode = HttpStatusCode.InternalServerError,
            errors = ex.Message,
            description = ex.Message
        };
    }

    public static object ExtractErrorInfor(Exception ex)
    {
        throw new NotImplementedException();
    }
}

public class ErrorInfo
{
    public HttpStatusCode errorCode { get; set; }
    public object? errors { get; set; }
    public string description { get; set; } = string.Empty;
}
