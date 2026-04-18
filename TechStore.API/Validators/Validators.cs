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
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone != null);
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
        RuleFor(x => x.CompareAtPrice).GreaterThan(x => x.Price)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare-at price must be greater than sale price.");
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(1);
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingFirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShippingLastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShippingCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShippingProvince).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShippingPostalCode).NotEmpty()
            .Matches(@"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$")
            .WithMessage("Enter a valid Canadian postal code (e.g. M5V 2T6).");
        RuleFor(x => x.ShippingCountry).NotEmpty().MaximumLength(50);
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
