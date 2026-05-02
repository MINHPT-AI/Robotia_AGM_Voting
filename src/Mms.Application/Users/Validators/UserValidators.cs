using FluentValidation;
using Mms.Application.Users.Commands;

namespace Mms.Application.Users.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidRoles = ["admin", "operator", "viewer", "checkin"];

    public CreateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống")
            .MinimumLength(4).WithMessage("Tên đăng nhập tối thiểu 4 ký tự")
            .Matches(@"^\S+$").WithMessage("Tên đăng nhập không được chứa khoảng trắng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches(@"[A-Z]").WithMessage("Cần ít nhất 1 chữ hoa")
            .Matches(@"\d").WithMessage("Cần ít nhất 1 chữ số");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Role)
            .Must(r => ValidRoles.Contains(r)).WithMessage("Vai trò không hợp lệ");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống");
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    private static readonly string[] ValidRoles = ["admin", "operator", "viewer", "checkin"];

    public UpdateUserValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.NewRole)
            .Must(r => ValidRoles.Contains(r)).WithMessage("Vai trò không hợp lệ");
    }
}

public class AdminResetPasswordValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches(@"[A-Z]").WithMessage("Cần ít nhất 1 chữ hoa")
            .Matches(@"\d").WithMessage("Cần ít nhất 1 chữ số");
    }
}

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class ChangeOwnPasswordValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches(@"[A-Z]").WithMessage("Cần ít nhất 1 chữ hoa")
            .Matches(@"\d").WithMessage("Cần ít nhất 1 chữ số");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");
    }
}
