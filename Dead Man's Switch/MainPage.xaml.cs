using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dead_Man_s_Switch
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private Accelerometer _accelerometer;
        private Gyrometer _gyrometer;
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _expireTime = DateTime.MinValue;
        private string _timeRemaining;
        private InputInjector _injector = InputInjector.TryCreate();

        private string TimeRemaining
        {
            get => _timeRemaining;
            set { SetProperty(ref _timeRemaining, value); }
        }

        public MainPage()
        {
            this.InitializeComponent();
            _accelerometer = Accelerometer.GetDefault();
            _gyrometer = Gyrometer.GetDefault();
            if (_accelerometer != null)
            {
                _accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            }
            if (_gyrometer != null)
            {
                _gyrometer.ReadingChanged += Gyrometer_ReadingChanged;
            }
            _timer.Interval = TimeSpan.FromSeconds(.1d);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void ResetExpire()
        {
            if (_expireTime != DateTime.MinValue) _expireTime = DateTime.Now.AddMinutes(10);
        }

        private void Timer_Tick(object sender, object e)
        {
            var timeRemaining = _expireTime.Subtract(DateTime.Now);
            if (_expireTime != DateTime.MinValue && timeRemaining.TotalMilliseconds <= 0)
            {
                _injector.InjectKeyboardInput(
                    new[] {
                        new InjectedInputKeyboardInfo() {
                            VirtualKey = 0xB3 //Stop - Play/Pause is 0xB3 - https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
                        }
                    });
                _expireTime = DateTime.MinValue;
            }

            if (_expireTime == DateTime.MinValue)
            {
                TimeRemaining = "Stopped";
            }
            else
            {
                TimeRemaining = timeRemaining.ToString("m\\:ss");
            }
        }

        private void Gyrometer_ReadingChanged(Gyrometer sender, GyrometerReadingChangedEventArgs args)
        {
            if (Math.Abs(args.Reading.AngularVelocityX) > 2.5
                || Math.Abs(args.Reading.AngularVelocityY) > 2.5
                || Math.Abs(args.Reading.AngularVelocityZ) > 2.5)
            {
                Debug.WriteLine("Significant Gyro Change");
                ResetExpire();
            }
        }

        private void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            if (Math.Abs(args.Reading.AccelerationX) > 1.5
                || Math.Abs(args.Reading.AccelerationY) > 1.5
                || Math.Abs(args.Reading.AccelerationZ) > 1.5)
            {
                Debug.WriteLine("Significant Accel Change");
                ResetExpire();
            }
        }
        private void Enable_Clicked(object sender, RoutedEventArgs e)
        {
            if (_expireTime == DateTime.MinValue)
            {
                _expireTime = DateTime.MaxValue;
                ResetExpire();
            }
            else
            {
                _expireTime = DateTime.MinValue;
            }
        }

        private void Reset_Clicked(object sender, RoutedEventArgs e)
        {
            ResetExpire();
        }

        #region NPC
        /// <summary>
        /// Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Checks if a property already matches a desired value.  Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;

            storage = value;
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
