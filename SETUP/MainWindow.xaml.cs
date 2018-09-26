using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using System.Data;

namespace SETUP
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow AppWindow;//MainWindow의 정적인스턴스 선언

        string[,] ArraySteel = new string[32,3];
        public Material material = new Material();
        public Mill mill = new Mill();
        
        public MainWindow()
        {
            InitializeComponent();
            AppWindow = this;   // MainWindows 정적 인스턴스 초기화
            mill.ReadMillData(); // Mill Data Read
            mill.ReadSpeed(); // Speed Data Read
            int l = material.ReadSteelGrade(ref ArraySteel); //강종, 계열, 최대단위장력 Table Read
            for (int i = 0; i < l; i++) //콤보박스 강종등록
                cboSteel.Items.Add(ArraySteel[i,0]);
            DisplayReductionCheck(false);
        }

        private void cboSteel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSteel.SelectedItem == null) return; //콤보박스 초기화로 인한 이벤트 발생시 재실행 방지를 위한 종료
            if (cboSteel.SelectedIndex >= 0)
            {
                
                material.SteelGrade = cboSteel.SelectedItem.ToString();
                for (int i = 0; i < ArraySteel.Length; i++)
                {
                    if (material.SteelGrade == ArraySteel[i, 0])
                    {
                        material.SteelSeries = ArraySteel[i, 1];
                        material.MaxUnitTension = double.Parse(ArraySteel[i, 2]);
                        break;
                    }
                }
                material.ReadStress(material.SteelGrade);  
            }
            if (!CheckInputData())
            {
                material.InpuData();
                material.ReadThick2(material.SteelSeries);
                ChangedInputData();
            }
            else
            {
                cboSteel.SelectedIndex = -1;//에러 발생시 콤보박스 초기화
            }
        }

        private void ChangedInputData()
        {
            if (material.SteelGrade != null)
            {
                material.ReadR1maxRmean(material.SteelSeries, txtEntryThickness.Text);
                txtR1max.Text = String.Format("{0:F1}", material.R1max);
                txtRmean.Text = String.Format("{0:F1}", material.Rmean);
            }

        }

        private void ModalWindow(string str)
        {
            Error err = new Error();
            err.Owner = this;
            err.txbTextBlock.Text = str;
            err.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            err.ShowDialog();
        }

        private bool CheckInputData()
        {
            if (txtEntryThickness.Text == "") {ModalWindow("Please input the Entry Thickness."); return true; }
            if (txtExitThickness.Text == "") { ModalWindow("Please input the Exit Thickness."); return true; }
            if (txtWidth.Text == "") { ModalWindow("Please input the Width."); return true; }
            if (double.Parse(txtEntryThickness.Text) <= double.Parse(txtExitThickness.Text)) { ModalWindow("Entry Thickness is smaller than Exit Thickness. Please Check."); return true; }
            if (double.Parse(txtWidth.Text) > 1300.0) { ModalWindow("The Width must be less than 1300mm. Please check."); return true; }
            if (double.Parse(txtEntryThickness.Text) > 3.0) { ModalWindow("Entry Thickness should be less than 3.0mm. Please check."); return true; }
            return false;
        }

        private void DisplayReductionCheck(bool check)
        {
            txtR1max.IsEnabled = check;
            txtRmean.IsEnabled = check;
            lblR1max.IsEnabled = check;
            lblRmean.IsEnabled = check;
            scbR1max.IsEnabled = check;
            scbRmean.IsEnabled = check;
            lblR1.IsEnabled = check;
            lblRm.IsEnabled = check;
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            if (CheckInputData()) return;
            material.InpuData();
            material.Schedule();
            Result();
           
        }

        private void Result()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("No", typeof(string));
            dt.Columns.Add("Hen", typeof(string));
            dt.Columns.Add("Hex", typeof(string));
            dt.Columns.Add("Red", typeof(string));
            dt.Columns.Add("TRed", typeof(string));
            dt.Columns.Add("Speed", typeof(string));
            dt.Columns.Add("Ten", typeof(string));
            dt.Columns.Add("Tex", typeof(string));
            dt.Columns.Add("TTen", typeof(string));
            dt.Columns.Add("TTex", typeof(string));
            dt.Columns.Add("RForce", typeof(string));

            DataRow row;

            for (int i = 1; i <= 19; i++)
            {
                row = dt.NewRow();
                if (i <= material.n)
                {
                    row["No"] = i.ToString();
                    row["Hen"] = material.Hexit[i - 1].ToString("0.000");
                    row["Hex"] = material.Hexit[i].ToString("0.000");
                    row["Red"] = material.RRA[i].ToString("0.0");
                    row["TRed"] = material.RRTA[i].ToString("0.0");
                    row["Speed"] = material.VA[i].ToString("0");
                    row["Ten"] = material.TENA[i].ToString("0.0");
                    row["Tex"] = material.TEXA[i].ToString("0.0");
                    row["TTen"] = material.TTENA[i].ToString("0.0");
                    row["TTex"] = material.TTEXA[i].ToString("0.0");
                    row["RForce"] = material.PTOTALA[i].ToString("0");
                }
                else
                {
                    row["No"] = "";
                    row["Hen"] = "";
                    row["Hex"] = "";
                    row["Red"] = "";
                    row["TRed"] = "";
                    row["Speed"] = "";
                    row["Ten"] = "";
                    row["Tex"] = "";
                    row["TTen"] = "";
                    row["TTex"] = "";
                    row["RForce"] = "";
                }
                dt.Rows.Add(row);
            }
            dgPass.ItemsSource = dt.DefaultView;
        }


        private void chkAdjReduction_Checked(object sender, RoutedEventArgs e)
        {
            DisplayReductionCheck(true);
        }

        private void chkAdjReduction_Unchecked(object sender, RoutedEventArgs e)
        {
            DisplayReductionCheck(false);
            ChangedInputData();
        }

        private void txtEntryThickness_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach(char c in e.Text)           
                if (e.Text != ".")
                    if (!char.IsDigit(c)) { e.Handled = true; break; }
        }

        private void txtExitThickness_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (e.Text != ".")
                    if (!char.IsDigit(c)) { e.Handled = true; break; }
        }

        private void txtWidth_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (e.Text != ".")
                    if (!char.IsDigit(c)) { e.Handled = true; break; }
        }

        private void txtR1max_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (e.Text != ".")
                    if (!char.IsDigit(c)) { e.Handled = true; break; }
        }

        private void txtRmean_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (e.Text != ".")
                    if (!char.IsDigit(c)) { e.Handled = true; break; }
        }

        private void scbR1max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value;
            if (e.OldValue < e.NewValue)
                value = - 0.1;
            else
                value = 0.1;
            txtR1max.Text = String.Format("{0:F1}", double.Parse(txtR1max.Text) + value);
            material.R1max = double.Parse(txtR1max.Text);
        }

        private void scbRmean_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value;
            if (e.OldValue < e.NewValue)
                value = -0.1;
            else
                value = 0.1;
            txtRmean.Text = String.Format("{0:F1}", double.Parse(txtRmean.Text) + value);
            material.Rmean = double.Parse(txtRmean.Text);
        }

        private void txtEntryThickness_TextChanged(object sender, TextChangedEventArgs e)
        {
            material.EntryThickness = double.Parse(txtEntryThickness.Text);
            ChangedInputData();
        }
    }
}
