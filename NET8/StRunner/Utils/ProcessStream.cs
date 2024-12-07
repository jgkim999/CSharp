using System.Diagnostics;
using System.Text;

namespace StRunner.Utils;

public class ProcessStream
{
    /*
     * Class to get process stdout/stderr streams
     * Author: SeemabK (seemabk@yahoo.com)
     * Usage:
        //create ProcessStream
        ProcessStream myProcessStream = new ProcessStream();
        //create and populate Process as needed
        Process myProcess = new Process();
        myProcess.StartInfo.FileName = "myexec.exe";
        myProcess.StartInfo.Arguments = "-myargs";

        //redirect stdout and/or stderr
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardOutput = true;
        myProcess.StartInfo.RedirectStandardError = true;

        //start Process
        myProcess.Start();
        //connect to ProcessStream
        myProcessStream.Read(ref myProcess);
        //wait for Process to end
        myProcess.WaitForExit();

        //get the captured output :)
        string output = myProcessStream.StandardOutput;
        string error = myProcessStream.StandardError;
     */
    private Thread StandardOutputReader;
    private Thread StandardErrorReader;
    private Process RunProcess;
    private string _StandardOutput = "";
    private string _StandardError = "";

    public string StandardOutput
    {
        get { return _StandardOutput; }
    }

    public string StandardError
    {
        get { return _StandardError; }
    }

    public ProcessStream()
    {
        Init();
    }

    public void Read(Process process)
    {
        try
        {
            Init();
            RunProcess = process;

            if (RunProcess.StartInfo.RedirectStandardOutput)
            {
                StandardOutputReader = new Thread(new ThreadStart(ReadStandardOutput));
                StandardOutputReader.Start();
            }
            if (RunProcess.StartInfo.RedirectStandardError)
            {
                StandardErrorReader = new Thread(new ThreadStart(ReadStandardError));
                StandardErrorReader.Start();
            }

            int TIMEOUT = 1 * 60 * 1000; // one minute
            if (StandardOutputReader != null)
                StandardOutputReader.Join(TIMEOUT);
            if (StandardErrorReader != null)
                StandardErrorReader.Join(TIMEOUT);

        }
        catch { }
    }

    private void ReadStandardOutput()
    {
        if (RunProcess == null) return;
        try
        {
            StringBuilder sb = new StringBuilder();
            string line = null;
            while ((line = RunProcess.StandardOutput.ReadLine()) != null)
            {
                sb.Append(line);
                sb.Append(Environment.NewLine);
            }
            _StandardOutput = sb.ToString();
        }
        catch { }
    }

    private void ReadStandardError()
    {
        if (RunProcess == null) return;
        try
        {
            StringBuilder sb = new StringBuilder();
            string line = null;
            while ((line = RunProcess.StandardError.ReadLine()) != null)
            {
                sb.Append(line);
                sb.Append(Environment.NewLine);
            }
            _StandardError = sb.ToString();
        }
        catch { }
    }

    private void Init()
    {
        _StandardError = "";
        _StandardOutput = "";
        RunProcess = null;
        Stop();
    }

    public void Stop()
    {
        try { if (StandardOutputReader != null) StandardOutputReader.Abort(); } catch { }
        try { if (StandardErrorReader != null) StandardErrorReader.Abort(); } catch { }
        StandardOutputReader = null;
        StandardErrorReader = null;
    }
}
