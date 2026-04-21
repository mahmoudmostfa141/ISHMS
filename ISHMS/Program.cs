
using System;
using BLL.Services;
using Core.Interfaces;
using DAL.Repositories;
using ISHMS.Core.Interfaces;
using ISHMS.DAL;
using ISHMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Core.Settings;
using ISHMS.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ISHMS.BLL.Services;

var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            // Swagger With JWT Support

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ISHMS API",
                    Version = "v1"
                });

                // ‰÷Ì› “—«— Authorize ›Ì Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "«ﬂ »: Bearer {token}"
                    // „À«·: Bearer eyJhbGciOiJIUzI1NiIs...
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
            });


            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Default")
                ));

            // ====================================================
            //  ASP.NET Identity
            // ====================================================

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;

                    options.Password.RequireLowercase = true;

                    options.Password.RequireUppercase = true;

                    options.Password.RequireNonAlphanumeric = false;

                    options.Password.RequiredLength = 6;

                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>();


            // ====================================================
            // 3. JWT Settings
            // ====================================================

            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection("JwtSettings"));


            // ====================================================
            // 4. JWT Authentication
            // ====================================================

            var jwtSettings = builder.Configuration
                                     .GetSection("JwtSettings")
                                     .Get<JwtSettings>()!;

            builder.Services
                .AddAuthentication(options =>
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

                        ValidIssuer = jwtSettings.Issuer,

                        ValidAudience = jwtSettings.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.Key))
                    };
                });



            // Dependency Injection
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
       builder.Services.AddScoped<IPatientService, PatientService>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();


            var app = builder.Build();


  

            // Seed Roles first time

            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider
                                       .GetRequiredService<RoleManager<IdentityRole>>();

                string[] roles = { "Admin", "Doctor", "Nurse", "Receptionist" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }

            // Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();

