using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DevIO.Api.Data
{
    /// <summary>
    /// Configurado pelo IdentityConfig, chamado no Program.cs.
    /// Migration gerada através do código: PM console: "Add-Migration Identity -Context ApplicationDbContext".
    /// Banco atualizado com o comando: "Update-Database -Context ApplicationDbContext".
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    }
}