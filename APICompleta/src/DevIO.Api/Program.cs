using DevIO.Api.Configuration;
using DevIO.Api.Extensions;
using DevIO.Data.Context;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);

//Adiciona contexto de banco de dados.
builder.Services.AddDbContext<MeuDbContext>(options => 
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Adiciona configuração do identity.
builder.Services.AddIdentityConfiguration(builder.Configuration);

//Controllers.
builder.Services.AddControllers();

//Migrado para arquivo de configuração específico para swagger.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
//});

//AutoMapper: Produra todas classes que implementam "Profile" para buscar mapeamentos, no assembly do tipo informado.
builder.Services.AddAutoMapper(typeof(Program));

//Configurações do swagger.
builder.Services.AddSwaggerConfig();

//DI.
builder.Services.ResolveDependencies();

//Versionamento da API.
builder.Services.AddApiVersioning(options => {
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(2, 0); //Versão da API. Exemplo: 2.0.
    options.ReportApiVersions = true; //Avisa sobre versões obsoletas.
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; //'v' 3.0.0 por exemplo.
    options.SubstituteApiVersionInUrl = true; //Versão na url.
});

//Desativa validação de models padrão do asp.net.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

//Sobre o CORS:
//O Cors por padrão é fechado, caso queira "relaxar" a segurança, adicionar políticas.
//O Cors só funciona entre navegadores, então de uma Applicação SPA por exemplo, mesmo com uma política restritiva, o CORS não vai funcionar.
//Caso queira restringir totalmente o Cross Origin mesmo em uma Aplicação SPA, adicionar a seguinte anotação na controller: [DisableCors].
//Caso queria explicitar uma política do CORS em uma controller: [EnableCors("Development")].
//As políticas aplicadas via anotação, não sobrescrevem as políticas adicionadas no: UseCors(...) do Program.cs.
//Testar CORS no ambiente de produção, porque testar local é complicado.
builder.Services.AddCors(options => {
    //Segurança, aberto para qualquer origem.
    options.AddPolicy("Development",
        builderPolicy => builderPolicy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

    //Adicionando algumas regras de "relaxar" a segurança do CORS.
    options.AddPolicy("Production",
        builderPolicy => builderPolicy
            .WithMethods("GET")
            .WithOrigins("http://dominio.com.br")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            //.WithHeaders(HeaderNames.ContentType, "x-custom-header")
            .AllowAnyHeader());

    //Política padrão.
    //options.AddDefaultPolicy(
    //    builderPolicy => builderPolicy
    //        .AllowAnyHeader()
    //        .AllowAnyMethod()
    //        .AllowAnyHeader()
    //        .AllowCredentials());
});

//HealthChecks.
builder.Services.AddHealthChecks()
    .AddCheck("Produtos", new SqlServerHealthCheck(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "BancoSQL");

builder.Services.AddHealthChecksUI().AddSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

//Migrado para arquivo de configuração específico do swagger.
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c => 
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
//    });
//}

//Redireciona de HTTP para HTTPS, porém não obriga o uso do HTTPS.
//Para obrigar o uso, utilizar "app.UseHsts", confome abaixo.
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    //Informa que usará o Cors "Development".
    app.UseCors("Development");

    app.UseDeveloperExceptionPage();
}
else
{
    //Informa que usará o Cors "Development".
    app.UseCors("Production");

    //Força uso do HTTPS.
    app.UseHsts();
}

//Usar autenticação do Identity.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

//Usar configs do swagger.
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);

//HealthChecks.
app.UseHealthChecks("/api/hc", new HealthCheckOptions() 
{ 
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
//Depende de configuração no appsettings.json, na chave "HealthChecks-UI".
app.UseHealthChecksUI(options => 
{ 
    options.UIPath = "/api/hc-ui";
    options.ResourcesPath = $"{options.UIPath}/resources";
    options.UseRelativeApiPath = false;
    options.UseRelativeResourcesPath = false;
    options.UseRelativeWebhookPath = false;
});

app.Run();
