using System.Reflection;
using BrilliantSkies.DataManagement.Serialisation;

namespace DecoLimitLifter.ExtendedSerialization
{
    internal static class Priv
    {
        private const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // -------- SuperBase.HeaderCount (property) ----------
        private static readonly PropertyInfo P_HeaderCount =
            typeof(SuperBase).GetProperty("HeaderCount", BF);

        public static uint GetHeaderCount(object obj) =>
            (uint)P_HeaderCount.GetValue(obj, null);

        public static void SetHeaderCount(object obj, uint v) =>
            P_HeaderCount.SetValue(obj, v, null);

        // ---------------- SuperLoader fields ----------------
        private static readonly FieldInfo F_SL_Header =
            typeof(SuperLoader).GetField("Header", BF);
        private static readonly FieldInfo F_SL_DataSorted =
            typeof(SuperLoader).GetField("DataSorted", BF);
        private static readonly FieldInfo F_SL_ReaderLenSorted =
            typeof(SuperLoader).GetField("_readerLengthOfSortedSegment", BF);
        private static readonly FieldInfo F_SL_TotalDataLenSorted =
            typeof(SuperLoader).GetField("_totalDataLengthSorted", BF);
        private static readonly FieldInfo F_SL_DatasWrittenSorted =
            typeof(SuperLoader).GetField("_datasWrittenSorted", BF);

        public static byte[] HeaderRef_Loader(SuperLoader self) =>
            (byte[])F_SL_Header.GetValue(self);
        public static void SetHeaderRef_Loader(SuperLoader self, byte[] v) =>
            F_SL_Header.SetValue(self, v);

        public static byte[] DataSortedRef_Loader(SuperLoader self) =>
            (byte[])F_SL_DataSorted.GetValue(self);
        public static void SetDataSortedRef_Loader(SuperLoader self, byte[] v) =>
            F_SL_DataSorted.SetValue(self, v);

        public static uint ReaderLenRef(SuperLoader self) =>
            (uint)F_SL_ReaderLenSorted.GetValue(self);
        public static void SetReaderLenRef(SuperLoader self, uint v) =>
            F_SL_ReaderLenSorted.SetValue(self, v);

        public static uint TotalDataLenRef(SuperLoader self) =>
            (uint)F_SL_TotalDataLenSorted.GetValue(self);
        public static void SetTotalDataLenRef(SuperLoader self, uint v) =>
            F_SL_TotalDataLenSorted.SetValue(self, v);

        public static uint DatasWrittenRef_Loader(SuperLoader self) =>
            (uint)F_SL_DatasWrittenSorted.GetValue(self);
        public static void SetDatasWrittenRef_Loader(SuperLoader self, uint v) =>
            F_SL_DatasWrittenSorted.SetValue(self, v);

        // ---------------- SuperSaver fields -----------------
        private static readonly FieldInfo F_SS_Header =
            typeof(SuperSaver).GetField("Header", BF);
        private static readonly FieldInfo F_SS_DataSorted =
            typeof(SuperSaver).GetField("DataSorted", BF);
        private static readonly FieldInfo F_SS_DatasWrittenSorted =
            typeof(SuperSaver).GetField("_datasWrittenSorted", BF);

        public static byte[] HeaderRef_Saver(SuperSaver self) =>
            (byte[])F_SS_Header.GetValue(self);
        public static void SetHeaderRef_Saver(SuperSaver self, byte[] v) =>
            F_SS_Header.SetValue(self, v);

        public static byte[] DataSortedRef_Saver(SuperSaver self) =>
            (byte[])F_SS_DataSorted.GetValue(self);
        public static void SetDataSortedRef_Saver(SuperSaver self, byte[] v) =>
            F_SS_DataSorted.SetValue(self, v);
    }
}
