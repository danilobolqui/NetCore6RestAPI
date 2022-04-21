using DevIO.Api.Data;
using DevIO.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DevIO.Api.Configuration
{
    public static class IdentityConfig
    {
        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, IConfiguration configuration) 
        {
            //Adiciona contexto de banco de dados do Identity.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            //Configurando o Identity.
            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddErrorDescriber<IdentityMensagensPortugues>()
                .AddDefaultTokenProviders();

            //JWT dentro do Identity:
            //Busca dados no appsettings.json.
            var appSettingsSection = configuration.GetSection("AppSettings");
            //Vincula classe AppSettings com dados do arquivo appsettings.
            //Quando esta classe for injetada, ela já virá populada.
            services.Configure<AppSettings>(appSettingsSection);

            //Busca secret.
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            
            //Padrão de autenticação.
            services.AddAuthentication(x =>
            {
                //Padrão.
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //Validação de token.
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                //Requer HTTPS.
                x.RequireHttpsMetadata = true;
                //Guardar token após autenticação.
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    //Validar chave emissor.
                    ValidateIssuerSigningKey = true,
                    //Criptografia.
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    //Validar emissor.
                    ValidateIssuer = true,
                    //Validar audiência (localhost, no caso configurado).
                    ValidateAudience = true,
                    //Informação da audiência.
                    ValidAudience = appSettings.ValidoEm,
                    //Emissor.
                    ValidIssuer = appSettings.Emissor
                };
            });

            return services;
        }
    }
}