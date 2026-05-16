using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartBank.API.Filters;

public class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments)
        {
            var type = argument.Value?.GetType();
            if (type == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            var validator = _serviceProvider.GetService(validatorType);

            if (validator == null) continue;
            // Try to call ValidateAsync(T instance, CancellationToken) if available, otherwise call Validate(T)
            FluentValidation.Results.ValidationResult result;
            var validateAsync = validatorType.GetMethod("ValidateAsync", new[] { typeof(FluentValidation.ValidationContext<>).MakeGenericType(type), typeof(System.Threading.CancellationToken) });
            if (validateAsync != null)
            {
                // Build a ValidationContext<T> instance
                var validationContextType = typeof(FluentValidation.ValidationContext<>).MakeGenericType(type);
                var validationContext = Activator.CreateInstance(validationContextType, argument.Value)!;
                var task = (System.Threading.Tasks.Task)validateAsync.Invoke(validator, new[] { validationContext, System.Threading.CancellationToken.None })!;
                await task.ConfigureAwait(false);
                // get Result property from Task<TResult>
                var resultProperty = task.GetType().GetProperty("Result")!;
                result = (FluentValidation.Results.ValidationResult)resultProperty.GetValue(task)!;
            }
            else
            {
                var validate = validatorType.GetMethod("Validate", new[] { typeof(FluentValidation.ValidationContext<>).MakeGenericType(type) });
                if (validate != null)
                {
                    var validationContextType = typeof(FluentValidation.ValidationContext<>).MakeGenericType(type);
                    var validationContext = Activator.CreateInstance(validationContextType, argument.Value)!;
                    result = (FluentValidation.Results.ValidationResult)validate.Invoke(validator, new[] { validationContext })!;
                }
                else
                {
                    // Fallback: no usable validate method
                    continue;
                }
            }

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(x => string.IsNullOrEmpty(x.Key) ? "" : x.Key, x => x.Select(e => e.ErrorMessage).ToArray());

                // Return errors in a ModelState-like structure to match previous behavior
                var problem = new ValidationProblemDetails();
                foreach (var kv in errors)
                {
                    problem.Errors.Add(kv.Key, kv.Value);
                }

                context.Result = new BadRequestObjectResult(problem);
                return;
            }
        }

        await next();
    }
}
