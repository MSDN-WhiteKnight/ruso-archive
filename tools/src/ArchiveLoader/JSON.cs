using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace ArchiveLoader
{
    public static class JSON
    {
        
        public static object Parse(string str)
        {
            dynamic html=null;
            dynamic JsonObject = null;
            try
            {
                html = Activator.CreateInstance(Type.GetTypeFromProgID("htmlfile"));
                html.open();
                html.write("<html><head><meta http-equiv=\"x-ua-compatible\" content=\"IE=9\" /></head><body></body></html>");

                JsonObject = html.defaultView.JSON;
                return JsonObject.parse(str);
            }
            finally
            {
                if (html != null) Marshal.ReleaseComObject(html);
                if (JsonObject != null) Marshal.ReleaseComObject(JsonObject);
            }
        }

        static object GetProperty(IDispatch dispatchObject, string property)
        {
            int dispId = GetDispId(dispatchObject, property);
            return InvokePropertyGetter(dispatchObject, dispId);            
        }
        
        const uint DISPATCH_METHOD = 0x1;
        const uint DISPATCH_PROPERTYGET = 0x2;
        const uint DISPATCH_PROPERTYPUT = 0x4;
                
        // Source: https://github.com/dotnet/corefx/issues/19731
        static int GetDispId(IDispatch dispatchObject, string methodName)
        {
            int[] dispIds = new int[1];
            Guid emtpyRiid = Guid.Empty;
            dispatchObject.GetIDsOfNames(
                emtpyRiid,
                new string[] { methodName },
                1,
                0,
                dispIds);

            if (dispIds[0] == -1)
            {
                throw new ArgumentException(String.Format("Method name {0} cannot be recognized", methodName));
            }

            return dispIds[0];
        }

        static object InvokePropertyGetter(IDispatch target, int dispId)
        {            
            var paramArray = new ComTypes.DISPPARAMS[1];
            paramArray[0].cArgs = 0;
            paramArray[0].cNamedArgs = 0;

            ComTypes.EXCEPINFO info = default(ComTypes.EXCEPINFO);
            object result = null;
            uint puArgErrNotUsed = 0;

            target.Invoke(dispId, new Guid(), 0x0409, ComTypes.INVOKEKIND.INVOKE_PROPERTYGET,
                    paramArray, out result, out info, out puArgErrNotUsed);

            return result;
        }

        public static IEnumerable<object> ToCollection(object o)
        {
            IDispatch dispatchObject = null;
            dispatchObject = o as IDispatch;

            if (dispatchObject == null) throw new ArgumentException("Object is not IDispatch");                       

            try
            {
                int len = Convert.ToInt32(GetProperty(dispatchObject, "length"));
                List<object> res = new List<object>(len);

                for (int i = 0; i < len; i++)
                {
                    res.Add( GetProperty(dispatchObject, i.ToString()) );
                }
                return res;
            }
            finally
            {
                Marshal.ReleaseComObject(dispatchObject);
            }
        }

        // *** COM INTEROP **
        [DllImport("ole32.dll")]
        static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

        [Guid("00020400-0000-0000-c000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        interface IDispatch
        {
            [PreserveSig]
            int GetTypeInfoCount(out int info);

            [PreserveSig]
            int GetTypeInfo(int iTInfo, int lcid, out ComTypes.ITypeInfo ppTInfo);

            void GetIDsOfNames(
                [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgszNames,
                int cNames,
                int lcid,
                [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] rgDispId);

            void Invoke(
                int dispIdMember,
                [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
                int lcid,
                ComTypes.INVOKEKIND wFlags,
                [In, Out] [MarshalAs(UnmanagedType.LPArray)] ComTypes.DISPPARAMS[] paramArray,
                out object pVarResult,
                out ComTypes.EXCEPINFO pExcepInfo,
                out uint puArgErr);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Variant
        {
            public ushort vt;
            ushort wReserved1;
            ushort wReserved2;
            ushort wReserved3;
            public IntPtr p;
        }

    }
}
