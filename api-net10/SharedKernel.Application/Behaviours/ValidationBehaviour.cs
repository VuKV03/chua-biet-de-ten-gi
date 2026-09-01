using FluentValidation;
using MediatR;

namespace SharedKernel.Application.Behaviours;

/// <summary>
/// Pipeline behavior của MediatR: tự động chạy toàn bộ IValidator&lt;TRequest&gt; đã đăng ký
/// trước khi Handler xử lý. Ném FluentValidation.ValidationException nếu có lỗi,
/// được ErrorCtr.ExtractErrorInfo bắt và trả về 400 Bad Request ở tầng Api.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
