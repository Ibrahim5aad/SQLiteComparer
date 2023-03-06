using System;

namespace SQLiteComparer
{
    /// <summary>
    /// This class performs BLOB comparison
    /// </summary>
    public class BlobComparer : IDisposable
    {
        #region Fields

        private BlobReaderWriter _engine;
        private string _tableName;
        private string _columnName;
        private long _rowId1;
        private long _rowId2;
        private bool _disposed;
        private bool _equalBlobs = false;

        #endregion

        #region Constructors & Destructors

        public BlobComparer(string dbpath1, string dbpath2, string tableName, string columnName, long rowId1, long rowId2)
        {
            _engine = new BlobReaderWriter(dbpath1, dbpath2);
            _tableName = tableName;
            _columnName = columnName;
            _rowId1 = rowId1;
            _rowId2 = rowId2;
        }

        ~BlobComparer()
        {
            Dispose(false);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get TRUE if the comparison result is that the two BLOBs are equal,
        /// Get FALSE if the comparison result is that the two BLOBs are different.
        /// </summary>
        public bool IsBlobsEqual
        {
            get { return _equalBlobs; }
        }

        #endregion

        #region Methods

        public void Compare()
        {
            // Call the BLOB reader object to do the job
            _equalBlobs = _engine.CompareBlobs(_tableName, _columnName, _rowId1, _rowId2, HandleBlobCompare);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                    _engine.Dispose();
                }

                // Mark that the object was disposed
                _disposed = true;
            }
        }

        private void HandleBlobCompare(int bytesRead, int totalBytes, ref bool cancel)
        {
            // TODO: Allow the user to cancel during comparison
            cancel = false;
        }
        #endregion
    }
}
