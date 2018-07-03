﻿namespace TrackTimer.Controls
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using TrackTimer.Contracts;
    using TrackTimer.ViewModels;

    [TemplatePart(Name = ButtonPartName, Type = typeof(ButtonBase))]
    public class DegreesPicker : Control
    {
        private const string ButtonPartName = "IntButton";

        private ButtonBase _degreesButtonPart;
        private PhoneApplicationFrame _frame;
        private object _frameContentWhenOpened;
        private NavigationInTransition _savedNavigationInTransition;
        private NavigationOutTransition _savedNavigationOutTransition;
        private IDoublePickerPage pickerPage;

        /// <summary>
        /// Event that is invoked when the Value property changes.
        /// </summary>
        public event EventHandler<DoubleValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Gets or sets the int value.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Matching the use of Value as a Picker naming convention.")]
        public double? Value
        {
            get { return (double?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Identifies the Value DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double?), typeof(DegreesPicker), new PropertyMetadata(null, OnValueChanged));

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((DegreesPicker)o).OnValueChanged((double?)e.OldValue, (double?)e.NewValue);
        }

        private void OnValueChanged(double? oldValue, double? newValue)
        {
            UpdateValueString();
            OnValueChanged(new DoubleValueChangedEventArgs(oldValue, newValue));
        }

        /// <summary>
        /// Called when the value changes.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnValueChanged(DoubleValueChangedEventArgs e)
        {
            EventHandler<DoubleValueChangedEventArgs> handler = ValueChanged;
            if (null != handler)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Gets the string representation of the selected value.
        /// </summary>
        public string ValueString
        {
            get { return (string)GetValue(ValueStringProperty); }
            private set { SetValue(ValueStringProperty, value); }
        }

        /// <summary>
        /// Identifies the ValueString DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ValueStringProperty = DependencyProperty.Register(
            "ValueString", typeof(string), typeof(DegreesPicker), null);

        /// <summary>
        /// Gets or sets the format string to use when converting the Value property to a string.
        /// </summary>
        public string ValueStringFormat
        {
            get { return (string)GetValue(ValueStringFormatProperty); }
            set { SetValue(ValueStringFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the ValueStringFormat DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ValueStringFormatProperty = DependencyProperty.Register(
            "ValueStringFormat", typeof(string), typeof(DegreesPicker), new PropertyMetadata(null, OnValueStringFormatChanged));

        private static void OnValueStringFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((DegreesPicker)o).OnValueStringFormatChanged(/*(string)e.OldValue, (string)e.NewValue*/);
        }

        private void OnValueStringFormatChanged(/*string oldValue, string newValue*/)
        {
            UpdateValueString();
        }

        /// <summary>
        /// Gets or sets the header of the control.
        /// </summary>
        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the Header DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof(object), typeof(DegreesPicker), null);

        /// <summary>
        /// Gets or sets the template used to display the control's header.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the HeaderTemplate DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate", typeof(DataTemplate), typeof(DegreesPicker), null);

        /// <summary>
        /// Gets or sets the Uri to use for loading the IPickerPage instance when the control is clicked.
        /// </summary>
        public Uri PickerPageUri
        {
            get { return (Uri)GetValue(PickerPageUriProperty); }
            set { SetValue(PickerPageUriProperty, value); }
        }

        /// <summary>
        /// Identifies the PickerPageUri DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty PickerPageUriProperty = DependencyProperty.Register(
            "PickerPageUri", typeof(Uri), typeof(DegreesPicker), null);

        /// <summary>
        /// Gets the fallback value for the ValueStringFormat property.
        /// </summary>
        protected virtual string ValueStringFormatFallback { get { return "{0}"; } }

        /// <summary>
        /// Initializes a new instance of the DegreesPicker control.
        /// </summary>
        public DegreesPicker()
        {
        }

        /// <summary>
        /// Called when the control's Template is expanded.
        /// </summary>
        public override void OnApplyTemplate()
        {
            // Unhook from old template
            if (null != _degreesButtonPart)
            {
                _degreesButtonPart.Click -= new RoutedEventHandler(HandleDateButtonClick);
            }

            base.OnApplyTemplate();

            // Hook up to new template
            _degreesButtonPart = GetTemplateChild(ButtonPartName) as ButtonBase;
            if (null != _degreesButtonPart)
            {
                _degreesButtonPart.Click += new RoutedEventHandler(HandleDateButtonClick);
            }
        }

        private void HandleDateButtonClick(object sender, RoutedEventArgs e)
        {
            OpenPickerPage();
        }

        private void UpdateValueString()
        {
            ValueString = string.Format(CultureInfo.CurrentCulture, ValueStringFormat ?? ValueStringFormatFallback, Value);
        }

        private void OpenPickerPage()
        {
            if (null == PickerPageUri)
            {
                throw new ArgumentException("PickerPageUri property must not be null.");
            }

            if (null == _frame)
            {
                // Hook up to necessary events and navigate
                _frame = Application.Current.RootVisual as PhoneApplicationFrame;
                if (null != _frame)
                {
                    _frameContentWhenOpened = _frame.Content;

                    // Save and clear host page transitions for the upcoming "popup" navigation
                    UIElement frameContentWhenOpenedAsUIElement = _frameContentWhenOpened as UIElement;
                    if (null != frameContentWhenOpenedAsUIElement)
                    {
                        _savedNavigationInTransition = TransitionService.GetNavigationInTransition(frameContentWhenOpenedAsUIElement);
                        TransitionService.SetNavigationInTransition(frameContentWhenOpenedAsUIElement, null);
                        _savedNavigationOutTransition = TransitionService.GetNavigationOutTransition(frameContentWhenOpenedAsUIElement);
                        TransitionService.SetNavigationOutTransition(frameContentWhenOpenedAsUIElement, null);
                    }

                    _frame.Navigated += new NavigatedEventHandler(HandleFrameNavigated);
                    _frame.NavigationStopped += new NavigationStoppedEventHandler(HandleFrameNavigationStoppedOrFailed);
                    _frame.NavigationFailed += new NavigationFailedEventHandler(HandleFrameNavigationStoppedOrFailed);

                    _frame.Navigate(PickerPageUri);
                }
            }

        }

        private void ClosePickerPage()
        {
            // Unhook from events
            if (null != _frame)
            {
                _frame.Navigated -= new NavigatedEventHandler(HandleFrameNavigated);
                _frame.NavigationStopped -= new NavigationStoppedEventHandler(HandleFrameNavigationStoppedOrFailed);
                _frame.NavigationFailed -= new NavigationFailedEventHandler(HandleFrameNavigationStoppedOrFailed);

                // Restore host page transitions for the completed "popup" navigation
                UIElement frameContentWhenOpenedAsUIElement = _frameContentWhenOpened as UIElement;
                if (null != frameContentWhenOpenedAsUIElement)
                {
                    TransitionService.SetNavigationInTransition(frameContentWhenOpenedAsUIElement, _savedNavigationInTransition);
                    _savedNavigationInTransition = null;
                    TransitionService.SetNavigationOutTransition(frameContentWhenOpenedAsUIElement, _savedNavigationOutTransition);
                    _savedNavigationOutTransition = null;
                }

                _frame = null;
                _frameContentWhenOpened = null;
            }
            // Commit the value if available
            if (null != pickerPage)
            {
                if (pickerPage.Value.HasValue)
                {
                    Value = pickerPage.Value.Value;
                }
                pickerPage = null;
            }
        }

        private void HandleFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (e.Content == _frameContentWhenOpened)
            {
                // Navigation to original page; close the picker page
                ClosePickerPage();
            }
            else if (null == pickerPage)
            {
                // Navigation to a new page; capture it and push the value in
                pickerPage = e.Content as IDoublePickerPage;
                if (null != pickerPage)
                {
                    pickerPage.Value = Value.GetValueOrDefault(0);
                }
            }
        }

        private void HandleFrameNavigationStoppedOrFailed(object sender, EventArgs e)
        {
            // Abort
            ClosePickerPage();
        }
    }
}