using System.ComponentModel;

namespace NetworkService.Common
{
    public abstract class ValidationBase : BindableBase
    {
        public ValidationErrors ValidationErrors { get; set; }

        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { SetProperty(ref isValid, value); }
        }

        protected ValidationBase()
        {
            ValidationErrors = new ValidationErrors();
        }

        protected abstract void ValidateSelf();

        public void Validate()
        {
            ValidationErrors.Clear();
            ValidateSelf();
            IsValid = ValidationErrors.IsValid;
            OnPropertyChanged("IsValid");
            OnPropertyChanged("ValidationErrors");
        }
    }
}