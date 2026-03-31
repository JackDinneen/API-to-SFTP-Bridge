namespace API.Core.Models;

public enum ConnectionStatus
{
    Active,
    Paused,
    Error
}

public enum AuthType
{
    ApiKey,
    OAuth2ClientCredentials,
    BasicAuth,
    CustomHeaders
}

public enum UserRole
{
    Admin,
    Operator,
    Viewer
}

public enum SyncRunStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

public enum TransformType
{
    DirectMapping,
    ValueMapping,
    UnitConversion,
    DateParse,
    StaticValue,
    Concatenation,
    Split
}

public enum AggregationType
{
    Sum,
    Average,
    Last,
    Max
}

public enum UtilityType
{
    Electricity,
    Gas,
    Water,
    Waste,
    DistrictHeating,
    DistrictCooling
}
