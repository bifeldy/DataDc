using System.Data;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;

using bifeldy_sd3_mbz_60.Abstractions;
using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.Services {

    public interface IPlanogramDisplayService : IBaseService { }

    public sealed class CPlanogramDisplayService : CBaseService, IPlanogramDisplayService {

        public CPlanogramDisplayService() {
            // Singleton Class
            // Yang Berubah Hanya Param
            // Jadi Ya Di Buat Per-Function Saja
            // Kalau Query Kan Pasti Sama Bisa Di Share

            jsonKeysTableColumns = new Dictionary<string, string>() {
                { "pla_dc_kode", "pla_dc_kode" },
                { "pla_fk_tipe", "pla_fk_tipe" },
                { "pla_fk_pluid", "pla_fk_pluid" },
                { "pla_line", "pla_line" },
                { "pla_rak", "pla_rak" },
                { "pla_shelf", "pla_shelf" },
                { "pla_cell", "pla_cell" }
            };

            sqlQuery = $@"
                FROM
                    dc_planogram_display_v
                WHERE
                    pla_dc_kode = :kode_dc
            ";
        }

        public override async Task<(decimal, decimal, DataTable)> GetDataPaging(IDatabase db, InputJsonDc fd, string sort, string order, string page, string row) {
            var sqlParam = new List<CDbQueryParamBind>() {
                new CDbQueryParamBind { NAME = "kode_dc", VALUE = fd.kode_dc.ToUpper() }
            };
            return await GetDataPagingWithParam(db, fd, sort, order, page, row, sqlParam);
        }

        public override async Task<(decimal, decimal, DataTable)> GetDataFull(IDatabase db, InputJsonDc fd, string sort, string order) {
            var sqlParam = new List<CDbQueryParamBind>() {
                new CDbQueryParamBind { NAME = "kode_dc", VALUE = fd.kode_dc.ToUpper() }
            };
            return await GetDataFullWithParam(db, fd, sort, order, sqlParam);
        }

    }

}
