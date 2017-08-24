using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices;
using Windows.Devices.Pwm;

namespace Tourelle
{
    public sealed class StartupTask : IBackgroundTask
    {
        //For pan
        private const int X_PIN = 18;
        //For tilt
        private const int Y_PIN = 23;
        //Token to keep alive the background task
        BackgroundTaskDeferral GlobalDeferal;
        //PWM pin for pan
        PwmPin servoGpioPinX;
        //PWM pin for tilt
        PwmPin servoGpioPinY;

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

        private void MakeRandom()
        {
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
                RotateX(r.NextDouble() * 180.0 - 90.0);
                RotateY(r.NextDouble() * 90.0);
                //Wait rotation finished
                Task.Delay(1000).Wait();
                //Stop rotation
                StopRotation(servoGpioPinX);
                StopRotation(servoGpioPinY);
                //Wait 5 sec between next rotation
                Task.Delay(5000).Wait();
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
