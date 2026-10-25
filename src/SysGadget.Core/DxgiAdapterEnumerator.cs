using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SysGadget.Core
{
    /// <summary>Lists the hardware graphics adapters through DXGI.</summary>
    public sealed class DxgiAdapterEnumerator
    {
        private const int DxgiErrorNotFound = unchecked((int)0x887A0002);
        private const uint SoftwareAdapterFlag = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct AdapterDesc1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Description;
            public uint VendorId;
            public uint DeviceId;
            public uint SubSysId;
            public uint Revision;
            public UIntPtr DedicatedVideoMemory;
            public UIntPtr DedicatedSystemMemory;
            public UIntPtr SharedSystemMemory;
            public uint LuidLow;
            public int LuidHigh;
            public uint Flags;
        }

        // Only the vtable order matters; the placeholders stand for methods that are never called.
        [ComImport, Guid("770aae78-f26f-4dba-a829-253c83d1b387"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDxgiFactory1
        {
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetPrivateData();
            void GetParent();
            void EnumAdapters();
            void MakeWindowAssociation();
            void GetWindowAssociation();
            void CreateSwapChain();
            void CreateSoftwareAdapter();

            [PreserveSig]
            int EnumAdapters1(uint index, out IDxgiAdapter1 adapter);
        }

        [ComImport, Guid("29038f61-3839-4626-91fd-086879011a05"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDxgiAdapter1
        {
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetPrivateData();
            void GetParent();
            void EnumOutputs();
            void GetDesc();
            void CheckInterfaceSupport();

            [PreserveSig]
            int GetDesc1(out AdapterDesc1 desc);
        }

        [DllImport("dxgi.dll")]
        private static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr factory);

        /// <summary>Lists the hardware adapters; the software renderer is left out.</summary>
        /// <returns>One entry per hardware adapter.</returns>
        /// <exception cref="InvalidOperationException">DXGI could not be used.</exception>
        public IReadOnlyList<GpuAdapterInfo> List()
        {
            Guid factoryId = typeof(IDxgiFactory1).GUID;
            int hr = CreateDXGIFactory1(ref factoryId, out IntPtr factoryPointer);
            if (hr < 0)
            {
                throw new InvalidOperationException("CreateDXGIFactory1 failed, HRESULT 0x" + hr.ToString("x8"));
            }

            IDxgiFactory1 factory = (IDxgiFactory1)Marshal.GetObjectForIUnknown(factoryPointer);
            Marshal.Release(factoryPointer);
            List<GpuAdapterInfo> adapters = new List<GpuAdapterInfo>();
            try
            {
                for (uint index = 0; ; index++)
                {
                    hr = factory.EnumAdapters1(index, out IDxgiAdapter1 adapter);
                    if (hr == DxgiErrorNotFound)
                    {
                        break;
                    }

                    if (hr < 0)
                    {
                        throw new InvalidOperationException("EnumAdapters1 failed, HRESULT 0x" + hr.ToString("x8"));
                    }

                    try
                    {
                        hr = adapter.GetDesc1(out AdapterDesc1 desc);
                        if (hr < 0)
                        {
                            throw new InvalidOperationException("GetDesc1 failed, HRESULT 0x" + hr.ToString("x8"));
                        }

                        if ((desc.Flags & SoftwareAdapterFlag) == 0)
                        {
                            adapters.Add(new GpuAdapterInfo(
                                GpuInstanceName.FormatLuid(desc.LuidHigh, desc.LuidLow),
                                desc.Description,
                                desc.DedicatedVideoMemory.ToUInt64()));
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(adapter);
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }

            return adapters;
        }
    }
}
