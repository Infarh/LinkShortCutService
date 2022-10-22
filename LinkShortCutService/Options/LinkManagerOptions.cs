using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkShortCutService.Options;

public record LinkManagerOptions
(
    int HashStringLength = 5,
    string HashAlgorithm = "MD5",
    string Encoding = "UTF-32",
    int ConcurrentDbTimeout = 100,
    int ConcurrentDbTryCount = 10
)
{
    public LinkManagerOptions() : this(5, "MD5", "UTF-32", 100, 10) { }
}
