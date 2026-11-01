using System.Text;
using System.Threading.RateLimiting;
using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TechStore.API.Data;
using TechStore.API.Helpers;
using TechStore.API.Middleware;
using TechStore.API.Repositories;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services;
using TechStore.API.Services.Interfaces;
using TechStore.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// Key Vault — loaded first so all subsequent config reads get vault values
// Uses Managed Identity on App Service, falls back to local dev credentials locally
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl) && !builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Database — SQL Server (unchanged from your repo)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(connStr,
        sql => sql.EnableRetryOnFailure(3)));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); // for Paystack verification calls
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IDeliveryRateService, DeliveryRateService>();
builder.Services.AddScoped<IJwtHelper, JwtHelper>();

// Azure Blob Storage
var blobConnStr = builder.Configuration["AzureStorage:ConnectionString"]!;
builder.Services.AddSingleton(new BlobServiceClient(blobConnStr));
builder.Services.AddScoped<IImageService, ImageService>();

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// JWT Auth
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate limiting — 10 requests/minute on auth endpoints (sign-in, sign-up, password reset),
// counted per visitor's IP address. One shared count used to let 10 sign-ins from anyone
// in a minute lock out every other customer.
builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 10,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0,
        }));
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Azure App Service passes requests on with the visitor's IP in X-Forwarded-For; read it so
// the limit above counts per visitor rather than per App Service front end. Only the last
// address (the one App Service added) is used, so a visitor can't pick their own.
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.ForwardLimit = 1;
    opts.KnownIPNetworks.Clear(); // App Service's front ends don't have fixed addresses
    opts.KnownProxies.Clear();
});

// Background token cleanup (runs every 24 h)
builder.Services.AddHostedService<TokenCleanupService>();
// Reminds about and cancels unpaid bank-transfer orders (every Orders:UnpaidCheckMinutes)
builder.Services.AddHostedService<UnpaidOrderService>();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!;
builder.Services.AddCors(opts =>
    opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hytel Phones API",
        Version = "v1",
        Description = "E-commerce backend for phones, tablets, smart watches and more."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseForwardedHeaders(); // before the rate limiter, which counts per visitor IP
app.UseRateLimiter();

// Swagger lists every endpoint, so it's only served when running locally. On live, the root
// address just says the API is up (Azure's Always On and any uptime checks ping it).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hytel Phones API v1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    app.MapGet("/", () => "Hytel Phones API is running.");
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");


app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();