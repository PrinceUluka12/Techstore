using FluentValidation;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Product;

namespace TechStore.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^(\+234|0)[789]\d{9}$")
            .WithMessage("Enter a valid Nigerian phone number (e.g. 08012345678).")
            .When(x => x.Phone != null);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare-at price must be greater than the sale price.");
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(1);
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    // Valid Nigerian payment methods accepted by the system
    private static readonly string[] ValidPaymentMethods = { "Card", "BankTransfer" };

    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingFirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShippingLastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShippingCity).NotEmpty().MaximumLength(100);

        // Province = State in Nigeria
        RuleFor(x => x.ShippingProvince)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(50);

        // Nigerian postal code — 6 digits (e.g. 100001 for Lagos Island)
        RuleFor(x => x.ShippingPostalCode)
            .NotEmpty()
            .Matches(@"^\d{6}$")
            .WithMessage("Enter a valid Nigerian postal code (6 digits, e.g. 100001).");

        RuleFor(x => x.ShippingCountry).NotEmpty().MaximumLength(50);

        // Validate payment method when provided
        RuleFor(x => x.PaymentMethod)
            .Must(m => m == null || ValidPaymentMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}.");
    }
}

public class AddToCartRequestValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(99);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}