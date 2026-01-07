using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Text;

namespace WindowsGSM.Plugins
{
    public class StarRupture : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.StarRupture",
            author = "Emeria",
            description = "WindowsGSM plugin for supporting StarRupture Dedicated Server",
            version = "1.4",
            url = "https://github.com/emeria/WindowsGSM.StarRupture",
            color = "#34ebcf"
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "3809400"; 

        // - Standard Constructor
        public StarRupture(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;

        // - Game server Fixed variables
        public override string StartPath => @"StarRuptureServerEOS.exe";
        public string FullName = "StarRupture Dedicated Server";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public object QueryMethod = new A2S();

        // - Game server default values
        public string Port = "7777";
        public string QueryPort = "27015";
        public string Defaultmap = "Default";
        public string Maxplayers = "4";
        public string Additional = "-Log";

        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Ensure config directory exists
            await CreateServerConfig();

            StringBuilder sb = new StringBuilder();
            sb.Append("-Log -MULTIHOME=0.0.0.0");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerPort) ? "" : $" -Port={_serverData.ServerPort}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? "" : $" -QueryPort={_serverData.ServerQueryPort}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? "" : $" -MaxPlayers={_serverData.ServerMaxPlayer}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerName) ? "" : $" -ServerName=\"{_serverData.ServerName}\"");
            sb.Append($" {_serverData.ServerParam}");

            var gameServerProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = sb.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            if (AllowsEmbedConsole)
            {
                gameServerProcess.StartInfo.CreateNoWindow = true;
                gameServerProcess.StartInfo.RedirectStandardInput = true;
                gameServerProcess.StartInfo.RedirectStandardOutput = true;
                gameServerProcess.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                gameServerProcess.OutputDataReceived += serverConsole.AddOutput;
                gameServerProcess.ErrorDataReceived += serverConsole.AddOutput;

                try { gameServerProcess.Start(); } catch (Exception e) { Error = e.Message; return null; }

                gameServerProcess.BeginOutputReadLine();
                gameServerProcess.BeginErrorReadLine();
                return gameServerProcess;
            }

            try { gameServerProcess.Start(); return gameServerProcess; } catch (Exception e) { Error = e.Message; return null; }
        }

        public Task CreateServerConfig()
        {
            string configDir = ServerPath.GetServersServerFiles(_serverData.ServerID, @"StarRupture\Saved\Config\WindowsServer");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            return Task.CompletedTask;
        }

        public async Task Stop(Process gameServerProcess)
        {
            await Task.Run(() =>
            {
                if (gameServerProcess != null && !gameServerProcess.HasExited)
                {
                    gameServerProcess.Kill();
                    gameServerProcess.WaitForExit(5000);
                }
            });
        }

        // Use 'new' keyword to resolve inheritance hiding warnings
        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p?.WaitForExit(); });
            return p;
        }

        public new bool IsInstallValid() => File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        public new bool IsImportValid(string path) => File.Exists(Path.Combine(path, StartPath));
        public new string GetLocalBuild() => new Installer.SteamCMD().GetLocalBuild(_serverData.ServerID, AppId);
        public new async Task<string> GetRemoteBuild() => await new Installer.SteamCMD().GetRemoteBuild(AppId);
    }
}
