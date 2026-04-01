namespace API.Application.Validators;

using API.Core.DTOs;
using FluentValidation;

public class CreateConnectionRequestValidator : AbstractValidator<CreateConnectionRequest>
{
    public CreateConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(256);

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required")
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Base URL must be a valid absolute URL");

        RuleFor(x => x.AuthType)
            .IsInEnum().WithMessage("Invalid authentication type");

        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(256);

        RuleFor(x => x.PlatformName)
            .NotEmpty().WithMessage("Platform name is required")
            .MaximumLength(256);

        RuleFor(x => x.SftpPort)
            .InclusiveBetween(1, 65535).WithMessage("SFTP port must be between 1 and 65535");

        RuleFor(x => x.ReportingLagDays)
            .GreaterThanOrEqualTo(0).WithMessage("Reporting lag days must be non-negative");

        RuleFor(x => x.ScheduleCron)
            .MaximumLength(128);
    }
}

public class UpdateConnectionRequestValidator : AbstractValidator<UpdateConnectionRequest>
{
    public UpdateConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(256);

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required")
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Base URL must be a valid absolute URL");

        RuleFor(x => x.AuthType)
            .IsInEnum().WithMessage("Invalid authentication type");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid connection status");

        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(256);

        RuleFor(x => x.PlatformName)
            .NotEmpty().WithMessage("Platform name is required")
            .MaximumLength(256);

        RuleFor(x => x.SftpPort)
            .InclusiveBetween(1, 65535).WithMessage("SFTP port must be between 1 and 65535");

        RuleFor(x => x.ReportingLagDays)
            .GreaterThanOrEqualTo(0).WithMessage("Reporting lag days must be non-negative");

        RuleFor(x => x.ScheduleCron)
            .MaximumLength(128);
    }
}
