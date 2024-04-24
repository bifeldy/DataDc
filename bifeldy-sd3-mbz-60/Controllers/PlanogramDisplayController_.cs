using System.Data;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorator;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

using bifeldy_sd3_mbz_60.Models;
using bifeldy_sd3_mbz_60.Services;

namespace bifeldy_sd3_mbz_60.Controllers {

    [ApiController]
    [Route("planogram-display")]
    public sealed class PlanogramDisplayController : DataDcController {

        private readonly IApplicationService _app;
        private readonly IHttpService _http;
        private readonly IOraPg _orapg;
        private readonly IConverterService _cs;
        private readonly IGeneralRepository _generalRepo;
        private readonly IPlanogramDisplayService _planDisp;

        private List<string> ExcludeJenisDc = new List<string> { "WHK" };

        public PlanogramDisplayController(
            IOptions<ENV> env,
            IApplicationService app,
            IHttpService http,
            IOraPg orapg,
            IConverterService cs,
            IGeneralRepository generalRepo,
            IPlanogramDisplayService planDisp
        ) : base(env, orapg) {
            _app = app;
            _http = http;
            _orapg = orapg;
            _cs = cs;
            _generalRepo = generalRepo;
            _planDisp = planDisp;
        }

        [HttpPost]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Ambil data Data DC dari / berdasarkan kode gudang tertentu (ex. G001)")]
        public async Task<dynamic> HitDcAPI(
            [FromBody, SwaggerParameter("JSON body yang berisi kode gudang yang diminta", Required = true)] InputJsonDc fd,
            [FromQuery, SwaggerParameter("Angka nomor halaman yang diminta", Required = false)] string page = "1",
            [FromQuery, SwaggerParameter("Jumlah data yang di minta dalam satu halaman (max. 100)", Required = false)] string row = "10",
            [FromQuery, SwaggerParameter("Menggunakan key dari JSON yang akan di order", Required = false)] string sort = "pla_fk_pluid",
            [FromQuery, SwaggerParameter("ASC / DESC saat sorting", Required = false)] string order = "asc"
        ) {
            try {
                if (fd == null || string.IsNullOrEmpty(fd?.kode_dc)) {
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} 😪",
                        result = new {
                            message = "Format data tidak lengkap!"
                        }
                    });
                }

                string currentKodeDc = await _generalRepo.GetKodeDc();
                if (currentKodeDc != "DCHO") {
                    (decimal pages, decimal count, DataTable dt) = await _planDisp.GetDataPaging(_orapg, fd, sort, order, page, row);

                    return Ok(new ResponseJsonMulti<dynamic> {
                        info = $"😅 201 - {GetType().Name} 🤣",
                        results = dt.ToList<DC_PLANOGRAM_DISPLAY_V>(),
                        pages = pages,
                        count = count
                    });
                }
                else {
                    ObjectResult er = await CheckExcludeJenisDc(fd, ExcludeJenisDc);
                    if (er != null) {
                        return er;
                    }

                    string e = null;
                    Uri u = null;
                    await _generalRepo.GetDcApiPathAppFromHo(Request, fd.kode_dc, (err, res) => {
                        e = err;
                        u = res;
                    });

                    if (u == null) {
                        return BadRequest(new ResponseJsonSingle<dynamic> {
                            info = $"🙄 400 - {GetType().Name} 😪",
                            result = new {
                                message = e
                            }
                        });
                    }

                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(_cs.ObjectToJson(fd)))) {
                        Request.Body = ms;
                        return await _http.ForwardRequest(u.ToString(), Request, Response);
                    }
                }
            }
            catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"🙄 500 - {GetType().Name} 😪",
                    result = new {
                        message = _app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
                    }
                });
            }
        }

        /* ** ALTERNATIVE ** */

        [HttpPost("mirror")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Server cadangan (mirror) jika yang utama down")]
        public async Task<IActionResult> DirectDbConnection(
            [FromBody] InputJsonDc fd,
            [FromQuery] string page = "1",
            [FromQuery] string row = "10",
            [FromQuery] string sort = "pla_fk_pluid",
            [FromQuery] string order = "asc"
        ) {
            try {
                string currentKodeDc = await _generalRepo.GetKodeDc();
                if (currentKodeDc != "DCHO") {
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} (Mirror) 😪",
                        result = new {
                            message = "Endpoint ini hanya dapat diakses melalui HO"
                        }
                    });
                }

                if (fd == null || string.IsNullOrEmpty(fd?.kode_dc)) {
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} (Mirror) 😪",
                        result = new {
                            message = "Format data tidak lengkap!"
                        }
                    });
                }

                ObjectResult er = await CheckExcludeJenisDc(fd, ExcludeJenisDc);
                if (er != null) {
                    return er;
                }

                (bool dbIsUsingPostgre, CDatabase dbOraPg, CDatabase dbMsSql) = await _generalRepo.OpenConnectionToDcFromHo(fd.kode_dc);
                if (dbOraPg == null) {
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} (Mirror) 😪",
                        result = new {
                            message = $"Kode gudang ({fd.kode_dc}) tidak tersedia!"
                        }
                    });
                }

                (decimal pages, decimal count, DataTable dt) = await _planDisp.GetDataPaging(dbOraPg, fd, sort, order, page, row);

                return Ok(new ResponseJsonMulti<dynamic> {
                    info = $"😅 201 - {GetType().Name} (Mirror) 🤣",
                    results = dt.ToList<DC_PLANOGRAM_DISPLAY_V>(),
                    pages = pages,
                    count = count
                });
            }
            catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"🙄 500 - {GetType().Name} (Mirror) 😪",
                    result = new {
                        message = _app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
                    }
                });
            }
        }

    }

}
