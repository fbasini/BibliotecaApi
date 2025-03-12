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
using BibliotecaAPI.Utilities.V1;
using BibliotecaAPI.Entities;

var builder = WebApplication.CreateBuilder(args);

// servicios

//builder.Services.AddOutputCache(opciones =>
//{
//    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
//});

builder.Services.AddStackExchangeRedisOutputCache(opciones =>
{
    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("cantidad-total-registros");
        //opcionesCORS.WithOrigins(origenesPermitidos)
        //    .AllowAnyMethod()
        //    .AllowAnyHeader()
        //    .WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers(opciones =>
{
    opciones.Conventions.Add(new ConvencionAgrupaPorVersion());
}).AddNewtonsoftJson();

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosAzure>();
//builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddScoped<FiltroValidacionLibro>();
builder.Services.AddScoped<BibliotecaAPI.Services.V1.IServicioAutores,
            BibliotecaAPI.Services.V1.ServicioAutores>();

builder.Services.AddScoped<BibliotecaAPI.Services.V1.IGeneradorEnlaces, BibliotecaAPI.Services.V1.GeneradorEnlaces>();

builder.Services.AddScoped<HATEOASAutorAttribute>();
builder.Services.AddScoped<HATEOASAutoresAttribute>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});

//builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Biblioteca API", 
        Version = "v1",
        Description = "Este es un web api para trabajar con autores y libros",
        Contact = new OpenApiContact
        {
            Name = "Felipe Basini",
            Email = "felipe@hotmail.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
        },
    });

    opciones.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Biblioteca API",
        Version = "v2",
        Description = "Este es un web api para trabajar con autores y libros",
        Contact = new OpenApiContact
        {
            Name = "Felipe Basini",
            Email = "felipe@hotmail.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
        },
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opciones.OperationFilter<FiltroAutorizacion>();
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
    var excepcion = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeDeError = excepcion.Message,
        StrackTrace = excepcion.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new
    {
        tipo = "error",
        mensaje = "Ha ocurrido un error inesperado",
        estatus = 500
    }).ExecuteAsync(context);
}));

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API v1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API v2");
});

app.UseStaticFiles();

app.UseCors();

app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program { }