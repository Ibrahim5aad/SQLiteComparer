using System;

namespace SQLiteComparer
{
    /// <summary>
    /// This class provides an implementation for saving BLOB fields from a file to the 
    /// BLOB field that is specified in the constructor
    /// </summary>
    public class BlobSaver : IDisposable
    {
        #region Constructors & Destructors
        public BlobSaver(string dbpath, string tableName, string columnName, long rowId, string blobFile)
        {
            _blobWriter = new BlobReaderWriter(dbpath, false);
            _tableName = tableName;
            _columnName = columnName;
            _rowId = rowId;
            _blobFile = blobFile;
        }

        public BlobSaver(string dbpath, string tableName, string columnName, long rowId, byte[] buffer)
        {
            _blobWriter = new BlobReaderWriter(dbpath, false);
            _tableName = tableName;
            _columnName = columnName;
            _rowId = rowId;
            _buffer = buffer;
        }

        ~BlobSaver()
        {
            Dispose(false);
        }
        #endregion

        #region Protected Overrided Methods
        protected void Save()
        {
            if (_blobFile != null)
            {
                _blobWriter.WriteBlobFromFile(_tableName, _columnName, _rowId, _blobFile, ProgressHandler);
            }
            else
            {
                _blobWriter.WriteBlob(_tableName, _columnName, _rowId, _buffer.Length, MemoryWriter);
            } // else
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Virtual Methods
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                    _blobWriter.Dispose();
                }

                // Mark that the object was disposed
                _disposed = true;
            }
        }
        #endregion

        #region Private Methods
        private int MemoryWriter(byte[] buffer, int max, int written, int total, ref bool cancel)
        {
            ProgressHandler(buffer, max, written, total, ref cancel);

            int nbytes = _buffer.Length;
            if (nbytes > max)
                nbytes = max;
            Buffer.BlockCopy(_buffer, written, buffer, 0, nbytes);
            return nbytes;
        }

        private int ProgressHandler(byte[] buffer, int max, int written, int total, ref bool cancel)
        {
            int progress = (int)(100.0 * written / total);
            if (progress > _progress)
            {
                _progress = progress;
            }
            return max;
        }
        #endregion

        #region Private Variables
        private long _rowId;
        private string _blobFile;
        private byte[] _buffer;
        private string _tableName;
        private string _columnName;
        private int _progress = 0;
        private bool _disposed;
        private BlobReaderWriter _blobWriter = null;
        #endregion
    }
}
