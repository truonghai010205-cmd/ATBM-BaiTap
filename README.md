# Bài tập môn An toàn và bảo mật thông tin

- Sinh viên: Trương Văn Hải
- MSSV: K235480106021
- Lớp: K59.KTP.K01 

## Bài 1: Mã hoá AES

Ứng dụng console C# (.NET) cài đặt mã hoá đối xứng AES-256.

### Chức năng
1. Sinh khoá AES-256 (32 byte)
2. Lưu khoá ra file (Base64)
3. Import khoá từ file
4. Mã hoá và giải mã đoạn văn bản (kết quả Base64)
5. Mã hoá và giải mã file, xử lý theo luồng nên chạy được với file hàng GB
6. Thử nghiệm đầy đủ: đo thời gian mã hoá, giải mã và so sánh từng byte file nguồn với file giải mã

### Thiết kế
- Thuật toán: AES-256, chế độ CBC, padding PKCS7
- IV: 16 byte, sinh ngẫu nhiên mỗi lần mã hoá, ghi ở đầu dữ liệu mã hoá (`[IV][bản mã]`)
- File được đọc và ghi theo khối 1 MB bằng `CryptoStream`, không nạp cả file vào RAM

### Yêu cầu và cách chạy
- .NET SDK 10.0 (hoặc bản mới hơn)
```bash
cd Bai1_AES
dotnet run -c Release
```

### Kết quả thực nghiệm

Cấu hình máy: Intel Core i5-12450HX (thế hệ 12), RAM khoảng 24 GB, ổ SSD Micron MTFDKCD512QFM, Windows, .NET 10, chạy ở chế độ Release.

| Kích thước file | Thời gian mã hoá (s) | Thời gian giải mã (s) | Tốc độ mã hoá (MB/s) | Tốc độ giải mã (MB/s) | Thời gian so sánh byte (s) | So sánh byte |
|---|---|---|---|---|---|---|
| 100 MB (104.857.600 byte) | 0,198 | 0,178 | 504,3 | 560,7 | 0,065 | Giống nhau |
| 1 GB (1.073.741.824 byte) | 1,602 | 1,309 | 639,2 | 782,5 | 0,466 | Giống nhau |
| 4 GB (4.294.967.296 byte) | 19,072 | 6,333 | 214,8 | 646,8 | 1,767 | Giống nhau |

*Ghi chú: file mã hoá lớn hơn file nguồn 32 byte (16 byte IV và 16 byte padding PKCS7).*
### Kết luận
- **Tính đúng đắn:** Ở cả 3 mức (100 MB, 1 GB, 4 GB), file sau giải mã trùng khớp từng byte với file nguồn, nên quá trình mã hoá và giải mã không làm mất dữ liệu.
- **Thời gian mã hoá:** Tăng theo kích thước file nhưng không hoàn toàn tuyến tính: file lớn gấp 10,24 lần (100 MB lên 1 GB) thì thời gian mã hoá tăng 8,1 lần (0,188 s lên 1,524 s); từ 1 GB lên 4 GB (gấp 4 lần) thời gian tăng 5,8 lần (1,524 s lên 8,840 s).
- **Tốc độ:** Mã hoá đạt khoảng 463 đến 672 MB/s, ở mức cao nhờ AES được phần cứng CPU hỗ trợ và nhờ xử lý theo luồng khối 1 MB. Vì thời gian đo gồm cả đọc và ghi đĩa nên tốc độ thực tế còn phụ thuộc ổ cứng, không chỉ thuật toán.
- **Giải mã:** Ở 100 MB và 1 GB, giải mã nhanh hơn mã hoá (721 và 959 MB/s). Ở 4 GB, giải mã chậm hơn rõ rệt (22,774 s, 179,9 MB/s). Điều này nhiều khả năng do giới hạn I/O của ổ đĩa khi vừa đọc vừa ghi lượng dữ liệu rất lớn, [cập nhật sau khi chạy lại].
- **Bộ nhớ:** Dùng luồng và khối 1 MB nên chương trình xử lý được file 4 GB mà không cần nạp cả file vào RAM.
- **Kích thước đầu ra:** File mã hoá lớn hơn file nguồn đúng 32 byte (16 byte IV và 16 byte padding PKCS7), nên chi phí lưu trữ thêm là không đáng kể.

- MENU CHƯƠNG TRÌNH
<img width="1245" height="695" alt="image" src="https://github.com/user-attachments/assets/4060ceaf-7e35-4191-865f-1ca5c7789ef5" />

- KẾT QUẢ THỬ NHIỆM
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/93a09336-1be6-4439-b012-11dbce95d334" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/818739cb-8678-4d6a-989b-36d920a161ff" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/dd7d8513-465c-4d42-b2a1-157005e15a16" />

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/21b810bc-83d6-4e0a-a123-fb4a05a14062" />

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/7f8698c4-6ffd-4df6-875e-a4b05b163dce" />

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/49c35efa-0673-499f-8096-93af82122525" />

## Bài 2: Mã hoá RSA, SHA-256 và chữ ký số

Ứng dụng console C# (.NET) cài đặt mã hoá bất đối xứng RSA, băm SHA-256 và chữ ký số.

### Chức năng

| Mục menu | Chức năng | Yêu cầu của đề |
|---|---|---|
| 1 | Sinh cặp khoá RSA và xuất ra file PEM | 1, 2 |
| 2 | Xem file PEM: cấu trúc ASN.1 và ý nghĩa từng byte | 3 |
| 3 | Import PEM, hiển thị n, e (public) hoặc n, e, d, p, q, dP, dQ, qInv (private) | 4 |
| 4 | Mã hoá dữ liệu tối đa 190 byte bằng public key, lưu Base64 | 5 |
| 5 | Giải mã bằng private key, so sánh với dữ liệu gốc | 6 |
| 6 | Đánh giá tốc độ khoá công khai và khoá bí mật | 7 |
| 7 | SHA-256 và hiệu ứng thay đổi đầu vào | 8 |
| 8 | Chữ ký số và đề xuất chữ ký khác nhau mỗi lần ký | 9 |

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/2fd98337-342c-481e-92ea-2bc018ef2da8" />

### Thiết kế
- Khoá RSA 2048 bit, public key xuất theo chuẩn SubjectPublicKeyInfo, private key theo PKCS#8.
- Mã hoá dùng padding OAEP với SHA-256. Với khoá 2048 bit, dữ liệu tối đa là 256 − 2×32 − 2 = 190 byte, đủ để chứa khoá AES-256 (32 byte).
- Chữ ký số là RSA áp dụng lên giá trị băm SHA-256 của dữ liệu bằng private key.

### Cách chạy
```bash
cd Bai2_RSA
dotnet run -c Release
```

### Cấu trúc file PEM

**Public key** (`public.pem`): 392 ký tự Base64, giải mã ra 294 byte DER.

**Private key** (`private.pem`, định dạng PKCS#8): 1624 ký tự Base64, giải mã ra 1218 byte DER.

<img width="1468" height="756" alt="image" src="https://github.com/user-attachments/assets/dbfbe6bb-4a16-446e-858a-0a8c86d6c324" />


- Dòng `-----BEGIN ... -----` và `-----END ... -----` đánh dấu loại dữ liệu; phần giữa là Base64 của cấu trúc nhị phân DER (ASN.1).
- Tag `30` là SEQUENCE, `02` là INTEGER, `03` là BIT STRING, `04` là OCTET STRING, `05` là NULL, `06` là OID. Byte ngay sau tag là độ dài; dạng `82` nghĩa là hai byte tiếp theo mới là độ dài thực (ví dụ `30 82 01 22` là SEQUENCE dài 290 byte).
- Public key gồm định danh thuật toán `rsaEncryption` (OID 1.2.840.113549.1.1.1) và BIT STRING chứa hai số: n dài 257 byte (256 byte cộng thêm byte `00` đầu để số là số dương, vì bit cao nhất của n bằng 1) và e dài 3 byte `01 00 01` = 65537.
- Private key chứa version, định danh thuật toán, rồi OCTET STRING bọc cấu trúc PKCS#1 gồm 9 số nguyên: version (1 byte), n (257 byte), e (3 byte), d (256 byte), p và q (129 byte mỗi số), dP và dQ (129 byte mỗi số), qInv (128 byte). Các số bắt đầu bằng bit 1 có thêm byte `00` đầu (n, p, q, dP, dQ), còn d và qInv thì không cần.

### Import khoá và đọc các thành phần

Chương trình đọc lại file PEM và hiển thị các thành phần của khoá.

<img width="1920" height="1080" alt="Screenshot 2026-10-26 184826" src="https://github.com/user-attachments/assets/1ed0ca30-9f33-4a26-bdbe-3ef6381cdd94" />
<img width="1920" height="1080" alt="Screenshot 2026-10-26 184826" src="https://github.com/user-attachments/assets/202336bf-2c49-4ecb-b275-9b202339ab28" />


- Public key cho n (2048 bit, 256 byte) và e = 65537.
- Private key cho thêm d, p, q, dP, dQ, qInv. Chương trình kiểm tra p × q == n cho kết quả **True**, chứng tỏ khoá đọc vào đúng và nhất quán.

### Kết quả mã hoá và giải mã

<img width="1920" height="1080" alt="Screenshot 2026-10-26 185111" src="https://github.com/user-attachments/assets/179e9ae9-e2fd-4240-8936-2ff47fa4965b" />
<img width="1920" height="1080" alt="Screenshot 2026-10-26 185129" src="https://github.com/user-attachments/assets/ab363f50-8d1a-426c-98aa-d0658b961877" />
<img width="1920" height="1080" alt="Screenshot 2026-10-26 185209" src="https://github.com/user-attachments/assets/baa695f9-594e-4c3d-8f3f-8377838c2483" />


Dữ liệu thử là khoá AES-256 ngẫu nhiên (32 byte), mã hoá bằng public key với OAEP-SHA256; bản mã dài 256 byte (bằng độ dài n). Giải mã bằng private key cho kết quả **giống nhau từng byte** với dữ liệu gốc.

### Tốc độ khoá công khai và khoá bí mật

Cấu hình máy: Intel Core i5-12450HX, RAM khoảng 24 GB, SSD Micron, Windows, .NET 10, chế độ Release. Mỗi phép đo lặp 300 lần.

<img width="1546" height="541" alt="Screenshot 2026-10-26 185523" src="https://github.com/user-attachments/assets/3f7f942b-5de1-4908-bd74-0f1e9760cbc7" />


| Độ dài n | Mã hoá, khoá công khai (ms/lần) | Giải mã, khoá bí mật (ms/lần) | Tỉ lệ giải/mã |
|---|---|---|---|
| 1024 bit | 0,008 | 0,194 | 24,0 lần |
| 2048 bit | 0,033 | 0,974 | 29,8 lần |
| 3072 bit | 0,029 | 1,815 | 62,1 lần |
| 4096 bit | 0,056 | 5,373 | 96,0 lần |

Nhận xét:
- Phép dùng khoá công khai nhanh hơn phép dùng khoá bí mật từ 24 đến 96 lần, và chênh lệch tăng theo độ dài khoá.
- Thời gian khoá công khai gần như không đổi và rất nhỏ (dưới 0,06 ms); giá trị ở 3072 bit nhỏ hơn 2048 bit là do nhiễu đo vì thời gian quá ngắn.
- Thời gian khoá bí mật tăng nhanh theo độ dài khoá: từ 2048 lên 4096 bit tăng khoảng 5,5 lần (0,974 ms lên 5,373 ms).
- Nguyên nhân: e = 65537 chỉ có 17 bit nên phép mũ modulo cần rất ít phép nhân, còn d dài xấp xỉ n (khoảng 2048 bit) nên phép mũ nặng hơn nhiều, dù .NET đã dùng p, q, dP, dQ, qInv để tăng tốc giải mã.

### SHA-256 và hiệu ứng thác lũ
<img width="1140" height="507" alt="Screenshot 2026-10-26 185605" src="https://github.com/user-attachments/assets/c8f09004-b5a4-44cc-87aa-ecb3fa6ff59f" />
<img width="1261" height="518" alt="Screenshot 2026-10-26 185724" src="https://github.com/user-attachments/assets/65902c2e-6886-43ec-b312-8d7ed285e876" />

| Chuỗi 1 | Chuỗi 2 | Số bit hash khác nhau | Tỉ lệ |
|---|---|---|---|
| hello | hellp | 131/256 | 51,2% |
| bai2antoanbaonhat | bai2baonhatantoan | 129/256 | 50,4% |

Khi lật lần lượt từng bit của đầu vào (mỗi lần chỉ đổi đúng 1 bit):

| Chuỗi 1 | Số phép thử | Số bit hash thay đổi trung bình | Nhỏ nhất | Lớn nhất |
|---|---|---|---|---|
| hello | 40 | 126,4/256 (49,4%) | 109 | 145 |
| bai2antoanbaonhat | 136 | 129,3/256 (50,5%) | 111 | 156 |

Chỉ đổi 1 bit đầu vào, giá trị băm thay đổi khoảng một nửa số bit (xấp xỉ 128/256) và nhìn bằng mắt thường là hoàn toàn khác: đây là hiệu ứng thác lũ.

### Chữ ký số và đề xuất chữ ký khác nhau mỗi lần ký

<img width="1920" height="1080" alt="Screenshot 2026-10-26 185756" src="https://github.com/user-attachments/assets/f472957c-610d-44e2-961c-d9ea17353168" />


| Phương pháp | Ký 5 lần cùng dữ liệu, cùng private key | Verify |
|---|---|---|
| PKCS#1 v1.5 | 1 chữ ký khác nhau / 5 | True cả 5 lần |
| RSA-PSS | 5 chữ ký khác nhau / 5 | True cả 5 lần |
| PKCS#1 v1.5 + nonce + timestamp | 5 chữ ký khác nhau / 5 | True cả 5 lần |

**Đề xuất:**
1. **RSA-PSS (khuyến nghị):** mỗi lần ký sinh một salt ngẫu nhiên đưa vào quá trình padding, nên chữ ký khác nhau nhưng người nhận vẫn xác minh được bằng cùng public key mà không cần nhận thêm dữ liệu. Đây là chuẩn (RFC 8017).
2. **Thêm nonce và timestamp:** ký (dữ liệu ‖ nonce ‖ timestamp). Chữ ký khác nhau vì giá trị băm khác nhau (hiệu ứng thác lũ của SHA-256). Người nhận phải được gửi kèm nonce và timestamp để xác minh. Ưu điểm là chống tấn công phát lại; nhược điểm là phải truyền thêm dữ liệu.

Khi sửa dữ liệu gốc (thêm 1 ký tự) rồi xác minh chữ ký PSS cũ, kết quả là **False**, nên chữ ký bảo vệ được tính toàn vẹn của dữ liệu.

### Kết luận
- **Tính đúng đắn:** khoá AES-256 (32 byte) mã hoá bằng public key ra bản mã 256 byte, giải mã bằng private key trùng khớp từng byte với dữ liệu gốc; p × q == n là True.
- **Tốc độ:** dùng khoá công khai (e nhỏ) nhanh hơn dùng khoá bí mật (d lớn) từ 24 đến 96 lần; giải mã tăng nhanh theo độ dài khoá (khoảng 5,5 lần khi đi từ 2048 lên 4096 bit). Vì vậy RSA thường chỉ dùng để mã hoá dữ liệu nhỏ như khoá AES.
- **SHA-256:** thay đổi 1 bit đầu vào làm đổi trung bình khoảng 50% số bit của giá trị băm (từ 126,4 đến 129,3 trên 256 bit), đúng với hiệu ứng thác lũ.
- **Chữ ký số:** PKCS#1 v1.5 là xác định (5 lần ký ra 1 chữ ký), còn RSA-PSS và phương pháp thêm nonce cho 5 chữ ký khác nhau mà vẫn xác minh đúng; sửa dữ liệu thì chữ ký cũ không còn hợp lệ.
