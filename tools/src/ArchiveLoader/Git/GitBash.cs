using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Integration.Git
{
    public class GitBash : IDisposable
    {
        Process pr;
        StringBuilder resultbuilder;

        public GitBash(string path, string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = path;//"C:\\Program Files\\Git\\bin\\sh.exe"
            psi.WorkingDirectory = Environment.CurrentDirectory;
            psi.Arguments = "-x -c \"" + command + "\"";
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            this.pr = new Process();
            this.pr.StartInfo = psi;
            this.pr.OutputDataReceived += pr_DataReceived;
            this.pr.ErrorDataReceived += pr_DataReceived;            
            this.resultbuilder = new StringBuilder(500);
        }

        public void Start()
        {
            this.pr.Start();
            this.pr.BeginOutputReadLine();
            this.pr.BeginErrorReadLine();
        }

        public void WaitForExit()
        {
            this.pr.WaitForExit();            
        }

        public string Output { get { return this.resultbuilder.ToString(); } }

        public string Run()
        {
            Start();
            WaitForExit();
            return Output;
        }

        void pr_DataReceived(object sender, DataReceivedEventArgs e)
        {
            resultbuilder.AppendLine(e.Data);
        }

        public void Dispose()
        {
            this.pr.Dispose();            
        }

        public static string GetPath()
        {
            return "C:\\Program Files\\Git\\bin\\sh.exe";
        }

        public static string ExecuteCommand(string command)
        {
            GitBash gb = new GitBash(GitBash.GetPath(), command);
            return gb.Run();
        }
    }
}
