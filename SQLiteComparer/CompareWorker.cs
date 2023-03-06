using SQLiteParser;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SQLiteComparer
{
    /// <summary>
    /// This class provides all the methods that are necessary to compare
    /// two SQLite files. It includes support for both schema and data 
    /// comparisons.
    /// </summary>
    public class SQLiteComparer
    {
        #region Fields

        private CompareParams _params;
        private bool _cancelled;
        private Scanner _scanner = new Scanner();
        private SQLiteParser.SQLiteParser _parser;
        private Dictionary<SchemaObject, List<SchemaComparisonItem>> _result;
        private Dictionary<string, TableChanges> _tchanges;
        private Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> _leftSchema;
        private Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> _rightSchema;

        #endregion

        #region Constructors

        public SQLiteComparer(CompareParams cp)
        {
            _params = cp;
            _parser = new SQLiteParser.SQLiteParser(_scanner);
        }

        #endregion

        #region IWorker Implementation

        /// <summary>
        /// Begin the comparison process.
        /// </summary>
        public void Compare()
        {
            _result = null;
            _cancelled = false;
            try
            {
                _leftSchema = ParseDB(_params.LeftDbPath);
                _rightSchema = ParseDB(_params.RightDbPath);
                _result = CompareSchema(_leftSchema, _rightSchema);

                if (_params.ComparisonType == ComparisonType.CompareSchemaAndData)
                {
                    CompareTables(_params.LeftDbPath, _params.RightDbPath, _result, _params.IsCompareBlobFields);
                }
            }
            catch (Exception ex)
            {
                if (_result != null)
                    CleanupTempFiles(_result);
            }
        }

        /// <summary>
        /// Cancel the comparison process
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }

        /// <summary>
        /// Return the result of the operation
        /// </summary>
        public Dictionary<SchemaObject, List<SchemaComparisonItem>> Result
        {
            get { return _result; }
        }

        /// <summary>
        /// This worker supports dual progress notifications
        /// </summary>
        public bool SupportsDualProgress
        {
            get { return true; }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Returns the left schema dictionary
        /// </summary>
        /// <remarks>Call only after the worker has finished doing its job</remarks>
        public Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> LeftSchema
        {
            get { return _leftSchema; }
        }

        /// <summary>
        /// Returns the right schema dictionary
        /// </summary>
        /// <remarks>Call only after the worker has finished doing its job</remarks>
        public Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> RightSchema
        {
            get { return _rightSchema; }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// For every table that has the same schema in both databases - compare its
        /// data and update the _result object if necessary.
        /// </summary>
        /// <param name="leftdb">The leftdb.</param>
        /// <param name="rightdb">The rightdb.</param>
        /// <param name="changes">The list of schema changes to check</param>
        private void CompareTables(
            string leftdb, string rightdb,
            Dictionary<SchemaObject, List<SchemaComparisonItem>> changes, bool allowBlobComparison)
        {
            // Go over all tables and select for comparison only those tables that have identical
            // schema.
            List<SchemaComparisonItem> clist = changes[SchemaObject.Table];

            foreach (SchemaComparisonItem item in clist)
            {
                if (item.Result == ComparisonResult.Same || item.Result == ComparisonResult.DifferentSchema)
                {
                    var tableComparer =
                        new TableComparer(
                                (SQLiteCreateTableStatement)item.LeftDdlStatement,
                                (SQLiteCreateTableStatement)item.RightDdlStatement,
                                leftdb,
                                rightdb,
                                allowBlobComparison);

                    try
                    {
                        tableComparer.Compare();

                        TableChanges tableChanges = (TableChanges)tableComparer.Result;
                        item.TableChanges = tableChanges;
                    }
                    catch (Exception ex)
                    {
                        item.ErrorMessage = ex.Message;
                    }

                    if (_cancelled)
                        return;
                }
            }
        }

        /// <summary>
        /// Cleanup any table changes leftovers
        /// </summary>
        /// <param name="result">The global comparison results object</param>
        private void CleanupTempFiles(Dictionary<SchemaObject, List<SchemaComparisonItem>> result)
        {
            List<SchemaComparisonItem> tableItems = result[SchemaObject.Table];
            foreach (SchemaComparisonItem item in tableItems)
            {
                item.TableChanges?.Dispose();
            }
        }

        /// <summary>
        /// Check if the specified talbe has primary key(s)
        /// </summary>
        /// <param name="table">the table to check</param>
        /// <returns>TRUE if the table has primary key(s)</returns>
        private bool HasPrimaryKeys(SQLiteCreateTableStatement table)
        {
            List<SQLiteColumnStatement> res = Utils.GetPrimaryColumns(table);
            if (res.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Compare the schema information in the specified left/right database schema objects
        /// and return a list of differences.
        /// </summary>
        /// <param name="left">The schema information for the left database.</param>
        /// <param name="right">The schema information for the right database.</param>
        /// <returns>A dictionary that maps, for every schema object type - a list of differences
        /// that were found between the two databases.</returns>
        private Dictionary<SchemaObject, List<SchemaComparisonItem>> CompareSchema(
            Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> left,
            Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> right)
        {
            Dictionary<SchemaObject, List<SchemaComparisonItem>> res =
                new Dictionary<SchemaObject, List<SchemaComparisonItem>>();
            res.Add(SchemaObject.Table, new List<SchemaComparisonItem>());
            res.Add(SchemaObject.Index, new List<SchemaComparisonItem>());
            res.Add(SchemaObject.Trigger, new List<SchemaComparisonItem>());
            res.Add(SchemaObject.View, new List<SchemaComparisonItem>());

            // Prepare auxiliary variables used for notifying progress
            int total = left[SchemaObject.Table].Count + left[SchemaObject.Index].Count +
                left[SchemaObject.Trigger].Count + left[SchemaObject.View].Count +
                right[SchemaObject.Table].Count + right[SchemaObject.Index].Count +
                right[SchemaObject.Trigger].Count + right[SchemaObject.View].Count;

            // First locate all objects that exist in the left DB but not in the right DB
            int index = 0;
            foreach (SchemaObject so in left.Keys)
            {
                foreach (string objname in left[so].Keys)
                {
                    // Ignore internal sqlite tables
                    if (objname.StartsWith("sqlite_"))
                        continue;

                    if (!right[so].ContainsKey(objname))
                    {
                        // This object exists only in the left DB
                        SchemaComparisonItem item =
                            new SchemaComparisonItem(objname, left[so][objname], null, ComparisonResult.ExistsInLeftDB);
                        res[so].Add(item);
                    }
                    else if (!left[so][objname].Equals(right[so][objname]))
                    {
                        // This object exists in both the left DB and the right DB, but it has
                        // different schema layout.
                        SchemaComparisonItem item =
                            new SchemaComparisonItem(objname, left[so][objname], right[so][objname], ComparisonResult.DifferentSchema);
                        res[so].Add(item);
                    }
                    else
                    {
                        // This object exists in both the left DB abd the right DB and it has
                        // the same schema layout in both databases.
                        SchemaComparisonItem item =
                            new SchemaComparisonItem(objname, left[so][objname], right[so][objname], ComparisonResult.Same);
                        res[so].Add(item);
                    }

                    if (_cancelled)
                        return res;
                }
            }

            // Next locate all objects that exist only in the right DB
            foreach (SchemaObject so in right.Keys)
            {
                foreach (string objname in right[so].Keys)
                {
                    // Ignore internal sqlite tables
                    if (objname.StartsWith("sqlite_"))
                        continue;

                    if (!left[so].ContainsKey(objname))
                    {
                        // This object exists only in the right DB
                        SchemaComparisonItem item =
                            new SchemaComparisonItem(objname, null, right[so][objname], ComparisonResult.ExistsInRightDB);
                        res[so].Add(item);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Parse the schema information in the specified DB file and return the information
        /// in a form of a dictionary that provides, for every type of database object (table, index,
        /// trigger and view) - a dictionary of DDL statements that are keyed by the names of these
        /// objects.
        /// </summary>
        /// <param name="fpath">The path to the SQLite database file</param>
        /// <returns>The schema information for the specified file.</returns>
        private Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> ParseDB(string fpath)
        {
            Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>> res =
                new Dictionary<SchemaObject, Dictionary<string, SQLiteDdlStatement>>
                {
                    { SchemaObject.Table, new Dictionary<string, SQLiteDdlStatement>() },
                    { SchemaObject.View, new Dictionary<string, SQLiteDdlStatement>() },
                    { SchemaObject.Trigger, new Dictionary<string, SQLiteDdlStatement>() },
                    { SchemaObject.Index, new Dictionary<string, SQLiteDdlStatement>() }
                };

            SQLiteConnectionStringBuilder sb = new SQLiteConnectionStringBuilder();
            sb.DataSource = fpath;
            sb.ReadOnly = true;

            using (SQLiteConnection conn = new SQLiteConnection(sb.ConnectionString))
            {
                conn.Open();

                SQLiteCommand queryCount = new SQLiteCommand(
                    @"SELECT COUNT(*) FROM sqlite_master WHERE sql IS NOT NULL", conn);
                long count = (long)queryCount.ExecuteScalar();

                int index = 0;
                SQLiteCommand query = new SQLiteCommand(
                    @"SELECT * FROM sqlite_master WHERE sql IS NOT NULL", conn);
                using (SQLiteDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sql = (string)reader["sql"];
                        sql = Utils.StripComments(sql);
                        _scanner.SetSource(sql, 0);

                        // Request the parser to parse the SQL statement
                        bool ok = _parser.Parse();
                        if (!ok)
                            throw new ApplicationException("invalid sql string");

                        SQLiteDdlStatement stmt = SQLiteDdlMain.GetStatement();
                        if (stmt is SQLiteCreateTableStatement)
                            res[SchemaObject.Table].Add(SQLiteParser.Utils.Chop(stmt.ObjectName.ToString()).ToLower(), stmt);
                        else if (stmt is SQLiteCreateIndexStatement)
                            res[SchemaObject.Index].Add(SQLiteParser.Utils.Chop(stmt.ObjectName.ToString()).ToLower(), stmt);
                        else if (stmt is SQLiteCreateViewStatement)
                            res[SchemaObject.View].Add(SQLiteParser.Utils.Chop(stmt.ObjectName.ToString()).ToLower(), stmt);
                        else if (stmt is SQLiteCreateTriggerStatement)
                            res[SchemaObject.Trigger].Add(SQLiteParser.Utils.Chop(stmt.ObjectName.ToString()).ToLower(), stmt);
                        else
                            throw new ApplicationException("illegal ddl statement type [" + stmt.GetType().FullName + "]");

                    }
                }
            }

            return res;
        }


        #endregion
    }

}
