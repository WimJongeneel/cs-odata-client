namespace Hoppinger.OdataClient.Compilation
{
  public static class Utils
  {
        public static string Escape(string s)
        {
            var escaped = "";
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
                current = cc;
            }

            return escaped;
        }
  }
}