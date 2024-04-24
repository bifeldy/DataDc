namespace bifeldy_sd3_mbz_60.Components.Spinner {

    public interface ISpinnerService {
        event Action OnShow;
        event Action OnHide;
        void Show();
        void Hide();
    }

    public sealed class CSpinnerService : ISpinnerService {

        public event Action OnShow;
        public event Action OnHide;

        public CSpinnerService() {
            //
        }

        public void Show() {
            OnShow?.Invoke();
        }

        public void Hide() {
            OnHide?.Invoke();
        }

    }

}
