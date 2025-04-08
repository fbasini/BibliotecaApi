using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using BibliotecaAPI.Data;
using BibliotecaAPI.Services;
using BibliotecaAPI.Utilities;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Entities;

var builder = WebApplication.CreateBuilder(args);

// servicios

builder.Services.AddOutputCache(opciones =>
{
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
});

//builder.Services.AddStackExchangeRedisOutputCache(opciones =>
//{
//    opciones.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
//});

builder.Services.AddDataProtection();

var allowedOrigins = builder.Configuration.GetSection("allowedOrigins").Get<string[]>()!;

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsOptions =>
    {
        corsOptions.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("total-records-count");
        //opcionesCORS.WithOrigins(origenesPermitidos)
        //    .AllowAnyMethod()
        //    .AllowAnyHeader()
        //    .WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers(options =>
{
    //options.Conventions.Add(new GroupByVersionConvention());
}).AddNewtonsoftJson();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IFileStorage, AzureFileStorage>();
//builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddScoped<BookValidationFilter>();
builder.Services.AddScoped<IAuthorService, AuthorService>();

builder.Services.AddScoped<ILinkGenerator, BibliotecaAPI.Services.LinkGenerator>();

builder.Services.AddScoped<HATEOASAuthorAttribute>();
builder.Services.AddScoped<HATEOASAuthorsAttribute>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["jwtkey"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("isadmin", policy => policy.RequireClaim("isadmin"));
});

//builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("biblioteca", new OpenApiInfo { 
        Title = "Biblioteca API", 
        Version = "v1",
        Description = "This web API provides functionality for managing authors and books in a library system",
        Contact = new OpenApiContact
        {
            Name = "Felipe Basini",
            Email = "felipebasini97@gmail.com"
        },
        License = new OpenApiLicense()
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.OperationFilter<AuthorizationFilter>();
    options.EnableAnnotations();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// middlewares

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        ErrorMessage = exception.Message,
        StrackTrace = exception.StackTrace,
        Date = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new
    {
        type = "error",
        message = "An unexpected error occurred",
        status = 500
    }).ExecuteAsync(context);
}));

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/biblioteca/swagger.json", "Biblioteca API");
});

app.UseStaticFiles();

app.UseCors();

app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program { }