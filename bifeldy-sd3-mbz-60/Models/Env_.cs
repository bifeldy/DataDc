using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_mbz_60.Models {

    public sealed class ENV : EnvVar {

        private string excludeJenisDc = string.Empty;
        public string EXCLUDE_JENIS_DC {
            get {
                string excludeJenisDcEnv = GetEnvVar("EXCLUDE_JENIS_DC");
                if (!string.IsNullOrEmpty(excludeJenisDcEnv)) {
                    excludeJenisDc = excludeJenisDcEnv;
                }
                return excludeJenisDc;
            }
            set {
                excludeJenisDc = value;
            }
        }

    }

}
