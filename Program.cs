using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PixelPerfect.Entities;
using PixelPerfect.Repos;
using PixelPerfect.Services;
using PixelPerfect.Services.Impl;

namespace PixelPerfect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        var env = context.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                              .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                              .AddEnvironmentVariables();
                    });

                    webBuilder.ConfigureServices((context, services) =>
                    {
                        var configuration = context.Configuration;

                        // 从配置中获取 JWT 相关设置
                        var jwtSettings = configuration.GetSection("JwtSettings");
                        var secretKey = jwtSettings.GetValue<string>("SecretKey");
                        var issuer = jwtSettings.GetValue<string>("Issuer");
                        var audience = jwtSettings.GetValue<string>("Audience");

                        // 添加身份验证服务
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                options.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = false,
                                    ValidateAudience = false,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    //ValidIssuer = issuer,
                                    //ValidAudience = audience,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                                };
                            });

                        services.AddControllers().AddNewtonsoftJson(options =>
                            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                        );

                        services.AddHttpContextAccessor();

                        services.AddDbContext<PhotobookingdbContext>(options =>
                            options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                                new MySqlServerVersion(new Version(8, 0, 28))));

                        // 注册仓库
                        services.AddScoped<UserRepo>();
                        //services.AddScoped<BookingRepo>();
                        //services.AddScoped<PortfolioRepo>();
                        //services.AddScoped<PhotoRepo>();
                        //services.AddScoped<PostRepo>();
                        //services.AddScoped<RetouchOrderRepo>();
                        //services.AddScoped<NotificationRepo>();

                        // 注册服务
                        services.AddScoped<IUser, UserService>();
                        //services.AddScoped<IBooking, BookingService>();
                        //services.AddScoped<IPortfolio, PortfolioService>();
                        //services.AddScoped<IPhoto, PhotoService>();
                        //services.AddScoped<IPost, PostService>();
                        //services.AddScoped<IRetouchOrder, RetouchOrderService>();
                        //services.AddScoped<INotification, NotificationService>();

                        // Swagger配置
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PixelPerfect API", Version = "v1" });

                            // 添加JWT认证配置
                            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                            {
                                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                                Name = "Authorization",
                                In = ParameterLocation.Header,
                                Type = SecuritySchemeType.ApiKey,
                                Scheme = "Bearer"
                            });

                            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

                        // Add CORS policy
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowAnyOrigin",
                                builder => builder.AllowAnyOrigin()
                                                  .AllowAnyHeader()
                                                  .AllowAnyMethod());
                        });
                    });

                    webBuilder.Configure((context, app) =>
                    {
                        var env = context.HostingEnvironment;

                        if (env.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }

                        app.UseHttpsRedirection();

                        app.UseRouting();
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PixelPerfect API v1"));
                        }

                        app.UseCors("AllowAnyOrigin");

                        // 启用身份验证中间件
                        app.UseAuthentication();
                        app.UseAuthorization();

                        // 在 app.UseEndpoints 之前添加
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", context =>
                            {
                                context.Response.Redirect("/swagger");
                                return Task.CompletedTask;
                            });

                            endpoints.MapControllers();
                        });
                    });
                });
    }
}