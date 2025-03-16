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
using Microsoft.AspNetCore.Http.Features;
using PixelPerfect.Core.Entities;
using PixelPerfect.DataAccess.Repos;
using PixelPerfect.Services;
using PixelPerfect.Services.Impl;
using System.IO; // 用于文件和目录操作
using SixLabors.ImageSharp.Web;

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

                        services.AddDbContext<PhotoBookingDbContext>(options =>
                            options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                                new MySqlServerVersion(new Version(8, 0, 28))));

                        // 注册文件存储服务
                        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

                        // 添加 ImageSharp 支持
                        //services.AddImageSharp();

                        // 配置文件上传大小限制
                        services.Configure<FormOptions>(options =>
                        {
                            options.MultipartBodyLengthLimit = 20971520; // 20MB
                        });

                        // 注册仓库
                        services.AddScoped<UserRepo>();
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

                        // 注册预约相关服务
                        services.AddScoped<BookingRepo>();
                        services.AddScoped<IBookingService, BookingService>();

                        // 注册修图订单相关服务
                        // 注册所有需要的仓库
                        services.AddScoped<PhotoRepo>();
                        services.AddScoped<RetoucherRepo>();
                        services.AddScoped<RetouchOrderRepo>();

                        // 注册所有需要的服务
                        services.AddScoped<IPhotoService, PhotoService>();
                        services.AddScoped<IRetoucherService, RetoucherService>();
                        services.AddScoped<IRetouchOrderService, RetouchOrderService>();

                        // 注册通知相关服务
                        services.AddScoped<NotificationRepo>();
                        services.AddScoped<INotificationService, NotificationService>();

                        // 注册社区功能相关服务
                        services.AddScoped<PostRepo>();
                        services.AddScoped<LikeRepo>();
                        services.AddScoped<IPostService, PostService>();
                        services.AddScoped<ILikeService, LikeService>();

                        // 在 Program.cs 中添加下列服务注册

                        // 数据访问层
                        services.AddScoped<PixelPerfect.DataAccess.Repos.RoleApplicationRepo>();

                        // 服务层
                        services.AddScoped<PixelPerfect.Services.IRoleApplicationService, PixelPerfect.Services.Impl.RoleApplicationService>();


                        // 注册关注功能相关仓库
                        services.AddScoped<FollowRepo>();
                        // 注册关注功能相关服务
                        services.AddScoped<IFollowService, FollowService>();

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

                        // 确保上传目录结构存在
                        if (!string.IsNullOrEmpty(env.WebRootPath))
                        {
                            // 确保基础上传目录存在
                            var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
                            if (!Directory.Exists(uploadsDir))
                                Directory.CreateDirectory(uploadsDir);

                            // 确保照片上传目录存在
                            var photosDir = Path.Combine(uploadsDir, "photos");
                            if (!Directory.Exists(photosDir))
                                Directory.CreateDirectory(photosDir);

                            // 确保作品集上传目录存在
                            var portfolioDir = Path.Combine(uploadsDir, "portfolio");
                            if (!Directory.Exists(portfolioDir))
                                Directory.CreateDirectory(portfolioDir);

                            // 确保摄影师作品集目录存在
                            var photographerDir = Path.Combine(portfolioDir, "photographer");
                            if (!Directory.Exists(photographerDir))
                                Directory.CreateDirectory(photographerDir);

                            // 确保修图师作品集目录存在
                            var retoucherDir = Path.Combine(portfolioDir, "retoucher");
                            if (!Directory.Exists(retoucherDir))
                                Directory.CreateDirectory(retoucherDir);

                            // 确保头像上传目录存在
                            var avatarsDir = Path.Combine(uploadsDir, "avatars");
                            if (!Directory.Exists(avatarsDir))
                                Directory.CreateDirectory(avatarsDir);

                            // 确保社区帖子上传目录存在
                            var postsDir = Path.Combine(uploadsDir, "posts");
                            if (!Directory.Exists(postsDir))
                                Directory.CreateDirectory(postsDir);
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

                            // 创建各种上传目录
                            var dirsToCreate = new[] {
                                Path.Combine(uploadsDir, "photos"),
                                Path.Combine(uploadsDir, "portfolio"),
                                Path.Combine(uploadsDir, "portfolio", "photographer"),
                                Path.Combine(uploadsDir, "portfolio", "retoucher"),
                                Path.Combine(uploadsDir, "avatars"),
                                Path.Combine(uploadsDir, "posts")
                            };

                            foreach (var dir in dirsToCreate)
                            {
                                if (!Directory.Exists(dir))
                                    Directory.CreateDirectory(dir);
                            }
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