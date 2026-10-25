using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static byte[]? key;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        while (true)
        {
            Console.WriteLine("\n===== MÃ HOÁ AES-256 (CBC, PKCS7) =====");
            Console.WriteLine(key == null ? "Khoá hiện tại: (chưa có)" : "Khoá hiện tại: đã nạp (32 byte)");
            Console.WriteLine("1. Sinh khoá AES");
            Console.WriteLine("2. Lưu khoá ra file");
            Console.WriteLine("3. Import khoá từ file");
            Console.WriteLine("4. Mã hoá đoạn văn bản");
            Console.WriteLine("5. Giải mã đoạn văn bản");
            Console.WriteLine("6. Mã hoá file");
            Console.WriteLine("7. Giải mã file");
            Console.WriteLine("8. Tạo file thử nghiệm (dung lượng tuỳ chọn)");
            Console.WriteLine("9. Thử nghiệm đầy đủ: mã hoá + giải mã + đo thời gian + so sánh byte");
            Console.WriteLine("0. Thoát");
            Console.Write("Chọn: ");
            string? c = Console.ReadLine()?.Trim();

            try
            {
                switch (c)
                {
                    case "1": GenKey(); break;
                    case "2": SaveKey(); break;
                    case "3": LoadKey(); break;
                    case "4": EncryptText(); break;
                    case "5": DecryptText(); break;
                    case "6": MenuEncryptFile(); break;
                    case "7": MenuDecryptFile(); break;
                    case "8": MenuCreateTestFile(); break;
                    case "9": FullTest(); break;
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

    // ---------- Tiện ích nhập liệu ----------
    static string Ask(string prompt)
    {
        Console.Write(prompt);
        return (Console.ReadLine() ?? "").Trim().Trim('"');   // bỏ dấu " khi kéo thả đường dẫn
    }

    static byte[] RequireKey()
    {
        if (key == null) throw new InvalidOperationException("Chưa có khoá. Hãy dùng chức năng 1 hoặc 3 trước.");
        return key;
    }

    static string FormatSize(long bytes)
    {
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v:0.##} {u[i]}";
    }

    // ---------- 1, 2, 3: Quản lý khoá ----------
    static void GenKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        key = aes.Key;
        Console.WriteLine("Đã sinh khoá AES-256 (32 byte). Base64:");
        Console.WriteLine(Convert.ToBase64String(key));
    }

    static void SaveKey()
    {
        byte[] k = RequireKey();
        string path = Ask("Nhập đường dẫn file lưu khoá (vd: aes.key): ");
        File.WriteAllText(path, Convert.ToBase64String(k));
        Console.WriteLine("Đã lưu khoá vào: " + Path.GetFullPath(path));
    }

    static void LoadKey()
    {
        string path = Ask("Nhập đường dẫn file khoá: ");
        byte[] k = Convert.FromBase64String(File.ReadAllText(path).Trim());
        if (k.Length != 32) throw new Exception($"Khoá phải dài 32 byte (AES-256), file này có {k.Length} byte.");
        key = k;
        Console.WriteLine("Đã import khoá thành công.");
    }

    // ---------- 4, 5: Văn bản ----------
    static void EncryptText()
    {
        byte[] k = RequireKey();
        Console.Write("Nhập đoạn văn bản cần mã hoá: ");
        string plain = Console.ReadLine() ?? "";

        using var aes = Aes.Create();
        aes.Key = k;
        aes.GenerateIV();                                   // IV ngẫu nhiên mỗi lần
        using var enc = aes.CreateEncryptor();
        byte[] data = Encoding.UTF8.GetBytes(plain);
        byte[] cipher = enc.TransformFinalBlock(data, 0, data.Length);
        byte[] result = aes.IV.Concat(cipher).ToArray();    // [IV 16 byte][bản mã]

        Console.WriteLine("Kết quả mã hoá (Base64):");
        Console.WriteLine(Convert.ToBase64String(result));
    }

    static void DecryptText()
    {
        byte[] k = RequireKey();
        Console.Write("Nhập chuỗi Base64 cần giải mã: ");
        byte[] all = Convert.FromBase64String((Console.ReadLine() ?? "").Trim());

        using var aes = Aes.Create();
        aes.Key = k;
        aes.IV = all[..16];
        using var dec = aes.CreateDecryptor();
        byte[] plain = dec.TransformFinalBlock(all, 16, all.Length - 16);
        Console.WriteLine("Văn bản gốc: " + Encoding.UTF8.GetString(plain));
    }

    // ---------- 6, 7: File ----------
    static void CopyWithProgress(Stream src, Stream dst, long total, string label)
    {
        byte[] buf = new byte[1 << 20];     // khối 1 MB, không đọc cả file vào RAM
        long done = 0; int n, lastPct = -1;
        while ((n = src.Read(buf, 0, buf.Length)) > 0)
        {
            dst.Write(buf, 0, n);
            done += n;
            if (total > 0)
            {
                int pct = (int)Math.Min(100, done * 100 / total);
                if (pct != lastPct) { Console.Write($"\r{label}: {pct}%   "); lastPct = pct; }
            }
        }
        Console.WriteLine();
    }

    static double EncryptFile(string inPath, string outPath, byte[] k)
    {
        var sw = Stopwatch.StartNew();
        using (var aes = Aes.Create())
        {
            aes.Key = k;
            aes.GenerateIV();
            using var fin = new FileStream(inPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
            using var fout = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
            fout.Write(aes.IV, 0, 16);                      // ghi IV vào đầu file
            using (var cs = new CryptoStream(fout, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
            {
                CopyWithProgress(fin, cs, fin.Length, "Mã hoá");
            }
        }
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }

    static double DecryptFile(string inPath, string outPath, byte[] k)
    {
        var sw = Stopwatch.StartNew();
        using (var fin = new FileStream(inPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        {
            byte[] iv = new byte[16];
            fin.ReadExactly(iv, 0, 16);
            using var aes = Aes.Create();
            aes.Key = k;
            aes.IV = iv;
            using var fout = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
            using var cs = new CryptoStream(fin, aes.CreateDecryptor(), CryptoStreamMode.Read);
            CopyWithProgress(cs, fout, fin.Length - 16, "Giải mã");
        }
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }

    static void MenuEncryptFile()
    {
        byte[] k = RequireKey();
        string src = Ask("File nguồn: ");
        string dst = Ask("File kết quả (vd: data.enc): ");
        double t = EncryptFile(src, dst, k);
        Console.WriteLine($"Xong. Thời gian mã hoá: {t:0.000} giây");
    }

    static void MenuDecryptFile()
    {
        byte[] k = RequireKey();
        string src = Ask("File đã mã hoá: ");
        string dst = Ask("File kết quả giải mã: ");
        double t = DecryptFile(src, dst, k);
        Console.WriteLine($"Xong. Thời gian giải mã: {t:0.000} giây");
    }

    // ---------- 8: Tạo file thử ----------
    static void MenuCreateTestFile()
    {
        string path = Ask("Đường dẫn file cần tạo (vd: test.bin): ");
        Console.Write("Dung lượng (MB), vd 1024 = 1 GB: ");
        long mb = long.Parse(Console.ReadLine() ?? "0");

        byte[] block = new byte[1 << 20];
        RandomNumberGenerator.Fill(block);                  // dữ liệu ngẫu nhiên 1 MB, ghi lặp lại
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
        for (long i = 0; i < mb; i++)
        {
            block[0] = (byte)i; block[1] = (byte)(i >> 8);  // đổi nhẹ để các khối không giống hệt nhau
            fs.Write(block, 0, block.Length);
        }
        Console.WriteLine($"Đã tạo {path} ({FormatSize(mb << 20)}).");
    }

    // ---------- 9: Thử nghiệm đầy đủ ----------
    static bool CompareFiles(string a, string b)
    {
        if (new FileInfo(a).Length != new FileInfo(b).Length) return false;
        using var fa = new FileStream(a, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
        using var fb = new FileStream(b, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
        byte[] ba = new byte[1 << 20], bb = new byte[1 << 20];
        int ra;
        while ((ra = fa.Read(ba, 0, ba.Length)) > 0)
        {
            fb.ReadExactly(bb, 0, ra);
            if (!ba.AsSpan(0, ra).SequenceEqual(bb.AsSpan(0, ra))) return false;
        }
        return true;
    }

    static void FullTest()
    {
        byte[] k = RequireKey();
        string src = Ask("File nguồn cần thử nghiệm: ");
        string enc = src + ".enc";
        string dec = src + ".dec";

        long size = new FileInfo(src).Length;
        double tEnc = EncryptFile(src, enc, k);
        double tDec = DecryptFile(enc, dec, k);

        Console.WriteLine("So sánh từng byte file nguồn và file giải mã...");
        var sw = Stopwatch.StartNew();
        bool same = CompareFiles(src, dec);
        sw.Stop();

        double mb = size / 1024.0 / 1024.0;
        Console.WriteLine("\n========== KẾT QUẢ ==========");
        Console.WriteLine($"Kích thước nguồn   : {FormatSize(size)} ({size:N0} byte)");
        Console.WriteLine($"Kích thước đã mã hoá: {FormatSize(new FileInfo(enc).Length)}");
        Console.WriteLine($"Thời gian mã hoá   : {tEnc:0.000} giây  ({mb / tEnc:0.0} MB/s)");
        Console.WriteLine($"Thời gian giải mã  : {tDec:0.000} giây  ({mb / tDec:0.0} MB/s)");
        Console.WriteLine($"Thời gian so sánh  : {sw.Elapsed.TotalSeconds:0.000} giây");
        Console.WriteLine($"Nguồn và giải mã   : {(same ? "GIỐNG NHAU từng byte" : "KHÁC NHAU")}");
    }
}