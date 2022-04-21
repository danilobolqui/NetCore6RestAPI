using DevIO.Api.Controllers;
using DevIO.Business.Intefaces;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.Api.V2.Controllers
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/teste")]
    public class TesteController : MainController
    {
        private readonly ILogger<TesteController> _logger;

        public TesteController(INotificador notificador, IUser appUser, ILogger<TesteController> logger) : base(notificador, appUser)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Valor()
        {
            //Log mínimo. Usado mais pra desenvolvimento.
            //Microsoft desabilitou. Usar Outro tipo de log.
            _logger.LogTrace("Log de Trace");

            //Teste de debug, focado em desenvolvimento.
            _logger.LogDebug("Log de Debug");

            //Usar para produção. Apenas uma informação.
            _logger.LogInformation("Log de Informação");

            //Usar para produção. Aviso.
            _logger.LogWarning("Log de Aviso");

            //Usar para produção. Erro.
            _logger.LogError("Log de Erro");

            //Usar para produção. Problema crítico, acima do Error.
            _logger.LogCritical("Log de Problema Critico");

            return "Sou a V2";
        }
    }
}
