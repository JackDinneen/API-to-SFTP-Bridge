namespace UnitTests.Application.Validators;

using API.Application.Validators;
using API.Core.DTOs;
using API.Core.Models;
using FluentAssertions;
using FluentValidation.TestHelper;

public class CreateMappingDtoValidatorTests
{
    private readonly CreateMappingDtoValidator _sut = new();

    // ---------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------

    [Fact]
    public void ValidMapping_DirectMapping_Passes()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.value",
            TargetColumn = "Value",
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Asset ID")]
    [InlineData("Asset name")]
    [InlineData("Submeter Code")]
    [InlineData("Utility Type")]
    [InlineData("Year")]
    [InlineData("Month")]
    [InlineData("Value")]
    public void ValidTargetColumn_Passes(string targetColumn)
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = targetColumn,
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.TargetColumn);
    }

    [Theory]
    [InlineData("DirectMapping")]
    [InlineData("ValueMapping")]
    [InlineData("UnitConversion")]
    [InlineData("DateParse")]
    [InlineData("StaticValue")]
    [InlineData("Concatenation")]
    [InlineData("Split")]
    public void ValidTransformType_Passes(string transformType)
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = "Value",
            TransformType = transformType
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.TransformType);
    }

    // ---------------------------------------------------------------
    // StaticValue special case
    // ---------------------------------------------------------------

    [Fact]
    public void StaticValue_WithEmptySourcePath_Passes()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "",
            TargetColumn = "Asset ID",
            TransformType = "StaticValue"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.SourcePath);
    }

    [Fact]
    public void StaticValue_WithNullSourcePath_DoesNotFailOnSourcePath()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = null!,
            TargetColumn = "Asset ID",
            TransformType = "StaticValue"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.SourcePath);
    }

    // ---------------------------------------------------------------
    // Missing SourcePath for non-StaticValue
    // ---------------------------------------------------------------

    [Fact]
    public void NonStaticValue_WithEmptySourcePath_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "",
            TargetColumn = "Value",
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.SourcePath)
            .WithErrorMessage("Source path is required for non-static mappings");
    }

    // ---------------------------------------------------------------
    // Invalid TargetColumn
    // ---------------------------------------------------------------

    [Fact]
    public void InvalidTargetColumn_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = "InvalidColumn",
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.TargetColumn);
    }

    [Fact]
    public void EmptyTargetColumn_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = "",
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.TargetColumn);
    }

    // ---------------------------------------------------------------
    // Invalid TransformType
    // ---------------------------------------------------------------

    [Fact]
    public void InvalidTransformType_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = "Value",
            TransformType = "InvalidTransform"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.TransformType);
    }

    [Fact]
    public void EmptyTransformType_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = "data.field",
            TargetColumn = "Value",
            TransformType = ""
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.TransformType);
    }

    // ---------------------------------------------------------------
    // SourcePath max length
    // ---------------------------------------------------------------

    [Fact]
    public void SourcePath_ExceedingMaxLength_Fails()
    {
        var dto = new CreateMappingDto
        {
            SourcePath = new string('a', 1025),
            TargetColumn = "Value",
            TransformType = "DirectMapping"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.SourcePath);
    }
}

public class CreateConnectionRequestValidatorTests
{
    private readonly CreateConnectionRequestValidator _sut = new();

    private static CreateConnectionRequest BuildValidRequest()
    {
        return new CreateConnectionRequest
        {
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            ClientName = "acme",
            PlatformName = "energystar",
            SftpPort = 22,
            ReportingLagDays = 5,
            Mappings = new List<CreateMappingDto>
            {
                new()
                {
                    SourcePath = "data.value",
                    TargetColumn = "Value",
                    TransformType = "DirectMapping"
                }
            },
            Credentials = new CreateCredentialDto
            {
                ApiKey = "test-key-123"
            }
        };
    }

    // ---------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------

    [Fact]
    public void ValidRequest_WithMappings_Passes()
    {
        var request = BuildValidRequest();

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ---------------------------------------------------------------
    // Empty mappings
    // ---------------------------------------------------------------

    [Fact]
    public void EmptyMappings_Fails()
    {
        var request = BuildValidRequest();
        request.Mappings = new List<CreateMappingDto>();

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Mappings)
            .WithErrorMessage("At least one field mapping is required");
    }

    // ---------------------------------------------------------------
    // ApiKey credential validation
    // ---------------------------------------------------------------

    [Fact]
    public void ApiKeyAuth_WithMissingApiKey_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.ApiKey;
        request.Credentials = new CreateCredentialDto
        {
            ApiKey = ""
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.ApiKey")
            .WithErrorMessage("API key is required for ApiKey auth type");
    }

    [Fact]
    public void ApiKeyAuth_WithApiKey_Passes()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.ApiKey;
        request.Credentials = new CreateCredentialDto
        {
            ApiKey = "my-secret-key"
        };

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Credentials.ApiKey");
    }

    // ---------------------------------------------------------------
    // BasicAuth credential validation
    // ---------------------------------------------------------------

    [Fact]
    public void BasicAuth_WithMissingUsername_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.BasicAuth;
        request.Credentials = new CreateCredentialDto
        {
            BasicUsername = "",
            BasicPassword = "pass"
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.BasicUsername")
            .WithErrorMessage("Username is required for Basic auth type");
    }

    [Fact]
    public void BasicAuth_WithMissingPassword_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.BasicAuth;
        request.Credentials = new CreateCredentialDto
        {
            BasicUsername = "user",
            BasicPassword = ""
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.BasicPassword")
            .WithErrorMessage("Password is required for Basic auth type");
    }

    [Fact]
    public void BasicAuth_WithBothFields_Passes()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.BasicAuth;
        request.Credentials = new CreateCredentialDto
        {
            BasicUsername = "user",
            BasicPassword = "pass"
        };

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Credentials.BasicUsername");
        result.ShouldNotHaveValidationErrorFor("Credentials.BasicPassword");
    }

    // ---------------------------------------------------------------
    // OAuth2 credential validation
    // ---------------------------------------------------------------

    [Fact]
    public void OAuth2_WithMissingClientId_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.OAuth2ClientCredentials;
        request.Credentials = new CreateCredentialDto
        {
            OAuthClientId = "",
            OAuthClientSecret = "secret",
            OAuthTokenUrl = "https://auth.example.com/token"
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.OAuthClientId")
            .WithErrorMessage("Client ID is required for OAuth2 auth type");
    }

    [Fact]
    public void OAuth2_WithMissingClientSecret_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.OAuth2ClientCredentials;
        request.Credentials = new CreateCredentialDto
        {
            OAuthClientId = "client-id",
            OAuthClientSecret = "",
            OAuthTokenUrl = "https://auth.example.com/token"
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.OAuthClientSecret")
            .WithErrorMessage("Client secret is required for OAuth2 auth type");
    }

    [Fact]
    public void OAuth2_WithMissingTokenUrl_Fails()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.OAuth2ClientCredentials;
        request.Credentials = new CreateCredentialDto
        {
            OAuthClientId = "client-id",
            OAuthClientSecret = "secret",
            OAuthTokenUrl = ""
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Credentials.OAuthTokenUrl")
            .WithErrorMessage("Token URL is required for OAuth2 auth type");
    }

    [Fact]
    public void OAuth2_WithAllFields_Passes()
    {
        var request = BuildValidRequest();
        request.AuthType = AuthType.OAuth2ClientCredentials;
        request.Credentials = new CreateCredentialDto
        {
            OAuthClientId = "client-id",
            OAuthClientSecret = "secret",
            OAuthTokenUrl = "https://auth.example.com/token"
        };

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Credentials.OAuthClientId");
        result.ShouldNotHaveValidationErrorFor("Credentials.OAuthClientSecret");
        result.ShouldNotHaveValidationErrorFor("Credentials.OAuthTokenUrl");
    }

    // ---------------------------------------------------------------
    // Invalid mapping within request is surfaced
    // ---------------------------------------------------------------

    [Fact]
    public void Request_WithInvalidMapping_Fails()
    {
        var request = BuildValidRequest();
        request.Mappings = new List<CreateMappingDto>
        {
            new()
            {
                SourcePath = "data.value",
                TargetColumn = "BadColumn",
                TransformType = "DirectMapping"
            }
        };

        var result = _sut.TestValidate(request);

        result.ShouldHaveAnyValidationError();
    }

    // ---------------------------------------------------------------
    // Core field validation (quick coverage)
    // ---------------------------------------------------------------

    [Fact]
    public void EmptyName_Fails()
    {
        var request = BuildValidRequest();
        request.Name = "";

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void InvalidBaseUrl_Fails()
    {
        var request = BuildValidRequest();
        request.BaseUrl = "not-a-url";

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    [Fact]
    public void SftpPort_OutOfRange_Fails()
    {
        var request = BuildValidRequest();
        request.SftpPort = 0;

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SftpPort);
    }

    [Fact]
    public void NegativeReportingLagDays_Fails()
    {
        var request = BuildValidRequest();
        request.ReportingLagDays = -1;

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ReportingLagDays);
    }
}
