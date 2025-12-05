using EntityGenerator.EntityGenerator.Wpf;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;

namespace EntityGenerator.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtOutputFolder.Text = dlg.SelectedPath;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();

            var connString = txtConnectionString.Text.Trim();
            var outputFolder = txtOutputFolder.Text.Trim();
            var namespaceName = txtNamespace.Text.Trim();

            if (string.IsNullOrEmpty(connString))
            {
                MessageBox.Show("Please enter a connection string.");
                return;
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                MessageBox.Show("Please enter a namespace.");
                return;
            }

            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                MessageBox.Show("Please select a valid output folder.");
                return;
            }

            try
            {
                using var conn = new SqlConnection(connString);
                conn.Open();
                Log("Connected to database.");

                var tables = SchemaReader.GetTables(conn);

                foreach (var table in tables)
                {
                    Log($"Processing table: {table}");

                    var columns = SchemaReader.GetColumns(conn, table);

                    var baseCode = CodeGenerator.GenerateBaseClass(namespaceName, table, columns);
                    var derivedCode = CodeGenerator.GenerateDerivedClass(namespaceName, table, columns);

                    File.WriteAllText(Path.Combine(outputFolder, table + "Base.cs"), baseCode, Encoding.UTF8);
                    File.WriteAllText(Path.Combine(outputFolder, table + ".cs"), derivedCode, Encoding.UTF8);

                    Log($"Generated: {table}Base.cs, {table}.cs");
                }

                Log("Done.");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
        }
    }
}
