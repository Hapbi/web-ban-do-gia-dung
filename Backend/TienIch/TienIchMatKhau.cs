using System.Security.Cryptography;
using System.Text;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Băm và kiểm tra mật khẩu; không lưu mật khẩu dạng chữ thường vào cơ sở dữ liệu.</summary>
public static class MatKhauHelper
{
    private const string TienTo = "pbkdf2-sha256";
    private const int SoVongLap = 210_000;
    private const int DoDaiHash = 32;
    private const string MuoiCu = "GIADUNGPRO_2024_";

    public static string Hash(string matKhau)
    {
        ArgumentNullException.ThrowIfNull(matKhau);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, SoVongLap, HashAlgorithmName.SHA256, DoDaiHash);
        return $"{TienTo}${SoVongLap}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool LaHashCu(string? hash) =>
        string.IsNullOrWhiteSpace(hash) || !hash.StartsWith(TienTo + "$", StringComparison.Ordinal);

    public static bool KiemTra(string matKhau, string hash)
    {
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrWhiteSpace(hash)) return false;
        var parts = hash.Split('$');
        if (parts.Length == 4 && parts[0] == TienTo &&
            int.TryParse(parts[1], out var iterations) && iterations is >= 100_000 and <= 1_000_000)
        {
            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
        }

        // Tương thích mật khẩu cũ: kiểm tra hash cũ và tự nâng cấp sau khi đăng nhập thành công.
        using var sha = SHA256.Create();
        var legacy = sha.ComputeHash(Encoding.UTF8.GetBytes(MuoiCu + matKhau));
        try
        {
            var expected = Convert.FromBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(legacy, expected);
        }
        catch (FormatException) { return false; }
    }
}
