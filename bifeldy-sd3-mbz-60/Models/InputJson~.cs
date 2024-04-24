namespace bifeldy_sd3_mbz_60.Models {
    
    public class InputJsonDc {
        public string kode_dc { get; set; }
    }

    public class InputJsonDcTgl : InputJsonDc {
        public DateTime? tgl_awal { get; set; }
        public DateTime? tgl_akhir { get; set; }
    }

}
