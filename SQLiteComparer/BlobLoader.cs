using System;

namespace SQLiteComparer
{
    /// <summary>
    /// This class is responsible for loading a BLOB field into the local file system
    /// and doing this in a chunky way that won't cause too much memory to be used.
    /// </summary>
    public class BlobLoader : IDisposable
    {
        #region Fields

        private long _rowId;
        private string _blobFile;
        private string _tableName;
        private string _columnName;
        private int _progress = 0;
        private bool _disposed;
        private BlobReaderWriter _blobReader = null;
        private bool _canceled;

        #endregion

        #region Constructors & Destructors
        public BlobLoader(string dbpath, string tableName, string columnName, long rowId, string blobFile)
        {
            _blobReader = new BlobReaderWriter(dbpath, true);
            _tableName = tableName;
            _columnName = columnName;
            _rowId = rowId;
            _blobFile = blobFile;
        }

        ~BlobLoader()
        {
            Dispose(false);
        }

        #endregion

        #region Methods

        public void Load()
        {
            // Call the BLOB reader object to do the job
            _blobReader.ReadBlobToFile(_tableName, _columnName, _rowId, _blobFile, ProgressHandler);
        }

        public void Cancel()
        {
            _canceled = true;
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
                    _blobReader.Dispose();
                }

                // Mark that the object was disposed
                _disposed = true;
            }
        }

        private void ProgressHandler(byte[] buffer, int length, int bytesRead, int totalBytes, ref bool cancel)
        {
            int progress = (int)(100.0 * bytesRead / totalBytes);
            if (progress > _progress)
            {
                _progress = progress;
            }
        }

        #endregion
    }
}
