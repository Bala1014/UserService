using FluentValidation;
using Racinglazing.User.Application.Contracts;

namespace Racinglazing.User.Application.Features.Users;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(80);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048)
            .Must(BeAValidHttpUrl!).When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl))
            .WithMessage("Avatar URL must be a valid http(s) URL.");

        RuleFor(x => x.Bio).MaximumLength(1000);
    }

    private static bool BeAValidHttpUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
