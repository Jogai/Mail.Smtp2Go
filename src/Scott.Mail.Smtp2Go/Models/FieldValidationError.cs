namespace Scott.Mail.Smtp2Go;

/// <summary>One entry of the API's <c>field_validation_errors</c>.</summary>
/// <param name="FieldName">The offending request field, when the API named one.</param>
/// <param name="Message">The API's description of the problem.</param>
public sealed record FieldValidationError(string? FieldName, string Message);
