using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;

namespace Tourelle
{
    public sealed class StartupTask : IBackgroundTask
    {
        //For pan
        private const int X_PIN = 18;
        //For tilt
        private const int Y_PIN = 23;

        //Laser connected pin
        private List<int> LASER_PIN = new List<int>() { 24, 25, 21, 7, 16 };

        //Token to keep alive the background task
        BackgroundTaskDeferral GlobalDeferal;
        //PWM pin for pan
        PwmPin servoGpioPinX;
        //PWM pin for tilt
        PwmPin servoGpioPinY;

        //GPIO Pin for lasers
        GpioPin[] GPIOPIN_LASER;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //Create the "keep alive" token
            GlobalDeferal = taskInstance.GetDeferral();
            //Start the task
            Task.Factory.StartNew(() => { Initialize(); });
        }

        private async void Initialize()
        {
            //Set everithing ready for PWM mode
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            var gpio = GpioController.GetDefault();
            GPIOPIN_LASER = new GpioPin[5];

            //Init lasers states
            for (int i = 0; i < LASER_PIN.Count; i++)
            {
                GPIOPIN_LASER[i] = gpio.OpenPin(LASER_PIN[i]);
                GPIOPIN_LASER[i].Write(GpioPinValue.Low);
                GPIOPIN_LASER[i].SetDriveMode(GpioPinDriveMode.Output);
            }

            var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
            if (pwmControllers != null)
            {
                var pwmController = pwmControllers[1];
                pwmController.SetDesiredFrequency(50);
                
                //Open the two pin to start working
                servoGpioPinX = pwmController.OpenPin(X_PIN);
                servoGpioPinY = pwmController.OpenPin(Y_PIN);
                MakeRandom();
            }
            else
            {
                GlobalDeferal.Complete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">0..4 as values</param>
        private void SwitchOnLaser(int index)
        {
            GPIOPIN_LASER[index].Write(GpioPinValue.High);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">0..4 as values</param>
        private void SwitchOffLaser(int index)
        {
            GPIOPIN_LASER[index].Write(GpioPinValue.Low);
        }

        private void SwitchOnAllLaser()
        {
            for (int i = 0; i < GPIOPIN_LASER.Count(); i++)
            {
                SwitchOnLaser(i);
            }
        }

        private void SwitchOffAllLaser()
        {
            for (int i = 0; i < GPIOPIN_LASER.Count(); i++)
            {
                SwitchOffLaser(i);
            }
        }

        private void MakeRandom()
        {

            SwitchOnAllLaser();

            //Startup
            //Rotate left...
            RotateX(-90);
            Task.Delay(1000).Wait();
            StopRotation(servoGpioPinX);
            //...then right...
            RotateX(90);
            Task.Delay(1000).Wait();
            StopRotation(servoGpioPinX);
            //...then center...
            RotateX(0);
            Task.Delay(1000).Wait();
            StopRotation(servoGpioPinX);

            //...then bottom...
            RotateY(0);
            Task.Delay(1000).Wait();
            StopRotation(servoGpioPinY);
            //...then top...
            RotateY(90);
            Task.Delay(1000).Wait();
            StopRotation(servoGpioPinY);

            Random r = new Random(DateTime.Now.Millisecond);
            while (true)
            {

                //Set random rotation for pan and tilt
                //Ensure double rotation during same delay
                RotateX(r.NextDouble() * 180.0 - 90.0);
                RotateY(r.NextDouble() * 90.0);
                //Wait rotation finished
                Task.Delay(1000).Wait();
                //Stop rotation
                StopRotation(servoGpioPinX);
                StopRotation(servoGpioPinY);
                //Wait 5 sec between next rotation

                //Blink light for 5 secs.
                for (int i = 0; i < 50; i++)
                {
                    Task.Delay(50).Wait();
                    SwitchOffAllLaser();
                    Task.Delay(50).Wait();
                    SwitchOnAllLaser();
                }

            }
        }
        /// <summary>
        /// Start pan
        /// </summary>
        /// <param name="angle">Between 90 & -90 otherwise nothing is done</param>
        private void RotateX(double angle)
        {
            if (angle > 90 || angle < -90)
            {
                return;
            }

            //Go from -90;90 to 0.01;0.12
            Rotate(servoGpioPinX, ((angle + 90.0) * 0.11 / 180.0) + 0.01);
        }

        /// <summary>
        /// Start tilt.
        /// 
        /// </summary>
        /// <param name="angle">Between 0 & 90 otherwise nothing is done</param>
        private void RotateY(double angle)
        {
            if (angle < 0 || angle > 90)
            {
                return;
            }
            //Go from 0;90 to 0.08;0.13
            Rotate(servoGpioPinY, (angle * 0.05/ 90.0) + 0.08);
        }

        /// <summary>
        /// Make rotation
        /// </summary>
        /// <param name="pinToRotate"></param>
        /// <param name="value"></param>
        private void Rotate(PwmPin pinToRotate, double value)
        {
            pinToRotate.SetActiveDutyCyclePercentage(value);
            pinToRotate.Start();
        }

        /// <summary>
        /// Stop rotation
        /// </summary>
        /// <param name="pinToRotate"></param>
        private void StopRotation(PwmPin pinToRotate)
        {
            pinToRotate.Stop();
        }
    }
}
