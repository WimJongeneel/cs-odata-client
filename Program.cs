using System;
using System.Linq;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Collections.Generic;
using OdataClient;

namespace cs_odata_client
{
  class Program
  {
    static void Main(string[] args)
    {
      // var q = new PersonQuery<Unit>()
      //   .Select(p => new { p.Id, p.Email });

      // var q1 = new PersonQuery<Unit>()
      // .Select(p => p.Email);


      Func<string> V = () => "henk@mail.com";
      var q2 = new Query<Person>()
        .Filter(p => p.Id == 1 && p.Email == V())
        .Select(p => p.Email)
        .Compile();

      Console.WriteLine(q2);

      Console.WriteLine("Hello World!");
    }
  }

  interface Unit { }

  public class Person
  {
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public Person(int Id)
    {
      this.Id = Id;
    }
  }
}