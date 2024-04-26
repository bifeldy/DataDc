using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.AttributeFilterDecorator;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

using bifeldy_sd3_mbz_60.Abstractions;
using bifeldy_sd3_mbz_60.Models;
using bifeldy_sd3_mbz_60.Services;

namespace bifeldy_sd3_mbz_60.Controllers {

    [ApiController]
    [Route("prod-mast")]
    public sealed class ProdMastController : CBaseController {

        public ProdMastController(
            IOptions<ENV> env,
            IApplicationService app,
            IHttpService http,
            IOraPg orapg,
            IConverterService cs,
            IGeneralRepository generalRepo,
            IProdMastService prodMast
        ) : base(env, app, http, orapg, cs, generalRepo, prodMast) {
            ExcludeJenisDc = new List<string> { "LPG", "IPLAZA" };
        }

        [HttpPost]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Ambil data Prod Mast dari / berdasarkan kode gudang tertentu (ex. G001)")]
        public override async Task<dynamic> HitDcAPI(
            [FromBody, SwaggerParameter("JSON body yang berisi kode gudang yang diminta", Required = true)] InputJsonDc fd,
            [FromQuery, SwaggerParameter("Angka nomor halaman yang diminta", Required = false)] string page = "1",
            [FromQuery, SwaggerParameter("Jumlah data yang di minta dalam satu halaman (max. 100)", Required = false)] string row = "10",
            [FromQuery, SwaggerParameter("Menggunakan key dari JSON yang akan di order", Required = false)] string sort = "plumd",
            [FromQuery, SwaggerParameter("ASC / DESC saat sorting", Required = false)] string order = "asc"
        ) {
            return await base.HitDcAPI(fd, page, row, sort, order);
        }

        /* ** */

        [HttpPost("mirror")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Server cadangan (mirror) jika yang utama down")]
        public override async Task<IActionResult> DirectDbConnection(
            [FromBody] InputJsonDc fd,
            [FromQuery] string page = "1",
            [FromQuery] string row = "10",
            [FromQuery] string sort = "plumd",
            [FromQuery] string order = "asc"
        ) {
            return await base.DirectDbConnection(fd, page, row, sort, order);
        }

    }

}
