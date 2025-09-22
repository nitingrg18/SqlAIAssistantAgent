using FluentValidation;

namespace SqlAgentApi.Application.Assistants
{
    public class GenerateSqlCommandValidator : AbstractValidator<GenerateSqlCommand>
    {
        public GenerateSqlCommandValidator()
        {
            RuleFor(x => x.Question)
                .NotEmpty().WithMessage("Question cannot be empty.")
                .MaximumLength(1000).WithMessage("Question is too long.");
        }
    }
}