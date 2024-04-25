using System.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.Abstractions {

    public abstract class CBaseController : ControllerBase {

        private readonly ENV _env;

        private readonly IOraPg _orapg;

        public CBaseController(
            IOptions<ENV> env,
            IOraPg orapg
        ) {
            _env = env.Value;
            _orapg = orapg;
        }

        protected async Task<ObjectResult> CheckExcludeJenisDc(InputJsonDc fd, List<string> excludeJenisDc) {
            string targetJenisDc = await _orapg.ExecScalarAsync<string>($@"
                SELECT
                    tbl_dc_nama
                FROM
                    dc_tabel_v
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
                        message = $"Tidak Dapat mengambil data pada DC {string.Join(", ", excludeJenisDc.ToArray())} karena masuk ke dalam daftar pengecualian"
                    }
                });
            }

            return null;
        }

    }

}
