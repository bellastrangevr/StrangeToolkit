using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StrangeToolkit
{
    internal sealed class VideoUrlProcess : IDisposable
    {
        private readonly Process _process;
        private readonly Task<string> _output;
        private readonly Task<string> _errors;
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        private readonly double _timeoutSeconds;
        private bool _disposed;
        private bool _started;

        public VideoUrlProcess(ProcessStartInfo start, double timeoutSeconds = 30)
        {
            _process = new Process { StartInfo = start };
            _timeoutSeconds = timeoutSeconds;
            try
            {
                _started = _process.Start();
                if (!_started) throw new InvalidOperationException("Could not start the URL resolver process.");
                _output = _process.StandardOutput.ReadToEndAsync();
                _errors = _process.StandardError.ReadToEndAsync();
            }
            catch (Exception startError)
            {
                try { Dispose(); }
                catch (Exception cleanupError) { throw new AggregateException(startError, cleanupError); }
                throw;
            }
        }

        public bool TimedOut => _elapsed.Elapsed.TotalSeconds >= _timeoutSeconds;
        public bool Completed => !_disposed && _process.HasExited && _output.IsCompleted && _errors.IsCompleted;
        public int ExitCode => _process.ExitCode;
        public string Output => _output.GetAwaiter().GetResult();
        public string Errors => _errors.GetAwaiter().GetResult();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { if (_started && !_process.HasExited) { _process.Kill(); _process.WaitForExit(1000); } }
            finally
            {
                _process.Dispose();
                _output?.ContinueWith(task => Console.Error.WriteLine("[Strange Video] Output cleanup: " + task.Exception), TaskContinuationOptions.OnlyOnFaulted);
                _errors?.ContinueWith(task => Console.Error.WriteLine("[Strange Video] Error stream cleanup: " + task.Exception), TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }
}
