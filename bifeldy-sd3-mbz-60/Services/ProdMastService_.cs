using System.Data;

using bifeldy_sd3_lib_60.Abstractions;

using bifeldy_sd3_mbz_60.Abstractions;
using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.Services {

    public interface IProdMastService : IBaseService { }

    public sealed class CProdMastService : CBaseService, IProdMastService {

        public CProdMastService() {
            // Singleton Class
            // Yang Berubah Hanya Param
            // Jadi Ya Di Buat Per-Function Saja
            // Kalau Query Kan Pasti Sama Bisa Di Share

            jsonKeysTableColumns = new Dictionary<string, string>() {
                { "kode_dc", "b.tbl_dc_kode" },
                { "prdcd", "a.mbr_plukode" },
                { "plumd", "a.mbr_pluid" },
                { "minor", "b.mbr_minor_toko" },
                { "cat_cod", "SUBSTR(a.mbr_fk_div, 2, 1) || a.mbr_fk_dep || a.mbr_fk_kat" },
                { "merk", "a.mbr_merk" },
                { "nama", "a.mbr_nama" },
                { "flavour", "a.mbr_flavour" },
                { "kemasan", "a.mbr_kemasan" },
                { "\"size\"", "a.mbr_size" },
                { "singkatan", "a.mbr_singkatan" },
                { "unit", "c.mbr_sat_beli" },
                { "tag", "b.mbr_tag" },
                { "length", "COALESCE(d.mbr_panjang, 1)" },
                { "width", "COALESCE(d.mbr_lebar, 1)" },
                { "height", "COALESCE(d.mbr_tinggi, 1)" },
                { "tgl_tambah", "b.mbr_tgl_tambah" },
                { "hpp_dc", "TRUNC(COALESCE(b.mbr_acost, b.mbr_lcost), 6)" }
            };

            sqlQuery = $@"
                FROM
                    DC_BARANG_T a
                    LEFT OUTER JOIN (
                        SELECT
                            mbr_fk_pluid,
                            mbr_panjang,
                            mbr_lebar,
                            mbr_tinggi
                        FROM (
                            SELECT
                                *
                            FROM
                                DC_BRG_DIMENSI_T
                            WHERE (mbr_fk_pluid, mbr_flag_pcs, mbr_updrec_date) IN (
                                SELECT
                                    mbr_fk_pluid,
                                    mbr_flag_pcs,
                                    MAX(mbr_updrec_date)
                                FROM
                                    DC_BRG_DIMENSI_T
                                GROUP BY
                                    mbr_fk_pluid,
                                    mbr_flag_pcs
                            )
                        ) s
                        WHERE
                            mbr_flag_pcs = 'Y'
                    ) d ON a.mbr_pluid = d.mbr_fk_pluid,
                    DC_BARANG_DC_T b
                    LEFT OUTER JOIN (
                        SELECT
                            tbl_dc_kode,
                            mbr_fk_pluid,
                            mbr_sat_beli
                        FROM
                            DC_BRG_HRGBELI_DC_T
                        WHERE
                            mbr_supp_dc = 'Y'
                    ) C ON (
                        b.mbr_fk_pluid = C.mbr_fk_pluid
                        AND b.tbl_dc_kode = C.tbl_dc_kode
                    )
                WHERE
                    a.mbr_tgl_plumati IS NULL
                    AND a.mbr_pluid = b.mbr_fk_pluid
                    AND (
                        b.mbr_bkl = 'N'
                        OR mbr_bkl IS NULL
                    )
            ";
        }

        public override async Task<(decimal, decimal, DataTable)> GetDataPaging(bool isPg, IDatabase db, InputJsonDc fd, string sort, string order, string page, string row) {
            return await GetDataPagingWithParam(isPg, db, fd, sort, order, page, row);
        }

        public override async Task<(decimal, decimal, DataTable)> GetDataFull(bool isPg, IDatabase db, InputJsonDc fd, string sort, string order) {
            return await GetDataFullWithParam(isPg, db, fd, sort, order);
        }

    }

}
