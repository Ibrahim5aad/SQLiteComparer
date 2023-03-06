using System;

namespace SQLiteComparer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // Remove any stale table change files
            TableChanges.RemoveStaleChangeFiles();

            try
            {
                var file1 = @"D:\Work\Xbim\Flex\test\Xbim.Flex.Brep - Copy\Xbim.Geometry.NetCore.Tests\bin\Debug\net6.0\TestData\Ifc\door.geomdb";
                var file2 = @"D:\Work\Xbim\Flex\test\Xbim.Flex.Brep - Copy\Xbim.Geometry.NetCore.Tests\bin\Debug\net6.0\TestData\Ifc\ImperialModelObject.geomdb";

                CompareParams compareParams = new CompareParams(file1, file2, ComparisonType.CompareSchemaAndData, true);
                SQLiteComparer comparer = new SQLiteComparer(compareParams);
                // NativeLibrary.Load("e_sqlite3.dll", Assembly.GetExecutingAssembly(), DllImportSearchPath.AssemblyDirectory);

                comparer.Compare();
                var tableChanges = comparer.Result[SchemaObject.Table][0].TableChanges;

                var total = tableChanges.GetTotalChangesCount(new string[] { TableChanges.EXISTS_IN_LEFT_TABLE_NAME });
                try
                {

                var changes = tableChanges.GetChanges(TableChanges.EXISTS_IN_LEFT_TABLE_NAME, (int)total, 0);
                }
                catch (Exception)
                { 
                }
                try
                {

                var changes2 = tableChanges.GetChanges(TableChanges.EXISTS_IN_RIGHT_TABLE_NAME, (int)total, 0);
                }
                catch (Exception)
                { 
                }


                DiffExporter diffExporter = new DiffExporter(tableChanges, "out.csv", true, true, true);

                diffExporter.Export();
            }
            catch (Exception ex)
            {
                ShowUnexpectedErrorDialog(ex);
            }
            finally
            {
                // Remove all active change files
                TableChanges.RemoveActiveChangeFiles();
            }
        }

        private static void ShowUnexpectedErrorDialog(Exception error)
        {

        }

    }
}