namespace API.Application.Validators;

using API.Core.DTOs;
using API.Core.Models;
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

        RuleFor(x => x.Mappings)
            .NotEmpty().WithMessage("At least one field mapping is required");

        RuleForEach(x => x.Mappings).SetValidator(new CreateMappingDtoValidator());

        When(x => x.AuthType == AuthType.ApiKey, () =>
        {
            RuleFor(x => x.Credentials)
                .NotNull().WithMessage("Credentials are required for ApiKey auth type");
            RuleFor(x => x.Credentials!.ApiKey)
                .NotEmpty().WithMessage("API key is required for ApiKey auth type")
                .When(x => x.Credentials != null);
        });

        When(x => x.AuthType == AuthType.BasicAuth, () =>
        {
            RuleFor(x => x.Credentials)
                .NotNull().WithMessage("Credentials are required for Basic auth type");
            RuleFor(x => x.Credentials!.BasicUsername)
                .NotEmpty().WithMessage("Username is required for Basic auth type")
                .When(x => x.Credentials != null);
            RuleFor(x => x.Credentials!.BasicPassword)
                .NotEmpty().WithMessage("Password is required for Basic auth type")
                .When(x => x.Credentials != null);
        });

        When(x => x.AuthType == AuthType.OAuth2ClientCredentials, () =>
        {
            RuleFor(x => x.Credentials)
                .NotNull().WithMessage("Credentials are required for OAuth2 auth type");
            RuleFor(x => x.Credentials!.OAuthClientId)
                .NotEmpty().WithMessage("Client ID is required for OAuth2 auth type")
                .When(x => x.Credentials != null);
            RuleFor(x => x.Credentials!.OAuthClientSecret)
                .NotEmpty().WithMessage("Client secret is required for OAuth2 auth type")
                .When(x => x.Credentials != null);
            RuleFor(x => x.Credentials!.OAuthTokenUrl)
                .NotEmpty().WithMessage("Token URL is required for OAuth2 auth type")
                .When(x => x.Credentials != null);
        });
    }
}

public class CreateMappingDtoValidator : AbstractValidator<CreateMappingDto>
{
    private static readonly string[] ValidObiColumns =
    {
        "Asset ID", "Asset name", "Submeter Code", "Utility Type", "Year", "Month", "Value"
    };

    private static readonly string[] ValidTransformTypes =
    {
        "DirectMapping", "ValueMapping", "UnitConversion", "DateParse", "StaticValue", "Concatenation", "Split"
    };

    public CreateMappingDtoValidator()
    {
        RuleFor(x => x.TargetColumn)
            .NotEmpty().WithMessage("Target column is required")
            .Must(tc => ValidObiColumns.Contains(tc))
            .WithMessage("Target column must be one of: " + string.Join(", ", ValidObiColumns));

        RuleFor(x => x.TransformType)
            .NotEmpty().WithMessage("Transform type is required")
            .Must(tt => ValidTransformTypes.Contains(tt))
            .WithMessage("Transform type must be one of: " + string.Join(", ", ValidTransformTypes));

        RuleFor(x => x.SourcePath)
            .NotEmpty().WithMessage("Source path is required for non-static mappings")
            .When(x => x.TransformType != "StaticValue");

        RuleFor(x => x.SourcePath)
            .MaximumLength(1024);

        RuleFor(x => x.TransformConfig)
            .MaximumLength(8192).WithMessage("Transform config must not exceed 8192 characters")
            .Must(BeValidJsonOrNull).WithMessage("Transform config must be valid JSON")
            .When(x => !string.IsNullOrEmpty(x.TransformConfig));
    }

    private static bool BeValidJsonOrNull(string? json)
    {
        if (string.IsNullOrEmpty(json)) return true;
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
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
