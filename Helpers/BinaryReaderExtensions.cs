using System.Runtime.InteropServices;

namespace Xargon.NET.Helpers;

public static class BinaryReaderExtensions
{
    public static T ReadStruct<T>(this BinaryReader reader) where T : struct
    {
        byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }

    public static void WriteStruct<T>(this BinaryWriter writer, T value) where T : struct
    {
        byte[] bytes = new byte[Marshal.SizeOf(typeof(T))];
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            writer.Write(bytes);
        }
        finally
        {
            handle.Free();
        }
    }
}
