# 簡易圖書管理系統（WinForms + EF Core + SQL Server）

## 建置步驟
1. **先在 SQL Server 建立資料庫與資料表/儲存程序**：
   - 打開 SSMS，執行你先前拿到的 `LibraryDb` SQL 腳本（建立 Book/Author/BookAuthor/Copy/Member/Loan 與 `sp_BorrowCopy`、`sp_ReturnCopy`）。
2. **開啟專案**：
   - 使用 Visual Studio 2022/2025，開啟 `LibraryWinForms.csproj`。
   - 第一次開啟會自動還原 NuGet 套件（EF Core）。
3. **設定連線字串**：
   - 在 `Data/LibraryContext.cs` 中調整 `UseSqlServer("Server=.;Database=LibraryDb;...")` 為你的環境。
4. **執行**：
   - F5 直接執行。
5. **示範操作**：
   - 「書籍/館藏」分頁：可搜尋書籍、新增書籍、針對選中書籍新增館藏。
   - 「會員」分頁：可搜尋、新增會員。
   - 「借出/歸還」分頁：輸入 `CopyId` 與 `MemberId` → 借出；輸入 `LoanId` → 歸還；亦可查某會員未歸還清單。

> 狀態碼：Copy.Status = 0 可借、1 外借中、2 遺失、3 毀損。  
> 借出/歸還直接呼叫儲存程序，具事務安全與逾期罰款計算（預設 10 元/天）。

## 延伸
- 若要用條碼掃描器，只需讓焦點在「欄位」上，掃描器當成鍵盤輸入即可。
- 想做權限/登入，可在 DB 新增 `User`、`Role` 等表並於 WinForms 加入登入畫面。
