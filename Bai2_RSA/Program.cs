using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static readonly RSAEncryptionPadding OAEP = RSAEncryptionPadding.OaepSHA256;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        while (true)
        {
            Console.WriteLine("\n===== RSA / SHA-256 / CHỮ KÝ SỐ =====");
            Console.WriteLine("1. Sinh cặp khoá RSA và xuất ra file PEM");
            Console.WriteLine("2. Xem file PEM (cấu trúc ASN.1 và ý nghĩa các byte)");
            Console.WriteLine("3. Import PEM và hiển thị n, e (public) hoặc n, e, d, p, q... (private)");
            Console.WriteLine("4. Mã hoá dữ liệu (tối đa 190 byte) bằng public key, lưu Base64");
            Console.WriteLine("5. Giải mã bằng private key và so sánh với dữ liệu gốc");
            Console.WriteLine("6. Đánh giá tốc độ: khoá công khai (e nhỏ) và khoá bí mật (d lớn)");
            Console.WriteLine("7. SHA-256 và hiệu ứng thay đổi đầu vào");
            Console.WriteLine("8. Chữ ký số: PKCS#1 v1.5, RSA-PSS và phương pháp thêm nonce");
            Console.WriteLine("0. Thoát");
            Console.Write("Chọn: ");
            string? c = Console.ReadLine()?.Trim();

            try
            {
                switch (c)
                {
                    case "1": GenKeys(); break;
                    case "2": ViewPem(); break;
                    case "3": ImportPem(); break;
                    case "4": EncryptRsa(); break;
                    case "5": DecryptRsa(); break;
                    case "6": Benchmark(); break;
                    case "7": Sha256Demo(); break;
                    case "8": SignDemo(); break;
                    case "0": return;
                    default: Console.WriteLine("Lựa chọn không hợp lệ."); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
        }
    }

    // ---------- Tiện ích ----------
    static string Ask(string prompt, string def = "")
    {
        Console.Write(def == "" ? prompt : $"{prompt}[{def}] ");
        string s = (Console.ReadLine() ?? "").Trim().Trim('"');
        return s == "" ? def : s;
    }

    static RSA LoadRsa(string path)
    {
        var r = RSA.Create();
        r.ImportFromPem(File.ReadAllText(path));
        return r;
    }

    static BigInteger ToBig(byte[] b) => new BigInteger(b, isUnsigned: true, isBigEndian: true);

    // ---------- 1. Sinh khoá + xuất PEM ----------
    static void GenKeys()
    {
        int bits = int.Parse(Ask("Độ dài khoá (1024/2048/3072/4096): ", "2048"));
        var sw = Stopwatch.StartNew();
        using var r = RSA.Create(bits);
        sw.Stop();

        string pubPath = Ask("File lưu khoá công khai: ", "public.pem");
        string privPath = Ask("File lưu khoá bí mật: ", "private.pem");
        File.WriteAllText(pubPath, r.ExportSubjectPublicKeyInfoPem());   // BEGIN PUBLIC KEY
        File.WriteAllText(privPath, r.ExportPkcs8PrivateKeyPem());       // BEGIN PRIVATE KEY

        Console.WriteLine($"Đã sinh khoá RSA {bits} bit trong {sw.Elapsed.TotalMilliseconds:0.0} ms.");
        Console.WriteLine("Public : " + Path.GetFullPath(pubPath));
        Console.WriteLine("Private: " + Path.GetFullPath(privPath));
    }

    // ---------- 2. Xem PEM ----------
    static readonly string[] PublicNames = { "n (modulus)", "e (publicExponent)" };
    static readonly string[] PrivateNames =
    {
        "version (PKCS#8)", "version (PKCS#1)", "n (modulus)", "e (publicExponent)",
        "d (privateExponent)", "p (prime1)", "q (prime2)", "dP = d mod (p-1)",
        "dQ = d mod (q-1)", "qInv = q^-1 mod p"
    };
    static string[] curNames = PublicNames;
    static int intIdx;

    static void ViewPem()
    {
        string path = Ask("File PEM: ", "public.pem");
        string[] lines = File.ReadAllText(path)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string header = lines[0], footer = lines[^1];
        string b64 = string.Concat(lines.Skip(1).Take(lines.Length - 2));
        byte[] der = Convert.FromBase64String(b64);

        Console.WriteLine($"\nDòng đầu  : {header}   (đánh dấu loại dữ liệu)");
        Console.WriteLine($"Phần giữa : {b64.Length} ký tự Base64 (chia dòng 64 ký tự) -> giải mã ra {der.Length} byte DER");
        Console.WriteLine($"Dòng cuối : {footer}");
        Console.WriteLine("32 byte đầu của DER: " + Convert.ToHexString(der, 0, Math.Min(32, der.Length)));

        if (header.Contains("BEGIN PRIVATE KEY")) curNames = PrivateNames;
        else if (header.Contains("RSA PRIVATE KEY")) curNames = PrivateNames[1..];
        else curNames = PublicNames;
        intIdx = 0;

        Console.WriteLine("\nCấu trúc ASN.1 DER (mỗi dòng: byte tag + byte độ dài, rồi ý nghĩa):");
        ParseDer(der, 0, der.Length, 0);
    }

    static void ParseDer(byte[] d, int start, int end, int depth)
    {
        int pos = start;
        string ind = new string(' ', depth * 2);
        while (pos < end)
        {
            byte tag = d[pos++];
            int len = d[pos++];
            int hdr = 2;
            if ((len & 0x80) != 0)                 // dạng độ dài dài: 0x82 -> 2 byte tiếp theo là độ dài
            {
                int n = len & 0x7F;
                len = 0;
                for (int i = 0; i < n; i++) len = (len << 8) | d[pos++];
                hdr = 2 + n;
            }
            string hdrHex = Convert.ToHexString(d, pos - hdr, hdr);

            switch (tag)
            {
                case 0x30:
                    Console.WriteLine($"{ind}{hdrHex}  SEQUENCE, nội dung {len} byte");
                    ParseDer(d, pos, pos + len, depth + 1);
                    break;
                case 0x02:
                {
                    string nm = intIdx < curNames.Length ? curNames[intIdx] : "INTEGER";
                    intIdx++;
                    string show = Convert.ToHexString(d, pos, Math.Min(len, 8)) + (len > 8 ? "..." : "");
                    string lead = (len > 1 && d[pos] == 0) ? " [byte 00 đầu để số là số dương]" : "";
                    Console.WriteLine($"{ind}{hdrHex}  INTEGER {nm}, {len} byte: {show}{lead}");
                    break;
                }
                case 0x03:
                    Console.WriteLine($"{ind}{hdrHex}  BIT STRING, {len} byte (byte đầu = số bit đệm = {d[pos]})");
                    if (len > 1 && d[pos + 1] == 0x30) ParseDer(d, pos + 1, pos + len, depth + 1);
                    break;
                case 0x04:
                    Console.WriteLine($"{ind}{hdrHex}  OCTET STRING, {len} byte (bọc khoá RSA bên trong)");
                    if (len > 0 && d[pos] == 0x30) ParseDer(d, pos, pos + len, depth + 1);
                    break;
                case 0x05:
                    Console.WriteLine($"{ind}{hdrHex}  NULL (không có tham số)");
                    break;
                case 0x06:
                {
                    string oid = DecodeOid(d, pos, len);
                    string name = oid == "1.2.840.113549.1.1.1" ? " = rsaEncryption" : "";
                    Console.WriteLine($"{ind}{hdrHex}  OBJECT IDENTIFIER {oid}{name}");
                    break;
                }
                default:
                    Console.WriteLine($"{ind}{hdrHex}  (tag 0x{tag:X2}), {len} byte");
                    break;
            }
            pos += len;
        }
    }

    static string DecodeOid(byte[] d, int pos, int len)
    {
        var sb = new StringBuilder();
        sb.Append(d[pos] / 40).Append('.').Append(d[pos] % 40);
        long v = 0;
        for (int i = 1; i < len; i++)
        {
            v = (v << 7) | (uint)(d[pos + i] & 0x7F);
            if ((d[pos + i] & 0x80) == 0) { sb.Append('.').Append(v); v = 0; }
        }
        return sb.ToString();
    }

    // ---------- 3. Import PEM ----------
    static void ImportPem()
    {
        string path = Ask("File PEM (public hoặc private): ", "private.pem");
        string text = File.ReadAllText(path);
        bool isPriv = text.Contains("PRIVATE KEY");
        using var r = RSA.Create();
        r.ImportFromPem(text);
        var prm = r.ExportParameters(isPriv);

        Console.WriteLine($"\nLoại khoá: {(isPriv ? "BÍ MẬT (private)" : "CÔNG KHAI (public)")}, modulus {r.KeySize} bit");
        Console.WriteLine($"e = {ToBig(prm.Exponent!)}");
        Console.WriteLine($"n ({prm.Modulus!.Length} byte, hex):\n{Convert.ToHexString(prm.Modulus)}");
        Console.WriteLine($"n (thập phân):\n{ToBig(prm.Modulus)}");
        if (isPriv)
        {
            Console.WriteLine($"\nd ({prm.D!.Length} byte, hex):\n{Convert.ToHexString(prm.D)}");
            Console.WriteLine($"\np (hex):\n{Convert.ToHexString(prm.P!)}");
            Console.WriteLine($"\nq (hex):\n{Convert.ToHexString(prm.Q!)}");
            Console.WriteLine($"\ndP (hex):\n{Convert.ToHexString(prm.DP!)}");
            Console.WriteLine($"\ndQ (hex):\n{Convert.ToHexString(prm.DQ!)}");
            Console.WriteLine($"\nqInv (hex):\n{Convert.ToHexString(prm.InverseQ!)}");
            var check = ToBig(prm.P!) * ToBig(prm.Q!) == ToBig(prm.Modulus);
            Console.WriteLine($"\nKiểm tra p x q == n : {check}");
        }
    }

    // ---------- 4. Mã hoá ----------
    static void EncryptRsa()
    {
        string pubPath = Ask("File public key: ", "public.pem");
        using var r = LoadRsa(pubPath);
        int max = r.KeySize / 8 - 2 * 32 - 2;   // OAEP-SHA256
        Console.WriteLine($"Khoá {r.KeySize} bit, OAEP-SHA256: tối đa {max} byte dữ liệu.");

        string b64in = Ask("Dữ liệu Base64 (Enter = sinh khoá AES-256 ngẫu nhiên, 32 byte): ");
        byte[] data = b64in == "" ? RandomNumberGenerator.GetBytes(32) : Convert.FromBase64String(b64in);
        if (data.Length > max) throw new Exception($"Dữ liệu {data.Length} byte vượt quá giới hạn {max} byte.");

        string srcPath = Ask("File lưu dữ liệu gốc (Base64): ", "source.b64");
        File.WriteAllText(srcPath, Convert.ToBase64String(data));

        byte[] cipher = r.Encrypt(data, OAEP);
        string outPath = Ask("File lưu bản mã (Base64): ", "cipher.b64");
        File.WriteAllText(outPath, Convert.ToBase64String(cipher));

        Console.WriteLine($"Dữ liệu gốc: {data.Length} byte -> bản mã: {cipher.Length} byte");
        Console.WriteLine("Base64 bản mã: " + Convert.ToBase64String(cipher));
    }

    // ---------- 5. Giải mã ----------
    static void DecryptRsa()
    {
        string privPath = Ask("File private key: ", "private.pem");
        string cipherPath = Ask("File bản mã (Base64): ", "cipher.b64");
        string srcPath = Ask("File dữ liệu gốc để so sánh (Base64): ", "source.b64");

        using var r = LoadRsa(privPath);
        byte[] cipher = Convert.FromBase64String(File.ReadAllText(cipherPath).Trim());
        byte[] plain = r.Decrypt(cipher, OAEP);
        byte[] src = Convert.FromBase64String(File.ReadAllText(srcPath).Trim());

        Console.WriteLine("Kết quả giải mã (Base64): " + Convert.ToBase64String(plain));
        Console.WriteLine("Dữ liệu gốc     (Base64): " + Convert.ToBase64String(src));
        Console.WriteLine(plain.AsSpan().SequenceEqual(src) ? "=> GIỐNG NHAU từng byte" : "=> KHÁC NHAU");
    }

    // ---------- 6. Đo tốc độ ----------
    static void Benchmark()
    {
        int n = int.Parse(Ask("Số lần lặp mỗi phép đo: ", "300"));
        byte[] data = RandomNumberGenerator.GetBytes(32);

        Console.WriteLine("\n| Độ dài n | Sinh khoá (ms) | Mã hoá, khoá công khai (ms/lần) | Giải mã, khoá bí mật (ms/lần) | Tỉ lệ giải/mã |");
        Console.WriteLine("|---|---|---|---|---|");
        foreach (int bits in new[] { 1024, 2048, 3072, 4096 })
        {
            var sw = Stopwatch.StartNew();
            using var r = RSA.Create(bits);
            double tGen = sw.Elapsed.TotalMilliseconds;

            byte[] c = r.Encrypt(data, OAEP);      // chạy nóng
            r.Decrypt(c, OAEP);

            sw.Restart();
            for (int i = 0; i < n; i++) r.Encrypt(data, OAEP);
            double tEnc = sw.Elapsed.TotalMilliseconds / n;

            sw.Restart();
            for (int i = 0; i < n; i++) r.Decrypt(c, OAEP);
            double tDec = sw.Elapsed.TotalMilliseconds / n;

            Console.WriteLine($"| {bits} bit | {tGen:0.0} | {tEnc:0.000} | {tDec:0.000} | {tDec / tEnc:0.0} lần |");
        }
        Console.WriteLine("\n(e = 65537 chỉ 17 bit, d dài gần bằng n nên phép mũ bằng d chậm hơn nhiều)");
    }

    // ---------- 7. SHA-256 ----------
    static int BitDiff(byte[] a, byte[] b)
    {
        int c = 0;
        for (int i = 0; i < a.Length; i++) c += BitOperations.PopCount((uint)(a[i] ^ b[i]));
        return c;
    }

    static void Sha256Demo()
    {
        string s1 = Ask("Chuỗi 1: ", "hello");
        string s2 = Ask("Chuỗi 2 (sửa rất ít so với chuỗi 1): ", "hellp");
        byte[] h1 = SHA256.HashData(Encoding.UTF8.GetBytes(s1));
        byte[] h2 = SHA256.HashData(Encoding.UTF8.GetBytes(s2));

        Console.WriteLine($"\nSHA-256(\"{s1}\") = {Convert.ToHexString(h1)}");
        Console.WriteLine($"SHA-256(\"{s2}\") = {Convert.ToHexString(h2)}");
        int diff = BitDiff(h1, h2);
        Console.WriteLine($"Số bit khác nhau: {diff}/256 ({diff / 2.56:0.0}%)");

        // Lật từng bit của chuỗi 1, đo mức thay đổi của hash
        byte[] data = Encoding.UTF8.GetBytes(s1);
        if (data.Length == 0) return;
        int total = 0, min = int.MaxValue, max = 0, cnt = data.Length * 8;
        for (int i = 0; i < cnt; i++)
        {
            byte[] m = (byte[])data.Clone();
            m[i / 8] ^= (byte)(1 << (i % 8));
            int dd = BitDiff(h1, SHA256.HashData(m));
            total += dd; min = Math.Min(min, dd); max = Math.Max(max, dd);
        }
        Console.WriteLine($"\nLật lần lượt từng bit của chuỗi 1 ({cnt} phép thử, mỗi lần đổi đúng 1 bit đầu vào):");
        Console.WriteLine($"  Số bit hash thay đổi: trung bình {(double)total / cnt:0.0}/256 ({total / (cnt * 2.56):0.0}%), nhỏ nhất {min}, lớn nhất {max}");
    }

    // ---------- 8. Chữ ký số ----------
    static string Short(byte[] b) => Convert.ToBase64String(b)[..28] + "...";

    static void SignDemo()
    {
        string privPath = Ask("File private key: ", "private.pem");
        string pubPath = Ask("File public key: ", "public.pem");
        string msg = Ask("Dữ liệu cần ký: ", "Hop dong so 01");
        using var priv = LoadRsa(privPath);
        using var pub = LoadRsa(pubPath);
        byte[] data = Encoding.UTF8.GetBytes(msg);
        int n = 5;

        Console.WriteLine("\n[A] PKCS#1 v1.5: ký 5 lần cùng dữ liệu, cùng private key");
        var setA = new HashSet<string>();
        for (int i = 1; i <= n; i++)
        {
            byte[] sig = priv.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            setA.Add(Convert.ToBase64String(sig));
            bool ok = pub.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Console.WriteLine($"  lần {i}: {Short(sig)}  verify = {ok}");
        }
        Console.WriteLine($"  => {setA.Count} chữ ký khác nhau / {n} lần");

        Console.WriteLine("\n[B] RSA-PSS (salt ngẫu nhiên): ký 5 lần cùng dữ liệu, cùng private key");
        var setB = new HashSet<string>();
        byte[] lastPss = Array.Empty<byte>();
        for (int i = 1; i <= n; i++)
        {
            byte[] sig = priv.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            setB.Add(Convert.ToBase64String(sig));
            lastPss = sig;
            bool ok = pub.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            Console.WriteLine($"  lần {i}: {Short(sig)}  verify = {ok}");
        }
        Console.WriteLine($"  => {setB.Count} chữ ký khác nhau / {n} lần");

        Console.WriteLine("\n[C] PKCS#1 v1.5 + nonce ngẫu nhiên + timestamp: ký (dữ liệu || nonce || timestamp)");
        var setC = new HashSet<string>();
        for (int i = 1; i <= n; i++)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(16);
            byte[] ts = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            byte[] pack = data.Concat(nonce).Concat(ts).ToArray();
            byte[] sig = priv.SignData(pack, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            setC.Add(Convert.ToBase64String(sig));
            bool ok = pub.VerifyData(pack, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Console.WriteLine($"  lần {i}: nonce={Convert.ToHexString(nonce)[..8]}...  {Short(sig)}  verify = {ok}");
        }
        Console.WriteLine($"  => {setC.Count} chữ ký khác nhau / {n} lần");

        byte[] tampered = Encoding.UTF8.GetBytes(msg + "!");
        bool bad = pub.VerifyData(tampered, lastPss, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        Console.WriteLine($"\nSửa dữ liệu gốc (thêm 1 ký tự) rồi verify chữ ký PSS cũ: {bad}  (phải là False)");
    }
}