using System.Text.RegularExpressions;
using FluentValidation;
using TechStore.API.DTOs.Pos;
using TechStore.API.DTOs.Transfer;
using TechStore.API.DTOs.BankAccount;
using TechStore.API.Models;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Newsletter;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Product;
using TechStore.API.DTOs.Review;
using TechStore.API.DTOs.Store;

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

public class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[A-Za-z0-9]{2,10}$")
            .WithMessage("Code must be 2–10 letters or numbers, e.g. IKJ.");
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Phone).Matches(@"^[0-9+()\-\s]{7,20}$")
            .WithMessage("Enter a valid phone number.").When(x => !string.IsNullOrEmpty(x.Phone));
    }
}

public class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name != null);
        RuleFor(x => x.Code).Matches(@"^[A-Za-z0-9]{2,10}$")
            .WithMessage("Code must be 2–10 letters or numbers, e.g. IKJ.").When(x => x.Code != null);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Phone).Matches(@"^[0-9+()\-\s]{7,20}$")
            .WithMessage("Enter a valid phone number.").When(x => !string.IsNullOrEmpty(x.Phone));
    }
}

public class PosSaleRequestValidator : AbstractValidator<PosSaleRequest>
{
    public PosSaleRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one item.")
            .Must(i => i.Count <= 50).WithMessage("A sale can have at most 50 lines.");
        RuleForEach(x => x.Items).SetValidator(new PosSaleItemRequestValidator());

        RuleFor(x => x.Payments).NotEmpty().WithMessage("Add at least one payment.")
            .Must(p => p.Count <= 5).WithMessage("A sale can be split into at most 5 payments.");
        RuleForEach(x => x.Payments).SetValidator(new PosPaymentRequestValidator());

        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountReason).MaximumLength(200);
        RuleFor(x => x.Approval!.ApproverUserId).GreaterThan(0).When(x => x.Approval != null);
        RuleFor(x => x.Approval!.Pin).Matches(@"^\d{4,6}$").WithMessage("The PIN is 4–6 digits.")
            .When(x => x.Approval != null);

        RuleFor(x => x.CustomerName).MaximumLength(100);
        RuleFor(x => x.CustomerPhone)
            .Must(p => Regex.IsMatch(Regex.Replace(p!, @"[\s()-]", ""), @"^(\+234|0)[789]\d{9}$"))
            .WithMessage("Enter a valid Nigerian phone number (e.g. 08012345678).")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhone));
        RuleFor(x => x.CustomerEmail).EmailAddress().MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class PosSaleItemRequestValidator : AbstractValidator<PosSaleItemRequest>
{
    public PosSaleItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
        RuleFor(x => x.UnitPrice).GreaterThan(0).When(x => x.UnitPrice.HasValue);
        RuleForEach(x => x.SerialNumbers).MaximumLength(50)
            .Matches(@"^[A-Za-z0-9-]*$").WithMessage("Serial numbers can only contain letters, numbers and -.");
    }
}

public class PosPaymentRequestValidator : AbstractValidator<PosPaymentRequest>
{
    public PosPaymentRequestValidator()
    {
        RuleFor(x => x.Method).Must(m => PaymentMethods.InStore.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", PaymentMethods.InStore)}.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.ToStoreId).GreaterThan(0).WithMessage("Choose the shop to send to.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one product.")
            .Must(i => i.Count <= 50).WithMessage("A transfer can have at most 50 lines.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 999);
        });
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class SaveBankAccountRequestValidator : AbstractValidator<SaveBankAccountRequest>
{
    public SaveBankAccountRequestValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().WithMessage("Enter the bank's name.").MaximumLength(100);
        RuleFor(x => x.AccountName).NotEmpty().WithMessage("Enter the account name.").MaximumLength(150);
        // Nigerian account numbers (NUBAN) are 10 digits
        RuleFor(x => x.AccountNumber).Matches(@"^\d{10}$").WithMessage("Enter the 10-digit account number.");
    }
}

public class SetApprovalPinRequestValidator : AbstractValidator<SetApprovalPinRequest>
{
    public SetApprovalPinRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.Pin).Matches(@"^\d{4,6}$").WithMessage("The PIN must be 4–6 digits.");
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
        RuleFor(x => x.Warranty).MaximumLength(100);
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Warranty).MaximumLength(100);
    }
}

public class GuestCartRequestValidator : AbstractValidator<GuestCartRequest>
{
    public GuestCartRequestValidator()
    {
        RuleFor(x => x.Items).NotNull().Must(i => i == null || i.Count <= 50).WithMessage("Too many items in one cart.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 100);
        });
    }
}

// Same order details as a signed-in checkout, plus an email, and a phone number either way:
// there's no account to look them up in
public class GuestCheckoutRequestValidator : AbstractValidator<GuestCheckoutRequest>
{
    public GuestCheckoutRequestValidator(IConfiguration config)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Your cart is empty.")
            .Must(i => i == null || i.Count <= 50).WithMessage("Too many items in one order.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 100);
        });
        RuleFor(x => x.Order).NotNull().SetValidator(new CreateOrderRequestValidator(config));
        RuleFor(x => x.Order.ShippingPhone).NotEmpty()
            .WithMessage("Enter a phone number so we can reach you about your order.")
            .When(x => x.Order != null);
    }
}

public class ClaimOrdersRequestValidator : AbstractValidator<ClaimOrdersRequest>
{
    public ClaimOrdersRequestValidator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(64);
}

public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
{
    public SubscribeRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}

public class UnsubscribeRequestValidator : AbstractValidator<UnsubscribeRequest>
{
    public UnsubscribeRequestValidator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(64);
}

public class FindGuestOrderRequestValidator : AbstractValidator<FindGuestOrderRequest>
{
    public FindGuestOrderRequestValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IConfiguration config)
    {
        // Card payments go through Paystack, which is switched off until it's set up
        string[] validPaymentMethods = config.GetValue<bool>("Paystack:Enabled")
            ? ["Card", "BankTransfer"]
            : ["BankTransfer"];

        // Letters, spaces, apostrophes, dots and hyphens, as the checkout form allows (NAME_RE in
        // utils/validation.js). The name goes into emails, and a guest can choose who gets them.
        const string name = @"^\p{L}[\p{L}\p{M}\s'.-]*$";
        RuleFor(x => x.ShippingFirstName).NotEmpty().MaximumLength(50).Matches(name).WithMessage("Use letters only.");
        RuleFor(x => x.ShippingLastName).NotEmpty().MaximumLength(50).Matches(name).WithMessage("Use letters only.");
        RuleFor(x => x.Fulfilment).IsInEnum();

        // Pickup orders need a shop and a phone number to call when it's ready, not an address
        When(x => x.Fulfilment == FulfilmentMethod.Pickup, () =>
        {
            RuleFor(x => x.PickupStoreId).NotNull().WithMessage("Choose a shop to collect from.");
            RuleFor(x => x.ShippingPhone).NotEmpty().WithMessage("Enter a phone number so the shop can reach you.");
        }).Otherwise(() =>
        {
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
        });

        RuleFor(x => x.ShippingCountry).MaximumLength(50);
        RuleFor(x => x.TransactionId).MaximumLength(100);

        // Validate payment method when provided
        RuleFor(x => x.PaymentMethod)
            .Must(m => m == null || validPaymentMethods.Contains(m))
            .WithMessage(validPaymentMethods.Length == 1
                ? "Only bank transfer is available for online orders at the moment."
                : $"Payment method must be one of: {string.Join(", ", validPaymentMethods)}.");
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

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5 stars.");
        RuleFor(x => x.Title).MaximumLength(100).When(x => x.Title != null);
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment != null);
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