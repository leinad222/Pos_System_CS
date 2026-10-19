using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pos_System_CS
{
    public partial class Form1 : Form
    {
        // Printing helpers
        private List<Bitmap> _printPages;
        private int _currentPrintPage;

        // UI helpers for items
        private System.Windows.Forms.ListBox _lstItems;
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                // Create sale from DataGridView rows
                var saleItems = new List<Pos_System_CS.Data.SaleItem>();
                decimal total = 0m;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    string name = Convert.ToString(row.Cells[0].Value);
                    int qty = 1;
                    if (!int.TryParse(Convert.ToString(row.Cells[1].Value), out qty)) qty = 1;
                    decimal amt = 0m;
                    decimal.TryParse(Convert.ToString(row.Cells[2].Value), out amt);

                    // Try to resolve item id by name
                    var item = Pos_System_CS.Data.DataAccess.GetItems().Find(i => i.Name == name);
                    int itemId = item?.Id ?? 0;

                    saleItems.Add(new Pos_System_CS.Data.SaleItem { ItemId = itemId, Quantity = qty, Amount = amt });
                    total += amt;
                }

                if (saleItems.Count == 0)
                {
                    MessageBox.Show("No items to sell.");
                    return;
                }

                int saleId = Pos_System_CS.Data.DataAccess.CreateSale(total, saleItems);
                if (saleId > 0)
                {
                    MessageBox.Show($"Sale recorded (Id: {saleId})");
                    dataGridView1.Rows.Clear();
                    UpdateTotals();
                }
                else
                {
                    MessageBox.Show("Failed to record sale. See log for details.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Pay button failed", ex);
                MessageBox.Show("An error occurred while processing payment. See logs.");
            }
        }
        Bitmap bitmap;
        private void button14_Click(object sender, EventArgs e)
        {
            if (dataGridView1 == null || dataGridView1.RowCount == 0)
            {
                MessageBox.Show("There is no data to print.");
                return;
            }

            int originalHeight = dataGridView1.Height;
            try
            {
                // Expand the grid to include all rows for rendering. Limit to a reasonable max to avoid huge bitmaps.
                int desiredHeight = Math.Min(dataGridView1.RowCount * dataGridView1.RowTemplate.Height * 2, 3000);
                dataGridView1.Height = desiredHeight;
                bitmap = new Bitmap(dataGridView1.Width, dataGridView1.Height);
                dataGridView1.DrawToBitmap(bitmap, new Rectangle(0, 0, dataGridView1.Width, dataGridView1.Height));
                // Reset paging state; pages will be generated on first PrintPage call because page bounds are known then
                _printPages = null;
                _currentPrintPage = 0;
                printPreviewDialog1.Document = printDocument1;
                printPreviewDialog1.PrintPreviewControl.Zoom = 1;
                printPreviewDialog1.ShowDialog();
            }
            catch (Exception ex)
            {
                // Log detailed error and show friendly message
                Logger.LogError("Error printing DataGridView", ex);
                MessageBox.Show("An error occurred while trying to print. See log for details.");
            }
            finally
            {
                // Ensure original height is restored even on error
                try { dataGridView1.Height = originalHeight; } catch { }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cboPayment.Items.Add("Cash");
            cboPayment.Items.Add("Visa Card");
            cboPayment.Items.Add("Master Card");

            // Add a small 'View Logs' button to open the log viewer
            try
            {
                var btnLogs = new Button();
                btnLogs.Text = "View Logs";
                btnLogs.Size = new Size(100, 30);
                btnLogs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                btnLogs.Location = new Point(this.ClientSize.Width - btnLogs.Width - 10, 10);
                btnLogs.Click += (s, ev) =>
                {
                    try
                    {
                        using (var viewer = new LogViewer())
                        {
                            viewer.ShowDialog(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to open log viewer", ex);
                        MessageBox.Show("Could not open log viewer. See log for details.");
                    }
                };
                this.Controls.Add(btnLogs);
            }
            catch { }
            // Add items list on the right side for quick add
            try
            {
                _lstItems = new System.Windows.Forms.ListBox();
                _lstItems.Width = 300;
                _lstItems.Height = 400;
                _lstItems.Location = new Point(1400, 73);
                _lstItems.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _lstItems.DoubleClick += LstItems_DoubleClick;
                this.Controls.Add(_lstItems);

                LoadItemsToList();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create items list", ex);
            }
        }

        private void LoadItemsToList()
        {
            try
            {
                _lstItems.Items.Clear();
                var items = Pos_System_CS.Data.DataAccess.GetItems();
                foreach (var it in items)
                {
                    _lstItems.Items.Add(it);
                }
                _lstItems.DisplayMember = "Name";
            }
            catch (Exception ex)
            {
                Logger.LogError("LoadItemsToList failed", ex);
            }
        }

        private void LstItems_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                var it = _lstItems.SelectedItem as Pos_System_CS.Data.Item;
                if (it == null) return;
                // Add to DataGridView: Item, Qty, Amount
                dataGridView1.Rows.Add(it.Name, 1, it.Price.ToString("F2"));
                UpdateTotals();
            }
            catch (Exception ex)
            {
                Logger.LogError("LstItems_DoubleClick failed", ex);
            }
        }

        private void UpdateTotals()
        {
            try
            {
                decimal subtotal = 0m;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (decimal.TryParse(Convert.ToString(row.Cells[2].Value), out var amount))
                    {
                        subtotal += amount;
                    }
                }
                lblSubTotal.Text = subtotal.ToString("F2");
                decimal tax = Math.Round(subtotal * 0.07m, 2);
                lblTax.Text = tax.ToString("F2");
                lblTotal.Text = (subtotal + tax).ToString("F2");
            }
            catch (Exception ex)
            {
                Logger.LogError("UpdateTotals failed", ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            try
            {
                if (bitmap == null)
                {
                    Logger.LogError("printDocument1_PrintPage called with null bitmap", null);
                    e.HasMorePages = false;
                    return;
                }

                // On first call, slice the large bitmap into page-sized bitmaps based on printable area
                if (_printPages == null)
                {
                    _printPages = new List<Bitmap>();
                    int pageWidth = e.MarginBounds.Width;
                    int pageHeight = e.MarginBounds.Height;

                    // If DataGridView is wider than printable area, we'll scale pages when drawing
                    int y = 0;
                    while (y < bitmap.Height)
                    {
                        int sliceHeight = Math.Min(pageHeight, bitmap.Height - y);
                        var pageBmp = new Bitmap(bitmap.Width, sliceHeight);
                        using (var g = Graphics.FromImage(pageBmp))
                        {
                            g.Clear(Color.White);
                            g.DrawImage(bitmap, new Rectangle(0, 0, pageBmp.Width, pageBmp.Height), new Rectangle(0, y, pageBmp.Width, pageBmp.Height), GraphicsUnit.Pixel);
                        }
                        _printPages.Add(pageBmp);
                        y += sliceHeight;
                    }
                }

                if (_currentPrintPage < 0 || _currentPrintPage >= _printPages.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                var pageToDraw = _printPages[_currentPrintPage];

                // Scale to fit printable margin bounds
                float scale = Math.Min((float)e.MarginBounds.Width / pageToDraw.Width, (float)e.MarginBounds.Height / pageToDraw.Height);
                int drawW = (int)(pageToDraw.Width * scale);
                int drawH = (int)(pageToDraw.Height * scale);
                int drawX = e.MarginBounds.X + (e.MarginBounds.Width - drawW) / 2;
                int drawY = e.MarginBounds.Y;

                e.Graphics.DrawImage(pageToDraw, new Rectangle(drawX, drawY, drawW, drawH));

                _currentPrintPage++;
                e.HasMorePages = _currentPrintPage < _printPages.Count;

                // If this was the last page, clean up
                if (!e.HasMorePages)
                {
                    foreach (var b in _printPages)
                    {
                        try { b.Dispose(); } catch { }
                    }
                    _printPages = null;
                    try { bitmap.Dispose(); } catch { }
                    bitmap = null;
                    _currentPrintPage = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during PrintPage", ex);
                MessageBox.Show("An error occurred while trying to print the document. See log for details.");
            }
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {

        }

        private void btn_Reset_Click_1(object sender, EventArgs e)
        {
            try
            {
                lblBarCode.Text = "";
                lblCost.Text = "";
                lblChange.Text = "";
                lblSubTotal.Text = "";
                lblTax.Text = "";
                lblTotal.Text = "";
                dataGridView1.Rows.Clear();
                dataGridView1.Refresh();
                lblMethodofPayment.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while trying to reset the form: " + ex.Message);
            }
        }

        private void cboPayment_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click_Log(object sender, EventArgs e)
        {
            Logger.LogInfo("lblBarCode clicked");
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
