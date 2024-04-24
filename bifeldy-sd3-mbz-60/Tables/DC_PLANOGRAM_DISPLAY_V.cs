using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_mbz_60.Models {

    [Keyless]
    public class DC_PLANOGRAM_DISPLAY_V : EntityTable {
        public string PLA_DC_KODE { get; set; }
        public string PLA_FK_TIPE { get; set; }
        public decimal? PLA_FK_PLUID { get; set; }
        public string PLA_LINE { get; set; }
        public string PLA_RAK { get; set; }
        public string PLA_SHELF { get; set; }
        public string PLA_CELL { get; set; }
    }

}
