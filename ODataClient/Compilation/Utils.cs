namespace Hoppinger.OdataClient.Compilation
{
  public static class Utils
  {
        public static string Escape(string s)
        {
            var escaped = "";
            var prev = ' ';
            var current = ' ';

            foreach(var cc in s)
            {
                if(cc == '\'')
                {
                    escaped += "''";
                }
                else
                {
                    escaped += cc;
                }
                prev = current;
                current = cc;
            }

            return escaped;
        }
  }
}