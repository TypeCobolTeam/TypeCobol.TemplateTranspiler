using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;

namespace TypeCobol.TemplateTask
{
    public class TemplateClassGenerationTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ToolPath { get; set; }

        /// <summary>
        /// Gets or sets the directory to place the generated files.
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        public string GeneratedSourceExtension { get; set; }

        /// <summary>
        /// Gets or sets the path to the assembly for this build task.
        /// </summary>
        public string BuildTaskPath { get; set; }

        /// <summary>
        /// Gets or sets a list of source code files in the project.
        /// </summary>
        public ITaskItem[] SourceCodeFiles { get; set; }

        public TemplateClassGenerationTask()
        {
            this.GeneratedSourceExtension = "cs";
        }

        public override bool Execute()
        {
            if (!Path.IsPathRooted(this.ToolPath))
                this.ToolPath = Path.Combine(Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode), this.ToolPath);
            if (!Path.IsPathRooted(this.BuildTaskPath))
                this.BuildTaskPath = Path.Combine(Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode), this.BuildTaskPath);

            IList<string> filePaths = (IList<string>)null;
            if (this.SourceCodeFiles != null)
            {
                filePaths = (IList<string>)new List<string>(this.SourceCodeFiles.Length);
                foreach (ITaskItem sourceCodeFile in this.SourceCodeFiles)
                    filePaths.Add(sourceCodeFile.ItemSpec);
            }

            bool bResult = true;
            foreach (string path in filePaths)
            {
                if (!GenerateOutput(path))
                    bResult = false;
            }
            return false;
        }

        /// <summary>
        /// Generates the corresponding output file.
        /// </summary>
        /// <param name="path"></param>
        private bool GenerateOutput(string path)
        {
            string exePath = Path.Combine(Path.GetDirectoryName(this.ToolPath), "TypeCobol.TemplateTranspiler.exe");
            List<string> arguments = new List<string>();
            arguments.Add("-i");
            arguments.Add(path);

            ProcessStartInfo processStartInfo = new ProcessStartInfo(exePath, JoinArguments((IEnumerable<string>)arguments))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            //this.BuildMessages.Add(new BuildMessage(TraceLevel.Info, "Executing command: \"" + processStartInfo.FileName + "\" " + processStartInfo.Arguments, "", 0, 0));
            Process process = new Process();
            process.StartInfo = processStartInfo;
            //process.ErrorDataReceived += new DataReceivedEventHandler(this.HandleErrorDataReceived);
            //process.OutputDataReceived += new DataReceivedEventHandler(this.HandleOutputDataReceived);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.Dispose();
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private static string JoinArguments(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string str1 in arguments)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(' ');
                if (str1.IndexOfAny(new char[2] { '"', ' ' }) < 0)
                {
                    stringBuilder.Append(str1);
                }
                else
                {
                    string str2 = str1.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"");
                    stringBuilder.Append('"').Append(str2).Append('"');
                }
            }
            return stringBuilder.ToString();
        }

    }
}
