using System.Data;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.Abstractions {

    public abstract class CBaseController : ControllerBase {

        private readonly ENV _env;

        private readonly IApplicationService _app;
        private readonly IHttpService _http;
        private readonly IOraPg _orapg;
        private readonly IConverterService _cs;
        private readonly IGeneralRepository _generalRepo;
        private readonly IBaseService _baseService;

        protected List<string> ExcludeJenisDc = null;

        public CBaseController(
            IOptions<ENV> env,
            IApplicationService app,
            IHttpService http,
            IOraPg orapg,
            IConverterService cs,
            IGeneralRepository generalRepo,
            IBaseService baseService
        ) {
            _env = env.Value;
            _app = app;
            _http = http;
            _orapg = orapg;
            _cs = cs;
            _generalRepo = generalRepo;
            _baseService = baseService;
        }

        protected async Task<ObjectResult> CheckExcludeJenisDc(InputJsonDc fd, List<string> excludeJenisDc) {
            string kodeDcSekarang = await _generalRepo.GetKodeDc();
            if (kodeDcSekarang.ToUpper() != "DCHO") {
                throw new Exception("Khusus HO!");
            }

            string targetJenisDc = await _orapg.ExecScalarAsync<string>($@"
                SELECT
                    tbl_jenis_dc
                FROM
                    dc_tabel_dc_t
                WHERE
                    tbl_dc_kode = :kode_dc
            ", new List<CDbQueryParamBind>() {
                new CDbQueryParamBind { NAME = "kode_dc", VALUE = fd.kode_dc.ToUpper() }
            });

            if (string.IsNullOrEmpty(targetJenisDc)) {
                return BadRequest(new ResponseJsonSingle<dynamic> {
                    info = $"🙄 404 - {GetType().Name} 😪",
                    result = new {
                        message = $"Kode DC {fd.kode_dc.ToUpper()} tidak tersedia"
                    }
                });
            }

            if (excludeJenisDc == null || excludeJenisDc?.Count <= 0) {
                excludeJenisDc = _env.EXCLUDE_JENIS_DC?.Split(",").Select(d => d.ToUpper().Trim()).ToList();
            }
            if (excludeJenisDc != null && excludeJenisDc.Count > 0 && excludeJenisDc.Contains(targetJenisDc.ToUpper())) {
                return BadRequest(new ResponseJsonSingle<dynamic> {
                    info = $"🙄 403 - {GetType().Name} 😪",
                    result = new {
                        message = $"Tidak dapat mengambil data pada DC \"{string.Join(", ", excludeJenisDc.ToArray())}\" karena masuk ke dalam daftar pengecualian"
                    }
                });
            }

            return null;
        }

        public virtual async Task<dynamic> HitDcAPI(InputJsonDc fd, string page, string row, string sort, string order) {
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
                    (decimal pages, decimal count, DataTable dt) = await _baseService.GetDataPaging(_env.IS_USING_POSTGRES, _orapg, fd, sort, order, page, row);

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

        public virtual async Task<IActionResult> DirectDbConnection(InputJsonDc fd, string page, string row, string sort, string order) {
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

                (decimal pages, decimal count, DataTable dt) = await _baseService.GetDataPaging(dbIsUsingPostgre, dbOraPg, fd, sort, order, page, row);

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
