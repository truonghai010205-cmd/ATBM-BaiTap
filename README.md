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
(Chèn ảnh chụp kết quả mục 2 và giải thích ngắn.)

![Xem public.pem](images/bai2-pem-public.png)
![Xem private.pem](images/bai2-pem-private.png)

- Dòng `-----BEGIN ... -----` và `-----END ... -----` đánh dấu loại dữ liệu; phần giữa là Base64 của cấu trúc nhị phân DER (ASN.1).
- Tag `30` là SEQUENCE, `02` là INTEGER, `03` là BIT STRING, `04` là OCTET STRING, `05` là NULL, `06` là OID. Byte ngay sau tag là độ dài; nếu có dạng `82` thì hai byte tiếp theo mới là độ dài thực.
- Public key gồm định danh thuật toán `rsaEncryption` (OID 1.2.840.113549.1.1.1) và BIT STRING chứa n và e. [Điền: e = ..., n dài ... byte]
- Private key chứa version, n, e, d, p, q, dP, dQ, qInv. [Điền: mô tả các số thấy trong ảnh]

### Kết quả mã hoá và giải mã
![Mã hoá](images/bai2-ma-hoa.png)
![Giải mã](images/bai2-giai-ma.png)

Dữ liệu thử: khoá AES-256 ngẫu nhiên (32 byte). Kết quả giải mã [điền: giống / khác] dữ liệu gốc từng byte. Bản mã dài [điền] byte.

### Tốc độ khoá công khai và khoá bí mật
Cấu hình máy: Intel Core i5-12450HX, RAM khoảng 24 GB, SSD Micron, Windows, .NET 10, chế độ Release. Mỗi phép đo lặp [điền số] lần.

| Độ dài n | Sinh khoá (ms) | Mã hoá, khoá công khai (ms/lần) | Giải mã, khoá bí mật (ms/lần) | Tỉ lệ giải/mã |
|---|---|---|---|---|
| 1024 bit | | | | |
| 2048 bit | | | | |
| 3072 bit | | | | |
| 4096 bit | | | | |

Nhận xét: [điền dựa trên bảng: phép nào nhanh hơn và gấp bao nhiêu lần; thời gian thay đổi thế nào khi tăng độ dài khoá]. Nguyên nhân: e = 65537 chỉ có 17 bit nên phép mũ modulo rất nhanh, còn d dài xấp xỉ n nên phép mũ chậm hơn nhiều.

### SHA-256 và hiệu ứng thác lũ

| Chuỗi 1 | Chuỗi 2 | Số bit hash khác nhau | Tỉ lệ |
|---|---|---|---|
| hello | hellp | [điền]/256 | [điền]% |
| [thêm cặp khác] | | | |

Khi lật lần lượt từng bit của đầu vào, số bit hash thay đổi trung bình là [điền]/256 ([điền]%). Chỉ cần đổi rất ít đầu vào, giá trị băm thay đổi hoàn toàn: đây là hiệu ứng thác lũ.

### Chữ ký số và đề xuất chữ ký khác nhau mỗi lần ký

![Kết quả chữ ký số](images/bai2-chu-ky.png)

| Phương pháp | Ký 5 lần cùng dữ liệu, cùng private key | Verify |
|---|---|---|
| PKCS#1 v1.5 | [điền] chữ ký khác nhau / 5 | [điền] |
| RSA-PSS | [điền] chữ ký khác nhau / 5 | [điền] |
| PKCS#1 v1.5 + nonce + timestamp | [điền] chữ ký khác nhau / 5 | [điền] |

**Đề xuất:**
1. **RSA-PSS (khuyến nghị):** mỗi lần ký sinh một salt ngẫu nhiên đưa vào quá trình padding, nên chữ ký khác nhau nhưng người nhận vẫn xác minh được bằng cùng public key mà không cần nhận thêm dữ liệu. Đây là chuẩn (RFC 8017).
2. **Thêm nonce và timestamp:** ký (dữ liệu ‖ nonce ‖ timestamp). Chữ ký khác nhau vì giá trị băm khác nhau (hiệu ứng thác lũ của SHA-256). Người nhận phải được gửi kèm nonce và timestamp để xác minh. Ưu điểm là chống tấn công phát lại; nhược điểm là phải truyền thêm dữ liệu.

Khi sửa dữ liệu gốc dù chỉ một ký tự, chữ ký cũ không còn xác minh được (kết quả [điền]).

### Kết luận
- [Điền 3 đến 5 ý dựa trên số liệu của bạn: tốc độ public so với private, ảnh hưởng của độ dài khoá, hiệu ứng thác lũ, so sánh các phương pháp ký.]
