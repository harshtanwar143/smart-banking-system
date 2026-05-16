using FluentValidation;
using SmartBank.Models.DTOs.Transactions;

namespace SmartBank.API.Validators;

public class DepositRequestValidator : AbstractValidator<DepositRequestDto>
{
    public DepositRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .WithMessage("Account ID must be greater than 0.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(10_000_000)
            .WithMessage("Amount must not exceed 10,000,000.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
