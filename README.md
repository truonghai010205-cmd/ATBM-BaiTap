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

### Ảnh chụp màn hình
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

[Sẽ bổ sung khi hoàn thành]

## Cấu trúc thư mục
```
ATBM/
  Bai1_AES/    # Mã nguồn bài 1
  Bai2_RSA/    # Mã nguồn bài 2
  images/      # Ảnh chụp màn hình
  README.md
```

## Ghi chú
Các file thử nghiệm dung lượng lớn (`*.bin`, `*.enc`, `*.dec`) và khoá (`*.key`, `*.pem`) không được đưa lên repo (xem `.gitignore`).
