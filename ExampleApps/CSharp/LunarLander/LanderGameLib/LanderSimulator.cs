using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanderGameLib
{
    public class LanderSimulator
    {
        public const double Gravity = 1.62;
        public const double Thrust = 10;
        public const double TerminalVelocity = 40;

        public LanderSimulator()
        {
            Fuel = 200;
            Seconds = 0;
            Altitude = 10000;
            Velocity = 0;
        }

        public int Fuel { get; set; }
        public int Seconds { get; set; }
        public double Altitude { get; set; }
        public double Velocity { get; set; }

        public int Score
        {
            get { return (int)((Fuel * 10) + Seconds + (Velocity * 1000)); }
        }

        public void Turn(bool thrust)
        {
            Seconds++;
            Velocity -= Gravity;
            Altitude += Velocity;

            if (thrust && Fuel > 0)
            {
                Fuel--;
                Velocity += Thrust;
            }

            Velocity = Math.Max(-TerminalVelocity, Velocity);
            Velocity = Math.Min(TerminalVelocity, Velocity);

            if (Altitude < 0)
                Altitude = 0;
        }

        public String Telemetry()
        {
            return string
                .Format("Elapsed: {0} s, Fuel: {1} l, Velocity: {2} m/s, {3} m",
                Seconds,
                Fuel,
                Velocity.ToString("#0.####"),
                (int)Altitude);
        }

        public bool Flying
        {
            get { return Altitude > 0; }
        }
    }
}
