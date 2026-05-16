using FluentValidation;
using SmartBank.Models.DTOs.Transactions;

namespace SmartBank.API.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequestDto>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.FromAccountId)
            .GreaterThan(0)
            .WithMessage("From Account ID must be greater than 0.");

        RuleFor(x => x.ToAccountNumber)
            .NotEmpty()
            .WithMessage("Destination account number is required.")
            .MaximumLength(20)
            .WithMessage("Account number must not exceed 20 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(10_000_000)
            .WithMessage("Amount must not exceed 10,000,000.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .WithMessage("Remarks must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Remarks));
    }
}
