using DemoMinimAPI.Data;
using DemoMinimAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MiniValidation;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

namespace DemoMinimAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            #region Configure Services

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Minimal API Sample",
                    Description = "Developed by Eduardo Pires - Owner @ desenvolvedor.io",
                    Contact = new OpenApiContact { Name = "Eduardo Pires", Email = "contato@eduardopires.net.br" },
                    License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insira o token JWT desta maneira: Bearer {seu token}",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
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
            new string[] {}
        }
    });
            });

            var connectionString = builder.Configuration.GetConnectionString
             ("ConnectionStringSql");
            builder.Services.AddDbContext<MinimalContextDb>(o =>
            {
                o.UseSqlServer(connectionString);
            });

            builder.Services.AddIdentityEntityFrameworkContextConfiguration(o =>
            o.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionStringSql"),
            b => b.MigrationsAssembly("DemoMinimAPI")));

            builder.Services.AddIdentityConfiguration();
            builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");
            builder.Services.AddAuthorization(o =>
            {
                o.AddPolicy("ExcluirFornecedor",
                policy => policy.RequireClaim("ExcluirFornecedor"));
            });


            var app = builder.Build();
            #endregion
            #region Configure Pipeline
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseAuthConfiguration();
            app.UseHttpsRedirection();

            app.UseAuthorization();

            MapActions(app);





            app.Run();
            #endregion
            #region Action
            void MapActions(WebApplication app)
            {
                app.MapPost("/registro", [AllowAnonymous] async (
                 SignInManager<IdentityUser> signInManager,
                 UserManager<IdentityUser> userManager,
                 IOptions<AppJwtSettings> appJwtSettings,
                 RegisterUser registerUser) =>

                {
                    if (registerUser == null)
                        return Results.BadRequest("Usuario não informado");
                    if (!MiniValidator.TryValidate(registerUser, out var errors))
                        return Results.ValidationProblem(errors);

                    var user = new IdentityUser
                    {
                        UserName = registerUser.Email,
                        Email = registerUser.Email,
                        EmailConfirmed = true


                    };

                    var result = await userManager.CreateAsync(user, registerUser.Password);

                    if (!result.Succeeded)
                        return Results.BadRequest(result.Errors);

                    var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

                    return Results.Ok(jwt);


                })

                .ProducesValidationProblem()
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RegistroUsuario")
                .WithTags("Usuario");


                app.MapPost("/login", [AllowAnonymous] async (
                  SignInManager<IdentityUser> signInManager,
                  UserManager<IdentityUser> userManager,
                  IOptions<AppJwtSettings> appJwtSettings,
                  LoginUser loginUser) =>

                {
                    if (loginUser == null)
                        return Results.BadRequest("Usuario não informado");
                    if (!MiniValidator.TryValidate(loginUser, out var errors))
                        return Results.ValidationProblem(errors);

                    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

                    if (result.IsLockedOut)
                        return Results.BadRequest("Usuario Bloqueado");

                    if (!result.Succeeded)
                        return Results.BadRequest("Usuario ou senha invalida");

                    var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

                    return Results.Ok(jwt);


                })

                 .ProducesValidationProblem()
                 .Produces(StatusCodes.Status200OK)
                 .Produces(StatusCodes.Status400BadRequest)
                 .WithName("LoginUsuario")
                 .WithTags("Usuario");





                var summaries = new[]
                {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
                };

                app.MapGet("/fornecedor", [AllowAnonymous] async (
                    MinimalContextDb context) =>
                await context.Fornecedores.ToListAsync())

                .WithName("GetFornecedor")
                .WithTags("Fornecedor");

                app.MapGet("/fornecedor/{id}", [AllowAnonymous] async (
                  Guid id,
                  MinimalContextDb context) =>

                  await context.Fornecedores.FindAsync(id)
                    is Fornecedor fornecedor
                       ? Results.Ok(fornecedor)
                        : Results.NotFound())
               .Produces<Fornecedor>(StatusCodes.Status200OK)
               .Produces<Fornecedor>(StatusCodes.Status404NotFound)
               .WithName("GetFornecedorPorId")
               .WithTags("Fornecedor");

                app.MapPost("/fornecedor", [Authorize] async (
                    MinimalContextDb context,
                    Fornecedor fornecedor) =>
                {
                    //Validação
                    if (!MiniValidator.TryValidate(fornecedor, out var errors))
                        return Results.ValidationProblem(errors);

                    context.Fornecedores.Add(fornecedor);
                    var result = await context.SaveChangesAsync();

                    return result > 0
                  //  ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
                  ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.Id }, fornecedor)
                  : Results.BadRequest("Houve um problema ao salvar o resgistro");
                })
               .ProducesValidationProblem()
               .Produces<Fornecedor>(StatusCodes.Status201Created)
               .Produces<Fornecedor>(StatusCodes.Status400BadRequest)
               .WithName("PostFornecedor")
               .WithTags("Fornecedor");

                app.MapPut("/fornecedor/{id}", [Authorize] async (
                    Guid id,
                   MinimalContextDb context,
                   Fornecedor fornecedor) =>
                {
                    var fornecedorBanco = await context.Fornecedores.AsNoTracking<Fornecedor>()
                                                                    .FirstOrDefaultAsync(f => f.Id == id);
                    if (fornecedor == null) return Results.NotFound();


                    //Validação
                    if (!MiniValidator.TryValidate(fornecedor, out var errors))
                        return Results.ValidationProblem(errors);

                    context.Fornecedores.Update(fornecedor);
                    var result = await context.SaveChangesAsync();

                    return result > 0
                  //  ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
                  ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.Id }, fornecedor)
                  : Results.BadRequest("Houve um problema ao salvar o resgistro");
                })
              .ProducesValidationProblem()
              .Produces<Fornecedor>(StatusCodes.Status201Created)
              .Produces<Fornecedor>(StatusCodes.Status400BadRequest)
              .WithName("PutFornecedor")
              .WithTags("Fornecedor");

                app.MapDelete("/fornecedor/{id}", [Authorize] async (
                   Guid id,
                    MinimalContextDb context
                  ) =>
                {

                    var fornecedor = await context.Fornecedores.FindAsync(id);
                    if (fornecedor == null) return Results.NotFound();

                    context.Fornecedores.Remove(fornecedor);
                    var result = await context.SaveChangesAsync();

                    return result > 0
                  //  ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor)
                  ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.Id }, fornecedor)
                  : Results.BadRequest("Houve um problema ao salvar o resgistro");
                })
              .ProducesValidationProblem()
              .Produces<Fornecedor>(StatusCodes.Status201Created)
              .Produces<Fornecedor>(StatusCodes.Status400BadRequest)
              .RequireAuthorization("ExcluirFornecedor")
              .WithName("DeleteFornecedor")
              .WithTags("Fornecedor");

               
            }
            #endregion
        }
    }
}