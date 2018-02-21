using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SymbolSource.Processor.Legacy
{
    public class ConsoleRunner : IDisposable
    {
        private readonly string workingPath;
        private readonly string program;
        private bool disposed;

        public ConsoleRunner(string workingPath, string program, params IProgramComponent[] dependencies)
            : this(workingPath, program, (IEnumerable<IProgramComponent>)dependencies)
        {
        }

        public ConsoleRunner(string workingPath, string program, IEnumerable<IProgramComponent> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (workingPath == null)
                {
                    workingPath = Path.GetTempFileName();
                    File.Delete(workingPath);
                    Directory.CreateDirectory(workingPath);
                }

                var dependencyPath = Path.Combine(workingPath, dependency.Name);

                using (var inputStream = dependency.GetStream())
                using (var outputStream = File.OpenWrite(dependencyPath))
                    inputStream.CopyTo(outputStream);
            }

            this.workingPath = workingPath;
            this.program = program;
        }

        public string WorkingPath
        {
            get { return workingPath; }
        }

        public void Dispose()
        {
            if (!disposed && workingPath != null)
            {
                Directory.Delete(workingPath, true);
                disposed = true;
            }
        }

        public int Run(Func<string, string> outputHandler, Func<string, string> errorHandler, IEnumerable<string> arguments, bool quote, IDictionary<string, string> variables, Encoding encoding)
        {
            if (disposed)
                throw new ObjectDisposedException("Program to run was already deleted");

            using (var process = new Process())
            {
                process.StartInfo.FileName = program;

                if (workingPath != null)
                    process.StartInfo.FileName = Path.Combine(workingPath, process.StartInfo.FileName);

                process.StartInfo.Arguments = (quote ? "\"" : "") + string.Join(quote ? "\" \"" : " ", arguments.ToArray()) + (quote ? "\"" : "");
                process.StartInfo.WorkingDirectory = workingPath;
                process.StartInfo.UseShellExecute = false;

                if (variables != null)
                    foreach (var variable in variables)
                        process.StartInfo.EnvironmentVariables[variable.Key] = variable.Value;

                Exception exception = null;

                process.StartInfo.RedirectStandardInput = process.StartInfo.RedirectStandardOutput = process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardErrorEncoding = process.StartInfo.StandardOutputEncoding = encoding;

                process.OutputDataReceived += CreateHandler(process, outputHandler, e => exception = e, encoding);
                process.ErrorDataReceived += CreateHandler(process, errorHandler, e => exception = e, encoding);

                Debug.WriteLine("Running: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (exception != null)
                    throw new TargetInvocationException(exception);

                return process.ExitCode;
            }
        }

        private static DataReceivedEventHandler CreateHandler(Process process, Func<string, string> lineHandler, Action<Exception> exceptionHandler, Encoding encoding)
        {
            var thrown = false;

            return (sender, e) =>
            {
                if (thrown || e.Data == null)
                    return;

                try
                {
                    var response = lineHandler(e.Data);

                    if (response != null)
                    {
                        var input = new StreamWriter(process.StandardInput.BaseStream, encoding);
                        input.WriteLine(response);
                        input.Flush();
                    }
                }
                catch (Exception exception)
                {
                    if (!process.HasExited)
                        process.Kill();

                    thrown = true;
                    exceptionHandler(exception);
                }
            };
        }
    }
}
