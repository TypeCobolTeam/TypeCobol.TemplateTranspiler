using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.Build.Utilities;

namespace TypeCobol.TemplateTask
{
    public class TemplateClassGenerationTask : Microsoft.Build.Utilities.Task    
    {
        private List<ITaskItem> _generatedCodeFiles = new List<ITaskItem>();
        [Required]
        public string ToolPath { get; set; }


        /// <summary>
        /// Gets or sets the directory to place the generated files.
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        public string Encoding
        {
            get;
            set;
        }

        public string TargetLanguage
        {
            get;
            set;
        }

        public string TargetFrameworkVersion
        {
            get;
            set;
        }

        public string GeneratedSourceExtension { get; set; }

        public string[] LanguageSourceExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the assembly for this build task.
        /// </summary>
        public string BuildTaskPath { get; set; }

        public string TargetNamespace
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a list of source code files in the project.
        /// </summary>
        public ITaskItem[] SourceCodeFiles { get; set; }

        [Output]
        public ITaskItem[] GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles.ToArray();
            }
            set
            {
                this._generatedCodeFiles = new List<ITaskItem>(value);
            }
        }


        public TemplateClassGenerationTask()
        {
            this.GeneratedSourceExtension = "cs";
        }

        /// <summary>
        /// Signature for a message Box time out
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="wLanguageId"></param>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint MessageBoxTimeoutW(IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPWStr)]  String text,
            [MarshalAs(UnmanagedType.LPWStr)] String title,
            [MarshalAs(UnmanagedType.U4)] uint type,
            Int16 wLanguageId,
            Int32 milliseconds);
    
        public override bool Execute()
        {
            //MessageBoxTimeoutW(IntPtr.Zero, "INSIDE TemplateCustomKask", "THIS IS A TEST", 0, 0, 60000);

            if (!Path.IsPathRooted(ToolPath))
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
            return bResult;
        }

        /// <summary>
        /// Based on the input path and the TargetNamespace value, figure out the output file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetOutputFilePath(string path)
        {
            if (!string.IsNullOrWhiteSpace(this.TargetNamespace))
            {
                FileInfo fiPath = new FileInfo(path);
                FileInfo targetPath = new FileInfo(Path.Combine(fiPath.DirectoryName, this.TargetNamespace));
                if (Directory.Exists(targetPath.FullName))
                {//This is a Directoty ==> combine with the input file name
                    string name = null;
                    int dotIndex = fiPath.Name.LastIndexOf('.');
                    if (dotIndex >= 0)
                    {
                        name = fiPath.Name.Substring(0, dotIndex);
                    }
                    else
                    {
                        name = fiPath.Name;
                    }
                    targetPath = new FileInfo(Path.Combine(targetPath.FullName, name + '.' + GeneratedSourceExtension));
                }

                return targetPath.FullName;
            }
            else
            {
                int dotIndex = path.LastIndexOf('.');
                if (dotIndex >= 0)
                {
                    return path.Substring(0, dotIndex) + '.' + GeneratedSourceExtension;
                }
                else
                {
                    return path + '.' + GeneratedSourceExtension;
                }
            }
        }
        /// <summary>
        /// Generates the corresponding output file.
        /// </summary>
        /// <param name="path"></param>
        private bool GenerateOutput(string path)
        {
            DirectoryInfo diToolPath = new DirectoryInfo(this.ToolPath);
            string dirName = diToolPath.FullName;
            string exePath = Path.Combine(dirName, "TypeCobol.TemplateTranspiler.exe");
            List<string> arguments = new List<string>();
            arguments.Add("-i");
            arguments.Add(path);

            string genFile = GetOutputFilePath(path);
            arguments.Add("-o");
            arguments.Add(genFile);

            ProcessStartInfo processStartInfo = new ProcessStartInfo(exePath, JoinArguments((IEnumerable<string>)arguments))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            this.Log.LogCommandLine(processStartInfo.FileName + processStartInfo.Arguments);
            this.Log.LogMessage(MessageImportance.Normal, "Executing command: \"" + processStartInfo.FileName + "\" " + processStartInfo.Arguments);            
            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.ErrorDataReceived += new DataReceivedEventHandler(this.HandleErrorDataReceived);
            process.OutputDataReceived += new DataReceivedEventHandler(this.HandleOutputDataReceived);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.Dispose();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                this._generatedCodeFiles.Add(new TaskItem(genFile));
            }

            return process.ExitCode == 0;
        }

        private void HandleOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.Log.LogMessage(MessageImportance.Normal, e.Data);
            }
        }

        private void HandleErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.Log.LogMessage(MessageImportance.Normal, e.Data);
            }
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
