using DemoMinimAPI.Data;
using DemoMinimAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoMinimAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            var connectionString = builder.Configuration.GetConnectionString
    ("ConnectionStringMySql");
            builder.Services.AddDbContext<MinimalContextDb>(o =>
            {
                o.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

            app.MapGet("/fornecedor", async (
                MinimalContextDb context) =>
            await context.Fornecedores.ToListAsync())

            .WithName("GetFornecedor")
            .WithTags("Fornecedor");

            app.MapGet("/fornecedor/{id}", async (
              Guid id,
              MinimalContextDb context) =>

              await context.Fornecedores.FindAsync(id)
                is Fornecedor fornecedor
                   ? Results.Ok(fornecedor)
                    :Results.NotFound())
           .Produces<Fornecedor>(StatusCodes.Status200OK)
           .Produces<Fornecedor>(StatusCodes.Status404NotFound)
           .WithName("GetFornecedorPorId")
           .WithTags("Fornecedor");

            app.Run();
        }
    }
}