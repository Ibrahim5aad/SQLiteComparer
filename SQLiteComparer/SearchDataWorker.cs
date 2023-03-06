namespace SQLiteComparer
{
    /// <summary>
    /// This class is responsible to search for a data row starting from the specified
    /// row index that matches the specified criteria
    /// </summary>
    public class SearchDataWorker
    {
        #region Constructors
        public SearchDataWorker(bool isLeft, string diff, TableChanges tchanges, long rowIndex, long maxRowIndex, string sql)
        {
            _maxRowIndex = maxRowIndex;
            _isLeft = isLeft;
            _changes = tchanges;
            _rowIndex = rowIndex;
            _sql = sql;
            _diff = diff;
        }
        #endregion

        public long Result { get; private set; }

        #region Methods

        public void Search()
        {
            if (_sql != null)
                Result = _changes.SearchRowsBySQL(_isLeft, _diff, _rowIndex, _maxRowIndex, _sql, SearchHandler);
        }

        private void SearchHandler(long searchedRows, long total, ref bool cancel)
        {
            if (total == 0)
                return;
            int next = (int)(100.0 * searchedRows / total);
            if (next > _progress)
            {
                _progress = next;
            }
        }

        #endregion

        #region Private Variables
        private bool _isLeft;
        private string _diff;
        private TableChanges _changes;
        private long _rowIndex;
        private long _maxRowIndex = -1;
        private string _sql;
        private int _progress;
        #endregion
    }
}
