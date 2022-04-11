global using System.Linq.Expressions;
global using System.Reflection;
global using Microsoft.EntityFrameworkCore;
global using System.Data.SqlClient;
global using Dapper;
global using Dapper.Contrib.Extensions;
global using System.Data;
using Microsoft.AspNetCore.ResponseCompression;
using RepositoryDemo.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<MemoryRepository<Customer>>(x =>
  new MemoryRepository<Customer>("Id"));
builder.Services.AddTransient<RepositoryDemoContext, RepositoryDemoContext>();
builder.Services.AddTransient<EFRepository<Customer, RepositoryDemoContext>>();
builder.Services.AddTransient<DapperRepository<Customer>>(s =>
    new DapperRepository<Customer>(
        builder.Configuration.GetConnectionString("RepositoryDemoConnectionString")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
