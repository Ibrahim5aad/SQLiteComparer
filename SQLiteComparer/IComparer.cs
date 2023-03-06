namespace SQLiteComparer
{
    public interface IComparer
    {

        #region Methods

        /// <summary>
        /// Cancel the comparer
        /// </summary>
        void Cancel();

        /// <summary>
        /// This method should be overrided in sub-classes in order to add the specific
        /// code that needs to run in order to perform the comparer's job
        /// </summary>
        void Compare();

        #endregion
    }
}
