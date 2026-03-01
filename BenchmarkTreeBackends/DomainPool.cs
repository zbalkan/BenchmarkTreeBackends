namespace BenchmarkTreeBackends
{
    public static class DomainPool
    {
        public static readonly string[] ReadDomains =
        {
            "google.com", "www.google.com", "mail.google.com", "drive.google.com",
            "microsoft.com", "www.microsoft.com", "login.microsoft.com",
            "github.com", "www.github.com", "api.github.com",
            "example.com", "www.example.com", "api.example.com",
            "wikipedia.org", "www.wikipedia.org", "en.wikipedia.org",
            "mozilla.org", "developer.mozilla.org",
            "a.b.c.d.e.f.g.h.i.j.k.example.com",
            "bbc.co.uk", "news.bbc.co.uk",
            "golang.org", "pkg.go.dev",
            "a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.a.com",
            "xn--hxajbheg2az3al.gr",
            "*.google.com"
        };

        public static string[] GenerateWriteDomains(int threadIndex, int count)
        {
            string[] domains = new string[count];
            for (int i = 0; i < count; i++)
                domains[i] = $"t{threadIndex}.bench{i}.concurrent-test.com";
            return domains;
        }
    }
}