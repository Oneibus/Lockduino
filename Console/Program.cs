using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.Threading;
using System.Runtime.InteropServices;

using LockDuinoAPI;

namespace LockDuinoConsole
{
    class Program
    {
        // useful stack overflow article for getting notifications from windows about locked/unlocked desktop sessions
        // http://stackoverflow.com/questions/603484/checking-for-workstation-lock-unlock-change-with-c-sharp

        // inside a driver
        // http://msdn.microsoft.com/en-us/library/windows/hardware/ff549501(v=vs.85).aspx

        // in a windows service
        // http://stackoverflow.com/questions/16282231/get-notified-from-logon-and-logoff

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        [STAThread]
        static void Main(string[] args)
        {
            GetLockDuinoSettings();
        }

        private static LockDuinoSettings GetLockDuinoSettings()
        {
            var lockDuinoSettings = new LockDuinoSettings();

            using (var lockDuino = LockDuino.GetLockDuino())
            {
                Console.WriteLine("Now calibrating the LockDuino.  Please sit (or stand) at your workstation as you normally would.");
                Console.WriteLine("Hit [Enter] to continue, then please remain at your workstation until the next prompt appears.");
                Console.ReadLine();

                const int sampleSize = 12;
                const int restTimeMilliseconds = 1000;
                const int waitTimeMilliseconds = 15000;

                int[] sampleBuffer; 

                var decision = 'T';
                var averageDistanceAtRest = 0.0;

                while (decision != 'K' && decision != 'k')
                {
                    sampleBuffer = new int[sampleSize];;

                    for (var i = 0; i < sampleSize; i++)
                    {
                        Console.Clear();
                        Console.Write("\rWorking{0}", new String('.', i % 4));

                        sampleBuffer[i] = lockDuino.ReadRange();

                        Thread.Sleep(restTimeMilliseconds);
                    }

                    averageDistanceAtRest = sampleBuffer.CulledOutliersAverage();

                    Console.WriteLine();
                    Console.WriteLine("Average distance at rest is {0} cm.", averageDistanceAtRest);
                    Console.WriteLine("[K]eep this setting, or [T]ry again?");

                    decision = (char)Console.Read();
                    Console.ReadLine();
                }

                lockDuinoSettings.AverageDistanceWhenPresent = averageDistanceAtRest;

                decision = 'T';
                var averageDistanceWhenAbsent = 0.0;

                while (decision != 'K' && decision != 'k')
                {
                    Console.WriteLine("LockDuino now needs to get an average reading of the distance when you are absent from your workstation.");
                    Console.WriteLine("After hitting [Enter], step away from your desktop.  After 15 seconds, LockDuino will measure the distance.");
                    Console.WriteLine("Wait at least 30 seconds, then return to your desk.");
                    Console.WriteLine("Hit [Enter] to begin calibrating the 'absent' distance.");
                    
                    Console.ReadLine();

                    Console.Clear();
                    Console.WriteLine("Getting ready to start 'absent' calibration.  Please step away from your workstation.");

                    Thread.Sleep(waitTimeMilliseconds);

                    sampleBuffer = new int[sampleSize];

                    for (var i = 0; i < sampleSize; i++)
                    {
                        Console.Clear();
                        Console.Write("\rWorking{0}", new String('.', i % 4));

                        sampleBuffer[i] = lockDuino.ReadRange();

                        Thread.Sleep(restTimeMilliseconds);
                    }

                    averageDistanceWhenAbsent = sampleBuffer.CulledOutliersAverage();

                    Console.Beep(1000, 500);
                    
                    Console.WriteLine();
                    Console.WriteLine("Average distance when absent is {0} cm.", averageDistanceWhenAbsent);
                    Console.WriteLine("[K]eep this setting, or [T]ry again?");

                    decision = (char)Console.Read();
                    Console.ReadLine();
                }

                lockDuinoSettings.AverageDistanceWhenAbsent = averageDistanceWhenAbsent;

                return lockDuinoSettings;
            }
        }

        private static void TestLockDuino()
        {
            using (var lockDuino = LockDuino.GetLockDuino())
            {
                lockDuino.Threshold = 125;
                lockDuino.GuardWorkstation(new AutoResetEvent(false));

                while(true) { }
            }
        }
    }
}
