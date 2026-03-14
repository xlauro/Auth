using System.Text;
using Auth.Application.Interfaces;
using Auth.Application.UseCases;
using Auth.Domain.Entities;
using Auth.Domain.Validation;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Options;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<RegisterUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<IValidator<User>, UserValidation>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
        if (jwtOptions is null || string.IsNullOrWhiteSpace(jwtOptions.Secret))
        {
            throw new InvalidOperationException(
                "Jwt configuration is missing. Ensure Jwt:Secret, Jwt:Issuer, and Jwt:Audience are configured.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey
                };
            });

        return services;
    }
}

