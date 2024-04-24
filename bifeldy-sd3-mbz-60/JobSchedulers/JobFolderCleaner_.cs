using Microsoft.Extensions.Options;

using Quartz;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Services;

using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.JobSchedulers {

    public sealed class JobFolderCleaner : IJob {

        private readonly ILogger<JobFolderCleaner> _logger;
        private readonly ENV _env;

        private readonly IOraPg _orapg;
        private readonly IBerkasService _berkas;

        public JobFolderCleaner(
            ILogger<JobFolderCleaner> logger,
            IOptions<ENV> env,
            IOraPg orapg,
            IBerkasService berkas
        ) {
            _logger = logger;
            _env = env.Value;
            _orapg = orapg;
            _berkas = berkas;
        }

        public async Task Execute(IJobExecutionContext _context) {
            try {
                DateTime dt = await _orapg.ExecScalarAsync<DateTime>($@"SELECT {(_env.IS_USING_POSTGRES ? "NOW()" : "SYSDATE FROM DUAL")}");
                _logger.LogInformation($"Tanggal & Waktu Database => {dt}");
                _berkas.CleanUp(false);
            }
            catch (Exception ex) {
                _logger.LogError($"{_context.Scheduler.SchedulerName} {ex.Message}");
            }
        }

    }

}
