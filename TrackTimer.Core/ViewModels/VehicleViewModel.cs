namespace TrackTimer.Core.ViewModels
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using TrackTimer.Core.Resources;

    public class VehicleViewModel : BaseViewModel, INotifyDataErrorInfo, IEquatable<VehicleViewModel>
    {
        private long id;
        private Guid key;
        private string make;
        private string model;
        private KeyValuePair<VehicleClass, string> selectedClass;
        private Dictionary<string, List<string>> errorMessages;

        public VehicleViewModel()
        {
            errorMessages = new Dictionary<string, List<string>>();
            selectedClass = Constants.VehicleClasses.First();
            PropertyChanged += VehicleViewModel_PropertyChanged;
            key = Guid.Empty;
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public long Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        public Guid Key
        {
            get { return key; }
            set { SetProperty(ref key, value); }
        }
        public string Make
        {
            get { return make; }
            set { SetProperty(ref make, value); }
        }
        public string Model
        {
            get { return model; }
            set { SetProperty(ref model, value); }
        }
        public VehicleClass Class
        {
            get { return selectedClass.Key; }
            set
            {
                if (selectedClass.Key == value)
                    return;
                SelectedClass = Constants.VehicleClasses.Single(c => c.Key == value);
                NotifyPropertyChanged();
            }
        }
        public KeyValuePair<VehicleClass, string> SelectedClass
        {
            get { return selectedClass;  }
            set
            {
                SetProperty(ref selectedClass, value);
                NotifyPropertyChanged("Class");
            }
        }
        public bool Equals(VehicleViewModel other)
        {
            if (object.ReferenceEquals(other, null))
                return false;

            return this.Key == other.Key;
        }

        public override int GetHashCode()
        {
            return this.key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as VehicleViewModel);
        }

        public bool HasErrors
        {
            get { return errorMessages.Any(); }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return errorMessages.Where(er => er.Key == propertyName).SelectMany(er => er.Value);
        }

        public void ClearErrors()
        {
            errorMessages.Clear();
        }

        public void AddServerError(string errorMessage)
        {
            errorMessages.Add(string.Empty, new List<string> { errorMessage });
            ErrorsChanged(this, new DataErrorsChangedEventArgs(string.Empty));
            NotifyPropertyChanged("HasErrors");
        }

        private void VehicleViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Model":
                    if (string.IsNullOrWhiteSpace(Model))
                    {
                        if (errorMessages.ContainsKey(e.PropertyName))
                            errorMessages.Remove(e.PropertyName);
                        errorMessages.Add(e.PropertyName, new List<string> { CoreResources.Text_Validation_Required_VehicleModel });
                        ErrorsChanged(this, new DataErrorsChangedEventArgs(e.PropertyName));
                        NotifyPropertyChanged("HasErrors");
                    }
                    else
                    {
                        if (errorMessages.ContainsKey(e.PropertyName))
                            errorMessages.Remove(e.PropertyName);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}