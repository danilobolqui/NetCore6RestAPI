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

//Adiciona configura��o do identity.
builder.Services.AddIdentityConfiguration(builder.Configuration);

//Controllers.
builder.Services.AddControllers();

//Migrado para arquivo de configura��o espec�fico para swagger.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
//});

//AutoMapper: Produra todas classes que implementam "Profile" para buscar mapeamentos, no assembly do tipo informado.
builder.Services.AddAutoMapper(typeof(Program));

//Configura��es do swagger.
builder.Services.AddSwaggerConfig();

//DI.
builder.Services.ResolveDependencies();

//Versionamento da API.
builder.Services.AddApiVersioning(options => {
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(2, 0); //Vers�o da API. Exemplo: 2.0.
    options.ReportApiVersions = true; //Avisa sobre vers�es obsoletas.
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; //'v' 3.0.0 por exemplo.
    options.SubstituteApiVersionInUrl = true; //Vers�o na url.
});

//Desativa valida��o de models padr�o do asp.net.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

//Sobre o CORS:
//O Cors por padr�o � fechado, caso queira "relaxar" a seguran�a, adicionar pol�ticas.
//O Cors s� funciona entre navegadores, ent�o de uma Applica��o SPA por exemplo, mesmo com uma pol�tica restritiva, o CORS n�o vai funcionar.
//Caso queira restringir totalmente o Cross Origin mesmo em uma Aplica��o SPA, adicionar a seguinte anota��o na controller: [DisableCors].
//Caso queria explicitar uma pol�tica do CORS em uma controller: [EnableCors("Development")].
//As pol�ticas aplicadas via anota��o, n�o sobrescrevem as pol�ticas adicionadas no: UseCors(...) do Program.cs.
//Testar CORS no ambiente de produ��o, porque testar local � complicado.
builder.Services.AddCors(options => {
    //Seguran�a, aberto para qualquer origem.
    options.AddPolicy("Development",
        builderPolicy => builderPolicy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

    //Adicionando algumas regras de "relaxar" a seguran�a do CORS.
    options.AddPolicy("Production",
        builderPolicy => builderPolicy
            .WithMethods("GET")
            .WithOrigins("http://dominio.com.br")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            //.WithHeaders(HeaderNames.ContentType, "x-custom-header")
            .AllowAnyHeader());

    //Pol�tica padr�o.
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

//Migrado para arquivo de configura��o espec�fico do swagger.
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c => 
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
//    });
//}

//Redireciona de HTTP para HTTPS, por�m n�o obriga o uso do HTTPS.
//Para obrigar o uso, utilizar "app.UseHsts", confome abaixo.
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    //Informa que usar� o Cors "Development".
    app.UseCors("Development");

    app.UseDeveloperExceptionPage();
}
else
{
    //Informa que usar� o Cors "Development".
    app.UseCors("Production");

    //For�a uso do HTTPS.
    app.UseHsts();
}

//Usar autentica��o do Identity.
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
//Depende de configura��o no appsettings.json, na chave "HealthChecks-UI".
app.UseHealthChecksUI(options => 
{ 
    options.UIPath = "/api/hc-ui";
    options.ResourcesPath = $"{options.UIPath}/resources";
    options.UseRelativeApiPath = false;
    options.UseRelativeResourcesPath = false;
    options.UseRelativeWebhookPath = false;
});

app.Run();
