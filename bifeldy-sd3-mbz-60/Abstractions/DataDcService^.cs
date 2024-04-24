using System.Data;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_mbz_60.Models;

namespace bifeldy_sd3_mbz_60.Abstractions {

    public interface IDataDcService {
        Task<(decimal, decimal, DataTable)> GetDataPaging(IDatabase db, InputJsonDc fd, string sort, string order, string page, string row);
        Task<(decimal, decimal, DataTable)> GetDataFull(IDatabase db, InputJsonDc fd, string sort, string order);
    }

    public abstract class CDataDcService : IDataDcService {

        //
        // { "key_json", "column_name" }
        // SELECT column_name AS key_json
        //
        // Contoh ::
        // { "tgl_sekarang_tanpa_jam", "TRUNC(COALESCE(b.updrec_date, CURRENT_TIMESTAMP))" }
        // SELECT TRUNC(COALESCE(b.tbl_updrec_date, CURRENT_TIMESTAMP)) AS tgl_sekarang_tanpa_jam
        //
        protected IDictionary<string, string> jsonKeysTableColumns = null;
        //
        // Contoh Hasil Json Akhir 1 & 2
        //
        // {
        //     tgl_sekarang_tanpa_jam: ...
        // }

        // Universal Query Support Oracle & Postgre Database
        // Jangan Pakai Query Spesifik Kusus Database :: Ex. SYSDATE, NOW()
        //
        // Akan Di Gabung Dengan Key Tabel Atas
        //
        // Contoh 1 ::
        // sqlQuery = "FROM dc_tabel_dc_t b";
        // SELECT TRUNC(COALESCE(b.tbl_updrec_date, CURRENT_TIMESTAMP)) AS tgl_sekarang_tanpa_jam
        // FROM dc_tabel_dc_t b
        //
        protected string sqlQuery = null;

        protected string GetAllColumnSelectAsString() {
            return string.Join(", ", jsonKeysTableColumns.Select(p => $"{p.Value} AS {p.Key}"));
        }

        protected List<CDbQueryParamBind> GetPageRowParamList(decimal qp = 0, decimal qr = 0) {
            return new List<CDbQueryParamBind>() {
                new CDbQueryParamBind { NAME = "page_num", VALUE = qp },
                new CDbQueryParamBind { NAME = "row_num", VALUE = qr }
            };
        }

        protected virtual async Task<(decimal, decimal, DataTable)> GetDataPagingWithParam(IDatabase db, InputJsonDc fd, string sort, string order, string page, string row, List<CDbQueryParamBind> sqlParam = null) {
            decimal queryPage = string.IsNullOrEmpty(page) ? 1 : ulong.Parse(page);
            decimal queryRow = string.IsNullOrEmpty(row) ? 10 : ulong.Parse(row);

            decimal qp = queryPage > 0 ? queryPage * queryRow - queryRow : 0;
            decimal qr = (queryRow > 0 && queryRow <= 100) ? queryPage * queryRow : 10;
            string qs = jsonKeysTableColumns[sort.ToLower()];
            string qo = order?.ToLower() == "desc" ? "DESC" : "ASC";

            var defaultSqlParam = GetPageRowParamList(qp, qr);
            if (sqlParam == null) {
                sqlParam = defaultSqlParam;
            }
            else {
                foreach (var dsp in defaultSqlParam) {
                    if (sqlParam.FindIndex(d => d.NAME.ToLower() == dsp.NAME.ToLower()) < 0) {
                        sqlParam.Add(dsp);
                    }
                }
            }

            decimal count = await db.ExecScalarAsync<decimal>($"SELECT COUNT(*) {sqlQuery}", sqlParam);
            decimal pages = Math.Ceiling(count / ((queryRow > 0 && queryRow <= 100) ? queryRow : 10));

            DataTable dt = await db.GetDataTableAsync($@"
                SELECT * FROM (
                    SELECT
                        ROW_NUMBER() OVER (ORDER BY {qs} {qo}) AS rnum,
                        {GetAllColumnSelectAsString()}
                    {sqlQuery}
                ) xx
                WHERE
                    xx.rnum > :page_num /* OFFSET */
                    AND xx.rnum <= :row_num /* LIMIT */
            ", sqlParam);

            return (pages, count, dt);
        }

        protected virtual async Task<(decimal, decimal, DataTable)> GetDataFullWithParam(IDatabase db, InputJsonDc fd, string sort, string order, List<CDbQueryParamBind> sqlParam = null) {
            DataTable dt = await db.GetDataTableAsync($@"
                SELECT
                    {GetAllColumnSelectAsString()}
                {sqlQuery}
            ", sqlParam);

            return (1, dt.Rows.Count, dt);
        }

        public abstract Task<(decimal, decimal, DataTable)> GetDataPaging(IDatabase db, InputJsonDc fd, string sort, string order, string page, string row);
        public abstract Task<(decimal, decimal, DataTable)> GetDataFull(IDatabase db, InputJsonDc fd, string sort, string order);

    }

}
