using System.Threading.Tasks;

namespace OdataClient
{
  public class Query<T> where T : class {
    public string Filter { get; set; }
    public string Select { get; set; }

    public string Compile() => $"$select={Select}&$fitler={Filter}";

    public async Task<T> Exec() 
    {
      return null;
    }
   }
}