using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DizzyMouse
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern uint SendInput(uint nInputs, ref MOUSE_INPUT pInputs, int cbSize);

        public static void Main(string[] args)
        {
            var frequency = TimeSpan.FromSeconds(55); // one move each 55 seconds.

            if (args != null && args.Length == 1)
            {
                if (Int32.TryParse(args[0], out int parameter)) {
                    if (parameter < TimeSpan.FromHours(1).TotalSeconds)
                    {
                        frequency = TimeSpan.FromSeconds(parameter);
                    }
                    else
                    {
                        Console.WriteLine($"The frequency of {parameter} is longer than one hour. Using {frequency.TotalSeconds} seconds instead.");
                    }
                }
                else
                {
                    Console.WriteLine($"The parameter {parameter} is invalid. Using {frequency.TotalSeconds} seconds instead.");
                }
            }

            var tokenSource = new CancellationTokenSource();
            var task = CreateNeverEndingTask(now =>
            {
                int delta = 1; // one pixel

                // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-input
                var input = new MOUSE_INPUT()
                {
                    TYPE = 0, // INPUT_MOUSE
                    // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput
                    dx = delta,
                    dy = delta,
                    mouseData = 0,
                    dwFlags = 1,
                    time = 0,
                    dwExtraInfo = (IntPtr)0
                };

                var sizeOf = Marshal.SizeOf(typeof(MOUSE_INPUT));

                while (true)
                {
                    input.dx = input.dy = delta;

                    if (SendInput(1, ref input, sizeOf) != 1)
                    {
                        Console.WriteLine("Failed to move the mouse");
                    }

                    delta *= -1;

                    Thread.Sleep(frequency);
                }

            }, tokenSource.Token);

            task.Post(DateTimeOffset.Now);

            Console.ReadKey();

            using (tokenSource)
            {
                tokenSource.Cancel();
            }

            tokenSource = null;
            task = null;
        }

        private static ITargetBlock<DateTimeOffset> CreateNeverEndingTask(Action<DateTimeOffset> action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            ActionBlock<DateTimeOffset> block = null;

            block = new ActionBlock<DateTimeOffset>(async now =>
            {

                action(now);

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).
                    ConfigureAwait(false);

                block.Post(DateTimeOffset.Now);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            return block;
        }
    }
}
