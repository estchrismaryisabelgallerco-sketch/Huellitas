
using Huellitas.Data;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Services
{
    public class CitasVencidasService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CitasVencidasService> _logger;

        private static readonly TimeSpan DuracionCita =
            TimeSpan.FromMinutes(45);

        private static readonly TimeSpan IntervaloRevision =
            TimeSpan.FromMinutes(5);

        public CitasVencidasService(
            IServiceScopeFactory scopeFactory,
            ILogger<CitasVencidasService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Servicio de citas vencidas iniciado.");

            // Primera revisión inmediatamente al iniciar la aplicación.
            await RevisarCitasVencidasAsync(stoppingToken);

            // Revisiones periódicas mientras la aplicación esté funcionando.
            using var timer = new PeriodicTimer(IntervaloRevision);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RevisarCitasVencidasAsync(stoppingToken);
            }
        }

        private async Task RevisarCitasVencidasAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                // Obtener la hora actual de Bolivia.
                TimeZoneInfo zonaBolivia = ObtenerZonaHorariaBolivia();

                DateTime horaBolivia = TimeZoneInfo
                    .ConvertTime(DateTimeOffset.UtcNow, zonaBolivia)
                    .DateTime;

                // Una cita se considera vencida cuando ya transcurrieron
                // sus 45 minutos de duración.
                DateTime limite = horaBolivia.Subtract(DuracionCita);

                await using var scope = _scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<HuellitasContext>();

                // Actualización condicional directamente en MariaDB.
                // Nunca modifica citas Atendidas ni Canceladas.
                int cantidadCancelada = await context.Citas
                    .Where(c =>
                        c.FechaHora <= limite &&
                        (c.Estado == "Pendiente" ||
                         c.Estado == "Confirmada"))
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            c => c.Estado,
                            "Cancelada"),
                        stoppingToken
                    );

                _logger.LogInformation(
                    "Revisión de citas finalizada. " +
                    "Hora Bolivia: {HoraBolivia}. " +
                    "Citas canceladas: {Cantidad}.",
                    horaBolivia,
                    cantidadCancelada
                );
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Cierre normal del servicio.
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al revisar las citas vencidas.");
            }
        }

        private static TimeZoneInfo ObtenerZonaHorariaBolivia()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/La_Paz");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "SA Western Standard Time");
            }
        }
    }
}