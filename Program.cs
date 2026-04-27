using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

const string JwtIssuer = "coursera-dotnet-assignment";
const string JwtAudience = "coursera-dotnet-assignment-client";
const string JwtSigningKey = "coursera-dotnet-assignment-demo-signing-key-12345";
const string TestUsername = "testuser";
const string TestPassword = "Password123!";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = JwtIssuer,
            ValidAudience = JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.WebHost.UseKestrel(options =>
{
    options.ListenLocalhost(5000);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RequestLogging");

    logger.LogInformation(
        "HTTP {Method} {Path} requested at {Timestamp}",
        context.Request.Method,
        context.Request.Path,
        DateTime.UtcNow);

    await next();

    logger.LogInformation(
        "HTTP {Method} {Path} responded with {StatusCode}",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode);
});

app.UseAuthentication();
app.UseAuthorization();

var users = new Dictionary<int, User>
{
    [1] = new(1, "Alice", "alice@example.com"),
    [2] = new(2, "Bob", "bob@example.com")
};

app.MapPost("/login", (LoginRequest request) =>
{
    if (request.Username != TestUsername || request.Password != TestPassword)
    {
        return Results.Unauthorized();
    }

    var token = GenerateJwtToken(request.Username);
    return Results.Ok(new
    {
        accessToken = token,
        tokenType = "Bearer",
        expiresIn = 3600
    });
});

var usersApi = app.MapGroup("/users")
    .RequireAuthorization();

usersApi.MapGet("", () => Results.Ok(users.Values));

usersApi.MapGet("/{id:int}", (int id) =>
{
    users.TryGetValue(id, out var user);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

usersApi.MapPost("", (CreateUserRequest request) =>
{
    var validationErrors = ValidateUserRequest(request.Name, request.Email, users);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var nextId = users.Count == 0 ? 1 : users.Keys.Max() + 1;
    var user = new User(nextId, request.Name.Trim(), request.Email.Trim());
    users[user.Id] = user;

    return Results.Created($"/users/{user.Id}", user);
});

usersApi.MapPut("/{id:int}", (int id, UpdateUserRequest request) =>
{
    users.TryGetValue(id, out var user);
    if (user is null)
    {
        return Results.NotFound();
    }

    var validationErrors = ValidateUserRequest(request.Name, request.Email, users, id);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    user.Name = request.Name.Trim();
    user.Email = request.Email.Trim();
    return Results.Ok(user);
});

usersApi.MapDelete("/{id:int}", (int id) =>
{
    if (!users.Remove(id))
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});

static string GenerateJwtToken(string username)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, "Tester")
    };

    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: JwtIssuer,
        audience: JwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static Dictionary<string, string[]> ValidateUserRequest(
    string name,
    string email,
    Dictionary<int, User> users,
    int? currentUserId = null)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(name))
    {
        errors["name"] = ["Name is required."];
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        errors["email"] = ["Email is required."];
        return errors;
    }

    var normalizedEmail = email.Trim();

    if (!IsValidEmail(normalizedEmail))
    {
        errors["email"] = ["Email format is invalid."];
        return errors;
    }

    var emailExists = users.Values.Any(u =>
        u.Id != currentUserId &&
        string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));

    if (emailExists)
    {
        errors["email"] = ["Email must be unique."];
    }

    return errors;
}

static bool IsValidEmail(string email)
{
    try
    {
        _ = new MailAddress(email);
        return true;
    }
    catch (FormatException)
    {
        return false;
    }
}

app.Run();

record LoginRequest(string Username, string Password);

record CreateUserRequest(string Name, string Email);

record UpdateUserRequest(string Name, string Email);

class User
{
    public User(int id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }

    public int Id { get; init; }

    public string Name { get; set; }

    public string Email { get; set; }
}
