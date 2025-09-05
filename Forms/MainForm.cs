using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing; // for Size
using LibraryWinForms.Services;
using LibraryWinForms.Models;

namespace LibraryWinForms.Forms
{
    public class MainForm : Form
    {
        private readonly LibraryService _svc = new LibraryService();

        private TabControl tabs = new TabControl() { Dock = DockStyle.Fill };

        // Books tab controls
        private TextBox tbBookSearch = new TextBox() { Width = 200 };
        private Button btnBookSearch = new Button() { Text = "搜尋" };
        private Button btnAddBook = new Button() { Text = "新增書籍" };
        private Button btnEditBook = new Button() { Text = "更新書籍" };
        private Button btnDeleteBook = new Button() { Text = "刪除書籍" };
        private DataGridView gridBooks = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
        private DataGridView gridCopies = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
        private Button btnAddCopy = new Button() { Text = "新增館藏" };

        // Members tab controls
        private TextBox tbMemberSearch = new TextBox() { Width = 200 };
        private Button btnMemberSearch = new Button() { Text = "搜尋" };
        private Button btnAddMember = new Button() { Text = "新增會員" };
        private Button btnEditMember = new Button() { Text = "更新會員" };
        private Button btnDeleteMember = new Button() { Text = "刪除會員" };
        private DataGridView gridMembers = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };

        // Borrow/Return tab controls
        private NumericUpDown numCopyId = new NumericUpDown() { Minimum = 1, Maximum = int.MaxValue, Width = 120 };
        private NumericUpDown numMemberId = new NumericUpDown() { Minimum = 1, Maximum = int.MaxValue, Width = 120 };
        private Button btnBorrow = new Button() { Text = "借出" };
        private Button btnReturn = new Button() { Text = "歸還" };
        private NumericUpDown numLoansMemberId = new NumericUpDown() { Minimum = 1, Maximum = int.MaxValue, Width = 120 };
        private Button btnLoadLoans = new Button() { Text = "載入未歸還清單" };
        private DataGridView gridLoans = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
        private NumericUpDown numBookId = new NumericUpDown() { Minimum = 1, Maximum = int.MaxValue, Width = 120 };

        public MainForm()
        {
            Text = "簡易圖書管理系統 (WinForms + SQL Server)";
            Width = 1100;
            Height = 700;
            MinimumSize = new Size(1000, 650);

            Controls.Add(tabs);
            BuildBooksTab();
            BuildMembersTab();
            BuildBorrowTab();

            Load += async (_, __) => await ReloadBooksAsync();
        }

        // ===== 共用：避免文字被切掉 =====
        private void EnsureNoTextCut(DataGridView g)
        {
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
        }

        // ===== Tabs =====
        private void BuildBooksTab()
        {
            var tab = new TabPage("書籍/館藏");
            tabs.TabPages.Add(tab);

            var split = new SplitContainer() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };

            // Panel1: 搜尋區 + gridBooks
            var panel1 = new Panel() { Dock = DockStyle.Fill };
            var top = new FlowLayoutPanel() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            top.Controls.Add(new Label() { Text = "關鍵字：", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            top.Controls.Add(tbBookSearch);
            top.Controls.Add(btnBookSearch);
            top.Controls.Add(btnAddBook);
            top.Controls.Add(btnEditBook);
            top.Controls.Add(btnDeleteBook);

            gridBooks.Dock = DockStyle.Fill;
            panel1.Controls.Add(gridBooks); // 先加 DataGridView
            panel1.Controls.Add(top);       // 再加搜尋區
            split.Panel1.Controls.Add(panel1);

            // Panel2: 新增館藏 + gridCopies
            var panel2 = new Panel() { Dock = DockStyle.Fill };
            var bottomTop = new FlowLayoutPanel() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            bottomTop.Controls.Add(btnAddCopy);
            gridCopies.Dock = DockStyle.Fill;
            panel2.Controls.Add(gridCopies);    // 先加 DataGridView
            panel2.Controls.Add(bottomTop);     // 再加新增館藏
            split.Panel2.Controls.Add(panel2);

            tab.Controls.Add(split);

            // 固定表頭高度
            gridBooks.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridBooks.ColumnHeadersHeight = 32;
            gridCopies.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridCopies.ColumnHeadersHeight = 32;

            gridBooks.ColumnHeadersVisible = true;
            gridBooks.BorderStyle = BorderStyle.FixedSingle;
            gridCopies.ColumnHeadersVisible = true;
            gridCopies.BorderStyle = BorderStyle.FixedSingle;

            EnsureNoTextCut(gridBooks);
            EnsureNoTextCut(gridCopies);

            btnBookSearch.Click += async (_, __) => await ReloadBooksAsync();
            btnAddBook.Click += async (_, __) => await AddBookDialogAsync();
            btnEditBook.Click += async (_, __) => await EditBookDialogAsync();
            btnDeleteBook.Click += async (_, __) => await DeleteBookAsync();
            gridBooks.SelectionChanged += async (_, __) => await LoadCopiesForSelectedBookAsync();
            btnAddCopy.Click += async (_, __) => await AddCopyDialogAsync();

            gridBooks.DataBindingComplete += OnGridBooksDataBindingComplete;
            gridCopies.DataBindingComplete += OnGridCopiesDataBindingComplete;

            gridCopies.CellFormatting += (s, e) =>
            {
                if (gridCopies.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
                {
                    var sVal = Convert.ToInt32(e.Value);
                    e.Value = sVal switch { 0 => "可借", 1 => "外借中", 2 => "遺失", 3 => "毀損", _ => sVal.ToString() };
                    e.FormattingApplied = true;
                }
            };
        }


        private void BuildMembersTab()
        {
            var tab = new TabPage("會員");
            tabs.TabPages.Add(tab);

            // 搜尋區
            var top = new FlowLayoutPanel() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            top.Controls.Add(new Label() { Text = "關鍵字：", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            top.Controls.Add(tbMemberSearch);
            top.Controls.Add(btnMemberSearch);
            top.Controls.Add(btnAddMember);
            top.Controls.Add(btnEditMember);
            top.Controls.Add(btnDeleteMember);

            // 主 Panel
            var panel = new Panel() { Dock = DockStyle.Fill };
            gridMembers.Dock = DockStyle.Fill;
            panel.Controls.Add(gridMembers); // 先加 DataGridView
            panel.Controls.Add(top);         // 再加搜尋區
            tab.Controls.Add(panel);

            // DataGridView 設定
            gridMembers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridMembers.ColumnHeadersHeight = 32;
            gridMembers.ColumnHeadersVisible = true;
            gridMembers.BorderStyle = BorderStyle.FixedSingle;

            EnsureNoTextCut(gridMembers);

            btnMemberSearch.Click += async (_, __) => await ReloadMembersAsync();
            btnAddMember.Click += async (_, __) => await AddMemberDialogAsync();
            btnEditMember.Click += async (_, __) => await EditMemberDialogAsync();
            btnDeleteMember.Click += async (_, __) => await DeleteMemberAsync();
            tab.Enter += async (_, __) => await ReloadMembersAsync();

            gridMembers.DataBindingComplete += OnGridMembersDataBindingComplete;
        }


        private void BuildBorrowTab()
        {
            var tab = new TabPage("借出 / 歸還");
            tabs.TabPages.Add(tab);

            // 操作區
            var top = new FlowLayoutPanel() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            top.Controls.Add(new Label() { Text = "BookId：", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            top.Controls.Add(numBookId);
            top.Controls.Add(new Label() { Text = "MemberId：", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            top.Controls.Add(numMemberId);
            top.Controls.Add(btnBorrow);
            top.Controls.Add(btnReturn);

            // 會員未歸還區
            var second = new FlowLayoutPanel() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            second.Controls.Add(new Label() { Text = "查看會員未歸還 Loan：MemberId", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            second.Controls.Add(numLoansMemberId);
            second.Controls.Add(btnLoadLoans);

            // 主 Panel
            var panel = new Panel() { Dock = DockStyle.Fill };
            gridLoans.Dock = DockStyle.Fill;
            panel.Controls.Add(gridLoans); // 先加 DataGridView
            panel.Controls.Add(second);    // 再加會員未歸還區
            panel.Controls.Add(top);       // 最上方操作區
            tab.Controls.Add(panel);

            // DataGridView 設定
            gridLoans.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridLoans.ColumnHeadersHeight = 32;
            gridLoans.ColumnHeadersVisible = true;
            gridLoans.BorderStyle = BorderStyle.FixedSingle;

            EnsureNoTextCut(gridLoans);

            btnBorrow.Click += async (_, __) => await BorrowAsync();
            btnReturn.Click += async (_, __) => await ReturnAsync();
            btnLoadLoans.Click += async (_, __) => await LoadOpenLoansAsync();

            gridLoans.DataBindingComplete += OnGridLoansDataBindingComplete;
        }


        // ===== Books/Copies =====
        private async Task ReloadBooksAsync()
        {
            try
            {
                var list = await _svc.GetBooksAsync(tbBookSearch.Text.Trim());
                gridBooks.DataSource = list
                    .OrderBy(b => b.BookId) // 依 BookId 排序
                    .Select(b => new
                    {
                        b.BookId,
                        b.ISBN,
                        b.Title,
                        b.Category,
                        b.Publisher,
                        b.PublishYear,
                        b.CreatedAt,
                        Authors = b.BookAuthors != null
                            ? string.Join(", ", b.BookAuthors.Select(ba => ba.Author != null ? ba.Author.Name : ""))
                            : ""
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入書籍資料失敗：" + ex.Message);
            }
        }

        private async Task LoadCopiesForSelectedBookAsync()
        {
            // 取得選取列的 BookId
            if (gridBooks.CurrentRow?.DataBoundItem is not null)
            {
                var bookIdProp = gridBooks.CurrentRow.DataBoundItem.GetType().GetProperty("BookId");
                if (bookIdProp == null) return;
                int bookId = (int)bookIdProp.GetValue(gridBooks.CurrentRow.DataBoundItem)!;

                try
                {
                    var copies = await _svc.GetCopiesByBookAsync(bookId);
                    gridCopies.DataSource = copies.Select(c => new
                    {
                        c.BookId,
                        c.Barcode,
                        Status = c.Status, // 保留原始數值
                        c.Location,
                        c.CreatedAt,
                        Borrower = c.Status == 1 && c.Loans != null
                            ? c.Loans.Where(l => l.ReturnDate == null).Select(l => l.Member != null ? l.Member.Name : "").FirstOrDefault() ?? ""
                            : ""
                    }).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("載入館藏失敗：" + ex.Message);
                }
            }
        }

        private async Task AddBookDialogAsync()
        {
            using var f = new Form() { Text = "新增書籍", Width = 420, Height = 320 };
            var tbIsbn = new TextBox() { Width = 260 };
            var tbTitle = new TextBox() { Width = 260 };
            var tbCategory = new TextBox() { Width = 260 }; // 新增分類欄位
            var tbPublisher = new TextBox() { Width = 260 };
            var tbYear = new TextBox() { Width = 100 };
            var tbAuthors = new TextBox() { Width = 260 };
            var ok = new Button() { Text = "儲存", DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel };

            var panel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label() { Text = "ISBN：", AutoSize = true }, 0, 0); panel.Controls.Add(tbIsbn, 1, 0);
            panel.Controls.Add(new Label() { Text = "書名：", AutoSize = true }, 0, 1); panel.Controls.Add(tbTitle, 1, 1);
            panel.Controls.Add(new Label() { Text = "分類：", AutoSize = true }, 0, 2); panel.Controls.Add(tbCategory, 1, 2);
            panel.Controls.Add(new Label() { Text = "出版社：", AutoSize = true }, 0, 3); panel.Controls.Add(tbPublisher, 1, 3);
            panel.Controls.Add(new Label() { Text = "出版年：", AutoSize = true }, 0, 4); panel.Controls.Add(tbYear, 1, 4);
            panel.Controls.Add(new Label() { Text = "作者（可多位，逗號分隔）：", AutoSize = true }, 0, 5); panel.Controls.Add(tbAuthors, 1, 5);
            var btns = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btns.Controls.Add(ok); btns.Controls.Add(cancel);
            panel.Controls.Add(btns, 1, 6);
            f.Controls.Add(panel);

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    int? year = int.TryParse(tbYear.Text, out var y) ? y : null;
                    var authors = tbAuthors.Text.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToArray();
                    await _svc.AddBookWithAuthorsAsync(
                        tbIsbn.Text.Trim(),
                        tbTitle.Text.Trim(),
                        tbPublisher.Text.Trim(),
                        year,
                        authors,
                        tbCategory.Text.Trim() // 傳遞分類
                    );
                    await ReloadBooksAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增書籍失敗：" + ex.Message);
                }
            }
        }

        private async Task EditBookDialogAsync()
        {
            if (gridBooks.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("請先選擇一本書。");
                return;
            }
            var bookIdProp = gridBooks.CurrentRow.DataBoundItem.GetType().GetProperty("BookId");
            if (bookIdProp == null) return;
            int bookId = (int)bookIdProp.GetValue(gridBooks.CurrentRow.DataBoundItem)!;

            var book = (await _svc.GetBooksAsync()).FirstOrDefault(b => b.BookId == bookId);
            if (book == null) return;

            using var f = new Form() { Text = "更新書籍", Width = 420, Height = 320 };
            var tbIsbn = new TextBox() { Width = 260, Text = book.ISBN };
            var tbTitle = new TextBox() { Width = 260, Text = book.Title };
            var tbCategory = new TextBox() { Width = 260, Text = book.Category ?? "" }; // 新增分類欄位
            var tbPublisher = new TextBox() { Width = 260, Text = book.Publisher ?? "" };
            var tbYear = new TextBox() { Width = 100, Text = book.PublishYear?.ToString() ?? "" };
            var tbAuthors = new TextBox() { Width = 260, Text = string.Join(", ", book.BookAuthors.Select(ba => ba.Author.Name)) };
            var ok = new Button() { Text = "儲存", DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel };

            var panel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label() { Text = "ISBN：", AutoSize = true }, 0, 0); panel.Controls.Add(tbIsbn, 1, 0);
            panel.Controls.Add(new Label() { Text = "書名：", AutoSize = true }, 0, 1); panel.Controls.Add(tbTitle, 1, 1);
            panel.Controls.Add(new Label() { Text = "分類：", AutoSize = true }, 0, 2); panel.Controls.Add(tbCategory, 1, 2);
            panel.Controls.Add(new Label() { Text = "出版社：", AutoSize = true }, 0, 3); panel.Controls.Add(tbPublisher, 1, 3);
            panel.Controls.Add(new Label() { Text = "出版年：", AutoSize = true }, 0, 4); panel.Controls.Add(tbYear, 1, 4);
            panel.Controls.Add(new Label() { Text = "作者（可多位，逗號分隔）：", AutoSize = true }, 0, 5); panel.Controls.Add(tbAuthors, 1, 5);
            var btns = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btns.Controls.Add(ok); btns.Controls.Add(cancel);
            panel.Controls.Add(btns, 1, 6);
            f.Controls.Add(panel);

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    int? year = int.TryParse(tbYear.Text, out var y) ? y : null;
                    var authors = tbAuthors.Text.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToArray();
                    await _svc.UpdateBookAsync(
                        bookId,
                        tbIsbn.Text.Trim(),
                        tbTitle.Text.Trim(),
                        tbPublisher.Text.Trim(),
                        year,
                        authors,
                        tbCategory.Text.Trim() // 傳遞分類
                    );
                    await ReloadBooksAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("更新書籍失敗：" + ex.Message);
                }
            }
        }

        private async Task DeleteBookAsync()
        {
            if (gridBooks.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("請先選擇一本書。");
                return;
            }
            var bookIdProp = gridBooks.CurrentRow.DataBoundItem.GetType().GetProperty("BookId");
            if (bookIdProp == null) return;
            int bookId = (int)bookIdProp.GetValue(gridBooks.CurrentRow.DataBoundItem)!;

            var result = MessageBox.Show("確認要刪除此書籍及所有館藏嗎？", "刪除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    await _svc.DeleteBookAsync(bookId);
                    await ReloadBooksAsync();
                    gridCopies.DataSource = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("刪除書籍失敗：" + ex.Message);
                }
            }
        }

        private async Task AddCopyDialogAsync()
        {
            // 從 gridBooks 取得選取列的 BookId
            if (gridBooks.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("請先選擇一本書。");
                return;
            }
            var bookIdProp = gridBooks.CurrentRow.DataBoundItem.GetType().GetProperty("BookId");
            if (bookIdProp == null)
            {
                MessageBox.Show("請先選擇一本書。");
                return;
            }
            int bookId = (int)bookIdProp.GetValue(gridBooks.CurrentRow.DataBoundItem)!;

            using var f = new Form() { Text = "新增館藏", Width = 380, Height = 180 };
            var tbBarcode = new TextBox() { Width = 220 };
            var tbLoc = new TextBox() { Width = 220 };
            var ok = new Button() { Text = "儲存", DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel };

            var panel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label() { Text = "條碼：", AutoSize = true }, 0, 0); panel.Controls.Add(tbBarcode, 1, 0);
            panel.Controls.Add(new Label() { Text = "位置：", AutoSize = true }, 0, 1); panel.Controls.Add(tbLoc, 1, 1);
            var btns = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btns.Controls.Add(ok); btns.Controls.Add(cancel);
            panel.Controls.Add(btns, 1, 2);
            f.Controls.Add(panel);

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    await _svc.AddCopyAsync(bookId, tbBarcode.Text.Trim(), tbLoc.Text.Trim());
                    await LoadCopiesForSelectedBookAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增館藏失敗：" + ex.Message);
                }
            }
        }

        // ===== Members =====
        private async Task ReloadMembersAsync()
        {
            try
            {
                var list = await _svc.GetMembersAsync(tbMemberSearch.Text.Trim());
                gridMembers.DataSource = list
                    .OrderBy(m => m.MemberId)
                    .Select(m => new
                    {
                        m.MemberId,
                        m.Name,
                        m.Email,
                        m.Phone,
                        m.JoinDate,
                        // 只顯示目前尚未歸還的 BookId
                        Loans = m.Loans != null
                            ? string.Join(", ", m.Loans
                                .Where(l => l.ReturnDate == null)
                                .Select(l => l.Copy.BookId))
                            : ""
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入會員失敗：" + ex.Message);
            }
        }

        private async Task AddMemberDialogAsync()
        {
            using var f = new Form() { Text = "新增會員", Width = 420, Height = 220 };
            var tbName = new TextBox() { Width = 260 };
            var tbEmail = new TextBox() { Width = 260 };
            var tbPhone = new TextBox() { Width = 260 };
            var ok = new Button() { Text = "儲存", DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel };

            var panel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label() { Text = "姓名：", AutoSize = true }, 0, 0); panel.Controls.Add(tbName, 1, 0);
            panel.Controls.Add(new Label() { Text = "Email：", AutoSize = true }, 0, 1); panel.Controls.Add(tbEmail, 1, 1);
            panel.Controls.Add(new Label() { Text = "電話：", AutoSize = true }, 0, 2); panel.Controls.Add(tbPhone, 1, 2);
            var btns = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btns.Controls.Add(ok); btns.Controls.Add(cancel);
            panel.Controls.Add(btns, 1, 3);
            f.Controls.Add(panel);

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    await _svc.AddMemberAsync(tbName.Text.Trim(), tbEmail.Text.Trim(), tbPhone.Text.Trim());
                    await ReloadMembersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增會員失敗：" + ex.Message);
                }
            }
        }

        private async Task EditMemberDialogAsync()
        {
            if (gridMembers.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("請先選擇一位會員。");
                return;
            }
            var memberIdProp = gridMembers.CurrentRow.DataBoundItem.GetType().GetProperty("MemberId");
            if (memberIdProp == null) return;
            int memberId = (int)memberIdProp.GetValue(gridMembers.CurrentRow.DataBoundItem)!;

            var member = (await _svc.GetMembersAsync()).FirstOrDefault(m => m.MemberId == memberId);
            if (member == null) return;

            using var f = new Form() { Text = "更新會員", Width = 420, Height = 220 };
            var tbName = new TextBox() { Width = 260, Text = member.Name };
            var tbEmail = new TextBox() { Width = 260, Text = member.Email };
            var tbPhone = new TextBox() { Width = 260, Text = member.Phone };
            var ok = new Button() { Text = "儲存", DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel };

            var panel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label() { Text = "姓名：", AutoSize = true }, 0, 0); panel.Controls.Add(tbName, 1, 0);
            panel.Controls.Add(new Label() { Text = "Email：", AutoSize = true }, 0, 1); panel.Controls.Add(tbEmail, 1, 1);
            panel.Controls.Add(new Label() { Text = "電話：", AutoSize = true }, 0, 2); panel.Controls.Add(tbPhone, 1, 2);
            var btns = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btns.Controls.Add(ok); btns.Controls.Add(cancel);
            panel.Controls.Add(btns, 1, 3);
            f.Controls.Add(panel);

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    await _svc.UpdateMemberAsync(memberId, tbName.Text.Trim(), tbEmail.Text.Trim(), tbPhone.Text.Trim());
                    await ReloadMembersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("更新會員失敗：" + ex.Message);
                }
            }
        }

        private async Task DeleteMemberAsync()
        {
            if (gridMembers.CurrentRow?.DataBoundItem == null)
            {
                MessageBox.Show("請先選擇一位會員。");
                return;
            }
            var memberIdProp = gridMembers.CurrentRow.DataBoundItem.GetType().GetProperty("MemberId");
            if (memberIdProp == null) return;
            int memberId = (int)memberIdProp.GetValue(gridMembers.CurrentRow.DataBoundItem)!;

            var result = MessageBox.Show("確認要刪除此會員嗎？", "刪除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    await _svc.DeleteMemberAsync(memberId);
                    await ReloadMembersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("刪除會員失敗：" + ex.Message);
                }
            }
        }

        // ===== Borrow / Return =====
        private async Task BorrowAsync()
        {
            try
            {
                await _svc.BorrowByBookAsync((int)numBookId.Value, (int)numMemberId.Value, 14);
                MessageBox.Show("借出成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("借出失敗：" + ex.Message);
            }
        }

        private async Task ReturnAsync()
        {
            try
            {
                await _svc.ReturnByBookAsync((int)numBookId.Value, (int)numMemberId.Value, 10m);
                MessageBox.Show("歸還成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("歸還失敗：" + ex.Message);
            }
        }

        private async Task LoadOpenLoansAsync()
        {
            try
            {
                var list = await _svc.GetOpenLoansByMemberAsync((int)numLoansMemberId.Value);
                gridLoans.DataSource = list.Select(l => new
                {
                    l.LoanId,
                    l.MemberId,
                    l.CopyId,
                    BookTitle = l.Copy.Book.Title,
                    l.LoanDate,
                    l.DueDate
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入未歸還清單失敗：" + ex.Message);
            }
        }

        private void OnGridBooksDataBindingComplete(object? s, EventArgs e)
        {
            var g = (DataGridView)s!;
            foreach (DataGridViewColumn col in g.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            var title = g.Columns["Title"];
            if (title != null)
            {
                title.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                title.FillWeight = 250;
                title.MinimumWidth = 200;
                title.DefaultCellStyle.WrapMode = DataGridViewTriState.False; // 要換行可改 True
            }

            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 32; // 或你想要的高度

            g.AutoResizeColumns();
            g.AutoResizeRows();
            g.ScrollBars = ScrollBars.Both;
        }

        private void OnGridCopiesDataBindingComplete(object? s, EventArgs e)
        {
            var g = (DataGridView)s!;
            foreach (DataGridViewColumn col in g.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            // 隱藏 CopyId 欄位
            if (g.Columns.Contains("CopyId"))
                g.Columns["CopyId"].Visible = false;

            var loc = g.Columns["Location"];
            if (loc != null)
            {
                loc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                loc.FillWeight = 150;
                loc.MinimumWidth = 150;
                loc.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            }

            g.AutoResizeColumns();
            g.AutoResizeRows();
            g.ScrollBars = ScrollBars.Both;
        }

        private void OnGridMembersDataBindingComplete(object? s, EventArgs e)
        {
            var g = (DataGridView)s!;
            foreach (DataGridViewColumn col in g.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            var email = g.Columns["Email"];
            if (email != null)
            {
                email.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                email.FillWeight = 200;
                email.MinimumWidth = 180;
            }

            g.AutoResizeColumns();
            g.AutoResizeRows();
            g.ScrollBars = ScrollBars.Both;
        }

        private void OnGridLoansDataBindingComplete(object? s, EventArgs e)
        {
            var g = (DataGridView)s!;
            foreach (DataGridViewColumn col in g.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            var title = g.Columns["BookTitle"]; // 借閱清單的書名欄
            if (title != null)
            {
                title.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                title.FillWeight = 220;
                title.MinimumWidth = 180;
            }

            g.AutoResizeColumns();
            g.AutoResizeRows();
            g.ScrollBars = ScrollBars.Both;
        }

    }
}
