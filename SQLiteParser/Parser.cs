namespace SQLiteParser
{
    public partial class SQLiteParser
    {
        public SQLiteParser(Scanner scanner)
          : base(scanner)
        {
        }


        public SQLiteParser()
           : base(new Scanner())
        {
        }
    }
}
