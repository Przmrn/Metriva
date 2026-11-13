using Microsoft.EntityFrameworkCore;
using Metriva.Data;
using Metriva.Features.Customers;
using Metriva.Features.FinancingApplications;

var builder = WebApplication.CreateBuilder(args);

// Register database connection
builder.Services.AddDbContext<MetrivaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Metriva")
    )
);

// Register OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Metriva health check
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        application = "Metriva",
        status = "Healthy"
    });
});

// Get all customers
app.MapGet("/api/customers", async (MetrivaDbContext db) =>
{
    var customers = await db.Customers
        .AsNoTracking()
        .OrderBy(c => c.CreatedAt)
        .ToListAsync();

    return Results.Ok(customers);
});

// Register a new customer
app.MapPost("/api/customers", async (
    CreateCustomerRequest request,
    MetrivaDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.FirstName) ||
        string.IsNullOrWhiteSpace(request.LastName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.PhoneNumber))
    {
        return Results.BadRequest(new
        {
            message = "All customer fields are required."
        });
    }


    var normalizedEmail = request.Email.Trim().ToLowerInvariant();

    var emailExists = await db.Customers.AnyAsync(
        c => c.Email.ToLower() == normalizedEmail
    );

    if (emailExists)
    {
        return Results.Conflict(new
        {
            message = "A customer with this email already exist."
        });
    }

    var customer = new Customer
    {
        FirstName = request.FirstName.Trim(),
        LastName = request.LastName.Trim(),
        Email = request.Email.Trim(),
        PhoneNumber = request.PhoneNumber.Trim()
    };

    db.Customers.Add(customer);

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/customers/{customer.Id}",
        customer
    );
});

// Get all financing applications
app.MapGet("/api/financing-applications", async (
    MetrivaDbContext db) =>
{
    var applications = await db.FinancingApplications
        .AsNoTracking()
        .OrderByDescending(f => f.CreatedAt)
        .Select(f => new
        {
            f.Id,
            f.CustomerId,
            f.PurchasePrice,
            f.DownPayment,
            f.AmountFinanced,
            f.TermMonths,
            f.Status,
            f.CreatedAt,
        })
        .ToListAsync();

    return Results.Ok(applications);
});

// Submit a functioning application
app.MapPost("/api/financing-applications", async (
    CreateFinancingApplicationRequest request,
    MetrivaDbContext db) =>
{
    // Validate purchase price
    if (request.PurchasePrice <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Purchase price must be greater than 0."
        });
    }

    // Validate down payment
    if (request.DownPayment < 0 ||
        request.DownPayment >= request.PurchasePrice)
    {
        return Results.BadRequest(new
        {
            message = "Down payment must be at least zero and less than the purchase price."
        });
    }

    // Validate financing term
    if (request.TermMonths is not (3 or 6 or 9 or 12))
    {
        return Results.BadRequest(new
        {
            message = "Financing term must be 3, 6, 9, or 12 months."
        });
    }

    // Check whether the customer exists
    var customerExist = await db.Customers.AnyAsync(
        c => c.Id == request.CustomerId
    );

    if (!customerExist)
    {
        return Results.NotFound(new
        {
            message = "Customer was not found."
        });
    }

    // Calculate the financed amount
    var amountFinanced = request.PurchasePrice - request.DownPayment;

    var application = new FinancingApplications
    {
        CustomerId = request.CustomerId,
        PurchasePrice = request.PurchasePrice,
        DownPayment = request.DownPayment,
        AmountFinanced = amountFinanced,
        TermMonths = request.TermMonths,
        Status = "Pending"
    };

    db.FinancingApplications.Add(application);

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/financing-applications/{application.Id}",
        application
    );
});

app.Run();
