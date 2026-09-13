using FluentValidation;
using Racinglazing.User.Application.Contracts;

namespace Racinglazing.User.Application.Features.Authentication;

/// <summary>
/// Input rules for the auth endpoints. These guard shape and policy (a valid
/// email, an acceptable password); business rules such as "email already taken"
/// live in the use case, where the database is the source of truth.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256)
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 32).WithMessage("Username must be between 3 and 32 characters.")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may only contain letters, digits, and the characters _ . -");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"Password must be at least {PasswordMinLength} characters.")
            .MaximumLength(PasswordMaxLength)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Password and confirmation do not match.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername).NotEmpty().WithMessage("Email or username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("A refresh token is required.");
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("A refresh token is required.");
}

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
        => RuleFor(x => x.IdToken).NotEmpty().WithMessage("A Google id_token is required.");
}
