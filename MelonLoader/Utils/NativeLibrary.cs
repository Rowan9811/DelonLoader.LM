using MelonLoader.Utils;
using System;
using System.CodeDom;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MelonLoader
{
    public class NativeLibrary
    {
        public readonly IntPtr Ptr;
        public NativeLibrary(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            Ptr = ptr;
        }

        public static NativeLibrary Load(string filepath)
            => LoadLib(filepath).ToNewNativeLibrary();
        public static NativeLibrary<T> Load<T>(string filepath)
            => LoadLib(filepath).ToNewNativeLibrary<T>();
        public static T ReflectiveLoad<T>(string filepath)
            => Load<T>(filepath).Instance;
        public static IntPtr LoadLib(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException(nameof(filepath));
            IntPtr ptr = AgnosticLoadLibrary(filepath);
            if (ptr == IntPtr.Zero)
            {
                var error = Marshal.PtrToStringAnsi(dlerror());
                throw new DlErrorException($"Unable to Load Native Library {filepath}!\ndlerror: {error}");
            }

            return ptr;
        }

        public IntPtr GetExport(string name)
            => GetExport(Ptr, name);
        public Delegate GetExport(Type type, string name)
            => GetExport(name).GetDelegate(type);
        public T GetExport<T>(string name) where T : Delegate
            => GetExport(name).GetDelegate<T>();
        public void GetExport<T>(string name, out T output) where T : Delegate
            => output = GetExport<T>(name);
        public static IntPtr GetExport(IntPtr nativeLib, string name)
        {
            if (nativeLib == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativeLib));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            IntPtr returnval = AgnosticGetProcAddress(nativeLib, name);
            if (returnval == IntPtr.Zero)
                throw new Exception($"Unable to Find Native Library Export {name}!");

            return returnval;
        }

        public static IntPtr AgnosticLoadLibrary(string name)
        {
            string path = name;
            if (File.Exists(path)) // prevents it from copying libs that don't need copied
            {
                string fileName = Path.GetFileName(path);
                // gotta love net35 not having a combine with more than two arguments
                path = Path.Combine("/data/data/", MelonEnvironment.PackageName);
                path = Path.Combine(path, fileName);

                FileInfo newLib = new(name);
                FileInfo copiedLib = new(path);
                if (copiedLib.Exists && newLib.LastWriteTime > copiedLib.LastWriteTime)
                {
                    copiedLib.Delete();
                    File.Copy(name, path);
                }
                else if (!copiedLib.Exists)
                    File.Copy(name, path);
            }

            return dlopen(path, RTLD_NOW);
        }

        public static IntPtr AgnosticLoadLibrary(Stream stream, string fileName)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            string path = Path.Combine("/data/data/", MelonEnvironment.PackageName);
            path = Path.Combine(path, fileName);

            if (File.Exists(path))
                File.Delete(path);

            using FileStream fileStream = File.OpenWrite(path);
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            fileStream.Write(buffer, 0, buffer.Length);

            return dlopen(path, RTLD_NOW);
        }

        public static IntPtr AgnosticGetProcAddress(IntPtr hModule, string lpProcName)
        {
            return dlsym(hModule, lpProcName);
        }

        [DllImport("libdl.so")]
        protected static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        protected static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        protected static extern IntPtr dlerror();

        const int RTLD_NOW = 2; // for dlopen's flags 
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal delegate string StringDelegate();
    }

    public class NativeLibrary<T> : NativeLibrary
    {
        public readonly T Instance;

        public NativeLibrary(IntPtr ptr) : base(ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));

            Type specifiedType = typeof(T);
            if (specifiedType.IsAbstract && specifiedType.IsSealed)
                throw new Exception($"Specified Type {specifiedType.FullName} must be Non-Static!");

            Instance = (T)Activator.CreateInstance(specifiedType);

            FieldInfo[] fields = specifiedType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length <= 0)
                return;

            foreach (FieldInfo fieldInfo in fields)
            {
                Type fieldType = fieldInfo.FieldType;
                if (fieldType.GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), false).Length <= 0)
                    continue;

                fieldInfo.SetValue(Instance, GetExport(fieldType, fieldInfo.Name));
            }
        }
    }

    public class DlErrorException : Exception
    {
        public DlErrorException() { }
        public DlErrorException(string message) : base(message) { }
        public DlErrorException(string message, Exception inner) : base(message, inner) { }
    }
}
