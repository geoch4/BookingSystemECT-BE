// Backend purpose: Configures dependency injection, middleware, database startup, and HTTP routing for the API.
using System.Net;
using System.Net.Sockets;
using System.Text;
using Bogus.DataSets;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Behaviors;
using BookingSystem.Application.Integrations;
using BookingSystem.Application.Queries.GetAllUsers;
using BookingSystem.Application.Service.Implementation;
using BookingSystem.Application.Validators;
using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.DataBase;
using BookingSystem.Infrastructure.IRepository;
using BookingSystem.Infrastructure.Repositories;
using BookingSystem.Infrastructure.Repository;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Read authentication settings once so the same values configure JWT validation below.
            var jwtKey = builder.Configuration["Jwt:SecretKey"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];

            // Register the SQL Server-backed EF Core context used by handlers and repositories.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register repository abstractions for legacy service flows that do not use MediatR directly.
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            
            

            // These service registrations are retained as documentation while active controllers use MediatR handlers.
            //builder.Services.AddScoped<IPatientService, PatientService>();
            //builder.Services.AddScoped<IUserService, UserService>();
            //builder.Services.AddScoped<IRoleService, RoleService>();
            //builder.Services.AddScoped<ITreatmentTypeService, TreatmentTypeService>();
            //builder.Services.AddScoped<IDocumentService, DocumentService>();
            //builder.Services.AddScoped<IBookingService, BookingService>();
            //builder.Services.AddScoped<INotificationService, NotificationService>();
           
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAuditContext, HttpAuditContext>();
            builder.Services.AddScoped<AuditLogWriter>();
            builder.Services.Configure<IntegrationReadinessOptions>(
                builder.Configuration.GetSection("Integrations"));
            builder.Services.AddScoped<IExternalIntegrationGateway, DisabledExternalIntegrationGateway>();
            
            builder.Services.AddAccountAuthorization();

            // Discover commands, queries, and handlers from the application assembly.
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(GetAllUsersQuery).Assembly);
            });
            
            
            // Apply exception handling, logging, and validation around every MediatR request.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            
            // Register FluentValidation validators used by the validation pipeline.
            builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreatePatientDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateNotificationDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateRoleDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateDocumentDtoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserDtoValidator>();


            



            
            // Validate bearer tokens against the configured issuer, audience, lifetime, and signing key.
            builder.Services.AddAccountJwtAuthentication(builder.Configuration);

            // The seeder runs after migrations and only fills development tables that are empty.
            builder.Services.AddScoped<DatabaseSeeder>();

            // Expose attribute-routed controllers and OpenAPI metadata.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BookingSystem API", Version = "v1" });

                // Add a bearer-token input to Swagger so authenticated endpoints can be tested interactively.
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer {token}'. Example: 'Bearer eyJhbGciOi...'"
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
                        []
                    }
                });
            });

            
            builder.Services.AddCors();

            
            var app = builder.Build();

            // Apply pending schema migrations before any request can reach the application.
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();

                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                seeder.SeedData();
            }
            
            // Swagger is available only during local development.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            // Convert controlled and unexpected exceptions before authentication or controllers handle the response.
            app.UseMiddleware<ExceptionMiddleware.ExceptionMiddleware>();
            
            app.UseCors(corsPolicyBuilder =>
            {
                corsPolicyBuilder
                    .AllowAnyOrigin() // Replace with WithOrigins(...) when deployment origins are known.
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            
            // Finish the request pipeline with HTTPS, security, and controller routing.
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
