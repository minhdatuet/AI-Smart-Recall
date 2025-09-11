using AISmartRecallAPI.Data;
using AISmartRecallAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure MongoDB
builder.Services.AddSingleton<MongoDBContext>();

// Configure Services
builder.Services.AddScoped<IUserService, UserService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-here-make-it-very-long-and-secure-for-production";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AISmartRecall";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add CORS for Unity client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnityClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AI Smart Recall API",
        Version = "v1",
        Description = "API for AI Smart Recall learning platform"
    });
    
    // Configure JWT authentication in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Smart Recall API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowUnityClient");

// Use Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize MongoDB indexes on startup
try
{
    var mongoContext = app.Services.GetRequiredService<MongoDBContext>();
    await mongoContext.CreateIndexesAsync();
    Console.WriteLine("MongoDB indexes created successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating MongoDB indexes: {ex.Message}");
}

Console.WriteLine("AI Smart Recall API is starting...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("Available endpoints:");
Console.WriteLine("- POST /api/auth/register - User registration");
Console.WriteLine("- POST /api/auth/login - User login");
Console.WriteLine("- GET /api/auth/profile - Get user profile (requires auth)");
Console.WriteLine("- PUT /api/auth/profile - Update user profile (requires auth)");
Console.WriteLine("- PUT /api/auth/api-keys - Update API keys (requires auth)");
Console.WriteLine("- GET /api/auth/ai-providers - Get available AI providers");
Console.WriteLine("- Swagger UI available at: http://localhost:5000");

app.Run();
