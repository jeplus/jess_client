using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Runtime.InteropServices;

[RunInstaller(true)]
public class CustomInstaller : Installer {
    [DllImport("regsvr32.exe", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllPath);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GetLastError();

    public override void Install(System.Collections.IDictionary stateSaver) {
        base.Install(stateSaver);

        string assemblyPath = Path.Combine(Path.GetDirectoryName(Context.Parameters ["assemblypath"]), "JessShellExtension.dll");

        try {
            // Register COM component
            int result = RunRegAsm(assemblyPath, true);
            if (result != 0) {
                throw new InstallException($"Failed to register COM component. Error code: {result}; regasm: {System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()}");
            }
        } catch (Exception ex) {
            throw new InstallException($"Registration failed: {ex.Message}, assembly path: {assemblyPath}");
        }

        try {
            // Get the installation directory
            string installPath = Path.GetDirectoryName(Context.Parameters ["assemblypath"]); // Context.Parameters ["TARGETDIR"];

            // Modify the system PATH
            AddToSystemPath(installPath);
        } catch (Exception ex) {
            // Log the error
            throw new InstallException($"Failed to update PATH: {ex.Message}");
        }
    }

    public override void Uninstall(System.Collections.IDictionary savedState) {
        base.Uninstall(savedState);

        string assemblyPath = Path.Combine(Path.GetDirectoryName(Context.Parameters ["assemblypath"]), "JessShellExtension.dll");

        try {
            // Unregister COM component
            int result = RunRegAsm(assemblyPath, false);
            if (result != 0) {
                throw new InstallException($"Failed to unregister COM component. Error code: {result}");
            }
        } catch (Exception ex) {
            throw new InstallException($"Unregistration failed: {ex.Message}, assembly path: {assemblyPath}");
        }

        try {
            // Get the installation directory
            string installPath = Path.GetDirectoryName(Context.Parameters ["assemblypath"]); // Context.Parameters ["TARGETDIR"];

            // Remove from system PATH
            RemoveFromSystemPath(installPath);
        } catch (Exception ex) {
            // Log the error
            throw new InstallException($"Failed to remove PATH: {ex.Message}");
        }

    }

    private int RunRegAsm(string assemblyPath, bool register) {
        string regAsmPath = Path.Combine(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
            "regasm.exe"
        );

        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo {
            FileName = regAsmPath,
            Arguments = $"{(register ? "/codebase" : "/u")} \"{assemblyPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (var process = System.Diagnostics.Process.Start(psi)) {
            process.WaitForExit();
            return process.ExitCode;
        }
    }

    private void AddToSystemPath(string pathToAdd) {
        // Get the current system PATH
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

        // Check if path already exists
        if (!currentPath.Contains(pathToAdd)) {
            // Append the new path
            string newPath = currentPath + (currentPath.EndsWith(";") ? "" : ";") + pathToAdd;

            // Set the new PATH
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
        }
    }

    private void RemoveFromSystemPath(string pathToRemove) {
        // Get the current system PATH
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

        // Remove the specific path
        string newPath = currentPath.Replace(pathToRemove + ";", "")
                                    .Replace(";" + pathToRemove, "")
                                    .Replace(pathToRemove, "");

        // Set the updated PATH
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
    }
}