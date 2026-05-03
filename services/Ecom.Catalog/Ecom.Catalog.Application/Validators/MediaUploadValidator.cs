using FluentValidation;

namespace Ecom.Catalog.Application.Validators;

public class MediaUploadRequest
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}

public class MediaUploadValidator : AbstractValidator<MediaUploadRequest>
{
    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp" };

    public MediaUploadValidator()
    {
        RuleFor(x => x.FileUrl).NotEmpty();
        RuleFor(x => x.FileType).Must(t => Allowed.Contains(t))
            .WithMessage("Unsupported file type. Allowed: jpeg, png, webp.");
    }
}
