using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SysGadget.Core
{
    /// <summary>
    /// Reads NVIDIA GPU temperatures through NVML (nvml.dll, installed with the NVIDIA driver).
    /// Needs no admin rights and no extra driver.
    /// </summary>
    public sealed class NvmlTemperatureProvider : IDisposable
    {
        private const string Library = "nvml.dll";
        private const int Success = 0;
        private const int GpuSensor = 0; // NVML_TEMPERATURE_GPU
        private const int NameLength = 96; // NVML_DEVICE_NAME_V2_BUFFER_SIZE

        [DllImport(Library, EntryPoint = "nvmlInit_v2")]
        private static extern int Init();

        [DllImport(Library, EntryPoint = "nvmlShutdown")]
        private static extern int Shutdown();

        [DllImport(Library, EntryPoint = "nvmlDeviceGetCount_v2")]
        private static extern int GetCount(out uint count);

        [DllImport(Library, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int GetHandle(uint index, out IntPtr device);

        [DllImport(Library, EntryPoint = "nvmlDeviceGetName", CharSet = CharSet.Ansi)]
        private static extern int GetName(IntPtr device, StringBuilder name, uint length);

        [DllImport(Library, EntryPoint = "nvmlDeviceGetTemperature")]
        private static extern int GetTemperature(IntPtr device, int sensor, out uint celsius);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);

        private readonly List<IntPtr> devices = new List<IntPtr>();
        private readonly List<string> names = new List<string>();
        private bool open;

        /// <summary>Starts NVML and lists the NVIDIA GPUs.</summary>
        /// <exception cref="InvalidOperationException">An NVML call failed; the message names the call and its code.</exception>
        public NvmlTemperatureProvider()
        {
            Check(Init(), "nvmlInit_v2");
            open = true;
            Check(GetCount(out uint count), "nvmlDeviceGetCount_v2");
            for (uint index = 0; index < count; index++)
            {
                Check(GetHandle(index, out IntPtr device), "nvmlDeviceGetHandleByIndex_v2");
                StringBuilder name = new StringBuilder(NameLength);
                Check(GetName(device, name, NameLength), "nvmlDeviceGetName");
                devices.Add(device);
                names.Add(name.ToString());
            }
        }

        /// <summary>Whether nvml.dll can be loaded, that is, whether an NVIDIA driver is installed.</summary>
        /// <returns>True when NVML is present.</returns>
        public static bool IsInstalled()
        {
            IntPtr module = LoadLibrary(Library);
            if (module == IntPtr.Zero)
            {
                return false;
            }

            FreeLibrary(module);
            return true;
        }

        /// <summary>Reads the core temperature of every NVIDIA GPU.</summary>
        /// <returns>One reading per GPU; the hardware name is the GPU name, the sensor name "GPU".</returns>
        /// <exception cref="InvalidOperationException">An NVML call failed.</exception>
        public IReadOnlyList<TemperatureReading> Read()
        {
            List<TemperatureReading> readings = new List<TemperatureReading>(devices.Count);
            for (int i = 0; i < devices.Count; i++)
            {
                Check(GetTemperature(devices[i], GpuSensor, out uint celsius), "nvmlDeviceGetTemperature");
                readings.Add(new TemperatureReading(names[i], "GPU", celsius));
            }

            return readings;
        }

        /// <summary>Shuts NVML down.</summary>
        public void Dispose()
        {
            if (open)
            {
                Shutdown();
                open = false;
            }
        }

        private static void Check(int result, string call)
        {
            if (result != Success)
            {
                throw new InvalidOperationException(call + " failed with NVML code " + result + ".");
            }
        }
    }
}
