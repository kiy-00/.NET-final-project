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
using System.IO; // 用于文件和目录操作

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
                        services.AddScoped<IUserService, UserService>();

                        // 添加摄影师服务
                        services.AddScoped<PhotographerRepo>();
                        services.AddScoped<IPhotographerService, PhotographerService>();

                        // 添加修图师服务
                        services.AddScoped<RetoucherRepo>();
                        services.AddScoped<IRetoucherService, RetoucherService>();

                        // 添加作品集服务
                        services.AddScoped<PortfolioRepo>();
                        services.AddScoped<IPortfolioService, PortfolioService>();
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
                        // 添加静态文件中间件
                        app.UseStaticFiles();

                        // 确保上传目录存在
                        if (!string.IsNullOrEmpty(env.WebRootPath))
                        {
                            var uploadDir = Path.Combine(env.WebRootPath, "uploads", "portfolio");
                            if (!Directory.Exists(uploadDir))
                            {
                                Directory.CreateDirectory(uploadDir);
                            }
                        }
                        else
                        {
                            // WebRootPath为null时，使用ContentRootPath创建目录
                            var contentRoot = env.ContentRootPath;
                            var wwwrootDir = Path.Combine(contentRoot, "wwwroot");

                            // 确保wwwroot目录存在
                            if (!Directory.Exists(wwwrootDir))
                                Directory.CreateDirectory(wwwrootDir);

                            var uploadsDir = Path.Combine(wwwrootDir, "uploads");
                            if (!Directory.Exists(uploadsDir))
                                Directory.CreateDirectory(uploadsDir);

                            var portfolioDir = Path.Combine(uploadsDir, "portfolio");
                            if (!Directory.Exists(portfolioDir))
                                Directory.CreateDirectory(portfolioDir);
                        }

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