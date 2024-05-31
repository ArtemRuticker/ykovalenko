using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using GeneratedClasses;
using NHibernate;
partial class Program
{
    static void Main(string[] args)
    {
        var sessionFactory = CreateSessionFactory();

        using (var session = sessionFactory.OpenSession())
        {
            var class1Items = session.Query<GeneratedClasses.Class1>().ToList();
            foreach (var item in class1Items)
            {
                Console.WriteLine($"Id: {item.Id},  col: {item.Col}");
            }
        }
    }

    private static ISessionFactory CreateSessionFactory()
    {
        return Fluently.Configure()
            .Database(PostgreSQLConfiguration.Standard
                .ConnectionString("Host=localhost;Username=postgres;Password=121212;Database=test_db"))
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Class1Map>())
            .BuildSessionFactory();
    }
}