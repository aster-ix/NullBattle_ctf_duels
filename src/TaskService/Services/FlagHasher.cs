using System.Security.Cryptography;
using System.Text;

namespace NullBattle.TaskBank.Services;

public class FlagHasher
{
    public string Hash(string flag)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(flag));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string flag, string hash)
    {
        return Hash(flag) == hash;
    }
}